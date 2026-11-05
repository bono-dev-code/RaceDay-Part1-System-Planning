using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class AccountController(IRaceDayApiClient api) : Controller
{
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Register the user through the RaceDay API.
        var created = await api.PostAsync<LoginUser>("/api/auth/register", new
        {
            model.FirstName,
            model.LastName,
            model.Email,
            model.Password,
            model.PhoneNumber,
            model.DateOfBirth,
            model.RoleName
        });
        if (!created.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "Registration failed.");
            return View(model);
        }

        var login = await api.PostAsync<LoginResponse>(
            "/api/auth/login",
            new
            {
                model.Email,
                model.Password
            },
            true);

        if (!login.IsSuccess || login.Data is null)
        {
            return RedirectToAction(nameof(Login));
        }

        SignIn(login.Data.User);
        TempData["Success"] = "Welcome to RaceDay. Your account is ready.";
        return RedirectForRole(login.Data.User.Role);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await api.PostAsync<LoginResponse>("/api/auth/login", model, true);
        if (!result.IsSuccess || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Login failed.");
            return View(model);
        }

        SignIn(result.Data.User);
        TempData["Success"] = $"Welcome back, {result.Data.User.FirstName}.";
        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectForRole(result.Data.User.Role);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await api.PostAsync<object>("/api/auth/logout");
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out safely.";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    private void SignIn(LoginUser user)
    {
        HttpContext.Session.SetInt32("UserID", user.UserID);
        HttpContext.Session.SetString("FirstName", user.FirstName);
        HttpContext.Session.SetString("Role", user.Role);
    }

    private IActionResult RedirectForRole(string role) => role == "Organiser"
        ? RedirectToAction("Dashboard", "Organiser")
        : RedirectToAction("MyEvents", "Participant");
}
