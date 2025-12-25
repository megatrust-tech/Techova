using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using taskedin_be.src.Modules.Notifications.Interfaces;

namespace taskedin_be.src.Modules.Notifications.Services;

public class NotificationBackgroundWorker : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IServiceProvider _serviceProvider;

    public NotificationBackgroundWorker(INotificationQueue queue, IServiceProvider serviceProvider)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[Background Service] Notification Worker Started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. Wait for a job to appear in the queue
                var workItem = await _queue.DequeueAsync(stoppingToken);

                // 2. Create a scope (Mandatory because NotificationService relies on scoped DbContext)
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // 3. Process the notification (DB + Email + Push)
                // This now happens in the background, so the user's HTTP request has already finished!
                await notificationService.NotifyUserAsync(workItem.UserId, workItem.Subject, workItem.Message);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown requested
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker Error] Processing notification failed: {ex.Message}");
            }
        }

        Console.WriteLine("[Background Service] Notification Worker Stopped.");
    }
}