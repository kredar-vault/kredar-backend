using Kredar.API.Customers.Dto;

namespace Kredar.API.Customers;

public class CustomerService(CustomerRepository customerRepo)
{
    public async Task<List<CustomerResponse>> GetAllAsync(Guid tenantId) =>
        (await customerRepo.GetAllAsync(tenantId)).Select(MapToResponse).ToList();

    public async Task<List<CustomerResponse>> GetByStatusAsync(Guid tenantId, CustomerStatus status) =>
        (await customerRepo.GetByStatusAsync(tenantId, status)).Select(MapToResponse).ToList();

    public async Task<CustomerStatsResponse> GetStatsAsync(Guid tenantId) => new()
    {
        TotalCustomers = await customerRepo.CountAsync(tenantId),
        ActiveCustomers = await customerRepo.CountByStatusAsync(tenantId, CustomerStatus.Active),
        InactiveCustomers = await customerRepo.CountByStatusAsync(tenantId, CustomerStatus.Inactive)
    };

    public async Task<CustomerResponse> CreateAsync(Guid tenantId, CreateCustomerRequest request)
    {
        var exists = await customerRepo.FindByEmailAsync(tenantId, request.Email);
        if (exists is not null)
            throw new InvalidOperationException("A customer with this email already exists.");

        var customer = new Customer
        {
            TenantId = tenantId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        await customerRepo.AddAsync(customer);
        return MapToResponse(customer);
    }

    private static CustomerResponse MapToResponse(Customer c) => new()
    {
        Id = c.Id,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Email = c.Email,
        PhoneNumber = c.PhoneNumber,
        DedicatedAccountNumber = c.DedicatedAccountNumber,
        KycStatus = c.KycStatus.ToString(),
        Status = c.Status.ToString(),
        CreatedAt = c.CreatedAt
    };
}
