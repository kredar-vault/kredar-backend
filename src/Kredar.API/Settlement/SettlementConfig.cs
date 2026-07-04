namespace Kredar.API.Settlement;

public class SettlementConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string? SettlementAccountNumber { get; set; }
    public string? SettlementBankCode { get; set; }
    public string? SettlementAccountName { get; set; }
    public bool AutoSettle { get; set; } = false;
    public decimal MinPayoutNaira { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum SplitBasis { Percentage, Flat }

public class SettlementSplit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string BeneficiaryName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public SplitBasis Basis { get; set; } = SplitBasis.Percentage;
    public int ShareBps { get; set; } = 0;
    public decimal FlatNaira { get; set; } = 0;
    public int Priority { get; set; } = 0;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum EscrowState { Held, Released }

public class EscrowHold
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid DedicatedAccountId { get; set; }
    public decimal AmountNaira { get; set; }
    public EscrowState State { get; set; } = EscrowState.Held;
    public string? ReleaseCondition { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAt { get; set; }
}
