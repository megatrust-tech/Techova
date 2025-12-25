using System.ComponentModel.DataAnnotations;

namespace taskedin_be.src.Modules.Users.DTOs
{
    public class CreateUserDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }
}
