using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoodLog.Application.Interfaces;

namespace MoodLog.Web.Controllers;

[Authorize]
public class SeedController : Controller
{
    private readonly IDataSeedingService _dataSeedingService;
    private readonly UserManager<IdentityUser> _userManager;

    public SeedController(
        IDataSeedingService dataSeedingService,
        UserManager<IdentityUser> userManager)
    {
        _dataSeedingService = dataSeedingService;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> SeedMoodData(int entryCount = 45)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            await _dataSeedingService.SeedMoodDataForUserAsync(userId.Value, entryCount);

            return Json(new { 
                success = true, 
                message = $"Successfully seeded {entryCount} mood entries for demonstration purposes." 
            });
        }
        catch (Exception ex)
        {
            return Json(new { 
                success = false, 
                message = $"Failed to seed data: {ex.Message}" 
            });
        }
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;

        // Use a hash of the user ID to create a consistent integer ID
        return Math.Abs(user.Id.GetHashCode());
    }
}
