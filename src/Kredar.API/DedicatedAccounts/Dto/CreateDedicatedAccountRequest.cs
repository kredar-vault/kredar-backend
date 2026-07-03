using System.ComponentModel.DataAnnotations;

namespace Kredar.API.DedicatedAccounts.Dto;

public class CreateDedicatedAccountRequest
{
    [Required]
    public Guid CustomerId { get; set; }

    public decimal? ExpectedAmount { get; set; }
}
