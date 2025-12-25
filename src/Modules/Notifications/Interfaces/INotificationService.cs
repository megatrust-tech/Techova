namespace taskedin_be.src.Modules.Notifications.Interfaces;

public interface INotificationService
{
    // Sends a notification to a specific user via all configured channels (Email, Push).
    Task NotifyUserAsync(int userId, string subject, string textBody, string? htmlBody = null);
}