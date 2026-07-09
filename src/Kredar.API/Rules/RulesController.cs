using Kredar.API.Common;
using Kredar.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Rules;

public class CreateRuleRequest
{
    public string Trigger { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public decimal? ThresholdNaira { get; set; }
    public int Priority { get; set; } = 0;
}

[ApiController]
[Route("api/v1/rules")]
[Authorize]
public class RulesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var rules = await db.MoneyRules
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<MoneyRule>>.Success(rules));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRuleRequest request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);

        if (!Enum.TryParse<RuleTrigger>(request.Trigger, true, out var trigger))
            throw new Exception($"Invalid trigger. Valid values: {string.Join(", ", Enum.GetNames<RuleTrigger>())}");

        if (!Enum.TryParse<RuleAction>(request.Action, true, out var action))
            throw new Exception($"Invalid action. Valid values: {string.Join(", ", Enum.GetNames<RuleAction>())}");

        var rule = new MoneyRule
        {
            TenantId = tenantId,
            Trigger = trigger,
            Action = action,
            ThresholdNaira = request.ThresholdNaira,
            Priority = request.Priority
        };

        db.MoneyRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<MoneyRule>.Success(rule, "Rule created."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var rule = await db.MoneyRules.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct)
            ?? throw new Exception("Rule not found.");
        db.MoneyRules.Remove(rule);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { }, "Rule deleted."));
    }
}
