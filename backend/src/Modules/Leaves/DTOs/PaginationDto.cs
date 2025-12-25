namespace taskedin_be.src.Modules.Leaves.DTOs;

public class PaginationDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}