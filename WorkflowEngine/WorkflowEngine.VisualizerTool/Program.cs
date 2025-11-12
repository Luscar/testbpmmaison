using System;
using System.IO;
using System.Threading.Tasks;
using WorkflowEngine.Core.Models;
using WorkflowEngine.Core.Services;
using WorkflowEngine.Core.Visualization;

namespace WorkflowEngine.VisualizerTool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Workflow Visualizer Tool ===\n");

            if (args.Length < 1)
            {
                ShowUsage();
                return;
            }

            var inputFile = args[0];
            var outputFormat = args.Length > 1 ? args[1].ToLower() : "html";
            var outputFile = args.Length > 2 ? args[2] : GetDefaultOutputFile(inputFile, outputFormat);

            try
            {
                // Load workflow definition
                var loader = new WorkflowDefinitionLoader();
                var workflow = await loader.LoadFromFileAsync(inputFile);
                
                Console.WriteLine($"Loaded workflow: {workflow.Name}");
                Console.WriteLine($"Version: {workflow.Version}");
                Console.WriteLine($"Steps: {workflow.Steps.Count}");
                Console.WriteLine();

                // Generate visualization
                var visualizer = new WorkflowVisualizer();
                string output;

                switch (outputFormat)
                {
                    case "mermaid":
                    case "md":
                        output = visualizer.GenerateMermaidDiagram(workflow);
                        break;

                    case "html":
                        output = visualizer.GenerateHtmlVisualization(workflow);
                        break;

                    case "text":
                    case "txt":
                        output = visualizer.GenerateTextVisualization(workflow);
                        break;

                    default:
                        Console.WriteLine($"Unknown format: {outputFormat}");
                        ShowUsage();
                        return;
                }

                // Write output
                await File.WriteAllTextAsync(outputFile, output);
                
                Console.WriteLine($"✓ Visualization generated: {outputFile}");
                Console.WriteLine();

                if (outputFormat == "html")
                {
                    Console.WriteLine("Open the HTML file in a web browser to view the interactive diagram.");
                }
                else if (outputFormat == "mermaid" || outputFormat == "md")
                {
                    Console.WriteLine("You can view this Mermaid diagram at: https://mermaid.live/");
                    Console.WriteLine("Or in any Markdown viewer that supports Mermaid (VS Code, GitHub, etc.)");
                }

                // Show preview for text format
                if (outputFormat == "text" || outputFormat == "txt")
                {
                    Console.WriteLine("\nPreview:");
                    Console.WriteLine(new string('=', 60));
                    Console.WriteLine(output);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  WorkflowVisualizer <input-file> [format] [output-file]");
            Console.WriteLine();
            Console.WriteLine("Formats:");
            Console.WriteLine("  html    - Interactive HTML visualization (default)");
            Console.WriteLine("  mermaid - Mermaid flowchart diagram");
            Console.WriteLine("  text    - Simple text-based visualization");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  WorkflowVisualizer workflow.json");
            Console.WriteLine("  WorkflowVisualizer workflow.json html output.html");
            Console.WriteLine("  WorkflowVisualizer workflow.json mermaid diagram.md");
            Console.WriteLine("  WorkflowVisualizer workflow.json text");
        }

        static string GetDefaultOutputFile(string inputFile, string format)
        {
            var baseName = Path.GetFileNameWithoutExtension(inputFile);
            var extension = format switch
            {
                "html" => "html",
                "mermaid" or "md" => "md",
                "text" or "txt" => "txt",
                _ => "html"
            };
            return $"{baseName}-visualization.{extension}";
        }
    }
}
