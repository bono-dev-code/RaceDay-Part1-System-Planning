using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize("Participant")]
public class ParticipantController(IRaceDayApiClient api) : Controller
{
    public async Task<IActionResult> MyEvents()
    {
        var result = await api.GetAsync<List<EnrolmentModel>>("/api/enrolments/me");
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        return View(result.Data ?? []);
    }

    public async Task<IActionResult> MyResults()
    {
        var result = await api.GetAsync<List<ResultModel>>("/api/results/me");
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        return View(result.Data ?? []);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        // The API creates the enrolment and assigns the selected category.
        var result = await api.PostAsync<CreatedEnrolment>(
            $"/api/events/{eventId}/enrolments",
            new
            {
                categoryId
            });
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Your event entry was submitted and is pending confirmation."
            : result.Error;
        return result.IsSuccess
            ? RedirectToAction(nameof(MyEvents))
            : RedirectToAction("Details", "Home", new { id = eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await api.DeleteAsync($"/api/enrolments/{id}");
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Your enrolment was cancelled."
            : result.Error;
        return RedirectToAction(nameof(MyEvents));
    }
}
