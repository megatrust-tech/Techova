namespace taskedin_be.src.Modules.Leaves.DTOs;

public class LeaveRequestFilterDto : PaginationDto
{
    public string? Status { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}