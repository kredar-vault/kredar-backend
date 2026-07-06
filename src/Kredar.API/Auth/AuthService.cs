using Kredar.API.Auth.Dto;
using Kredar.API.Tenants;

namespace Kredar.API.Auth;

public class AuthService(TenantRepository tenantRepo, JwtService jwtService, EmailService emailService, RefreshTokenRepository refreshTokenRepo)
{
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            throw new Exception("Passwords do not match.");

        var existing = await tenantRepo.FindByEmailAsync(request.Email);
        if (existing != null)
            throw new Exception("Email already registered.");

        var verificationToken = Guid.NewGuid().ToString("N");

        var tenant = new Tenant
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10),
            IsVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        await tenantRepo.AddAsync(tenant);
        _ = emailService.SendVerificationEmailAsync(tenant.Email, verificationToken);

        return new RegisterResponse
        {
            Message = "Registration successful. Please check your email to verify your account.",
            VerificationToken = verificationToken
        };
    }

    public async Task<string> VerifyEmailAsync(string token)
    {
        var tenant = await tenantRepo.FindByVerificationTokenAsync(token)
            ?? throw new Exception("Invalid or expired verification link.");

        if (tenant.EmailVerificationTokenExpiry < DateTime.UtcNow)
            throw new Exception("Verification link has expired. Please register again.");

        tenant.IsVerified = true;
        tenant.EmailVerificationToken = null;
        tenant.EmailVerificationTokenExpiry = null;

        await tenantRepo.UpdateAsync(tenant);

        return "Email verified successfully. You can now log in.";
    }

    public async Task<string> LoginAsync(LoginRequest request)
    {
        var tenant = await tenantRepo.FindByEmailAsync(request.Email)
            ?? throw new Exception("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, tenant.PasswordHash))
            throw new Exception("Invalid email or password.");

        if (!tenant.IsVerified)
            throw new Exception("Please verify your email before logging in.");

        var otp = Random.Shared.Next(100000, 999999).ToString();

        tenant.LoginOtp = otp;
        tenant.LoginOtpExpiry = DateTime.UtcNow.AddMinutes(10);
        await tenantRepo.UpdateAsync(tenant);

        _ = emailService.SendLoginOtpEmailAsync(tenant.Email, otp);

        return "A 6-digit code has been sent to your email.";
    }

    public async Task<AuthResponse> VerifyLoginOtpAsync(VerifyLoginOtpRequest request)
    {
        var tenant = await tenantRepo.FindByEmailAsync(request.Email)
            ?? throw new Exception("Invalid request.");

        if (tenant.LoginOtp == null || tenant.LoginOtpExpiry < DateTime.UtcNow)
            throw new Exception("Code has expired. Please log in again.");

        if (tenant.LoginOtp != request.Otp)
            throw new Exception("Invalid code. Please try again.");

        tenant.LoginOtp = null;
        tenant.LoginOtpExpiry = null;
        await tenantRepo.UpdateAsync(tenant);

        var refreshToken = new RefreshToken
        {
            TenantId = tenant.Id,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await refreshTokenRepo.AddAsync(refreshToken);

        return new AuthResponse
        {
            Token = jwtService.GenerateToken(tenant.Id, tenant.Email),
            RefreshToken = refreshToken.Token,
            BusinessName = tenant.BusinessName,
            Email = tenant.Email
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var tenant = await tenantRepo.FindByEmailAsync(request.Email);
        if (tenant == null) return; // silent — don't reveal if email exists

        var code = Random.Shared.Next(100000, 999999).ToString();
        tenant.PasswordResetToken = code;
        tenant.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await tenantRepo.UpdateAsync(tenant);

        _ = emailService.SendPasswordResetEmailAsync(tenant.Email, code);
    }

    public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new Exception("Passwords do not match.");

        var tenant = await tenantRepo.FindByEmailAsync(request.Email)
            ?? throw new Exception("Invalid request.");

        if (tenant.PasswordResetToken == null || tenant.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new Exception("Code has expired. Please request a new one.");

        if (tenant.PasswordResetToken != request.Code)
            throw new Exception("Invalid code. Please try again.");

        tenant.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 10);
        tenant.PasswordResetToken = null;
        tenant.PasswordResetTokenExpiry = null;
        await tenantRepo.UpdateAsync(tenant);

        return "Password reset successfully. You can now log in.";
    }

    public async Task<string> ResendVerificationAsync(string email)
    {
        var tenant = await tenantRepo.FindByEmailAsync(email)
            ?? throw new Exception("Email not found.");

        if (tenant.IsVerified)
            throw new Exception("This email is already verified.");

        var token = Guid.NewGuid().ToString("N");
        tenant.EmailVerificationToken = token;
        tenant.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await tenantRepo.UpdateAsync(tenant);

        _ = emailService.SendVerificationEmailAsync(tenant.Email, token);

        return "Verification email resent. Please check your inbox.";
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request)
    {
        var refreshToken = await refreshTokenRepo.FindByTokenAsync(request.RefreshToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");

        var tenant = await tenantRepo.FindByIdAsync(refreshToken.TenantId)
            ?? throw new UnauthorizedAccessException("Tenant not found.");

        await refreshTokenRepo.RevokeAsync(refreshToken);

        var newRefreshToken = new RefreshToken
        {
            TenantId = tenant.Id,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await refreshTokenRepo.AddAsync(newRefreshToken);

        return new AuthResponse
        {
            Token = jwtService.GenerateToken(tenant.Id, tenant.Email),
            RefreshToken = newRefreshToken.Token,
            BusinessName = tenant.BusinessName,
            Email = tenant.Email
        };
    }
}
