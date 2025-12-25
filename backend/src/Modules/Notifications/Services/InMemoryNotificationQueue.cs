using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using taskedin_be.src.Modules.Notifications.Interfaces;

namespace taskedin_be.src.Modules.Notifications.Services;

public class InMemoryNotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationWorkItem> _queue;

    public InMemoryNotificationQueue()
    {
        // Unbounded channel: Fast and simple. 
        // For extremely high-load systems, consider Channel.CreateBounded<T> to apply backpressure.
        _queue = Channel.CreateUnbounded<NotificationWorkItem>();
    }

    public async ValueTask QueueNotificationAsync(int userId, string subject, string message)
    {
        await _queue.Writer.WriteAsync(new NotificationWorkItem(userId, subject, message));
    }

    public async ValueTask<NotificationWorkItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}