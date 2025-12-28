namespace taskedin_be.src.Modules.Leaves.DTOs;

/// <summary>
/// Individual leave request for calendar display
/// </summary>
public class CalendarLeaveDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfDays { get; set; }
}

/// <summary>
/// HR view - leaves grouped by manager with department info
/// </summary>
public class CalendarGroupedByManagerDto
{
    public int ManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public List<CalendarLeaveDto> Leaves { get; set; } = new();
}

/// <summary>
/// Calendar data response - contains either flat list or grouped data based on role
/// </summary>
public class CalendarDataResponseDto
{
    /// <summary>
    /// Flat list of leaves - used for Employee and Manager views
    /// </summary>
    public List<CalendarLeaveDto>? Leaves { get; set; }
    
    /// <summary>
    /// Grouped by manager - used for HR view
    /// </summary>
    public List<CalendarGroupedByManagerDto>? GroupedByManager { get; set; }
}
