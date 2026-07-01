using Kredar.API.Auth.Dto;
using Kredar.API.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Auth;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return Ok(ApiResponse<RegisterResponse>.Success(result));
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var message = await authService.VerifyEmailAsync(token);
        return Ok(ApiResponse<string>.Success(message));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var message = await authService.LoginAsync(request);
        return Ok(ApiResponse<string>.Success(message));
    }

    [HttpPost("login/verify")]
    public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpRequest request)
    {
        var result = await authService.VerifyLoginOtpAsync(request);
        return Ok(ApiResponse<AuthResponse>.Success(result, "Login successful."));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await authService.RefreshAsync(request);
        return Ok(ApiResponse<AuthResponse>.Success(result, "Token refreshed."));
    }
}
