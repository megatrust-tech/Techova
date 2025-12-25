namespace taskedin_be.src.Modules.Leaves.DTOs;

public class ConflictCheckResponse
{
    public bool HasConflict { get; set; }
    public string? ConflictingEmployeeName { get; set; }
    public string Message { get; set; } = string.Empty;
}