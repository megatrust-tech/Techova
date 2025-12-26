using Microsoft.EntityFrameworkCore;
using taskedin_be.src.Infrastructure.Persistence;
using taskedin_be.src.Modules.Notifications.Interfaces;
using taskedin_be.src.Modules.Notifications.Entities;
using taskedin_be.src.Modules.Users.Entities; // Required for UserDevice

namespace taskedin_be.src.Modules.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailService _emailService;
    private readonly IFirebaseNotificationService _firebaseService;

    public NotificationService(
        IServiceProvider serviceProvider,
        IEmailService emailService,
        IFirebaseNotificationService firebaseService)
    {
        _serviceProvider = serviceProvider;
        _emailService = emailService;
        _firebaseService = firebaseService;
    }

    public async Task NotifyUserAsync(int userId, string subject, string textBody, string? htmlBody = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Fetch User and their Devices
        var user = await context.Users
            .Include(u => u.UserDevices)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;

        // 2. Persist Notification to Database (Inbox)
        Notification? savedNotification = null;
        try
        {
            savedNotification = new Notification
            {
                UserId = userId,
                Title = subject,
                Message = textBody,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Set<Notification>().Add(savedNotification);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Notification DB Error] {ex.Message}");
        }

        // 3. Send Email
        if (!string.IsNullOrEmpty(user.Email))
        {
            try
            {
                string emailContent = !string.IsNullOrEmpty(htmlBody) ? htmlBody : textBody;
                bool isHtml = !string.IsNullOrEmpty(htmlBody);
                await _emailService.SendEmailAsync(user.Email, subject, emailContent, isHtml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email Error] {ex.Message}");
            }
        }

        // 4. Send Push Notification (Multi-Device)
        var tokens = user.UserDevices?.Select(d => d.DeviceToken).ToList()
                     ?? await context.Set<UserDevice>()
                                     .Where(ud => ud.UserId == userId)
                                     .Select(ud => ud.DeviceToken)
                                     .ToListAsync();

        if (tokens.Any())
        {
            try
            {
                var dataPayload = new Dictionary<string, string>
                {
                    { "click_action", "FLUTTER_NOTIFICATION_CLICK" },
                    { "type", "general" }
                };

                if (savedNotification != null)
                {
                    dataPayload["notification_id"] = savedNotification.Id.ToString();

                    if (!string.IsNullOrEmpty(savedNotification.RelatedEntityType))
                    {
                        dataPayload["type"] = savedNotification.RelatedEntityType;
                        dataPayload["related_id"] = savedNotification.RelatedEntityId?.ToString() ?? "";
                    }
                }

                await _firebaseService.SendMulticastNotificationAsync(tokens, subject, textBody, dataPayload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Firebase Error] {ex.Message}");
            }
        }
    }
}