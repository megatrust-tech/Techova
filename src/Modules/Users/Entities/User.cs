using taskedin_be.src.Modules.Common.Entities;

namespace taskedin_be.src.Modules.Users.Entities;

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
}

