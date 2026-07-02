using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Customers.Dto;

public class UpdateKycDocumentStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Pending|Verified|Rejected)$",
        ErrorMessage = "Status must be Pending, Verified, or Rejected.")]
    public string Status { get; set; } = string.Empty;
}
