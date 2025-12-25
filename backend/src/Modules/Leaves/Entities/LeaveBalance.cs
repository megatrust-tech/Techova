using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using taskedin_be.src.Modules.Common.Entities;
using taskedin_be.src.Modules.Users.Entities;

namespace taskedin_be.src.Modules.Leaves.Entities;

public class LeaveBalance : BaseEntity
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int Year { get; set; }

    [Required]
    public LeaveType Type { get; set; }

    public int TotalDays { get; set; }
    public int UsedDays { get; set; } = 0;

    // Helper property to calculate remaining
    [NotMapped]
    public int RemainingDays => TotalDays - UsedDays;

    [ForeignKey("EmployeeId")]
    public virtual User? Employee { get; set; }
}