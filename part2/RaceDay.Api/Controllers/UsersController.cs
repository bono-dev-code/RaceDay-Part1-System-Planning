using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/users"), SessionAuthorize]
public class UsersController(RaceDayDbContext db, IBlobStorageService blobStorage) : ControllerBase
{
    // Get the logged-in user's profile without showing the password
    [HttpGet("me"), ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetMe()
    {
        // Get the user ID from the session
        var id = HttpContext.Session.GetInt32("UserID")!.Value;
        var user = await db.Users.Include(x => x.Role).Where(x => x.UserID == id).Select(x => new { x.UserID, x.FirstName, x.LastName, x.Email, x.PhoneNumber, x.DateOfBirth, x.ProfilePictureUrl, x.CreatedAt, role = x.Role.RoleName }).SingleOrDefaultAsync();
        return user is null ? NotFound(new { message = "User not found." }) : Ok(user);
    }

    // Update the logged-in user's profile details
    [HttpPut("me"), ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        // Find the user using their ID
        var id = HttpContext.Session.GetInt32("UserID")!.Value;
        var user = await db.Users.FindAsync(id); if (user is null) return NotFound(new { message = "User not found." });

        // Save the updated profile details
        user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim(); user.PhoneNumber = request.PhoneNumber?.Trim(); user.DateOfBirth = request.DateOfBirth;
        await db.SaveChangesAsync(); return Ok(new { user.UserID, user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.DateOfBirth });
    }

    [HttpPost("me/profile-picture"), Consumes("multipart/form-data"), ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile image, CancellationToken cancellationToken)
    {
        var id = HttpContext.Session.GetInt32("UserID")!.Value;
        var user = await db.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound(new { message = "User not found." });

        try
        {
            user.ProfilePictureUrl = await blobStorage.UploadImageAsync(image, $"profiles/{id}", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { user.UserID, user.ProfilePictureUrl });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Problem(ex.Message, statusCode: 503); }
    }
}
