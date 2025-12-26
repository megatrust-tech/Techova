using taskedin_be.src.Modules.Users.Entities;

namespace taskedin_be.src.Modules.Users.DTOs
{
    public class CreateUserResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public required int RoleId { get; set; }
    }
}
