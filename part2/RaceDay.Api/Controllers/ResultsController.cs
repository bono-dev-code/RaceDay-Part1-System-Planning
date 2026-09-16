using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api")]
public class ResultsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Lists published results for an event, ordered by finishing position.</summary>
    [HttpGet("events/{eventId:int}/results"), ProducesResponseType(200), ProducesResponseType(404)]
    public async Task<IActionResult> GetForEvent(int eventId)=>!await db.Events.AnyAsync(x=>x.EventID==eventId)?NotFound(new{message="Event not found."}):Ok(await db.Results.AsNoTracking().Where(x=>x.Enrolment.EventID==eventId).OrderBy(x=>x.FinishingPosition).Select(x=>new{x.ResultID,x.FinishTime,x.FinishingPosition,x.PublishedAt,participant=new{x.Enrolment.Participant.UserID,x.Enrolment.Participant.FirstName,x.Enrolment.Participant.LastName},category=x.Enrolment.Category.CategoryName}).ToListAsync());

    /// <summary>Returns the logged-in Participant's race-result history.</summary>
    [HttpGet("results/me"), SessionAuthorize("Participant"), ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> GetMine()=>Ok(await db.Results.AsNoTracking().Where(x=>x.Enrolment.ParticipantID==UserId()).OrderByDescending(x=>x.Enrolment.Event.EventDate).Select(x=>new{x.ResultID,eventName=x.Enrolment.Event.EventName,eventDate=x.Enrolment.Event.EventDate,category=x.Enrolment.Category.CategoryName,x.FinishTime,x.FinishingPosition}).ToListAsync());

    /// <summary>Returns one result to its Participant or owning Organiser.</summary>
    [HttpGet("results/{resultId:int}"), SessionAuthorize, ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int resultId)
    {
        var r=await db.Results.AsNoTracking().Include(x=>x.Enrolment).ThenInclude(x=>x.Event).Include(x=>x.Enrolment).ThenInclude(x=>x.Participant).Include(x=>x.Enrolment).ThenInclude(x=>x.Category).SingleOrDefaultAsync(x=>x.ResultID==resultId);if(r is null)return NotFound(new{message="Result not found."});if(r.Enrolment.ParticipantID!=UserId()&&r.Enrolment.Event.OrganiserID!=UserId())return StatusCode(403,new{message="You cannot access this result."});return Ok(new{r.ResultID,r.FinishTime,r.FinishingPosition,r.PublishedAt,participant=new{r.Enrolment.Participant.UserID,r.Enrolment.Participant.FirstName,r.Enrolment.Participant.LastName},eventInfo=new{r.Enrolment.Event.EventID,r.Enrolment.Event.EventName},category=r.Enrolment.Category.CategoryName});
    }

    /// <summary>Captures a result for a confirmed enrolment in the Organiser's event.</summary>
    [HttpPost("enrolments/{enrolmentId:int}/result"), SessionAuthorize("Organiser"), ProducesResponseType(201), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Create(int enrolmentId, ResultRequest request)
    {
        if(request.FinishTime<=TimeSpan.Zero)return BadRequest(new{message="FinishTime must be greater than zero."});var e=await db.Enrolments.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.EnrolmentID==enrolmentId);if(e is null)return NotFound(new{message="Enrolment not found."});if(e.Event.OrganiserID!=UserId())return StatusCode(403,new{message="You do not own this event."});if(e.EnrolmentStatus!="Confirmed")return BadRequest(new{message="Only confirmed enrolments can receive results."});if(await db.Results.AnyAsync(x=>x.EnrolmentID==enrolmentId||x.Enrolment.EventID==e.EventID&&x.FinishingPosition==request.FinishingPosition))return Conflict(new{message="A result or finishing position already exists."});var r=new Result{EnrolmentID=enrolmentId,FinishTime=request.FinishTime,FinishingPosition=request.FinishingPosition};db.Results.Add(r);await db.SaveChangesAsync();return CreatedAtAction(nameof(GetById),new{resultId=r.ResultID},new{r.ResultID,r.EnrolmentID,r.FinishTime,r.FinishingPosition,r.PublishedAt});
    }

    /// <summary>Corrects a result owned by the Organiser's event.</summary>
    [HttpPut("results/{resultId:int}"), SessionAuthorize("Organiser"), ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Update(int resultId, ResultRequest request)
    {
        if(request.FinishTime<=TimeSpan.Zero)return BadRequest(new{message="FinishTime must be greater than zero."});var r=await db.Results.Include(x=>x.Enrolment).ThenInclude(x=>x.Event).SingleOrDefaultAsync(x=>x.ResultID==resultId);if(r is null)return NotFound(new{message="Result not found."});if(r.Enrolment.Event.OrganiserID!=UserId())return StatusCode(403,new{message="You do not own this event."});if(await db.Results.AnyAsync(x=>x.ResultID!=resultId&&x.Enrolment.EventID==r.Enrolment.EventID&&x.FinishingPosition==request.FinishingPosition))return Conflict(new{message="Finishing position already exists."});r.FinishTime=request.FinishTime;r.FinishingPosition=request.FinishingPosition;await db.SaveChangesAsync();return Ok(new{r.ResultID,r.EnrolmentID,r.FinishTime,r.FinishingPosition,r.PublishedAt});
    }

    /// <summary>Removes an incorrectly captured result from the Organiser's event.</summary>
    [HttpDelete("results/{resultId:int}"), SessionAuthorize("Organiser"), ProducesResponseType(204), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int resultId)
    {
        var r=await db.Results.Include(x=>x.Enrolment).ThenInclude(x=>x.Event).SingleOrDefaultAsync(x=>x.ResultID==resultId);if(r is null)return NotFound(new{message="Result not found."});if(r.Enrolment.Event.OrganiserID!=UserId())return StatusCode(403,new{message="You do not own this event."});db.Results.Remove(r);await db.SaveChangesAsync();return NoContent();
    }
    private int UserId()=>HttpContext.Session.GetInt32("UserID")!.Value;
}
