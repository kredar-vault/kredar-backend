using Kredar.API.Customers;
using Kredar.API.Transactions.Dto;

namespace Kredar.API.Transactions;

public class TransactionService(TransactionRepository transactionRepo, CustomerRepository customerRepo)
{
    public async Task<List<TransactionResponse>> GetAllAsync(Guid tenantId, TransactionStatus? status = null) =>
        (await transactionRepo.GetAllAsync(tenantId, status)).Select(t => MapToResponse(t)).ToList();

    public async Task<List<TransactionResponse>> GetByCustomerAsync(Guid tenantId, Guid customerId, TransactionStatus? status = null) =>
        (await transactionRepo.GetByCustomerAsync(tenantId, customerId, status)).Select(t => MapToResponse(t)).ToList();

    public async Task<TransactionResponse> GetByIdAsync(Guid tenantId, Guid id)
    {
        var tx = await transactionRepo.FindByIdAsync(tenantId, id)
            ?? throw new KeyNotFoundException("Transaction not found.");

        string? customerName = null;
        if (tx.CustomerId.HasValue)
        {
            var customer = await customerRepo.FindByIdAsync(tenantId, tx.CustomerId.Value);
            if (customer is not null)
                customerName = $"{customer.FirstName} {customer.LastName}";
        }

        return MapToResponse(tx, customerName);
    }

    public async Task<CustomerTransactionStatsResponse> GetCustomerStatsAsync(Guid tenantId, Guid customerId) => new()
    {
        TotalPaymentsToday = await transactionRepo.SumTodayAsync(tenantId, customerId),
        PendingTransactions = await transactionRepo.CountByStatusAsync(tenantId, customerId, TransactionStatus.Pending),
        Exceptions = await transactionRepo.CountExceptionsAsync(tenantId, customerId)
    };

    public async Task<TransactionResponse> CreateAsync(Guid tenantId, CreateTransactionRequest request)
    {
        var tx = new Transaction
        {
            TenantId = tenantId,
            CustomerId = request.CustomerId,
            Reference = GenerateReference(),
            PaymentReference = request.PaymentReference,
            Amount = request.Amount,
            Fee = request.Fee,
            Currency = request.Currency,
            PaymentMethod = request.PaymentMethod,
            DedicatedAccountNumber = request.DedicatedAccountNumber,
            Narration = request.Narration,
            ExpectedAmount = request.ExpectedAmount ?? request.Amount,
            AmountReceived = null,
            Status = TransactionStatus.Pending
        };

        await transactionRepo.AddAsync(tx);
        return MapToResponse(tx);
    }

    private static string GenerateReference() =>
        $"TRX{DateTime.UtcNow.Year}{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

    private static TransactionResponse MapToResponse(Transaction t, string? customerName = null) => new()
    {
        Id = t.Id,
        Reference = t.Reference,
        PaymentReference = t.PaymentReference,
        Status = t.Status.ToString(),
        Amount = t.Amount,
        Fee = t.Fee,
        Currency = t.Currency,
        PaymentMethod = t.PaymentMethod,
        CreatedAt = t.CreatedAt,
        CustomerId = t.CustomerId,
        CustomerName = customerName,
        DedicatedAccountNumber = t.DedicatedAccountNumber,
        Narration = t.Narration,
        ExpectedAmount = t.ExpectedAmount,
        AmountReceived = t.AmountReceived,
        Difference = t.ExpectedAmount.HasValue && t.AmountReceived.HasValue
            ? Math.Abs(t.AmountReceived.Value - t.ExpectedAmount.Value)
            : null
    };
}
