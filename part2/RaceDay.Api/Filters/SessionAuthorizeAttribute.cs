using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Filters;

// Check if a user is allowed to access a resource
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    // Store the allowed user roles
    private readonly string[] _roles;
    public SessionAuthorizeAttribute(params string[] roles) => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Get the user ID and role from the session
        var userId = context.HttpContext.Session.GetInt32("UserID");
        var role = context.HttpContext.Session.GetString("Role");

        // Check if the user is logged in
        if (userId is null || string.IsNullOrWhiteSpace(role))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Authentication is required." });
            return;
        }

        // Check if the user has the correct role
        if (_roles.Length > 0 && !_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            context.Result = new ObjectResult(new { message = "Your role cannot access this resource." }) { StatusCode = StatusCodes.Status403Forbidden };
    }
}
