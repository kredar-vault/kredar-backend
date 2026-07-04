using Kredar.API.Common;
using Kredar.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Onboarding;

public class SubmitOnboardingRequest
{
    public string LegalName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? BusinessType { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }
    public string SettlementBankName { get; set; } = string.Empty;
    public string SettlementBankCode { get; set; } = string.Empty;
    public string SettlementAccountName { get; set; } = string.Empty;
    public string SettlementAccountNumber { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/onboarding")]
[Authorize]
public class OnboardingController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var app = await GetOrCreateAsync(tenantId, ct);
        return Ok(ApiResponse<OnboardingApplication>.Success(app));
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitOnboardingRequest request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var app = await GetOrCreateAsync(tenantId, ct);

        if (app.Status == OnboardingStatus.Approved)
            throw new Exception("Your account is already approved.");

        if (app.Status == OnboardingStatus.UnderReview)
            throw new Exception("Your application is already under review.");

        app.LegalName = request.LegalName;
        app.RegistrationNumber = request.RegistrationNumber;
        app.BusinessType = request.BusinessType;
        app.Industry = request.Industry;
        app.Country = request.Country;
        app.Address = request.Address;
        app.ContactPhone = request.ContactPhone;
        app.Website = request.Website;
        app.SettlementBankName = request.SettlementBankName;
        app.SettlementBankCode = request.SettlementBankCode;
        app.SettlementAccountName = request.SettlementAccountName;
        app.SettlementAccountNumber = request.SettlementAccountNumber;
        app.Status = OnboardingStatus.UnderReview;
        app.SubmittedAt = DateTime.UtcNow;

        db.OnboardingApplications.Update(app);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OnboardingApplication>.Success(app, "Application submitted for review."));
    }

    private async Task<OnboardingApplication> GetOrCreateAsync(Guid tenantId, CancellationToken ct)
    {
        var app = await db.OnboardingApplications.FirstOrDefaultAsync(a => a.TenantId == tenantId, ct);
        if (app != null) return app;
        app = new OnboardingApplication { TenantId = tenantId };
        db.OnboardingApplications.Add(app);
        await db.SaveChangesAsync(ct);
        return app;
    }
}
