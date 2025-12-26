using System.Text;

namespace taskedin_be.src.Modules.Notifications.Helpers;

public static class NotificationTemplates
{
    public static (string Subject, string Text, string Html) NewRequest(string requesterName, string type, DateTime start, DateTime end, double days)
    {
        string subject = $"Action Required: New {type} Leave Request";
        string text = $"{requesterName} has requested {days} days of {type} leave ({start:MMM dd} - {end:MMM dd}). Please review.";

        string html = $@"
            <div style='font-family: Arial, sans-serif; color: #333;'>
                <h2 style='color: #2c3e50;'>New Leave Request</h2>
                <p><strong>{requesterName}</strong> has submitted a new leave request.</p>
                <table style='border-collapse: collapse; width: 100%; max-width: 600px;'>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Type:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{type}</td></tr>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Dates:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{start:MMM dd, yyyy} to {end:MMM dd, yyyy}</td></tr>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Duration:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{days} Days</td></tr>
                </table>
                <p style='margin-top: 20px;'>Please log in to the portal to Approve or Reject this request.</p>
            </div>";

        return (subject, text, html);
    }

    public static (string Subject, string Text, string Html) StatusUpdate(string status, string type, DateTime start, DateTime end)
    {
        string color = status.ToLower().Contains("approved") ? "#27ae60" : "#c0392b";
        string subject = $"Leave Request {status}";
        string text = $"Your {type} leave request ({start:MMM dd} - {end:MMM dd}) has been {status}.";

        string html = $@"
            <div style='font-family: Arial, sans-serif; color: #333;'>
                <h2 style='color: {color};'>Request {status}</h2>
                <p>Your request for <strong>{type}</strong> leave has been updated.</p>
                <p><strong>Status:</strong> <span style='color: {color}; font-weight: bold;'>{status}</span></p>
                <p><strong>Dates:</strong> {start:MMM dd, yyyy} - {end:MMM dd, yyyy}</p>
            </div>";

        return (subject, text, html);
    }

    public static (string Subject, string Text, string Html) ManagerActionToHR(string managerName, string employeeName, string type, double days)
    {
        string subject = "HR Review: Manager Approved Leave";
        string text = $"Manager {managerName} approved {type} leave for {employeeName} ({days} days). Waiting for HR final approval.";

        string html = $@"
            <div style='font-family: Arial, sans-serif; color: #333;'>
                <h2 style='color: #2980b9;'>Manager Approval Confirmed</h2>
                <p><strong>{managerName}</strong> has approved a leave request for <strong>{employeeName}</strong>.</p>
                <p>This request is now pending your final review.</p>
                <ul>
                    <li><strong>Type:</strong> {type}</li>
                    <li><strong>Duration:</strong> {days} Days</li>
                </ul>
                <p>Please proceed to the HR dashboard to finalize this request.</p>
            </div>";

        return (subject, text, html);
    }

    public static (string Subject, string Text, string Html) Cancelled(DateTime start, DateTime end)
    {
        string subject = "Leave Request Cancelled";
        string text = "Your leave request has been successfully cancelled.";
        string html = $@"
            <div style='font-family: Arial, sans-serif; color: #333;'>
                <h2>Request Cancelled</h2>
                <p>Your leave request for <strong>{start:MMM dd} - {end:MMM dd}</strong> has been successfully cancelled.</p>
            </div>";

        return (subject, text, html);
    }
}