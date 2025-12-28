namespace taskedin_be.src.Modules.Leaves.DTOs;

/// <summary>
/// Represents a user with their current leave balances for the current year
/// </summary>
public class UserBalanceDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<LeaveBalanceItemDto> Balances { get; set; } = new();
}

public class LeaveBalanceItemDto
{
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays { get; set; }
}

/// <summary>
/// Request to update balances for selected users
/// </summary>
public class UpdateBalancesRequestDto
{
    public List<int> UserIds { get; set; } = new();
    
    /// <summary>
    /// Target year for balance updates. Defaults to current year if not specified.
    /// </summary>
    public int? Year { get; set; }
    
    /// <summary>
    /// Updates to apply. Key is LeaveTypeId, Value is new TotalDays
    /// </summary>
    public List<BalanceUpdateItem> Updates { get; set; } = new();
}

public class BalanceUpdateItem
{
    public int LeaveTypeId { get; set; }
    public int NewTotalDays { get; set; }
}

public class UpdateBalancesResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int UsersUpdated { get; set; }
    public int BalanceRecordsUpdated { get; set; }
}
