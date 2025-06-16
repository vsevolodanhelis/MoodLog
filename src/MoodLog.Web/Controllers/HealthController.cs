using Microsoft.AspNetCore.Mvc;
using MoodLog.Infrastructure.Data;
using MoodLog.Web.Services;
using System.Diagnostics;

namespace MoodLog.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PerformanceMonitoringService _performanceService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ApplicationDbContext context,
        PerformanceMonitoringService performanceService,
        ILogger<HealthController> logger)
    {
        _context = context;
        _performanceService = performanceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var healthCheck = new HealthCheckResult
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetApplicationVersion(),
            Checks = new List<HealthCheckItem>()
        };

        try
        {
            // Database connectivity check
            var dbCheck = await CheckDatabaseHealth();
            healthCheck.Checks.Add(dbCheck);

            // Memory usage check
            var memoryCheck = CheckMemoryUsage();
            healthCheck.Checks.Add(memoryCheck);

            // Disk space check
            var diskCheck = CheckDiskSpace();
            healthCheck.Checks.Add(diskCheck);

            // Performance metrics check
            var performanceCheck = CheckPerformanceMetrics();
            healthCheck.Checks.Add(performanceCheck);

            // Determine overall status
            if (healthCheck.Checks.Any(c => c.Status == "Critical"))
            {
                healthCheck.Status = "Critical";
                return StatusCode(503, healthCheck);
            }
            else if (healthCheck.Checks.Any(c => c.Status == "Warning"))
            {
                healthCheck.Status = "Warning";
                return Ok(healthCheck);
            }

            return Ok(healthCheck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            healthCheck.Status = "Critical";
            healthCheck.Checks.Add(new HealthCheckItem
            {
                Name = "General",
                Status = "Critical",
                Message = "Health check failed with exception",
                Details = ex.Message
            });

            return StatusCode(503, healthCheck);
        }
    }

    [HttpGet("performance")]
    public IActionResult GetPerformanceReport()
    {
        try
        {
            var report = _performanceService.GetPerformanceReport();
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate performance report");
            return StatusCode(500, new { error = "Failed to generate performance report" });
        }
    }

    private async Task<HealthCheckItem> CheckDatabaseHealth()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var canConnect = await _context.Database.CanConnectAsync();
            stopwatch.Stop();

            if (!canConnect)
            {
                return new HealthCheckItem
                {
                    Name = "Database",
                    Status = "Critical",
                    Message = "Cannot connect to database",
                    ResponseTime = stopwatch.ElapsedMilliseconds
                };
            }

            var status = stopwatch.ElapsedMilliseconds > 1000 ? "Warning" : "Healthy";
            var message = stopwatch.ElapsedMilliseconds > 1000 ? "Slow database response" : "Database connection healthy";

            return new HealthCheckItem
            {
                Name = "Database",
                Status = status,
                Message = message,
                ResponseTime = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckItem
            {
                Name = "Database",
                Status = "Critical",
                Message = "Database health check failed",
                Details = ex.Message
            };
        }
    }

    private HealthCheckItem CheckMemoryUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var memoryUsageMB = process.WorkingSet64 / 1024 / 1024;

            string status;
            string message;

            if (memoryUsageMB > 1000) // > 1GB
            {
                status = "Critical";
                message = "High memory usage detected";
            }
            else if (memoryUsageMB > 500) // > 500MB
            {
                status = "Warning";
                message = "Elevated memory usage";
            }
            else
            {
                status = "Healthy";
                message = "Memory usage normal";
            }

            return new HealthCheckItem
            {
                Name = "Memory",
                Status = status,
                Message = message,
                Details = $"Current usage: {memoryUsageMB} MB"
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckItem
            {
                Name = "Memory",
                Status = "Warning",
                Message = "Could not check memory usage",
                Details = ex.Message
            };
        }
    }

    private HealthCheckItem CheckDiskSpace()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory)!);
            var freeSpaceGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;

            string status;
            string message;

            if (freeSpaceGB < 1) // < 1GB
            {
                status = "Critical";
                message = "Very low disk space";
            }
            else if (freeSpaceGB < 5) // < 5GB
            {
                status = "Warning";
                message = "Low disk space";
            }
            else
            {
                status = "Healthy";
                message = "Sufficient disk space";
            }

            return new HealthCheckItem
            {
                Name = "Disk Space",
                Status = status,
                Message = message,
                Details = $"Available: {freeSpaceGB} GB"
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckItem
            {
                Name = "Disk Space",
                Status = "Warning",
                Message = "Could not check disk space",
                Details = ex.Message
            };
        }
    }

    private HealthCheckItem CheckPerformanceMetrics()
    {
        try
        {
            var report = _performanceService.GetPerformanceReport();
            var slowOperations = report.Operations.Where(o => o.AverageMs > 1000).ToList();

            string status;
            string message;

            if (slowOperations.Count > 5)
            {
                status = "Critical";
                message = "Multiple slow operations detected";
            }
            else if (slowOperations.Any())
            {
                status = "Warning";
                message = "Some slow operations detected";
            }
            else
            {
                status = "Healthy";
                message = "Performance metrics normal";
            }

            return new HealthCheckItem
            {
                Name = "Performance",
                Status = status,
                Message = message,
                Details = $"Slow operations: {slowOperations.Count}"
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckItem
            {
                Name = "Performance",
                Status = "Warning",
                Message = "Could not check performance metrics",
                Details = ex.Message
            };
        }
    }

    private string GetApplicationVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}

public class HealthCheckResult
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public List<HealthCheckItem> Checks { get; set; } = new();
}

public class HealthCheckItem
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public long? ResponseTime { get; set; }
}
