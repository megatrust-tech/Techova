using System.Threading;
using System.Threading.Tasks;

namespace taskedin_be.src.Modules.Notifications.Interfaces;

public interface INotificationQueue
{
    ValueTask QueueNotificationAsync(int userId, string subject, string message);
    ValueTask<NotificationWorkItem> DequeueAsync(CancellationToken cancellationToken);
}

public record NotificationWorkItem(int UserId, string Subject, string Message);