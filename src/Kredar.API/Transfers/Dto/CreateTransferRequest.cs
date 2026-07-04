using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Transfers.Dto;

public class CreateTransferRequest
{
    [Required]
    public string MerchantTxRef { get; set; } = string.Empty;

    [Required, Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    public string BankCode { get; set; } = string.Empty;

    public string? Narration { get; set; }
}
