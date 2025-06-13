using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MoodLog.Application.Services.External
{
    /// <summary>
    /// Service demonstrating HttpClient usage for external API communication.
    /// Educational focus: HttpClient patterns, async/await, exception handling, JSON deserialization.
    /// </summary>
    public interface IWellnessQuoteService
    {
        Task<WellnessQuote?> GetDailyQuoteAsync(CancellationToken cancellationToken = default);
        Task<WellnessQuote?> GetMotivationalQuoteAsync(CancellationToken cancellationToken = default);
    }

    public class WellnessQuoteService : IWellnessQuoteService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed = false;

        public WellnessQuoteService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // Configure HttpClient
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MoodLog/1.0");

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<WellnessQuote?> GetDailyQuoteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("Fetching daily wellness quote");

                // For demonstration, we'll use a fallback approach since we don't want external dependencies
                // In a real scenario, this would call an actual quotes API
                return await GetFallbackQuoteAsync("daily", cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request failed while fetching daily quote: {ex.Message}");
                return GetOfflineQuote("daily");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine("Request timeout while fetching daily quote");
                return GetOfflineQuote("daily");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while fetching daily quote: {ex.Message}");
                return GetOfflineQuote("daily");
            }
        }

        public async Task<WellnessQuote?> GetMotivationalQuoteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("Fetching motivational wellness quote");

                return await GetFallbackQuoteAsync("motivational", cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request failed while fetching motivational quote: {ex.Message}");
                return GetOfflineQuote("motivational");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine("Request timeout while fetching motivational quote");
                return GetOfflineQuote("motivational");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while fetching motivational quote: {ex.Message}");
                return GetOfflineQuote("motivational");
            }
        }

        private async Task<WellnessQuote?> GetFallbackQuoteAsync(string category, CancellationToken cancellationToken)
        {
            // Simulate API call with delay
            await Task.Delay(100, cancellationToken);

            // Return curated wellness quotes based on category
            var quotes = category.ToLower() switch
            {
                "daily" => GetDailyQuotes(),
                "motivational" => GetMotivationalQuotes(),
                _ => GetDailyQuotes()
            };

            var randomIndex = new Random().Next(quotes.Length);
            var selectedQuote = quotes[randomIndex];

            Console.WriteLine($"Retrieved {category} quote: {selectedQuote.Text}");

            return selectedQuote;
        }

        private WellnessQuote GetOfflineQuote(string category)
        {
            Console.WriteLine($"Using offline fallback quote for category: {category}");

            return category.ToLower() switch
            {
                "motivational" => new WellnessQuote
                {
                    Text = "Every small step forward is progress worth celebrating.",
                    Author = "MoodLog Team",
                    Category = "Motivational"
                },
                _ => new WellnessQuote
                {
                    Text = "Take time to make your soul happy.",
                    Author = "MoodLog Team",
                    Category = "Daily Wellness"
                }
            };
        }

        private static WellnessQuote[] GetDailyQuotes()
        {
            return new[]
            {
                new WellnessQuote { Text = "Take time to make your soul happy.", Author = "Unknown", Category = "Daily Wellness" },
                new WellnessQuote { Text = "Your mental health is a priority. Your happiness is essential.", Author = "Unknown", Category = "Daily Wellness" },
                new WellnessQuote { Text = "Be gentle with yourself. You're doing the best you can.", Author = "Unknown", Category = "Daily Wellness" },
                new WellnessQuote { Text = "Progress, not perfection.", Author = "Unknown", Category = "Daily Wellness" },
                new WellnessQuote { Text = "You are enough, just as you are.", Author = "Unknown", Category = "Daily Wellness" }
            };
        }

        private static WellnessQuote[] GetMotivationalQuotes()
        {
            return new[]
            {
                new WellnessQuote { Text = "Every small step forward is progress worth celebrating.", Author = "Unknown", Category = "Motivational" },
                new WellnessQuote { Text = "You have been assigned this mountain to show others it can be moved.", Author = "Mel Robbins", Category = "Motivational" },
                new WellnessQuote { Text = "Your current situation is not your final destination.", Author = "Unknown", Category = "Motivational" },
                new WellnessQuote { Text = "Difficult roads often lead to beautiful destinations.", Author = "Unknown", Category = "Motivational" },
                new WellnessQuote { Text = "You are stronger than you think and more capable than you imagine.", Author = "Unknown", Category = "Motivational" }
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Console.WriteLine("Disposing WellnessQuoteService resources");
                // HttpClient is typically managed by HttpClientFactory, so we don't dispose it here
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class WellnessQuote
    {
        public string Text { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    }
}
