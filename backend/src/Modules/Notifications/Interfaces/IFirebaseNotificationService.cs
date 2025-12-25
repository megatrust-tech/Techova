namespace taskedin_be.src.Modules.Notifications.Interfaces;

public interface IFirebaseNotificationService
{
    Task SendMulticastNotificationAsync(List<string> tokens, string title, string body, Dictionary<string, string>? data = null);
}