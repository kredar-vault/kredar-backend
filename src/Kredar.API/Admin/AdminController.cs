using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kredar.API.Common;
using Kredar.API.Config;
using Kredar.API.Data;
using Kredar.API.Onboarding;
using Kredar.API.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kredar.API.Admin;

public class AdminLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CreateAdminRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
}

public class ReviewRequest
{
    public string? Reason { get; set; }
}

[ApiController]
[Route("api/v1/admin")]
public class AdminController(AppDbContext db, IOptions<JwtSettings> jwtOptions) : ControllerBase
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    [HttpPost("auth/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken ct)
    {
        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Email == request.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = GenerateAdminToken(admin);
        return Ok(ApiResponse<object>.Success(new { token, role = admin.Role.ToString(), email = admin.Email }));
    }

    [HttpPost("admins")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request, CancellationToken ct)
    {
        var exists = await db.AdminUsers.AnyAsync(a => a.Email == request.Email, ct);
        if (exists) throw new Exception("Admin with this email already exists.");

        var role = request.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
            ? AdminRole.SuperAdmin : AdminRole.Admin;

        var admin = new AdminUser
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10),
            Role = role
        };
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { admin.Id, admin.Email, role = admin.Role.ToString() }, "Admin created."));
    }

    [HttpGet("tenants")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListTenants([FromQuery] string? status, CancellationToken ct)
    {
        OnboardingStatus? filter = Enum.TryParse<OnboardingStatus>(status, true, out var s) ? s : null;
        var query = db.OnboardingApplications.Include(a => a.Tenant).AsQueryable();
        if (filter.HasValue) query = query.Where(a => a.Status == filter.Value);
        var apps = await query.OrderByDescending(a => a.SubmittedAt).ToListAsync(ct);
        return Ok(ApiResponse<object>.Success(apps));
    }

    [HttpGet("tenants/{tenantId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetTenant(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync(new object[] { tenantId }, ct)
            ?? throw new Exception("Tenant not found.");
        var app = await db.OnboardingApplications.FirstOrDefaultAsync(a => a.TenantId == tenantId, ct);
        return Ok(ApiResponse<object>.Success(new { tenant, onboarding = app }));
    }

    [HttpPost("tenants/{tenantId:guid}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Approve(Guid tenantId, [FromBody] ReviewRequest? request, CancellationToken ct)
    {
        var app = await db.OnboardingApplications.FirstOrDefaultAsync(a => a.TenantId == tenantId, ct)
            ?? throw new Exception("No onboarding application found.");
        app.Status = OnboardingStatus.Approved;
        app.Tier = OnboardingTier.Live;
        app.DecisionReason = request?.Reason;
        app.DecidedAt = DateTime.UtcNow;
        db.OnboardingApplications.Update(app);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { }, "Tenant approved for live."));
    }

    [HttpPost("tenants/{tenantId:guid}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Reject(Guid tenantId, [FromBody] ReviewRequest? request, CancellationToken ct)
    {
        var app = await db.OnboardingApplications.FirstOrDefaultAsync(a => a.TenantId == tenantId, ct)
            ?? throw new Exception("No onboarding application found.");
        app.Status = OnboardingStatus.Rejected;
        app.DecisionReason = request?.Reason;
        app.DecidedAt = DateTime.UtcNow;
        db.OnboardingApplications.Update(app);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { }, "Tenant rejected."));
    }

    [HttpGet("reconciliation/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ReconciliationSummary(CancellationToken ct)
    {
        var summary = new
        {
            reconciled = await db.Transactions.CountAsync(t => t.Status == Transactions.TransactionStatus.Reconciled, ct),
            underpaid = await db.Transactions.CountAsync(t => t.Status == Transactions.TransactionStatus.Underpaid, ct),
            overpaid = await db.Transactions.CountAsync(t => t.Status == Transactions.TransactionStatus.Overpaid, ct),
            reversed = await db.Transactions.CountAsync(t => t.Status == Transactions.TransactionStatus.Reversed, ct),
            totalTenants = await db.Tenants.CountAsync(ct),
            pendingReview = await db.OnboardingApplications.CountAsync(a => a.Status == OnboardingStatus.UnderReview, ct),
        };
        return Ok(ApiResponse<object>.Success(summary));
    }

    private string GenerateAdminToken(AdminUser admin)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim("adminId", admin.Id.ToString()),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.Role, admin.Role.ToString()),
            new Claim("scope", "admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer, audience: _jwt.Audience,
            claims: claims, expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
