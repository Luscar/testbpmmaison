using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Integration;

namespace WorkflowEngine.Core.Services.Executors
{
    /// <summary>
    /// Executor for interaction steps (user input required)
    /// Integrates with external task management system
    /// </summary>
    public class InteractionStepExecutor : IStepExecutor
    {
        private readonly IExternalTaskSystem _externalTaskSystem;

        public InteractionStepExecutor(IExternalTaskSystem externalTaskSystem = null)
        {
            _externalTaskSystem = externalTaskSystem;
        }

        public bool CanExecute(string stepType) => stepType == StepTypes.InteractionStep;

        public async Task<StepExecutionResult> ExecuteAsync(
            StepInstance stepInstance, 
            StepDefinition stepDefinition, 
            WorkflowInstance workflowInstance)
        {
            var config = JsonSerializer.Deserialize<InteractionStepConfig>(
                JsonSerializer.Serialize(stepDefinition.Configuration));

            // If external task system is configured, create a task
            if (_externalTaskSystem != null)
            {
                try
                {
                    var taskInfo = new ExternalTaskInfo
                    {
                        WorkflowInstanceId = workflowInstance.Id,
                        StepInstanceId = stepInstance.Id,
                        Title = stepDefinition.Name,
                        Description = $"Workflow: {workflowInstance.WorkflowDefinitionId}\nStep: {stepDefinition.Name}",
                        AssignedUsers = config.AssignedUsers,
                        AssignedRoles = config.AssignedRoles,
                        FormSchema = config.FormSchema,
                        WorkflowContext = new Dictionary<string, object>(workflowInstance.Variables),
                        Metadata = new Dictionary<string, object>
                        {
                            ["stepType"] = stepDefinition.Type,
                            ["stepId"] = stepDefinition.Id,
                            ["workflowDefinitionId"] = workflowInstance.WorkflowDefinitionId
                        }
                    };

                    // Calculate due date if timeout is specified
                    if (config.TimeoutMinutes.HasValue)
                    {
                        taskInfo.DueDate = DateTime.UtcNow.AddMinutes(config.TimeoutMinutes.Value);
                    }

                    // Create task in external system
                    var externalTaskId = await _externalTaskSystem.CreateTaskAsync(taskInfo);

                    // Store the external task ID in the step instance for later reference
                    stepInstance.OutputData["externalTaskId"] = externalTaskId;
                    stepInstance.OutputData["taskCreatedAt"] = DateTime.UtcNow;

                    Console.WriteLine($"✓ External task created: {externalTaskId} for step {stepInstance.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Warning: Failed to create external task: {ex.Message}");
                    // Continue anyway - the workflow can still function without external task system
                    stepInstance.OutputData["externalTaskError"] = ex.Message;
                }
            }

            // Set the step to waiting for input
            return await Task.FromResult(new StepExecutionResult
            {
                Success = true,
                Status = StepStatus.WaitingForInput,
                OutputData = stepInstance.OutputData
            });
        }
    }
}
