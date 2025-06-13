using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoodLog.Web.Models;
using MoodLog.Application.Interfaces;

namespace MoodLog.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IDataSeedingService _dataSeedingService;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IDataSeedingService dataSeedingService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _dataSeedingService = dataSeedingService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, 
                model.Password, 
                model.RememberMe, 
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Check if user is admin and redirect accordingly
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var user = new IdentityUser 
            { 
                UserName = model.Email, 
                Email = model.Email 
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure roles exist
                await EnsureRolesExist();

                // Make the first user an admin
                var userCount = _userManager.Users.Count();
                if (userCount == 1)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }

                await _signInManager.SignInAsync(user, isPersistent: false);

                // Automatically seed demo data for new users (except admins)
                if (!await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    try
                    {
                        var userId = Math.Abs(user.Id.GetHashCode());
                        await _dataSeedingService.SeedMoodDataForUserAsync(userId, 30);
                    }
                    catch (Exception)
                    {
                        // Log error but don't prevent user registration
                        // Note: In production, this should use proper logging (ILogger)
                    }
                }

                // Check if user is admin and redirect accordingly
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task EnsureRolesExist()
    {
        var roles = new[] { "Admin", "User" };
        
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // Temporary endpoint to assign admin role - for development/demo purposes
    [HttpGet]
    public async Task<IActionResult> MakeAdmin()
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found. Please log in first." });
            }

            // Check if user is already admin
            if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                return Json(new {
                    success = true,
                    message = $"User {currentUser.Email} is already an admin! Please refresh the page to see the Admin Panel button.",
                    alreadyAdmin = true
                });
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
                    message = $"Successfully assigned admin role to {currentUser.Email}! Please refresh the page to see the Admin Panel button.",
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
