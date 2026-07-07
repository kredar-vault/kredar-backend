using Kredar.API.Common;
using Kredar.API.Data;
using Kredar.API.Transfers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Onboarding;

public class SubmitDeveloperKycRequest
{
    public string FullName { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string IdType { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? PortfolioUrl { get; set; }
    public string? ProjectDescription { get; set; }
}

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
public class OnboardingController(AppDbContext db, TransferService transferService, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var app = await GetOrCreateAsync(tenantId, ct);
        return Ok(ApiResponse<OnboardingApplication>.Success(app));
    }

    [HttpPost("developer")]
    public async Task<IActionResult> SubmitDeveloper([FromBody] SubmitDeveloperKycRequest request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var app = await GetOrCreateAsync(tenantId, ct);

        if (app.DeveloperKycStatus == KycStatus.Approved)
            return BadRequest(ApiResponse<string>.Fail("Developer KYC is already approved."));

        if (!Enum.TryParse<GovIdType>(request.IdType, ignoreCase: true, out var idType))
            return BadRequest(ApiResponse<string>.Fail("IdType must be 'Bvn' or 'Nin'."));

        app.DevFullName = request.FullName;
        app.DevDateOfBirth = request.DateOfBirth;
        app.DevCountry = request.Country;
        app.DevAddress = request.Address;
        app.DevGovIdType = idType;
        app.DevGovIdNumber = request.IdNumber;
        app.DevBankName = request.BankName;
        app.DevBankCode = request.BankCode;
        app.DevAccountName = request.AccountName;
        app.DevAccountNumber = request.AccountNumber;
        app.PortfolioUrl = request.PortfolioUrl;
        app.ProjectDescription = request.ProjectDescription;
        app.DeveloperKycStatus = KycStatus.UnderReview;

        db.OnboardingApplications.Update(app);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OnboardingApplication>.Success(app, "Developer KYC submitted for review."));
    }

    [HttpPost("documents")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument([FromForm] string type, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("A file is required."));

        if (!Enum.TryParse<KycDocumentType>(type, ignoreCase: true, out var docType))
            return BadRequest(ApiResponse<string>.Fail("type must be 'CertificateOfIncorporation' or 'ProofOfAddress'."));

        var allowed = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(ApiResponse<string>.Fail("Only PDF, JPG, and PNG files are allowed."));

        var tenantId = TenantContext.GetTenantId(HttpContext);
        var app = await GetOrCreateAsync(tenantId, ct);

        var uploadDir = Path.Combine(env.ContentRootPath, "uploads", "onboarding", tenantId.ToString());
        Directory.CreateDirectory(uploadDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{docType}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);

        await using (var stream = System.IO.File.Create(filePath))
            await file.CopyToAsync(stream, ct);

        var existing = app.Documents.FirstOrDefault(d => d.DocumentType == docType);
        if (existing is not null)
        {
            if (System.IO.File.Exists(existing.FilePath))
                System.IO.File.Delete(existing.FilePath);
            db.OnboardingDocuments.Remove(existing);
        }

        var doc = new OnboardingDocument
        {
            OnboardingApplicationId = app.Id,
            DocumentType = docType,
            FileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType
        };

        db.OnboardingDocuments.Add(doc);
        await db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Success(new { doc.Id, doc.DocumentType, doc.FileName, doc.UploadedAt }, "Document uploaded."));
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

        try
        {
            var lookup = await transferService.LookupAsync(request.SettlementAccountNumber, request.SettlementBankCode, ct);
            app.SettlementAccountName = lookup.AccountName;
        }
        catch
        {
            return BadRequest(ApiResponse<string>.Fail("Could not verify the settlement account number. Please check and try again."));
        }

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
        app.SettlementAccountNumber = request.SettlementAccountNumber;
        app.BusinessKybStatus = KycStatus.UnderReview;
        app.Status = OnboardingStatus.UnderReview;
        app.SubmittedAt = DateTime.UtcNow;

        db.OnboardingApplications.Update(app);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OnboardingApplication>.Success(app, "Application submitted for review."));
    }

    private async Task<OnboardingApplication> GetOrCreateAsync(Guid tenantId, CancellationToken ct)
    {
        var app = await db.OnboardingApplications
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.TenantId == tenantId, ct);
        if (app != null) return app;
        app = new OnboardingApplication { TenantId = tenantId };
        db.OnboardingApplications.Add(app);
        await db.SaveChangesAsync(ct);
        return app;
    }
}
