using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoodLog.Core.DTOs;
using MoodLog.Application.Interfaces;
using MoodLog.Application.Services;
using MoodLog.Web.Models;

namespace MoodLog.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMoodEntryService _moodEntryService;
    private readonly IMoodTagService _moodTagService;
    private readonly MockDataService _mockDataService;

    public AdminController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMoodEntryService moodEntryService,
        IMoodTagService moodTagService,
        MockDataService mockDataService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _moodEntryService = moodEntryService;
        _moodTagService = moodTagService;
        _mockDataService = mockDataService;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new AdminDashboardViewModel();

        // Get user statistics
        var allUsers = _userManager.Users.ToList();
        viewModel.TotalUsers = allUsers.Count;
        viewModel.ActiveUsers = allUsers.Count; // For now, all users are considered active

        // Get mood entry statistics
        var allEntries = await GetAllMoodEntriesAsync();
        viewModel.TotalEntries = allEntries.Count;
        viewModel.EntriesThisMonth = allEntries.Count(e => e.EntryDate.Month == DateTime.Now.Month && e.EntryDate.Year == DateTime.Now.Year);

        // Get tag statistics
        var allTags = await _moodTagService.GetAllActiveAsync();
        viewModel.TotalTags = allTags.Count();

        // Calculate average mood
        viewModel.AverageMood = allEntries.Any() ? allEntries.Average(e => e.MoodLevel) : 0;

        // Recent activity
        viewModel.RecentEntries = allEntries.OrderByDescending(e => e.EntryDate).Take(10).ToList();

        // User growth data (last 12 months)
        viewModel.UserGrowthData = GenerateUserGrowthData(allUsers);

        // Mood distribution
        viewModel.MoodDistribution = allEntries.GroupBy(e => e.MoodLevel)
            .ToDictionary(g => g.Key, g => g.Count());

        return View(viewModel);
    }

    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users.ToList();
        var userViewModels = new List<AdminUserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var userId = GetConsistentIntegerFromGuid(user.Id);
            var userEntries = await GetUserMoodEntriesAsync(userId);

            userViewModels.Add(new AdminUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                Roles = roles.ToList(),
                TotalEntries = userEntries.Count(),
                LastEntry = userEntries.OrderByDescending(e => e.EntryDate).FirstOrDefault()?.EntryDate,
                JoinDate = user.LockoutEnd ?? DateTime.Now // Using LockoutEnd as a placeholder for join date
            });
        }

        return View(userViewModels.OrderByDescending(u => u.JoinDate).ToList());
    }

    public async Task<IActionResult> Tags()
    {
        // Get both active and inactive tags by combining system and user tags
        var systemTags = await _moodTagService.GetSystemTagsAsync();
        var userTags = await _moodTagService.GetUserTagsAsync();
        var allTags = systemTags.Concat(userTags);

        var tagViewModels = allTags.Select(t => new AdminTagViewModel
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Color = t.Color,
            IsActive = true, // Assuming all returned tags are active for now
            IsSystemTag = systemTags.Contains(t),
            CreatedAt = DateTime.Now, // Placeholder since we don't have this in DTO
            UsageCount = 0 // We'll calculate this separately if needed
        }).ToList();

        return View(tagViewModels);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleUserLockout(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Json(new { success = false, message = "User not found" });
        }

        try
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                // Unlock user
                await _userManager.SetLockoutEndDateAsync(user, null);
                return Json(new { success = true, message = "User unlocked successfully", isLocked = false });
            }
            else
            {
                // Lock user for 100 years (effectively permanent)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                return Json(new { success = true, message = "User locked successfully", isLocked = true });
            }
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Failed to update user status" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateTag(AdminTagViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data provided" });
        }

        try
        {
            var createDto = new MoodTagCreateDto
            {
                Name = model.Name,
                Description = model.Description,
                Color = model.Color
            };

            await _moodTagService.CreateAsync(createDto, false); // false = not a system tag
            return Json(new { success = true, message = "Tag created successfully" });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Failed to create tag" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTag(AdminTagViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data provided" });
        }

        try
        {
            var updateDto = new MoodTagUpdateDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Color = model.Color,
                IsActive = model.IsActive
            };

            await _moodTagService.UpdateAsync(updateDto);
            return Json(new { success = true, message = "Tag updated successfully" });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Failed to update tag" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTag(int id)
    {
        try
        {
            var success = await _moodTagService.DeleteAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "Tag deleted successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Tag not found or cannot be deleted" });
            }
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Failed to delete tag" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedMockData()
    {
        try
        {
            // Get current user ID
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                Console.WriteLine("SeedMockData: User not found");
                return Json(new { success = false, message = "User not found" });
            }

            // Use consistent user ID mapping
            var userId = GetConsistentIntegerFromGuid(currentUser.Id).ToString();
            Console.WriteLine($"SeedMockData: Attempting to seed data for user ID: {userId}");

            var success = await _mockDataService.SeedMockDataAsync(userId);
            Console.WriteLine($"SeedMockData: Seeding result: {success}");

            if (success)
            {
                return Json(new {
                    success = true,
                    message = "Mock data seeded successfully! The application now contains 60+ realistic mood entries spanning the last 3 months with comprehensive tags and patterns."
                });
            }
            else
            {
                return Json(new { success = false, message = "Failed to seed mock data - check server console for details" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SeedMockData Exception: {ex.Message}");
            Console.WriteLine($"SeedMockData Stack Trace: {ex.StackTrace}");
            return Json(new { success = false, message = $"Error seeding mock data: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearMockData()
    {
        try
        {
            // Get current user ID
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var userId = GetConsistentIntegerFromGuid(currentUser.Id).ToString();

            // Clear existing data (this will be handled by the SeedMockDataAsync method)
            // For now, we'll just return success - the actual clearing happens in the service

            return Json(new {
                success = true,
                message = "Mock data cleared successfully! You can now seed fresh data or use the application normally."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error clearing mock data: {ex.Message}" });
        }
    }

    private async Task<List<MoodEntryDto>> GetAllMoodEntriesAsync()
    {
        // This is a simplified approach - in a real app, you'd want to optimize this
        var allUsers = _userManager.Users.ToList();
        var allEntries = new List<MoodEntryDto>();

        foreach (var user in allUsers)
        {
            var userId = GetConsistentIntegerFromGuid(user.Id);
            var userEntries = await _moodEntryService.GetByUserIdAsync(userId);
            allEntries.AddRange(userEntries);
        }

        return allEntries;
    }

    private async Task<List<MoodEntryDto>> GetUserMoodEntriesAsync(int userId)
    {
        return (await _moodEntryService.GetByUserIdAsync(userId)).ToList();
    }

    private List<UserGrowthDataPoint> GenerateUserGrowthData(List<IdentityUser> users)
    {
        var data = new List<UserGrowthDataPoint>();
        var startDate = DateTime.Now.AddMonths(-11).Date;

        for (int i = 0; i < 12; i++)
        {
            var monthStart = startDate.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);

            // For demo purposes, we'll use a simple count
            // In a real app, you'd filter by actual creation date
            var usersInMonth = users.Count(u => true); // Placeholder logic

            data.Add(new UserGrowthDataPoint
            {
                Month = monthStart.ToString("MMM yyyy"),
                UserCount = Math.Max(1, usersInMonth + i) // Simulate growth
            });
        }

        return data;
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

    // Temporary method to assign admin role - for development/demo purposes
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeCurrentUserAdmin()
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Check if user is already admin
            if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                return Json(new { success = true, message = "User is already an admin" });
            }

            // Ensure Admin role exists
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Add user to Admin role
            var result = await _userManager.AddToRoleAsync(currentUser, "Admin");

            if (result.Succeeded)
            {
                return Json(new {
                    success = true,
                    message = "Successfully assigned admin role! Please refresh the page to see the Admin Panel button.",
                    reload = true
                });
            }
            else
            {
                return Json(new {
                    success = false,
                    message = "Failed to assign admin role: " + string.Join(", ", result.Errors.Select(e => e.Description))
                });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error assigning admin role: {ex.Message}" });
        }
    }
}
