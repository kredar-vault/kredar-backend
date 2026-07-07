using Kredar.API.Customers;
using Kredar.API.DedicatedAccounts.Dto;
using Kredar.API.Nomba;
using Kredar.API.Notifications;

namespace Kredar.API.DedicatedAccounts;

public class DedicatedAccountService(
    DedicatedAccountRepository repo,
    CustomerRepository customerRepo,
    NombaClient nombaClient,
    NotificationService notif)
{
    public async Task<DedicatedAccountResponse> CreateAsync(Guid tenantId, CreateDedicatedAccountRequest req, CancellationToken ct = default)
    {
        var customer = await customerRepo.FindByIdAsync(tenantId, req.CustomerId)
            ?? throw new KeyNotFoundException("Customer not found.");

        var existing = await repo.FindByCustomerAsync(tenantId, req.CustomerId);
        if (existing is not null)
            return Map(existing, customer);

        var reference = $"KRD-{Guid.NewGuid():N}";
        var accountName = $"{customer.FirstName} {customer.LastName}".Trim();

        var provisioned = await nombaClient.CreateDedicatedAccountAsync(
            reference, accountName, customer.Email, customer.PhoneNumber, ct);

        var account = new DedicatedAccount
        {
            TenantId = tenantId,
            CustomerId = customer.Id,
            Reference = reference,
            AccountNumber = provisioned.AccountNumber,
            BankName = provisioned.BankName,
            AccountName = provisioned.AccountName,
            ProviderAccountId = provisioned.ProviderId,
            ExpectedAmount = req.ExpectedAmount,
        };

        await repo.AddAsync(account);

        customer.DedicatedAccountNumber = provisioned.AccountNumber;
        await customerRepo.UpdateAsync(customer);

        _ = notif.CreateAsync(tenantId, NotificationType.DedicatedAccountCreated,
            "Dedicated account provisioned",
            $"Virtual account {provisioned.AccountNumber} ({provisioned.BankName}) was created for {accountName}.");

        return Map(account, customer);
    }

    public async Task<List<DedicatedAccountResponse>> GetAllAsync(Guid tenantId)
    {
        var accounts = await repo.GetAllAsync(tenantId);
        var result = new List<DedicatedAccountResponse>(accounts.Count);
        foreach (var a in accounts)
        {
            var customer = await customerRepo.FindByIdAsync(tenantId, a.CustomerId);
            result.Add(Map(a, customer));
        }
        return result;
    }

    public async Task<DedicatedAccountResponse> GetByIdAsync(Guid tenantId, Guid id)
    {
        var account = await repo.FindByIdAsync(tenantId, id)
            ?? throw new KeyNotFoundException("Dedicated account not found.");
        var customer = await customerRepo.FindByIdAsync(tenantId, account.CustomerId);
        return Map(account, customer);
    }

    private static DedicatedAccountResponse Map(DedicatedAccount a, Customer? c) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        CustomerName = c is null ? "" : $"{c.FirstName} {c.LastName}".Trim(),
        Reference = a.Reference,
        AccountNumber = a.AccountNumber,
        BankName = a.BankName,
        AccountName = a.AccountName,
        ExpectedAmount = a.ExpectedAmount,
        AmountPaid = a.AmountPaid,
        Status = a.Status.ToString(),
        PaymentState = a.PaymentState.ToString(),
        CreatedAt = a.CreatedAt,
    };
}
