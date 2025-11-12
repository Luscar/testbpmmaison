# Workflow Step Navigation: NextStepId vs Transitions

## Overview

The workflow engine supports two ways to define the flow between steps:

1. **`nextStepId`** - Simple, direct navigation (RECOMMENDED for linear flows)
2. **`transitions`** - Complex, conditional routing (REQUIRED for Gateway steps)

## When to Use What

### Use `nextStepId` for:
✅ Linear workflows (step A → step B → step C)  
✅ Interaction steps with one possible path  
✅ Activity steps that always proceed to the same next step  
✅ Scheduled steps with deterministic flow  
✅ **Cleaner, more readable JSON**  

### Use `transitions` for:
✅ Gateway steps (REQUIRED)  
✅ Conditional routing based on workflow variables  
✅ Multiple possible paths from one step  
✅ Loops and complex flow patterns  

## Examples

### ❌ Old Way (Verbose - still works but unnecessary)

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
      "targetStepId": "setup-account",
      "label": "Next"
    }
  ]
}
```

### ✅ New Way (Clean and Simple)

```json
{
  "id": "send-email",
  "name": "Send Welcome Email",
  "type": "activity",
  "nextStepId": "setup-account",
  "configuration": {
    "serviceName": "EmailService",
    "methodName": "SendEmail"
  }
}
```

## Complete Example Comparison

### Verbose Workflow (Old Style)

```json
{
  "id": "approval-v1",
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
      "type": "gateway",
      "transitions": [
        {"id": "approve", "targetStepId": "step4", "condition": "approved == true"},
        {"id": "reject", "targetStepId": "step5", "condition": "approved == false"}
      ]
    },
    {
      "id": "step4",
      "type": "activity",
      "transitions": []
    },
    {
      "id": "step5",
      "type": "activity",
      "transitions": []
    }
  ]
}
```

### Simplified Workflow (New Style)

```json
{
  "id": "approval-v2",
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
      "type": "gateway",
      "transitions": [
        {"id": "approve", "targetStepId": "step4", "condition": "approved == true"},
        {"id": "reject", "targetStepId": "step5", "condition": "approved == false"}
      ]
    },
    {
      "id": "step4",
      "type": "activity"
    },
    {
      "id": "step5",
      "type": "activity"
    }
  ]
}
```

**Result:** Much cleaner! 60% less boilerplate for linear steps.

## Step-by-Step Patterns

### Pattern 1: Simple Linear Flow

```json
{
  "steps": [
    {
      "id": "collect-data",
      "type": "interaction",
      "nextStepId": "validate-data"
    },
    {
      "id": "validate-data",
      "type": "activity",
      "nextStepId": "save-data"
    },
    {
      "id": "save-data",
      "type": "activity"
    }
  ]
}
```

### Pattern 2: Conditional Branch (Gateway Required)

```json
{
  "steps": [
    {
      "id": "get-amount",
      "type": "interaction",
      "nextStepId": "check-amount"
    },
    {
      "id": "check-amount",
      "type": "gateway",
      "transitions": [
        {
          "id": "low",
          "targetStepId": "auto-approve",
          "condition": "amount < 1000"
        },
        {
          "id": "high",
          "targetStepId": "manual-approval",
          "condition": "amount >= 1000"
        }
      ]
    },
    {
      "id": "auto-approve",
      "type": "activity",
      "nextStepId": "notify-user"
    },
    {
      "id": "manual-approval",
      "type": "interaction",
      "nextStepId": "notify-user"
    },
    {
      "id": "notify-user",
      "type": "activity"
    }
  ]
}
```

### Pattern 3: Terminal Steps (No Next Step)

```json
{
  "id": "final-step",
  "type": "activity",
  "configuration": {
    "serviceName": "EmailService",
    "methodName": "SendCompletionEmail"
  }
  // No nextStepId or transitions - workflow ends here
}
```

## Rules and Validation

### ✅ Valid Configurations

```json
// 1. Using nextStepId
{"id": "step1", "type": "activity", "nextStepId": "step2"}

// 2. Using transitions
{"id": "step1", "type": "gateway", "transitions": [...]}

// 3. Terminal step (no navigation)
{"id": "step1", "type": "activity"}

// 4. Single transition (works like nextStepId)
{"id": "step1", "type": "activity", "transitions": [{"targetStepId": "step2"}]}
```

### ❌ Invalid Configurations

```json
// Cannot use both nextStepId AND transitions
{
  "id": "step1",
  "type": "activity",
  "nextStepId": "step2",
  "transitions": [{"targetStepId": "step3"}]  // ERROR!
}

// Gateway steps must have transitions
{
  "id": "step1",
  "type": "gateway",
  "nextStepId": "step2"  // ERROR! Use transitions for gateways
}

// Gateway steps must have at least one transition
{
  "id": "step1",
  "type": "gateway",
  "transitions": []  // ERROR!
}
```

## Migration Guide

If you have existing workflows using verbose transition syntax, you can simplify them:

### Before:
```json
{
  "id": "my-step",
  "type": "activity",
  "transitions": [
    {
      "id": "to-next",
      "targetStepId": "next-step",
      "label": "Next"
    }
  ]
}
```

### After:
```json
{
  "id": "my-step",
  "type": "activity",
  "nextStepId": "next-step"
}
```

**Note:** Both formats work! The engine automatically handles both styles.

## Best Practices

1. **Use `nextStepId` by default** - Only use transitions when you need conditional logic
2. **Gateway steps always use transitions** - This is enforced by validation
3. **Be consistent** - Pick one style for linear flows and stick with it in your workflow
4. **Document your gateways** - Add clear labels to transition conditions
5. **Keep it simple** - The less complex your workflow definition, the easier to maintain

## Internal Behavior

When you use `nextStepId`, the engine:
1. Checks if `nextStepId` is specified
2. If yes, navigates directly to that step
3. If no, checks for transitions
4. If neither exists, completes the workflow

This means:
- `nextStepId` is just syntactic sugar for workflows
- Both approaches work identically at runtime
- Choose based on readability and your use case

## Summary

| Feature | nextStepId | Transitions |
|---------|-----------|-------------|
| **Syntax** | Simple | More verbose |
| **Use Case** | Linear flows | Conditional routing |
| **Required For** | Optional | Gateway steps |
| **Can Coexist** | No | No |
| **Cleaner JSON** | ✅ Yes | For complex routing |
| **Easier to Read** | ✅ Yes | For branching logic |

**Recommendation:** Use `nextStepId` for all non-gateway steps, and `transitions` only for gateway steps or complex conditional logic.
