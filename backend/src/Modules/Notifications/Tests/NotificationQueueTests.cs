using Xunit;
using taskedin_be.src.Modules.Notifications.Services;

namespace taskedin_be.src.Modules.Notifications.Tests;

public class NotificationQueueTests
{
    [Fact]
    public async Task Queue_EnqueueAndDequeue_WorksCorrectly()
    {
        // Arrange
        var queue = new InMemoryNotificationQueue();
        int userId = 99;
        string subject = "Async Test";
        string message = "Running in background";

        // Act
        // 1. Producer writes to queue
        await queue.QueueNotificationAsync(userId, subject, message);

        // 2. Consumer reads from queue
        var workItem = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(workItem);
        Assert.Equal(userId, workItem.UserId);
        Assert.Equal(subject, workItem.Subject);
        Assert.Equal(message, workItem.Message);
    }
}