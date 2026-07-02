using Kredar.API.Customers.Dto;

namespace Kredar.API.Customers;

public class KycService(KycRepository kycRepo, CustomerRepository customerRepo)
{
    public async Task<List<KycDocumentResponse>> GetDocumentsAsync(Guid tenantId, Guid customerId)
    {
        var customer = await customerRepo.FindByIdAsync(tenantId, customerId)
            ?? throw new KeyNotFoundException("Customer not found.");

        var docs = await kycRepo.GetByCustomerAsync(tenantId, customerId);
        return docs.Select(MapToResponse).ToList();
    }

    public async Task<KycDocumentResponse> SubmitDocumentAsync(Guid tenantId, Guid customerId, SubmitKycDocumentRequest request)
    {
        var customer = await customerRepo.FindByIdAsync(tenantId, customerId)
            ?? throw new KeyNotFoundException("Customer not found.");

        var docType = Enum.Parse<KycDocumentType>(request.DocumentType);

        var doc = new CustomerKycDocument
        {
            CustomerId = customerId,
            TenantId = tenantId,
            DocumentType = docType,
            FileUrl = request.FileUrl,
            Status = KycDocumentStatus.Pending
        };

        await kycRepo.AddAsync(doc);

        // Move customer KYC status to InReview once at least one doc is submitted
        if (customer.KycStatus == KycStatus.Pending)
        {
            customer.KycStatus = KycStatus.InReview;
            await customerRepo.UpdateAsync(customer);
        }

        return MapToResponse(doc);
    }

    public async Task<KycDocumentResponse> UpdateDocumentStatusAsync(Guid tenantId, Guid docId, KycDocumentStatus newStatus)
    {
        var doc = await kycRepo.FindByIdAsync(tenantId, docId)
            ?? throw new KeyNotFoundException("KYC document not found.");

        doc.Status = newStatus;
        await kycRepo.UpdateAsync(doc);

        // Auto-verify customer KYC when all documents are verified
        if (newStatus == KycDocumentStatus.Verified)
        {
            var allVerified = await kycRepo.AllVerifiedAsync(tenantId, doc.CustomerId);
            if (allVerified)
            {
                var customer = await customerRepo.FindByIdAsync(tenantId, doc.CustomerId);
                if (customer is not null)
                {
                    customer.KycStatus = KycStatus.Verified;
                    await customerRepo.UpdateAsync(customer);
                }
            }
        }

        return MapToResponse(doc);
    }

    private static KycDocumentResponse MapToResponse(CustomerKycDocument doc) => new()
    {
        Id = doc.Id,
        DocumentType = doc.DocumentType.ToString(),
        FileUrl = doc.FileUrl,
        Status = doc.Status.ToString(),
        SubmittedAt = doc.SubmittedAt
    };
}
