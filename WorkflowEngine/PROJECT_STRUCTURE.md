# WorkflowEngine Project Structure

```
WorkflowEngine/
│
├── WorkflowEngine.sln                          # Visual Studio solution file
├── README.md                                   # Main documentation
├── IMPLEMENTATION_GUIDE.md                     # Detailed implementation guide
├── .gitignore                                  # Git ignore rules
├── build-nuget.sh                             # Linux/Mac NuGet build script
├── build-nuget.bat                            # Windows NuGet build script
│
├── WorkflowEngine.Core/                       # Main NuGet package
│   ├── WorkflowEngine.Core.csproj            # Project file
│   │
│   ├── Models/                                # Data models
│   │   ├── WorkflowDefinition.cs             # Workflow template model
│   │   ├── WorkflowInstance.cs               # Runtime instance model
│   │   └── StepTypes.cs                      # Step type definitions and configs
│   │
│   ├── Repositories/                          # Data access layer
│   │   ├── IRepositories.cs                  # Repository interfaces
│   │   └── Oracle/                           # Oracle implementations
│   │       ├── OracleWorkflowDefinitionRepository.cs
│   │       ├── OracleWorkflowInstanceRepository.cs
│   │       └── OracleStepInstanceRepository.cs
│   │
│   └── Services/                              # Business logic
│       ├── IWorkflowServices.cs              # Service interfaces
│       ├── WorkflowEngine.cs                 # Main workflow engine
│       ├── ExpressionEvaluator.cs            # Expression evaluation
│       ├── WorkflowDefinitionLoader.cs       # JSON loader
│       └── Executors/                        # Step executors
│           └── StepExecutors.cs              # All step executor implementations
│
├── WorkflowEngine.Example/                    # Example console application
│   ├── WorkflowEngine.Example.csproj         # Project file
│   └── Program.cs                            # Example usage code
│
├── Database/                                  # Database scripts
│   └── oracle_schema.sql                     # Oracle table creation script
│
└── Examples/                                  # Example workflows and code
    ├── employee-onboarding.json              # Example workflow definition
    ├── invoice-approval-workflow.json        # Complex workflow example
    └── CustomRepositoryExample.cs            # Custom repository example

```

## Key Components

### Core Library (WorkflowEngine.Core)

**Models:**
- `WorkflowDefinition` - Template for a workflow process
- `WorkflowInstance` - Running instance of a workflow
- `StepInstance` - Individual step execution record
- `StepTypes` - Constants and configurations for step types

**Repositories:**
- `IWorkflowDefinitionRepository` - CRUD for workflow templates
- `IWorkflowInstanceRepository` - CRUD for workflow instances
- `IStepInstanceRepository` - CRUD for step instances
- Oracle implementations with customizable table names

**Services:**
- `IWorkflowEngine` - Main orchestration service
- `IStepExecutor` - Interface for step execution
- `IActivityServiceRegistry` - Registry for business services
- `IExpressionEvaluator` - Expression evaluation
- `WorkflowDefinitionLoader` - JSON definition loader

**Step Executors:**
- `InteractionStepExecutor` - Handles user input steps
- `ScheduledStepExecutor` - Handles time-based waits
- `ActivityStepExecutor` - Executes business logic
- `GatewayStepExecutor` - Handles routing decisions

### Database Schema

**Tables:**
- `WF_DEFINITIONS` - Workflow templates
- `WF_INSTANCES` - Running workflows
- `WF_STEP_INSTANCES` - Step execution records
- `WF_STEP_HISTORY` - Audit trail (optional)

**Features:**
- Customizable table names
- Comprehensive indexes
- Support for Oracle-specific features
- Built-in cleanup procedures

### Example Application

Demonstrates:
- Service registration
- Workflow definition loading
- Workflow execution
- Interaction step completion
- Status monitoring

## Step Types (Current Names)

| Step Type | Purpose | JSON Type |
|-----------|---------|-----------|
| InteractionStep | Wait for user/external input | `"interaction"` |
| ScheduledStep | Wait until specific date/time | `"scheduled"` |
| **BusinessStep** | Execute business logic | `"business"` |
| **DecisionStep** | Conditional/service routing | `"decision"` |
| **SubWorkflowStep** | Execute nested workflow | `"subworkflow"` |

## Getting Started

1. **Install Dependencies:**
   ```bash
   dotnet restore
   ```

2. **Create Database:**
   ```sql
   -- Run Database/oracle_schema.sql
   ```

3. **Build NuGet Package:**
   ```bash
   ./build-nuget.sh  # or build-nuget.bat on Windows
   ```

4. **Run Example:**
   ```bash
   cd WorkflowEngine.Example
   dotnet run
   ```

## Usage Pattern

```csharp
// 1. Setup DI
services.AddWorkflowEngine(config);

// 2. Register business services
registry.RegisterService("EmailService", new EmailService());

// 3. Load workflow definition
var definition = await loader.LoadFromFileAsync("workflow.json");
await definitionRepo.CreateAsync(definition);

// 4. Start workflow
var instanceId = await engine.StartWorkflowAsync(definitionId, variables);

// 5. Complete interaction steps
await engine.CompleteInteractionStepAsync(stepId, outputData);

// 6. Process scheduled steps (background service)
await engine.ProcessScheduledStepsAsync();
```

## Customization Points

1. **Custom Table Names:** Override repository `TableName` property
2. **Custom Step Types:** Implement `IStepExecutor` interface
3. **Custom Expressions:** Extend `ExpressionEvaluator` class
4. **Audit Logging:** Override repository methods
5. **Business Services:** Register any service in `ActivityServiceRegistry`

## License

MIT License - See LICENSE file for details

## Contributing

Contributions welcome! Please submit issues and PRs to the repository.
