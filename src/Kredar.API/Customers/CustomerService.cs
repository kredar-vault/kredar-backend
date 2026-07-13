using Kredar.API.Customers.Dto;
using Kredar.API.DedicatedAccounts;
using Kredar.API.DedicatedAccounts.Dto;
using Kredar.API.Notifications;

namespace Kredar.API.Customers;

public class CustomerService(CustomerRepository customerRepo, NotificationService notif, DedicatedAccountService dvaService, ILogger<CustomerService> logger)
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

    public async Task<CustomerResponse> GetByIdAsync(Guid tenantId, Guid customerId)
    {
        var customer = await customerRepo.FindByIdAsync(tenantId, customerId)
            ?? throw new KeyNotFoundException("Customer not found.");
        return MapToResponse(customer);
    }

    public async Task<CustomerResponse> UpdateStatusAsync(Guid tenantId, Guid customerId, CustomerStatus newStatus)
    {
        var customer = await customerRepo.FindByIdAsync(tenantId, customerId)
            ?? throw new KeyNotFoundException("Customer not found.");
        customer.Status = newStatus;
        await customerRepo.UpdateAsync(customer);
        return MapToResponse(customer);
    }

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

        try
        {
            await dvaService.CreateAsync(tenantId, new CreateDedicatedAccountRequest { CustomerId = customer.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DVA provisioning failed for customer {CustomerId} ({Email}) — Nomba error: {Message}", customer.Id, customer.Email, ex.Message);
        }

        _ = notif.CreateAsync(tenantId, NotificationType.CustomerCreated,
            "New customer added",
            $"{request.FirstName} {request.LastName} ({request.Email}) was added as a customer.");
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
