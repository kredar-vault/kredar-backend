namespace Kredar.API.Auth.Dto;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
