using Kredar.API.Common;
using Kredar.API.Customers.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Customers;

[ApiController]
[Route("api/v1/customers")]
[Authorize]
public class CustomerController(CustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var customers = await customerService.GetAllAsync(tenantId);
        return Ok(ApiResponse<List<CustomerResponse>>.Success(customers));
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var customers = await customerService.GetByStatusAsync(tenantId, CustomerStatus.Active);
        return Ok(ApiResponse<List<CustomerResponse>>.Success(customers));
    }

    [HttpGet("inactive")]
    public async Task<IActionResult> GetInactive()
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var customers = await customerService.GetByStatusAsync(tenantId, CustomerStatus.Inactive);
        return Ok(ApiResponse<List<CustomerResponse>>.Success(customers));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var stats = await customerService.GetStatsAsync(tenantId);
        return Ok(ApiResponse<CustomerStatsResponse>.Success(stats));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var customer = await customerService.GetByIdAsync(tenantId, id);
        return Ok(ApiResponse<CustomerResponse>.Success(customer));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateCustomerStatusRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var status = Enum.Parse<CustomerStatus>(request.Status);
        var customer = await customerService.UpdateStatusAsync(tenantId, id, status);
        return Ok(ApiResponse<CustomerResponse>.Success(customer, "Customer status updated."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var customer = await customerService.CreateAsync(tenantId, request);
        return Ok(ApiResponse<CustomerResponse>.Success(customer, "Customer created successfully."));
    }
}
