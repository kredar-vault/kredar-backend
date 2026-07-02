using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Customers.Dto;

public class UpdateCustomerStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Active|Inactive|Restricted)$",
        ErrorMessage = "Status must be Active, Inactive, or Restricted.")]
    public string Status { get; set; } = string.Empty;
}
