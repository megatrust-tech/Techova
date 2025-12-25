using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using taskedin_be.src.Modules.Leaves.Entities;

namespace taskedin_be.src.Modules.Leaves.DTOs;

public class SubmitLeaveRequestDto
{
    [Required]
    public LeaveType Type { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public IFormFile? Attachment { get; set; } 
}

public class LeaveRequestResponseDto
{
    public int Id { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? AttachmentUrl { get; set; }
    public int ManagerId { get; set; }
    public DateTime CreatedAt { get; set; }

    public string EmployeeEmail { get; set; } = string.Empty;
}

public class EnumDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}