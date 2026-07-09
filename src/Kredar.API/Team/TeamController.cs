using Kredar.API.Common;
using Kredar.API.Team.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Team;

[ApiController]
[Route("api/v1/team")]
[Authorize]
[Tags("Team")]
public class TeamController(TeamService teamService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var members = await teamService.GetAllAsync(tenantId);
        return Ok(ApiResponse<List<TeamMemberResponse>>.Success(members));
    }

    [HttpPost]
    public async Task<IActionResult> Invite([FromBody] InviteTeamMemberRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var member = await teamService.InviteAsync(tenantId, request);
        return Ok(ApiResponse<TeamMemberResponse>.Success(member, "Team member added successfully."));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamMemberRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var member = await teamService.UpdateAsync(tenantId, id, request);
        return Ok(ApiResponse<TeamMemberResponse>.Success(member, "Team member updated successfully."));
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
    {
        var member = await teamService.AcceptInviteAsync(request.Token);
        return Ok(ApiResponse<TeamMemberResponse>.Success(member, "Invitation accepted."));
    }

    [HttpPost("{id:guid}/resend")]
    public async Task<IActionResult> ResendInvite(Guid id)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var member = await teamService.ResendInviteAsync(tenantId, id);
        return Ok(ApiResponse<TeamMemberResponse>.Success(member, "Invitation resent."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        await teamService.DeleteAsync(tenantId, id);
        return Ok(ApiResponse<string>.Success("Team member removed."));
    }
}
