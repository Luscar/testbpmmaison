using System;
using System.Collections.Generic;

namespace WorkflowEngine.Core.Models
{
    /// <summary>
    /// Represents a complete workflow process definition
    /// </summary>
    public class WorkflowDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public List<StepDefinition> Steps { get; set; } = new List<StepDefinition>();
        public string InitialStepId { get; set; }
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Base class for all step definitions
    /// </summary>
    public class StepDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// DEPRECATED: Use DecisionRoute in DecisionStepConfig instead.
        /// Kept for backward compatibility with existing workflows.
        /// </summary>
        [Obsolete("Use DecisionRoute in DecisionStepConfig for decision steps")]
        public List<Transition> Transitions { get; set; } = new List<Transition>();
        
        /// <summary>
        /// Next step to execute (recommended for all non-decision steps)
        /// </summary>
        public string NextStepId { get; set; }
    }

    /// <summary>
    /// Defines a transition from one step to another
    /// </summary>
    public class Transition
    {
        public string Id { get; set; }
        public string TargetStepId { get; set; }
        public string Condition { get; set; } // Expression to evaluate
        public string Label { get; set; }
    }
}
