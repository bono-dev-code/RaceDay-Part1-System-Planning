using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    [Required, StringLength(100)] public string CategoryName { get; set; } = string.Empty;
    [Required, StringLength(20)] public string CategoryType { get; set; } = string.Empty;
    [StringLength(255)] public string? Description { get; set; }
    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
