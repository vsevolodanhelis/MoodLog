using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoodLog.Application.Interfaces;
using MoodLog.Core.DTOs;
using MoodLog.Web.Models;

namespace MoodLog.Web.Controllers;

[Authorize]
public class MoodTagsController : Controller
{
    private readonly IMoodTagService _moodTagService;
    private readonly UserManager<IdentityUser> _userManager;

    public MoodTagsController(IMoodTagService moodTagService, UserManager<IdentityUser> userManager)
    {
        _moodTagService = moodTagService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var systemTags = await _moodTagService.GetSystemTagsAsync();
        var userTags = await _moodTagService.GetUserTagsAsync();
        var isAdmin = await IsCurrentUserAdminAsync();

        var viewModel = new MoodTagListViewModel
        {
            SystemTags = systemTags.Select(MapToViewModel).ToList(),
            UserTags = userTags.Select(MapToViewModel).ToList(),
            IsAdmin = isAdmin
        };

        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View(new MoodTagViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(MoodTagViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var dto = new MoodTagCreateDto
                {
                    Name = model.Name,
                    Description = model.Description,
                    Color = model.Color
                };

                await _moodTagService.CreateAsync(dto, model.IsSystemTag);
                TempData["Success"] = "Tag created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        return View(model);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var tag = await _moodTagService.GetByIdAsync(id);
        if (tag == null) return NotFound();

        var viewModel = MapToViewModel(tag);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(MoodTagViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var dto = new MoodTagUpdateDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    Color = model.Color,
                    IsActive = model.IsActive
                };

                await _moodTagService.UpdateAsync(dto);
                TempData["Success"] = "Tag updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _moodTagService.DeleteAsync(id);
        
        if (success)
        {
            TempData["Success"] = "Tag deleted successfully!";
        }
        else
        {
            TempData["Error"] = "Failed to delete tag.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsCurrentUserAdminAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return false;
        
        return await _userManager.IsInRoleAsync(user, "Admin");
    }

    private static MoodTagViewModel MapToViewModel(MoodTagDto dto)
    {
        return new MoodTagViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            Color = dto.Color,
            IsSystemTag = dto.IsSystemTag,
            IsActive = dto.IsActive
        };
    }
}
