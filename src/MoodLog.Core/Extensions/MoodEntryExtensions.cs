using MoodLog.Core.DTOs;
using MoodLog.Core.Entities;

namespace MoodLog.Core.Extensions;

public static class MoodEntryExtensions
{
    /// <summary>
    /// Extension method to categorize mood level
    /// </summary>
    public static string GetMoodCategory(this MoodEntryDto entry) => entry.MoodLevel switch
    {
        <= 2 => "Very Low",
        <= 4 => "Low",
        <= 6 => "Moderate",
        <= 8 => "Good",
        _ => "Excellent"
    };

    /// <summary>
    /// Extension method to get mood emoji
    /// </summary>
    public static string GetMoodEmoji(this MoodEntryDto entry) => entry.MoodLevel switch
    {
        1 => "😢",
        2 => "😞",
        3 => "😐",
        4 => "🙂",
        5 => "😊",
        6 => "😄",
        7 => "😁",
        8 => "🤗",
        9 => "🥳",
        10 => "🌟",
        _ => "❓"
    };

    /// <summary>
    /// Extension method to check if mood entry is recent
    /// </summary>
    public static bool IsRecent(this MoodEntryDto entry, int days = 7)
        => entry.EntryDate >= DateTime.Today.AddDays(-days);

    /// <summary>
    /// Extension method to check if mood is positive
    /// </summary>
    public static bool IsPositiveMood(this MoodEntryDto entry)
        => entry.MoodLevel >= 6;

    /// <summary>
    /// Extension method to format entry for display
    /// </summary>
    public static string ToDisplayString(this MoodEntryDto entry)
        => $"{entry.EntryDate:yyyy-MM-dd} - {entry.GetMoodEmoji()} ({entry.MoodLevel}/10) - {entry.GetMoodCategory()}";
}

public static class MoodEntryCollectionExtensions
{
    /// <summary>
    /// Extension method to calculate mood trend
    /// </summary>
    public static string GetMoodTrend(this IEnumerable<MoodEntryDto> entries)
    {
        var entryList = entries.OrderBy(e => e.EntryDate).ToList();
        if (entryList.Count < 2) return "Insufficient data";

        var firstHalf = entryList.Take(entryList.Count / 2);
        var secondHalf = entryList.Skip(entryList.Count / 2);

        var firstAvg = firstHalf.Average(e => e.MoodLevel);
        var secondAvg = secondHalf.Average(e => e.MoodLevel);

        var difference = secondAvg - firstAvg;

        return difference switch
        {
            > 1 => "Improving significantly",
            > 0.5 => "Improving",
            > -0.5 => "Stable",
            > -1 => "Declining",
            _ => "Declining significantly"
        };
    }

    /// <summary>
    /// Extension method to get mood statistics
    /// </summary>
    public static MoodStatistics GetStatistics(this IEnumerable<MoodEntryDto> entries)
    {
        var entryList = entries.ToList();
        if (!entryList.Any())
            return new MoodStatistics();

        return new MoodStatistics
        {
            Count = entryList.Count,
            Average = Math.Round(entryList.Average(e => e.MoodLevel), 2),
            Minimum = entryList.Min(e => e.MoodLevel),
            Maximum = entryList.Max(e => e.MoodLevel),
            StandardDeviation = CalculateStandardDeviation(entryList.Select(e => e.MoodLevel)),
            MostCommonMood = entryList.GroupBy(e => e.MoodLevel)
                .OrderByDescending(g => g.Count())
                .First().Key,
            PositiveMoodPercentage = Math.Round(
                (double)entryList.Count(e => e.IsPositiveMood()) / entryList.Count * 100, 2)
        };
    }

    /// <summary>
    /// Extension method to filter entries by mood range
    /// </summary>
    public static IEnumerable<MoodEntryDto> FilterByMoodRange(this IEnumerable<MoodEntryDto> entries, int minMood, int maxMood)
        => entries.Where(e => e.MoodLevel >= minMood && e.MoodLevel <= maxMood);

    /// <summary>
    /// Extension method to group entries by week
    /// </summary>
    public static IEnumerable<IGrouping<int, MoodEntryDto>> GroupByWeek(this IEnumerable<MoodEntryDto> entries)
        => entries.GroupBy(e => GetWeekOfYear(e.EntryDate));

    /// <summary>
    /// Extension method to group entries by month
    /// </summary>
    public static IEnumerable<IGrouping<string, MoodEntryDto>> GroupByMonth(this IEnumerable<MoodEntryDto> entries)
        => entries.GroupBy(e => e.EntryDate.ToString("yyyy-MM"));

    /// <summary>
    /// Extension method to find mood patterns
    /// </summary>
    public static Dictionary<DayOfWeek, double> GetDayOfWeekPattern(this IEnumerable<MoodEntryDto> entries)
        => entries.GroupBy(e => e.EntryDate.DayOfWeek)
            .ToDictionary(g => g.Key, g => Math.Round(g.Average(e => e.MoodLevel), 2));

    /// <summary>
    /// Extension method to calculate current streak
    /// </summary>
    public static int CalculateCurrentStreak(this IEnumerable<MoodEntryDto> entries)
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

    private static double CalculateStandardDeviation(IEnumerable<int> values)
    {
        var valueList = values.ToList();
        if (valueList.Count < 2) return 0;

        var mean = valueList.Average();
        var variance = valueList.Sum(x => Math.Pow(x - mean, 2)) / valueList.Count;
        return Math.Round(Math.Sqrt(variance), 2);
    }

    private static int GetWeekOfYear(DateTime date)
    {
        var jan1 = new DateTime(date.Year, 1, 1);
        var daysOffset = (int)jan1.DayOfWeek;
        var firstWeekDay = jan1.AddDays(-daysOffset);
        return ((date - firstWeekDay).Days / 7) + 1;
    }
}

public class MoodStatistics
{
    public int Count { get; set; }
    public double Average { get; set; }
    public int Minimum { get; set; }
    public int Maximum { get; set; }
    public double StandardDeviation { get; set; }
    public int MostCommonMood { get; set; }
    public double PositiveMoodPercentage { get; set; }
}

// Generic delegate for data processing
public delegate T DataProcessor<T>(IEnumerable<MoodEntryDto> data);

// Functional programming helpers
public static class FunctionalHelpers
{
    /// <summary>
    /// Higher-order function that creates a mood filter delegate
    /// </summary>
    public static Func<MoodEntryDto, bool> CreateMoodFilter(int threshold, bool above = true)
        => above ? entry => entry.MoodLevel >= threshold : entry => entry.MoodLevel <= threshold;

    /// <summary>
    /// Compose multiple filters using delegates
    /// </summary>
    public static Func<MoodEntryDto, bool> ComposeFilters(params Func<MoodEntryDto, bool>[] filters)
        => entry => filters.All(filter => filter(entry));

    /// <summary>
    /// Create a data processor delegate for mood analysis
    /// </summary>
    public static DataProcessor<double> CreateAverageProcessor()
        => data => data.Any() ? data.Average(e => e.MoodLevel) : 0;

    /// <summary>
    /// Create a data processor delegate for trend analysis
    /// </summary>
    public static DataProcessor<string> CreateTrendProcessor()
        => data => data.GetMoodTrend();
}
