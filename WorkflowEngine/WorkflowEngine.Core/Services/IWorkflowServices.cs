using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Services
{
    /// <summary>
    /// Main workflow engine interface for managing workflow execution
    /// </summary>
    public interface IWorkflowEngine
    {
        /// <summary>
        /// Start a new workflow instance
        /// </summary>
        Task<string> StartWorkflowAsync(string workflowDefinitionId, Dictionary<string, object> variables = null, string correlationId = null, string createdBy = null);

        /// <summary>
        /// Complete an interaction step with user input
        /// </summary>
        Task CompleteInteractionStepAsync(string stepInstanceId, Dictionary<string, object> outputData, string completedBy = null);

        /// <summary>
        /// Get workflow instance status
        /// </summary>
        Task<WorkflowInstance> GetWorkflowInstanceAsync(string instanceId);

        /// <summary>
        /// Cancel a running workflow
        /// </summary>
        Task CancelWorkflowAsync(string instanceId, string reason = null);

        /// <summary>
        /// Resume a suspended workflow
        /// </summary>
        Task ResumeWorkflowAsync(string instanceId);

        /// <summary>
        /// Process scheduled steps that are due
        /// </summary>
        Task ProcessScheduledStepsAsync();

        /// <summary>
        /// Get pending interaction steps for a user
        /// </summary>
        Task<IEnumerable<StepInstance>> GetPendingInteractionStepsAsync(string userId);
    }

    /// <summary>
    /// Service for executing individual workflow steps
    /// </summary>
    public interface IStepExecutor
    {
        /// <summary>
        /// Execute a step instance
        /// </summary>
        Task<StepExecutionResult> ExecuteAsync(StepInstance stepInstance, StepDefinition stepDefinition, WorkflowInstance workflowInstance);

        /// <summary>
        /// Check if this executor can handle the given step type
        /// </summary>
        bool CanExecute(string stepType);
    }

    /// <summary>
    /// Result of step execution
    /// </summary>
    public class StepExecutionResult
    {
        public bool Success { get; set; }
        public string NextStepId { get; set; }  // Direct next step ID
        public Dictionary<string, object> OutputData { get; set; } = new Dictionary<string, object>();
        public string ErrorMessage { get; set; }
        public StepStatus Status { get; set; }
    }

    /// <summary>
    /// Service registry for business activities
    /// </summary>
    public interface IActivityServiceRegistry
    {
        /// <summary>
        /// Register a service that can be called from activity steps
        /// </summary>
        void RegisterService(string serviceName, object serviceInstance);

        /// <summary>
        /// Get a registered service
        /// </summary>
        object GetService(string serviceName);

        /// <summary>
        /// Invoke a method on a registered service
        /// </summary>
        Task<object> InvokeServiceMethodAsync(string serviceName, string methodName, Dictionary<string, object> parameters);
    }

    /// <summary>
    /// Service for evaluating expressions in transitions and configurations
    /// </summary>
    public interface IExpressionEvaluator
    {
        /// <summary>
        /// Evaluate an expression against workflow variables
        /// </summary>
        bool EvaluateCondition(string expression, Dictionary<string, object> variables);

        /// <summary>
        /// Evaluate an expression and return the result
        /// </summary>
        object EvaluateExpression(string expression, Dictionary<string, object> variables);
    }
}
