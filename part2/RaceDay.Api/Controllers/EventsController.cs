using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api")]
public class EventsController(RaceDayDbContext db, IBlobStorageService blobStorage) : ControllerBase
{
    // Get all events and filter by type, date or location
    [HttpGet("events"), ProducesResponseType(200), ProducesResponseType(400)]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] DateTime? date, [FromQuery] string? location, [FromQuery] bool upcoming = false)
    {
        // Check if the event type is valid
        if (type is not null && type is not ("Run" or "Walk" or "Cycle")) return BadRequest(new { message = "Type must be Run, Walk or Cycle." });
        var q = db.Events.AsNoTracking().AsQueryable();
        if (type is not null) q = q.Where(x => x.EventType == type); if (date.HasValue) q = q.Where(x => x.EventDate.Date == date.Value.Date); if (!string.IsNullOrWhiteSpace(location)) q = q.Where(x => x.Location.Contains(location)); if (upcoming) q = q.Where(x => x.EventDate >= DateTime.UtcNow);
        return Ok(await q.OrderBy(x => x.EventDate).Select(x => new { x.EventID, x.EventName, x.EventDate, x.Location, x.Distance, x.EventType, x.BannerImageUrl }).ToListAsync());
    }

    // Get an event with its organiser and categories
    [HttpGet("events/{eventId:int}"), ProducesResponseType(200), ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int eventId)
    {
        var item = await db.Events.AsNoTracking().Where(x => x.EventID == eventId).Select(x => new { x.EventID, x.EventName, x.Description, x.EventDate, x.Location, x.Distance, x.EventType, x.BannerImageUrl, organiser = new { x.Organiser.UserID, x.Organiser.FirstName, x.Organiser.LastName }, categories = x.Categories.Select(c => new { c.CategoryID, c.CategoryName, c.CategoryType, c.Description }) }).SingleOrDefaultAsync();
        return item is null ? NotFound(new { message = "Event not found." }) : Ok(item);
    }

    // Create a new event for the logged-in organiser
    [HttpPost("events"), SessionAuthorize("Organiser"), ProducesResponseType(201), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        if (!ValidType(request.EventType)) return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

        // Save the new event to the database
        var item = new Event { OrganiserID = UserId(), EventName = request.EventName.Trim(), Description = request.Description.Trim(), EventDate = request.EventDate, Location = request.Location.Trim(), Distance = request.Distance, EventType = request.EventType };
        db.Events.Add(item); await db.SaveChangesAsync(); return CreatedAtAction(nameof(GetById), new { eventId = item.EventID }, item);
    }

    // Update an event owned by the organiser
    [HttpPut("events/{eventId:int}"), SessionAuthorize("Organiser"), ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404)]
    public async Task<IActionResult> Update(int eventId, EventRequest request)
    {
        if (!ValidType(request.EventType)) return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });
        var item = await db.Events.FindAsync(eventId); if (item is null) return NotFound(new { message = "Event not found." }); if (item.OrganiserID != UserId()) return StatusCode(403, new { message = "You do not own this event." });

        // Save the updated event details
        item.EventName=request.EventName.Trim(); item.Description=request.Description.Trim(); item.EventDate=request.EventDate; item.Location=request.Location.Trim(); item.Distance=request.Distance; item.EventType=request.EventType; await db.SaveChangesAsync(); return Ok(item);
    }

    // Delete an event if it has no enrolments
    [HttpDelete("events/{eventId:int}"), SessionAuthorize("Organiser"), ProducesResponseType(204), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Delete(int eventId)
    {
        var item=await db.Events.FindAsync(eventId); if(item is null)return NotFound(new{message="Event not found."}); if(item.OrganiserID!=UserId())return StatusCode(403,new{message="You do not own this event."}); if(await db.Enrolments.AnyAsync(x=>x.EventID==eventId))return Conflict(new{message="Event has enrolments and cannot be deleted."}); db.Events.Remove(item); await db.SaveChangesAsync(); return NoContent();
    }

    // Get all events created by the logged-in organiser
    [HttpGet("organisers/me/events"), SessionAuthorize("Organiser"), ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> GetMine() => Ok(await db.Events.AsNoTracking().Where(x=>x.OrganiserID==UserId()).OrderBy(x=>x.EventDate).ToListAsync());

    [HttpGet("organisers/me/dashboard"), SessionAuthorize("Organiser"), ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Dashboard()
    {
        var now = DateTime.UtcNow;
        var events = await db.Events.AsNoTracking()
            .Where(x => x.OrganiserID == UserId())
            .OrderBy(x => x.EventDate)
            .Select(x => new
            {
                x.EventID,
                x.EventName,
                x.EventDate,
                x.Location,
                x.EventType,
                enrolmentCount = x.Enrolments.Count,
                pendingCount = x.Enrolments.Count(e => e.EnrolmentStatus == "Pending"),
                confirmedCount = x.Enrolments.Count(e => e.EnrolmentStatus == "Confirmed")
            }).ToListAsync();

        return Ok(new
        {
            totalEvents = events.Count,
            totalEnrolments = events.Sum(x => x.enrolmentCount),
            upcomingEvents = events.Count(x => x.EventDate >= now),
            events
        });
    }

    [HttpPost("events/{eventId:int}/banner"), SessionAuthorize("Organiser"), Consumes("multipart/form-data"), ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404)]
    public async Task<IActionResult> UploadBanner(int eventId, IFormFile image, CancellationToken cancellationToken)
    {
        var item = await db.Events.FindAsync([eventId], cancellationToken);
        if (item is null) return NotFound(new { message = "Event not found." });
        if (item.OrganiserID != UserId()) return StatusCode(403, new { message = "You do not own this event." });

        try
        {
            item.BannerImageUrl = await blobStorage.UploadImageAsync(image, $"events/{eventId}", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { item.EventID, item.BannerImageUrl });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Problem(ex.Message, statusCode: 503); }
    }

    // Get the logged-in user's ID
    private int UserId()=>HttpContext.Session.GetInt32("UserID")!.Value;

    // Check if the event type is valid
    private static bool ValidType(string value)=>value is "Run" or "Walk" or "Cycle";
}
