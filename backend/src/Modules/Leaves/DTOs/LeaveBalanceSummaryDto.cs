namespace taskedin_be.src.Modules.Leaves.DTOs;

public class LeaveBalanceSummaryDto
{
    public string Type { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays { get; set; }
}