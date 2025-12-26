using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using taskedin_be.src.Modules.Common.Entities;
using taskedin_be.src.Modules.Users.Entities;

namespace taskedin_be.src.Modules.Leaves.Entities;

public enum LeaveAction
{
    Submitted,
    ManagerApproved,
    ManagerRejected,
    HRApproved,
    HRRejected,
    Cancelled
}

public class LeaveAuditLog : BaseEntity
{
    [Required]
    public int LeaveRequestId { get; set; }

    [Required]
    public int ActionByUserId { get; set; }

    [Required]
    public LeaveAction Action { get; set; }

    [Required]
    public LeaveStatus NewStatus { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("LeaveRequestId")]
    public virtual LeaveRequest? LeaveRequest { get; set; }

    [ForeignKey("ActionByUserId")]
    public virtual User? ActionByUser { get; set; }
}