namespace Kredar.API.Rules;

public enum RuleTrigger { AnyDeposit, Overpaid, Underpaid, Reconciled, Reversed }
public enum RuleAction { Notify, Hold, Flag }

public class MoneyRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public RuleTrigger Trigger { get; set; }
    public RuleAction Action { get; set; }
    public decimal? ThresholdNaira { get; set; }
    public int Priority { get; set; } = 0;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
