using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using WorkflowEngine.Core.Models;

namespace WorkflowEngine.Core.Visualization
{
    /// <summary>
    /// Generates visual representations of workflow definitions
    /// </summary>
    public class WorkflowVisualizer
    {
        /// <summary>
        /// Generate a Mermaid flowchart diagram from a workflow definition
        /// </summary>
        public string GenerateMermaidDiagram(WorkflowDefinition workflow)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("```mermaid");
            sb.AppendLine("flowchart TD");
            sb.AppendLine($"    Start([Start: {EscapeLabel(workflow.Name)}])");
            sb.AppendLine();

            // Add all steps
            foreach (var step in workflow.Steps)
            {
                var shape = GetNodeShape(step.Type);
                var label = EscapeLabel(step.Name);
                sb.AppendLine($"    {step.Id}{shape[0]}{label}{shape[1]}");
            }

            sb.AppendLine();
            sb.AppendLine("    End([End])");
            sb.AppendLine();

            // Add connections
            sb.AppendLine($"    Start --> {workflow.InitialStepId}");

            foreach (var step in workflow.Steps)
            {
                AddStepConnections(sb, step, workflow);
            }

            sb.AppendLine("```");

            return sb.ToString();
        }

        /// <summary>
        /// Generate an HTML visualization with interactive features
        /// </summary>
        public string GenerateHtmlVisualization(WorkflowDefinition workflow)
        {
            var mermaidCode = GenerateMermaidDiagram(workflow).Replace("```mermaid\n", "").Replace("```", "");
            
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{workflow.Name} - Workflow Visualization</title>
    <script src=""https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js""></script>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 20px;
            background-color: #f5f5f5;
        }}
        .container {{
            max-width: 1200px;
            margin: 0 auto;
            background-color: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        h1 {{
            color: #333;
            border-bottom: 2px solid #4CAF50;
            padding-bottom: 10px;
        }}
        .info {{
            background-color: #f9f9f9;
            padding: 15px;
            border-left: 4px solid #4CAF50;
            margin: 20px 0;
        }}
        .mermaid {{
            background-color: white;
            padding: 20px;
            border: 1px solid #ddd;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .step-details {{
            margin-top: 30px;
        }}
        .step-card {{
            background-color: #fafafa;
            border: 1px solid #e0e0e0;
            border-radius: 4px;
            padding: 15px;
            margin: 10px 0;
        }}
        .step-type {{
            display: inline-block;
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 12px;
            font-weight: bold;
            color: white;
        }}
        .type-interaction {{ background-color: #2196F3; }}
        .type-business {{ background-color: #4CAF50; }}
        .type-decision {{ background-color: #FF9800; }}
        .type-scheduled {{ background-color: #9C27B0; }}
        .type-subworkflow {{ background-color: #00BCD4; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>{workflow.Name}</h1>
        
        <div class=""info"">
            <strong>ID:</strong> {workflow.Id}<br>
            <strong>Version:</strong> {workflow.Version}<br>
            <strong>Description:</strong> {workflow.Description}<br>
            <strong>Initial Step:</strong> {workflow.InitialStepId}<br>
            <strong>Total Steps:</strong> {workflow.Steps.Count}
        </div>

        <h2>Workflow Diagram</h2>
        <div class=""mermaid"">
{mermaidCode}
        </div>

        <h2>Step Details</h2>
        <div class=""step-details"">
            {GenerateStepDetailsHtml(workflow)}
        </div>
    </div>

    <script>
        mermaid.initialize({{ startOnLoad: true, theme: 'default' }});
    </script>
</body>
</html>";

            return html;
        }

        /// <summary>
        /// Generate a simple text-based ASCII visualization
        /// </summary>
        public string GenerateTextVisualization(WorkflowDefinition workflow)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Workflow: {workflow.Name}");
            sb.AppendLine($"Version: {workflow.Version}");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine();

            var visited = new HashSet<string>();
            GenerateTextNode(workflow, workflow.InitialStepId, visited, sb, 0);

            return sb.ToString();
        }

        private void GenerateTextNode(WorkflowDefinition workflow, string stepId, HashSet<string> visited, StringBuilder sb, int indent)
        {
            if (string.IsNullOrEmpty(stepId) || visited.Contains(stepId))
                return;

            visited.Add(stepId);
            var step = workflow.Steps.FirstOrDefault(s => s.Id == stepId);
            if (step == null)
                return;

            var prefix = new string(' ', indent * 2);
            sb.AppendLine($"{prefix}[{step.Type.ToUpper()}] {step.Name} (ID: {step.Id})");

            if (step.Type == "decision")
            {
                var config = GetDecisionConfig(step);
                if (config?.Routes != null)
                {
                    foreach (var route in config.Routes)
                    {
                        sb.AppendLine($"{prefix}  └─ {route.Name} → {route.NextStepId}");
                        if (!string.IsNullOrEmpty(route.Condition))
                        {
                            sb.AppendLine($"{prefix}     Condition: {route.Condition}");
                        }
                    }
                    if (!string.IsNullOrEmpty(config.DefaultNextStepId))
                    {
                        sb.AppendLine($"{prefix}  └─ default → {config.DefaultNextStepId}");
                    }
                }
            }
            else if (!string.IsNullOrEmpty(step.NextStepId))
            {
                sb.AppendLine($"{prefix}  └─ Next: {step.NextStepId}");
                sb.AppendLine();
                GenerateTextNode(workflow, step.NextStepId, visited, sb, indent + 1);
            }
        }

        private DecisionStepConfig GetDecisionConfig(StepDefinition step)
        {
            try
            {
                return JsonSerializer.Deserialize<DecisionStepConfig>(
                    JsonSerializer.Serialize(step.Configuration));
            }
            catch
            {
                return null;
            }
        }

        private void AddStepConnections(StringBuilder sb, StepDefinition step, WorkflowDefinition workflow)
        {
            if (step.Type == "decision")
            {
                var config = GetDecisionConfig(step);
                if (config?.Routes != null)
                {
                    foreach (var route in config.Routes)
                    {
                        var label = !string.IsNullOrEmpty(route.Condition) 
                            ? $"|{EscapeLabel(route.Condition)}|" 
                            : $"|{route.Name}|";
                        sb.AppendLine($"    {step.Id} -->{label} {route.NextStepId}");
                    }
                }
                
                if (!string.IsNullOrEmpty(config?.DefaultNextStepId))
                {
                    sb.AppendLine($"    {step.Id} -.default.-> {config.DefaultNextStepId}");
                }
            }
            else if (!string.IsNullOrEmpty(step.NextStepId))
            {
                sb.AppendLine($"    {step.Id} --> {step.NextStepId}");
            }
            else
            {
                // Terminal step
                sb.AppendLine($"    {step.Id} --> End");
            }
        }

        private string[] GetNodeShape(string stepType)
        {
            return stepType.ToLower() switch
            {
                "interaction" => new[] { "[/", "/]" },      // Parallelogram
                "business" => new[] { "[", "]" },           // Rectangle
                "decision" => new[] { "{", "}" },           // Diamond
                "scheduled" => new[] { "([", "])" },        // Stadium
                "subworkflow" => new[] { "[[", "]]" },      // Subroutine
                _ => new[] { "[", "]" }
            };
        }

        private string EscapeLabel(string label)
        {
            return label?.Replace("\"", "'")
                        .Replace("[", "(")
                        .Replace("]", ")")
                        .Replace("{", "(")
                        .Replace("}", ")") ?? "";
        }

        private string GenerateStepDetailsHtml(WorkflowDefinition workflow)
        {
            var sb = new StringBuilder();

            foreach (var step in workflow.Steps)
            {
                var typeClass = $"type-{step.Type}";
                sb.AppendLine($@"
            <div class=""step-card"">
                <h3>{step.Name} <span class=""step-type {typeClass}"">{step.Type}</span></h3>
                <p><strong>ID:</strong> {step.Id}</p>");

                if (!string.IsNullOrEmpty(step.NextStepId))
                {
                    sb.AppendLine($"                <p><strong>Next Step:</strong> {step.NextStepId}</p>");
                }

                if (step.Type == "decision")
                {
                    var config = GetDecisionConfig(step);
                    if (config?.Routes != null && config.Routes.Any())
                    {
                        sb.AppendLine("                <p><strong>Routes:</strong></p>");
                        sb.AppendLine("                <ul>");
                        foreach (var route in config.Routes)
                        {
                            var conditionInfo = !string.IsNullOrEmpty(route.Condition) 
                                ? $" - Condition: <code>{route.Condition}</code>" 
                                : "";
                            sb.AppendLine($"                    <li>{route.Name} → {route.NextStepId}{conditionInfo}</li>");
                        }
                        sb.AppendLine("                </ul>");
                    }
                }

                sb.AppendLine("            </div>");
            }

            return sb.ToString();
        }
    }
}
