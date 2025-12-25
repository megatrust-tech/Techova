using System.ComponentModel.DataAnnotations;

namespace taskedin_be.src.Modules.Auth.DTOs.Request
{
    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

}
