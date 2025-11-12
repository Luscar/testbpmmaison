using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Services.Executors
{
    /// <summary>
    /// Executor for interaction steps (user input required)
    /// </summary>
    public class InteractionStepExecutor : IStepExecutor
    {
        public bool CanExecute(string stepType) => stepType == StepTypes.InteractionStep;

        public async Task<StepExecutionResult> ExecuteAsync(StepInstance stepInstance, StepDefinition stepDefinition, WorkflowInstance workflowInstance)
        {
            var config = JsonSerializer.Deserialize<InteractionStepConfig>(
                JsonSerializer.Serialize(stepDefinition.Configuration));

            // Set the step to waiting for input
            return await Task.FromResult(new StepExecutionResult
            {
                Success = true,
                Status = StepStatus.WaitingForInput,
                OutputData = new Dictionary<string, object>()
            });
        }
    }

    /// <summary>
    /// Executor for scheduled steps (wait until a specific time)
    /// </summary>
    public class ScheduledStepExecutor : IStepExecutor
    {
        private readonly IExpressionEvaluator _expressionEvaluator;

        public ScheduledStepExecutor(IExpressionEvaluator expressionEvaluator)
        {
            _expressionEvaluator = expressionEvaluator;
        }

        public bool CanExecute(string stepType) => stepType == StepTypes.ScheduledStep;

        public async Task<StepExecutionResult> ExecuteAsync(StepInstance stepInstance, StepDefinition stepDefinition, WorkflowInstance workflowInstance)
        {
            var config = JsonSerializer.Deserialize<ScheduledStepConfig>(
                JsonSerializer.Serialize(stepDefinition.Configuration));

            DateTime targetDateTime;

            // Determine target datetime from configuration
            if (!string.IsNullOrEmpty(config.TargetDateTime))
            {
                targetDateTime = DateTime.Parse(config.TargetDateTime);
            }
            else if (!string.IsNullOrEmpty(config.DateVariable) && workflowInstance.Variables.ContainsKey(config.DateVariable))
            {
                targetDateTime = Convert.ToDateTime(workflowInstance.Variables[config.DateVariable]);
            }
            else if (!string.IsNullOrEmpty(config.ScheduleExpression))
            {
                // Evaluate the schedule expression
                var result = _expressionEvaluator.EvaluateExpression(config.ScheduleExpression, workflowInstance.Variables);
                targetDateTime = Convert.ToDateTime(result);
            }
            else
            {
                return new StepExecutionResult
                {
                    Success = false,
                    Status = StepStatus.Failed,
                    ErrorMessage = "No valid schedule configuration found"
                };
            }

            // Check if target date has passed
            if (targetDateTime <= DateTime.UtcNow)
            {
                if (config.SkipIfPast)
                {
                    return new StepExecutionResult
                    {
                        Success = true,
                        Status = StepStatus.Skipped,
                        NextStepId = stepDefinition.NextStepId
                    };
                }
                else
                {
                    // Execute immediately
                    return new StepExecutionResult
                    {
                        Success = true,
                        Status = StepStatus.Completed,
                        NextStepId = stepDefinition.NextStepId
                    };
                }
            }

            // Schedule for future execution
            stepInstance.StartedAt = targetDateTime;
            return await Task.FromResult(new StepExecutionResult
            {
                Success = true,
                Status = StepStatus.Scheduled
            });
        }
    }

    /// <summary>
    /// Executor for business steps (business logic execution)
    /// </summary>
    public class BusinessStepExecutor : IStepExecutor
    {
        private readonly IActivityServiceRegistry _serviceRegistry;
        private readonly IExpressionEvaluator _expressionEvaluator;

        public BusinessStepExecutor(IActivityServiceRegistry serviceRegistry, IExpressionEvaluator expressionEvaluator)
        {
            _serviceRegistry = serviceRegistry;
            _expressionEvaluator = expressionEvaluator;
        }

        public bool CanExecute(string stepType) => stepType == StepTypes.BusinessStep;

        public async Task<StepExecutionResult> ExecuteAsync(StepInstance stepInstance, StepDefinition stepDefinition, WorkflowInstance workflowInstance)
        {
            var config = JsonSerializer.Deserialize<BusinessStepConfig>(
                JsonSerializer.Serialize(stepDefinition.Configuration));

            try
            {
                // Prepare input parameters from mapping
                var parameters = new Dictionary<string, object>();
                foreach (var mapping in config.InputMapping)
                {
                    if (mapping.Value is string strValue && strValue.StartsWith("$"))
                    {
                        // Variable reference
                        var variableName = strValue.Substring(1);
                        if (workflowInstance.Variables.ContainsKey(variableName))
                        {
                            parameters[mapping.Key] = workflowInstance.Variables[variableName];
                        }
                    }
                    else
                    {
                        parameters[mapping.Key] = mapping.Value;
                    }
                }

                // Invoke the service method
                var result = await _serviceRegistry.InvokeServiceMethodAsync(
                    config.ServiceName,
                    config.MethodName,
                    parameters);

                // Map output to workflow variables
                var outputData = new Dictionary<string, object>();
                if (result != null && config.OutputMapping != null)
                {
                    foreach (var mapping in config.OutputMapping)
                    {
                        var resultDict = result as Dictionary<string, object>;
                        if (resultDict != null && resultDict.ContainsKey(mapping.Key))
                        {
                            outputData[mapping.Value] = resultDict[mapping.Key];
                            workflowInstance.Variables[mapping.Value] = resultDict[mapping.Key];
                        }
                    }
                }

                return new StepExecutionResult
                {
                    Success = true,
                    Status = StepStatus.Completed,
                    OutputData = outputData,
                    NextStepId = stepDefinition.NextStepId
                };
            }
            catch (Exception ex)
            {
                // Handle retry logic
                if (config.RetryCount.HasValue && stepInstance.RetryCount < config.RetryCount.Value)
                {
                    stepInstance.RetryCount++;
                    return new StepExecutionResult
                    {
                        Success = false,
                        Status = StepStatus.Pending,
                        ErrorMessage = ex.Message
                    };
                }

                // If error next step is configured, route there
                if (!string.IsNullOrEmpty(config.ErrorNextStepId))
                {
                    return new StepExecutionResult
                    {
                        Success = true, // Route to error handler
                        Status = StepStatus.Completed,
                        ErrorMessage = ex.Message,
                        NextStepId = config.ErrorNextStepId
                    };
                }

                return new StepExecutionResult
                {
                    Success = false,
                    Status = StepStatus.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    /// <summary>
    /// Executor for decision steps (routing/decision logic)
    /// </summary>
    public class DecisionStepExecutor : IStepExecutor
    {
        private readonly IExpressionEvaluator _expressionEvaluator;
        private readonly IActivityServiceRegistry _serviceRegistry;

        public DecisionStepExecutor(IExpressionEvaluator expressionEvaluator, IActivityServiceRegistry serviceRegistry)
        {
            _expressionEvaluator = expressionEvaluator;
            _serviceRegistry = serviceRegistry;
        }

        public bool CanExecute(string stepType) => stepType == StepTypes.DecisionStep;

        public async Task<StepExecutionResult> ExecuteAsync(StepInstance stepInstance, StepDefinition stepDefinition, WorkflowInstance workflowInstance)
        {
            var config = JsonSerializer.Deserialize<DecisionStepConfig>(
                JsonSerializer.Serialize(stepDefinition.Configuration));

            string selectedNextStepId = null;

            // Two decision modes: conditions or service
            if (config.DecisionType == "service")
            {
                selectedNextStepId = await ExecuteServiceBasedDecisionAsync(config, workflowInstance);
            }
            else // "conditions" or default
            {
                selectedNextStepId = ExecuteConditionBasedDecision(config, workflowInstance);
            }

            // Use default if no route was selected
            if (selectedNextStepId == null && !string.IsNullOrEmpty(config.DefaultNextStepId))
            {
                selectedNextStepId = config.DefaultNextStepId;
            }

            if (selectedNextStepId == null)
            {
                return new StepExecutionResult
                {
                    Success = false,
                    Status = StepStatus.Failed,
                    ErrorMessage = "No matching route found and no default route specified"
                };
            }

            return await Task.FromResult(new StepExecutionResult
            {
                Success = true,
                Status = StepStatus.Completed,
                NextStepId = selectedNextStepId
            });
        }

        private string ExecuteConditionBasedDecision(DecisionStepConfig config, WorkflowInstance workflowInstance)
        {
            // Evaluate routes in order until one matches
            foreach (var route in config.Routes ?? new List<DecisionRoute>())
            {
                if (string.IsNullOrEmpty(route.Condition) ||
                    _expressionEvaluator.EvaluateCondition(route.Condition, workflowInstance.Variables))
                {
                    return route.NextStepId;
                }
            }

            return null;
        }

        private async Task<string> ExecuteServiceBasedDecisionAsync(DecisionStepConfig config, WorkflowInstance workflowInstance)
        {
            try
            {
                // Prepare input parameters from mapping
                var parameters = new Dictionary<string, object>();
                if (config.InputMapping != null)
                {
                    foreach (var mapping in config.InputMapping)
                    {
                        if (mapping.Value is string strValue && strValue.StartsWith("$"))
                        {
                            // Variable reference
                            var variableName = strValue.Substring(1);
                            if (workflowInstance.Variables.ContainsKey(variableName))
                            {
                                parameters[mapping.Key] = workflowInstance.Variables[variableName];
                            }
                        }
                        else
                        {
                            parameters[mapping.Key] = mapping.Value;
                        }
                    }
                }

                // Call the decision service
                var result = await _serviceRegistry.InvokeServiceMethodAsync(
                    config.ServiceName,
                    config.MethodName,
                    parameters);

                // Service should return the next step ID or a route name
                if (result is string nextStepId)
                {
                    return nextStepId;
                }
                else if (result is Dictionary<string, object> resultDict)
                {
                    // Look for "nextStepId" or "routeName" in the result
                    if (resultDict.ContainsKey("nextStepId"))
                    {
                        return resultDict["nextStepId"]?.ToString();
                    }
                    else if (resultDict.ContainsKey("routeName"))
                    {
                        // Find route by name
                        var routeName = resultDict["routeName"]?.ToString();
                        var route = config.Routes?.FirstOrDefault(r => 
                            r.Name.Equals(routeName, StringComparison.OrdinalIgnoreCase));
                        return route?.NextStepId;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Service-based decision failed: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Executor for sub-workflow steps (executes another workflow)
    /// </summary>
    public class SubWorkflowStepExecutor : IStepExecutor
    {
        private readonly IWorkflowEngine _workflowEngine;
        private readonly IWorkflowInstanceRepository _instanceRepository;

        public SubWorkflowStepExecutor(IWorkflowEngine workflowEngine, IWorkflowInstanceRepository instanceRepository)
        {
            _workflowEngine = workflowEngine;
            _instanceRepository = instanceRepository;
        }

        public bool CanExecute(string stepType) => stepType == StepTypes.SubWorkflowStep;

        public async Task<StepExecutionResult> ExecuteAsync(StepInstance stepInstance, StepDefinition stepDefinition, WorkflowInstance workflowInstance)
        {
            var config = JsonSerializer.Deserialize<SubWorkflowStepConfig>(
                JsonSerializer.Serialize(stepDefinition.Configuration));

            try
            {
                // Prepare variables for sub-workflow
                var subWorkflowVariables = new Dictionary<string, object>();
                
                if (config.InputMapping != null)
                {
                    foreach (var mapping in config.InputMapping)
                    {
                        // mapping.Key = variable name in sub-workflow
                        // mapping.Value = variable name in parent workflow (with $ prefix)
                        var parentVarName = mapping.Value.TrimStart('$');
                        if (workflowInstance.Variables.ContainsKey(parentVarName))
                        {
                            subWorkflowVariables[mapping.Key] = workflowInstance.Variables[parentVarName];
                        }
                    }
                }

                // Start the sub-workflow
                var subWorkflowInstanceId = await _workflowEngine.StartWorkflowAsync(
                    config.WorkflowDefinitionId,
                    subWorkflowVariables,
                    correlationId: $"{workflowInstance.Id}:sub:{stepInstance.Id}",
                    createdBy: workflowInstance.CreatedBy
                );

                if (config.WaitForCompletion)
                {
                    // Poll for completion (in a real implementation, use events/callbacks)
                    var subInstance = await _instanceRepository.GetByIdAsync(subWorkflowInstanceId);
                    var maxAttempts = 1000;
                    var attempt = 0;
                    
                    while (attempt < maxAttempts && 
                           subInstance.Status != WorkflowStatus.Completed && 
                           subInstance.Status != WorkflowStatus.Failed &&
                           subInstance.Status != WorkflowStatus.Cancelled)
                    {
                        await Task.Delay(100);
                        subInstance = await _instanceRepository.GetByIdAsync(subWorkflowInstanceId);
                        attempt++;
                    }

                    if (subInstance.Status == WorkflowStatus.Failed || subInstance.Status == WorkflowStatus.Cancelled)
                    {
                        // Sub-workflow failed - route to error handler if configured
                        if (!string.IsNullOrEmpty(config.ErrorNextStepId))
                        {
                            return new StepExecutionResult
                            {
                                Success = true,
                                Status = StepStatus.Completed,
                                ErrorMessage = $"Sub-workflow {subWorkflowInstanceId} failed",
                                NextStepId = config.ErrorNextStepId
                            };
                        }

                        return new StepExecutionResult
                        {
                            Success = false,
                            Status = StepStatus.Failed,
                            ErrorMessage = $"Sub-workflow {subWorkflowInstanceId} did not complete successfully"
                        };
                    }

                    // Map output variables back to parent workflow
                    var outputData = new Dictionary<string, object>();
                    if (config.OutputMapping != null)
                    {
                        foreach (var mapping in config.OutputMapping)
                        {
                            // mapping.Key = variable name in sub-workflow
                            // mapping.Value = variable name in parent workflow
                            if (subInstance.Variables.ContainsKey(mapping.Key))
                            {
                                outputData[mapping.Value] = subInstance.Variables[mapping.Key];
                                workflowInstance.Variables[mapping.Value] = subInstance.Variables[mapping.Key];
                            }
                        }
                    }

                    return new StepExecutionResult
                    {
                        Success = true,
                        Status = StepStatus.Completed,
                        OutputData = outputData,
                        NextStepId = stepDefinition.NextStepId
                    };
                }
                else
                {
                    // Fire and forget - don't wait for completion
                    return new StepExecutionResult
                    {
                        Success = true,
                        Status = StepStatus.Completed,
                        OutputData = new Dictionary<string, object>
                        {
                            ["subWorkflowInstanceId"] = subWorkflowInstanceId
                        },
                        NextStepId = stepDefinition.NextStepId
                    };
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(config.ErrorNextStepId))
                {
                    return new StepExecutionResult
                    {
                        Success = true,
                        Status = StepStatus.Completed,
                        ErrorMessage = ex.Message,
                        NextStepId = config.ErrorNextStepId
                    };
                }

                return new StepExecutionResult
                {
                    Success = false,
                    Status = StepStatus.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
