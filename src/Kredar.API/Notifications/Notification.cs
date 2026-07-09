namespace Kredar.API.Notifications;

public enum NotificationType
{
    // Auth
    Login,
    PasswordChanged,

    // API Keys
    ApiKeyCreated,
    ApiKeyRotated,
    ApiKeyRevoked,

    // Team
    TeamMemberInvited,
    TeamMemberAccepted,
    TeamMemberRemoved,

    // Transactions / Payments
    PaymentReceived,
    PaymentFailed,
    TransferInitiated,
    TransferCompleted,
    TransferFailed,

    // Onboarding
    OnboardingSubmitted,
    OnboardingApproved,
    OnboardingRejected,
    OnboardingMoreInfoRequired,

    // Billing
    BillingScheduleCreated,
    BillingSchedulePaused,
    BillingScheduleCancelled,
    BillingPeriodOverdue,

    // Webhooks
    WebhookEndpointAdded,
    WebhookEndpointRemoved,

    // Customers & Accounts
    CustomerCreated,
    DedicatedAccountCreated,

    // Security
    NewDeviceLogin,
}

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
