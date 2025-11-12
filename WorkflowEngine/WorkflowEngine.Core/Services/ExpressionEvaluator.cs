using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Threading.Tasks;

namespace WorkflowEngine.Core.Services
{
    /// <summary>
    /// Simple expression evaluator using System.Linq.Dynamic.Core
    /// </summary>
    public class ExpressionEvaluator : IExpressionEvaluator
    {
        public bool EvaluateCondition(string expression, Dictionary<string, object> variables)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return true;

            try
            {
                var result = EvaluateExpression(expression, variables);
                return Convert.ToBoolean(result);
            }
            catch
            {
                return false;
            }
        }

        public object EvaluateExpression(string expression, Dictionary<string, object> variables)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return null;

            try
            {
                // Create a parameter array for the dynamic LINQ expression
                var parameters = variables.Select(kvp => 
                    new { Name = kvp.Key, Value = kvp.Value }).ToArray();

                // Use System.Linq.Dynamic.Core to evaluate the expression
                var config = new ParsingConfig { };
                var result = DynamicExpressionParser.ParseLambda(
                    config,
                    false,
                    parameters.Select(p => System.Linq.Expressions.Expression.Parameter(
                        p.Value?.GetType() ?? typeof(object), p.Name)).ToArray(),
                    null,
                    expression,
                    parameters.Select(p => p.Value).ToArray()
                ).Compile().DynamicInvoke(parameters.Select(p => p.Value).ToArray());

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to evaluate expression: {expression}", ex);
            }
        }
    }

    /// <summary>
    /// Registry for business activity services
    /// </summary>
    public class ActivityServiceRegistry : IActivityServiceRegistry
    {
        private readonly Dictionary<string, object> _services = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public void RegisterService(string serviceName, object serviceInstance)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

            if (serviceInstance == null)
                throw new ArgumentNullException(nameof(serviceInstance));

            _services[serviceName] = serviceInstance;
        }

        public object GetService(string serviceName)
        {
            if (_services.TryGetValue(serviceName, out var service))
                return service;

            throw new InvalidOperationException($"Service '{serviceName}' not found in registry");
        }

        public async Task<object> InvokeServiceMethodAsync(string serviceName, string methodName, Dictionary<string, object> parameters)
        {
            var service = GetService(serviceName);
            var serviceType = service.GetType();

            // Find the method
            var method = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));

            if (method == null)
                throw new InvalidOperationException($"Method '{methodName}' not found on service '{serviceName}'");

            // Prepare parameters in the correct order
            var methodParams = method.GetParameters();
            var args = new object[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++)
            {
                var param = methodParams[i];
                if (parameters.TryGetValue(param.Name, out var value))
                {
                    // Convert if necessary
                    if (value != null && !param.ParameterType.IsAssignableFrom(value.GetType()))
                    {
                        args[i] = Convert.ChangeType(value, param.ParameterType);
                    }
                    else
                    {
                        args[i] = value;
                    }
                }
                else if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else
                {
                    throw new InvalidOperationException($"Required parameter '{param.Name}' not provided for method '{methodName}'");
                }
            }

            // Invoke the method
            var result = method.Invoke(service, args);

            // Handle async methods
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                
                // Get the result if it's a Task<T>
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    return resultProperty.GetValue(task);
                }
                
                return null;
            }

            return result;
        }
    }
}
