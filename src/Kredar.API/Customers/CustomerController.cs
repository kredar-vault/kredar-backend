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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var customer = await customerService.CreateAsync(tenantId, request);
        return Ok(ApiResponse<CustomerResponse>.Success(customer, "Customer created successfully."));
    }
}
