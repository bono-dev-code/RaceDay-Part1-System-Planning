using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/users"), SessionAuthorize]
public class UsersController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Returns the authenticated user's profile without exposing PasswordHash.</summary>
    [HttpGet("me"), ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetMe()
    {
        var id = HttpContext.Session.GetInt32("UserID")!.Value;
        var user = await db.Users.Include(x => x.Role).Where(x => x.UserID == id).Select(x => new { x.UserID, x.FirstName, x.LastName, x.Email, x.PhoneNumber, x.DateOfBirth, x.ProfilePictureUrl, x.CreatedAt, role = x.Role.RoleName }).SingleOrDefaultAsync();
        return user is null ? NotFound(new { message = "User not found." }) : Ok(user);
    }

    /// <summary>Updates editable profile details for the authenticated user.</summary>
    [HttpPut("me"), ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var id = HttpContext.Session.GetInt32("UserID")!.Value;
        var user = await db.Users.FindAsync(id); if (user is null) return NotFound(new { message = "User not found." });
        user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim(); user.PhoneNumber = request.PhoneNumber?.Trim(); user.DateOfBirth = request.DateOfBirth;
        await db.SaveChangesAsync(); return Ok(new { user.UserID, user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.DateOfBirth });
    }
}
