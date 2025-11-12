# InteractionStep Assignment Options

## Do You Need assignedUsers and assignedRoles?

**Short answer: No, they're completely optional!**

Since your external task system already handles all assignment logic, you have three options:

## Option 1: No Assignment Fields (Recommended) ✅

**Use this when:** Your task system determines assignment based on its own logic (task type, workflow context, business rules, etc.)

**Workflow JSON:**
```json
{
  "id": "approval-step",
  "type": "interaction",
  "nextStepId": "next-step",
  "configuration": {
    "timeoutMinutes": 2880,
    "customData": {
      "taskType": "approval",
      "category": "purchase"
    }
  }
}
```

**Your Task System:**
```csharp
public async Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
{
    // YOU determine assignment based on your own logic
    var assignedUser = await DetermineAssignee(
        taskInfo.WorkflowContext["department"],
        taskInfo.Metadata["category"],
        taskInfo.WorkflowContext["amount"]
    );
    
    // Create task in YOUR system with YOUR assignment
    return await _api.CreateTask(new {
        title = taskInfo.Title,
        assignedTo = assignedUser,  // YOUR logic
        // ... other fields
    });
}
```

**Benefits:**
- ✅ Clean workflow definitions
- ✅ All assignment logic in one place (your system)
- ✅ Easy to change assignment rules without updating workflows
- ✅ No redundant data

---

## Option 2: Pass Hints/Suggestions to Your System

**Use this when:** You want to provide suggestions from the workflow, but your system makes the final decision

**Workflow JSON:**
```json
{
  "id": "approval-step",
  "type": "interaction",
  "configuration": {
    "assignedRoles": ["Manager"],
    "customData": {
      "suggestedUsers": ["john@company.com"],
      "minApprovers": 2
    }
  }
}
```

**Your Task System:**
```csharp
public async Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
{
    // Use the hints if provided, but apply YOUR logic
    var suggestedRoles = taskInfo.AssignedRoles ?? new List<string>();
    
    // YOUR system determines actual assignment
    var assignedUsers = await _assignmentService.GetUsersForRoles(
        suggestedRoles,
        taskInfo.WorkflowContext,
        businessRules: true  // Apply your business rules
    );
    
    return await _api.CreateTask(new {
        assignedTo = assignedUsers,  // Based on YOUR logic
        // ...
    });
}
```

**Benefits:**
- ✅ Workflow provides hints
- ✅ Your system still controls actual assignment
- ✅ Flexible - use hints or ignore them

---

## Option 3: Use Custom Data Fields

**Use this when:** You need to pass specific assignment criteria that your system understands

**Workflow JSON:**
```json
{
  "id": "review-step",
  "type": "interaction",
  "configuration": {
    "customData": {
      "assignmentRule": "round-robin",
      "team": "finance",
      "skillRequired": "budget-approval",
      "maxWorkload": 10
    }
  }
}
```

**Your Task System:**
```csharp
public async Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
{
    var customData = taskInfo.Metadata;
    
    // Use YOUR system's assignment engine
    var assignedUser = await _assignmentEngine.AssignTask(
        team: customData["team"],
        skill: customData["skillRequired"],
        rule: customData["assignmentRule"],
        maxWorkload: customData["maxWorkload"]
    );
    
    return await _api.CreateTask(new {
        assignedTo = assignedUser,
        // ...
    });
}
```

**Benefits:**
- ✅ Workflow specifies assignment criteria
- ✅ Your system interprets and applies them
- ✅ Very flexible
- ✅ Can include complex rules

---

## Comparison

| Approach | Workflow Complexity | Assignment Logic | Best For |
|----------|-------------------|------------------|----------|
| **No assignment fields** | Simplest ✅ | 100% in your system | Most cases |
| **Hints/suggestions** | Medium | Mostly in your system | When workflow has preferences |
| **Custom data** | Most flexible | Interpreted by your system | Complex assignment rules |

---

## Recommended Pattern

**For most cases, use Option 1:**

```json
{
  "id": "task-step",
  "type": "interaction",
  "nextStepId": "next",
  "configuration": {
    "timeoutMinutes": 2880
  }
}
```

Let your external task system handle ALL assignment logic based on:
- Current workflow variables (passed in `WorkflowContext`)
- Task metadata (workflow ID, step name, etc.)
- Your business rules
- User availability, workload, skills, etc.

**Why this is best:**
- ✅ Workflows stay simple and readable
- ✅ One place for assignment logic (easier to maintain)
- ✅ Can change assignment rules without updating workflows
- ✅ Your system already knows how to assign tasks
- ✅ Less duplication

---

## Example: Your Task System Determines Everything

**Workflow:**
```json
{
  "steps": [
    {
      "id": "approval",
      "type": "interaction",
      "nextStepId": "process"
    }
  ]
}
```

**Your Task System:**
```csharp
public async Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
{
    var workflowVars = taskInfo.WorkflowContext;
    
    // Extract context from workflow
    var amount = (decimal)workflowVars["amount"];
    var department = workflowVars["department"]?.ToString();
    var requestor = workflowVars["requestorEmail"]?.ToString();
    
    // Apply YOUR assignment logic
    string assignedUser;
    if (amount > 10000)
    {
        assignedUser = await _assignmentService.GetExecutiveApprover(department);
    }
    else
    {
        assignedUser = await _assignmentService.GetManagerApprover(department, requestor);
    }
    
    // Create task in YOUR system
    return await _taskApi.CreateTask(new {
        title = taskInfo.Title,
        assignedTo = assignedUser,
        dueDate = taskInfo.DueDate,
        context = workflowVars
    });
}
```

**Result:**
- Clean workflow definition
- All assignment logic in your task system
- Easy to test and modify
- Reusable across workflows

---

## What About FormSchema?

Also optional! If your task system already knows what form to show based on task type, you don't need it:

**Minimal Configuration:**
```json
{
  "id": "approval",
  "type": "interaction",
  "nextStepId": "next",
  "configuration": {
    "customData": {
      "formType": "purchase-approval"  // Your system knows this form
    }
  }
}
```

**Your Task System:**
```csharp
var formType = taskInfo.Metadata["formType"];
var formDefinition = await _formService.GetFormByType(formType);
// Show the form in your UI
```

---

## Summary

**You don't need assignedUsers or assignedRoles if:**
- ✅ Your task system handles assignment
- ✅ You want simple workflow definitions
- ✅ Assignment logic changes frequently
- ✅ You have complex assignment rules

**Consider using them only if:**
- ⚠️ You want to provide hints/suggestions
- ⚠️ Workflow explicitly specifies requirements
- ⚠️ You're migrating from another system

**For most cases:** Keep it simple! Just use `customData` for any task-specific info your system needs.

---

## Updated Example

**Simple and Clean:**
```json
{
  "id": "approval-workflow",
  "steps": [
    {
      "id": "submit",
      "type": "business",
      "nextStepId": "approve"
    },
    {
      "id": "approve",
      "type": "interaction",
      "nextStepId": "process",
      "configuration": {
        "timeoutMinutes": 2880
      }
    },
    {
      "id": "process",
      "type": "business"
    }
  ]
}
```

**Your task system handles everything else!** 🎉

See `Examples/purchase-approval-no-assignment.json` for a complete example.
