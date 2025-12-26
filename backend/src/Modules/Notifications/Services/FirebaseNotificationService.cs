using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using taskedin_be.src.Modules.Notifications.Interfaces;

namespace taskedin_be.src.Modules.Notifications.Services;

public class FirebaseNotificationService : IFirebaseNotificationService
{
    private readonly bool _isInitialized;

    public FirebaseNotificationService(IConfiguration configuration)
    {
        var credentialPath = configuration["Firebase:CredentialPath"];

        if (!string.IsNullOrEmpty(credentialPath) && File.Exists(credentialPath))
        {
            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(credentialPath)
                    });
                }
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Firebase Init Failed] {ex.Message}");
                _isInitialized = false;
            }
        }
        else
        {
            Console.WriteLine("[Firebase] No credential file found. Push notifications disabled.");
            _isInitialized = false;
        }
    }

    public async Task SendNotificationAsync(string token, string title, string body)
    {
        if (!_isInitialized || string.IsNullOrEmpty(token)) return;

        var message = new Message()
        {
            Token = token,
            Notification = new Notification()
            {
                Title = title,
                Body = body
            },
            Android = new AndroidConfig()
            {
                Priority = Priority.High,
                Notification = new AndroidNotification()
                {
                    ChannelId = "default_channel",
                    Icon = "@mipmap/ic_launcher"
                }
            },
            Apns = new ApnsConfig()
            {
                Aps = new Aps()
                {
                    Alert = new ApsAlert()
                    {
                        Title = title,
                        Body = body,
                    },
                    Sound = "default"
                }
            }
        };

        try
        {
            await FirebaseMessaging.DefaultInstance.SendAsync(message);
            Console.WriteLine($"[Firebase Sent] To: {token}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Firebase Error] {ex.Message}");
        }
    }

    // In FirebaseNotificationService.cs

    public async Task SendMulticastNotificationAsync(List<string> tokens, string title, string body, Dictionary<string, string>? data = null)
    {
        if (!_isInitialized || tokens == null || !tokens.Any()) return;

        // 1. Create a specific Message object for EACH token
        // The V1 API does not support "broadcasting" to a list of tokens in a single payload.
        // The SDK's SendEachForMulticastAsync handles the batching logic internally for you.
        var messages = tokens.Select(token => new Message()
        {
            Token = token,
            Notification = new Notification()
            {
                Title = title,
                Body = body
            },
            Data = data,
            Android = new AndroidConfig()
            {
                Priority = Priority.High,
                Notification = new AndroidNotification()
                {
                    ChannelId = "default_channel",
                    Icon = "@mipmap/ic_launcher",
                    ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                }
            },
            Apns = new ApnsConfig()
            {
                Aps = new Aps()
                {
                    Alert = new ApsAlert()
                    {
                        Title = title,
                        Body = body,
                    },
                    Sound = "default",
                    ContentAvailable = true
                }
            }
        }).ToList();

        try
        {
            // 2. Use the new V1-compatible method
            // Note: Check if you are using FirebaseAdmin SDK v2.3.0+ or v3.0.0+
            var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages);

            if (response.FailureCount > 0)
            {
                Console.WriteLine($"[Firebase V1 Partial Fail] Success: {response.SuccessCount}, Failures: {response.FailureCount}");

                for (var i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        // The order of responses matches the order of the 'messages' list
                        var failedToken = tokens[i];
                        Console.WriteLine($"[Token Failed] {failedToken} - {response.Responses[i].Exception.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"[Firebase V1 Sent] To {tokens.Count} devices.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Firebase V1 Critical Error] {ex.Message}");
        }
    }
}