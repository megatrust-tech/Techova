namespace taskedin_be.src.Modules.Users.DTOs;

public class UserSearchResultDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
}
