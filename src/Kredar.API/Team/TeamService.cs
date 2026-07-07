using Kredar.API.Auth;
using Kredar.API.Team.Dto;
using Kredar.API.Tenants;

namespace Kredar.API.Team;

public class TeamService(TeamRepository teamRepo, EmailService emailService, TenantRepository tenantRepo)
{
    public async Task<List<TeamMemberResponse>> GetAllAsync(Guid tenantId) =>
        (await teamRepo.GetAllAsync(tenantId)).Select(MapToResponse).ToList();

    public async Task<TeamMemberResponse> InviteAsync(Guid tenantId, InviteTeamMemberRequest request)
    {
        var exists = await teamRepo.FindByEmailAsync(tenantId, request.Email);
        if (exists is not null)
            throw new InvalidOperationException("A team member with this email already exists.");

        var token = Guid.NewGuid().ToString("N");
        var member = new TeamMember
        {
            TenantId = tenantId,
            FullName = request.FullName,
            Email = request.Email,
            Role = Enum.Parse<TeamRole>(request.Role),
            Status = TeamMemberStatus.Pending,
            InviteToken = token,
            InviteTokenExpiry = DateTime.UtcNow.AddHours(72)
        };

        await teamRepo.AddAsync(member);

        var tenant = await tenantRepo.FindByIdAsync(tenantId);
        var tenantName = tenant?.BusinessName ?? "Kredar";
        _ = emailService.SendTeamInviteEmailAsync(member.Email, token, tenantName);

        return MapToResponse(member);
    }

    public async Task<TeamMemberResponse> AcceptInviteAsync(string token)
    {
        var member = await teamRepo.FindByInviteTokenAsync(token)
            ?? throw new KeyNotFoundException("Invitation not found or already accepted.");

        if (member.InviteTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invitation has expired. Ask the team admin to resend it.");

        member.Status = TeamMemberStatus.Active;
        member.InviteToken = null;
        member.InviteTokenExpiry = null;

        await teamRepo.UpdateAsync(member);
        return MapToResponse(member);
    }

    public async Task<TeamMemberResponse> ResendInviteAsync(Guid tenantId, Guid id)
    {
        var member = await teamRepo.FindByIdAsync(tenantId, id)
            ?? throw new KeyNotFoundException("Team member not found.");

        if (member.Status == TeamMemberStatus.Active)
            throw new InvalidOperationException("This member has already accepted their invitation.");

        var token = Guid.NewGuid().ToString("N");
        member.InviteToken = token;
        member.InviteTokenExpiry = DateTime.UtcNow.AddHours(72);

        await teamRepo.UpdateAsync(member);

        var tenant = await tenantRepo.FindByIdAsync(tenantId);
        var tenantName = tenant?.BusinessName ?? "Kredar";
        _ = emailService.SendTeamInviteEmailAsync(member.Email, token, tenantName);

        return MapToResponse(member);
    }

    public async Task<TeamMemberResponse> UpdateAsync(Guid tenantId, Guid id, UpdateTeamMemberRequest request)
    {
        var member = await teamRepo.FindByIdAsync(tenantId, id)
            ?? throw new KeyNotFoundException("Team member not found.");

        if (request.FullName is not null)
            member.FullName = request.FullName;

        member.Role = Enum.Parse<TeamRole>(request.Role);

        await teamRepo.UpdateAsync(member);
        return MapToResponse(member);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id)
    {
        var member = await teamRepo.FindByIdAsync(tenantId, id)
            ?? throw new KeyNotFoundException("Team member not found.");

        await teamRepo.DeleteAsync(member);
    }

    private static TeamMemberResponse MapToResponse(TeamMember m) => new()
    {
        Id = m.Id,
        FullName = m.FullName,
        Email = m.Email,
        Role = m.Role.ToString(),
        Status = m.Status.ToString(),
        DateAdded = m.DateAdded
    };
}
