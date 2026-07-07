using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Team.Dto;

public class AcceptInviteRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
