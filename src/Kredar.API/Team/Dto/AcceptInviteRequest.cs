using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Team.Dto;

public class AcceptInviteRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}
