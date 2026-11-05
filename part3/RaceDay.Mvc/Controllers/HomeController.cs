using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class HomeController(IRaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? type, DateTime? date, string? location)
    {
        var query = new List<string> { "upcoming=true" };
        if (!string.IsNullOrWhiteSpace(type))
        {
            query.Add($"type={Uri.EscapeDataString(type)}");
        }

        if (date.HasValue)
        {
            query.Add($"date={date:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            query.Add($"location={Uri.EscapeDataString(location)}");
        }
        var path = "/api/events?" + string.Join('&', query);
        var result = await api.GetAsync<List<EventSummary>>(path);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        return View(new EventSearchViewModel
        {
            Type = type,
            Date = date,
            Location = location,
            Events = result.Data ?? []
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await api.GetAsync<EventDetails>($"/api/events/{id}");
        if (!result.IsSuccess)
        {
            return result.StatusCode == 404
                ? NotFound()
                : RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }

    public IActionResult Error() => View();
}
