using System.Collections.Generic;
using System.Threading.Tasks;

namespace WorkflowEngine.Core.Integration
{
    /// <summary>
    /// Interface for integrating with external task management systems
    /// Implement this interface to connect to your existing task system
    /// </summary>
    public interface IExternalTaskSystem
    {
        /// <summary>
        /// Create a task in the external system when an InteractionStep is reached
        /// </summary>
        /// <param name="taskInfo">Information about the workflow task to create</param>
        /// <returns>External task ID from the external system</returns>
        Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo);

        /// <summary>
        /// Close/complete a task in the external system
        /// Called when the workflow interaction step is completed
        /// </summary>
        /// <param name="externalTaskId">The task ID from the external system</param>
        /// <param name="completionData">Data from the task completion</param>
        Task CloseTaskAsync(string externalTaskId, Dictionary<string, object> completionData);

        /// <summary>
        /// Optional: Update task information in the external system
        /// </summary>
        /// <param name="externalTaskId">The task ID from the external system</param>
        /// <param name="updates">Fields to update</param>
        Task UpdateTaskAsync(string externalTaskId, Dictionary<string, object> updates);

        /// <summary>
        /// Optional: Cancel a task in the external system
        /// Called when the workflow is cancelled or the step times out
        /// </summary>
        /// <param name="externalTaskId">The task ID from the external system</param>
        /// <param name="reason">Reason for cancellation</param>
        Task CancelTaskAsync(string externalTaskId, string reason);
    }

    /// <summary>
    /// Information passed to the external task system when creating a task
    /// </summary>
    public class ExternalTaskInfo
    {
        /// <summary>
        /// Workflow instance ID (for correlation)
        /// </summary>
        public string WorkflowInstanceId { get; set; }

        /// <summary>
        /// Step instance ID (internal workflow tracking)
        /// </summary>
        public string StepInstanceId { get; set; }

        /// <summary>
        /// Task title/subject
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Task description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Priority level (if supported by external system)
        /// </summary>
        public string Priority { get; set; }

        /// <summary>
        /// Due date/time (if applicable)
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Users assigned to this task (from InteractionStep configuration)
        /// Note: Assignment logic is handled by the external system
        /// This is just for passing the configured users
        /// </summary>
        public List<string> AssignedUsers { get; set; }

        /// <summary>
        /// Roles assigned to this task (from InteractionStep configuration)
        /// Note: Assignment logic is handled by the external system
        /// </summary>
        public List<string> AssignedRoles { get; set; }

        /// <summary>
        /// Form schema or UI configuration (if needed by external system)
        /// </summary>
        public string FormSchema { get; set; }

        /// <summary>
        /// Current workflow variables (context for the task)
        /// </summary>
        public Dictionary<string, object> WorkflowContext { get; set; }

        /// <summary>
        /// Additional metadata for the external system
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Result from the external task system after closing a task
    /// </summary>
    public class ExternalTaskCloseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}
