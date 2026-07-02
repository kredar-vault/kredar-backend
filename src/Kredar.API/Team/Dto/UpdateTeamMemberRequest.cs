using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Team.Dto;

public class UpdateTeamMemberRequest
{
    [MaxLength(100)]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(Admin|Employee|Developer)$",
        ErrorMessage = "Role must be Admin, Employee, or Developer.")]
    public string Role { get; set; } = string.Empty;
}
