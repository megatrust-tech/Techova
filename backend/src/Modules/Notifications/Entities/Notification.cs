using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using taskedin_be.src.Modules.Common.Entities;
using taskedin_be.src.Modules.Users.Entities;

namespace taskedin_be.src.Modules.Notifications.Entities;

public class Notification : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    // Optional: To link deep-linking later (e.g., "LeaveRequest", "105")
    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}