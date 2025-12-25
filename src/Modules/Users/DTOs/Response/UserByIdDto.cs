using taskedin_be.src.Modules.Users.Entities;

namespace taskedin_be.src.Modules.Users.DTOs
{
    public class UserByIdDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public required Role Role { get; set; }
    }
}
