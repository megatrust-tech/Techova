using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using taskedin_be.src.Modules.Notifications.Interfaces;

namespace taskedin_be.src.Modules.Notifications.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false)
    {
        // Read from environment variables first, then fall back to appsettings.json
        var settings = _configuration.GetSection("EmailSettings");
        var smtpServer = Environment.GetEnvironmentVariable("EMAIL_SMTP_SERVER")
            ?? settings["SmtpServer"];
        var port = int.Parse(Environment.GetEnvironmentVariable("EMAIL_PORT")
            ?? settings["Port"] ?? "587");
        var senderEmail = Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL")
            ?? settings["SenderEmail"];
        var senderName = Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME")
            ?? settings["SenderName"];
        var username = Environment.GetEnvironmentVariable("EMAIL_USERNAME")
            ?? settings["Username"] ?? senderEmail;
        var password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD")
            ?? settings["Password"];

        if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail))
        {
            throw new InvalidOperationException($"[Email Service] Configuration missing. Server: {smtpServer}, Email: {senderEmail}. Please check appsettings.json.");
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = body;
            }
            else
            {
                bodyBuilder.TextBody = body;
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Connect using StartTls (Required for Outlook/Office365)
            await client.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);

            if (!string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Console.WriteLine($"[Email Sent] To: {toEmail}, Subject: {subject}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Email Failed] Error: {ex.Message}");
            throw;
        }
    }
}