namespace RaceDay.Api.Models;

// Store the race result details
public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }

    // Store the finishing time and position
    public TimeSpan FinishTime { get; set; }
    public int FinishingPosition { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    // Link the result to an enrolment
    public Enrolment Enrolment { get; set; } = null!;
}
