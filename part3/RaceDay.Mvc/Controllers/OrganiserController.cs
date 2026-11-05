using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize("Organiser")]
public class OrganiserController(IRaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        var result = await api.GetAsync<DashboardModel>("/api/organisers/me/dashboard");
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        return View(result.Data ?? new DashboardModel());
    }

    public async Task<IActionResult> Events()
    {
        var result = await api.GetAsync<List<EventSummary>>("/api/organisers/me/events");
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        return View(result.Data ?? []);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        return View("EventForm", new EventFormModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        // Send the event details to the API before uploading the banner.
        var result = await api.PostAsync<CreatedEvent>("/api/events", EventBody(model));
        if (!result.IsSuccess || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Event creation failed.");
            return View("EventForm", model);
        }
        if (model.BannerImage is not null)
        {
            var upload = await api.UploadAsync<UploadResponse>(
                $"/api/events/{result.Data.EventID}/banner",
                model.BannerImage);

            if (!upload.IsSuccess)
            {
                TempData["Error"] = $"Event created, but the banner failed: {upload.Error}";
            }
        }
        TempData["Success"] = "The event was created.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await api.GetAsync<EventDetails>($"/api/events/{id}");
        if (!result.IsSuccess || result.Data is null)
        {
            return NotFound();
        }

        return View("EventForm", new EventFormModel
        {
            EventID = result.Data.EventID,
            EventName = result.Data.EventName,
            Description = result.Data.Description,
            EventDate = result.Data.EventDate,
            Location = result.Data.Location,
            Distance = result.Data.Distance,
            EventType = result.Data.EventType
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        var result = await api.PutAsync<EventDetails>($"/api/events/{model.EventID}", EventBody(model));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Event update failed.");
            return View("EventForm", model);
        }
        if (model.BannerImage is not null)
        {
            var upload = await api.UploadAsync<UploadResponse>(
                $"/api/events/{model.EventID}/banner",
                model.BannerImage);

            if (!upload.IsSuccess)
            {
                TempData["Error"] = upload.Error;
            }
        }
        TempData["Success"] = "The event was updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await api.DeleteAsync($"/api/events/{id}");
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "The event was deleted." : result.Error;
        return RedirectToAction(nameof(Events));
    }

    public async Task<IActionResult> Categories(int eventId)
    {
        var result = await api.GetAsync<List<CategoryModel>>($"/api/events/{eventId}/categories");
        ViewBag.EventId = eventId;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        return View(result.Data ?? []);
    }

    [HttpGet]
    public IActionResult CreateCategory(int eventId)
    {
        return View("CategoryForm", new CategoryFormModel
        {
            EventID = eventId
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("CategoryForm", model);
        }

        var result = await api.PostAsync<CategoryModel>(
            $"/api/events/{model.EventID}/categories",
            CategoryBody(model));

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Category creation failed.");
            return View("CategoryForm", model);
        }

        TempData["Success"] = "The category was added.";
        return RedirectToAction(nameof(Categories), new { eventId = model.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var result = await api.GetAsync<CategoryModel>($"/api/categories/{id}");
        if (!result.IsSuccess || result.Data is null)
        {
            return NotFound();
        }

        return View("CategoryForm", new CategoryFormModel
        {
            CategoryID = result.Data.CategoryID,
            EventID = result.Data.EventID,
            CategoryName = result.Data.CategoryName,
            CategoryType = result.Data.CategoryType,
            Description = result.Data.Description
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("CategoryForm", model);
        }

        var result = await api.PutAsync<CategoryModel>(
            $"/api/categories/{model.CategoryID}",
            CategoryBody(model));

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Category update failed.");
            return View("CategoryForm", model);
        }

        TempData["Success"] = "The category was updated.";
        return RedirectToAction(nameof(Categories), new { eventId = model.EventID });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var result = await api.DeleteAsync($"/api/categories/{id}");
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "The category was deleted." : result.Error;
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    public async Task<IActionResult> Enrolments(int eventId)
    {
        var result = await api.GetAsync<List<EnrolmentModel>>($"/api/events/{eventId}/enrolments");
        ViewBag.EventId = eventId;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        return View(result.Data ?? []);
    }

    public async Task<IActionResult> Results(int eventId)
    {
        var result = await api.GetAsync<List<ResultModel>>($"/api/events/{eventId}/results");
        ViewBag.EventId = eventId;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        return View(result.Data ?? []);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, int eventId, string enrolmentStatus)
    {
        var result = await api.PutAsync<object>($"/api/enrolments/{id}/status", new { enrolmentStatus });
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "The enrolment status was updated." : result.Error;
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    [HttpGet]
    public IActionResult AddResult(int enrolmentId, int eventId)
    {
        return View(new ResultFormModel
        {
            EnrolmentID = enrolmentId,
            EventID = eventId
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddResult(ResultFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await api.PostAsync<ResultModel>(
            $"/api/enrolments/{model.EnrolmentID}/result",
            new
            {
                model.FinishTime,
                model.FinishingPosition
            });

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Result creation failed.");
            return View(model);
        }

        TempData["Success"] = "The race result was published.";
        return RedirectToAction(nameof(Enrolments), new { eventId = model.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> EditResult(int id, int eventId)
    {
        var result = await api.GetAsync<ResultModel>($"/api/results/{id}");
        if (!result.IsSuccess || result.Data is null)
        {
            return NotFound();
        }

        return View("AddResult", new ResultFormModel
        {
            ResultID = result.Data.ResultID,
            EnrolmentID = result.Data.EnrolmentID,
            EventID = eventId,
            FinishTime = result.Data.FinishTime,
            FinishingPosition = result.Data.FinishingPosition
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditResult(ResultFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("AddResult", model);
        }

        var result = await api.PutAsync<ResultModel>(
            $"/api/results/{model.ResultID}",
            new
            {
                model.FinishTime,
                model.FinishingPosition
            });

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Result update failed.");
            return View("AddResult", model);
        }

        TempData["Success"] = "The race result was updated.";
        return RedirectToAction(nameof(Results), new { eventId = model.EventID });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResult(int id, int eventId)
    {
        var result = await api.DeleteAsync($"/api/results/{id}");
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "The result was deleted." : result.Error;
        return RedirectToAction(nameof(Results), new { eventId });
    }

    private static object EventBody(EventFormModel model)
    {
        return new
        {
            model.EventName,
            model.Description,
            model.EventDate,
            model.Location,
            model.Distance,
            model.EventType
        };
    }

    private static object CategoryBody(CategoryFormModel model)
    {
        return new
        {
            model.CategoryName,
            model.CategoryType,
            model.Description
        };
    }
}
