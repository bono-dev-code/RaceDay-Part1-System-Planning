using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public string? Error { get; init; }
    public T? Data { get; init; }
}

public class UserProfile
{
    public int UserID { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; } = "";
}

public class LoginResponse
{
    public string Message { get; set; } = "";
    public LoginUser User { get; set; } = new();
}

public class LoginUser
{
    public int UserID { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}

public class EventSummary
{
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public decimal Distance { get; set; }
    public string EventType { get; set; } = "";
    public string? BannerImageUrl { get; set; }
}

public class EventDetails : EventSummary
{
    public string Description { get; set; } = "";
    public OrganiserSummary Organiser { get; set; } = new();
    public List<CategoryModel> Categories { get; set; } = [];
}

public class OrganiserSummary
{
    public int UserID { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

public class CategoryModel
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = "";
    public string CategoryType { get; set; } = "";
    public string? Description { get; set; }
}

public class EnrolmentModel
{
    public int EnrolmentID { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string EnrolmentStatus { get; set; } = "";
    public ParticipantSummary? Participant { get; set; }
    public EventInfo EventInfo { get; set; } = new();
    public CategoryModel Category { get; set; } = new();
}

public class ParticipantSummary
{
    public int UserID { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
}

public class EventInfo
{
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
}

public class ResultModel
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Category { get; set; } = "";
    public TimeSpan FinishTime { get; set; }
    public int FinishingPosition { get; set; }
    public int TotalFinishers { get; set; }
    public DateTime PublishedAt { get; set; }
    public ParticipantSummary? Participant { get; set; }
    public EventInfo EventInfo { get; set; } = new();
}

public class DashboardModel
{
    public int TotalEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public int UpcomingEvents { get; set; }
    public List<DashboardEvent> Events { get; set; } = [];
}

public class DashboardEvent
{
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string EventType { get; set; } = "";
    public int EnrolmentCount { get; set; }
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
}

public class UploadResponse
{
    public string? BannerImageUrl { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class CreatedEvent
{
    public int EventID { get; set; }
}

public class CreatedEnrolment
{
    public int EnrolmentID { get; set; }
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class RegisterViewModel
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = "";

    [Required, StringLength(50)]
    public string LastName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [Required]
    public string RoleName { get; set; } = "Participant";
}

public class EventFormModel
{
    public int EventID { get; set; }
    [Required, StringLength(100)]
    public string EventName { get; set; } = "";

    [Required, StringLength(1000)]
    public string Description { get; set; } = "";

    [Required, DataType(DataType.DateTime)]
    public DateTime EventDate { get; set; } = DateTime.Now.AddDays(7);

    [Required, StringLength(150)]
    public string Location { get; set; } = "";

    [Range(0.01, 9999)]
    public decimal Distance { get; set; }

    [Required]
    public string EventType { get; set; } = "Run";
    public IFormFile? BannerImage { get; set; }
}

public class CategoryFormModel
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = "";

    [Required]
    public string CategoryType { get; set; } = "Age";

    [StringLength(255)]
    public string? Description { get; set; }
}

public class ProfileFormModel
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = "";

    [Required, StringLength(50)]
    public string LastName { get; set; } = "";

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }
    public IFormFile? ProfilePicture { get; set; }
}

public class ResultFormModel
{
    public int ResultID { get; set; }
    public int EventID { get; set; }
    public int EnrolmentID { get; set; }
    [Required]
    public TimeSpan FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int FinishingPosition { get; set; }
}

public class EventSearchViewModel
{
    public string? Type { get; set; }
    public DateTime? Date { get; set; }
    public string? Location { get; set; }
    public List<EventSummary> Events { get; set; } = [];
}
