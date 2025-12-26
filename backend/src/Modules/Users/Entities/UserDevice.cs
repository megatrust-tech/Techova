using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using taskedin_be.src.Modules.Common.Entities;

namespace taskedin_be.src.Modules.Users.Entities;

public class UserDevice : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string DeviceToken { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Platform { get; set; } = "Android";

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}