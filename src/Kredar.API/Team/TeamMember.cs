namespace Kredar.API.Team;

public enum TeamRole { Admin, Employee, Developer }
public enum TeamMemberStatus { Pending, Active }

public class TeamMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TeamRole Role { get; set; } = TeamRole.Employee;
    public TeamMemberStatus Status { get; set; } = TeamMemberStatus.Pending;
    public string? InviteToken { get; set; }
    public DateTime? InviteTokenExpiry { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}
