namespace Kredar.API.Auth.Dto;

public class RegisterResponse
{
    public string Message { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = string.Empty;
}
