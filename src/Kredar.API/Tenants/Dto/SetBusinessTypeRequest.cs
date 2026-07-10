using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Tenants.Dto;

public class SetBusinessTypeRequest
{
    [Required(ErrorMessage = "Business type is required.")]
    public string BusinessType { get; set; } = string.Empty;
}
