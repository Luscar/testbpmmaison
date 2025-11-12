using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Repositories
{
    /// <summary>
    /// Repository for workflow definitions
    /// </summary>
    public interface IWorkflowDefinitionRepository
    {
        Task<WorkflowDefinition> GetByIdAsync(string id);
        Task<WorkflowDefinition> GetByNameAndVersionAsync(string name, string version);
        Task<IEnumerable<WorkflowDefinition>> GetAllAsync();
        Task<string> CreateAsync(WorkflowDefinition definition);
        Task UpdateAsync(WorkflowDefinition definition);
        Task DeleteAsync(string id);
    }

    /// <summary>
    /// Repository for workflow instances
    /// </summary>
    public interface IWorkflowInstanceRepository
    {
        Task<WorkflowInstance> GetByIdAsync(string id);
        Task<IEnumerable<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status);
        Task<IEnumerable<WorkflowInstance>> GetByDefinitionIdAsync(string definitionId);
        Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId);
        Task<string> CreateAsync(WorkflowInstance instance);
        Task UpdateAsync(WorkflowInstance instance);
        Task DeleteAsync(string id);
    }

    /// <summary>
    /// Repository for step instances
    /// </summary>
    public interface IStepInstanceRepository
    {
        Task<StepInstance> GetByIdAsync(string id);
        Task<IEnumerable<StepInstance>> GetByWorkflowInstanceIdAsync(string workflowInstanceId);
        Task<IEnumerable<StepInstance>> GetPendingStepsAsync();
        Task<IEnumerable<StepInstance>> GetScheduledStepsAsync(DateTime beforeDate);
        Task<IEnumerable<StepInstance>> GetByAssignedUserAsync(string userId);
        Task<string> CreateAsync(StepInstance stepInstance);
        Task UpdateAsync(StepInstance stepInstance);
        Task DeleteAsync(string id);
    }

    /// <summary>
    /// Configuration for customizable table names
    /// </summary>
    public class RepositoryConfiguration
    {
        public string WorkflowDefinitionTable { get; set; } = "WF_DEFINITIONS";
        public string WorkflowInstanceTable { get; set; } = "WF_INSTANCES";
        public string StepInstanceTable { get; set; } = "WF_STEP_INSTANCES";
        public string StepHistoryTable { get; set; } = "WF_STEP_HISTORY";
        public string Schema { get; set; } = "dbo";
        public string ConnectionString { get; set; }
    }
}
