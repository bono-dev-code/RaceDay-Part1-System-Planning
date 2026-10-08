using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Store the user role details
public class Role
{
    public int RoleID { get; set; }
    [Required, StringLength(20)] public string RoleName { get; set; } = string.Empty;

    // Store the users assigned to this role
    public ICollection<User> Users { get; set; } = new List<User>();
}
