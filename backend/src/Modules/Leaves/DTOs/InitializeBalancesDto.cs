namespace taskedin_be.src.Modules.Leaves.DTOs;

public class UserWithoutBalanceDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class InitializeBalancesRequestDto
{
    public List<int> UserIds { get; set; } = new();
}

public class InitializeBalancesResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}
