namespace taskedin_be.src.Modules.Leaves.DTOs;

public class DepartmentCoverageDto
{
    public int DepartmentId { get; set; }
    public int TotalEmployees { get; set; }
    public int OnLeaveCount { get; set; }
    public int AvailableCount { get; set; }
    public double CapacityPercentage { get; set; }
}