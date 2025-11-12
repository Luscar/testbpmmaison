using System;
using System.Collections.Generic;

namespace WorkflowEngine.Core.Models
{
    /// <summary>
    /// Represents a running instance of a workflow
    /// </summary>
    public class WorkflowInstance
    {
        public string Id { get; set; }
        public string WorkflowDefinitionId { get; set; }
        public string CurrentStepId { get; set; }
        public WorkflowStatus Status { get; set; }
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string CorrelationId { get; set; }
        public List<StepInstance> StepHistory { get; set; } = new List<StepInstance>();
    }

    /// <summary>
    /// Represents the execution of a single step within a workflow instance
    /// </summary>
    public class StepInstance
    {
        public string Id { get; set; }
        public string WorkflowInstanceId { get; set; }
        public string StepDefinitionId { get; set; }
        public string StepName { get; set; }
        public string StepType { get; set; }
        public StepStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string AssignedTo { get; set; }
        public Dictionary<string, object> InputData { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> OutputData { get; set; } = new Dictionary<string, object>();
        public string ErrorMessage { get; set; }
        public string TransitionTaken { get; set; }
        public int RetryCount { get; set; }
    }

    /// <summary>
    /// Workflow instance status enumeration
    /// </summary>
    public enum WorkflowStatus
    {
        Created = 0,
        Running = 1,
        Waiting = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5,
        Suspended = 6
    }

    /// <summary>
    /// Step instance status enumeration
    /// </summary>
    public enum StepStatus
    {
        Pending = 0,
        Running = 1,
        WaitingForInput = 2,
        Scheduled = 3,
        Completed = 4,
        Failed = 5,
        Skipped = 6,
        Timeout = 7
    }
}
