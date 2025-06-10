using System.ComponentModel.DataAnnotations;
using MoodLog.Core.DTOs;

namespace MoodLog.Web.Models;

public class MoodTagViewModel
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters")]
    [Display(Name = "Tag Name")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string Description { get; set; } = string.Empty;
    
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    [Display(Name = "Color")]
    public string Color { get; set; } = "#007bff";
    
    [Display(Name = "System Tag")]
    public bool IsSystemTag { get; set; }
    
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class MoodTagListViewModel
{
    public List<MoodTagViewModel> SystemTags { get; set; } = new List<MoodTagViewModel>();
    public List<MoodTagViewModel> UserTags { get; set; } = new List<MoodTagViewModel>();
    public bool IsAdmin { get; set; }
}


