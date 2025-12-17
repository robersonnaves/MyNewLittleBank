namespace UseCases.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public Uri? BaseUrl { get; set; }

    public int TimeoutSeconds { get; set; } = 5;

    public bool EnableInsufficientFundsNotifications { get; set; } = true;
}
