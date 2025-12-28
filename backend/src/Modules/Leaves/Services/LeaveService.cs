using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using taskedin_be.src.Infrastructure.Persistence;
using taskedin_be.src.Modules.Leaves.DTOs;
using taskedin_be.src.Modules.Leaves.Entities;
using taskedin_be.src.Modules.Notifications.Interfaces;
using taskedin_be.src.Modules.Notifications.Helpers;
using taskedin_be.src.Modules.Users.Services;
using taskedin_be.src.Modules.Users.Entities;

namespace taskedin_be.src.Modules.Leaves.Services;

public interface ILeaveService
{
    Task<LeaveRequestResponseDto> SubmitRequestAsync(int userId, SubmitLeaveRequestDto dto);
    Task CancelRequestAsync(int userId, int requestId);
    Task<ConflictCheckResponse> CheckConflictAsync(int userId, DateTime startDate, DateTime endDate);
    Task<List<DepartmentCoverageDto>> GetDepartmentCoverageAsync(int requesterId, DateTime? date);
    Task<PagedResult<LeaveRequestResponseDto>> GetMyLeavesAsync(int userId, PaginationDto pagination);
    Task<PagedResult<LeaveRequestResponseDto>> GetTeamRequestsAsync(int managerId, string? status, PaginationDto pagination);
    Task<PagedResult<LeaveRequestResponseDto>> GetAllRequestsAsync(LeaveRequestFilterDto filter);
    Task ManagerActionAsync(int managerId, int requestId, LeaveActionDto dto);
    Task HRActionAsync(int hrId, int requestId, LeaveActionDto dto);
    Task<IEnumerable<LeaveAuditLogDto>> GetRequestHistoryAsync(int requestId);
    Task<IEnumerable<LeaveBalanceSummaryDto>> GetBalancesAsync(int userId);
    Task<IEnumerable<LeaveSettingsDto>> GetLeaveSettingsAsync();
    Task UpdateLeaveSettingsAsync(List<LeaveSettingsDto> settings);
    Task<PagedResult<UserWithoutBalanceDto>> GetUsersWithoutBalancesAsync(PaginationDto pagination, int? year = null);
    Task<InitializeBalancesResponseDto> InitializeBalancesAsync(InitializeBalancesRequestDto request);
    Task<PagedResult<UserBalanceDto>> GetAllUsersWithBalancesAsync(PaginationDto pagination, int? year = null);
    Task<UpdateBalancesResponseDto> UpdateBalancesAsync(UpdateBalancesRequestDto request);
    Task<PendingApprovalCountDto> GetPendingApprovalCountAsync(int userId);
    Task<CalendarDataResponseDto> GetCalendarDataAsync(int userId, DateTime? startDate, DateTime? endDate);
    Task<IEnumerable<AuditLogDownloadDto>> GetAuditLogsForDownloadAsync(int userId);
}

public class LeaveService : ILeaveService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly UserService _userService;
    private readonly INotificationQueue _notificationQueue; // CHANGED: Use Queue instead of direct Service
    private const long MaxFileSize = 5 * 1024 * 1024;

    public LeaveService(
        AppDbContext context,
        IWebHostEnvironment environment,
        UserService userService,
        INotificationQueue notificationQueue) // CHANGED: Inject Queue
    {
        _context = context;
        _environment = environment;
        _userService = userService;
        _notificationQueue = notificationQueue;
    }

    public async Task<List<DepartmentCoverageDto>> GetDepartmentCoverageAsync(int requesterId, DateTime? date)
    {
        var targetDate = date?.Date ?? DateTime.UtcNow.Date;

        // 1. Retrieve the requester to check permissions
        var requester = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == requesterId)
                ?? throw new UnauthorizedAccessException("User not found.");

        // Start with all users who belong to a department
        IQueryable<User> usersQuery = _context.Users.Where(u => u.DepartmentId.HasValue);

        // 2. Permission Logic: Managers see only their department; HR sees all
        bool isHR = requester.Role.Name.Equals("HR", StringComparison.OrdinalIgnoreCase);
        bool isManager = requester.Role.Name.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        if (isHR)
        {
            // No additional filtering required
        }
        else if (isManager)
        {
            if (!requester.DepartmentId.HasValue)
                return new List<DepartmentCoverageDto>();

            usersQuery = usersQuery.Where(u => u.DepartmentId == requester.DepartmentId);
        }
        else
        {
            throw new UnauthorizedAccessException("Only Managers and HR can view department coverage.");
        }

        // 3. OPTIMIZED QUERY: Perform aggregation in the database
        var coverageStats = await usersQuery
            .GroupBy(u => u.DepartmentId ?? 0)
            .Select(g => new
            {
                DepartmentId = g.Key,
                TotalEmployees = g.Count(),
                // Count users in this group who have an Approved request overlapping the target date
                OnLeaveCount = g.Count(u => _context.LeaveRequests.Any(lr =>
                    lr.EmployeeId == u.Id &&
                    lr.Status == LeaveStatus.Approved &&
                    lr.StartDate <= targetDate &&
                    lr.EndDate >= targetDate))
            })
            .ToListAsync();

        // 4. Map the anonymous DB results to the DTO in memory
        var result = coverageStats.Select(s =>
        {
            var available = s.TotalEmployees - s.OnLeaveCount;
            var capacity = s.TotalEmployees == 0 ? 0 : Math.Round((double)available / s.TotalEmployees * 100, 1);

            return new DepartmentCoverageDto
            {
                DepartmentId = s.DepartmentId,
                TotalEmployees = s.TotalEmployees,
                OnLeaveCount = s.OnLeaveCount,
                AvailableCount = available,
                CapacityPercentage = capacity
            };
        }).ToList();

        return result;
    }

    public async Task<ConflictCheckResponse> CheckConflictAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var user = await _context.Users.FindAsync(userId)
             ?? throw new UnauthorizedAccessException("User not found.");

        // 1. Self-conflict check: User cannot have overlapping leave requests (any active status)
        var selfConflict = await _context.LeaveRequests
            .Where(l =>
                l.EmployeeId == userId &&
                l.Status != LeaveStatus.Cancelled &&
                l.Status != LeaveStatus.Rejected &&
                (l.StartDate <= endDate && l.EndDate >= startDate)
            )
            .FirstOrDefaultAsync();

        if (selfConflict != null)
        {
            return new ConflictCheckResponse
            {
                HasConflict = true,
                ConflictingEmployeeName = "You",
                Message = $"You already have a leave request ({selfConflict.Status}) for {selfConflict.StartDate:MMM dd} - {selfConflict.EndDate:MMM dd}."
            };
        }

        // 2. Team conflict check: Only block if another teammate has an APPROVED leave
        var conflictingRequest = await _context.LeaveRequests
            .Include(l => l.Employee)
            .Where(l =>
                l.EmployeeId != userId &&
                l.ManagerId == user.ManagerId &&
                l.Status == LeaveStatus.Approved &&
                (l.StartDate <= endDate && l.EndDate >= startDate)
            )
            .FirstOrDefaultAsync();

        if (conflictingRequest != null)
        {
            var empName = $"{conflictingRequest.Employee?.FirstName} {conflictingRequest.Employee?.LastName}";
            return new ConflictCheckResponse
            {
                HasConflict = true,
                ConflictingEmployeeName = empName,
                Message = $"Conflict detected: {empName} already has approved leave during this period."
            };
        }

        return new ConflictCheckResponse { HasConflict = false, Message = "No conflicts found." };
    }

    public async Task<LeaveRequestResponseDto> SubmitRequestAsync(int userId, SubmitLeaveRequestDto dto)
    {
        if (dto.EndDate < dto.StartDate)
            throw new ArgumentException("End date must be after start date.");

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
             ?? throw new UnauthorizedAccessException("User not found.");

        var totalDays = (dto.EndDate.Date - dto.StartDate.Date).Days + 1;
        var fiscalYear = dto.StartDate.Year;

        // Fetch leave config FIRST to determine if we can bypass conflict check
        var leaveConfig = await _context.Set<LeaveTypeConfig>()
            .FirstOrDefaultAsync(c => c.LeaveTypeId == (int)dto.Type);

        bool willAutoApprove = leaveConfig?.AutoApproveEnabled == true 
            && totalDays <= leaveConfig.AutoApproveThresholdDays;

        // Check conflict ONLY if not bypassing (bypass requires both auto-approve AND bypass flag)
        bool shouldBypassConflict = willAutoApprove && (leaveConfig?.BypassConflictCheck ?? false);

        if (!shouldBypassConflict)
        {
            var conflict = await CheckConflictAsync(userId, dto.StartDate, dto.EndDate);
            if (conflict.HasConflict)
            {
                throw new InvalidOperationException($"Cannot submit request: {conflict.Message}");
            }
        }

        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == userId && b.Year == fiscalYear && b.Type == dto.Type);

        if (balance == null)
            throw new InvalidOperationException($"No leave balance record found for {dto.Type} in {fiscalYear}.");

        var pendingDays = await _context.LeaveRequests
            .Where(r => r.EmployeeId == userId &&
                        r.Type == dto.Type &&
                        r.StartDate.Year == fiscalYear &&
                        (r.Status == LeaveStatus.PendingManager || r.Status == LeaveStatus.PendingHR))
            .SumAsync(r => r.NumberOfDays);

        var effectiveRemaining = balance.RemainingDays - pendingDays;

        if (effectiveRemaining < totalDays)
        {
            throw new InvalidOperationException($"Insufficient leave balance for {fiscalYear}. Remaining: {balance.RemainingDays}, Pending: {pendingDays}, Requested: {totalDays}.");
        }

        bool isAutoApproved = willAutoApprove;

        bool requesterIsManager = user.Role.Name.Equals("Manager", StringComparison.OrdinalIgnoreCase);
    
        // Managers with a ManagerId go through normal approval flow (their manager approves)
        // Only Department Heads (managers with no manager) go directly to HR
        bool isDepartmentHead = requesterIsManager && !user.ManagerId.HasValue;

        var status = isAutoApproved ? LeaveStatus.Approved :
                     isDepartmentHead ? LeaveStatus.PendingHR : LeaveStatus.PendingManager;

        int managerId;

        if (status == LeaveStatus.PendingManager)
        {
            if (!user.ManagerId.HasValue)
            {
                throw new InvalidOperationException("Request Failed: No direct manager assigned. Please contact HR to assign a manager.");
            }
            managerId = user.ManagerId.Value;
        }
        else
        {
            managerId = userId;
        }

        string? attachmentPath = null;
        if (dto.Attachment != null)
        {
            if (dto.Attachment.Length > MaxFileSize)
                throw new ArgumentException("File size exceeds the 5MB limit.");
            attachmentPath = await SaveFileAsync(dto.Attachment);
        }

        if (isAutoApproved)
        {
            balance.UsedDays += totalDays;
            _context.LeaveBalances.Update(balance);
        }

        var entity = new LeaveRequest
        {
            EmployeeId = userId,
            ManagerId = managerId,
            Type = dto.Type,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            NumberOfDays = totalDays,
            Notes = dto.Notes,
            AttachmentPath = attachmentPath,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<LeaveRequest>().Add(entity);

        _context.LeaveAuditLogs.Add(new LeaveAuditLog
        {
            LeaveRequest = entity,
            ActionByUserId = userId,
            Action = isAutoApproved ? LeaveAction.HRApproved : LeaveAction.Submitted,
            NewStatus = status,
            Comment = isAutoApproved ? "Auto-approved by system policy" : "Request submitted"
        });

        await _context.SaveChangesAsync();

        var empName = $"{user.FirstName} {user.LastName}";

        // --- NOTIFICATIONS (UPDATED TO QUEUE) ---
        if (isAutoApproved)
        {
            var msg = NotificationTemplates.StatusUpdate("Auto-Approved", dto.Type.ToString(), dto.StartDate, dto.EndDate);
            // Queuing job for background worker
            await _notificationQueue.QueueNotificationAsync(userId, msg.Subject, msg.Text);
        }
        else if (status == LeaveStatus.PendingManager)
        {
            var msg = NotificationTemplates.NewRequest(empName, dto.Type.ToString(), dto.StartDate, dto.EndDate, totalDays);
            await _notificationQueue.QueueNotificationAsync(managerId, msg.Subject, msg.Text);
        }
        else if (status == LeaveStatus.PendingHR)
        {
            // Only notify HR users in the same department as the manager
            var managerDeptId = user.DepartmentId;
            var hrUsers = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.Name.ToLower() == "hr" && u.DepartmentId == managerDeptId)
                .ToListAsync();

            var msg = NotificationTemplates.NewRequest(empName, dto.Type.ToString(), dto.StartDate, dto.EndDate, totalDays);

            foreach (var hr in hrUsers)
            {
                await _notificationQueue.QueueNotificationAsync(hr.Id, msg.Subject, msg.Text);
            }
        }

        return MapToDto(entity);
    }

    public async Task CancelRequestAsync(int userId, int requestId)
    {
        var request = await _context.LeaveRequests.FindAsync(requestId)
            ?? throw new KeyNotFoundException("Leave request not found.");

        if (request.EmployeeId != userId)
            throw new UnauthorizedAccessException("You can only cancel your own requests.");

        if (request.Status != LeaveStatus.PendingManager && request.Status != LeaveStatus.PendingHR)
            throw new InvalidOperationException("Cannot cancel request that has already been processed.");

        request.Status = LeaveStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;

        _context.LeaveAuditLogs.Add(new LeaveAuditLog
        {
            LeaveRequest = request,
            ActionByUserId = userId,
            Action = LeaveAction.Cancelled,
            NewStatus = LeaveStatus.Cancelled,
            Comment = "Cancelled by user",
            ActionDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var msg = NotificationTemplates.Cancelled(request.StartDate, request.EndDate);
        // CHANGED: Use Queue
        await _notificationQueue.QueueNotificationAsync(userId, msg.Subject, msg.Text);
    }

    public async Task<PagedResult<LeaveRequestResponseDto>> GetMyLeavesAsync(int userId, PaginationDto pagination)
    {
        var query = _context.Set<LeaveRequest>()
            .AsNoTracking()
            .Where(x => x.EmployeeId == userId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => MapToDto(x))
            .ToListAsync();

        return new PagedResult<LeaveRequestResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task ManagerActionAsync(int managerId, int requestId, LeaveActionDto dto)
    {
        var request = await _context.LeaveRequests.Include(r => r.Employee).FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new KeyNotFoundException("Leave request not found.");

        // Prevent self-approval
        if (request.EmployeeId == managerId)
            throw new InvalidOperationException("You cannot approve your own leave request.");

        if (request.ManagerId != managerId)
            throw new UnauthorizedAccessException("You are not the manager of this request.");

        if (request.Status != LeaveStatus.PendingManager)
            throw new InvalidOperationException("Request is not pending manager approval.");

        // If approving, check for conflicts with other requests already PendingHR or Approved
        if (dto.IsApproved)
        {
            var conflictingRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Where(l =>
                    l.Id != requestId &&
                    l.ManagerId == managerId &&
                    (l.Status == LeaveStatus.PendingHR || l.Status == LeaveStatus.Approved) &&
                    (l.StartDate <= request.EndDate && l.EndDate >= request.StartDate)
                )
                .FirstOrDefaultAsync();

            if (conflictingRequest != null)
            {
                var conflictName = $"{conflictingRequest.Employee?.FirstName} {conflictingRequest.Employee?.LastName}";
                throw new InvalidOperationException($"Cannot approve: {conflictName} already has a leave request for this period that is pending HR approval or approved.");
            }
        }

        var action = dto.IsApproved ? LeaveAction.ManagerApproved : LeaveAction.ManagerRejected;
        var newStatus = dto.IsApproved ? LeaveStatus.PendingHR : LeaveStatus.Rejected;

        // Log manager update
        UpdateStatusAndLog(request, managerId, action, newStatus, dto.Comment);
        await _context.SaveChangesAsync();

        // 1. Notify Employee of outcome
        var empStatusStr = dto.IsApproved ? "Approved by Manager" : "Rejected by Manager";
        var empMsg = NotificationTemplates.StatusUpdate(empStatusStr, request.Type.ToString(), request.StartDate, request.EndDate);
        // CHANGED: Use Queue
        await _notificationQueue.QueueNotificationAsync(request.EmployeeId, empMsg.Subject, empMsg.Text);

        // 2. Notify HR if Manager Approved (only HR in same department)
        if (dto.IsApproved)
        {
            var manager = await _context.Users.FindAsync(managerId);
            var managerDeptId = manager?.DepartmentId;

            // Only notify HR users in the same department as the manager
            var hrUsers = await _context.Users
               .Include(u => u.Role)
               .Where(u => u.Role.Name.ToLower() == "hr" && u.DepartmentId == managerDeptId)
               .ToListAsync();

            string mgrName = manager != null ? $"{manager.FirstName} {manager.LastName}" : "Manager";
            string empName = request.Employee != null ? $"{request.Employee.FirstName} {request.Employee.LastName}" : "Employee";

            var hrMsg = NotificationTemplates.ManagerActionToHR(mgrName, empName, request.Type.ToString(), request.NumberOfDays);

            foreach (var hr in hrUsers)
            {
                // CHANGED: Use Queue
                await _notificationQueue.QueueNotificationAsync(hr.Id, hrMsg.Subject, hrMsg.Text);
            }
        }
    }

    public async Task HRActionAsync(int hrId, int requestId, LeaveActionDto dto)
    {
        var request = await _context.LeaveRequests.FindAsync(requestId)
            ?? throw new KeyNotFoundException("Leave request not found.");

        // Prevent self-approval
        if (request.EmployeeId == hrId)
            throw new InvalidOperationException("You cannot approve your own leave request.");

        if (request.Status != LeaveStatus.PendingHR)
            throw new InvalidOperationException("Request is not pending HR approval.");

        var action = dto.IsApproved ? LeaveAction.HRApproved : LeaveAction.HRRejected;
        var newStatus = dto.IsApproved ? LeaveStatus.Approved : LeaveStatus.Rejected;

        if (dto.IsApproved)
        {
            // Check for conflicts - if another request under same manager is already Approved
            var conflictingRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Where(l =>
                    l.Id != requestId &&
                    l.ManagerId == request.ManagerId &&
                    l.Status == LeaveStatus.Approved &&
                    (l.StartDate <= request.EndDate && l.EndDate >= request.StartDate)
                )
                .FirstOrDefaultAsync();

            if (conflictingRequest != null)
            {
                var conflictName = $"{conflictingRequest.Employee?.FirstName} {conflictingRequest.Employee?.LastName}";
                throw new InvalidOperationException($"Cannot approve: {conflictName} already has an approved leave for this period.");
            }

            var fiscalYear = request.StartDate.Year;

            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId
                                       && b.Year == fiscalYear
                                       && b.Type == request.Type);

            if (balance == null) throw new InvalidOperationException($"Leave balance record not found for {fiscalYear}.");

            if (balance.RemainingDays < request.NumberOfDays)
                throw new InvalidOperationException("Insufficient leave balance.");

            balance.UsedDays += request.NumberOfDays;
            _context.LeaveBalances.Update(balance);
        }

        UpdateStatusAndLog(request, hrId, action, newStatus, dto.Comment);
        await _context.SaveChangesAsync();

        var statusStr = dto.IsApproved ? "Final Approved" : "Rejected by HR";
        var msg = NotificationTemplates.StatusUpdate(statusStr, request.Type.ToString(), request.StartDate, request.EndDate);

        // CHANGED: Use Queue
        await _notificationQueue.QueueNotificationAsync(request.EmployeeId, msg.Subject, msg.Text);
    }

    public async Task<PagedResult<LeaveRequestResponseDto>> GetTeamRequestsAsync(int managerId, string? status, PaginationDto pagination)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Manager)
            .AsQueryable();

        LeaveStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveStatus>(status, true, out var parsedStatus))
        {
            statusEnum = parsedStatus;
        }

        if (!statusEnum.HasValue || statusEnum.Value != LeaveStatus.PendingHR)
        {
            // Manager view: exclude own requests
            query = query.Where(x => x.ManagerId == managerId && x.EmployeeId != managerId);
        }
        else if (statusEnum.Value == LeaveStatus.PendingHR)
        {
            // HR view: Get HR user's department
            var hrUser = await _context.Users.FindAsync(managerId);
            
            // Filter by department (using Manager's DepartmentId) and exclude own requests
            if (hrUser?.DepartmentId.HasValue == true)
            {
                query = query.Where(x => 
                    x.EmployeeId != managerId && 
                    x.Manager != null && 
                    x.Manager.DepartmentId == hrUser.DepartmentId);
            }
            else
            {
                // HR without department sees nothing (or throw error)
                query = query.Where(x => false);
            }
        }

        if (statusEnum.HasValue)
        {
            query = query.Where(x => x.Status == statusEnum.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(x => MapToDto(x))
            .ToListAsync();

        return new PagedResult<LeaveRequestResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PagedResult<LeaveRequestResponseDto>> GetAllRequestsAsync(LeaveRequestFilterDto filter)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Include(x => x.Employee)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<LeaveStatus>(filter.Status, true, out var statusEnum))
        {
            query = query.Where(x => x.Status == statusEnum);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.Employee!.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(x => x.StartDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(x => x.EndDate <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => MapToDto(x))
            .ToListAsync();

        return new PagedResult<LeaveRequestResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<LeaveAuditLogDto>> GetRequestHistoryAsync(int requestId)
    {
        var logs = await _context.LeaveAuditLogs
            .AsNoTracking()
            .Include(l => l.ActionByUser)
            .Where(l => l.LeaveRequestId == requestId)
            .OrderBy(l => l.ActionDate)
            .ToListAsync();

        return logs.Select(l => new LeaveAuditLogDto
        {
            Id = l.Id,
            Action = l.Action.ToString(),
            ActionBy = $"{l.ActionByUser?.FirstName} {l.ActionByUser?.LastName}",
            NewStatus = l.NewStatus.ToString(),
            Comment = l.Comment,
            ActionDate = l.ActionDate
        });
    }

    public async Task<IEnumerable<LeaveBalanceSummaryDto>> GetBalancesAsync(int userId)
    {
        var currentYear = DateTime.UtcNow.Year;

        var dbBalances = await _context.LeaveBalances
            .AsNoTracking()
            .Where(b => b.EmployeeId == userId && b.Year == currentYear)
            .ToListAsync();

        var allTypes = Enum.GetValues(typeof(LeaveType)).Cast<LeaveType>();
        var result = new List<LeaveBalanceSummaryDto>();

        foreach (var type in allTypes)
        {
            var existing = dbBalances.FirstOrDefault(b => b.Type == type);
            if (existing != null)
            {
                result.Add(new LeaveBalanceSummaryDto
                {
                    Type = type.ToString(),
                    TotalDays = existing.TotalDays,
                    UsedDays = existing.UsedDays,
                    RemainingDays = existing.RemainingDays
                });
            }
            else
            {
                result.Add(new LeaveBalanceSummaryDto
                {
                    Type = type.ToString(),
                    TotalDays = 0,
                    UsedDays = 0,
                    RemainingDays = 0
                });
            }
        }

        return result;
    }

    public async Task<IEnumerable<LeaveSettingsDto>> GetLeaveSettingsAsync()
    {
        var configs = await _context.Set<LeaveTypeConfig>().ToListAsync();
        var allTypes = Enum.GetValues(typeof(LeaveType)).Cast<LeaveType>();

        var result = new List<LeaveSettingsDto>();

        foreach (var type in allTypes)
        {
            var existing = configs.FirstOrDefault(c => c.LeaveTypeId == (int)type);
            if (existing != null)
            {
                result.Add(new LeaveSettingsDto
                {
                    LeaveTypeId = existing.LeaveTypeId,
                    Name = type.ToString(),
                    DefaultBalance = existing.DefaultBalance,
                    AutoApproveEnabled = existing.AutoApproveEnabled,
                    AutoApproveThresholdDays = existing.AutoApproveThresholdDays,
                    BypassConflictCheck = existing.BypassConflictCheck
                });
            }
            else
            {
                result.Add(new LeaveSettingsDto
                {
                    LeaveTypeId = (int)type,
                    Name = type.ToString(),
                    DefaultBalance = 21,
                    AutoApproveEnabled = false,
                    AutoApproveThresholdDays = 0,
                    BypassConflictCheck = false
                });
            }
        }
        return result;
    }

    public async Task UpdateLeaveSettingsAsync(List<LeaveSettingsDto> settings)
    {
        foreach (var dto in settings)
        {
            var config = await _context.Set<LeaveTypeConfig>()
                .FirstOrDefaultAsync(c => c.LeaveTypeId == dto.LeaveTypeId);

            if (config == null)
            {
                config = new LeaveTypeConfig
                {
                    LeaveTypeId = dto.LeaveTypeId,
                    Name = dto.Name
                };
                _context.Set<LeaveTypeConfig>().Add(config);
            }

            config.DefaultBalance = dto.DefaultBalance;
            config.AutoApproveEnabled = dto.AutoApproveEnabled;
            config.AutoApproveThresholdDays = dto.AutoApproveThresholdDays;
            config.BypassConflictCheck = dto.BypassConflictCheck;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<UserWithoutBalanceDto>> GetUsersWithoutBalancesAsync(PaginationDto pagination, int? year = null)
    {
        // Use specified year or default to current year
        var targetYear = year ?? DateTime.UtcNow.Year;
        
        // Use subquery for efficiency with large datasets - avoids loading all IDs into memory
        var usersWithBalancesQuery = _context.LeaveBalances
            .Where(b => b.Year == targetYear)
            .Select(b => b.EmployeeId)
            .Distinct();

        // Query users who don't have balance records using NOT IN subquery
        var query = _context.Users
            .AsNoTracking()
            .Where(u => !usersWithBalancesQuery.Contains(u.Id));

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Get paginated results
        var items = await query
            .OrderBy(u => u.Id)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(u => new UserWithoutBalanceDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email
            })
            .ToListAsync();

        return new PagedResult<UserWithoutBalanceDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<InitializeBalancesResponseDto> InitializeBalancesAsync(InitializeBalancesRequestDto request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return new InitializeBalancesResponseDto
            {
                Message = "No users specified. Please select users from the list to initialize.",
                Count = 0
            };
        }

        // Limit batch size to prevent memory issues (max 1000 users per request)
        const int maxBatchSize = 1000;
        if (request.UserIds.Count > maxBatchSize)
        {
            return new InitializeBalancesResponseDto
            {
                Message = $"Too many users selected. Maximum {maxBatchSize} users per request. Please use pagination.",
                Count = 0
            };
        }

        // Use specified year or default to current year
        var targetYear = request.Year ?? DateTime.UtcNow.Year;
        var userIds = request.UserIds;
        
        // Get leave type configs for default balances
        var configs = await _context.Set<LeaveTypeConfig>().ToListAsync();
        var allLeaveTypes = Enum.GetValues(typeof(LeaveType)).Cast<LeaveType>().ToList();

        // Bulk query: Get all existing balance types for the specified users in one query
        var existingBalances = await _context.LeaveBalances
            .Where(b => userIds.Contains(b.EmployeeId) && b.Year == targetYear)
            .Select(b => new { b.EmployeeId, b.Type })
            .ToListAsync();

        var existingByUser = existingBalances
            .GroupBy(b => b.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Type).ToHashSet());

        var balancesToAdd = new List<LeaveBalance>();
        var now = DateTime.UtcNow;

        foreach (var userId in userIds)
        {
            var userExistingTypes = existingByUser.GetValueOrDefault(userId) ?? new HashSet<LeaveType>();

            foreach (var leaveType in allLeaveTypes)
            {
                if (!userExistingTypes.Contains(leaveType))
                {
                    var config = configs.FirstOrDefault(c => c.LeaveTypeId == (int)leaveType);
                    var defaultDays = config?.DefaultBalance ?? (leaveType == LeaveType.Annual ? 21 : 7);

                    balancesToAdd.Add(new LeaveBalance
                    {
                        EmployeeId = userId,
                        Type = leaveType,
                        Year = targetYear,
                        TotalDays = defaultDays,
                        UsedDays = 0,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }
        }

        if (balancesToAdd.Any())
        {
            _context.LeaveBalances.AddRange(balancesToAdd);
            await _context.SaveChangesAsync();
        }

        return new InitializeBalancesResponseDto
        {
            Message = $"Leave balances for {targetYear} initialized for {userIds.Count} users ({balancesToAdd.Count} balance records created)",
            Count = userIds.Count
        };
    }

    public async Task<PagedResult<UserBalanceDto>> GetAllUsersWithBalancesAsync(PaginationDto pagination, int? year = null)
    {
        // Use specified year or default to current year
        var targetYear = year ?? DateTime.UtcNow.Year;
        var allLeaveTypes = Enum.GetValues(typeof(LeaveType)).Cast<LeaveType>().ToList();

        // Get total count of users
        var totalCount = await _context.Users.CountAsync();

        // Get paginated users
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        // Bulk query: Get all balances for these users in one query
        var balances = await _context.LeaveBalances
            .AsNoTracking()
            .Where(b => userIds.Contains(b.EmployeeId) && b.Year == targetYear)
            .ToListAsync();

        var balancesByUser = balances
            .GroupBy(b => b.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build result with all leave types for each user
        var items = users.Select(u =>
        {
            var userBalances = balancesByUser.GetValueOrDefault(u.Id) ?? new List<LeaveBalance>();
            
            return new UserBalanceDto
            {
                UserId = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Balances = allLeaveTypes.Select(lt =>
                {
                    var balance = userBalances.FirstOrDefault(b => b.Type == lt);
                    return new LeaveBalanceItemDto
                    {
                        LeaveTypeId = (int)lt,
                        LeaveTypeName = lt.ToString(),
                        TotalDays = balance?.TotalDays ?? 0,
                        UsedDays = balance?.UsedDays ?? 0,
                        RemainingDays = balance?.RemainingDays ?? 0
                    };
                }).ToList()
            };
        }).ToList();

        return new PagedResult<UserBalanceDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<UpdateBalancesResponseDto> UpdateBalancesAsync(UpdateBalancesRequestDto request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
        {
            return new UpdateBalancesResponseDto
            {
                Message = "No users specified",
                UsersUpdated = 0,
                BalanceRecordsUpdated = 0
            };
        }

        if (request.Updates == null || !request.Updates.Any())
        {
            return new UpdateBalancesResponseDto
            {
                Message = "No balance updates specified",
                UsersUpdated = 0,
                BalanceRecordsUpdated = 0
            };
        }

        // Limit batch size to prevent memory issues
        const int maxBatchSize = 1000;
        if (request.UserIds.Count > maxBatchSize)
        {
            return new UpdateBalancesResponseDto
            {
                Message = $"Too many users selected. Maximum {maxBatchSize} users per request.",
                UsersUpdated = 0,
                BalanceRecordsUpdated = 0
            };
        }

        // Use specified year or default to current year
        var targetYear = request.Year ?? DateTime.UtcNow.Year;
        var userIds = request.UserIds;
        var leaveTypeIds = request.Updates.Select(u => u.LeaveTypeId).ToList();

        // Get existing balances for the specified users and leave types
        var existingBalances = await _context.LeaveBalances
            .Where(b => userIds.Contains(b.EmployeeId) 
                     && b.Year == targetYear 
                     && leaveTypeIds.Contains((int)b.Type))
            .ToListAsync();

        var now = DateTime.UtcNow;
        int recordsUpdated = 0;
        int recordsCreated = 0;
        var balancesToAdd = new List<LeaveBalance>();

        // Group existing balances by user for quick lookup
        var balancesByUser = existingBalances
            .GroupBy(b => b.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(b => (int)b.Type));

        foreach (var userId in userIds)
        {
            var userBalances = balancesByUser.GetValueOrDefault(userId) ?? new Dictionary<int, LeaveBalance>();

            foreach (var update in request.Updates)
            {
                if (userBalances.TryGetValue(update.LeaveTypeId, out var existingBalance))
                {
                    // Update existing balance - only update TotalDays, preserve UsedDays
                    existingBalance.TotalDays = update.NewTotalDays;
                    existingBalance.UpdatedAt = now;
                    recordsUpdated++;
                }
                else
                {
                    // Create new balance record if it doesn't exist
                    balancesToAdd.Add(new LeaveBalance
                    {
                        EmployeeId = userId,
                        Type = (LeaveType)update.LeaveTypeId,
                        Year = targetYear,
                        TotalDays = update.NewTotalDays,
                        UsedDays = 0,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                    recordsCreated++;
                }
            }
        }

        if (balancesToAdd.Any())
        {
            _context.LeaveBalances.AddRange(balancesToAdd);
        }

        await _context.SaveChangesAsync();

        return new UpdateBalancesResponseDto
        {
            Message = $"Updated {recordsUpdated} balance records for {targetYear}, created {recordsCreated} new records for {userIds.Count} users",
            UsersUpdated = userIds.Count,
            BalanceRecordsUpdated = recordsUpdated + recordsCreated
        };
    }

    public async Task<PendingApprovalCountDto> GetPendingApprovalCountAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        int pendingManager = 0;
        int pendingHR = 0;

        var roleName = user.Role.Name.ToLower();

        if (roleName == "manager")
        {
            // Managers see requests where they are the assigned manager
            pendingManager = await _context.LeaveRequests
                .CountAsync(r => r.ManagerId == userId && r.Status == LeaveStatus.PendingManager);
        }

        if (roleName == "hr")
        {
            // HR sees only requests from their department (via Manager's DepartmentId)
            if (user.DepartmentId.HasValue)
            {
                pendingHR = await _context.LeaveRequests
                    .Include(r => r.Manager)
                    .CountAsync(r => r.Status == LeaveStatus.PendingHR && 
                                     r.Manager != null && 
                                     r.Manager.DepartmentId == user.DepartmentId);
            }
        }
        else if (roleName == "admin")
        {
            // Admin sees all requests pending HR approval
            pendingHR = await _context.LeaveRequests
                .CountAsync(r => r.Status == LeaveStatus.PendingHR);
        }

        return new PendingApprovalCountDto
        {
            PendingManagerApproval = pendingManager,
            PendingHRApproval = pendingHR,
            TotalPending = pendingManager + pendingHR
        };
    }

    public async Task<CalendarDataResponseDto> GetCalendarDataAsync(int userId, DateTime? startDate, DateTime? endDate)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var roleName = user.Role.Name.ToLower();

        // Base query for approved requests
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .Include(r => r.Manager)
            .Where(r => r.Status == LeaveStatus.Approved);

        // Apply date filter if provided
        if (startDate.HasValue)
            query = query.Where(r => r.EndDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(r => r.StartDate <= endDate.Value);

        if (roleName == "hr" || roleName == "admin")
        {
            // HR/Admin: Get all approved requests grouped by manager
            var requests = await query.ToListAsync();

            var grouped = requests
                .GroupBy(r => r.ManagerId)
                .Select(g =>
                {
                    var manager = g.First().Manager;
                    return new CalendarGroupedByManagerDto
                    {
                        ManagerId = g.Key,
                        ManagerName = manager != null ? $"{manager.FirstName} {manager.LastName}" : "Unknown",
                        DepartmentId = manager?.DepartmentId,
                        Leaves = g.Select(r => new CalendarLeaveDto
                        {
                            Id = r.Id,
                            EmployeeId = r.EmployeeId,
                            EmployeeName = r.Employee != null ? $"{r.Employee.FirstName} {r.Employee.LastName}" : "Unknown",
                            LeaveType = r.Type.ToString(),
                            StartDate = r.StartDate,
                            EndDate = r.EndDate,
                            NumberOfDays = r.NumberOfDays
                        }).ToList()
                    };
                })
                .OrderBy(g => g.ManagerName)
                .ToList();

            return new CalendarDataResponseDto { GroupedByManager = grouped };
        }
        else if (roleName == "manager")
        {
            // Manager: Get approved requests of their team
            var requests = await query
                .Where(r => r.ManagerId == userId)
                .OrderBy(r => r.StartDate)
                .Select(r => new CalendarLeaveDto
                {
                    Id = r.Id,
                    EmployeeId = r.EmployeeId,
                    EmployeeName = r.Employee != null ? $"{r.Employee.FirstName} {r.Employee.LastName}" : "Unknown",
                    LeaveType = r.Type.ToString(),
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    NumberOfDays = r.NumberOfDays
                })
                .ToListAsync();

            return new CalendarDataResponseDto { Leaves = requests };
        }
        else
        {
            // Employee: Get only their own approved requests
            var requests = await query
                .Where(r => r.EmployeeId == userId)
                .OrderBy(r => r.StartDate)
                .Select(r => new CalendarLeaveDto
                {
                    Id = r.Id,
                    EmployeeId = r.EmployeeId,
                    EmployeeName = r.Employee != null ? $"{r.Employee.FirstName} {r.Employee.LastName}" : "Unknown",
                    LeaveType = r.Type.ToString(),
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    NumberOfDays = r.NumberOfDays
                })
                .ToListAsync();

            return new CalendarDataResponseDto { Leaves = requests };
        }
    }

    public async Task<IEnumerable<AuditLogDownloadDto>> GetAuditLogsForDownloadAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new UnauthorizedAccessException("User not found.");

        bool isHR = user.Role.Name.Equals("HR", StringComparison.OrdinalIgnoreCase);
        bool isManager = user.Role.Name.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        if (!isHR && !isManager)
        {
            throw new UnauthorizedAccessException("Only Managers and HR can download audit logs.");
        }

        var query = _context.LeaveAuditLogs
            .AsNoTracking()
            .Include(l => l.LeaveRequest)
                .ThenInclude(r => r!.Employee)
            .Include(l => l.LeaveRequest)
                .ThenInclude(r => r!.Manager)
            .Where(l => l.ActionByUserId == userId);

        if (isManager)
        {
            // Manager: Only ManagerApproved or ManagerRejected actions by this manager
            query = query.Where(l => 
                l.Action == LeaveAction.ManagerApproved || 
                l.Action == LeaveAction.ManagerRejected);
        }
        else // isHR
        {
            // HR: Only HRApproved or HRRejected actions by this HR
            query = query.Where(l => 
                l.Action == LeaveAction.HRApproved || 
                l.Action == LeaveAction.HRRejected);
        }

        var logs = await query
            .OrderByDescending(l => l.ActionDate)
            .ToListAsync();

        var result = logs.Select(l => new AuditLogDownloadDto
        {
            DepartmentName = l.LeaveRequest?.Employee?.DepartmentId?.ToString() ?? "N/A",
            ManagerName = l.LeaveRequest?.Manager != null 
                ? $"{l.LeaveRequest.Manager.FirstName} {l.LeaveRequest.Manager.LastName}" 
                : "N/A",
            RequestId = l.LeaveRequestId,
            EmployeeName = l.LeaveRequest?.Employee != null 
                ? $"{l.LeaveRequest.Employee.FirstName} {l.LeaveRequest.Employee.LastName}" 
                : "Unknown",
            LeaveType = l.LeaveRequest?.Type.ToString() ?? "Unknown",
            StartDate = l.LeaveRequest?.StartDate ?? DateTime.MinValue,
            EndDate = l.LeaveRequest?.EndDate ?? DateTime.MinValue,
            NumberOfDays = l.LeaveRequest?.NumberOfDays ?? 0,
            CurrentStatus = l.LeaveRequest?.Status.ToString() ?? "Unknown",
            ActionTaken = l.Action.ToString(),
            ActionDate = l.ActionDate,
            Comment = l.Comment
        });

        if (isHR)
        {
            // Group by Department, then by Manager for HR
            result = result
                .OrderBy(r => r.DepartmentName)
                .ThenBy(r => r.ManagerName)
                .ThenByDescending(r => r.ActionDate);
        }

        return result.ToList();
    }

    private void UpdateStatusAndLog(LeaveRequest request, int actorId, LeaveAction action, LeaveStatus newStatus, string? comment)
    {
        request.Status = newStatus;
        request.UpdatedAt = DateTime.UtcNow;

        var log = new LeaveAuditLog
        {
            LeaveRequestId = request.Id,
            ActionByUserId = actorId,
            Action = action,
            NewStatus = newStatus,
            Comment = comment,
            ActionDate = DateTime.UtcNow
        };
        _context.LeaveAuditLogs.Add(log);
    }

    private async Task<string> SaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty.");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(ext))
            throw new ArgumentException("Invalid file type. Only images and documents are allowed.");

        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "leaves");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/leaves/{uniqueFileName}";
    }

    private static LeaveRequestResponseDto MapToDto(LeaveRequest entity)
    {
        return new LeaveRequestResponseDto
        {
            Id = entity.Id,
            LeaveType = entity.Type.ToString(),
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            NumberOfDays = entity.NumberOfDays,
            Status = entity.Status.ToString(),
            Notes = entity.Notes,
            AttachmentUrl = entity.AttachmentPath,
            ManagerId = entity.ManagerId,
            CreatedAt = entity.CreatedAt,
            EmployeeEmail = entity.Employee?.Email ?? string.Empty
        };
    }
}