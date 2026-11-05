using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize]
public class ProfileController(IRaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<UserProfile>("/api/users/me");
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "Home");
        }
        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var result = await api.GetAsync<UserProfile>("/api/users/me");
        if (!result.IsSuccess || result.Data is null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(new ProfileFormModel
        {
            FirstName = result.Data.FirstName,
            LastName = result.Data.LastName,
            PhoneNumber = result.Data.PhoneNumber,
            DateOfBirth = result.Data.DateOfBirth
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await api.PutAsync<UserProfile>("/api/users/me", new
        {
            model.FirstName,
            model.LastName,
            model.PhoneNumber,
            model.DateOfBirth
        });
        if (!updated.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, updated.Error ?? "Profile update failed.");
            return View(model);
        }

        if (model.ProfilePicture is not null)
        {
            var upload = await api.UploadAsync<UploadResponse>(
                "/api/users/me/profile-picture",
                model.ProfilePicture);
            if (!upload.IsSuccess)
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), upload.Error ?? "Image upload failed.");
                return View(model);
            }
        }

        HttpContext.Session.SetString("FirstName", model.FirstName);
        TempData["Success"] = "Your profile was updated.";
        return RedirectToAction(nameof(Index));
    }
}
