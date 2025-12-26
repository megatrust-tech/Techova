using System.ComponentModel.DataAnnotations.Schema;
using taskedin_be.src.Modules.Common.Entities;

namespace taskedin_be.src.Modules.Users.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Foreign key
        public int RoleId { get; set; }

        // Navigation property
        public Role Role { get; set; } = null!;

        public int? DepartmentId { get; set; }

        // Manager relationship (self-referencing)
        public int? ManagerId { get; set; }

        // Navigation properties for manager relationship
        [ForeignKey("ManagerId")]
        public virtual User? Manager { get; set; }

        public virtual ICollection<User> DirectReports { get; set; } = new List<User>();

        public virtual ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>();

        // Refresh token fields for JWT authentication
        // RefreshTokenHash: Stores only the hashed refresh token (never store plain token)
        public string? RefreshTokenHash { get; set; }

        // RefreshTokenExpiresAt: Expiry time for the refresh token (UTC)
        public DateTime? RefreshTokenExpiresAt { get; set; }

        // TokenVersion: Incremented on logout to invalidate all existing tokens (global logout)
        public int TokenVersion { get; set; } = 0;
    }
}