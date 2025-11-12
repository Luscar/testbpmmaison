using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WorkflowEngine.Core.Integration;

namespace WorkflowEngine.Example.Integration
{
    /// <summary>
    /// Example implementation of IExternalTaskSystem
    /// This shows how to integrate with an external task management system via REST API
    /// Adapt this to match your specific task system's API
    /// </summary>
    public class ExampleExternalTaskSystem : IExternalTaskSystem
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public ExampleExternalTaskSystem(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl;
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        /// <summary>
        /// Create a task in your external task system
        /// </summary>
        public async Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
        {
            // Build the request payload for YOUR external task system
            // This is just an example - adapt to your API structure
            var payload = new
            {
                title = taskInfo.Title,
                description = taskInfo.Description,
                assignedUsers = taskInfo.AssignedUsers,
                assignedRoles = taskInfo.AssignedRoles,
                dueDate = taskInfo.DueDate,
                priority = taskInfo.Priority ?? "normal",
                metadata = new
                {
                    workflowInstanceId = taskInfo.WorkflowInstanceId,
                    stepInstanceId = taskInfo.StepInstanceId,
                    source = "WorkflowEngine"
                },
                // Include form schema if your task system supports custom forms
                formDefinition = taskInfo.FormSchema,
                // Pass workflow context if needed
                context = taskInfo.WorkflowContext
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Call your external task system API
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/tasks", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            // Extract the task ID from the response (adapt to your API)
            var externalTaskId = result.GetProperty("taskId").GetString();

            Console.WriteLine($"✓ Created external task: {externalTaskId}");
            
            return externalTaskId;
        }

        /// <summary>
        /// Close/complete a task in your external task system
        /// This is called when the user completes the task through your UI
        /// </summary>
        public async Task CloseTaskAsync(string externalTaskId, Dictionary<string, object> completionData)
        {
            // Build the completion request for YOUR external task system
            var payload = new
            {
                status = "completed",
                completedAt = DateTime.UtcNow,
                completionData = completionData
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Call your external task system API to close the task
            var response = await _httpClient.PutAsync($"{_baseUrl}/api/tasks/{externalTaskId}/close", content);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"✓ Closed external task: {externalTaskId}");
        }

        /// <summary>
        /// Update a task in your external task system
        /// </summary>
        public async Task UpdateTaskAsync(string externalTaskId, Dictionary<string, object> updates)
        {
            var json = JsonSerializer.Serialize(updates);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync($"{_baseUrl}/api/tasks/{externalTaskId}", content);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"✓ Updated external task: {externalTaskId}");
        }

        /// <summary>
        /// Cancel a task in your external task system
        /// Called when workflow is cancelled or step times out
        /// </summary>
        public async Task CancelTaskAsync(string externalTaskId, string reason)
        {
            var payload = new
            {
                status = "cancelled",
                reason = reason,
                cancelledAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/api/tasks/{externalTaskId}/cancel", content);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"✓ Cancelled external task: {externalTaskId}");
        }
    }

    /// <summary>
    /// Alternative: Mock implementation for testing without external system
    /// </summary>
    public class MockExternalTaskSystem : IExternalTaskSystem
    {
        private readonly Dictionary<string, ExternalTaskInfo> _tasks = new();

        public Task<string> CreateTaskAsync(ExternalTaskInfo taskInfo)
        {
            var taskId = $"TASK-{Guid.NewGuid().ToString().Substring(0, 8)}";
            _tasks[taskId] = taskInfo;
            
            Console.WriteLine($"[MOCK] Created task {taskId}: {taskInfo.Title}");
            Console.WriteLine($"[MOCK]   Assigned to users: {string.Join(", ", taskInfo.AssignedUsers ?? new List<string>())}");
            Console.WriteLine($"[MOCK]   Assigned to roles: {string.Join(", ", taskInfo.AssignedRoles ?? new List<string>())}");
            
            return Task.FromResult(taskId);
        }

        public Task CloseTaskAsync(string externalTaskId, Dictionary<string, object> completionData)
        {
            Console.WriteLine($"[MOCK] Closed task {externalTaskId}");
            Console.WriteLine($"[MOCK]   Completion data: {JsonSerializer.Serialize(completionData)}");
            
            if (_tasks.ContainsKey(externalTaskId))
            {
                _tasks.Remove(externalTaskId);
            }
            
            return Task.CompletedTask;
        }

        public Task UpdateTaskAsync(string externalTaskId, Dictionary<string, object> updates)
        {
            Console.WriteLine($"[MOCK] Updated task {externalTaskId}");
            return Task.CompletedTask;
        }

        public Task CancelTaskAsync(string externalTaskId, string reason)
        {
            Console.WriteLine($"[MOCK] Cancelled task {externalTaskId}: {reason}");
            
            if (_tasks.ContainsKey(externalTaskId))
            {
                _tasks.Remove(externalTaskId);
            }
            
            return Task.CompletedTask;
        }

        public IReadOnlyDictionary<string, ExternalTaskInfo> GetActiveTasks() => _tasks;
    }
}
