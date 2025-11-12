# Simplified Workflow Syntax - Using `nextStepId`

## Overview

For **linear workflows** where most steps flow sequentially (except Gateway/Decision steps), you can use the simplified `nextStepId` property instead of defining full `transitions` arrays.

## When to Use Each Approach

### Use `nextStepId` (Simplified) When:
- ✅ Step has **only one possible next step**
- ✅ Step is **Interaction, Scheduled, or Activity type**
- ✅ No conditional routing needed
- ✅ You want cleaner, more readable JSON

### Use `transitions` (Full) When:
- ✅ Step is a **Gateway** (multiple paths based on conditions)
- ✅ Multiple possible next steps exist
- ✅ Conditional routing is required
- ✅ You need custom transition labels

## Examples

### Before (Using Transitions)

```json
{
  "id": "send-email",
  "name": "Send Welcome Email",
  "type": "activity",
  "configuration": {
    "serviceName": "EmailService",
    "methodName": "SendEmail"
  },
  "transitions": [
    {
      "id": "to-next-step",
      "targetStepId": "next-step",
      "label": "Email Sent"
    }
  ]
}
```

### After (Using nextStepId)

```json
{
  "id": "send-email",
  "name": "Send Welcome Email",
  "type": "activity",
  "configuration": {
    "serviceName": "EmailService",
    "methodName": "SendEmail"
  },
  "nextStepId": "next-step"
}
```

**Result:** 58% less code! Much cleaner and easier to read.

## Complete Example: Linear Workflow

```json
{
  "id": "simple-process-v1",
  "name": "Simple Linear Process",
  "initialStepId": "step1",
  "steps": [
    {
      "id": "step1",
      "name": "Get User Input",
      "type": "interaction",
      "configuration": {
        "assignedRoles": ["User"]
      },
      "nextStepId": "step2"
    },
    {
      "id": "step2",
      "name": "Process Data",
      "type": "activity",
      "configuration": {
        "serviceName": "DataService",
        "methodName": "Process"
      },
      "nextStepId": "step3"
    },
    {
      "id": "step3",
      "name": "Send Notification",
      "type": "activity",
      "configuration": {
        "serviceName": "EmailService",
        "methodName": "Notify"
      }
    }
  ]
}
```

## Gateway Steps (Always Use Transitions)

Gateway steps **must use transitions** because they have multiple paths:

```json
{
  "id": "approval-decision",
  "name": "Check Approval",
  "type": "gateway",
  "configuration": {
    "gatewayType": "exclusive"
  },
  "transitions": [
    {
      "id": "approved",
      "targetStepId": "process-approval",
      "condition": "approved == true"
    },
    {
      "id": "rejected",
      "targetStepId": "send-rejection",
      "condition": "approved == false"
    }
  ]
}
```

## Mixed Example: Workflow with Both

```json
{
  "id": "mixed-workflow-v1",
  "name": "Workflow with Linear and Branching Sections",
  "initialStepId": "collect-data",
  "steps": [
    {
      "id": "collect-data",
      "name": "Collect Data",
      "type": "interaction",
      "configuration": {
        "assignedRoles": ["User"]
      },
      "nextStepId": "validate-data"
    },
    {
      "id": "validate-data",
      "name": "Validate Data",
      "type": "activity",
      "configuration": {
        "serviceName": "ValidationService",
        "methodName": "Validate"
      },
      "nextStepId": "check-amount"
    },
    {
      "id": "check-amount",
      "name": "Route Based on Amount",
      "type": "gateway",
      "configuration": {
        "gatewayType": "exclusive"
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
    },
    {
      "id": "executive-approval",
      "name": "Executive Approval Required",
      "type": "interaction",
      "configuration": {
        "assignedRoles": ["Executive"]
      },
      "nextStepId": "finalize"
    },
    {
      "id": "manager-approval",
      "name": "Manager Approval Required",
      "type": "interaction",
      "configuration": {
        "assignedRoles": ["Manager"]
      },
      "nextStepId": "finalize"
    },
    {
      "id": "finalize",
      "name": "Finalize Process",
      "type": "activity",
      "configuration": {
        "serviceName": "FinalizationService",
        "methodName": "Finalize"
      }
    }
  ]
}
```

## Rules and Behavior

### Automatic Conversion
When you use `nextStepId`, the engine **automatically creates** a transition:
```json
{
  "nextStepId": "next-step"
}
```
**Becomes:**
```json
{
  "transitions": [
    {
      "id": "step-id-to-next-step",
      "targetStepId": "next-step",
      "label": "Next"
    }
  ]
}
```

### Precedence Rules
If both `nextStepId` AND `transitions` are specified:
- ✅ `transitions` takes precedence
- ⚠️ `nextStepId` is **ignored**
- 💡 This allows you to override the default behavior

### End Steps
For the final step in a workflow, simply **omit both**:
```json
{
  "id": "final-step",
  "name": "Complete Process",
  "type": "activity",
  "configuration": { ... }
  // No nextStepId or transitions = workflow ends here
}
```

## Validation

The workflow loader validates:
- ✅ `nextStepId` points to an existing step
- ✅ All transition targets exist
- ✅ Gateway steps have valid transitions
- ✅ No circular references (in complex scenarios)

## Benefits

### Readability
```json
// Clear, concise, easy to understand
"nextStepId": "next-step"

// vs verbose transitions for simple flows
"transitions": [
  {
    "id": "transition-1",
    "targetStepId": "next-step",
    "label": "Continue"
  }
]
```

### Maintenance
- **Fewer lines** of JSON to maintain
- **Less error-prone** - no need to create unique transition IDs
- **Easier to visualize** the workflow flow

### Flexibility
- Use **simple syntax** for linear flows
- Use **full syntax** for complex routing
- **Mix both** in the same workflow as needed

## Real-World Example Comparison

### Original (All Transitions)
```json
{
  "steps": [
    {
      "id": "step1",
      "type": "interaction",
      "transitions": [{"id": "t1", "targetStepId": "step2"}]
    },
    {
      "id": "step2",
      "type": "activity",
      "transitions": [{"id": "t2", "targetStepId": "step3"}]
    },
    {
      "id": "step3",
      "type": "activity",
      "transitions": [{"id": "t3", "targetStepId": "step4"}]
    },
    {
      "id": "step4",
      "type": "interaction",
      "transitions": []
    }
  ]
}
```
**Total:** ~20 lines

### Simplified (Using nextStepId)
```json
{
  "steps": [
    {
      "id": "step1",
      "type": "interaction",
      "nextStepId": "step2"
    },
    {
      "id": "step2",
      "type": "activity",
      "nextStepId": "step3"
    },
    {
      "id": "step3",
      "type": "activity",
      "nextStepId": "step4"
    },
    {
      "id": "step4",
      "type": "interaction"
    }
  ]
}
```
**Total:** ~12 lines (40% reduction!)

## Migration Guide

### Converting Existing Workflows

**Step 1:** Identify linear flow steps
```json
// Steps with only one transition and no condition
{
  "transitions": [
    {
      "id": "any-id",
      "targetStepId": "next-step",
      "condition": null  // or omitted
    }
  ]
}
```

**Step 2:** Replace with nextStepId
```json
{
  "nextStepId": "next-step"
}
```

**Step 3:** Keep Gateway transitions unchanged
```json
// Always keep this format for gateways
{
  "type": "gateway",
  "transitions": [
    {"targetStepId": "path-a", "condition": "x > 10"},
    {"targetStepId": "path-b", "condition": "x <= 10"}
  ]
}
```

## Best Practices

1. **Use `nextStepId` by default** for Interaction, Scheduled, and Activity steps
2. **Always use `transitions`** for Gateway steps (required for conditions)
3. **Be consistent** within each workflow
4. **Document** any complex transition logic with comments (if your JSON parser supports them)
5. **Test** your workflows after conversion

## Summary

| Feature | nextStepId | transitions |
|---------|------------|-------------|
| **Simplicity** | ✅ Very simple | ⚠️ More verbose |
| **Linear flows** | ✅ Perfect | ⚠️ Overkill |
| **Conditional routing** | ❌ Not supported | ✅ Required |
| **Gateway steps** | ❌ Cannot use | ✅ Must use |
| **Multiple paths** | ❌ Single path only | ✅ Multiple paths |
| **Custom labels** | ❌ Auto-generated | ✅ Customizable |

**Recommendation:** Use `nextStepId` for 80% of your steps, `transitions` for Gateway steps and complex routing.
