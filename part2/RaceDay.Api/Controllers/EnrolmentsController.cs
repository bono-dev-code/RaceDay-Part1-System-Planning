using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api")]
public class EnrolmentsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Enrols the logged-in Participant in an event and selected category.</summary>
    [HttpPost("events/{eventId:int}/enrolments"), SessionAuthorize("Participant"), ProducesResponseType(201), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Create(int eventId, EnrolmentRequest request)
    {
        if(!await db.Events.AnyAsync(x=>x.EventID==eventId))return NotFound(new{message="Event not found."}); var category=await db.Categories.FindAsync(request.CategoryId);if(category is null)return NotFound(new{message="Category not found."});if(category.EventID!=eventId)return BadRequest(new{message="Selected category does not belong to this event."});if(await db.Enrolments.AnyAsync(x=>x.ParticipantID==UserId()&&x.EventID==eventId))return Conflict(new{message="You are already enrolled in this event."});
        var e=new Enrolment{ParticipantID=UserId(),EventID=eventId,CategoryID=request.CategoryId};db.Enrolments.Add(e);await db.SaveChangesAsync();return CreatedAtAction(nameof(GetById),new{enrolmentId=e.EnrolmentID},new{e.EnrolmentID,e.ParticipantID,e.EventID,e.CategoryID,e.EnrolmentDate,e.EnrolmentStatus});
    }

    /// <summary>Lists enrolments belonging to the logged-in Participant.</summary>
    [HttpGet("enrolments/me"), SessionAuthorize("Participant"), ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> GetMine()=>Ok(await db.Enrolments.AsNoTracking().Where(x=>x.ParticipantID==UserId()).Select(x=>new{x.EnrolmentID,x.EnrolmentDate,x.EnrolmentStatus,eventInfo=new{x.Event.EventID,x.Event.EventName,x.Event.EventDate},category=new{x.Category.CategoryID,x.Category.CategoryName}}).ToListAsync());

    /// <summary>Returns an enrolment to its Participant or owning Organiser.</summary>
    [HttpGet("enrolments/{enrolmentId:int}"), SessionAuthorize, ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int enrolmentId)
    {
        var e=await db.Enrolments.AsNoTracking().Include(x=>x.Event).Include(x=>x.Category).Include(x=>x.Participant).SingleOrDefaultAsync(x=>x.EnrolmentID==enrolmentId);if(e is null)return NotFound(new{message="Enrolment not found."});if(e.ParticipantID!=UserId()&&e.Event.OrganiserID!=UserId())return StatusCode(403,new{message="You cannot access this enrolment."});return Ok(new{e.EnrolmentID,e.EnrolmentDate,e.EnrolmentStatus,participant=new{e.Participant.UserID,e.Participant.FirstName,e.Participant.LastName},eventInfo=new{e.Event.EventID,e.Event.EventName},category=new{e.Category.CategoryID,e.Category.CategoryName}});
    }

    /// <summary>Lists Participants enrolled in an event owned by the Organiser.</summary>
    [HttpGet("events/{eventId:int}/enrolments"), SessionAuthorize("Organiser"), ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev=await db.Events.FindAsync(eventId);if(ev is null)return NotFound(new{message="Event not found."});if(ev.OrganiserID!=UserId())return StatusCode(403,new{message="You do not own this event."});return Ok(await db.Enrolments.AsNoTracking().Where(x=>x.EventID==eventId).Select(x=>new{x.EnrolmentID,x.EnrolmentDate,x.EnrolmentStatus,participant=new{x.Participant.UserID,x.Participant.FirstName,x.Participant.LastName,x.Participant.Email},category=new{x.Category.CategoryID,x.Category.CategoryName}}).ToListAsync());
    }

    /// <summary>Changes an enrolment's status for an event owned by the Organiser.</summary>
    [HttpPut("enrolments/{enrolmentId:int}/status"), SessionAuthorize("Organiser"), ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStatus(int enrolmentId, StatusRequest request)
    {
        if(request.EnrolmentStatus is not ("Pending" or "Confirmed" or "Cancelled"))return BadRequest(new{message="Status must be Pending, Confirmed or Cancelled."});var e=await db.Enrolments.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.EnrolmentID==enrolmentId);if(e is null)return NotFound(new{message="Enrolment not found."});if(e.Event.OrganiserID!=UserId())return StatusCode(403,new{message="You do not own this event."});e.EnrolmentStatus=request.EnrolmentStatus;await db.SaveChangesAsync();return Ok(new{e.EnrolmentID,e.EnrolmentStatus});
    }

    /// <summary>Deletes the Participant's own enrolment before a result exists.</summary>
    [HttpDelete("enrolments/{enrolmentId:int}"), SessionAuthorize("Participant"), ProducesResponseType(204), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Delete(int enrolmentId)
    {
        var e=await db.Enrolments.FindAsync(enrolmentId);if(e is null)return NotFound(new{message="Enrolment not found."});if(e.ParticipantID!=UserId())return StatusCode(403,new{message="You do not own this enrolment."});if(await db.Results.AnyAsync(x=>x.EnrolmentID==enrolmentId))return Conflict(new{message="A result has already been recorded."});db.Enrolments.Remove(e);await db.SaveChangesAsync();return NoContent();
    }
    private int UserId()=>HttpContext.Session.GetInt32("UserID")!.Value;
}
