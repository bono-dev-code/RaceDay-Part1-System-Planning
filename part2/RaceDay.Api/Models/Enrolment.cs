using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Store the enrolment details
public class Enrolment
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int EventID { get; set; }
    public int CategoryID { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    [Required, StringLength(20)] public string EnrolmentStatus { get; set; } = "Pending";

    // Link the enrolment to the participant, event and category
    public User Participant { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;

    // Store the result for this enrolment
    public Result? Result { get; set; }
}
