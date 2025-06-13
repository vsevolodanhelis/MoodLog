using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MoodLog.Application.Services.Diagnostics;
using MoodLog.Application.Services.External;
using System;
using System.Threading.Tasks;

namespace MoodLog.Web.Controllers
{
    /// <summary>
    /// Controller demonstrating advanced C# features integration.
    /// Educational focus: Exception handling, async patterns, dependency injection.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class DiagnosticsController : Controller
    {
        private readonly ISystemDiagnosticsService _diagnosticsService;
        private readonly IWellnessQuoteService _quoteService;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
            ISystemDiagnosticsService diagnosticsService,
            IWellnessQuoteService quoteService,
            ILogger<DiagnosticsController> logger)
        {
            _diagnosticsService = diagnosticsService ?? throw new ArgumentNullException(nameof(diagnosticsService));
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Accessing system diagnostics dashboard");

                var diagnosticInfo = await _diagnosticsService.GetSystemDiagnosticsAsync();
                var services = await _diagnosticsService.GetRegisteredServicesAsync();

                var viewModel = new DiagnosticsViewModel
                {
                    SystemInfo = diagnosticInfo,
                    Services = services,
                    GeneratedAt = DateTime.UtcNow
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading diagnostics dashboard");
                TempData["Error"] = "Failed to load system diagnostics. Please try again.";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost]
        [Route("validate")]
        public async Task<IActionResult> ValidateSystem()
        {
            try
            {
                _logger.LogInformation("Starting system validation");

                await _diagnosticsService.ValidateSystemIntegrityAsync();
                
                TempData["Success"] = "System validation completed successfully. No issues found.";
            }
            catch (SystemValidationException ex)
            {
                _logger.LogWarning(ex, "System validation found issues");
                TempData["Warning"] = $"System validation found {ex.ValidationErrors.Count} issues: {string.Join(", ", ex.ValidationErrors)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during system validation");
                TempData["Error"] = "System validation failed unexpectedly. Please check logs.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("quote")]
        public async Task<IActionResult> GetWellnessQuote(string category = "daily")
        {
            try
            {
                _logger.LogInformation("Fetching wellness quote for category: {Category}", category);

                var quote = category.ToLower() switch
                {
                    "motivational" => await _quoteService.GetMotivationalQuoteAsync(),
                    _ => await _quoteService.GetDailyQuoteAsync()
                };

                if (quote == null)
                {
                    return Json(new { success = false, message = "No quote available" });
                }

                return Json(new 
                { 
                    success = true, 
                    quote = new 
                    {
                        text = quote.Text,
                        author = quote.Author,
                        category = quote.Category,
                        retrievedAt = quote.RetrievedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching wellness quote");
                return Json(new { success = false, message = "Failed to fetch quote" });
            }
        }

        [HttpGet]
        [Route("services")]
        public async Task<IActionResult> GetServices()
        {
            try
            {
                var services = await _diagnosticsService.GetRegisteredServicesAsync();
                return Json(new { success = true, services });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching service information");
                return Json(new { success = false, message = "Failed to fetch services" });
            }
        }

        [HttpGet]
        [Route("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var diagnosticInfo = await _diagnosticsService.GetSystemDiagnosticsAsync();
                
                var healthStatus = new
                {
                    isHealthy = diagnosticInfo.IsHealthy,
                    timestamp = DateTime.UtcNow,
                    memoryUsage = new
                    {
                        workingSet = diagnosticInfo.WorkingSet,
                        gcMemory = diagnosticInfo.GCMemory,
                        workingSetMB = Math.Round(diagnosticInfo.WorkingSet / 1024.0 / 1024.0, 2),
                        gcMemoryMB = Math.Round(diagnosticInfo.GCMemory / 1024.0 / 1024.0, 2)
                    },
                    system = new
                    {
                        processorCount = diagnosticInfo.ProcessorCount,
                        runtimeVersion = diagnosticInfo.RuntimeVersion,
                        machineName = diagnosticInfo.MachineName
                    },
                    issues = diagnosticInfo.ValidationIssues
                };

                return Json(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
                return Json(new 
                { 
                    isHealthy = false, 
                    timestamp = DateTime.UtcNow,
                    error = "Health check failed"
                });
            }
        }
    }

    // ViewModel for diagnostics page
    public class DiagnosticsViewModel
    {
        public SystemDiagnosticInfo SystemInfo { get; set; } = new();
        public List<ServiceInfo> Services { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
