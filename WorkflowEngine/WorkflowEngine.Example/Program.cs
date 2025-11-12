using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Repositories;
using WorkflowEngine.Core.Repositories.Oracle;
using WorkflowEngine.Core.Services;
using WorkflowEngine.Core.Services.Executors;

namespace WorkflowEngine.Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Workflow Engine Example ===\n");

            // Setup dependency injection
            var services = ConfigureServices();
            var serviceProvider = services.BuildServiceProvider();

            // Register business services
            RegisterBusinessServices(serviceProvider);

            // Example 1: Load and deploy a workflow definition
            await LoadWorkflowDefinitionExample(serviceProvider);

            // Example 2: Start a workflow
            var instanceId = await StartWorkflowExample(serviceProvider);

            // Example 3: Complete interaction steps
            await CompleteInteractionStepExample(serviceProvider, instanceId);

            // Example 4: Monitor workflow status
            await MonitorWorkflowExample(serviceProvider, instanceId);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static IServiceCollection ConfigureServices()
        {
            var services = new ServiceCollection();

            // Configure repository
            var repoConfig = new RepositoryConfiguration
            {
                ConnectionString = "User Id=your_user;Password=your_password;Data Source=your_oracle_db",
                Schema = "dbo",
                WorkflowDefinitionTable = "WF_DEFINITIONS",
                WorkflowInstanceTable = "WF_INSTANCES",
                StepInstanceTable = "WF_STEP_INSTANCES"
            };

            services.AddSingleton(repoConfig);
            services.AddScoped<IWorkflowDefinitionRepository, OracleWorkflowDefinitionRepository>();
            services.AddScoped<IWorkflowInstanceRepository, OracleWorkflowInstanceRepository>();
            services.AddScoped<IStepInstanceRepository, OracleStepInstanceRepository>();

            // Register services
            services.AddSingleton<IActivityServiceRegistry, ActivityServiceRegistry>();
            services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

            // Register step executors
            services.AddScoped<IStepExecutor, InteractionStepExecutor>();
            services.AddScoped<IStepExecutor, ScheduledStepExecutor>();
            services.AddScoped<IStepExecutor, ActivityStepExecutor>();
            services.AddScoped<IStepExecutor, GatewayStepExecutor>();

            // Register workflow engine
            services.AddScoped<IWorkflowEngine, WorkflowEngine.Core.Services.WorkflowEngine>();
            services.AddScoped<WorkflowDefinitionLoader>();

            return services;
        }

        static void RegisterBusinessServices(ServiceProvider serviceProvider)
        {
            var registry = serviceProvider.GetRequiredService<IActivityServiceRegistry>();

            // Register mock services (replace with real implementations)
            registry.RegisterService("EmailService", new MockEmailService());
            registry.RegisterService("EquipmentService", new MockEquipmentService());
            registry.RegisterService("ITService", new MockITService());

            Console.WriteLine("✓ Business services registered\n");
        }

        static async Task LoadWorkflowDefinitionExample(ServiceProvider serviceProvider)
        {
            Console.WriteLine("=== Loading Workflow Definition ===");

            var loader = serviceProvider.GetRequiredService<WorkflowDefinitionLoader>();
            
            // For this example, we'll create a simple workflow programmatically
            var definition = CreateSimpleWorkflowDefinition();

            // Validate
            loader.Validate(definition);
            Console.WriteLine("✓ Workflow definition validated");

            // Save to database
            var definitionRepo = serviceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
            await definitionRepo.CreateAsync(definition);
            Console.WriteLine($"✓ Workflow definition '{definition.Name}' deployed\n");
        }

        static async Task<string> StartWorkflowExample(ServiceProvider serviceProvider)
        {
            Console.WriteLine("=== Starting Workflow ===");

            var engine = serviceProvider.GetRequiredService<IWorkflowEngine>();

            var variables = new Dictionary<string, object>
            {
                ["employeeName"] = "Jane Smith",
                ["employeeEmail"] = "jane.smith@company.com",
                ["department"] = "Engineering",
                ["startDate"] = DateTime.UtcNow.AddDays(7)
            };

            var instanceId = await engine.StartWorkflowAsync(
                workflowDefinitionId: "simple-approval-v1",
                variables: variables,
                correlationId: "EMP-2024-001",
                createdBy: "hr.admin@company.com"
            );

            Console.WriteLine($"✓ Workflow started with ID: {instanceId}\n");
            return instanceId;
        }

        static async Task CompleteInteractionStepExample(ServiceProvider serviceProvider, string instanceId)
        {
            Console.WriteLine("=== Completing Interaction Step ===");

            var engine = serviceProvider.GetRequiredService<IWorkflowEngine>();

            // Simulate waiting for the workflow to reach an interaction step
            await Task.Delay(1000);

            var instance = await engine.GetWorkflowInstanceAsync(instanceId);
            var pendingStep = instance.StepHistory.Find(s => s.Status == StepStatus.WaitingForInput);

            if (pendingStep != null)
            {
                var outputData = new Dictionary<string, object>
                {
                    ["approved"] = true,
                    ["comments"] = "Approved by manager"
                };

                await engine.CompleteInteractionStepAsync(
                    stepInstanceId: pendingStep.Id,
                    outputData: outputData,
                    completedBy: "manager@company.com"
                );

                Console.WriteLine($"✓ Step '{pendingStep.StepName}' completed\n");
            }
            else
            {
                Console.WriteLine("⚠ No pending interaction steps found\n");
            }
        }

        static async Task MonitorWorkflowExample(ServiceProvider serviceProvider, string instanceId)
        {
            Console.WriteLine("=== Monitoring Workflow ===");

            var engine = serviceProvider.GetRequiredService<IWorkflowEngine>();
            var instance = await engine.GetWorkflowInstanceAsync(instanceId);

            Console.WriteLine($"Status: {instance.Status}");
            Console.WriteLine($"Current Step: {instance.CurrentStepId}");
            Console.WriteLine($"Started: {instance.StartedAt}");
            Console.WriteLine($"\nStep History:");

            foreach (var step in instance.StepHistory)
            {
                Console.WriteLine($"  • {step.StepName}");
                Console.WriteLine($"    Type: {step.StepType}");
                Console.WriteLine($"    Status: {step.Status}");
                Console.WriteLine($"    Started: {step.StartedAt}");
                if (step.CompletedAt.HasValue)
                    Console.WriteLine($"    Completed: {step.CompletedAt}");
                Console.WriteLine();
            }
        }

        static WorkflowDefinition CreateSimpleWorkflowDefinition()
        {
            return new WorkflowDefinition
            {
                Id = "simple-approval-v1",
                Name = "Simple Approval Process",
                Description = "A simple approval workflow for demonstration",
                Version = "1.0.0",
                InitialStepId = "request-approval",
                Variables = new Dictionary<string, object>
                {
                    ["approved"] = false,
                    ["comments"] = ""
                },
                Steps = new List<StepDefinition>
                {
                    new StepDefinition
                    {
                        Id = "request-approval",
                        Name = "Request Approval",
                        Type = StepTypes.InteractionStep,
                        Configuration = new Dictionary<string, object>
                        {
                            ["assignedRoles"] = new List<string> { "Manager" },
                            ["timeoutMinutes"] = 1440
                        },
                        Transitions = new List<Transition>
                        {
                            new Transition
                            {
                                Id = "to-decision",
                                TargetStepId = "approval-decision",
                                Label = "Submit"
                            }
                        }
                    },
                    new StepDefinition
                    {
                        Id = "approval-decision",
                        Name = "Check Approval",
                        Type = StepTypes.GatewayStep,
                        Configuration = new Dictionary<string, object>
                        {
                            ["gatewayType"] = "exclusive"
                        },
                        Transitions = new List<Transition>
                        {
                            new Transition
                            {
                                Id = "approved",
                                TargetStepId = "send-approval-email",
                                Condition = "approved == true",
                                Label = "Approved"
                            },
                            new Transition
                            {
                                Id = "rejected",
                                TargetStepId = "send-rejection-email",
                                Condition = "approved == false",
                                Label = "Rejected"
                            }
                        }
                    },
                    new StepDefinition
                    {
                        Id = "send-approval-email",
                        Name = "Send Approval Email",
                        Type = StepTypes.ActivityStep,
                        Configuration = new Dictionary<string, object>
                        {
                            ["serviceName"] = "EmailService",
                            ["methodName"] = "SendEmail",
                            ["inputMapping"] = new Dictionary<string, object>
                            {
                                ["to"] = "$employeeEmail",
                                ["subject"] = "Request Approved"
                            }
                        },
                        Transitions = new List<Transition>()
                    },
                    new StepDefinition
                    {
                        Id = "send-rejection-email",
                        Name = "Send Rejection Email",
                        Type = StepTypes.ActivityStep,
                        Configuration = new Dictionary<string, object>
                        {
                            ["serviceName"] = "EmailService",
                            ["methodName"] = "SendEmail",
                            ["inputMapping"] = new Dictionary<string, object>
                            {
                                ["to"] = "$employeeEmail",
                                ["subject"] = "Request Rejected"
                            }
                        },
                        Transitions = new List<Transition>()
                    }
                }
            };
        }
    }

    // Mock service implementations
    public class MockEmailService
    {
        public Task<Dictionary<string, object>> SendEmail(string to, string subject)
        {
            Console.WriteLine($"  [EmailService] Sending email to {to}: {subject}");
            return Task.FromResult(new Dictionary<string, object>
            {
                ["messageId"] = Guid.NewGuid().ToString(),
                ["success"] = true
            });
        }
    }

    public class MockEquipmentService
    {
        public Task<Dictionary<string, object>> OrderStandardPackage(string employeeName, string department, string email)
        {
            Console.WriteLine($"  [EquipmentService] Ordering equipment for {employeeName} in {department}");
            return Task.FromResult(new Dictionary<string, object>
            {
                ["orderId"] = Guid.NewGuid().ToString(),
                ["success"] = true
            });
        }
    }

    public class MockITService
    {
        public Task<Dictionary<string, object>> CreateUserAccount(string employeeName, string email, string department)
        {
            Console.WriteLine($"  [ITService] Creating account for {employeeName}");
            return Task.FromResult(new Dictionary<string, object>
            {
                ["username"] = email.Split('@')[0],
                ["success"] = true
            });
        }
    }
}
