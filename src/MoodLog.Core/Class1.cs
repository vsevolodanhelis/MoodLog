using System.ComponentModel.DataAnnotations;

namespace MoodLog.Core.Entities;

public class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(450)] // Same as IdentityUser.Id max length
    public string IdentityUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    public bool IsAdmin { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<MoodEntry> MoodEntries { get; set; } = new List<MoodEntry>();
}
