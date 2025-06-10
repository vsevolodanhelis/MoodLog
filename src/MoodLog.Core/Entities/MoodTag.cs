using System.ComponentModel.DataAnnotations;

namespace MoodLog.Core.Entities;

public class MoodTag
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(7)] // For hex color codes like #FF5733
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string Color { get; set; } = "#007bff";
    
    public bool IsSystemTag { get; set; } = false; // Admin-managed tags
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<MoodEntryTag> MoodEntryTags { get; set; } = new List<MoodEntryTag>();
}
