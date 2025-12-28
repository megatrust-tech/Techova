namespace taskedin_be.src.Modules.Leaves.DTOs;

public class LeaveSettingsDto
{
    public int LeaveTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultBalance { get; set; }
    public bool AutoApproveEnabled { get; set; }
    public int AutoApproveThresholdDays { get; set; }
    public bool BypassConflictCheck { get; set; }
}