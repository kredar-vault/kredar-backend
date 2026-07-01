using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Customers.Dto;

public class CreateCustomerRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^\+[1-9]\d{10,14}$",
        ErrorMessage = "Phone number must be in international format (e.g. +2348012345678).")]
    public string? PhoneNumber { get; set; }
}
