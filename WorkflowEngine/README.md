# WorkflowEngine.Core

A flexible and extensible Business Process Management (BPM) engine for .NET with Oracle database support.

## Features

- ✅ **Multiple Step Types**:
  - **Interaction Steps** - Wait for user input before continuing
  - **Scheduled Steps** - Wait until a specific date/time
  - **Activity Steps** - Execute business logic via registered services
  - **Gateway Steps** - Conditional routing and decision logic

- ✅ **Oracle Database Support** with customizable table names
- ✅ **JSON-based Workflow Definitions**
- ✅ **Expression Evaluation** for conditions and routing
- ✅ **Retry Logic** for activity steps
- ✅ **Workflow Correlation** for tracking related processes
- ✅ **Extensible Architecture** - Easy to add custom step types

## Installation

```bash
dotnet add package WorkflowEngine.Core
```

## Quick Start

### 1. Configure the Database

Run the Oracle schema script to create the necessary tables:

```sql
-- Use the provided oracle_schema.sql file
-- Customize table names if needed
```

### 2. Setup Dependency Injection

```csharp
using WorkflowEngine.Core.Repositories;
using WorkflowEngine.Core.Repositories.Oracle;
using WorkflowEngine.Core.Services;
using WorkflowEngine.Core.Services.Executors;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Configure repository with custom table names (optional)
var repoConfig = new RepositoryConfiguration
{
    ConnectionString = "your-oracle-connection-string",
    Schema = "YOUR_SCHEMA",
    WorkflowDefinitionTable = "WF_DEFINITIONS",
    WorkflowInstanceTable = "WF_INSTANCES",
    StepInstanceTable = "WF_STEP_INSTANCES"
};

// Register repositories
services.AddSingleton(repoConfig);
services.AddScoped<IWorkflowDefinitionRepository, OracleWorkflowDefinitionRepository>();
services.AddScoped<IWorkflowInstanceRepository, OracleWorkflowInstanceRepository>();
services.AddScoped<IStepInstanceRepository, OracleStepInstanceRepository>();

// Register services
services.AddSingleton<IActivityServiceRegistry, ActivityServiceRegistry>();
services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

// Register step executors
services.AddScoped<IStepExecutor, InteractionStepExecutor>();
services.AddScoped<IStepExecutor, ScheduledStepExecutor>();
services.AddScoped<IStepExecutor, ActivityStepExecutor>();
services.AddScoped<IStepExecutor, GatewayStepExecutor>();

// Register workflow engine
services.AddScoped<IWorkflowEngine, WorkflowEngine.Core.Services.WorkflowEngine>();

// Register workflow definition loader
services.AddScoped<WorkflowDefinitionLoader>();

var serviceProvider = services.BuildServiceProvider();
```

### 3. Load and Deploy a Workflow Definition

```csharp
// Load from JSON file
var loader = serviceProvider.GetRequiredService<WorkflowDefinitionLoader>();
var definition = await loader.LoadFromFileAsync("employee-onboarding.json");

// Validate the definition
loader.Validate(definition);

// Save to database
var definitionRepo = serviceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
await definitionRepo.CreateAsync(definition);
```

### 4. Register Business Services

```csharp
// Register your business services for Activity Steps
var serviceRegistry = serviceProvider.GetRequiredService<IActivityServiceRegistry>();

// Example: Email service
serviceRegistry.RegisterService("EmailService", new EmailService());

// Example: Equipment service
serviceRegistry.RegisterService("EquipmentService", new EquipmentService());

// Example: IT service
serviceRegistry.RegisterService("ITService", new ITService());
```

### 5. Start a Workflow

```csharp
var engine = serviceProvider.GetRequiredService<IWorkflowEngine>();

var variables = new Dictionary<string, object>
{
    ["employeeName"] = "John Doe",
    ["employeeEmail"] = "john.doe@company.com",
    ["department"] = "Engineering",
    ["startDate"] = DateTime.UtcNow.AddDays(7)
};

var instanceId = await engine.StartWorkflowAsync(
    workflowDefinitionId: "employee-onboarding-v1",
    variables: variables,
    correlationId: "EMP-2024-001",
    createdBy: "hr.admin@company.com"
);

Console.WriteLine($"Workflow started with ID: {instanceId}");
```

### 6. Complete Interaction Steps

```csharp
// Get pending steps for a user
var pendingSteps = await engine.GetPendingInteractionStepsAsync("hr.admin@company.com");

foreach (var step in pendingSteps)
{
    Console.WriteLine($"Step: {step.StepName} - {step.StepDefinitionId}");
}

// Complete an interaction step with user input
var outputData = new Dictionary<string, object>
{
    ["approved"] = true,
    ["comments"] = "Approved for onboarding"
};

await engine.CompleteInteractionStepAsync(
    stepInstanceId: pendingSteps.First().Id,
    outputData: outputData,
    completedBy: "manager@company.com"
);
```

### 7. Process Scheduled Steps

```csharp
// Run this periodically (e.g., in a background job)
await engine.ProcessScheduledStepsAsync();
```

### 8. Monitor Workflow Progress

```csharp
var instance = await engine.GetWorkflowInstanceAsync(instanceId);

Console.WriteLine($"Status: {instance.Status}");
Console.WriteLine($"Current Step: {instance.CurrentStepId}");
Console.WriteLine($"Step History:");

foreach (var step in instance.StepHistory)
{
    Console.WriteLine($"  - {step.StepName}: {step.Status} (Started: {step.StartedAt})");
}
```

## JSON Workflow Definition Structure

```json
{
  "id": "unique-workflow-id",
  "name": "Workflow Name",
  "description": "Workflow description",
  "version": "1.0.0",
  "initialStepId": "first-step-id",
  "variables": {
    "variableName": "defaultValue"
  },
  "steps": [
    {
      "id": "step-id",
      "name": "Step Name",
      "type": "interaction|scheduled|activity|gateway",
      "configuration": {
        // Step-specific configuration
      },
      "nextStepId": "next-step-id",  // Simple syntax for single-path flows
      "transitions": [                // Full syntax for conditional routing
        {
          "id": "transition-id",
          "targetStepId": "next-step-id",
          "condition": "variableName == 'value'",
          "label": "Transition Label"
        }
      ]
    }
  ]
}
```

### Simplified Syntax with `nextStepId`

For linear workflows where most steps flow sequentially, use `nextStepId` instead of `transitions`:

```json
{
  "id": "send-email",
  "type": "activity",
  "configuration": { ... },
  "nextStepId": "next-step"  // Much simpler!
}
```

**Use `nextStepId` for:** Interaction, Scheduled, and Activity steps with single path  
**Use `transitions` for:** Gateway steps (required for conditions) and multi-path routing

See [NEXTSTEPID_GUIDE.md](NEXTSTEPID_GUIDE.md) for detailed examples and migration guide.

## Step Type Configurations

### Interaction Step (User Input)

```json
{
  "type": "interaction",
  "configuration": {
    "formSchema": "{json-schema}",
    "assignedUsers": ["user1@email.com"],
    "assignedRoles": ["Manager", "HR"],
    "timeoutMinutes": 1440,
    "timeoutTransitionId": "timeout-transition",
    "allowReassignment": true
  }
}
```

### Scheduled Step (Wait Until Date)

```json
{
  "type": "scheduled",
  "configuration": {
    "targetDateTime": "2024-12-31T23:59:59Z",
    "dateVariable": "startDate",
    "scheduleExpression": "DateTime.UtcNow.AddDays(7)",
    "skipIfPast": false
  }
}
```

### Activity Step (Business Logic)

```json
{
  "type": "activity",
  "configuration": {
    "serviceName": "EmailService",
    "methodName": "SendEmail",
    "inputMapping": {
      "to": "$employeeEmail",
      "subject": "Welcome!",
      "body": "Welcome to the team"
    },
    "outputMapping": {
      "messageId": "emailMessageId"
    },
    "retryCount": 3,
    "retryDelaySeconds": 60,
    "errorTransitionId": "error-handler"
  }
}
```

### Gateway Step (Decision/Routing)

```json
{
  "type": "gateway",
  "configuration": {
    "gatewayType": "exclusive",
    "defaultTransitionId": "default-path"
  },
  "transitions": [
    {
      "id": "approved-path",
      "targetStepId": "next-step",
      "condition": "approved == true"
    },
    {
      "id": "rejected-path",
      "targetStepId": "rejection-step",
      "condition": "approved == false"
    }
  ]
}
```

## Expression Syntax

Expressions use C# syntax via System.Linq.Dynamic.Core:

```
// Comparison
approved == true
amount > 1000
status != "pending"

// Logical operators
approved == true && amount > 0
department == "IT" || priority == "high"

// Variable references
$employeeName
$startDate

// Method calls
DateTime.UtcNow.AddDays(7)
```

## Custom Table Names

Override the repository to use custom table names:

```csharp
public class CustomWorkflowRepository : OracleWorkflowDefinitionRepository
{
    public CustomWorkflowRepository(RepositoryConfiguration config) : base(config) { }
    
    protected override string TableName => "CUSTOM_SCHEMA.MY_WORKFLOWS";
}

// Register custom repository
services.AddScoped<IWorkflowDefinitionRepository, CustomWorkflowRepository>();
```

## Background Processing

For scheduled steps and long-running workflows, implement a background service:

```csharp
public class WorkflowBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var engine = scope.ServiceProvider.GetRequiredService<IWorkflowEngine>();
            
            await engine.ProcessScheduledStepsAsync();
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

## Advanced Features

### Workflow Correlation

Track related workflows using correlation IDs:

```csharp
await engine.StartWorkflowAsync(
    workflowDefinitionId: "approval-process",
    correlationId: "ORDER-12345"
);

// Later, find the workflow
var instanceRepo = serviceProvider.GetRequiredService<IWorkflowInstanceRepository>();
var instance = await instanceRepo.GetByCorrelationIdAsync("ORDER-12345");
```

### Error Handling

Activity steps support automatic retry and error transitions:

```json
{
  "type": "activity",
  "configuration": {
    "serviceName": "PaymentService",
    "methodName": "ProcessPayment",
    "retryCount": 3,
    "retryDelaySeconds": 30,
    "errorTransitionId": "payment-failed-step"
  }
}
```

## Examples

See the `Examples` folder for complete workflow definitions:
- `employee-onboarding.json` - Complete onboarding process
- Additional examples coming soon

## License

MIT License

## Contributing

Contributions are welcome! Please submit issues and pull requests on GitHub.
