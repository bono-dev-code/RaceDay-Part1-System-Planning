using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Registers a new Organiser or Participant account.</summary>
    [HttpPost("register"), ProducesResponseType(201), ProducesResponseType(400), ProducesResponseType(409)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var roleName = request.RoleName.Trim();
        if (roleName is not ("Organiser" or "Participant")) return BadRequest(new { message = "Role must be Organiser or Participant." });
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email)) return Conflict(new { message = "Email is already registered." });
        var role = await db.Roles.SingleOrDefaultAsync(x => x.RoleName == roleName);
        if (role is null) return Problem("Required roles have not been seeded.");
        var user = new User { RoleID = role.RoleID, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), PhoneNumber = request.PhoneNumber?.Trim(), DateOfBirth = request.DateOfBirth };
        db.Users.Add(user); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(UsersController.GetMe), "Users", null, new { user.UserID, user.FirstName, user.LastName, user.Email, role = role.RoleName });
    }

    /// <summary>Logs in and creates a server-side session containing the user ID and role.</summary>
    [HttpPost("login"), ProducesResponseType(200), ProducesResponseType(400), ProducesResponseType(401)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return Unauthorized(new { message = "Invalid email or password." });
        HttpContext.Session.SetInt32("UserID", user.UserID); HttpContext.Session.SetString("Role", user.Role.RoleName);
        return Ok(new { message = "Login successful.", user = new { user.UserID, user.FirstName, user.LastName, user.Email, role = user.Role.RoleName } });
    }

    /// <summary>Clears the current authenticated session.</summary>
    [HttpPost("logout"), ProducesResponseType(204)]
    public IActionResult Logout() { HttpContext.Session.Clear(); return NoContent(); }
}
