using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoodLog.Application.Interfaces;
using MoodLog.Core.DTOs;
using MoodLog.Web.Models;

namespace MoodLog.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IMoodEntryService _moodEntryService;
    private readonly UserManager<IdentityUser> _userManager;

    public DashboardController(
        IMoodEntryService moodEntryService,
        UserManager<IdentityUser> userManager)
    {
        _moodEntryService = moodEntryService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var today = DateTime.Today;
        var todayEntry = await _moodEntryService.GetByDateAsync(userId.Value, today);
        var recentEntries = await _moodEntryService.GetRecentEntriesAsync(userId.Value, 7);

        var viewModel = new DashboardViewModel
        {
            TodayEntry = todayEntry,
            RecentEntries = recentEntries.ToList(),
            HasTodayEntry = todayEntry != null
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogMood(int moodLevel, string? notes = null, string? tags = null)
    {
        // Validate mood level
        if (moodLevel < 1 || moodLevel > 10)
        {
            return Json(new { success = false, message = "Invalid mood level. Must be between 1 and 10." });
        }

        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Json(new { success = false, message = "User not authenticated" });
        }

        try
        {
            var today = DateTime.Today;
            var existingEntry = await _moodEntryService.GetByDateAsync(userId.Value, today);

            if (existingEntry != null)
            {
                // Update existing entry
                var updateDto = new MoodEntryUpdateDto
                {
                    Id = existingEntry.Id,
                    MoodLevel = moodLevel,
                    Notes = notes ?? string.Empty,
                    Symptoms = string.Empty,
                    TagIds = new List<int>() // For now, we'll handle tags separately
                };

                await _moodEntryService.UpdateAsync(updateDto, userId.Value);
            }
            else
            {
                // Create new entry
                var createDto = new MoodEntryCreateDto
                {
                    MoodLevel = moodLevel,
                    Notes = notes ?? string.Empty,
                    Symptoms = string.Empty,
                    EntryDate = today,
                    TagIds = new List<int>() // For now, we'll handle tags separately
                };

                await _moodEntryService.CreateAsync(createDto, userId.Value);
            }

            return Json(new { success = true, message = "Mood logged successfully!" });
        }
        catch (Exception ex)
        {
            // Log the actual error for debugging
            Console.WriteLine($"Error logging mood: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");

            return Json(new { success = false, message = $"Failed to log mood: {ex.Message}" });
        }
    }

    public async Task<IActionResult> Analytics()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-30);

        var entries = await _moodEntryService.GetByDateRangeAsync(userId.Value, startDate, endDate);
        var allEntries = await _moodEntryService.GetByUserIdAsync(userId.Value);

        var viewModel = new AnalyticsViewModel
        {
            TotalEntries = allEntries.Count(),
            AverageMood = allEntries.Any() ? allEntries.Average(e => e.MoodLevel) : 0,
            CurrentStreak = CalculateCurrentStreak(allEntries.OrderByDescending(e => e.EntryDate)),
            MoodDistribution = CalculateMoodDistribution(allEntries),
            MoodTrend = CalculateMoodTrend(entries.OrderBy(e => e.EntryDate))
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Settings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var viewModel = new SettingsViewModel
        {
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ExportData(string format = "json")
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var entries = await _moodEntryService.GetByUserIdAsync(userId.Value);

        if (format.ToLower() == "csv")
        {
            var csv = GenerateCsv(entries);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "mood-data.csv");
        }
        else
        {
            var json = System.Text.Json.JsonSerializer.Serialize(entries, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", "mood-data.json");
        }
    }

    private int CalculateCurrentStreak(IEnumerable<MoodEntryDto> entries)
    {
        var streak = 0;
        var currentDate = DateTime.Today;

        foreach (var entry in entries)
        {
            if (entry.EntryDate.Date == currentDate.Date)
            {
                streak++;
                currentDate = currentDate.AddDays(-1);
            }
            else if (entry.EntryDate.Date < currentDate.Date)
            {
                break;
            }
        }

        return streak;
    }

    private Dictionary<string, int> CalculateMoodDistribution(IEnumerable<MoodEntryDto> entries)
    {
        return entries.GroupBy(e => GetMoodCategory(e.MoodLevel))
                     .ToDictionary(g => g.Key, g => g.Count());
    }

    private List<MoodTrendPoint> CalculateMoodTrend(IEnumerable<MoodEntryDto> entries)
    {
        return entries.GroupBy(e => e.EntryDate.Date)
                     .Select(g => new MoodTrendPoint
                     {
                         Date = g.Key,
                         AverageMood = g.Average(e => e.MoodLevel)
                     })
                     .ToList();
    }

    private string GetMoodCategory(int moodLevel)
    {
        return moodLevel switch
        {
            1 => "Terrible",
            2 => "Very Bad",
            3 => "Bad",
            4 => "Poor",
            5 => "Okay",
            6 => "Fine",
            7 => "Good",
            8 => "Great",
            9 => "Excellent",
            10 => "Amazing",
            _ => "Unknown"
        };
    }

    private string GenerateCsv(IEnumerable<MoodEntryDto> entries)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Date,MoodLevel,Notes,Symptoms,Tags");

        foreach (var entry in entries.OrderBy(e => e.EntryDate))
        {
            csv.AppendLine($"{entry.EntryDate:yyyy-MM-dd},{entry.MoodLevel},\"{entry.Notes}\",\"{entry.Symptoms}\",\"{string.Join(";", entry.TagNames)}\"");
        }

        return csv.ToString();
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;

        // Use a hash of the user ID to create a consistent integer ID
        return Math.Abs(user.Id.GetHashCode());
    }
}
