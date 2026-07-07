using Kredar.API.Auth.Dto;
using Kredar.API.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Auth;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService authService, IConfiguration config) : ControllerBase
{
    private string FrontendUrl => config["AppSettings:FrontendUrl"] ?? "http://localhost:3000";

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return Ok(ApiResponse<RegisterResponse>.Success(result));
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        try
        {
            await authService.VerifyEmailAsync(token);
            return Redirect($"{FrontendUrl}/auth/email-verified?verified=true");
        }
        catch
        {
            return Redirect($"{FrontendUrl}/auth/email-verified?verified=false");
        }
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ForgotPasswordRequest request)
    {
        var message = await authService.ResendVerificationAsync(request.Email);
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await authService.ForgotPasswordAsync(request);
        return Ok(ApiResponse<string>.Success("If this email is registered, a reset link has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var message = await authService.ResetPasswordAsync(request);
        return Ok(ApiResponse<string>.Success(message));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await authService.RefreshAsync(request);
        return Ok(ApiResponse<AuthResponse>.Success(result, "Token refreshed."));
    }
}
