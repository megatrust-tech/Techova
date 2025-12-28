using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using taskedin_be.src.Infrastructure.Persistence;
using taskedin_be.src.Modules.Notifications.Services;
using taskedin_be.src.Modules.Notifications.Interfaces;
using taskedin_be.src.Modules.Users.Entities;
using taskedin_be.src.Modules.Notifications.Entities;

namespace taskedin_be.src.Modules.Notifications.Tests;

public class NotificationServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IFirebaseNotificationService> _mockFirebaseService;
    private readonly AppDbContext _context;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        // 1. Setup In-Memory Database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        // 2. Setup ServiceProvider Mock (needed because NotificationService creates a scope)
        _mockServiceProvider = new Mock<IServiceProvider>();
        var mockScope = new Mock<IServiceScope>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();

        // Ensure the scope returns the SAME Mock ServiceProvider
        mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        // Setup resolution
        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(mockScopeFactory.Object);
        // CRITICAL: Return the same in-memory context so we can verify the DB saves
        _mockServiceProvider.Setup(x => x.GetService(typeof(AppDbContext))).Returns(_context);

        // 3. Setup Mocks for Email and Firebase
        _mockEmailService = new Mock<IEmailService>();
        _mockFirebaseService = new Mock<IFirebaseNotificationService>();

        // 4. Create Service
        _notificationService = new NotificationService(
            _mockServiceProvider.Object,
            _mockEmailService.Object,
            _mockFirebaseService.Object
        );
    }

    [Fact]
    public async Task NotifyUser_UserHasEmail_CallsEmailService()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", FirstName = "John", LastName = "Doe" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _notificationService.NotifyUserAsync(1, "Test Subject", "Test Body");

        // Assert
        _mockEmailService.Verify(x => x.SendEmailAsync("test@example.com", "Test Subject", "Test Body", false), Times.Once);
    }

    [Fact]
    public async Task NotifyUser_UserHasDevices_CallsMulticastFirebaseService()
    {
        // Arrange
        var userId = 2;
        var user = new User { Id = userId, Email = "", FirstName = "Jane", LastName = "Doe" };

        // Add Multiple Devices
        var device1 = new UserDevice { UserId = userId, DeviceToken = "token_android_1", Platform = "Android" };
        var device2 = new UserDevice { UserId = userId, DeviceToken = "token_ios_1", Platform = "iOS" };

        _context.Users.Add(user);
        _context.UserDevices.AddRange(device1, device2);
        await _context.SaveChangesAsync();

        // Act
        await _notificationService.NotifyUserAsync(userId, "Push Title", "Push Body");

        // Assert
        // Verify that SendMulticastNotificationAsync was called with BOTH tokens
        _mockFirebaseService.Verify(x => x.SendMulticastNotificationAsync(
            It.Is<List<string>>(tokens =>
                tokens.Contains("token_android_1") &&
                tokens.Contains("token_ios_1") &&
                tokens.Count == 2),
            "Push Title",
            "Push Body",
            It.IsAny<Dictionary<string, string>>()), // We expect a dictionary (even if default)
            Times.Once);
    }

    [Fact]
    public async Task NotifyUser_PersistsNotificationToDatabase()
    {
        // Arrange
        var user = new User { Id = 10, Email = "db@test.com", FirstName = "DB", LastName = "Tester" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        string subject = "Persistence Check";
        string body = "Checking if this is saved to DB";

        // Act
        await _notificationService.NotifyUserAsync(10, subject, body);

        // Assert
        var savedNotification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == 10);

        Assert.NotNull(savedNotification);
        Assert.Equal(subject, savedNotification.Title);
        Assert.Equal(body, savedNotification.Message);
        Assert.False(savedNotification.IsRead);
    }

    [Fact]
    public async Task NotifyUser_NoDevices_DoesNotCallFirebase()
    {
        // Arrange
        var user = new User { Id = 5, Email = "", FirstName = "NoDevice", LastName = "User" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _notificationService.NotifyUserAsync(5, "Subj", "Body");

        // Assert
        _mockFirebaseService.Verify(x => x.SendMulticastNotificationAsync(
            It.IsAny<List<string>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>()),
            Times.Never);
    }

    // ==========================================
    // INTEGRATION TEST (Actual Sending)
    // ==========================================

    // [Fact(Skip = "Integration Test - Requires Real Credentials")]
    [Fact]
    public async Task Integration_SendRealEmail_ToVerifyCredentials()
    {
        // 1. Load Real Configuration
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        // 2. Create Real Service
        var realEmailService = new EmailService(config);

        // 3. Send
        string myTestEmail = "moh-mourad@outlook.com";

        try
        {
            await realEmailService.SendEmailAsync(myTestEmail, "Integration Test", "This is a test email from TaskedIn Backend.");
            // If no exception is thrown, connection worked.
            Assert.True(true);
        }
        catch (Exception ex)
        {
            // Fail if credential issue, but ignore if just config missing in test env
            if (ex is InvalidOperationException && ex.Message.Contains("Configuration missing"))
            {
                // Ignore missing config in test environment
                return;
            }
            throw;
        }
    }

    [Fact]
    public async Task Integration_SendRealPush_ToVerifyFirebase()
    {
        // 1. Load Real Configuration (to get the path to service-account.json)
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false) // Must exist for this test
            .Build();

        // 2. Create Real Service
        // We instantiate the concrete class directly to test the real logic
        var realFirebaseService = new FirebaseNotificationService(config);

        // 3. Define the Token from your DB
        string realDeviceToken = "cj6klVvUSEOj0BX1YJ8zTy:APA91bEri6S_THVXlMRL45LIIO_giswkw419xYp3Bip_5co0HvOxYVbVVbl1M_IbzSv3iVmfq6AbHetxrhNJc6cU4Oayiy9l61nOHUFeEI2dALShSynRWB0";

        var tokens = new List<string> { realDeviceToken };

        try
        {
            // 4. Send Notification
            // We use the Multicast method to verify if your fix for the 404 error works
            await realFirebaseService.SendMulticastNotificationAsync(
                tokens,
                "Integration Test",
                "This is a direct test from xUnit.",
                new Dictionary<string, string> { { "type", "test" } }
            );

            // If we get here without an exception, the request was sent to Google successfully.
            Assert.True(true);
        }
        catch (Exception ex)
        {
            // This will fail the test and print the real error (e.g., 404, 401, or Auth errors)
            Assert.Fail($"Firebase Integration Failed: {ex.Message}");
        }
    }

    [Fact]
    public async Task Integration_SendDirect_WithMessageId()
    {
        // Direct test using Firebase SDK to get actual message ID
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var credentialPath = config["Firebase:CredentialPath"];
        if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
        {
            Console.WriteLine("Firebase credentials not found, skipping test");
            return;
        }

        // Initialize Firebase if not already
        if (FirebaseAdmin.FirebaseApp.DefaultInstance == null)
        {
            FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions()
            {
                Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(credentialPath)
            });
        }

        string deviceToken = "cj6klVvUSEOj0BX1YJ8zTy:APA91bEri6S_THVXlMRL45LIIO_giswkw419xYp3Bip_5co0HvOxYVbVVbl1M_IbzSv3iVmfq6AbHetxrhNJc6cU4Oayiy9l61nOHUFeEI2dALShSynRWB0";

        var message = new FirebaseAdmin.Messaging.Message()
        {
            Token = deviceToken,
            Notification = new FirebaseAdmin.Messaging.Notification()
            {
                Title = "Direct Firebase Test",
                Body = $"Sent at {DateTime.Now:HH:mm:ss}"
            },
            Android = new FirebaseAdmin.Messaging.AndroidConfig()
            {
                Priority = FirebaseAdmin.Messaging.Priority.High,
                Notification = new FirebaseAdmin.Messaging.AndroidNotification()
                {
                    ChannelId = "default_channel",
                    ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                }
            }
        };

        try
        {
            // SendAsync returns the MESSAGE ID if successful
            string messageId = await FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance.SendAsync(message);
            
            Console.WriteLine($"===========================================");
            Console.WriteLine($"SUCCESS! Firebase accepted the notification");
            Console.WriteLine($"Message ID: {messageId}");
            Console.WriteLine($"===========================================");
            
            Assert.NotNull(messageId);
            Assert.StartsWith("projects/", messageId); // Firebase returns "projects/{project_id}/messages/{message_id}"
        }
        catch (FirebaseAdmin.Messaging.FirebaseMessagingException ex)
        {
            Console.WriteLine($"Firebase Error Code: {ex.MessagingErrorCode}");
            Console.WriteLine($"Firebase Error: {ex.Message}");
            Assert.Fail($"Firebase rejected: {ex.MessagingErrorCode} - {ex.Message}");
        }
    }
}