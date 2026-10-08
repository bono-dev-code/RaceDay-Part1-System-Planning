
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

// Store the details needed to register a user
public record RegisterRequest(
    [Required, StringLength(50)] string FirstName,
    [Required, StringLength(50)] string LastName,
    [Required, EmailAddress, StringLength(100)] string Email,
    [Required, MinLength(8)] string Password,
    [StringLength(20)] string? PhoneNumber,
    DateOnly? DateOfBirth,
    [Required] string RoleName);

// Store the user's login details
public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

// Store the details needed to update a profile
public record UpdateProfileRequest([Required, StringLength(50)] string FirstName, [Required, StringLength(50)] string LastName, [StringLength(20)] string? PhoneNumber, DateOnly? DateOfBirth);

// Store the details needed to create or update an event
public record EventRequest([Required, StringLength(100)] string EventName, [Required, StringLength(1000)] string Description, DateTime EventDate, [Required, StringLength(150)] string Location, [Range(0.01, 9999)] decimal Distance, [Required] string EventType);

// Store the category details
public record CategoryRequest([Required, StringLength(100)] string CategoryName, [Required] string CategoryType, [StringLength(255)] string? Description);

// Store the selected category for enrolment
public record EnrolmentRequest([Range(1, int.MaxValue)] int CategoryId);

// Store the enrolment status
public record StatusRequest([Required] string EnrolmentStatus);

// Store the race result details
public record ResultRequest(TimeSpan FinishTime, [Range(1, int.MaxValue)] int FinishingPosition);
