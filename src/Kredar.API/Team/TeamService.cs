using Kredar.API.Team.Dto;

namespace Kredar.API.Team;

public class TeamService(TeamRepository teamRepo)
{
    public async Task<List<TeamMemberResponse>> GetAllAsync(Guid tenantId) =>
        (await teamRepo.GetAllAsync(tenantId)).Select(MapToResponse).ToList();

    public async Task<TeamMemberResponse> InviteAsync(Guid tenantId, InviteTeamMemberRequest request)
    {
        var exists = await teamRepo.FindByEmailAsync(tenantId, request.Email);
        if (exists is not null)
            throw new InvalidOperationException("A team member with this email already exists.");

        var member = new TeamMember
        {
            TenantId = tenantId,
            FullName = request.FullName,
            Email = request.Email,
            Role = Enum.Parse<TeamRole>(request.Role)
        };

        await teamRepo.AddAsync(member);
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
        DateAdded = m.DateAdded
    };
}
