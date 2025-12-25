using System.ComponentModel.DataAnnotations;

namespace taskedin_be.src.Modules.Leaves.DTOs;

public class LeaveActionDto
{
    [Required]
    public bool IsApproved { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}