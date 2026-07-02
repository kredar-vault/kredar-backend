using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Transactions.Dto;

public class CreateTransactionRequest
{
    public Guid? CustomerId { get; set; }

    public string? PaymentReference { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Fee { get; set; } = 0;

    [Required(ErrorMessage = "Currency is required.")]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be a 3-letter code e.g. NGN, USD, GBP.")]
    public string Currency { get; set; } = "NGN";

    public string PaymentMethod { get; set; } = "Bank Transfer";

    public string? DedicatedAccountNumber { get; set; }

    public string? Narration { get; set; }

    public decimal? ExpectedAmount { get; set; }
}
