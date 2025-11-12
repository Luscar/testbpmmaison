# External Task System Integration - Visual Guide

## Architecture Diagram

```
┌────────────────────────────────────────────────────────────────────────────┐
│                        YOUR APPLICATION                                     │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐         ┌──────────────────────────────────────────┐    │
│  │   Your UI    │         │      Workflow Engine NuGet               │    │
│  │              │         │                                           │    │
│  │  - Task List │◄────────┤  1. WorkflowEngine.StartWorkflowAsync()  │    │
│  │  - Task Form │         │     ↓                                     │    │
│  │  - Actions   │         │  2. Reaches InteractionStep              │    │
│  └──────┬───────┘         │     ↓                                     │    │
│         │                 │  3. InteractionStepExecutor               │    │
│         │                 │     ↓                                     │    │
│         │                 │  4. Calls IExternalTaskSystem             │    │
│         │                 └────────────────┬──────────────────────────┘    │
│         │                                  │                                │
│         │                 ┌────────────────▼──────────────────────────┐    │
│         │                 │  YourTaskSystem : IExternalTaskSystem     │    │
│         │                 │                                            │    │
│         │  CreateTask()   │  - CreateTaskAsync()                      │    │
│         ◄─────────────────┤  - CloseTaskAsync()                       │    │
│         │                 │  - UpdateTaskAsync()                      │    │
│         │                 │  - CancelTaskAsync()                      │    │
│         │                 └────────────────┬──────────────────────────┘    │
│         │                                  │                                │
│         │                                  │ HTTP/REST/DB                   │
│         │                                  │                                │
└─────────┼──────────────────────────────────┼────────────────────────────────┘
          │                                  │
          │                                  ▼
┌─────────▼──────────────────────────────────────────────────────────────────┐
│                   YOUR EXISTING TASK MANAGEMENT SYSTEM                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  - User/Role Management                                                     │
│  - Task Assignment Logic                                                    │
│  - Task Lifecycle                                                           │
│  - Notifications                                                            │
│  - Audit Logging                                                            │
│  - Access Control                                                           │
│  - UI/Forms                                                                 │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Sequence Diagram

```
User          Your UI         Workflow Engine    YourTaskSystem    External System
 │                │                  │                  │                 │
 │  Start Flow    │                  │                  │                 │
 ├───────────────►│ StartWorkflow()  │                  │                 │
 │                ├─────────────────►│                  │                 │
 │                │                  │ Execute Steps    │                 │
 │                │                  ├──────────┐       │                 │
 │                │                  │          │       │                 │
 │                │                  │◄─────────┘       │                 │
 │                │                  │                  │                 │
 │                │                  │ Reach Interaction│                 │
 │                │                  │   Step           │                 │
 │                │                  ├─────────────────►│ CreateTask()    │
 │                │                  │                  ├────────────────►│
 │                │                  │                  │   Task Created  │
 │                │                  │   externalTaskId │◄────────────────┤
 │                │                  │◄─────────────────┤                 │
 │                │                  │                  │                 │
 │                │                  │ Status: Waiting  │                 │
 │                │                  │                  │                 │
 │                │◄─ ─ ─ ─ ─ ─ ─ ─ ─│                  │                 │
 │   Task appears │                  │                  │                 │
 │◄───────────────┤                  │                  │                 │
 │                │                  │                  │                 │
 │                │                  │                  │                 │
 │  Complete Task │                  │                  │                 │
 ├───────────────►│ CompleteStep()   │                  │                 │
 │                ├─────────────────►│                  │                 │
 │                │                  ├─────────────────►│ CloseTask()     │
 │                │                  │                  ├────────────────►│
 │                │                  │                  │   Task Closed   │
 │                │                  │                  │◄────────────────┤
 │                │                  │◄─────────────────┤                 │
 │                │                  │                  │                 │
 │                │                  │ Continue Flow    │                 │
 │                │                  ├──────────┐       │                 │
 │                │                  │          │       │                 │
 │                │                  │◄─────────┘       │                 │
 │                │     Success      │                  │                 │
 │                │◄─────────────────┤                  │                 │
 │  Confirmation  │                  │                  │                 │
 │◄───────────────┤                  │                  │                 │
```

## Data Flow: Task Creation

```
InteractionStep Config (JSON)          External Task Created
┌──────────────────────────┐          ┌─────────────────────────┐
│ "assignedUsers": [...]   │          │ Task ID: TASK-12345     │
│ "assignedRoles": [...]   │─────────►│ Title: "Approve Request"│
│ "formSchema": "{...}"    │          │ Assigned: YOUR LOGIC    │
│ "timeoutMinutes": 2880   │          │ Due: calculated         │
└──────────────────────────┘          │ Form: from schema       │
                                       │ Context: workflow vars  │
        Workflow Variables             └─────────────────────────┘
┌──────────────────────────┐                      │
│ amount: 5000             │                      │
│ requestor: "john@..."    │──────────────────────┘
│ department: "IT"         │          Stored in your system
└──────────────────────────┘          with your assignment logic
```

## Data Flow: Task Completion

```
User completes task in YOUR UI
         │
         ▼
┌──────────────────────────┐
│ Task ID: TASK-12345      │
│ Form Data:               │
│   approved: true         │
│   comments: "Looks good" │
└──────────┬───────────────┘
           │
           ▼
  Map Task ID → Step Instance ID
           │
           ▼
┌──────────────────────────┐
│ CompleteInteractionStep()│
│   stepInstanceId         │
│   outputData             │
└──────────┬───────────────┘
           │
           ├─────────────────────┐
           │                     │
           ▼                     ▼
  Close External Task    Update Workflow Variables
           │                     │
           │                     ▼
           │              ┌──────────────────┐
           │              │ approved: true   │
           │              │ comments: "..."  │
           │              └──────────────────┘
           │                     │
           └─────────┬───────────┘
                     │
                     ▼
            Continue Workflow
                to Next Step
```

## Your Responsibilities vs Engine Responsibilities

### ✅ YOUR External Task System Handles:

1. **User Management**
   - Resolving user IDs
   - Role membership
   - Permissions

2. **Task Assignment**
   - Assignment algorithm
   - Round-robin/load balancing
   - Reassignment
   - Escalation

3. **Task UI**
   - Rendering forms
   - Validation
   - Mobile/desktop views

4. **Lifecycle**
   - Task states
   - Notifications
   - Reminders
   - SLA tracking

5. **Audit**
   - Who did what when
   - Change history
   - Compliance logs

### ✅ Workflow Engine Handles:

1. **Workflow Orchestration**
   - Step sequencing
   - Conditional routing
   - Variable management

2. **Integration Points**
   - Calling CreateTask at right time
   - Calling CloseTask after completion
   - Passing context/data

3. **Workflow State**
   - Current step tracking
   - Execution history
   - Error handling

4. **Business Logic**
   - Pre-task validation
   - Post-task processing
   - Decision routing

## Code Example: Complete Integration

```csharp
// 1. Your task system implementation
public class AcmeTaskSystem : IExternalTaskSystem
{
    public async Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
    {
        // Call YOUR existing API
        var response = await _http.PostAsJsonAsync(
            "https://tasks.acme.com/api/tasks",
            new {
                title = taskInfo.Title,
                assigned_users = taskInfo.AssignedUsers,
                assigned_roles = taskInfo.AssignedRoles,
                due_date = taskInfo.DueDate,
                workflow_id = taskInfo.WorkflowInstanceId,
                step_id = taskInfo.StepInstanceId
            }
        );
        
        var result = await response.Content.ReadFromJsonAsync<TaskResult>();
        return result.id;  // YOUR task ID
    }
    
    public async Task CloseTaskAsync(string taskId, Dictionary<string, object> data)
    {
        await _http.PutAsJsonAsync(
            $"https://tasks.acme.com/api/tasks/{taskId}/complete",
            data
        );
    }
}

// 2. Register it
services.AddSingleton<IExternalTaskSystem, AcmeTaskSystem>();

// 3. Your UI task completion
[HttpPost("api/tasks/{taskId}/submit")]
public async Task<IActionResult> SubmitTask(string taskId, TaskData data)
{
    // Look up which workflow step this task belongs to
    var stepId = await _db.GetStepIdForTask(taskId);
    
    // Complete the workflow step
    await _workflowEngine.CompleteInteractionStepAsync(
        stepInstanceId: stepId,
        outputData: data.FormValues
    );
    
    // The engine automatically:
    // - Closes the task in YOUR system
    // - Continues the workflow
    
    return Ok();
}
```

## Benefits of This Design

✅ **Separation of Concerns**
- Task management stays in YOUR system
- Workflow orchestration in the engine
- Clear boundaries

✅ **Minimal Changes**
- Keep your existing task UI
- Keep your existing assignment logic
- Just add workflow completion call

✅ **Flexible**
- Works with ANY task system
- REST API, database, message queue
- Even multiple task systems

✅ **Resilient**
- Works even if external system is down
- Graceful error handling
- No blocking

✅ **Transparent**
- Automatic task creation/closure
- You just implement the interface
- Engine handles the rest

## Summary

You implement `IExternalTaskSystem` once, and the engine automatically:
1. Creates tasks when workflows reach InteractionSteps
2. Closes tasks when users complete them
3. Passes all necessary context
4. Handles errors gracefully

Your existing task system continues to handle:
- Assignment logic
- UI rendering
- Notifications
- User management
- All business logic

It's a clean separation that lets each system do what it does best! 🎯
