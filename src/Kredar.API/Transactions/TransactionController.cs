using Kredar.API.Common;
using Kredar.API.Customers;
using Kredar.API.Transactions.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Transactions;

[ApiController]
[Authorize]
[Tags("Transactions")]
public class TransactionController(TransactionService transactionService) : ControllerBase
{
    [HttpGet("/api/v1/transactions")]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        TransactionStatus? parsedStatus = status is not null ? Enum.Parse<TransactionStatus>(status, true) : null;
        var transactions = await transactionService.GetAllAsync(tenantId, parsedStatus);
        return Ok(ApiResponse<List<TransactionResponse>>.Success(transactions));
    }

    [HttpGet("/api/v1/transactions/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var transaction = await transactionService.GetByIdAsync(tenantId, id);
        return Ok(ApiResponse<TransactionResponse>.Success(transaction));
    }

    [HttpPost("/api/v1/transactions")]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var transaction = await transactionService.CreateAsync(tenantId, request);
        return Ok(ApiResponse<TransactionResponse>.Success(transaction, "Transaction created successfully."));
    }

    [HttpGet("/api/v1/customers/{customerId:guid}/transactions")]
    public async Task<IActionResult> GetByCustomer(Guid customerId, [FromQuery] string? status = null)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        TransactionStatus? parsedStatus = status is not null ? Enum.Parse<TransactionStatus>(status, true) : null;
        var transactions = await transactionService.GetByCustomerAsync(tenantId, customerId, parsedStatus);
        return Ok(ApiResponse<List<TransactionResponse>>.Success(transactions));
    }

    [HttpGet("/api/v1/customers/{customerId:guid}/transactions/stats")]
    public async Task<IActionResult> GetCustomerStats(Guid customerId)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var stats = await transactionService.GetCustomerStatsAsync(tenantId, customerId);
        return Ok(ApiResponse<CustomerTransactionStatsResponse>.Success(stats));
    }
}
