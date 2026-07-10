using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Balance.Dto;

public class WithdrawRequest
{
    [Required, Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    public string BankCode { get; set; } = string.Empty;

    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    public string Narration { get; set; } = "Business Withdrawal";
}
