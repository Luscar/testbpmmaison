# 🚀 WorkflowEngine.Core - Complete BPM Solution for C#

## 🚀 What's New in v2.0

### Major Improvements
✅ **Service-Based Decisions** - Decision steps can now query services/databases for routing  
✅ **Sub-Workflows** - Reuse workflows inside other workflows  
✅ **Workflow Visualizer** - Generate diagrams (HTML, Mermaid, Text)  
✅ **External Task Integration** - Seamless integration with your existing task system  
✅ **Simplified Syntax** - Routes integrated into DecisionStep configuration  
✅ **Better Names** - `BusinessStep` and `DecisionStep` (more intuitive)  

See **MAJOR_UPDATE.md** and **EXTERNAL_TASK_INTEGRATION.md** for complete details.

## 📦 What's Included

A complete, production-ready Business Process Management (BPM) engine with:

### Core Features
✅ **4 Powerful Step Types:**
- **InteractionStep** - Wait for user input (formerly UserStep)
- **ScheduledStep** - Wait until specific date/time (formerly DelayStep)  
- **ActivityStep** - Execute business logic (formerly BusinessStep)
- **GatewayStep** - Conditional routing (formerly DecisionStep)

✅ **Oracle Database Support** with customizable table names  
✅ **JSON Workflow Definitions** - Easy to create and version  
✅ **Expression Engine** - Dynamic condition evaluation  
✅ **Retry Logic** - Automatic retry for failed operations  
✅ **Correlation IDs** - Track related workflows  
✅ **Extensible Architecture** - Add custom step types  

### Package Contents

```
WorkflowEngine/
├── 📄 START_HERE.md                    ← YOU ARE HERE
├── 📄 QUICK_START.md                   ← 5-minute setup guide
├── 📄 README.md                        ← Complete documentation
├── 📄 IMPLEMENTATION_GUIDE.md          ← Best practices & advanced topics
├── 📄 PROJECT_STRUCTURE.md             ← Architecture overview
│
├── 📁 WorkflowEngine.Core/             ← Main NuGet library
│   ├── Models/                         ← Data models
│   ├── Repositories/                   ← Data access (Oracle)
│   └── Services/                       ← Business logic & executors
│
├── 📁 WorkflowEngine.Example/          ← Working example app
│
├── 📁 Database/                        ← SQL schema scripts
│   └── oracle_schema.sql               ← Create tables
│
├── 📁 Examples/                        ← Sample workflows
│   ├── employee-onboarding.json        ← Simple workflow
│   ├── invoice-approval-workflow.json  ← Complex workflow
│   └── CustomRepositoryExample.cs      ← Custom table names
│
└── 🔧 Build Scripts
    ├── build-nuget.sh                  ← Linux/Mac build
    └── build-nuget.bat                 ← Windows build
```

## 🎯 Where to Start

### New to Workflow Engines?
1. Read **QUICK_START.md** (5 minutes)
2. Run the **WorkflowEngine.Example** project
3. Check out the **employee-onboarding.json** example

### Ready to Implement?
1. Read **README.md** for complete API documentation
2. Follow **IMPLEMENTATION_GUIDE.md** for best practices
3. Customize repositories for your table names (see Examples/)

### Want to Build the NuGet Package?
```bash
# Linux/Mac
chmod +x build-nuget.sh
./build-nuget.sh

# Windows
build-nuget.bat
```

## ⚡ Quick Example

### 1. Define a Workflow (JSON) - Now with Simplified Syntax!
```json
{
  "id": "approval-v1",
  "name": "Approval Process",
  "initialStepId": "request",
  "steps": [
    {
      "id": "request",
      "type": "interaction",
      "nextStepId": "decision",  // ← Simple! No verbose transitions needed
      "configuration": {
        "assignedRoles": ["Manager"]
      }
    },
    {
      "id": "decision",
      "type": "gateway",
      "transitions": [  // ← Only gateways need transitions
        {
          "targetStepId": "approved",
          "condition": "approved == true"
        }
      ]
    },
    {
      "id": "approved",
      "type": "activity",
      "nextStepId": "notify"
    },
    {
      "id": "notify",
      "type": "activity"  // ← No next step = workflow ends
    }
  ]
}
```

**NEW:** Use simple `nextStepId` for linear flows instead of verbose `transitions`! See **NEXTSTEP_VS_TRANSITIONS.md** for details.

### 2. Start the Workflow (C#)
```csharp
var instanceId = await engine.StartWorkflowAsync(
    "approval-v1",
    new Dictionary<string, object> { ["amount"] = 1000 }
);
```

### 3. Complete a Step
```csharp
await engine.CompleteInteractionStepAsync(
    stepInstanceId,
    new Dictionary<string, object> { ["approved"] = true }
);
```

## 📚 Documentation Guide

| Document | Purpose | Read Time |
|----------|---------|-----------|
| **MAJOR_UPDATE.md** | What's new in v2.0 | 10 min |
| **QUICK_START.md** | Get running in 5 minutes | 5 min |
| **README.md** | Complete feature documentation | 15 min |
| **NEXTSTEP_VS_TRANSITIONS.md** | Simplified vs verbose syntax | 5 min |
| **IMPLEMENTATION_GUIDE.md** | Best practices & advanced topics | 30 min |
| **PROJECT_STRUCTURE.md** | Architecture & design decisions | 10 min |

## 🛠️ Visualization Tool

Generate beautiful diagrams of your workflows:

```bash
cd WorkflowEngine.VisualizerTool
dotnet run -- ../Examples/order-fulfillment-v2.json html
```

**Formats:**
- `html` - Interactive diagram with step details
- `mermaid` - For GitHub/VS Code/documentation  
- `text` - ASCII tree view for quick validation

## 📚 Documentation Guide

## 🔧 Technology Stack

- **Language:** C# (.NET 8.0)
- **Database:** Oracle (customizable)
- **Dependencies:**
  - Oracle.ManagedDataAccess.Core
  - System.Linq.Dynamic.Core
  - Microsoft.Extensions.DependencyInjection

## 🎓 Step Types Explained

| Type | Use Case | Example |
|------|----------|---------|
| **InteractionStep** | Manual approval, form input | Manager approval |
| **ScheduledStep** | Wait until date | Wait for start date |
| **BusinessStep** | Business logic, API calls | Send email, process payment |
| **DecisionStep** | Conditional/service-based routing | Route by amount or query DB |
| **SubWorkflowStep** | Execute nested workflow | Reusable approval process |

## 💡 Key Concepts

### Workflow Definition
- Template for a process
- Defined in JSON
- Version controlled
- Stored in database

### Workflow Instance
- Running execution of a workflow
- Has unique ID and correlation ID
- Tracks variables and state
- Maintains step history

### Step Instance
- Single step execution
- Records input/output
- Tracks status and timing
- Can be retried

## 🚀 Common Use Cases

- **Employee Onboarding** - Multi-step approval with scheduled tasks
- **Invoice Approval** - Multi-level approval with amount thresholds
- **Order Processing** - Payment, inventory, and shipping coordination
- **Document Review** - Collaborative review with escalation
- **IT Provisioning** - Automated account and access setup

## 🔒 Production Ready Features

✅ Transaction support  
✅ Error handling & retry logic  
✅ Audit trail (step history)  
✅ Correlation tracking  
✅ Timeout handling  
✅ Role-based access  
✅ Custom table names  
✅ Expression validation  

## 📞 Next Steps

1. **Read QUICK_START.md** - Get running in 5 minutes
2. **Explore Examples/** - See real workflow definitions
3. **Run WorkflowEngine.Example** - See it in action
4. **Read IMPLEMENTATION_GUIDE.md** - Learn best practices
5. **Build your first workflow!** 🎉

## 📖 Additional Resources

- **Database Schema:** See `Database/oracle_schema.sql`
- **Example Code:** See `WorkflowEngine.Example/Program.cs`
- **Custom Repositories:** See `Examples/CustomRepositoryExample.cs`
- **Sample Workflows:** See `Examples/*.json`

---

**Ready to get started?** → Open **QUICK_START.md**

**Need help?** → Check **README.md** or **IMPLEMENTATION_GUIDE.md**

**Want examples?** → Look in the **Examples/** folder

Happy workflow building! 🎉
