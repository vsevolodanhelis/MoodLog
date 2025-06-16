using MoodLog.Application.Interfaces;
using MoodLog.Core.DTOs;
using MoodLog.Core.Interfaces;

namespace MoodLog.Application.Services;

public class MoodAnalyticsService : IMoodAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;

    public MoodAnalyticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MoodAnalyticsDto> GetMoodAnalyticsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAndDateRangeAsync(
            userId, 
            startDate ?? DateTime.Today.AddDays(-30), 
            endDate ?? DateTime.Today
        );

        var entryList = entries.ToList();

        if (!entryList.Any())
        {
            return new MoodAnalyticsDto
            {
                AverageMood = 0,
                TotalEntries = 0,
                MoodTrend = "No data",
                BestDay = null,
                WorstDay = null,
                MostCommonMood = 0,
                MoodDistribution = new Dictionary<int, int>(),
                WeeklyAverages = new List<WeeklyMoodDto>(),
                Insights = new List<string> { "Start logging your mood to see insights!" }
            };
        }

        var analytics = new MoodAnalyticsDto
        {
            AverageMood = entryList.Average(e => e.MoodLevel),
            TotalEntries = entryList.Count,
            MoodTrend = CalculateMoodTrend(entryList),
            BestDay = entryList.OrderByDescending(e => e.MoodLevel).First(),
            WorstDay = entryList.OrderBy(e => e.MoodLevel).First(),
            MostCommonMood = CalculateMostCommonMood(entryList),
            MoodDistribution = CalculateMoodDistribution(entryList),
            WeeklyAverages = CalculateWeeklyAverages(entryList),
            Insights = GenerateInsights(entryList)
        };

        return analytics;
    }

    public async Task<List<MoodStreakDto>> GetMoodStreaksAsync(int userId)
    {
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        var sortedEntries = entries.OrderBy(e => e.EntryDate).ToList();

        var streaks = new List<MoodStreakDto>();
        
        if (!sortedEntries.Any()) return streaks;

        var currentStreak = new MoodStreakDto
        {
            StartDate = sortedEntries.First().EntryDate,
            EndDate = sortedEntries.First().EntryDate,
            Type = GetStreakType(sortedEntries.First().MoodLevel),
            Count = 1
        };

        for (int i = 1; i < sortedEntries.Count; i++)
        {
            var currentEntry = sortedEntries[i];
            var streakType = GetStreakType(currentEntry.MoodLevel);

            if (streakType == currentStreak.Type && 
                currentEntry.EntryDate == currentStreak.EndDate.AddDays(1))
            {
                // Continue current streak
                currentStreak.EndDate = currentEntry.EntryDate;
                currentStreak.Count++;
            }
            else
            {
                // End current streak and start new one
                if (currentStreak.Count >= 3) // Only include streaks of 3+ days
                {
                    streaks.Add(currentStreak);
                }

                currentStreak = new MoodStreakDto
                {
                    StartDate = currentEntry.EntryDate,
                    EndDate = currentEntry.EntryDate,
                    Type = streakType,
                    Count = 1
                };
            }
        }

        // Add the last streak if it's significant
        if (currentStreak.Count >= 3)
        {
            streaks.Add(currentStreak);
        }

        return streaks.OrderByDescending(s => s.Count).Take(10).ToList();
    }

    public async Task<List<MoodPatternDto>> GetMoodPatternsAsync(int userId)
    {
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        var patterns = new List<MoodPatternDto>();

        // Day of week patterns
        var dayOfWeekPattern = entries
            .GroupBy(e => e.EntryDate.DayOfWeek)
            .Select(g => new MoodPatternDto
            {
                PatternType = "DayOfWeek",
                PatternName = g.Key.ToString(),
                AverageMood = g.Average(e => e.MoodLevel),
                EntryCount = g.Count()
            })
            .OrderByDescending(p => p.AverageMood)
            .ToList();

        patterns.AddRange(dayOfWeekPattern);

        // Monthly patterns
        var monthlyPattern = entries
            .GroupBy(e => e.EntryDate.Month)
            .Select(g => new MoodPatternDto
            {
                PatternType = "Month",
                PatternName = new DateTime(2000, g.Key, 1).ToString("MMMM"),
                AverageMood = g.Average(e => e.MoodLevel),
                EntryCount = g.Count()
            })
            .OrderByDescending(p => p.AverageMood)
            .ToList();

        patterns.AddRange(monthlyPattern);

        return patterns;
    }

    private string CalculateMoodTrend(List<Core.Entities.MoodEntry> entries)
    {
        if (entries.Count < 2) return "Insufficient data";

        var recentEntries = entries.OrderByDescending(e => e.EntryDate).Take(7).ToList();
        var olderEntries = entries.OrderByDescending(e => e.EntryDate).Skip(7).Take(7).ToList();

        if (!olderEntries.Any()) return "Insufficient data";

        var recentAverage = recentEntries.Average(e => e.MoodLevel);
        var olderAverage = olderEntries.Average(e => e.MoodLevel);

        var difference = recentAverage - olderAverage;

        return difference switch
        {
            > 0.5 => "Improving",
            < -0.5 => "Declining",
            _ => "Stable"
        };
    }

    private int CalculateMostCommonMood(List<Core.Entities.MoodEntry> entries)
    {
        return entries
            .GroupBy(e => e.MoodLevel)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;
    }

    private Dictionary<int, int> CalculateMoodDistribution(List<Core.Entities.MoodEntry> entries)
    {
        return entries
            .GroupBy(e => e.MoodLevel)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private List<WeeklyMoodDto> CalculateWeeklyAverages(List<Core.Entities.MoodEntry> entries)
    {
        return entries
            .GroupBy(e => GetWeekOfYear(e.EntryDate))
            .Select(g => new WeeklyMoodDto
            {
                WeekStart = g.Min(e => e.EntryDate),
                WeekEnd = g.Max(e => e.EntryDate),
                AverageMood = g.Average(e => e.MoodLevel),
                EntryCount = g.Count()
            })
            .OrderBy(w => w.WeekStart)
            .ToList();
    }

    private List<string> GenerateInsights(List<Core.Entities.MoodEntry> entries)
    {
        var insights = new List<string>();
        var averageMood = entries.Average(e => e.MoodLevel);

        // General mood insights
        if (averageMood >= 7)
            insights.Add("🌟 You're maintaining a positive mood overall! Keep up the great work.");
        else if (averageMood >= 5)
            insights.Add("😊 Your mood is generally balanced. Consider what activities boost your happiness.");
        else
            insights.Add("💙 Your mood has been lower recently. Consider reaching out for support if needed.");

        // Consistency insights
        var moodVariance = CalculateVariance(entries.Select(e => (double)e.MoodLevel));
        if (moodVariance < 2)
            insights.Add("📊 Your mood has been quite consistent lately.");
        else
            insights.Add("📈 Your mood shows some variation - this is completely normal!");

        // Frequency insights
        if (entries.Count >= 7)
            insights.Add("🎯 Great job staying consistent with your mood tracking!");

        return insights;
    }

    private string GetStreakType(int moodLevel)
    {
        return moodLevel switch
        {
            >= 7 => "Positive",
            <= 4 => "Negative",
            _ => "Neutral"
        };
    }

    private int GetWeekOfYear(DateTime date)
    {
        var jan1 = new DateTime(date.Year, 1, 1);
        var daysOffset = (int)jan1.DayOfWeek;
        var firstWeekDay = jan1.AddDays(-daysOffset);
        var weekNum = ((date - firstWeekDay).Days / 7) + 1;
        return weekNum;
    }

    private double CalculateVariance(IEnumerable<double> values)
    {
        var valueList = values.ToList();
        var average = valueList.Average();
        var sumOfSquares = valueList.Sum(x => Math.Pow(x - average, 2));
        return sumOfSquares / valueList.Count;
    }
}
