using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Tenants.Dto;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Business name is required.")]
    [MinLength(2, ErrorMessage = "Business name must be at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Business name must not exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-\&\.\']+$",
        ErrorMessage = "Business name can only contain letters, numbers, spaces, hyphens, ampersands, periods and apostrophes.")]
    public string BusinessName { get; set; } = string.Empty;

    public string? BusinessRegistrationNumber { get; set; }

    [Required(ErrorMessage = "Business type is required.")]
    public string BusinessType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Industry is required.")]
    public string Industry { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required.")]
    public string Country { get; set; } = string.Empty;

    [Required(ErrorMessage = "Business address is required.")]
    public string BusinessAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^\+[1-9]\d{10,14}$",
        ErrorMessage = "Phone number must be in international format (e.g. +2348012345678, +12025550123, +447911123456).")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Url(ErrorMessage = "Please enter a valid website URL.")]
    public string? Website { get; set; }
}
