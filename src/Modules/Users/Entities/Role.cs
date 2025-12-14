using taskedin_be.src.Modules.Common.Entities;

namespace taskedin_be.src.Modules.Users.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public ICollection<User> Users { get; set; } = [];
}

