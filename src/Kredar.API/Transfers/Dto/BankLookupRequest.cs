using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Transfers.Dto;

public class BankLookupRequest
{
    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    public string BankCode { get; set; } = string.Empty;
}
