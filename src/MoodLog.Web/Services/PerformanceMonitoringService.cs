using System.Diagnostics;

namespace MoodLog.Web.Services;

public class PerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly Dictionary<string, List<long>> _performanceMetrics;

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
    {
        _logger = logger;
        _performanceMetrics = new Dictionary<string, List<long>>();
    }

    public async Task<T> MonitorAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds);
            
            if (stopwatch.ElapsedMilliseconds > 1000) // Log slow operations
            {
                _logger.LogWarning("Slow operation detected: {OperationName} took {ElapsedMs}ms", 
                    operationName, stopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Operation {OperationName} failed after {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public T Monitor<T>(string operationName, Func<T> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = operation();
            stopwatch.Stop();
            
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds);
            
            if (stopwatch.ElapsedMilliseconds > 500) // Log slow operations
            {
                _logger.LogWarning("Slow operation detected: {OperationName} took {ElapsedMs}ms", 
                    operationName, stopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Operation {OperationName} failed after {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public void RecordMetric(string operationName, long elapsedMilliseconds)
    {
        lock (_performanceMetrics)
        {
            if (!_performanceMetrics.ContainsKey(operationName))
            {
                _performanceMetrics[operationName] = new List<long>();
            }
            
            _performanceMetrics[operationName].Add(elapsedMilliseconds);
            
            // Keep only last 100 measurements per operation
            if (_performanceMetrics[operationName].Count > 100)
            {
                _performanceMetrics[operationName].RemoveAt(0);
            }
        }
    }

    public PerformanceReport GetPerformanceReport()
    {
        lock (_performanceMetrics)
        {
            var report = new PerformanceReport
            {
                GeneratedAt = DateTime.UtcNow,
                Operations = new List<OperationMetrics>()
            };

            foreach (var kvp in _performanceMetrics)
            {
                var metrics = kvp.Value;
                if (metrics.Any())
                {
                    report.Operations.Add(new OperationMetrics
                    {
                        OperationName = kvp.Key,
                        TotalCalls = metrics.Count,
                        AverageMs = metrics.Average(),
                        MinMs = metrics.Min(),
                        MaxMs = metrics.Max(),
                        MedianMs = CalculateMedian(metrics)
                    });
                }
            }

            return report;
        }
    }

    private double CalculateMedian(List<long> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var count = sorted.Count;
        
        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }
        else
        {
            return sorted[count / 2];
        }
    }
}

public class PerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public List<OperationMetrics> Operations { get; set; } = new();
}

public class OperationMetrics
{
    public string OperationName { get; set; } = string.Empty;
    public int TotalCalls { get; set; }
    public double AverageMs { get; set; }
    public long MinMs { get; set; }
    public long MaxMs { get; set; }
    public double MedianMs { get; set; }
}
