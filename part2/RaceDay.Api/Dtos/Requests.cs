using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record RegisterRequest(
    [Required, StringLength(50)] string FirstName,
    [Required, StringLength(50)] string LastName,
    [Required, EmailAddress, StringLength(100)] string Email,
    [Required, MinLength(8)] string Password,
    [StringLength(20)] string? PhoneNumber,
    DateOnly? DateOfBirth,
    [Required] string RoleName);

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record UpdateProfileRequest([Required, StringLength(50)] string FirstName, [Required, StringLength(50)] string LastName, [StringLength(20)] string? PhoneNumber, DateOnly? DateOfBirth);
public record EventRequest([Required, StringLength(100)] string EventName, [Required, StringLength(1000)] string Description, DateTime EventDate, [Required, StringLength(150)] string Location, [Range(0.01, 9999)] decimal Distance, [Required] string EventType);
public record CategoryRequest([Required, StringLength(100)] string CategoryName, [Required] string CategoryType, [StringLength(255)] string? Description);
public record EnrolmentRequest([Range(1, int.MaxValue)] int CategoryId);
public record StatusRequest([Required] string EnrolmentStatus);
public record ResultRequest(TimeSpan FinishTime, [Range(1, int.MaxValue)] int FinishingPosition);
