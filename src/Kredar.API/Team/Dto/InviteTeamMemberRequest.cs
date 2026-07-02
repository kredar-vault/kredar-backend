using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Team.Dto;

public class InviteTeamMemberRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(Admin|Employee|Developer)$",
        ErrorMessage = "Role must be Admin, Employee, or Developer.")]
    public string Role { get; set; } = string.Empty;
}
