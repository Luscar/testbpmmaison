namespace WorkflowEngine.Core.Models
{
    /// <summary>
    /// Enumeration of available workflow step types
    /// </summary>
    public static class StepTypes
    {
        public const string InteractionStep = "interaction";      // Waits for external user input
        public const string ScheduledStep = "scheduled";          // Waits until specific time
        public const string BusinessStep = "business";            // Executes business logic
        public const string DecisionStep = "decision";            // Routes workflow based on conditions
        public const string SubWorkflowStep = "subworkflow";      // Executes another workflow as a nested step
    }

    /// <summary>
    /// Configuration for interaction steps that require external input
    /// </summary>
    public class InteractionStepConfig
    {
        public string FormSchema { get; set; }              // JSON schema for input form (optional - your system may handle this)
        public List<string> AssignedUsers { get; set; }      // OPTIONAL: Users - only if you want to pass suggestions to external system
        public List<string> AssignedRoles { get; set; }      // OPTIONAL: Roles - only if you want to pass suggestions to external system
        public int? TimeoutMinutes { get; set; }            // OPTIONAL: Timeout for due date calculation
        public Dictionary<string, object> CustomData { get; set; }  // OPTIONAL: Any custom data your task system needs
    }

    /// <summary>
    /// Configuration for scheduled steps that wait until a specific time
    /// </summary>
    public class ScheduledStepConfig
    {
        public string ScheduleExpression { get; set; }       // Cron expression or date expression
        public string DateVariable { get; set; }            // Variable containing target datetime
        public string TargetDateTime { get; set; }          // ISO 8601 datetime string
        public bool SkipIfPast { get; set; }                // Skip if target date has passed
    }

    /// <summary>
    /// Configuration for business steps that execute business logic
    /// </summary>
    public class BusinessStepConfig
    {
        public string ServiceName { get; set; }             // Name of the service to invoke
        public string MethodName { get; set; }              // Method to call on the service
        public Dictionary<string, object> InputMapping { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> OutputMapping { get; set; } = new Dictionary<string, string>();
        public int? RetryCount { get; set; }
        public int? RetryDelaySeconds { get; set; }
        public string ErrorNextStepId { get; set; }         // Step to route to on error
    }

    /// <summary>
    /// Configuration for decision steps that route the workflow
    /// </summary>
    public class DecisionStepConfig
    {
        /// <summary>
        /// Type of decision: "conditions" (evaluate expressions) or "service" (call a service to decide)
        /// </summary>
        public string DecisionType { get; set; } = "conditions";  // "conditions" or "service"
        
        /// <summary>
        /// Default step to route to if no conditions match
        /// </summary>
        public string DefaultNextStepId { get; set; }
        
        /// <summary>
        /// Routes with conditions (used when DecisionType = "conditions")
        /// </summary>
        public List<DecisionRoute> Routes { get; set; } = new List<DecisionRoute>();
        
        /// <summary>
        /// Service-based decision configuration (used when DecisionType = "service")
        /// </summary>
        public string ServiceName { get; set; }
        public string MethodName { get; set; }
        public Dictionary<string, object> InputMapping { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// A routing decision with condition and target
    /// </summary>
    public class DecisionRoute
    {
        public string Name { get; set; }                    // Descriptive name for this route
        public string NextStepId { get; set; }              // Target step if condition is true
        public string Condition { get; set; }               // Expression to evaluate (optional for service-based)
    }

    /// <summary>
    /// Configuration for sub-workflow steps that execute another workflow
    /// </summary>
    public class SubWorkflowStepConfig
    {
        public string WorkflowDefinitionId { get; set; }    // ID of workflow to execute
        public Dictionary<string, string> InputMapping { get; set; } = new Dictionary<string, string>();   // Map parent vars to child
        public Dictionary<string, string> OutputMapping { get; set; } = new Dictionary<string, string>();  // Map child vars back to parent
        public bool WaitForCompletion { get; set; } = true; // Wait for sub-workflow to complete
        public string ErrorNextStepId { get; set; }         // Step to route to if sub-workflow fails
    }
}
