using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    [Required, StringLength(100)] public string EventName { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    [Required, StringLength(150)] public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    [Required, StringLength(10)] public string EventType { get; set; } = string.Empty;
    [StringLength(500)] public string? BannerImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
