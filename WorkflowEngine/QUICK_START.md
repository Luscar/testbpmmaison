# WorkflowEngine.Core - Quick Start Guide

## What You've Got

A complete, production-ready BPM (Business Process Management) engine for C# with:

✅ **4 Step Types** (renamed for clarity):
- **InteractionStep** (was UserStep) - Wait for user input
- **ScheduledStep** (was DelayStep) - Wait until a specific date
- **ActivityStep** (was BusinessStep) - Execute business logic  
- **GatewayStep** (was DecisionStep) - Route based on conditions

✅ **Oracle Database Support** with customizable table names
✅ **JSON Workflow Definitions** - Define processes in JSON
✅ **Expression Engine** - Powerful condition evaluation
✅ **Complete Example Application**

## 5-Minute Quick Start

### 1. Create Database Tables

```sql
-- Run: Database/oracle_schema.sql
-- Creates: WF_DEFINITIONS, WF_INSTANCES, WF_STEP_INSTANCES
```

### 2. Configure Connection

```csharp
var config = new RepositoryConfiguration
{
    ConnectionString = "your-oracle-connection-string",
    Schema = "dbo",
    WorkflowDefinitionTable = "WF_DEFINITIONS",
    WorkflowInstanceTable = "WF_INSTANCES", 
    StepInstanceTable = "WF_STEP_INSTANCES"
};
```

### 3. Register Services (Startup.cs or Program.cs)

```csharp
// Repositories
services.AddSingleton(config);
services.AddScoped<IWorkflowDefinitionRepository, OracleWorkflowDefinitionRepository>();
services.AddScoped<IWorkflowInstanceRepository, OracleWorkflowInstanceRepository>();
services.AddScoped<IStepInstanceRepository, OracleStepInstanceRepository>();

// Services
services.AddSingleton<IActivityServiceRegistry, ActivityServiceRegistry>();
services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

// Step Executors
services.AddScoped<IStepExecutor, InteractionStepExecutor>();
services.AddScoped<IStepExecutor, ScheduledStepExecutor>();
services.AddScoped<IStepExecutor, ActivityStepExecutor>();
services.AddScoped<IStepExecutor, GatewayStepExecutor>();

// Engine
services.AddScoped<IWorkflowEngine, WorkflowEngine.Core.Services.WorkflowEngine>();
services.AddScoped<WorkflowDefinitionLoader>();
```

### 4. Register Your Business Services

```csharp
var registry = serviceProvider.GetRequiredService<IActivityServiceRegistry>();
registry.RegisterService("EmailService", new EmailService());
registry.RegisterService("PaymentService", new PaymentService());
```

### 5. Load Workflow Definition

```csharp
var loader = serviceProvider.GetRequiredService<WorkflowDefinitionLoader>();
var definition = await loader.LoadFromFileAsync("employee-onboarding.json");
await definitionRepo.CreateAsync(definition);
```

### 6. Start a Workflow

```csharp
var engine = serviceProvider.GetRequiredService<IWorkflowEngine>();

var instanceId = await engine.StartWorkflowAsync(
    workflowDefinitionId: "employee-onboarding-v1",
    variables: new Dictionary<string, object>
    {
        ["employeeName"] = "John Doe",
        ["department"] = "Engineering"
    },
    correlationId: "EMP-001",
    createdBy: "hr@company.com"
);
```

## JSON Workflow Example

```json
{
  "id": "simple-approval-v1",
  "name": "Simple Approval Process",
  "version": "1.0.0",
  "initialStepId": "request-step",
  "variables": {
    "approved": false
  },
  "steps": [
    {
      "id": "request-step",
      "name": "Request Approval",
      "type": "interaction",
      "configuration": {
        "assignedRoles": ["Manager"]
      },
      "transitions": [
        {
          "id": "to-decision",
          "targetStepId": "decision-step"
        }
      ]
    },
    {
      "id": "decision-step",
      "name": "Check Approval",
      "type": "gateway",
      "configuration": {
        "gatewayType": "exclusive"
      },
      "transitions": [
        {
          "id": "approved",
          "targetStepId": "send-email",
          "condition": "approved == true"
        }
      ]
    },
    {
      "id": "send-email",
      "name": "Send Notification",
      "type": "activity",
      "configuration": {
        "serviceName": "EmailService",
        "methodName": "SendEmail",
        "inputMapping": {
          "to": "$userEmail"
        }
      },
      "transitions": []
    }
  ]
}
```

## Complete Interaction Steps

```csharp
// Get pending steps for a user
var pending = await engine.GetPendingInteractionStepsAsync("manager@company.com");

// Complete a step
await engine.CompleteInteractionStepAsync(
    stepInstanceId: pending.First().Id,
    outputData: new Dictionary<string, object>
    {
        ["approved"] = true,
        ["comments"] = "Looks good!"
    },
    completedBy: "manager@company.com"
);
```

## Process Scheduled Steps

Run this in a background service:

```csharp
public class WorkflowScheduler : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _engine.ProcessScheduledStepsAsync();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

## Custom Table Names

### Option 1: Override Repository

```csharp
public class MyWorkflowRepo : OracleWorkflowInstanceRepository
{
    protected override string TableName => "MY_SCHEMA.MY_WORKFLOWS";
}
```

### Option 2: Configuration

```csharp
var config = new RepositoryConfiguration
{
    Schema = "CUSTOM_SCHEMA",
    WorkflowDefinitionTable = "MY_WF_DEFS",
    WorkflowInstanceTable = "MY_WF_INSTANCES",
    StepInstanceTable = "MY_STEPS"
};
```

## Build NuGet Package

**Linux/Mac:**
```bash
chmod +x build-nuget.sh
./build-nuget.sh
```

**Windows:**
```cmd
build-nuget.bat
```

Package will be in `./nupkg/` folder.

## File Structure

```
WorkflowEngine/
├── WorkflowEngine.Core/           # Main library (NuGet package)
├── WorkflowEngine.Example/        # Example console app
├── Database/                      # SQL scripts
├── Examples/                      # Workflow JSON examples
├── README.md                      # Full documentation
├── IMPLEMENTATION_GUIDE.md        # Detailed guide
└── PROJECT_STRUCTURE.md           # Architecture overview
```

## Next Steps

1. **Read the full README.md** for complete documentation
2. **Check IMPLEMENTATION_GUIDE.md** for best practices
3. **Run the Example application** to see it in action
4. **Customize** repository classes for your table names
5. **Create** your workflow definitions in JSON
6. **Deploy** to production!

## Common Patterns

### Activity Service

```csharp
public class PaymentService
{
    public async Task<Dictionary<string, object>> ProcessPayment(decimal amount, string customerId)
    {
        // Your business logic
        var result = await _paymentGateway.ChargeAsync(amount, customerId);
        
        return new Dictionary<string, object>
        {
            ["transactionId"] = result.Id,
            ["success"] = result.Success
        };
    }
}
```

### Expression Examples

```javascript
// Simple comparison
amount > 1000

// Multiple conditions  
approved == true && amount <= 5000

// String operations
department == "IT" || priority == "high"

// Date calculations
DateTime.UtcNow.AddDays(30)
```

## Support

- Check the **README.md** for detailed documentation
- Review **IMPLEMENTATION_GUIDE.md** for advanced topics
- Look at **Examples/** folder for sample workflows
- See **WorkflowEngine.Example/** for working code

## Tips

1. Always validate workflow definitions before deploying
2. Use correlation IDs to track related workflows  
3. Implement idempotent activity services
4. Run scheduled step processor in background
5. Monitor workflow status and step history
6. Cleanup old completed workflows regularly

Happy workflow building! 🚀
