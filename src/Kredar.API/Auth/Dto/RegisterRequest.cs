namespace Kredar.API.Auth.Dto;

public class RegisterRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
