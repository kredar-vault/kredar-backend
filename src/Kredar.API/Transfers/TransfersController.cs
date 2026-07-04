using Kredar.API.Common;
using Kredar.API.Transfers.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Transfers;

[ApiController]
[Route("api/v1/transfers")]
[Authorize]
public class TransfersController(TransferService service) : ControllerBase
{
    [HttpPost("bank/lookup")]
    public async Task<IActionResult> Lookup([FromBody] BankLookupRequest req, CancellationToken ct)
    {
        var result = await service.LookupAsync(req.AccountNumber, req.BankCode, ct);
        return Ok(ApiResponse<BankLookupResponse>.Success(result));
    }

    [HttpPost("bank")]
    public async Task<IActionResult> Create([FromBody] CreateTransferRequest req, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.InitiateAsync(tenantId, req, ct);
        return Ok(ApiResponse<TransferResponse>.Success(result, "Transfer initiated."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.GetAllAsync(tenantId);
        return Ok(ApiResponse<List<TransferResponse>>.Success(result));
    }

    [HttpGet("{merchantTxRef}")]
    public async Task<IActionResult> GetByRef(string merchantTxRef)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.GetAsync(tenantId, merchantTxRef);
        return Ok(ApiResponse<TransferResponse>.Success(result));
    }
}
