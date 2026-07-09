namespace Kredar.API.SubMerchants;

public enum SubMerchantStatus { Active, Suspended }

public class SubMerchant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public SubMerchantStatus Status { get; set; } = SubMerchantStatus.Active;
    public string? SettlementBankName { get; set; }
    public string? SettlementBankCode { get; set; }
    public string? SettlementAccountNumber { get; set; }
    public string? SettlementAccountName { get; set; }
    public int PlatformFeeBps { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool HasPayoutAccount => SettlementAccountNumber != null;
}
