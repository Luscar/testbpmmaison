using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Repositories;

namespace WorkflowEngine.Core.Services
{
    /// <summary>
    /// Main workflow engine implementation
    /// </summary>
    public class WorkflowEngine : IWorkflowEngine
    {
        private readonly IWorkflowDefinitionRepository _definitionRepository;
        private readonly IWorkflowInstanceRepository _instanceRepository;
        private readonly IStepInstanceRepository _stepRepository;
        private readonly IEnumerable<IStepExecutor> _stepExecutors;
        private readonly IExternalTaskSystem _externalTaskSystem;

        public WorkflowEngine(
            IWorkflowDefinitionRepository definitionRepository,
            IWorkflowInstanceRepository instanceRepository,
            IStepInstanceRepository stepRepository,
            IEnumerable<IStepExecutor> stepExecutors,
            IExternalTaskSystem externalTaskSystem = null)
        {
            _definitionRepository = definitionRepository;
            _instanceRepository = instanceRepository;
            _stepRepository = stepRepository;
            _stepExecutors = stepExecutors;
            _externalTaskSystem = externalTaskSystem;
        }

        public async Task<string> StartWorkflowAsync(
            string workflowDefinitionId,
            Dictionary<string, object> variables = null,
            string correlationId = null,
            string createdBy = null)
        {
            // Load workflow definition
            var definition = await _definitionRepository.GetByIdAsync(workflowDefinitionId);
            if (definition == null)
                throw new InvalidOperationException($"Workflow definition '{workflowDefinitionId}' not found");

            // Create workflow instance
            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = workflowDefinitionId,
                Status = WorkflowStatus.Created,
                Variables = variables ?? new Dictionary<string, object>(),
                CorrelationId = correlationId,
                CreatedBy = createdBy,
                CurrentStepId = definition.InitialStepId
            };

            // Merge definition variables with instance variables
            foreach (var defVar in definition.Variables)
            {
                if (!instance.Variables.ContainsKey(defVar.Key))
                {
                    instance.Variables[defVar.Key] = defVar.Value;
                }
            }

            var instanceId = await _instanceRepository.CreateAsync(instance);

            // Start executing the first step
            instance.Status = WorkflowStatus.Running;
            instance.StartedAt = DateTime.UtcNow;
            await _instanceRepository.UpdateAsync(instance);

            await ExecuteNextStepAsync(instanceId, definition.InitialStepId);

            return instanceId;
        }

        public async Task CompleteInteractionStepAsync(string stepInstanceId, Dictionary<string, object> outputData, string completedBy = null)
        {
            var stepInstance = await _stepRepository.GetByIdAsync(stepInstanceId);
            if (stepInstance == null)
                throw new InvalidOperationException($"Step instance '{stepInstanceId}' not found");

            if (stepInstance.Status != StepStatus.WaitingForInput)
                throw new InvalidOperationException($"Step instance '{stepInstanceId}' is not waiting for input");

            var workflowInstance = await _instanceRepository.GetByIdAsync(stepInstance.WorkflowInstanceId);
            if (workflowInstance == null)
                throw new InvalidOperationException($"Workflow instance '{stepInstance.WorkflowInstanceId}' not found");

            var definition = await _definitionRepository.GetByIdAsync(workflowInstance.WorkflowDefinitionId);
            var stepDefinition = definition.Steps.FirstOrDefault(s => s.Id == stepInstance.StepDefinitionId);

            // Close task in external system if it was created
            if (stepInstance.OutputData.ContainsKey("externalTaskId") && _externalTaskSystem != null)
            {
                try
                {
                    var externalTaskId = stepInstance.OutputData["externalTaskId"]?.ToString();
                    if (!string.IsNullOrEmpty(externalTaskId))
                    {
                        await _externalTaskSystem.CloseTaskAsync(externalTaskId, outputData);
                        Console.WriteLine($"✓ External task closed: {externalTaskId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Warning: Failed to close external task: {ex.Message}");
                    // Continue anyway - workflow should proceed even if external system call fails
                }
            }

            // Update step instance with output data
            stepInstance.OutputData = outputData;
            stepInstance.Status = StepStatus.Completed;
            stepInstance.CompletedAt = DateTime.UtcNow;

            // Merge output data into workflow variables
            foreach (var data in outputData)
            {
                workflowInstance.Variables[data.Key] = data.Value;
            }

            await _stepRepository.UpdateAsync(stepInstance);
            await _instanceRepository.UpdateAsync(workflowInstance);

            // Determine next step
            var nextStepId = stepDefinition.NextStepId;
            
            if (!string.IsNullOrEmpty(nextStepId))
            {
                await ExecuteNextStepAsync(workflowInstance.Id, nextStepId);
            }
            else
            {
                // No more steps, complete the workflow
                await CompleteWorkflowAsync(workflowInstance.Id);
            }
        }

        public async Task<WorkflowInstance> GetWorkflowInstanceAsync(string instanceId)
        {
            var instance = await _instanceRepository.GetByIdAsync(instanceId);
            if (instance != null)
            {
                instance.StepHistory = (await _stepRepository.GetByWorkflowInstanceIdAsync(instanceId)).ToList();
            }
            return instance;
        }

        public async Task CancelWorkflowAsync(string instanceId, string reason = null)
        {
            var instance = await _instanceRepository.GetByIdAsync(instanceId);
            if (instance == null)
                throw new InvalidOperationException($"Workflow instance '{instanceId}' not found");

            instance.Status = WorkflowStatus.Cancelled;
            instance.CompletedAt = DateTime.UtcNow;
            await _instanceRepository.UpdateAsync(instance);
        }

        public async Task ResumeWorkflowAsync(string instanceId)
        {
            var instance = await _instanceRepository.GetByIdAsync(instanceId);
            if (instance == null)
                throw new InvalidOperationException($"Workflow instance '{instanceId}' not found");

            if (instance.Status != WorkflowStatus.Suspended)
                throw new InvalidOperationException($"Workflow instance '{instanceId}' is not suspended");

            instance.Status = WorkflowStatus.Running;
            await _instanceRepository.UpdateAsync(instance);

            if (!string.IsNullOrEmpty(instance.CurrentStepId))
            {
                await ExecuteNextStepAsync(instanceId, instance.CurrentStepId);
            }
        }

        public async Task ProcessScheduledStepsAsync()
        {
            var scheduledSteps = await _stepRepository.GetScheduledStepsAsync(DateTime.UtcNow);

            foreach (var stepInstance in scheduledSteps)
            {
                var workflowInstance = await _instanceRepository.GetByIdAsync(stepInstance.WorkflowInstanceId);
                if (workflowInstance?.Status == WorkflowStatus.Running)
                {
                    var definition = await _definitionRepository.GetByIdAsync(workflowInstance.WorkflowDefinitionId);
                    var stepDefinition = definition.Steps.FirstOrDefault(s => s.Id == stepInstance.StepDefinitionId);

                    if (stepDefinition != null)
                    {
                        // Complete the scheduled step and move to next
                        stepInstance.Status = StepStatus.Completed;
                        stepInstance.CompletedAt = DateTime.UtcNow;
                        await _stepRepository.UpdateAsync(stepInstance);

                        var nextStepId = stepDefinition.NextStepId;
                        
                        if (!string.IsNullOrEmpty(nextStepId))
                        {
                            await ExecuteNextStepAsync(workflowInstance.Id, nextStepId);
                        }
                        else
                        {
                            await CompleteWorkflowAsync(workflowInstance.Id);
                        }
                    }
                }
            }
        }

        public async Task<IEnumerable<StepInstance>> GetPendingInteractionStepsAsync(string userId)
        {
            return await _stepRepository.GetByAssignedUserAsync(userId);
        }

        private async Task ExecuteNextStepAsync(string workflowInstanceId, string stepDefinitionId)
        {
            var workflowInstance = await _instanceRepository.GetByIdAsync(workflowInstanceId);
            var definition = await _definitionRepository.GetByIdAsync(workflowInstance.WorkflowDefinitionId);
            var stepDefinition = definition.Steps.FirstOrDefault(s => s.Id == stepDefinitionId);

            if (stepDefinition == null)
            {
                await CompleteWorkflowAsync(workflowInstanceId);
                return;
            }

            // Create step instance
            var stepInstance = new StepInstance
            {
                WorkflowInstanceId = workflowInstanceId,
                StepDefinitionId = stepDefinition.Id,
                StepName = stepDefinition.Name,
                StepType = stepDefinition.Type,
                Status = StepStatus.Pending,
                InputData = new Dictionary<string, object>(workflowInstance.Variables)
            };

            await _stepRepository.CreateAsync(stepInstance);

            // Update workflow current step
            workflowInstance.CurrentStepId = stepDefinitionId;
            await _instanceRepository.UpdateAsync(workflowInstance);

            // Find appropriate executor and execute
            var executor = _stepExecutors.FirstOrDefault(e => e.CanExecute(stepDefinition.Type));
            if (executor == null)
                throw new InvalidOperationException($"No executor found for step type '{stepDefinition.Type}'");

            stepInstance.Status = StepStatus.Running;
            stepInstance.StartedAt = DateTime.UtcNow;
            await _stepRepository.UpdateAsync(stepInstance);

            var result = await executor.ExecuteAsync(stepInstance, stepDefinition, workflowInstance);

            // Update step instance based on result
            stepInstance.Status = result.Status;
            stepInstance.OutputData = result.OutputData;
            stepInstance.ErrorMessage = result.ErrorMessage;

            if (result.Status == StepStatus.Completed || result.Status == StepStatus.Skipped)
            {
                stepInstance.CompletedAt = DateTime.UtcNow;
            }

            // Merge output data into workflow variables
            foreach (var data in result.OutputData)
            {
                workflowInstance.Variables[data.Key] = data.Value;
            }

            await _stepRepository.UpdateAsync(stepInstance);
            await _instanceRepository.UpdateAsync(workflowInstance);

            // Handle result
            if (result.Success)
            {
                // Get next step from result or step definition
                var nextStepId = result.NextStepId ?? stepDefinition.NextStepId;
                
                if (!string.IsNullOrEmpty(nextStepId))
                {
                    await ExecuteNextStepAsync(workflowInstanceId, nextStepId);
                }
                else
                {
                    // No next step, complete the workflow
                    await CompleteWorkflowAsync(workflowInstanceId);
                }
            }
            else if (result.Status == StepStatus.WaitingForInput || result.Status == StepStatus.Scheduled)
            {
                // Workflow is now waiting
                workflowInstance.Status = WorkflowStatus.Waiting;
                await _instanceRepository.UpdateAsync(workflowInstance);
            }
            else if (!result.Success)
            {
                // Workflow failed
                workflowInstance.Status = WorkflowStatus.Failed;
                workflowInstance.CompletedAt = DateTime.UtcNow;
                await _instanceRepository.UpdateAsync(workflowInstance);
            }
        }

        private async Task CompleteWorkflowAsync(string workflowInstanceId)
        {
            var instance = await _instanceRepository.GetByIdAsync(workflowInstanceId);
            instance.Status = WorkflowStatus.Completed;
            instance.CompletedAt = DateTime.UtcNow;
            await _instanceRepository.UpdateAsync(instance);
        }
    }
}
