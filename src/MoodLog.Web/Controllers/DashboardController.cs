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

        // Auto-seed comprehensive mock data for admin users
        await EnsureAdminMockDataAsync(userId.Value);

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

                var updatedEntry = await _moodEntryService.UpdateAsync(updateDto, userId.Value);
                return Json(new { success = true, message = "Mood updated successfully!" });
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

                var newEntry = await _moodEntryService.CreateAsync(createDto, userId.Value);
                return Json(new { success = true, message = "Mood logged successfully!" });
            }
        }
        catch (InvalidOperationException ex)
        {
            // Handle specific business logic errors
            Console.WriteLine($"Business logic error logging mood: {ex.Message}");
            return Json(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Handle authorization errors
            Console.WriteLine($"Authorization error logging mood: {ex.Message}");
            return Json(new { success = false, message = "Access denied" });
        }
        catch (Exception ex)
        {
            // Log the actual error for debugging
            Console.WriteLine($"Unexpected error logging mood: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine($"User ID: {userId}");
            Console.WriteLine($"Mood Level: {moodLevel}");
            Console.WriteLine($"Notes: {notes ?? "null"}");
            Console.WriteLine($"Tags: {tags ?? "null"}");

            return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
        }
    }

    public async Task<IActionResult> Analytics()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Auto-seed comprehensive mock data for admin users
        await EnsureAdminMockDataAsync(userId.Value);

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
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Validate input
            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                errors["email"] = "Email is required";
            }
            else if (!IsValidEmail(request.Email))
            {
                errors["email"] = "Please enter a valid email address";
            }
            else if (request.Email != user.Email)
            {
                // Check if email is already taken
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    errors["email"] = "This email address is already in use";
                }
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                errors["username"] = "Username is required";
            }
            else if (request.Username.Length < 3)
            {
                errors["username"] = "Username must be at least 3 characters long";
            }
            else if (request.Username.Length > 50)
            {
                errors["username"] = "Username must be less than 50 characters";
            }
            else if (!IsValidUsername(request.Username))
            {
                errors["username"] = "Username can only contain letters, numbers, dots, underscores, and hyphens";
            }
            else if (request.Username != user.UserName)
            {
                // Check if username is already taken
                var existingUser = await _userManager.FindByNameAsync(request.Username);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    errors["username"] = "This username is already taken";
                }
            }

            if (errors.Any())
            {
                return Json(new { success = false, message = "Validation failed", errors = errors });
            }

            // Update user information
            bool needsUpdate = false;

            if (user.Email != request.Email)
            {
                user.Email = request.Email;
                user.NormalizedEmail = _userManager.NormalizeEmail(request.Email);
                needsUpdate = true;
            }

            if (user.UserName != request.Username)
            {
                user.UserName = request.Username;
                user.NormalizedUserName = _userManager.NormalizeName(request.Username);
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"Failed to update profile: {errorMessages}" });
                }
            }

            return Json(new { success = true, message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            // Log the exception in a real application
            Console.WriteLine($"Error updating profile: {ex.Message}");
            return Json(new { success = false, message = "An error occurred while updating your profile" });
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidUsername(string username)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$");
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

        // Create a consistent integer ID from the string GUID
        // This ensures the same user always gets the same integer ID
        return GetConsistentIntegerFromGuid(user.Id);
    }

    private static int GetConsistentIntegerFromGuid(string guidString)
    {
        // Parse the GUID and use a consistent method to convert to integer
        if (Guid.TryParse(guidString, out var guid))
        {
            // Use the first 4 bytes of the GUID to create a consistent integer
            var bytes = guid.ToByteArray();
            return Math.Abs(BitConverter.ToInt32(bytes, 0));
        }

        // Fallback to a consistent hash if not a valid GUID
        return Math.Abs(guidString.GetHashCode(StringComparison.Ordinal));
    }

    private async Task EnsureAdminMockDataAsync(int userId)
    {
        try
        {
            // Check if user is admin - need to get the current user properly
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                Console.WriteLine($"EnsureAdminMockDataAsync: User is not admin or not found");
                return; // Only seed data for admin users
            }

            // Check if admin already has sufficient mock data (60+ entries)
            var existingEntries = await _moodEntryService.GetByUserIdAsync(userId);
            Console.WriteLine($"EnsureAdminMockDataAsync: User {userId} has {existingEntries.Count()} existing entries");

            if (existingEntries.Count() >= 60)
            {
                Console.WriteLine($"EnsureAdminMockDataAsync: Admin already has sufficient data ({existingEntries.Count()} entries)");
                return; // Admin already has comprehensive data
            }

            Console.WriteLine($"EnsureAdminMockDataAsync: Generating mock data for admin user {userId}");
            // Generate comprehensive mock data for admin showcase
            await GenerateAdminMockDataAsync(userId);
            Console.WriteLine($"EnsureAdminMockDataAsync: Mock data generation completed");
        }
        catch (Exception ex)
        {
            // Log error but don't break the dashboard
            Console.WriteLine($"Error seeding admin mock data: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task GenerateAdminMockDataAsync(int userId)
    {
        var random = new Random(userId); // Consistent seed for reproducible data
        var startDate = DateTime.Today.AddDays(-90); // 3 months of data
        var endDate = DateTime.Today.AddDays(-1); // Up to yesterday

        // Generate realistic mood patterns
        var moodEntries = new List<MoodEntryCreateDto>();
        var currentDate = startDate;
        var baselineMood = 6.0; // Start with slightly positive baseline
        var trendDirection = 1; // Gradual improvement over time
        var weekendBoost = 0.5; // Weekends tend to be slightly better

        while (currentDate <= endDate)
        {
            // Skip some days randomly (75% chance of entry, higher for admin showcase)
            if (random.NextDouble() < 0.75)
            {
                var moodEntry = GenerateRealisticMoodEntry(currentDate, baselineMood, random, trendDirection, weekendBoost);
                moodEntries.Add(moodEntry);

                // Gradually improve mood over time with natural variations
                if (random.NextDouble() < 0.15) // 15% chance to adjust trend
                {
                    trendDirection = random.NextDouble() < 0.7 ? 1 : -1;
                }

                // Adjust baseline mood slightly with overall upward trend
                var adjustment = (random.NextDouble() - 0.4) * 0.3; // Slight positive bias
                baselineMood = Math.Max(3.0, Math.Min(8.5, baselineMood + adjustment));
            }

            currentDate = currentDate.AddDays(1);
        }

        // Create mood entries
        foreach (var entry in moodEntries)
        {
            try
            {
                await _moodEntryService.CreateAsync(entry, userId);
            }
            catch (InvalidOperationException)
            {
                // Entry for this date already exists, skip
                continue;
            }
        }
    }

    private MoodEntryCreateDto GenerateRealisticMoodEntry(DateTime date, double baselineMood, Random random, int trendDirection, double weekendBoost)
    {
        // Apply weekend boost
        var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        var moodAdjustment = isWeekend ? weekendBoost : 0;

        // Add some natural variation
        var variation = (random.NextDouble() - 0.5) * 2; // -1 to +1
        var finalMood = baselineMood + moodAdjustment + variation;

        // Clamp to valid range and round
        var moodLevel = Math.Max(1, Math.Min(10, (int)Math.Round(finalMood)));

        // Generate realistic time of day
        var timeOfDay = GenerateRealisticTimeOfDay(random, isWeekend);
        var entryDateTime = date.Add(timeOfDay);

        // Generate contextual notes and tags based on mood and day
        var (notes, tags) = GenerateContextualContent(moodLevel, date, isWeekend, random);

        return new MoodEntryCreateDto
        {
            MoodLevel = moodLevel,
            Notes = notes,
            Symptoms = GenerateSymptoms(moodLevel, random),
            EntryDate = entryDateTime,
            TagIds = new List<int>() // Tags will be handled as text for now
        };
    }

    private TimeSpan GenerateRealisticTimeOfDay(Random random, bool isWeekend)
    {
        if (isWeekend)
        {
            // Weekends: later wake up, more varied times
            var hour = random.Next(9, 22); // 9 AM to 10 PM
            var minute = random.Next(0, 60);
            return new TimeSpan(hour, minute, 0);
        }
        else
        {
            // Weekdays: more structured times
            var timeSlots = new[] { 8, 12, 18, 21 }; // Morning, lunch, evening, night
            var weights = new[] { 0.2, 0.3, 0.4, 0.1 }; // Prefer evening entries

            var selectedSlot = WeightedRandomChoice(timeSlots, weights, random);
            var hour = selectedSlot + random.Next(-1, 2); // ±1 hour variation
            var minute = random.Next(0, 60);

            return new TimeSpan(Math.Max(6, Math.Min(23, hour)), minute, 0);
        }
    }

    private T WeightedRandomChoice<T>(T[] items, double[] weights, Random random)
    {
        var totalWeight = weights.Sum();
        var randomValue = random.NextDouble() * totalWeight;
        var currentWeight = 0.0;

        for (int i = 0; i < items.Length; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
                return items[i];
        }

        return items[^1]; // Fallback to last item
    }

    private (string notes, string tags) GenerateContextualContent(int moodLevel, DateTime date, bool isWeekend, Random random)
    {
        var notes = GenerateRealisticNotes(moodLevel, date, isWeekend, random);
        var tags = GenerateRealisticTags(moodLevel, isWeekend, random);
        return (notes, tags);
    }

    private string GenerateRealisticNotes(int moodLevel, DateTime date, bool isWeekend, Random random)
    {
        var noteTemplates = moodLevel switch
        {
            <= 3 => new[]
            {
                "Feeling overwhelmed today. Work stress is getting to me.",
                "Had a difficult conversation that left me feeling down.",
                "Struggling with motivation and energy levels.",
                "Feeling anxious about upcoming deadlines.",
                "Not sleeping well, which is affecting my mood.",
                "Dealing with some personal challenges right now."
            },
            4 or 5 => new[]
            {
                "Okay day overall, nothing particularly exciting.",
                "Feeling neutral, just going through the motions.",
                "Some ups and downs, but managing okay.",
                "Work was fine, nothing special to report.",
                "Feeling a bit tired but stable.",
                "Average day, looking forward to the weekend."
            },
            6 or 7 => new[]
            {
                "Good day! Accomplished several tasks and feeling productive.",
                "Had a nice conversation with a friend that lifted my spirits.",
                "Feeling grateful for the small things today.",
                "Work went well and I'm feeling positive about progress.",
                "Enjoyed some time outdoors, which always helps my mood.",
                "Feeling more energetic and optimistic than usual."
            },
            _ => new[]
            {
                "Amazing day! Everything seemed to go right.",
                "Feeling incredibly grateful and happy today.",
                "Had a breakthrough at work that I'm excited about.",
                "Spent quality time with loved ones and feeling fulfilled.",
                "Accomplished something I've been working towards for a while.",
                "Feeling confident and ready to take on new challenges."
            }
        };

        var weekendNotes = new[]
        {
            "Enjoying some well-deserved downtime.",
            "Spent time on hobbies that bring me joy.",
            "Had a relaxing morning with no rush.",
            "Caught up with friends and family.",
            "Took time for self-care and reflection."
        };

        if (isWeekend && random.NextDouble() < 0.3)
        {
            return weekendNotes[random.Next(weekendNotes.Length)];
        }

        return noteTemplates[random.Next(noteTemplates.Length)];
    }

    private string GenerateRealisticTags(int moodLevel, bool isWeekend, Random random)
    {
        var baseTags = new List<string>();

        // Mood-based tags
        if (moodLevel <= 3)
        {
            var negativeTags = new[] { "stressed", "anxious", "tired", "overwhelmed", "sad", "frustrated" };
            baseTags.Add(negativeTags[random.Next(negativeTags.Length)]);
        }
        else if (moodLevel >= 7)
        {
            var positiveTags = new[] { "happy", "energetic", "grateful", "accomplished", "excited", "confident" };
            baseTags.Add(positiveTags[random.Next(positiveTags.Length)]);
        }

        // Context-based tags
        if (isWeekend)
        {
            var weekendTags = new[] { "weekend", "relaxing", "family", "hobbies", "social" };
            if (random.NextDouble() < 0.6)
                baseTags.Add(weekendTags[random.Next(weekendTags.Length)]);
        }
        else
        {
            var weekdayTags = new[] { "work", "productive", "busy", "meetings", "deadlines" };
            if (random.NextDouble() < 0.7)
                baseTags.Add(weekdayTags[random.Next(weekdayTags.Length)]);
        }

        // Activity-based tags
        var activityTags = new[] { "exercise", "sleep", "food", "social", "creative", "learning", "nature" };
        if (random.NextDouble() < 0.4)
            baseTags.Add(activityTags[random.Next(activityTags.Length)]);

        return string.Join(", ", baseTags.Distinct());
    }

    private string GenerateSymptoms(int moodLevel, Random random)
    {
        var symptoms = moodLevel switch
        {
            <= 3 => new[] { "Low energy", "Difficulty concentrating", "Restless sleep", "Loss of appetite", "Feeling isolated" },
            4 or 5 => new[] { "Mild fatigue", "Occasional worry", "Normal sleep", "Regular appetite", "Social as usual" },
            6 or 7 => new[] { "Good energy", "Clear thinking", "Restful sleep", "Healthy appetite", "Socially engaged" },
            _ => new[] { "High energy", "Sharp focus", "Excellent sleep", "Great appetite", "Very social" }
        };

        var selectedSymptoms = symptoms.OrderBy(x => random.Next()).Take(random.Next(1, 3));
        return string.Join(", ", selectedSymptoms);
    }
}
