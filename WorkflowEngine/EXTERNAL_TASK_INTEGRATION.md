# External Task System Integration Guide

## Overview

The workflow engine integrates seamlessly with your existing task management system. When a workflow reaches an **InteractionStep**, a task is automatically created in your external system. When users complete the task through your UI, the workflow continues.

## Integration Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  Workflow Engine Flow with External Task System                 │
└─────────────────────────────────────────────────────────────────┘

1. Workflow reaches InteractionStep
   ↓
2. WorkflowEngine → InteractionStepExecutor
   ↓
3. InteractionStepExecutor → IExternalTaskSystem.CreateTaskAsync()
   ↓
4. External Task System creates task
   │  - Applies assignment logic (users/roles)
   │  - Shows task in UI
   │  - Manages task lifecycle
   ↓
5. Task ID stored in StepInstance.OutputData["externalTaskId"]
   ↓
6. Workflow status = WaitingForInput
   
   ... Time passes, user works on task in external UI ...
   
7. User completes task in your UI
   ↓
8. Your UI calls → WorkflowEngine.CompleteInteractionStepAsync()
   ↓
9. WorkflowEngine → IExternalTaskSystem.CloseTaskAsync()
   ↓
10. External Task System closes task
   ↓
11. Workflow continues to next step
```

## Implementation Steps

### 1. Implement IExternalTaskSystem Interface

Create a class that connects to your task system's API:

```csharp
public class YourTaskSystem : IExternalTaskSystem
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    
    public YourTaskSystem(string apiBaseUrl, string apiKey)
    {
        _apiBaseUrl = apiBaseUrl;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
    {
        // Call YOUR task system's create task API
        var payload = new
        {
            title = taskInfo.Title,
            description = taskInfo.Description,
            assignedUsers = taskInfo.AssignedUsers,
            assignedRoles = taskInfo.AssignedRoles,
            dueDate = taskInfo.DueDate,
            customData = new
            {
                workflowInstanceId = taskInfo.WorkflowInstanceId,
                stepInstanceId = taskInfo.StepInstanceId
            }
        };

        var response = await _httpClient.PostAsync(
            $"{_apiBaseUrl}/tasks", 
            JsonContent.Create(payload)
        );
        
        var result = await response.Content.ReadFromJsonAsync<CreateTaskResponse>();
        
        // Return the task ID from YOUR system
        return result.TaskId;
    }

    public async Task CloseTaskAsync(string externalTaskId, Dictionary<string, object> completionData)
    {
        // Call YOUR task system's close task API
        await _httpClient.PutAsync(
            $"{_apiBaseUrl}/tasks/{externalTaskId}/complete",
            JsonContent.Create(new { data = completionData })
        );
    }

    // Implement other methods...
}
```

### 2. Register the External Task System

In your startup/configuration:

```csharp
services.AddSingleton<IExternalTaskSystem>(sp => 
    new YourTaskSystem(
        apiBaseUrl: Configuration["TaskSystem:ApiUrl"],
        apiKey: Configuration["TaskSystem:ApiKey"]
    )
);

// Register other workflow services
services.AddScoped<IWorkflowEngine, WorkflowEngine>();
// ... other registrations
```

### 3. Configure InteractionSteps in JSON

```json
{
  "id": "approval-step",
  "name": "Manager Approval Required",
  "type": "interaction",
  "nextStepId": "process-approval",
  "configuration": {
    "assignedUsers": ["manager@company.com"],
    "assignedRoles": ["Manager", "Supervisor"],
    "timeoutMinutes": 2880,
    "formSchema": "{\"type\":\"object\",\"properties\":{\"approved\":{\"type\":\"boolean\"},\"comments\":{\"type\":\"string\"}}}"
  }
}
```

**Note:** Assignment logic is handled by YOUR external system. The workflow engine just passes the configured users/roles.

### 4. When User Completes Task in Your UI

When a user completes a task through your existing UI, call the workflow engine:

```csharp
// In your task completion handler
public async Task OnTaskCompleted(string taskId, Dictionary<string, object> formData)
{
    // Get the step instance ID that's associated with this external task
    var stepInstanceId = await _taskMapping.GetStepInstanceId(taskId);
    
    // Complete the workflow step
    await _workflowEngine.CompleteInteractionStepAsync(
        stepInstanceId: stepInstanceId,
        outputData: formData,
        completedBy: currentUser.Email
    );
    
    // The workflow engine will:
    // 1. Close the task in your external system
    // 2. Continue the workflow to the next step
}
```

## Data Flow Details

### When Creating a Task

**Workflow Engine provides to YOUR system:**
- `WorkflowInstanceId` - For correlation
- `StepInstanceId` - Internal workflow tracking
- `Title` - Task title
- `Description` - Task description
- `AssignedUsers` - List of user emails/IDs from workflow config
- `AssignedRoles` - List of role names from workflow config
- `FormSchema` - JSON schema for the form (if your system supports it)
- `WorkflowContext` - Current workflow variables
- `DueDate` - Calculated from timeout
- `Metadata` - Additional context

**YOUR system handles:**
- User/role resolution (if needed)
- Task assignment logic
- UI rendering
- Task lifecycle management
- Notifications
- Reassignment

**YOUR system returns:**
- `externalTaskId` - Task ID from your system

### When Closing a Task

**Workflow Engine provides to YOUR system:**
- `externalTaskId` - The task ID to close
- `completionData` - Form data from the user

**YOUR system handles:**
- Marking task as complete
- Cleanup
- Audit logging

## Example: Complete Integration

```csharp
// 1. Implement your task system connector
public class CompanyTaskSystem : IExternalTaskSystem
{
    public async Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
    {
        // Your implementation here
        return await _api.CreateTask(...);
    }

    public async Task CloseTaskAsync(string externalTaskId, Dictionary<string, object> completionData)
    {
        // Your implementation here
        await _api.CompleteTask(externalTaskId, completionData);
    }
    
    // ... other methods
}

// 2. Register in DI
services.AddSingleton<IExternalTaskSystem, CompanyTaskSystem>();

// 3. Workflow JSON with interaction step
{
  "steps": [
    {
      "id": "review-request",
      "type": "interaction",
      "nextStepId": "process-result",
      "configuration": {
        "assignedRoles": ["Reviewer"]
      }
    }
  ]
}

// 4. Task completion in your UI controller
[HttpPost("tasks/{taskId}/complete")]
public async Task<IActionResult> CompleteTask(string taskId, [FromBody] TaskCompletionData data)
{
    // Map task ID to workflow step
    var stepId = await GetStepInstanceId(taskId);
    
    // Complete workflow step (this also closes the external task)
    await _workflowEngine.CompleteInteractionStepAsync(
        stepInstanceId: stepId,
        outputData: data.FormData
    );
    
    return Ok();
}
```

## Mapping Task IDs to Step IDs

You need to maintain a mapping between your external task IDs and workflow step instance IDs:

```csharp
// Simple mapping service
public class TaskStepMapping
{
    private readonly Dictionary<string, string> _map = new();
    
    public void Map(string externalTaskId, string stepInstanceId)
    {
        _map[externalTaskId] = stepInstanceId;
    }
    
    public string GetStepInstanceId(string externalTaskId)
    {
        return _map.TryGetValue(externalTaskId, out var stepId) ? stepId : null;
    }
}

// Store mapping when task is created
// You can also query the database by externalTaskId in OutputData
```

Or query from the database:

```csharp
public async Task<string> GetStepInstanceIdByExternalTaskId(string externalTaskId)
{
    var steps = await _stepRepository.GetPendingStepsAsync();
    
    var step = steps.FirstOrDefault(s => 
        s.OutputData.ContainsKey("externalTaskId") &&
        s.OutputData["externalTaskId"]?.ToString() == externalTaskId
    );
    
    return step?.Id;
}
```

## Error Handling

The integration includes graceful error handling:

### Task Creation Failure
If the external system is unavailable when creating a task:
- Error is logged
- Workflow continues (still waiting for input)
- You can manually create the task later
- Or retry via `UpdateTaskAsync`

### Task Closure Failure  
If the external system is unavailable when closing a task:
- Warning is logged
- **Workflow continues anyway** (doesn't block workflow progress)
- Your system should handle orphaned tasks

```csharp
// The engine includes try-catch blocks:
try
{
    await _externalTaskSystem.CreateTaskAsync(taskInfo);
}
catch (Exception ex)
{
    Console.WriteLine($"⚠ Warning: Failed to create external task: {ex.Message}");
    // Workflow continues - step is still WaitingForInput
}
```

## Testing Without External System

You can test workflows without implementing the external system:

```csharp
// Option 1: Don't register IExternalTaskSystem (pass null)
services.AddScoped<IWorkflowEngine>(sp => new WorkflowEngine(
    definitionRepo,
    instanceRepo,
    stepRepo,
    executors,
    externalTaskSystem: null  // No external system
));

// Option 2: Use the mock implementation
services.AddSingleton<IExternalTaskSystem, MockExternalTaskSystem>();
```

## Advanced: Timeout Handling

If you want to handle task timeouts:

```csharp
public class YourTaskSystem : IExternalTaskSystem
{
    public async Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
    {
        var taskId = await _api.CreateTask(...);
        
        // If timeout is specified, schedule cancellation
        if (taskInfo.DueDate.HasValue)
        {
            await ScheduleTimeoutCheck(taskId, taskInfo.DueDate.Value, taskInfo.StepInstanceId);
        }
        
        return taskId;
    }
    
    private async Task ScheduleTimeoutCheck(string taskId, DateTime dueDate, string stepInstanceId)
    {
        // Your timeout handling logic
        // When timeout occurs, you could:
        // 1. Cancel the task
        // 2. Escalate to another user
        // 3. Route workflow to timeout handler
    }
}
```

## Summary

✅ **Simple Integration**: Implement one interface (`IExternalTaskSystem`)  
✅ **Loose Coupling**: Your task system manages assignment/UI/lifecycle  
✅ **Graceful Degradation**: Works even if external system is unavailable  
✅ **Flexible**: Support any task system (REST API, database, message queue)  
✅ **Transparent**: Workflow engine handles task creation/closure automatically  

Your existing task management system remains in control of:
- User assignment logic
- Task UI and forms
- Notifications
- Task reassignment
- Access control

The workflow engine just:
- Creates tasks when InteractionSteps are reached
- Closes tasks when steps are completed
- Continues workflow execution

See `Examples/ExternalTaskSystemExample.cs` for complete implementation examples.
