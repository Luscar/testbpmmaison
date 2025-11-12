# WorkflowEngine Implementation Guide

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Step Types Explained](#step-types-explained)
3. [Database Schema](#database-schema)
4. [Custom Repository Implementation](#custom-repository-implementation)
5. [Expression Syntax](#expression-syntax)
6. [Best Practices](#best-practices)
7. [Performance Tuning](#performance-tuning)
8. [Security Considerations](#security-considerations)

## Architecture Overview

The WorkflowEngine.Core library follows a layered architecture:

```
┌─────────────────────────────────────┐
│     Workflow Engine Service         │  ← Main orchestrator
├─────────────────────────────────────┤
│     Step Executors                  │  ← Execute different step types
│  • InteractionStepExecutor          │
│  • ScheduledStepExecutor            │
│  • ActivityStepExecutor             │
│  • GatewayStepExecutor              │
├─────────────────────────────────────┤
│     Supporting Services             │
│  • ExpressionEvaluator              │  ← Evaluate conditions
│  • ActivityServiceRegistry          │  ← Manage business services
│  • WorkflowDefinitionLoader         │  ← Load JSON definitions
├─────────────────────────────────────┤
│     Repository Layer                │
│  • WorkflowDefinitionRepository     │
│  • WorkflowInstanceRepository       │
│  • StepInstanceRepository           │
├─────────────────────────────────────┤
│     Oracle Database                 │
└─────────────────────────────────────┘
```

## Step Types Explained

### 1. Interaction Steps (User Input)

**Original Name:** UserStep  
**Renamed To:** InteractionStep  
**Reason:** More descriptive - indicates interaction with external users/systems

**Use Cases:**
- Manual approval steps
- Form submissions
- Human task completion
- External system callbacks

**Configuration Example:**
```json
{
  "type": "interaction",
  "configuration": {
    "formSchema": "{json-schema}",
    "assignedUsers": ["john@company.com"],
    "assignedRoles": ["Manager", "Approver"],
    "timeoutMinutes": 1440,
    "timeoutTransitionId": "timeout-handler",
    "allowReassignment": true
  }
}
```

**Behavior:**
- Step status becomes `WaitingForInput`
- Workflow status becomes `Waiting`
- Can be assigned to specific users or roles
- Supports timeout with configurable action
- Completed via `CompleteInteractionStepAsync()`

### 2. Scheduled Steps (Wait Until Date)

**Original Name:** DelayStep  
**Renamed To:** ScheduledStep  
**Reason:** "Scheduled" better conveys waiting until a specific time vs. a generic delay

**Use Cases:**
- Wait until contract start date
- Schedule payment on due date
- Time-based workflow progression
- Compliance waiting periods

**Configuration Example:**
```json
{
  "type": "scheduled",
  "configuration": {
    "targetDateTime": "2025-01-01T00:00:00Z",
    "dateVariable": "startDate",
    "scheduleExpression": "DateTime.UtcNow.AddDays(30)",
    "skipIfPast": true
  }
}
```

**Behavior:**
- Step status becomes `Scheduled`
- Executed by `ProcessScheduledStepsAsync()`
- Can use fixed date, variable, or expression
- Optional skip if target date has passed

**Processing Pattern:**
```csharp
// Run this in a background service
public class ScheduledStepProcessor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _engine.ProcessScheduledStepsAsync();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

### 3. Activity Steps (Business Logic)

**Original Name:** BusinessStep  
**Renamed To:** ActivityStep  
**Reason:** Aligns with BPMN terminology and better describes execution of business activities

**Use Cases:**
- Call external APIs
- Execute business rules
- Data transformation
- System integrations
- Database operations

**Configuration Example:**
```json
{
  "type": "activity",
  "configuration": {
    "serviceName": "PaymentService",
    "methodName": "ProcessPayment",
    "inputMapping": {
      "amount": "$orderAmount",
      "customerId": "$customerId"
    },
    "outputMapping": {
      "transactionId": "paymentTxnId",
      "success": "paymentSuccess"
    },
    "retryCount": 3,
    "retryDelaySeconds": 60,
    "errorTransitionId": "payment-error-handler"
  }
}
```

**Behavior:**
- Invokes registered service methods
- Maps workflow variables to method parameters
- Maps return values back to workflow variables
- Supports automatic retry with configurable delay
- Can route to error handler on failure

**Service Registration:**
```csharp
var registry = serviceProvider.GetRequiredService<IActivityServiceRegistry>();

// Register your service
registry.RegisterService("PaymentService", new PaymentService());

// Service implementation
public class PaymentService
{
    public async Task<Dictionary<string, object>> ProcessPayment(decimal amount, string customerId)
    {
        // Your business logic here
        var result = await _paymentGateway.ChargeAsync(amount, customerId);
        
        return new Dictionary<string, object>
        {
            ["transactionId"] = result.TransactionId,
            ["success"] = result.Success
        };
    }
}
```

### 4. Gateway Steps (Decision/Routing)

**Original Name:** DecisionStep  
**Renamed To:** GatewayStep  
**Reason:** Matches BPMN standard terminology for routing patterns

**Use Cases:**
- Conditional workflow branching
- Parallel execution paths
- Inclusive routing
- Default fallback routes

**Configuration Example:**
```json
{
  "type": "gateway",
  "configuration": {
    "gatewayType": "exclusive",
    "defaultTransitionId": "default-path"
  },
  "transitions": [
    {
      "id": "high-value",
      "targetStepId": "executive-approval",
      "condition": "amount > 10000"
    },
    {
      "id": "standard",
      "targetStepId": "manager-approval",
      "condition": "amount <= 10000"
    }
  ]
}
```

**Gateway Types:**
- **Exclusive (XOR):** Takes first matching transition only
- **Parallel (AND):** Takes all transitions (requires parallel execution support)
- **Inclusive (OR):** Takes all matching transitions

**Evaluation Order:**
- Transitions evaluated in order defined in JSON
- First match wins for exclusive gateways
- Use `defaultTransitionId` as fallback

## Database Schema

### Table Purposes

**WF_DEFINITIONS:**
- Stores workflow templates/definitions
- Supports versioning
- Contains serialized step definitions

**WF_INSTANCES:**
- Runtime workflow executions
- Tracks current state and status
- Stores workflow variables

**WF_STEP_INSTANCES:**
- Individual step executions
- Audit trail of step history
- Input/output data for each step

**WF_STEP_HISTORY (Optional):**
- Detailed audit log
- Step state changes
- User actions

### Indexes for Performance

The schema includes indexes on:
- Status fields (for querying pending workflows)
- Foreign keys (for joins)
- Correlation IDs (for workflow lookup)
- Assigned users (for task lists)
- Scheduled dates (for scheduled step processing)

## Custom Repository Implementation

### Method 1: Override Table Names

```csharp
public class MyCompanyWorkflowRepository : OracleWorkflowInstanceRepository
{
    public MyCompanyWorkflowRepository(RepositoryConfiguration config) : base(config)
    {
    }

    protected override string TableName => "MYCOMPANY_SCHEMA.WORKFLOWS";
}
```

### Method 2: Configuration-Based

```csharp
var config = new RepositoryConfiguration
{
    ConnectionString = connectionString,
    Schema = "MYCOMPANY",
    WorkflowDefinitionTable = "WORKFLOW_DEFINITIONS",
    WorkflowInstanceTable = "WORKFLOW_INSTANCES",
    StepInstanceTable = "STEP_INSTANCES"
};
```

### Method 3: Add Custom Behavior

```csharp
public class AuditedRepository : OracleWorkflowInstanceRepository
{
    private readonly IAuditService _audit;

    public override async Task<string> CreateAsync(WorkflowInstance instance)
    {
        var id = await base.CreateAsync(instance);
        await _audit.LogCreationAsync(id);
        return id;
    }
}
```

## Expression Syntax

The engine uses System.Linq.Dynamic.Core for expressions:

### Variable References
```csharp
// In mapping, prefix with $
"inputMapping": {
    "amount": "$orderAmount"
}

// In conditions, use directly
"condition": "amount > 1000"
```

### Comparison Operators
```csharp
amount > 1000
status == "approved"
priority != "low"
age >= 18
count <= 100
```

### Logical Operators
```csharp
amount > 1000 && category == "urgent"
status == "pending" || status == "review"
!(approved == true)
```

### Date/Time Expressions
```csharp
DateTime.UtcNow.AddDays(30)
DateTime.Parse(dueDate) < DateTime.UtcNow
startDate >= DateTime.Today
```

### String Operations
```csharp
name.StartsWith("Admin")
email.Contains("@company.com")
status.ToLower() == "approved"
```

## Best Practices

### 1. Workflow Design

**Keep Workflows Focused:**
- One workflow per business process
- Break complex processes into sub-workflows
- Use clear, descriptive step names

**Error Handling:**
- Always provide error transitions for activities
- Use retry logic for transient failures
- Design compensation flows for rollback

**Variables:**
- Initialize all variables in workflow definition
- Use consistent naming conventions
- Document variable purposes

### 2. Service Implementation

**Activity Services:**
- Keep methods async
- Return Dictionary<string, object> for easy mapping
- Handle exceptions gracefully
- Log important operations

**Idempotency:**
- Make activity services idempotent when possible
- Use correlation IDs for duplicate detection
- Store operation results to enable retry

### 3. Performance

**Database:**
- Regularly cleanup completed workflows
- Archive old workflow data
- Monitor index performance
- Use connection pooling

**Scheduled Steps:**
- Adjust polling frequency based on requirements
- Use distributed locking for multiple instances
- Batch process scheduled steps when possible

**Caching:**
- Cache workflow definitions
- Consider caching frequently accessed instances
- Implement proper cache invalidation

### 4. Security

**Access Control:**
- Validate user permissions before completing interaction steps
- Use role-based assignment for interaction steps
- Audit all workflow operations

**Data Protection:**
- Encrypt sensitive workflow variables
- Use parameterized queries (already implemented)
- Sanitize user inputs in expressions

**Service Security:**
- Validate service method parameters
- Implement service-level authentication
- Use secure communication for external services

## Performance Tuning

### Database Optimization

```sql
-- Regular maintenance
EXEC CLEANUP_OLD_WORKFLOWS(90); -- Remove workflows older than 90 days

-- Add additional indexes if needed
CREATE INDEX IDX_WF_CUSTOM ON WF_INSTANCES(CUSTOM_FIELD);

-- Monitor query performance
SELECT * FROM V$SQL WHERE SQL_TEXT LIKE '%WF_INSTANCES%';
```

### Batch Processing

```csharp
// Process scheduled steps in batches
var scheduledSteps = await _stepRepository.GetScheduledStepsAsync(DateTime.UtcNow);
await Task.WhenAll(scheduledSteps.Select(s => ProcessStepAsync(s)));
```

### Async/Await Best Practices

```csharp
// Don't block on async operations
var result = await _engine.StartWorkflowAsync(...); // Good
var result = _engine.StartWorkflowAsync(...).Result; // Bad

// Use ConfigureAwait(false) in libraries
await _repository.GetByIdAsync(id).ConfigureAwait(false);
```

## Security Considerations

### Input Validation

```csharp
// Validate before completing interaction steps
public async Task ValidateAndCompleteStepAsync(string stepId, Dictionary<string, object> data)
{
    // Validate user has permission
    if (!await _authService.CanCompleteStepAsync(stepId, userId))
        throw new UnauthorizedException();
    
    // Validate data
    ValidateOutputData(data);
    
    await _engine.CompleteInteractionStepAsync(stepId, data);
}
```

### Expression Safety

```csharp
// The expression evaluator is already sandboxed by System.Linq.Dynamic.Core
// But you can add additional validation:

public class SafeExpressionEvaluator : ExpressionEvaluator
{
    private readonly string[] _blacklistedTerms = { "System.IO", "Process.Start" };
    
    public override bool EvaluateCondition(string expression, Dictionary<string, object> variables)
    {
        if (_blacklistedTerms.Any(term => expression.Contains(term)))
            throw new SecurityException("Expression contains forbidden terms");
            
        return base.EvaluateCondition(expression, variables);
    }
}
```

---

For more examples and updates, visit the GitHub repository or check the Examples folder.
