namespace taskedin_be.src.Modules.Leaves.DTOs;

public class LeaveAuditLogDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActionBy { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime ActionDate { get; set; }
}