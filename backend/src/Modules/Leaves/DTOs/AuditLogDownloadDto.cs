namespace taskedin_be.src.Modules.Leaves.DTOs;

public class AuditLogDownloadDto
{
    // Grouping fields (for HR)
    public string DepartmentName { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    
    // Request details
    public int RequestId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfDays { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    
    // Audit details
    public string ActionTaken { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
    public string? Comment { get; set; }
}
