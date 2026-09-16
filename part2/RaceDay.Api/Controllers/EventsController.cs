using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api")]
public class EventsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Lists events, with optional event type, date and location filters.</summary>
    [HttpGet("events"), ProducesResponseType(200), ProducesResponseType(400)]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] DateTime? date, [FromQuery] string? location)
    {
        if (type is not null && type is not ("Run" or "Walk" or "Cycle")) return BadRequest(new { message = "Type must be Run, Walk or Cycle." });
        var q = db.Events.AsNoTracking().AsQueryable();
        if (type is not null) q = q.Where(x => x.EventType == type); if (date.HasValue) q = q.Where(x => x.EventDate.Date == date.Value.Date); if (!string.IsNullOrWhiteSpace(location)) q = q.Where(x => x.Location.Contains(location));
        return Ok(await q.OrderBy(x => x.EventDate).Select(x => new { x.EventID, x.EventName, x.EventDate, x.Location, x.Distance, x.EventType }).ToListAsync());
    }

    /// <summary>Returns one event with organiser and category details.</summary>
    [HttpGet("events/{eventId:int}"), ProducesResponseType(200), ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int eventId)
    {
        var item = await db.Events.AsNoTracking().Where(x => x.EventID == eventId).Select(x => new { x.EventID, x.EventName, x.Description, x.EventDate, x.Location, x.Distance, x.EventType, x.BannerImageUrl, organiser = new { x.Organiser.UserID, x.Organiser.FirstName, x.Organiser.LastName }, categories = x.Categories.Select(c => new { c.CategoryID, c.CategoryName, c.CategoryType, c.Description }) }).SingleOrDefaultAsync();
        return item is null ? NotFound(new { message = "Event not found." }) : Ok(item);
    }

    /// <summary>Creates an event for the logged-in Organiser.</summary>
    [HttpPost("events"), SessionAuthorize("Organiser"), ProducesResponseType(201), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        if (!ValidType(request.EventType)) return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });
        var item = new Event { OrganiserID = UserId(), EventName = request.EventName.Trim(), Description = request.Description.Trim(), EventDate = request.EventDate, Location = request.Location.Trim(), Distance = request.Distance, EventType = request.EventType };
        db.Events.Add(item); await db.SaveChangesAsync(); return CreatedAtAction(nameof(GetById), new { eventId = item.EventID }, item);
    }

    /// <summary>Updates an event owned by the logged-in Organiser.</summary>
    [HttpPut("events/{eventId:int}"), SessionAuthorize("Organiser"), ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404)]
    public async Task<IActionResult> Update(int eventId, EventRequest request)
    {
        if (!ValidType(request.EventType)) return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });
        var item = await db.Events.FindAsync(eventId); if (item is null) return NotFound(new { message = "Event not found." }); if (item.OrganiserID != UserId()) return StatusCode(403, new { message = "You do not own this event." });
        item.EventName=request.EventName.Trim(); item.Description=request.Description.Trim(); item.EventDate=request.EventDate; item.Location=request.Location.Trim(); item.Distance=request.Distance; item.EventType=request.EventType; await db.SaveChangesAsync(); return Ok(item);
    }

    /// <summary>Deletes an owned event when it has no dependent enrolments.</summary>
    [HttpDelete("events/{eventId:int}"), SessionAuthorize("Organiser"), ProducesResponseType(204), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Delete(int eventId)
    {
        var item=await db.Events.FindAsync(eventId); if(item is null)return NotFound(new{message="Event not found."}); if(item.OrganiserID!=UserId())return StatusCode(403,new{message="You do not own this event."}); if(await db.Enrolments.AnyAsync(x=>x.EventID==eventId))return Conflict(new{message="Event has enrolments and cannot be deleted."}); db.Events.Remove(item); await db.SaveChangesAsync(); return NoContent();
    }

    /// <summary>Lists events created by the logged-in Organiser.</summary>
    [HttpGet("organisers/me/events"), SessionAuthorize("Organiser"), ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> GetMine() => Ok(await db.Events.AsNoTracking().Where(x=>x.OrganiserID==UserId()).OrderBy(x=>x.EventDate).ToListAsync());

    private int UserId()=>HttpContext.Session.GetInt32("UserID")!.Value;
    private static bool ValidType(string value)=>value is "Run" or "Walk" or "Cycle";
}
