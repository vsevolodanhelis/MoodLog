using System.ComponentModel.DataAnnotations;
using MoodLog.Core.DTOs;

namespace MoodLog.Web.Models;

public class MoodEntryViewModel
{
    public int Id { get; set; }
    
    [Required]
    [Range(1, 10, ErrorMessage = "Mood level must be between 1 and 10")]
    [Display(Name = "Mood Level")]
    public int MoodLevel { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Symptoms cannot exceed 500 characters")]
    public string? Symptoms { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Entry Date")]
    [DataType(DataType.Date)]
    public DateTime EntryDate { get; set; } = DateTime.Today;
    
    [Display(Name = "Tags")]
    public List<int>? SelectedTagIds { get; set; } = new List<int>();
    
    public List<MoodTagDto> AvailableTags { get; set; } = new List<MoodTagDto>();
    
    public List<string> TagNames { get; set; } = new List<string>();
    
    public string MoodLevelDescription => GetMoodDescription(MoodLevel);
    
    public string MoodLevelColor => GetMoodColor(MoodLevel);

    private static string GetMoodDescription(int level)
    {
        return level switch
        {
            1 => "Extremely Low",
            2 => "Very Low",
            3 => "Low",
            4 => "Below Average",
            5 => "Average",
            6 => "Above Average",
            7 => "Good",
            8 => "Very Good",
            9 => "Excellent",
            10 => "Outstanding",
            _ => "Unknown"
        };
    }
    
    private static string GetMoodColor(int level)
    {
        return level switch
        {
            <= 2 => "#dc3545", // Red
            <= 4 => "#fd7e14", // Orange
            <= 6 => "#ffc107", // Yellow
            <= 8 => "#20c997", // Teal
            _ => "#28a745"     // Green
        };
    }
}

public class MoodEntryListViewModel
{
    public List<MoodEntryViewModel> Entries { get; set; } = new List<MoodEntryViewModel>();
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double AverageMood { get; set; }
    public int TotalEntries { get; set; }
    public string FilterPeriod { get; set; } = "all";
}



public class MoodTrendData
{
    public DateTime Date { get; set; }
    public double AverageMood { get; set; }
    public string DateLabel => Date.ToString("MMM dd");
}
