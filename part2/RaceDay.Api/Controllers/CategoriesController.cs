using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api")]
public class CategoriesController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Lists all categories available for an event.</summary>
    [HttpGet("events/{eventId:int}/categories"), ProducesResponseType(200), ProducesResponseType(404)]
    public async Task<IActionResult> GetForEvent(int eventId) => !await db.Events.AnyAsync(x=>x.EventID==eventId) ? NotFound(new{message="Event not found."}) : Ok(await db.Categories.AsNoTracking().Where(x=>x.EventID==eventId).ToListAsync());

    /// <summary>Returns one category and its parent event ID.</summary>
    [HttpGet("categories/{categoryId:int}"), ProducesResponseType(200), ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int categoryId) { var c=await db.Categories.AsNoTracking().SingleOrDefaultAsync(x=>x.CategoryID==categoryId); return c is null?NotFound(new{message="Category not found."}):Ok(c); }

    /// <summary>Adds a category to an event owned by the logged-in Organiser.</summary>
    [HttpPost("events/{eventId:int}/categories"), SessionAuthorize("Organiser"), ProducesResponseType(201), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        var ev=await db.Events.FindAsync(eventId); if(ev is null)return NotFound(new{message="Event not found."}); if(ev.OrganiserID!=UserId())return StatusCode(403,new{message="You do not own this event."}); if(!ValidType(request.CategoryType))return BadRequest(new{message="CategoryType must be Age or Distance."}); if(await db.Categories.AnyAsync(x=>x.EventID==eventId&&x.CategoryName==request.CategoryName.Trim()))return Conflict(new{message="Category name already exists for this event."});
        var c=new Category{EventID=eventId,CategoryName=request.CategoryName.Trim(),CategoryType=request.CategoryType,Description=request.Description?.Trim()}; db.Categories.Add(c); await db.SaveChangesAsync(); return CreatedAtAction(nameof(GetById),new{categoryId=c.CategoryID},new{c.CategoryID,c.EventID,c.CategoryName,c.CategoryType,c.Description});
    }

    /// <summary>Updates a category belonging to the Organiser's event.</summary>
    [HttpPut("categories/{categoryId:int}"), SessionAuthorize("Organiser"), ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Update(int categoryId, CategoryRequest request)
    {
        var c=await db.Categories.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.CategoryID==categoryId); if(c is null)return NotFound(new{message="Category not found."}); if(c.Event.OrganiserID!=UserId())return StatusCode(403,new{message="You do not own this event."}); if(!ValidType(request.CategoryType))return BadRequest(new{message="CategoryType must be Age or Distance."}); if(await db.Categories.AnyAsync(x=>x.EventID==c.EventID&&x.CategoryID!=categoryId&&x.CategoryName==request.CategoryName.Trim()))return Conflict(new{message="Category name already exists for this event."}); c.CategoryName=request.CategoryName.Trim();c.CategoryType=request.CategoryType;c.Description=request.Description?.Trim();await db.SaveChangesAsync();return Ok(new{c.CategoryID,c.EventID,c.CategoryName,c.CategoryType,c.Description});
    }

    /// <summary>Deletes an unused category from an event owned by the Organiser.</summary>
    [HttpDelete("categories/{categoryId:int}"), SessionAuthorize("Organiser"), ProducesResponseType(204), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Delete(int categoryId)
    {
        var c=await db.Categories.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.CategoryID==categoryId);if(c is null)return NotFound(new{message="Category not found."});if(c.Event.OrganiserID!=UserId())return StatusCode(403,new{message="You do not own this event."});if(await db.Enrolments.AnyAsync(x=>x.CategoryID==categoryId))return Conflict(new{message="Category is used by enrolments."});db.Categories.Remove(c);await db.SaveChangesAsync();return NoContent();
    }
    private int UserId()=>HttpContext.Session.GetInt32("UserID")!.Value;
    private static bool ValidType(string value)=>value is "Age" or "Distance";
}
