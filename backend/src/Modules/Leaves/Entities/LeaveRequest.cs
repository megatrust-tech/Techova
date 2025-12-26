using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using taskedin_be.src.Modules.Common.Entities;
using taskedin_be.src.Modules.Users.Entities;

namespace taskedin_be.src.Modules.Leaves.Entities;

public enum LeaveType
{
    Annual,
    Sick,
    Emergency,
    Unpaid,
    Maternity,
    Paternity
}

public enum LeaveStatus {
    PendingManager,
    PendingHR,
    Approved,
    Rejected,
    Cancelled
}

public class LeaveRequest : BaseEntity
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int ManagerId { get; set; }

    [Required]
    public LeaveType Type { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int NumberOfDays { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? AttachmentPath { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.PendingManager;

    [ForeignKey("EmployeeId")]
    public virtual User? Employee { get; set; }

    [ForeignKey("ManagerId")]
    public virtual User? Manager { get; set; }
}