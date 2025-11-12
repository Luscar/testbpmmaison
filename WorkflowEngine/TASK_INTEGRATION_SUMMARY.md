# External Task System Integration - Summary

## What This Solves

You have an **existing task management system** that handles:
- Task creation and assignment
- User interface for task forms
- Task lifecycle management
- Notifications and reminders

You want the **workflow engine** to create tasks automatically when workflows reach InteractionSteps, and continue workflows when users complete tasks.

## The Solution

✅ **One Interface to Implement:** `IExternalTaskSystem`  
✅ **Automatic Task Creation:** Engine creates tasks when reaching InteractionSteps  
✅ **Automatic Task Closure:** Engine closes tasks when completing steps  
✅ **Full Control:** Your system handles all assignment, UI, and lifecycle logic  

## How It Works

### 1. Workflow Reaches InteractionStep

```json
{
  "id": "approval-step",
  "type": "interaction",
  "nextStepId": "process-result",
  "configuration": {
    "assignedRoles": ["Manager"],
    "timeoutMinutes": 2880
  }
}
```

### 2. Engine Calls Your Task System

```csharp
// Automatically called by InteractionStepExecutor
var externalTaskId = await yourTaskSystem.CreateTaskAsync(new ExternalTaskInfo
{
    WorkflowInstanceId = "WF-123",
    StepInstanceId = "STEP-456",
    Title = "Approve Request",
    AssignedRoles = ["Manager"],
    DueDate = DateTime.Now.AddMinutes(2880),
    WorkflowContext = workflowVariables
});

// Task ID stored for later: stepInstance.OutputData["externalTaskId"] = externalTaskId
```

### 3. Your Task System Creates Task

Your system receives the request and:
- Creates task with its own ID
- Applies your assignment logic
- Shows task in your UI
- Manages task lifecycle
- **Returns task ID to workflow engine**

### 4. User Completes Task in Your UI

User fills out form and submits through your existing interface.

### 5. Your UI Calls Workflow Engine

```csharp
// In your task completion handler
await workflowEngine.CompleteInteractionStepAsync(
    stepInstanceId: "STEP-456",  // Map from your task ID
    outputData: formData
);
```

### 6. Engine Closes Task in Your System

```csharp
// Automatically called by WorkflowEngine
await yourTaskSystem.CloseTaskAsync(
    externalTaskId: "TASK-789",
    completionData: formData
);
```

### 7. Workflow Continues

Engine proceeds to next step automatically.

## Implementation Checklist

### Step 1: Create Your Implementation ✅

```csharp
public class YourTaskSystem : IExternalTaskSystem
{
    public async Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
    {
        // POST to your task API
        var response = await _http.PostAsync("YOUR_API/tasks", ...);
        return response.TaskId;
    }

    public async Task CloseTaskAsync(string externalTaskId, Dictionary<string, object> data)
    {
        // PUT to your task API
        await _http.PutAsync($"YOUR_API/tasks/{externalTaskId}/complete", ...);
    }

    // Implement UpdateTaskAsync and CancelTaskAsync
}
```

### Step 2: Register in DI ✅

```csharp
services.AddSingleton<IExternalTaskSystem>(sp => 
    new YourTaskSystem(apiUrl, apiKey)
);
```

### Step 3: Map Task IDs to Step IDs ✅

Option A: Store in database when task is created
```csharp
_db.SaveMapping(externalTaskId, stepInstanceId);
```

Option B: Query by externalTaskId in OutputData
```csharp
var step = await _stepRepo.GetByExternalTaskId(externalTaskId);
```

### Step 4: Update UI Completion Handler ✅

```csharp
[HttpPost("tasks/{taskId}/complete")]
public async Task<IActionResult> CompleteTask(string taskId, TaskData data)
{
    var stepId = await GetStepIdForTask(taskId);
    
    await _workflowEngine.CompleteInteractionStepAsync(
        stepInstanceId: stepId,
        outputData: data.FormValues
    );
    
    return Ok();
}
```

## What Gets Passed to Your System

### On CreateTaskAsync:

```csharp
{
    "workflowInstanceId": "WF-123",           // For correlation
    "stepInstanceId": "STEP-456",             // Workflow internal ID
    "title": "Approve Purchase Request",      // From step name
    "description": "...",                     // Context
    "assignedUsers": ["john@...", "jane@..."],// From workflow config
    "assignedRoles": ["Manager", "Approver"], // From workflow config
    "dueDate": "2024-12-31T23:59:59Z",       // Calculated from timeout
    "formSchema": "{...}",                    // If provided
    "workflowContext": {                      // Current workflow variables
        "amount": 5000,
        "requestor": "bob@company.com"
    },
    "metadata": { ... }                       // Additional info
}
```

### On CloseTaskAsync:

```csharp
{
    "externalTaskId": "TASK-789",             // Your task ID
    "completionData": {                       // Form data from user
        "approved": true,
        "comments": "Approved with conditions"
    }
}
```

## Error Handling

### If External System Unavailable During Creation:
- ⚠️ Warning logged
- ✅ Workflow continues (step still waits for input)
- ✅ Can create task manually later
- ✅ Can retry via UpdateTaskAsync

### If External System Unavailable During Closure:
- ⚠️ Warning logged
- ✅ **Workflow proceeds anyway** (doesn't block)
- ⚠️ Your system may have orphaned tasks (handle in cleanup)

## Testing Without External System

### Option 1: Null Implementation
```csharp
services.AddScoped<IWorkflowEngine>(sp => new WorkflowEngine(
    ...,
    externalTaskSystem: null  // No external system
));
```

### Option 2: Mock Implementation
```csharp
services.AddSingleton<IExternalTaskSystem, MockExternalTaskSystem>();
```

The mock logs to console instead of calling real APIs.

## Example Workflow with Tasks

```json
{
  "id": "purchase-approval",
  "steps": [
    {
      "id": "submit-request",
      "type": "business",
      "nextStepId": "manager-review"
    },
    {
      "id": "manager-review",
      "type": "interaction",      ← Creates task in YOUR system
      "nextStepId": "check-amount",
      "configuration": {
        "assignedRoles": ["Manager"]
      }
    },
    {
      "id": "check-amount",
      "type": "decision",
      "configuration": {
        "routes": [
          {"name": "high", "nextStepId": "executive-review", "condition": "amount > 10000"},
          {"name": "low", "nextStepId": "process-order"}
        ]
      }
    },
    {
      "id": "executive-review",
      "type": "interaction",      ← Creates another task
      "nextStepId": "process-order",
      "configuration": {
        "assignedRoles": ["Executive"]
      }
    },
    {
      "id": "process-order",
      "type": "business"
    }
  ]
}
```

**Result:**
- Task created for manager
- Manager completes in your UI
- Workflow evaluates amount
- If > 10000, task created for executive
- Executive completes in your UI
- Order processed

## Architecture Benefits

### Clear Separation
```
┌─────────────────────────────────────┐
│   YOUR TASK SYSTEM                  │
│   - Assignment logic                │
│   - UI/Forms                        │
│   - Notifications                   │
│   - User management                 │
└─────────────────┬───────────────────┘
                  │ IExternalTaskSystem
                  │ (Simple interface)
┌─────────────────▼───────────────────┐
│   WORKFLOW ENGINE                   │
│   - Process orchestration           │
│   - Step sequencing                 │
│   - Decision routing                │
│   - Variable management             │
└─────────────────────────────────────┘
```

### Advantages

✅ **Minimal Integration Effort**
- One interface to implement
- ~100 lines of code
- No changes to existing task UI

✅ **Keep Your Existing System**
- No migration needed
- Use your current assignment logic
- Keep your UI as-is

✅ **Flexible**
- Works with any task system
- REST, SOAP, database, message queue
- Even multiple task systems

✅ **Resilient**
- Graceful error handling
- Non-blocking
- Can work offline

✅ **Transparent**
- Automatic task creation
- Automatic task closure
- You just implement the interface

## Files to Review

📄 **EXTERNAL_TASK_INTEGRATION.md** - Complete implementation guide  
📄 **EXTERNAL_TASK_DIAGRAMS.md** - Visual diagrams and flows  
📄 **Examples/ExternalTaskSystemExample.cs** - Code examples  
📄 **WorkflowEngine.Core/Integration/IExternalTaskSystem.cs** - Interface definition  

## Quick Start

1. **Implement the interface:**
   ```csharp
   public class MyTaskSystem : IExternalTaskSystem { ... }
   ```

2. **Register it:**
   ```csharp
   services.AddSingleton<IExternalTaskSystem, MyTaskSystem>();
   ```

3. **Update task completion:**
   ```csharp
   await _workflowEngine.CompleteInteractionStepAsync(stepId, data);
   ```

4. **Done!** ✅

Tasks are now automatically created and closed as workflows progress through InteractionSteps.

## Summary

Your existing task system and the workflow engine work together seamlessly:

**Your System:** Handles all task-related concerns (assignment, UI, lifecycle)  
**Workflow Engine:** Handles process orchestration (sequencing, routing, decisions)  
**Integration:** One simple interface with automatic task creation/closure  

Result: Best of both worlds! 🎉

---

Questions? See the full documentation in:
- EXTERNAL_TASK_INTEGRATION.md
- EXTERNAL_TASK_DIAGRAMS.md
