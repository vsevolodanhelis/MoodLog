using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoodLog.Application.Interfaces;
using MoodLog.Core.DTOs;
using MoodLog.Web.Models;

namespace MoodLog.Web.Controllers;

[Authorize]
public class MoodEntriesController : Controller
{
    private readonly IMoodEntryService _moodEntryService;
    private readonly IMoodTagService _moodTagService;
    private readonly UserManager<IdentityUser> _userManager;

    public MoodEntriesController(
        IMoodEntryService moodEntryService,
        IMoodTagService moodTagService,
        UserManager<IdentityUser> userManager)
    {
        _moodEntryService = moodEntryService;
        _moodTagService = moodTagService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string period = "all")
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null) return Challenge();

        var entries = new List<MoodEntryDto>();
        DateTime? startDate = null;
        DateTime? endDate = null;

        switch (period.ToLower())
        {
            case "week":
                startDate = DateTime.Today.AddDays(-7);
                endDate = DateTime.Today;
                entries = (await _moodEntryService.GetByDateRangeAsync(userId.Value, startDate.Value, endDate.Value)).ToList();
                break;
            case "month":
                startDate = DateTime.Today.AddDays(-30);
                endDate = DateTime.Today;
                entries = (await _moodEntryService.GetByDateRangeAsync(userId.Value, startDate.Value, endDate.Value)).ToList();
                break;
            default:
                entries = (await _moodEntryService.GetByUserIdAsync(userId.Value)).ToList();
                break;
        }

        var averageMood = entries.Any() ? entries.Average(e => e.MoodLevel) : 0;

        var viewModel = new MoodEntryListViewModel
        {
            Entries = entries.Select(MapToViewModel).ToList(),
            StartDate = startDate,
            EndDate = endDate,
            AverageMood = averageMood,
            TotalEntries = entries.Count,
            FilterPeriod = period
        };

        return View(viewModel);
    }

    public async Task<IActionResult> History()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null) return Challenge();

        var entries = await _moodEntryService.GetByUserIdAsync(userId.Value);
        return View(entries.ToList());
    }

    // Redirect for common mistake: /MoodEntries/Dashboard should go to /Dashboard
    public IActionResult Dashboard()
    {
        return RedirectToAction("Index", "Dashboard");
    }


    [HttpPost]
    public async Task<IActionResult> QuickCreate(int moodLevel, int intensity, string? notes = null, string? tags = null)
    {
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
                    Symptoms = $"Intensity: {intensity}/5",
                    TagIds = new List<int>() // For now, we'll handle tags as text
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
                    Symptoms = $"Intensity: {intensity}/5",
                    EntryDate = today,
                    TagIds = new List<int>() // For now, we'll handle tags as text
                };

                await _moodEntryService.CreateAsync(createDto, userId.Value);
            }

            return Json(new { success = true, message = "Mood logged successfully!" });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Failed to log mood. Please try again." });
        }
    }

    public async Task<IActionResult> Create()
    {
        var tags = await _moodTagService.GetAllActiveAsync();

        var viewModel = new MoodEntryViewModel
        {
            EntryDate = DateTime.Today,
            AvailableTags = tags.ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MoodEntryViewModel model)
    {


        if (ModelState.IsValid)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return Challenge();

            try
            {
                var dto = new MoodEntryCreateDto
                {
                    MoodLevel = model.MoodLevel,
                    Notes = model.Notes ?? string.Empty,
                    Symptoms = model.Symptoms ?? string.Empty,
                    EntryDate = model.EntryDate,
                    TagIds = model.SelectedTagIds ?? new List<int>()
                };

                await _moodEntryService.CreateAsync(dto, userId.Value);
                TempData["Success"] = "Mood entry created successfully!";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An unexpected error occurred while saving your mood entry. Please try again.");
            }
        }

        // Reload tags if validation fails
        var tags = await _moodTagService.GetAllActiveAsync();
        model.AvailableTags = tags.ToList();
        
        return View(model);
    }



    public async Task<IActionResult> Edit(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null) return Challenge();

        var entry = await _moodEntryService.GetByIdAsync(id, userId.Value);
        if (entry == null) return NotFound();

        var tags = await _moodTagService.GetAllActiveAsync();
        
        var viewModel = MapToViewModel(entry);
        viewModel.AvailableTags = tags.ToList();
        viewModel.SelectedTagIds = entry.TagIds;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MoodEntryViewModel model)
    {
        if (ModelState.IsValid)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return Challenge();

            try
            {
                var dto = new MoodEntryUpdateDto
                {
                    Id = model.Id,
                    MoodLevel = model.MoodLevel,
                    Notes = model.Notes,
                    Symptoms = model.Symptoms,
                    TagIds = model.SelectedTagIds
                };

                await _moodEntryService.UpdateAsync(dto, userId.Value);
                TempData["Success"] = "Mood entry updated successfully!";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // Reload tags if validation fails
        var tags = await _moodTagService.GetAllActiveAsync();
        model.AvailableTags = tags.ToList();
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null) return Challenge();

        var success = await _moodEntryService.DeleteAsync(id, userId.Value);
        
        if (success)
        {
            TempData["Success"] = "Mood entry deleted successfully!";
        }
        else
        {
            TempData["Error"] = "Failed to delete mood entry.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;

        // Create a consistent integer ID from the string GUID
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

    private static MoodEntryViewModel MapToViewModel(MoodEntryDto dto)
    {
        return new MoodEntryViewModel
        {
            Id = dto.Id,
            MoodLevel = dto.MoodLevel,
            Notes = dto.Notes,
            Symptoms = dto.Symptoms,
            EntryDate = dto.EntryDate,
            SelectedTagIds = dto.TagIds,
            TagNames = dto.TagNames
        };
    }
}
