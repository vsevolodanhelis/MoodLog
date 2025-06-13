using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MoodLog.Application.Services.Diagnostics
{
    /// <summary>
    /// Service demonstrating advanced exception handling and reflection patterns.
    /// Educational focus: Exception handling, reflection, custom exceptions, diagnostic information.
    /// </summary>
    public interface ISystemDiagnosticsService
    {
        Task<SystemDiagnosticInfo> GetSystemDiagnosticsAsync();
        Task<List<ServiceInfo>> GetRegisteredServicesAsync();
        Task ValidateSystemIntegrityAsync();
    }

    public class SystemDiagnosticsService : ISystemDiagnosticsService
    {
        private readonly IServiceProvider _serviceProvider;

        public SystemDiagnosticsService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<SystemDiagnosticInfo> GetSystemDiagnosticsAsync()
        {
            try
            {
                Console.WriteLine("Gathering system diagnostic information");

                var diagnosticInfo = new SystemDiagnosticInfo();

                // Use reflection to gather assembly information
                await PopulateAssemblyInfoAsync(diagnosticInfo);

                // Gather runtime information
                await PopulateRuntimeInfoAsync(diagnosticInfo);

                // Validate system components
                await ValidateSystemComponentsAsync(diagnosticInfo);

                Console.WriteLine("System diagnostics completed successfully");
                return diagnosticInfo;
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine($"Failed to load types during reflection: {string.Join(", ", ex.LoaderExceptions?.Select(e => e?.Message) ?? Array.Empty<string>())}");
                throw new SystemDiagnosticException("Failed to load system types", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during system diagnostics: {ex.Message}");
                throw new SystemDiagnosticException("System diagnostic operation failed", ex);
            }
        }

        public async Task<List<ServiceInfo>> GetRegisteredServicesAsync()
        {
            try
            {
                Console.WriteLine("Analyzing registered services using reflection");

                var services = new List<ServiceInfo>();

                // Use reflection to analyze the current assembly
                var assembly = Assembly.GetExecutingAssembly();
                var serviceTypes = assembly.GetTypes()
                    .Where(t => t.IsInterface && t.Name.EndsWith("Service"))
                    .ToList();

                foreach (var serviceType in serviceTypes)
                {
                    var serviceInfo = await AnalyzeServiceAsync(serviceType);
                    services.Add(serviceInfo);
                }

                Console.WriteLine($"Analyzed {services.Count} services");
                return services;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing registered services: {ex.Message}");
                throw new ServiceAnalysisException("Failed to analyze services", ex);
            }
        }

        public async Task ValidateSystemIntegrityAsync()
        {
            var validationErrors = new List<string>();

            try
            {
                Console.WriteLine("Validating system integrity");

                // Validate critical assemblies
                await ValidateAssembliesAsync(validationErrors);

                // Validate service dependencies
                await ValidateServiceDependenciesAsync(validationErrors);

                // Validate configuration
                await ValidateConfigurationAsync(validationErrors);

                if (validationErrors.Any())
                {
                    var errorMessage = $"System validation failed with {validationErrors.Count} errors: {string.Join(", ", validationErrors)}";
                    Console.WriteLine(errorMessage);
                    throw new SystemValidationException(errorMessage, validationErrors);
                }

                Console.WriteLine("System integrity validation completed successfully");
            }
            catch (SystemValidationException)
            {
                throw; // Re-throw custom exceptions
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during system validation: {ex.Message}");
                throw new SystemDiagnosticException("System validation failed unexpectedly", ex);
            }
        }

        private async Task PopulateAssemblyInfoAsync(SystemDiagnosticInfo diagnosticInfo)
        {
            await Task.Run(() =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                
                diagnosticInfo.AssemblyName = assembly.GetName().Name ?? "Unknown";
                diagnosticInfo.AssemblyVersion = assembly.GetName().Version?.ToString() ?? "Unknown";
                diagnosticInfo.AssemblyLocation = assembly.Location;
                
                // Use reflection to get custom attributes
                var titleAttribute = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
                diagnosticInfo.AssemblyTitle = titleAttribute?.Title ?? "Unknown";

                var companyAttribute = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
                diagnosticInfo.Company = companyAttribute?.Company ?? "Unknown";
            });
        }

        private async Task PopulateRuntimeInfoAsync(SystemDiagnosticInfo diagnosticInfo)
        {
            await Task.Run(() =>
            {
                diagnosticInfo.RuntimeVersion = Environment.Version.ToString();
                diagnosticInfo.OperatingSystem = Environment.OSVersion.ToString();
                diagnosticInfo.MachineName = Environment.MachineName;
                diagnosticInfo.ProcessorCount = Environment.ProcessorCount;
                diagnosticInfo.WorkingSet = Environment.WorkingSet;
                diagnosticInfo.GCMemory = GC.GetTotalMemory(false);
            });
        }

        private async Task ValidateSystemComponentsAsync(SystemDiagnosticInfo diagnosticInfo)
        {
            await Task.Run(() =>
            {
                var issues = new List<string>();

                // Validate memory usage
                if (diagnosticInfo.WorkingSet > 500_000_000) // 500MB
                {
                    issues.Add("High memory usage detected");
                }

                // Validate GC pressure
                if (diagnosticInfo.GCMemory > 100_000_000) // 100MB
                {
                    issues.Add("High GC memory pressure");
                }

                diagnosticInfo.ValidationIssues = issues;
                diagnosticInfo.IsHealthy = !issues.Any();
            });
        }

        private async Task<ServiceInfo> AnalyzeServiceAsync(Type serviceType)
        {
            return await Task.Run(() =>
            {
                var serviceInfo = new ServiceInfo
                {
                    Name = serviceType.Name,
                    FullName = serviceType.FullName ?? "Unknown",
                    IsInterface = serviceType.IsInterface,
                    Assembly = serviceType.Assembly.GetName().Name ?? "Unknown"
                };

                // Use reflection to get methods
                var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.DeclaringType == serviceType)
                    .Select(m => m.Name)
                    .ToList();

                serviceInfo.Methods = methods;
                serviceInfo.MethodCount = methods.Count;

                return serviceInfo;
            });
        }

        private async Task ValidateAssembliesAsync(List<string> validationErrors)
        {
            await Task.Run(() =>
            {
                try
                {
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var assembly in assemblies)
                    {
                        if (assembly.IsDynamic) continue;

                        try
                        {
                            // Validate assembly can be accessed
                            _ = assembly.GetTypes();
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            validationErrors.Add($"Assembly {assembly.GetName().Name} has type loading issues");
                            Console.WriteLine($"Type loading issues in assembly {assembly.GetName().Name}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    validationErrors.Add($"Assembly validation failed: {ex.Message}");
                }
            });
        }

        private async Task ValidateServiceDependenciesAsync(List<string> validationErrors)
        {
            await Task.Run(() =>
            {
                // Simulate service dependency validation
                // In a real scenario, this would check if all required services are registered
                try
                {
                    var testService = _serviceProvider.GetService(typeof(string));
                    if (testService == null)
                    {
                        validationErrors.Add("Service provider not working correctly");
                    }
                }
                catch (Exception ex)
                {
                    validationErrors.Add($"Service dependency validation failed: {ex.Message}");
                }
            });
        }

        private async Task ValidateConfigurationAsync(List<string> validationErrors)
        {
            await Task.Run(() =>
            {
                // Simulate configuration validation
                // In a real scenario, this would validate configuration settings
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
                {
                    validationErrors.Add("ASPNETCORE_ENVIRONMENT not configured");
                }
            });
        }
    }

    // Custom exception classes demonstrating exception handling patterns
    public class SystemDiagnosticException : Exception
    {
        public SystemDiagnosticException(string message) : base(message) { }
        public SystemDiagnosticException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ServiceAnalysisException : SystemDiagnosticException
    {
        public ServiceAnalysisException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class SystemValidationException : SystemDiagnosticException
    {
        public List<string> ValidationErrors { get; }

        public SystemValidationException(string message, List<string> validationErrors) : base(message)
        {
            ValidationErrors = validationErrors ?? new List<string>();
        }
    }

    // Data models for diagnostic information
    public class SystemDiagnosticInfo
    {
        public string AssemblyName { get; set; } = string.Empty;
        public string AssemblyVersion { get; set; } = string.Empty;
        public string AssemblyLocation { get; set; } = string.Empty;
        public string AssemblyTitle { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string RuntimeVersion { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public int ProcessorCount { get; set; }
        public long WorkingSet { get; set; }
        public long GCMemory { get; set; }
        public List<string> ValidationIssues { get; set; } = new();
        public bool IsHealthy { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class ServiceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsInterface { get; set; }
        public string Assembly { get; set; } = string.Empty;
        public List<string> Methods { get; set; } = new();
        public int MethodCount { get; set; }
    }
}
