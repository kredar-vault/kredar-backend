using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kredar.API.Common;
using Kredar.API.Config;
using Kredar.API.Data;
using Kredar.API.Notifications;
using Kredar.API.Onboarding;
using Kredar.API.Tenants;
using Kredar.API.Transactions;
using Kredar.API.Transfers;
using Kredar.API.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OtpNet;

namespace Kredar.API.Admin;

public class AdminLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TotpCode { get; set; }
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

public class RequestInfoRequest
{
    public string Reason { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/admin")]
public class AdminController(AppDbContext db, IOptions<JwtSettings> jwtOptions, IConfiguration config, NotificationService notif) : ControllerBase
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    // -------------------------------------------------------------------------
    // Bootstrap — create the very first admin (requires X-Bootstrap-Secret header)
    // -------------------------------------------------------------------------

    [HttpPost("bootstrap")]
    [AllowAnonymous]
    public async Task<IActionResult> Bootstrap([FromBody] CreateAdminRequest request, CancellationToken ct)
    {
        var secret = config["AdminSettings:BootstrapSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            return NotFound();

        var provided = Request.Headers["X-Bootstrap-Secret"].FirstOrDefault();
        if (provided != secret)
            return Unauthorized(ApiResponse<object>.Fail("Invalid bootstrap secret."));

        var exists = await db.AdminUsers.AnyAsync(a => a.Email == request.Email, ct);
        if (exists)
            return BadRequest(ApiResponse<object>.Fail("Admin with this email already exists."));

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

    // -------------------------------------------------------------------------
    // Auth
    // -------------------------------------------------------------------------

    [HttpPost("auth/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken ct)
    {
        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Email == request.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (admin.MfaEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TotpCode))
                return BadRequest(ApiResponse<object>.Fail("MFA code required."));

            var secretBytes = Base32Encoding.ToBytes(admin.TotpSecret!);
            var totp = new Totp(secretBytes);
            if (!totp.VerifyTotp(request.TotpCode, out _, VerificationWindow.RfcSpecifiedNetworkDelay))
                throw new UnauthorizedAccessException("Invalid MFA code.");
        }

        var token = GenerateAdminToken(admin);
        return Ok(ApiResponse<object>.Success(new { token, role = admin.Role.ToString(), email = admin.Email, mfaEnabled = admin.MfaEnabled }));
    }

    // -------------------------------------------------------------------------
    // MFA
    // -------------------------------------------------------------------------

    [HttpPost("mfa/enroll")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EnrollMfa(CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirstValue("adminId")!);
        var admin = await db.AdminUsers.FindAsync(new object[] { adminId }, ct)
            ?? throw new Exception("Admin not found.");

        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(secretBytes);
        admin.TotpSecret = secret;
        admin.MfaEnabled = false;
        db.AdminUsers.Update(admin);
        await db.SaveChangesAsync(ct);

        var otpUri = $"otpauth://totp/Kredar:{Uri.EscapeDataString(admin.Email)}?secret={secret}&issuer=Kredar";
        return Ok(ApiResponse<object>.Success(new { otpAuthUri = otpUri, secret }, "Scan QR code then call /mfa/verify to activate."));
    }

    [HttpPost("mfa/verify")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirstValue("adminId")!);
        var admin = await db.AdminUsers.FindAsync(new object[] { adminId }, ct)
            ?? throw new Exception("Admin not found.");

        if (string.IsNullOrWhiteSpace(admin.TotpSecret))
            return BadRequest(ApiResponse<object>.Fail("Call /mfa/enroll first."));

        var secretBytes = Base32Encoding.ToBytes(admin.TotpSecret);
        var totp = new Totp(secretBytes);
        if (!totp.VerifyTotp(request.Code, out _, VerificationWindow.RfcSpecifiedNetworkDelay))
            return BadRequest(ApiResponse<object>.Fail("Invalid code."));

        admin.MfaEnabled = true;
        db.AdminUsers.Update(admin);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { }, "MFA enabled successfully."));
    }

    // -------------------------------------------------------------------------
    // Admin management
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Tenant / onboarding review
    // -------------------------------------------------------------------------

    [HttpGet("tenants")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListTenants([FromQuery] string? status, CancellationToken ct)
    {
        var tenants = await db.Tenants.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
        var tenantIds = tenants.Select(t => t.Id).ToList();
        var appMap = await db.OnboardingApplications
            .Where(a => tenantIds.Contains(a.TenantId))
            .ToDictionaryAsync(a => a.TenantId, ct);

        var result = tenants.Select(t =>
        {
            appMap.TryGetValue(t.Id, out var app);
            return new
            {
                tenantId = t.Id,
                email = t.Email,
                businessName = t.BusinessName,
                isVerified = t.IsVerified,
                isSuspended = t.IsSuspended,
                createdAt = t.CreatedAt,
                onboardingStatus = app?.Status.ToString() ?? "NotStarted",
                submittedAt = app?.SubmittedAt,
            };
        });

        if (!string.IsNullOrWhiteSpace(status))
            result = result.Where(r => r.onboardingStatus.Equals(status, StringComparison.OrdinalIgnoreCase));

        return Ok(ApiResponse<object>.Success(result.ToList()));
    }

    [HttpGet("tenants/{tenantId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetTenant(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync(new object[] { tenantId }, ct)
            ?? throw new Exception("Tenant not found.");
        var app = await db.OnboardingApplications.FirstOrDefaultAsync(a => a.TenantId == tenantId, ct);
        var apiKeys = await db.ApiKeys.Where(k => k.TenantId == tenantId).ToListAsync(ct);
        return Ok(ApiResponse<object>.Success(new { tenant, onboarding = app, apiKeys }));
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
        await WriteAuditAsync("ApproveOnboarding", tenantId, request?.Reason, ct);
        await db.SaveChangesAsync(ct);
        _ = notif.CreateAsync(tenantId, NotificationType.OnboardingApproved,
            "Application approved",
            "Your KYB application has been approved. You now have live access.");
        return Ok(ApiResponse<object>.Success(new { }, "Tenant approved for live."));
    }

    [HttpPost("tenants/{tenantId:guid}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Reject(Guid tenantId, [FromBody] ReviewRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(ApiResponse<object>.Fail("Reason is required for rejection."));

        var app = await db.OnboardingApplications.FirstOrDefaultAsync(a => a.TenantId == tenantId, ct)
            ?? throw new Exception("No onboarding application found.");
        app.Status = OnboardingStatus.Rejected;
        app.DecisionReason = request.Reason;
        app.DecidedAt = DateTime.UtcNow;
        db.OnboardingApplications.Update(app);
        await WriteAuditAsync("RejectOnboarding", tenantId, request.Reason, ct);
        await db.SaveChangesAsync(ct);
        _ = notif.CreateAsync(tenantId, NotificationType.OnboardingRejected,
            "Application rejected",
            $"Your KYB application was rejected. Reason: {request.Reason}");
        return Ok(ApiResponse<object>.Success(new { }, "Tenant rejected."));
    }

    [HttpPost("tenants/{tenantId:guid}/request-info")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RequestInfo(Guid tenantId, [FromBody] RequestInfoRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(ApiResponse<object>.Fail("Reason is required."));

        var app = await db.OnboardingApplications.FirstOrDefaultAsync(a => a.TenantId == tenantId, ct)
            ?? throw new Exception("No onboarding application found.");
        app.Status = OnboardingStatus.MoreInfoRequired;
        app.DecisionReason = request.Reason;
        app.DecidedAt = DateTime.UtcNow;
        db.OnboardingApplications.Update(app);
        await WriteAuditAsync("RequestMoreInfo", tenantId, request.Reason, ct);
        await db.SaveChangesAsync(ct);
        _ = notif.CreateAsync(tenantId, NotificationType.OnboardingMoreInfoRequired,
            "More information required",
            $"Your KYB application needs additional information: {request.Reason}");
        return Ok(ApiResponse<object>.Success(new { }, "Tenant notified to provide more information."));
    }

    [HttpPost("tenants/{tenantId:guid}/suspend")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Suspend(Guid tenantId, [FromBody] ReviewRequest? request, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync(new object[] { tenantId }, ct)
            ?? throw new Exception("Tenant not found.");
        tenant.IsSuspended = true;
        db.Tenants.Update(tenant);

        // Revoke all active API keys immediately
        var activeKeys = await db.ApiKeys
            .Where(k => k.TenantId == tenantId && k.Status == ApiKeys.ApiKeyStatus.Active)
            .ToListAsync(ct);
        foreach (var key in activeKeys)
            key.Status = ApiKeys.ApiKeyStatus.Revoked;

        await WriteAuditAsync("SuspendTenant", tenantId, request?.Reason, ct);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { }, "Tenant suspended and all API keys revoked."));
    }

    [HttpPost("tenants/{tenantId:guid}/unsuspend")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Unsuspend(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync(new object[] { tenantId }, ct)
            ?? throw new Exception("Tenant not found.");
        tenant.IsSuspended = false;
        db.Tenants.Update(tenant);
        await WriteAuditAsync("UnsuspendTenant", tenantId, null, ct);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { }, "Tenant unsuspended."));
    }

    // -------------------------------------------------------------------------
    // Reconciliation terminal
    // -------------------------------------------------------------------------

    [HttpGet("reconciliation")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ReconciliationBucket([FromQuery] string? bucket, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var query = db.Transactions.AsQueryable();
        query = bucket?.ToLowerInvariant() switch
        {
            "overpaid"  => query.Where(t => t.Status == TransactionStatus.Overpaid),
            "underpaid" => query.Where(t => t.Status == TransactionStatus.Underpaid),
            "reversed"  => query.Where(t => t.Status == TransactionStatus.Reversed),
            "failed"    => query.Where(t => t.Status == TransactionStatus.Failed),
            "pending"   => query.Where(t => t.Status == TransactionStatus.Pending),
            _           => query
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Success(new { total, page, pageSize, items }));
    }

    [HttpGet("reconciliation/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ReconciliationSummary(CancellationToken ct)
    {
        var summary = new
        {
            reconciled   = await db.Transactions.CountAsync(t => t.Status == TransactionStatus.Reconciled, ct),
            underpaid    = await db.Transactions.CountAsync(t => t.Status == TransactionStatus.Underpaid, ct),
            overpaid     = await db.Transactions.CountAsync(t => t.Status == TransactionStatus.Overpaid, ct),
            reversed     = await db.Transactions.CountAsync(t => t.Status == TransactionStatus.Reversed, ct),
            failed       = await db.Transactions.CountAsync(t => t.Status == TransactionStatus.Failed, ct),
            totalTenants = await db.Tenants.CountAsync(ct),
            pendingReview = await db.OnboardingApplications.CountAsync(a => a.Status == OnboardingStatus.UnderReview, ct),
        };
        return Ok(ApiResponse<object>.Success(summary));
    }

    [HttpPost("reconciliation/{txId:guid}/force-reconcile")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ForceReconcile(Guid txId, CancellationToken ct)
    {
        var tx = await db.Transactions.FindAsync(new object[] { txId }, ct)
            ?? throw new Exception("Transaction not found.");
        tx.Status = TransactionStatus.Reconciled;
        db.Transactions.Update(tx);
        await WriteAuditAsync("ForceReconcile", tx.TenantId, $"txId={txId}", ct);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { }, "Transaction force-reconciled."));
    }

    [HttpPost("reconciliation/{txId:guid}/reverse")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Reverse(Guid txId, [FromBody] ReviewRequest? request, CancellationToken ct)
    {
        var tx = await db.Transactions.FindAsync(new object[] { txId }, ct)
            ?? throw new Exception("Transaction not found.");
        tx.Status = TransactionStatus.Reversed;
        db.Transactions.Update(tx);
        await WriteAuditAsync("ReverseTransaction", tx.TenantId, $"txId={txId} reason={request?.Reason}", ct);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { }, "Transaction reversed."));
    }

    // -------------------------------------------------------------------------
    // Webhook delivery management
    // -------------------------------------------------------------------------

    [HttpGet("webhooks/failed")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> FailedWebhooks([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var total = await db.WebhookDeliveries.CountAsync(d => d.Status == WebhookDeliveryStatus.Failed, ct);
        var items = await db.WebhookDeliveries
            .Where(d => d.Status == WebhookDeliveryStatus.Failed)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Success(new { total, page, pageSize, items }));
    }

    [HttpPost("webhooks/{deliveryId:guid}/retry")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RetryWebhook(Guid deliveryId, CancellationToken ct)
    {
        var delivery = await db.WebhookDeliveries.FindAsync(new object[] { deliveryId }, ct)
            ?? throw new Exception("Delivery not found.");
        delivery.Replay();
        db.WebhookDeliveries.Update(delivery);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { }, "Delivery queued for retry."));
    }

    // -------------------------------------------------------------------------
    // Audit log
    // -------------------------------------------------------------------------

    [HttpGet("audit")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AuditLog([FromQuery] Guid? tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var query = db.AdminAuditLogs.AsQueryable();
        if (tenantId.HasValue) query = query.Where(a => a.TargetTenantId == tenantId);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Success(new { total, page, pageSize, items }));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task WriteAuditAsync(string action, Guid? targetTenantId, string? detail, CancellationToken ct)
    {
        var adminId = User.FindFirstValue("adminId");
        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
        db.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminId = adminId != null ? Guid.Parse(adminId) : Guid.Empty,
            AdminEmail = adminEmail,
            Action = action,
            TargetTenantId = targetTenantId,
            Detail = detail
        });
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

public class VerifyMfaRequest
{
    public string Code { get; set; } = string.Empty;
}
