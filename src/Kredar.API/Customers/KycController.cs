using Kredar.API.Common;
using Kredar.API.Customers.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Customers;

[ApiController]
[Route("api/v1/customers")]
[Authorize]
[Tags("KYC")]
public class KycController(KycService kycService) : ControllerBase
{
    [HttpGet("{id:guid}/kyc")]
    public async Task<IActionResult> GetDocuments(Guid id)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var docs = await kycService.GetDocumentsAsync(tenantId, id);
        return Ok(ApiResponse<List<KycDocumentResponse>>.Success(docs));
    }

    [HttpPost("{id:guid}/kyc")]
    public async Task<IActionResult> SubmitDocument(Guid id, [FromBody] SubmitKycDocumentRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var doc = await kycService.SubmitDocumentAsync(tenantId, id, request);
        return Ok(ApiResponse<KycDocumentResponse>.Success(doc, "KYC document submitted successfully."));
    }

    [HttpPatch("kyc/{docId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid docId, [FromBody] UpdateKycDocumentStatusRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var status = Enum.Parse<KycDocumentStatus>(request.Status);
        var doc = await kycService.UpdateDocumentStatusAsync(tenantId, docId, status);
        return Ok(ApiResponse<KycDocumentResponse>.Success(doc, "KYC document status updated."));
    }
}
