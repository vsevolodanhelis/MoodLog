using Microsoft.ML.Data;

namespace MoodLog.Application.Models.ML;

public class MoodPredictionData
{
    [LoadColumn(0)]
    public float DayOfWeek { get; set; }

    [LoadColumn(1)]
    public float HourOfDay { get; set; }

    [LoadColumn(2)]
    public float DaysSinceLastEntry { get; set; }

    [LoadColumn(3)]
    public float AverageMoodLast7Days { get; set; }

    [LoadColumn(4)]
    public float AverageMoodLast30Days { get; set; }

    [LoadColumn(5)]
    public float HasStressTag { get; set; }

    [LoadColumn(6)]
    public float HasHappyTag { get; set; }

    [LoadColumn(7)]
    public float HasAnxiousTag { get; set; }

    [LoadColumn(8)]
    public float HasTiredTag { get; set; }

    [LoadColumn(9)]
    public float WeekendFactor { get; set; }

    [LoadColumn(10)]
    public float MoodTrend { get; set; } // Slope of last 7 days

    [LoadColumn(11)]
    public float SeasonalFactor { get; set; } // Month-based seasonal adjustment

    [LoadColumn(12)]
    [ColumnName("Label")]
    public float MoodLevel { get; set; }
}

public class MoodPredictionResult
{
    [ColumnName("Score")]
    public float PredictedMoodLevel { get; set; }
}

public class MoodInsight
{
    public string InsightText { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string Category { get; set; } = string.Empty; // "prediction", "pattern", "recommendation"
    public DateTime GeneratedAt { get; set; }
    public string[] SupportingData { get; set; } = Array.Empty<string>();
}

public class PatternDetectionResult
{
    public string PatternType { get; set; } = string.Empty; // "trigger", "improvement", "cycle"
    public string Description { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string[] Examples { get; set; } = Array.Empty<string>();
    public string Recommendation { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}

public class WeeklySummaryData
{
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public float AverageMood { get; set; }
    public float MoodVariability { get; set; }
    public string[] DominantTags { get; set; } = Array.Empty<string>();
    public string[] PositiveTriggers { get; set; } = Array.Empty<string>();
    public string[] NegativeTriggers { get; set; } = Array.Empty<string>();
    public int TotalEntries { get; set; }
    public string MoodTrend { get; set; } = string.Empty; // "improving", "declining", "stable"
}
