using MoodLog.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MoodLog.Application.Services.Background
{
    /// <summary>
    /// Background service demonstrating IDisposable pattern and proper resource management.
    /// Educational focus: IDisposable interface, background services, resource cleanup.
    /// </summary>
    public class DataCleanupService : IDisposable
    {
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed = false;

        public DataCleanupService()
        {
            // Initialize semaphore for thread-safe operations
            _semaphore = new SemaphoreSlim(1, 1);

            // Initialize timer for periodic cleanup (every 6 hours)
            _cleanupTimer = new Timer(
                callback: async _ => await PerformCleanupAsync(),
                state: null,
                dueTime: TimeSpan.FromMinutes(5), // First run after 5 minutes
                period: TimeSpan.FromHours(6)     // Then every 6 hours
            );

            Console.WriteLine("DataCleanupService initialized with 6-hour cleanup interval");
        }

        public async Task StartAsync()
        {
            Console.WriteLine("DataCleanupService started");
            await Task.CompletedTask;
        }

        private async Task PerformCleanupAsync()
        {
            if (_disposed) return;

            await _semaphore.WaitAsync();
            try
            {
                Console.WriteLine("Starting data cleanup operation");

                // Cleanup old temporary data (demonstration of business logic)
                var cutoffDate = DateTime.UtcNow.AddDays(-90);
                var cleanupCount = await CleanupOldTemporaryDataAsync(cutoffDate);

                Console.WriteLine($"Data cleanup completed. Processed {cleanupCount} items");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during data cleanup operation: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<int> CleanupOldTemporaryDataAsync(DateTime cutoffDate)
        {
            // Simulate cleanup operations - in a real scenario, this might:
            // - Remove old audit logs
            // - Clean up temporary files
            // - Archive old data
            // - Optimize database indexes
            
            await Task.Delay(100); // Simulate work
            
            var cleanupCount = new Random().Next(0, 10);
            Console.WriteLine($"Simulated cleanup of {cleanupCount} temporary items older than {cutoffDate}");
            
            return cleanupCount;
        }

        private async Task PerformHealthCheckAsync()
        {
            if (_disposed) return;

            try
            {
                // Perform lightweight health checks
                await Task.Delay(50); // Simulate health check

                Console.WriteLine("Health check completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health check failed: {ex.Message}");
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Console.WriteLine("Disposing DataCleanupService resources");

                // Dispose managed resources
                _cleanupTimer?.Dispose();
                _semaphore?.Dispose();

                _disposed = true;
            }
        }

        // Finalizer (destructor) - demonstrates proper IDisposable pattern
        ~DataCleanupService()
        {
            Dispose(disposing: false);
        }

        // Public dispose method
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
