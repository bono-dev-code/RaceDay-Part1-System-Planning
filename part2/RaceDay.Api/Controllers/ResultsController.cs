using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController(RaceDayDbContext db) : ControllerBase
{
    // Get all results for an event by finishing position
    [HttpGet("events/{eventId:int}/results")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(x => x.EventID == eventId))
        {
            return NotFound(new { message = "Event not found." });
        }

        var results = await db.Results
            .AsNoTracking()
            .Where(x => x.Enrolment.EventID == eventId)
            .OrderBy(x => x.FinishingPosition)
            .Select(x => new
            {
                x.ResultID,
                x.FinishTime,
                x.FinishingPosition,
                x.PublishedAt,
                participant = new
                {
                    x.Enrolment.Participant.UserID,
                    x.Enrolment.Participant.FirstName,
                    x.Enrolment.Participant.LastName
                },
                category = x.Enrolment.Category.CategoryName
            })
            .ToListAsync();

        return Ok(results);
    }

    // Get results for the logged-in participant
    [HttpGet("results/me")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetMine()
    {
        var userId = UserId();

        var results = await db.Results
            .AsNoTracking()
            .Where(x => x.Enrolment.ParticipantID == userId)
            .OrderByDescending(x => x.Enrolment.Event.EventDate)
            .Select(x => new
            {
                x.ResultID,
                eventName = x.Enrolment.Event.EventName,
                eventDate = x.Enrolment.Event.EventDate,
                category = x.Enrolment.Category.CategoryName,
                x.FinishTime,
                x.FinishingPosition,
                totalFinishers = db.Results.Count(r =>
                    r.Enrolment.EventID == x.Enrolment.EventID)
            })
            .ToListAsync();

        return Ok(results);
    }

    // Get a result using its ID
    [HttpGet("results/{resultId:int}")]
    [SessionAuthorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int resultId)
    {
        var result = await db.Results
            .AsNoTracking()
            .Include(x => x.Enrolment)
                .ThenInclude(x => x.Event)
            .Include(x => x.Enrolment)
                .ThenInclude(x => x.Participant)
            .Include(x => x.Enrolment)
                .ThenInclude(x => x.Category)
            .SingleOrDefaultAsync(x => x.ResultID == resultId);

        if (result is null)
        {
            return NotFound(new { message = "Result not found." });
        }

        if (result.Enrolment.ParticipantID != UserId() &&
            result.Enrolment.Event.OrganiserID != UserId())
        {
            return StatusCode(403, new
            {
                message = "You cannot access this result."
            });
        }

        return Ok(new
        {
            result.ResultID,
            result.EnrolmentID,
            result.FinishTime,
            result.FinishingPosition,
            result.PublishedAt,
            participant = new
            {
                result.Enrolment.Participant.UserID,
                result.Enrolment.Participant.FirstName,
                result.Enrolment.Participant.LastName
            },
            eventInfo = new
            {
                result.Enrolment.Event.EventID,
                result.Enrolment.Event.EventName
            },
            category = result.Enrolment.Category.CategoryName
        });
    }

    // Add a result for a confirmed enrolment
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create(
        int enrolmentId,
        ResultRequest request)
    {
        if (!IsValidFinishTime(request.FinishTime))
        {
            return InvalidFinishTimeResponse();
        }

        var enrolment = await db.Enrolments
            .Include(x => x.Event)
            .SingleOrDefaultAsync(x => x.EnrolmentID == enrolmentId);

        if (enrolment is null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.Event.OrganiserID != UserId())
        {
            return StatusCode(403, new
            {
                message = "You do not own this event."
            });
        }

        if (enrolment.Event.EventDate > DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Results can only be recorded after the event has concluded."
            });
        }

        if (enrolment.EnrolmentStatus != "Confirmed")
        {
            return BadRequest(new
            {
                message = "Only confirmed enrolments can receive results."
            });
        }

        var resultExists = await db.Results.AnyAsync(x =>
            x.EnrolmentID == enrolmentId ||
            (x.Enrolment.EventID == enrolment.EventID &&
             x.FinishingPosition == request.FinishingPosition));

        if (resultExists)
        {
            return Conflict(new
            {
                message = "A result or finishing position already exists."
            });
        }

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            FinishTime = request.FinishTime,
            FinishingPosition = request.FinishingPosition
        };

        db.Results.Add(result);
        await db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { resultId = result.ResultID },
            new
            {
                result.ResultID,
                result.EnrolmentID,
                result.FinishTime,
                result.FinishingPosition,
                result.PublishedAt
            });
    }

    // Update an existing race result
    [HttpPut("results/{resultId:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Update(
        int resultId,
        ResultRequest request)
    {
        if (!IsValidFinishTime(request.FinishTime))
        {
            return InvalidFinishTimeResponse();
        }

        var result = await db.Results
            .Include(x => x.Enrolment)
                .ThenInclude(x => x.Event)
            .SingleOrDefaultAsync(x => x.ResultID == resultId);

        if (result is null)
        {
            return NotFound(new { message = "Result not found." });
        }

        if (result.Enrolment.Event.OrganiserID != UserId())
        {
            return StatusCode(403, new
            {
                message = "You do not own this event."
            });
        }

        var positionExists = await db.Results.AnyAsync(x =>
            x.ResultID != resultId &&
            x.Enrolment.EventID == result.Enrolment.EventID &&
            x.FinishingPosition == request.FinishingPosition);

        if (positionExists)
        {
            return Conflict(new
            {
                message = "Finishing position already exists."
            });
        }

        result.FinishTime = request.FinishTime;
        result.FinishingPosition = request.FinishingPosition;

        await db.SaveChangesAsync();

        return Ok(new
        {
            result.ResultID,
            result.EnrolmentID,
            result.FinishTime,
            result.FinishingPosition,
            result.PublishedAt
        });
    }

    // Delete an incorrectly recorded result
    [HttpDelete("results/{resultId:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int resultId)
    {
        var result = await db.Results
            .Include(x => x.Enrolment)
                .ThenInclude(x => x.Event)
            .SingleOrDefaultAsync(x => x.ResultID == resultId);

        if (result is null)
        {
            return NotFound(new { message = "Result not found." });
        }

        if (result.Enrolment.Event.OrganiserID != UserId())
        {
            return StatusCode(403, new
            {
                message = "You do not own this event."
            });
        }

        db.Results.Remove(result);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static bool IsValidFinishTime(TimeSpan finishTime)
    {
        return finishTime > TimeSpan.Zero &&
               finishTime < TimeSpan.FromDays(1);
    }

    private BadRequestObjectResult InvalidFinishTimeResponse()
    {
        return BadRequest(new
        {
            message = "Finish time must use HH:MM:SS and be between 00:00:01 and 23:59:59."
        });
    }

    // Get the logged in user's ID
    private int UserId()
    {
        return HttpContext.Session.GetInt32("UserID")!.Value;
    }
}