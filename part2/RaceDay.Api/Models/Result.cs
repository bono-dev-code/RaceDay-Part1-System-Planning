namespace RaceDay.Api.Models;

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int FinishingPosition { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public Enrolment Enrolment { get; set; } = null!;
}
