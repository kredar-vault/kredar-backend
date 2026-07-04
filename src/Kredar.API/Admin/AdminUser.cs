namespace Kredar.API.Admin;

public enum AdminRole { Admin, SuperAdmin }

public class AdminUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AdminRole Role { get; set; } = AdminRole.Admin;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
