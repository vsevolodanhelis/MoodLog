using System.ComponentModel.DataAnnotations;
using MoodLog.Core.DTOs;

namespace MoodLog.Web.Models;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;
}

public class DashboardViewModel
{
    public MoodEntryDto? TodayEntry { get; set; }
    public List<MoodEntryDto> RecentEntries { get; set; } = new();
    public bool HasTodayEntry { get; set; }
}

public class AnalyticsViewModel
{
    public int TotalEntries { get; set; }
    public double AverageMood { get; set; }
    public int CurrentStreak { get; set; }
    public Dictionary<string, int> MoodDistribution { get; set; } = new();
    public List<MoodTrendPoint> MoodTrend { get; set; } = new();
}

public class MoodTrendPoint
{
    public DateTime Date { get; set; }
    public double AverageMood { get; set; }
}

public class SettingsViewModel
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool DarkMode { get; set; }
    public bool EmailNotifications { get; set; }
}

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalEntries { get; set; }
    public int EntriesThisMonth { get; set; }
    public int TotalTags { get; set; }
    public double AverageMood { get; set; }
    public List<MoodLog.Core.DTOs.MoodEntryDto> RecentEntries { get; set; } = new();
    public List<UserGrowthDataPoint> UserGrowthData { get; set; } = new();
    public Dictionary<int, int> MoodDistribution { get; set; } = new();
}

public class UserGrowthDataPoint
{
    public string Month { get; set; } = string.Empty;
    public int UserCount { get; set; }
}

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public List<string> Roles { get; set; } = new();
    public int TotalEntries { get; set; }
    public DateTime? LastEntry { get; set; }
    public DateTimeOffset JoinDate { get; set; }
    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.UtcNow;
}

public class AdminTagViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#667eea";
    public bool IsActive { get; set; } = true;
    public bool IsSystemTag { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UsageCount { get; set; }
}
