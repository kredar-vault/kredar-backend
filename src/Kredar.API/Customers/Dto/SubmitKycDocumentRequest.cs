using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Customers.Dto;

public class SubmitKycDocumentRequest
{
    [Required(ErrorMessage = "Document type is required.")]
    [RegularExpression("^(GovernmentId|ProfileImage|ProofOfAddress)$",
        ErrorMessage = "DocumentType must be GovernmentId, ProfileImage, or ProofOfAddress.")]
    public string DocumentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "File URL is required.")]
    [Url(ErrorMessage = "FileUrl must be a valid URL.")]
    public string FileUrl { get; set; } = string.Empty;
}
