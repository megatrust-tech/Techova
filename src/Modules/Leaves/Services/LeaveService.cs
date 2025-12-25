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

        var conflictingRequest = await _context.LeaveRequests
            .Include(l => l.Employee)
            .Where(l =>
                l.EmployeeId != userId &&
                l.Employee!.DepartmentId == user.DepartmentId &&
                (l.Status == LeaveStatus.PendingManager || l.Status == LeaveStatus.PendingHR || l.Status == LeaveStatus.Approved) &&
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
                Message = $"Conflict detected: {empName} has leave during this period."
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

        var conflict = await CheckConflictAsync(userId, dto.StartDate, dto.EndDate);
        if (conflict.HasConflict)
        {
            throw new InvalidOperationException($"Cannot submit request: {conflict.Message}");
        }

        var totalDays = (dto.EndDate.Date - dto.StartDate.Date).Days + 1;
        var fiscalYear = dto.StartDate.Year;

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

        var leaveConfig = await _context.Set<LeaveTypeConfig>()
            .FirstOrDefaultAsync(c => c.LeaveTypeId == (int)dto.Type);

        bool isAutoApproved = false;
        if (leaveConfig != null && leaveConfig.AutoApproveEnabled)
        {
            if (totalDays <= leaveConfig.AutoApproveThresholdDays)
            {
                isAutoApproved = true;
            }
        }

        bool requesterIsManager = user.Role.Name.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        var status = isAutoApproved ? LeaveStatus.Approved :
                     requesterIsManager ? LeaveStatus.PendingHR : LeaveStatus.PendingManager;

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
            var hrUsers = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.Name.ToLower() == "hr")
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

        if (request.ManagerId != managerId)
            throw new UnauthorizedAccessException("You are not the manager of this request.");

        if (request.Status != LeaveStatus.PendingManager)
            throw new InvalidOperationException("Request is not pending manager approval.");

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

        // 2. Notify HR if Manager Approved
        if (dto.IsApproved)
        {
            var hrUsers = await _context.Users
               .Include(u => u.Role)
               .Where(u => u.Role.Name.ToLower() == "hr")
               .ToListAsync();

            var manager = await _context.Users.FindAsync(managerId);
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

        if (request.Status != LeaveStatus.PendingHR)
            throw new InvalidOperationException("Request is not pending HR approval.");

        var action = dto.IsApproved ? LeaveAction.HRApproved : LeaveAction.HRRejected;
        var newStatus = dto.IsApproved ? LeaveStatus.Approved : LeaveStatus.Rejected;

        if (dto.IsApproved)
        {
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
            .AsQueryable();

        LeaveStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveStatus>(status, true, out var parsedStatus))
        {
            statusEnum = parsedStatus;
        }

        if (!statusEnum.HasValue || statusEnum.Value != LeaveStatus.PendingHR)
        {
            query = query.Where(x => x.ManagerId == managerId && x.EmployeeId != managerId);
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
                    AutoApproveThresholdDays = existing.AutoApproveThresholdDays
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
                    AutoApproveThresholdDays = 0
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
        }

        await _context.SaveChangesAsync();
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