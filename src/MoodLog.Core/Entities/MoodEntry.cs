using System.ComponentModel.DataAnnotations;

namespace MoodLog.Core.Entities;

public class MoodEntry
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int MoodLevel { get; set; } // 1-10 scale
    
    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Symptoms { get; set; } = string.Empty;
    
    public DateTime EntryDate { get; set; } = DateTime.UtcNow.Date;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<MoodEntryTag> MoodEntryTags { get; set; } = new List<MoodEntryTag>();
}
