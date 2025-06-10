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
