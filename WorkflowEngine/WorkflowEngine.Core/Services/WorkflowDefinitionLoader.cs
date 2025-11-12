using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Services
{
    /// <summary>
    /// Service for loading workflow definitions from JSON
    /// </summary>
    public class WorkflowDefinitionLoader
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public WorkflowDefinitionLoader()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        /// <summary>
        /// Load workflow definition from JSON string
        /// </summary>
        public WorkflowDefinition LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON content cannot be null or empty", nameof(json));

            try
            {
                return JsonSerializer.Deserialize<WorkflowDefinition>(json, _jsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse workflow definition JSON", ex);
            }
        }

        /// <summary>
        /// Load workflow definition from JSON file
        /// </summary>
        public async Task<WorkflowDefinition> LoadFromFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Workflow definition file not found: {filePath}");

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                return LoadFromJson(json);
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Failed to load workflow definition from file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Save workflow definition to JSON string
        /// </summary>
        public string SaveToJson(WorkflowDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return JsonSerializer.Serialize(definition, _jsonOptions);
        }

        /// <summary>
        /// Save workflow definition to JSON file
        /// </summary>
        public async Task SaveToFileAsync(WorkflowDefinition definition, string filePath)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var json = SaveToJson(definition);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Validate a workflow definition
        /// </summary>
        public void Validate(WorkflowDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.Id))
                throw new InvalidOperationException("Workflow definition must have an ID");

            if (string.IsNullOrWhiteSpace(definition.Name))
                throw new InvalidOperationException("Workflow definition must have a name");

            if (definition.Steps == null || definition.Steps.Count == 0)
                throw new InvalidOperationException("Workflow definition must have at least one step");

            if (string.IsNullOrWhiteSpace(definition.InitialStepId))
                throw new InvalidOperationException("Workflow definition must have an initial step ID");

            // Validate initial step exists
            if (!definition.Steps.Exists(s => s.Id == definition.InitialStepId))
                throw new InvalidOperationException($"Initial step '{definition.InitialStepId}' not found in workflow steps");

            // Process implicit transitions from NextStepId
            foreach (var step in definition.Steps)
            {
                // If NextStepId is specified but no transitions exist, create an implicit transition
                if (!string.IsNullOrWhiteSpace(step.NextStepId) && 
                    (step.Transitions == null || step.Transitions.Count == 0))
                {
                    step.Transitions = new List<Transition>
                    {
                        new Transition
                        {
                            Id = $"{step.Id}-to-{step.NextStepId}",
                            TargetStepId = step.NextStepId,
                            Label = "Next"
                        }
                    };
                }
            }

            // Validate all steps have IDs and types
            foreach (var step in definition.Steps)
            {
                if (string.IsNullOrWhiteSpace(step.Id))
                    throw new InvalidOperationException("All steps must have an ID");

                if (string.IsNullOrWhiteSpace(step.Type))
                    throw new InvalidOperationException($"Step '{step.Id}' must have a type");

                // Validate transition targets exist
                if (step.Transitions != null)
                {
                    foreach (var transition in step.Transitions)
                    {
                        if (!string.IsNullOrWhiteSpace(transition.TargetStepId) &&
                            !definition.Steps.Exists(s => s.Id == transition.TargetStepId))
                        {
                            throw new InvalidOperationException(
                                $"Transition target '{transition.TargetStepId}' in step '{step.Id}' not found");
                        }
                    }
                }

                // Validate NextStepId if specified
                if (!string.IsNullOrWhiteSpace(step.NextStepId) &&
                    !definition.Steps.Exists(s => s.Id == step.NextStepId))
                {
                    throw new InvalidOperationException(
                        $"NextStepId '{step.NextStepId}' in step '{step.Id}' not found");
                }

                // Warn if both NextStepId and Transitions are specified
                if (!string.IsNullOrWhiteSpace(step.NextStepId) && 
                    step.Transitions != null && step.Transitions.Count > 0)
                {
                    // NextStepId will be ignored if Transitions are defined
                    // This is by design - Transitions take precedence
                }
            }
        }
    }
}
