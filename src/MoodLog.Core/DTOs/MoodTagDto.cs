using System.ComponentModel.DataAnnotations;

namespace MoodLog.Core.DTOs;

public class MoodTagDto
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string Description { get; set; } = string.Empty;
    
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string Color { get; set; } = "#007bff";
    
    public bool IsSystemTag { get; set; }
    
    public bool IsActive { get; set; } = true;
}

public class MoodTagCreateDto
{
    [Required]
    [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string Description { get; set; } = string.Empty;
    
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string Color { get; set; } = "#007bff";
}

public class MoodTagUpdateDto
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string Description { get; set; } = string.Empty;
    
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string Color { get; set; } = "#007bff";
    
    public bool IsActive { get; set; } = true;
}
