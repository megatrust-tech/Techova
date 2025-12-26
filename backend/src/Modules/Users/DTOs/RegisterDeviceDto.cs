using System.ComponentModel.DataAnnotations;

namespace taskedin_be.src.Modules.Users.DTOs;

public class RegisterDeviceDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string Platform { get; set; } = "Android";
}