using System.ComponentModel.DataAnnotations;

namespace MoodLog.Core.DTOs;

public class MoodEntryDto
{
    public int Id { get; set; }
    
    [Required]
    [Range(1, 10, ErrorMessage = "Mood level must be between 1 and 10")]
    public int MoodLevel { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Symptoms cannot exceed 500 characters")]
    public string Symptoms { get; set; } = string.Empty;
    
    [Required]
    public DateTime EntryDate { get; set; } = DateTime.Today;
    
    public List<int> TagIds { get; set; } = new List<int>();
    
    public List<string> TagNames { get; set; } = new List<string>();
}

public class MoodEntryCreateDto
{
    [Required]
    [Range(1, 10, ErrorMessage = "Mood level must be between 1 and 10")]
    public int MoodLevel { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Symptoms cannot exceed 500 characters")]
    public string Symptoms { get; set; } = string.Empty;
    
    [Required]
    public DateTime EntryDate { get; set; } = DateTime.Today;
    
    public List<int> TagIds { get; set; } = new List<int>();
}

public class MoodEntryUpdateDto
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [Range(1, 10, ErrorMessage = "Mood level must be between 1 and 10")]
    public int MoodLevel { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Symptoms cannot exceed 500 characters")]
    public string Symptoms { get; set; } = string.Empty;
    
    public List<int> TagIds { get; set; } = new List<int>();
}

public class MoodAnalyticsDto
{
    public double AverageMood { get; set; }
    public int TotalEntries { get; set; }
    public string MoodTrend { get; set; } = string.Empty;
    public Core.Entities.MoodEntry? BestDay { get; set; }
    public Core.Entities.MoodEntry? WorstDay { get; set; }
    public int MostCommonMood { get; set; }
    public Dictionary<int, int> MoodDistribution { get; set; } = new();
    public List<WeeklyMoodDto> WeeklyAverages { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

public class WeeklyMoodDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public double AverageMood { get; set; }
    public int EntryCount { get; set; }
}

public class MoodStreakDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; } = string.Empty; // "Positive", "Negative", "Neutral"
    public int Count { get; set; }
}

public class MoodPatternDto
{
    public string PatternType { get; set; } = string.Empty; // "DayOfWeek", "Month", etc.
    public string PatternName { get; set; } = string.Empty;
    public double AverageMood { get; set; }
    public int EntryCount { get; set; }
}
