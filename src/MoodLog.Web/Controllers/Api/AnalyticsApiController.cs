using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoodLog.Application.Interfaces;

namespace MoodLog.Web.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AnalyticsApiController : ControllerBase
{
    private readonly IMoodEntryService _moodEntryService;
    private readonly UserManager<IdentityUser> _userManager;

    public AnalyticsApiController(
        IMoodEntryService moodEntryService,
        UserManager<IdentityUser> userManager)
    {
        _moodEntryService = moodEntryService;
        _userManager = userManager;
    }

    /// <summary>
    /// Get mood trends over time
    /// </summary>
    [HttpGet("trends")]
    public async Task<ActionResult<object>> GetMoodTrends(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string groupBy = "day")
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = endDate ?? DateTime.Today;

        var entries = await _moodEntryService.GetByDateRangeAsync(userId.Value, start, end);
        var entriesList = entries.ToList();

        var trends = groupBy.ToLower() switch
        {
            "week" => entriesList
                .GroupBy(e => GetWeekOfYear(e.EntryDate))
                .Select(g => new
                {
                    Period = $"Week {g.Key}",
                    Date = g.First().EntryDate,
                    AverageMood = g.Average(e => e.MoodLevel),
                    EntryCount = g.Count()
                })
                .OrderBy(x => x.Date),
            "month" => entriesList
                .GroupBy(e => new { e.EntryDate.Year, e.EntryDate.Month })
                .Select(g => new
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    AverageMood = g.Average(e => e.MoodLevel),
                    EntryCount = g.Count()
                })
                .OrderBy(x => x.Date),
            _ => entriesList
                .GroupBy(e => e.EntryDate.Date)
                .Select(g => new
                {
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    Date = g.Key,
                    AverageMood = g.Average(e => e.MoodLevel),
                    EntryCount = g.Count()
                })
                .OrderBy(x => x.Date)
        };

        return Ok(new
        {
            GroupBy = groupBy,
            DateRange = new { StartDate = start, EndDate = end },
            Trends = trends
        });
    }

    /// <summary>
    /// Get mood distribution analysis
    /// </summary>
    [HttpGet("distribution")]
    public async Task<ActionResult<object>> GetMoodDistribution(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = endDate ?? DateTime.Today;

        var entries = await _moodEntryService.GetByDateRangeAsync(userId.Value, start, end);
        var entriesList = entries.ToList();

        var distribution = entriesList
            .GroupBy(e => e.MoodLevel)
            .Select(g => new
            {
                MoodLevel = g.Key,
                Count = g.Count(),
                Percentage = Math.Round((double)g.Count() / entriesList.Count * 100, 2),
                Category = GetMoodCategory(g.Key)
            })
            .OrderBy(x => x.MoodLevel);

        var summary = new
        {
            TotalEntries = entriesList.Count,
            AverageMood = entriesList.Any() ? Math.Round(entriesList.Average(e => e.MoodLevel), 2) : 0,
            MostCommonMood = distribution.OrderByDescending(d => d.Count).FirstOrDefault()?.MoodLevel ?? 0,
            MoodRange = entriesList.Any() ? new
            {
                Lowest = entriesList.Min(e => e.MoodLevel),
                Highest = entriesList.Max(e => e.MoodLevel)
            } : null
        };

        return Ok(new
        {
            DateRange = new { StartDate = start, EndDate = end },
            Summary = summary,
            Distribution = distribution
        });
    }

    /// <summary>
    /// Get mood patterns and insights
    /// </summary>
    [HttpGet("insights")]
    public async Task<ActionResult<object>> GetMoodInsights(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var start = startDate ?? DateTime.Today.AddDays(-90);
        var end = endDate ?? DateTime.Today;

        var entries = await _moodEntryService.GetByDateRangeAsync(userId.Value, start, end);
        var entriesList = entries.OrderBy(e => e.EntryDate).ToList();

        var insights = new List<object>();

        // Day of week patterns
        var dayOfWeekPattern = entriesList
            .GroupBy(e => e.EntryDate.DayOfWeek)
            .Select(g => new
            {
                DayOfWeek = g.Key.ToString(),
                AverageMood = Math.Round(g.Average(e => e.MoodLevel), 2),
                EntryCount = g.Count()
            })
            .OrderByDescending(x => x.AverageMood);

        if (dayOfWeekPattern.Any())
        {
            var bestDay = dayOfWeekPattern.First();
            var worstDay = dayOfWeekPattern.Last();
            
            insights.Add(new
            {
                Type = "DayOfWeekPattern",
                Message = $"Your best mood day is typically {bestDay.DayOfWeek} (avg: {bestDay.AverageMood}), while {worstDay.DayOfWeek} tends to be more challenging (avg: {worstDay.AverageMood})",
                Data = dayOfWeekPattern
            });
        }

        // Streak analysis
        var currentStreak = CalculateCurrentStreak(entriesList);
        if (currentStreak > 0)
        {
            insights.Add(new
            {
                Type = "CurrentStreak",
                Message = $"You're on a {currentStreak}-day logging streak! Keep it up!",
                Data = new { StreakDays = currentStreak }
            });
        }

        // Mood volatility
        if (entriesList.Count >= 7)
        {
            var recentWeek = entriesList.TakeLast(7).ToList();
            var volatility = CalculateVolatility(recentWeek.Select(e => e.MoodLevel));
            
            insights.Add(new
            {
                Type = "MoodVolatility",
                Message = volatility < 1.5 ? "Your mood has been quite stable recently" : "Your mood has been more variable lately",
                Data = new { Volatility = Math.Round(volatility, 2) }
            });
        }

        return Ok(new
        {
            DateRange = new { StartDate = start, EndDate = end },
            Insights = insights
        });
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;
        return int.TryParse(user.Id, out var id) ? id : user.Id.GetHashCode();
    }

    private static string GetMoodCategory(int moodLevel) => moodLevel switch
    {
        <= 3 => "Low",
        <= 6 => "Moderate",
        <= 8 => "Good",
        _ => "Excellent"
    };

    private static int GetWeekOfYear(DateTime date)
    {
        var jan1 = new DateTime(date.Year, 1, 1);
        var daysOffset = (int)jan1.DayOfWeek;
        var firstWeekDay = jan1.AddDays(-daysOffset);
        var weekNum = ((date - firstWeekDay).Days / 7) + 1;
        return weekNum;
    }

    private static int CalculateCurrentStreak(IEnumerable<Core.DTOs.MoodEntryDto> entries)
    {
        var sortedEntries = entries.OrderByDescending(e => e.EntryDate).ToList();
        var streak = 0;
        var currentDate = DateTime.Today;

        foreach (var entry in sortedEntries)
        {
            if (entry.EntryDate.Date == currentDate.Date)
            {
                streak++;
                currentDate = currentDate.AddDays(-1);
            }
            else if (entry.EntryDate.Date < currentDate.Date)
            {
                break;
            }
        }

        return streak;
    }

    private static double CalculateVolatility(IEnumerable<int> moodLevels)
    {
        var values = moodLevels.ToList();
        if (values.Count < 2) return 0;

        var mean = values.Average();
        var variance = values.Sum(x => Math.Pow(x - mean, 2)) / values.Count;
        return Math.Sqrt(variance);
    }
}
