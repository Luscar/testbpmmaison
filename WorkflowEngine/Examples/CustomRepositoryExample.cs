using WorkflowEngine.Core.Repositories;
using WorkflowEngine.Core.Repositories.Oracle;

namespace YourCompany.CustomWorkflow
{
    /// <summary>
    /// Example of how to create a custom repository with different table names
    /// This allows clients to integrate the workflow engine into their existing database schema
    /// </summary>
    public class CustomWorkflowDefinitionRepository : OracleWorkflowDefinitionRepository
    {
        public CustomWorkflowDefinitionRepository(RepositoryConfiguration config) : base(config)
        {
        }

        // Override to use custom table name
        protected override string TableName => "CUSTOM_SCHEMA.MY_WORKFLOW_DEFINITIONS";
    }

    public class CustomWorkflowInstanceRepository : OracleWorkflowInstanceRepository
    {
        public CustomWorkflowInstanceRepository(RepositoryConfiguration config) : base(config)
        {
        }

        protected override string TableName => "CUSTOM_SCHEMA.MY_WORKFLOW_INSTANCES";
    }

    public class CustomStepInstanceRepository : OracleStepInstanceRepository
    {
        public CustomStepInstanceRepository(RepositoryConfiguration config) : base(config)
        {
        }

        protected override string TableName => "CUSTOM_SCHEMA.MY_STEP_INSTANCES";
    }

    /// <summary>
    /// Example of registering custom repositories with dependency injection
    /// </summary>
    public static class CustomRepositoryRegistration
    {
        public static void RegisterCustomRepositories(IServiceCollection services, string connectionString)
        {
            // Configure with custom table names
            var repoConfig = new RepositoryConfiguration
            {
                ConnectionString = connectionString,
                Schema = "CUSTOM_SCHEMA",
                WorkflowDefinitionTable = "MY_WORKFLOW_DEFINITIONS",
                WorkflowInstanceTable = "MY_WORKFLOW_INSTANCES",
                StepInstanceTable = "MY_STEP_INSTANCES"
            };

            services.AddSingleton(repoConfig);

            // Register custom repositories
            services.AddScoped<IWorkflowDefinitionRepository, CustomWorkflowDefinitionRepository>();
            services.AddScoped<IWorkflowInstanceRepository, CustomWorkflowInstanceRepository>();
            services.AddScoped<IStepInstanceRepository, CustomStepInstanceRepository>();
        }
    }

    /// <summary>
    /// Example of a repository that adds custom behavior
    /// </summary>
    public class AuditedWorkflowInstanceRepository : OracleWorkflowInstanceRepository
    {
        private readonly IAuditService _auditService;

        public AuditedWorkflowInstanceRepository(
            RepositoryConfiguration config,
            IAuditService auditService) : base(config)
        {
            _auditService = auditService;
        }

        public override async Task<string> CreateAsync(WorkflowInstance instance)
        {
            // Call base implementation
            var result = await base.CreateAsync(instance);

            // Add custom auditing
            await _auditService.LogWorkflowCreatedAsync(instance.Id, instance.CreatedBy);

            return result;
        }

        public override async Task UpdateAsync(WorkflowInstance instance)
        {
            // Load previous state for audit comparison
            var previousState = await base.GetByIdAsync(instance.Id);

            // Call base implementation
            await base.UpdateAsync(instance);

            // Add custom auditing
            if (previousState.Status != instance.Status)
            {
                await _auditService.LogWorkflowStatusChangedAsync(
                    instance.Id,
                    previousState.Status,
                    instance.Status);
            }
        }
    }

    // Mock audit service interface
    public interface IAuditService
    {
        Task LogWorkflowCreatedAsync(string workflowId, string createdBy);
        Task LogWorkflowStatusChangedAsync(string workflowId, WorkflowStatus from, WorkflowStatus to);
    }
}
