using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Team;

public class TeamRepository(AppDbContext db)
{
    public async Task<List<TeamMember>> GetAllAsync(Guid tenantId) =>
        await db.TeamMembers
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.DateAdded)
            .ToListAsync();

    public async Task<TeamMember?> FindByIdAsync(Guid tenantId, Guid id) =>
        await db.TeamMembers.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == id);

    public async Task<TeamMember?> FindByEmailAsync(Guid tenantId, string email) =>
        await db.TeamMembers.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Email == email);

    public async Task AddAsync(TeamMember member)
    {
        db.TeamMembers.Add(member);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(TeamMember member)
    {
        db.TeamMembers.Update(member);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(TeamMember member)
    {
        db.TeamMembers.Remove(member);
        await db.SaveChangesAsync();
    }
}
