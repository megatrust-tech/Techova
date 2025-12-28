using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using taskedin_be.src.Infrastructure.Persistence;
using taskedin_be.src.Modules.Leaves.Services;
using taskedin_be.src.Modules.Leaves.DTOs;
using taskedin_be.src.Modules.Leaves.Entities;
using taskedin_be.src.Modules.Users.Entities;
using taskedin_be.src.Modules.Users.Services;
using taskedin_be.src.Modules.Notifications.Interfaces;

namespace taskedin_be.src.Modules.Leaves.Tests;

public class LeaveServiceTests
{
    private readonly AppDbContext _context;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    // CHANGED: Mock the Queue, not the Service
    private readonly Mock<INotificationQueue> _mockNotificationQueue;
    private readonly LeaveService _leaveService;

    public LeaveServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;

        _context = new AppDbContext(options);

        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(e => e.WebRootPath).Returns("wwwroot");

        // CHANGED: Initialize Queue Mock
        _mockNotificationQueue = new Mock<INotificationQueue>();
        var userService = new UserService(_context);

        // CHANGED: Inject Queue Mock
        _leaveService = new LeaveService(_context, _mockEnvironment.Object, userService, _mockNotificationQueue.Object);
    }

    // --- Helpers ---
    private async Task SeedUser(int userId, int? deptId, int roleId = 2, int? managerId = null)
    {
        // Check if role exists, if not add it
        if (!_context.Roles.Any(r => r.Id == roleId))
        {
            _context.Roles.Add(new Role { Id = roleId, Name = roleId == 1 ? "Manager" : roleId == 3 ? "HR" : "Employee" });
            await _context.SaveChangesAsync();
        }

        var user = new User
        {
            Id = userId,
            DepartmentId = deptId,
            FirstName = $"User{userId}",
            LastName = "Test",
            RoleId = roleId,
            Email = $"user{userId}@test.com",
            ManagerId = managerId // Added to support Manager Check
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task SeedBalance(int userId, LeaveType type, int total, int year, int used = 0)
    {
        _context.LeaveBalances.Add(new LeaveBalance
        {
            EmployeeId = userId,
            Year = year,
            Type = type,
            TotalDays = total,
            UsedDays = used
        });
        await _context.SaveChangesAsync();
    }

    private async Task<int> SeedRequest(int empId, int mgrId, LeaveStatus status, DateTime start, DateTime end, int deptId, LeaveType type = LeaveType.Annual)
    {
        // Ensure users exist if not already seeded
        if (!await _context.Users.AnyAsync(u => u.Id == empId)) await SeedUser(empId, deptId);
        if (!await _context.Users.AnyAsync(u => u.Id == mgrId)) await SeedUser(mgrId, deptId, roleId: 1);

        var req = new LeaveRequest
        {
            EmployeeId = empId,
            ManagerId = mgrId,
            Type = type,
            StartDate = start,
            EndDate = end,
            NumberOfDays = (end - start).Days + 1,
            Status = status
        };
        _context.LeaveRequests.Add(req);
        await _context.SaveChangesAsync();
        return req.Id;
    }

    private async Task SeedLeaveSettings(LeaveType type, bool autoApprove, int threshold, bool bypassConflictCheck = false)
    {
        _context.LeaveTypeConfigs.Add(new LeaveTypeConfig
        {
            LeaveTypeId = (int)type,
            Name = type.ToString(),
            AutoApproveEnabled = autoApprove,
            AutoApproveThresholdDays = threshold,
            DefaultBalance = 21,
            BypassConflictCheck = bypassConflictCheck
        });
        await _context.SaveChangesAsync();
    }

    // ==========================================
    // 1. Validation & Submission Tests
    // ==========================================

    [Fact]
    public async Task Submit_DatesInverted_ThrowsArgumentException()
    {
        var dto = new SubmitLeaveRequestDto { StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(-1), Type = LeaveType.Annual };
        await Assert.ThrowsAsync<ArgumentException>(() => _leaveService.SubmitRequestAsync(1, dto));
    }

    [Theory]
    [InlineData(10, 0, 5, false)] // Have 10, Used 0, Request 5 -> Pass
    [InlineData(5, 0, 5, false)]  // Have 5, Used 0, Request 5 -> Pass (Boundary)
    [InlineData(5, 0, 6, true)]   // Have 5, Used 0, Request 6 -> Fail
    [InlineData(10, 8, 3, true)]  // Have 10, Used 8, Request 3 -> Fail (Total used would be 11)
    public async Task Submit_BalanceChecks(int total, int used, int requested, bool shouldFail)
    {
        int userId = 1;
        int deptId = 1;
        int mgrId = 999;

        // Seed Manager first
        await SeedUser(mgrId, deptId, roleId: 1);
        // Seed User with Manager Assigned
        await SeedUser(userId, deptId, roleId: 2, managerId: mgrId);

        // Uses UTC Now Year
        await SeedBalance(userId, LeaveType.Annual, total, DateTime.UtcNow.Year, used);

        var dto = new SubmitLeaveRequestDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(requested - 1),
            Type = LeaveType.Annual
        };

        if (shouldFail)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _leaveService.SubmitRequestAsync(userId, dto));
        }
        else
        {
            var result = await _leaveService.SubmitRequestAsync(userId, dto);
            Assert.Equal(LeaveStatus.PendingManager.ToString(), result.Status);
        }
    }

    [Fact]
    public async Task Submit_PendingRequestsCountTowardsBalanceLimit()
    {
        int userId = 99;
        int mgrId = 100;
        int deptId = 1;

        // Ensure user has manager
        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(userId, deptId, roleId: 2, managerId: mgrId);

        int currentYear = DateTime.UtcNow.Year;
        await SeedBalance(userId, LeaveType.Annual, 10, currentYear, 0);

        var startPending = new DateTime(currentYear, 1, 10);
        var endPending = new DateTime(currentYear, 1, 14); // 5 days

        // Seed Pending Request
        await SeedRequest(userId, mgrId, LeaveStatus.PendingManager, startPending, endPending, deptId);

        // New request later in the year
        var dto = new SubmitLeaveRequestDto
        {
            StartDate = new DateTime(currentYear, 6, 1),
            EndDate = new DateTime(currentYear, 6, 6), // 6 days
            Type = LeaveType.Annual
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _leaveService.SubmitRequestAsync(userId, dto));
        Assert.Contains("Insufficient leave balance", ex.Message);
    }

    [Fact]
    public async Task Submit_CrossYear_DeductsFromStartYear()
    {
        int userId = 150;
        int deptId = 1;
        int mgrId = 151;

        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(userId, deptId, roleId: 2, managerId: mgrId);

        await SeedBalance(userId, LeaveType.Annual, 10, 2023, 0);

        var dto = new SubmitLeaveRequestDto
        {
            StartDate = new DateTime(2023, 12, 31),
            EndDate = new DateTime(2024, 1, 2),
            Type = LeaveType.Annual
        };

        var result = await _leaveService.SubmitRequestAsync(userId, dto);
        Assert.NotNull(result);
        Assert.Equal(LeaveStatus.PendingManager.ToString(), result.Status);
    }

    [Fact]
    public async Task HRAction_CrossYear_DeductsFromStartYear()
    {
        int userId = 160;
        int hrId = 161;
        await SeedUser(userId, 1);
        await SeedUser(hrId, null, roleId: 3);

        await SeedBalance(userId, LeaveType.Annual, 10, 2023, 0);

        int reqId = await SeedRequest(userId, 999, LeaveStatus.PendingHR,
            new DateTime(2023, 12, 31), new DateTime(2024, 1, 2), 1);

        // Act
        await _leaveService.HRActionAsync(hrId, reqId, new LeaveActionDto { IsApproved = true });

        // Assert
        var bal2023 = await _context.LeaveBalances.FirstAsync(b => b.EmployeeId == userId && b.Year == 2023);

        Assert.Equal(3, bal2023.UsedDays);
    }

    // ==========================================
    // 2. Department Overlap Edge Cases
    // ==========================================

    [Theory]
    [InlineData("2023-01-01", "2023-01-05", true)]  // Exact Match -> Fail
    [InlineData("2023-01-02", "2023-01-04", true)]  // Inside/Subset -> Fail
    [InlineData("2022-12-31", "2023-01-02", true)]  // Overlap Start -> Fail
    [InlineData("2023-01-04", "2023-01-06", true)]  // Overlap End -> Fail
    [InlineData("2023-01-06", "2023-01-10", false)] // Adjacent (Touch end) -> Pass
    [InlineData("2022-12-25", "2022-12-31", false)] // Adjacent (Touch start) -> Pass
    public async Task Submit_DepartmentOverlap_Scenarios(string reqStartStr, string reqEndStr, bool shouldFail)
    {
        int colleagueId = 10;
        int myId = 11;
        int deptId = 100;
        int mgrId = 12;

        // Seed Manager
        await SeedUser(mgrId, deptId, roleId: 1);
        // Seed Users with Manager
        await SeedUser(colleagueId, deptId, roleId: 2, managerId: mgrId);
        await SeedUser(myId, deptId, roleId: 2, managerId: mgrId);

        await SeedBalance(myId, LeaveType.Annual, 20, 2023);
        await SeedBalance(myId, LeaveType.Annual, 20, 2022); // For 2022 cases

        // Existing approved leave for colleague
        await SeedRequest(colleagueId, mgrId, LeaveStatus.Approved, new DateTime(2023, 1, 1), new DateTime(2023, 1, 5), deptId);

        var dto = new SubmitLeaveRequestDto
        {
            StartDate = DateTime.Parse(reqStartStr),
            EndDate = DateTime.Parse(reqEndStr),
            Type = LeaveType.Annual
        };

        if (shouldFail)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _leaveService.SubmitRequestAsync(myId, dto));
            Assert.Contains("conflict", ex.Message.ToLower());
        }
        else
        {
            var res = await _leaveService.SubmitRequestAsync(myId, dto);
            Assert.NotNull(res);
        }
    }

    [Fact]
    public async Task CheckConflict_Standalone_ReturnsCorrectFlag()
    {
        int colleagueId = 30;
        int myId = 31;
        int deptId = 300;
        int mgrId = 32;

        // Both users must have same manager for conflict check (now checks by managerId)
        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(colleagueId, deptId, roleId: 2, managerId: mgrId);
        await SeedUser(myId, deptId, roleId: 2, managerId: mgrId);

        // Colleague is on leave Jan 1 - Jan 5
        await SeedRequest(colleagueId, mgrId, LeaveStatus.Approved, new DateTime(2023, 1, 1), new DateTime(2023, 1, 5), deptId);

        // Check Overlap
        var result = await _leaveService.CheckConflictAsync(myId, new DateTime(2023, 1, 2), new DateTime(2023, 1, 3));

        Assert.True(result.HasConflict);
        Assert.NotNull(result.ConflictingEmployeeName);
        Assert.Contains("Conflict detected", result.Message);

        // Check No Overlap
        var resultNoConflict = await _leaveService.CheckConflictAsync(myId, new DateTime(2023, 2, 1), new DateTime(2023, 2, 5));
        Assert.False(resultNoConflict.HasConflict);
    }

    [Fact]
    public async Task Submit_Overlap_DifferentDepartment_AllowsRequest()
    {
        int userA = 1, userB = 2;
        int deptA = 100, deptB = 200;

        await SeedUser(userA, deptA);

        // Seed Manager for Dept B and assign to userB
        int mgrB = 3;
        await SeedUser(mgrB, deptB, roleId: 1);
        await SeedUser(userB, deptB, roleId: 2, managerId: mgrB);

        await SeedBalance(userB, LeaveType.Annual, 10, 2023);

        await SeedRequest(userA, 99, LeaveStatus.Approved, new DateTime(2023, 1, 1), new DateTime(2023, 1, 5), deptA);

        var dto = new SubmitLeaveRequestDto { StartDate = new DateTime(2023, 1, 1), EndDate = new DateTime(2023, 1, 5), Type = LeaveType.Annual };

        var res = await _leaveService.SubmitRequestAsync(userB, dto);
        Assert.NotNull(res);
    }

    // ==========================================
    // 3. Workflow & Logic
    // ==========================================

    [Fact]
    public async Task Submit_DepartmentHead_GoesDirectlyToHR()
    {
        // Arrange - Department Head is a Manager with no ManagerId
        int departmentHeadId = 500;
        int deptId = 5;
        // RoleId 1 = Manager, no managerId = Department Head
        await SeedUser(departmentHeadId, deptId, roleId: 1);
        await SeedBalance(departmentHeadId, LeaveType.Annual, 20, DateTime.UtcNow.Year);

        // Seed an HR user in the same department to receive notification
        int hrId = 900;
        await SeedUser(hrId, deptId, roleId: 3); // HR must be in same department

        var dto = new SubmitLeaveRequestDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2),
            Type = LeaveType.Annual
        };

        // Act
        var result = await _leaveService.SubmitRequestAsync(departmentHeadId, dto);

        // Assert - Goes directly to HR since no manager
        Assert.Equal(LeaveStatus.PendingHR.ToString(), result.Status);

        // Check Notification sent to HR
        _mockNotificationQueue.Verify(n =>
            n.QueueNotificationAsync(
                hrId,
                It.Is<string>(s => s.Contains("Action Required") || s.Contains("New")),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Submit_ManagerWithManager_GoesToPendingManager()
    {
        // Arrange - Team Lead/Senior Manager has a manager (reports to Department Head)
        int departmentHeadId = 501;
        int teamLeadId = 502;
        int deptId = 5;

        // Department Head - Manager with no manager
        await SeedUser(departmentHeadId, deptId, roleId: 1);
        // Team Lead - Manager with a manager
        await SeedUser(teamLeadId, deptId, roleId: 1, managerId: departmentHeadId);
        await SeedBalance(teamLeadId, LeaveType.Annual, 20, DateTime.UtcNow.Year);

        var dto = new SubmitLeaveRequestDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2),
            Type = LeaveType.Annual
        };

        // Act
        var result = await _leaveService.SubmitRequestAsync(teamLeadId, dto);

        // Assert - Goes to their manager (Department Head), not directly to HR
        Assert.Equal(LeaveStatus.PendingManager.ToString(), result.Status);

        // Check Notification sent to their manager (Department Head)
        _mockNotificationQueue.Verify(n =>
            n.QueueNotificationAsync(
                departmentHeadId,
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelRequest_OwnRequestPending_CancelsSuccessfully()
    {
        // Arrange
        int userId = 600;
        int mgrId = 601;
        await SeedUser(userId, 1);
        await SeedUser(mgrId, 1, roleId: 1);

        int reqId = await SeedRequest(userId, mgrId, LeaveStatus.PendingManager, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 1);

        // Act
        await _leaveService.CancelRequestAsync(userId, reqId);

        // Assert
        var request = await _context.LeaveRequests.FindAsync(reqId);
        Assert.Equal(LeaveStatus.Cancelled, request.Status);

        // Check Audit Log
        var history = await _leaveService.GetRequestHistoryAsync(reqId);
        Assert.Contains(history, h => h.Action == "Cancelled");
    }

    [Fact]
    public async Task CancelRequest_OtherUserRequest_ThrowsUnauthorized()
    {
        // Arrange
        int ownerId = 700;
        int attackerId = 701;
        int mgrId = 702;
        await SeedUser(ownerId, 1);
        await SeedUser(attackerId, 1);
        await SeedUser(mgrId, 1, roleId: 1);

        int reqId = await SeedRequest(ownerId, mgrId, LeaveStatus.PendingManager, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 1);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _leaveService.CancelRequestAsync(attackerId, reqId));
    }

    [Fact]
    public async Task CancelRequest_AlreadyApproved_ThrowsInvalidOperation()
    {
        // Arrange
        int userId = 800;
        int mgrId = 801;
        await SeedUser(userId, 1);

        // Request is already Approved
        int reqId = await SeedRequest(userId, mgrId, LeaveStatus.Approved, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _leaveService.CancelRequestAsync(userId, reqId));
        Assert.Contains("already been processed", ex.Message);
    }

    // ==========================================
    // 4. Notifications & History
    // ==========================================

    [Fact]
    public async Task FullWorkflow_NotificationsAndHistoryAreCreated()
    {
        int empId = 1, mgrId = 2, hrId = 3;

        await SeedUser(mgrId, 1, roleId: 1);
        await SeedUser(empId, 1, roleId: 2, managerId: mgrId);

        await SeedUser(hrId, null, roleId: 3);
        await SeedBalance(empId, LeaveType.Annual, 10, DateTime.UtcNow.Year);

        // 1. Employee Submits -> Manager gets notified
        var dto = new SubmitLeaveRequestDto { StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1), Type = LeaveType.Annual };
        var req = await _leaveService.SubmitRequestAsync(empId, dto);

        // CHANGED: Verify Queue (3 args)
        _mockNotificationQueue.Verify(n =>
            n.QueueNotificationAsync(mgrId, It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);

        // 2. Manager Approves -> Employee gets notified (Approved by Manager)
        await _leaveService.ManagerActionAsync(mgrId, req.Id, new LeaveActionDto { IsApproved = true, Comment = "Manager OK" });

        // CHANGED: Verify Queue (3 args)
        _mockNotificationQueue.Verify(n =>
            n.QueueNotificationAsync(
                empId,
                It.Is<string>(s => s.Contains("Approved by Manager")),
                It.IsAny<string>()),
            Times.Once);

        // 3. HR Approves -> Employee gets notified (Final Approved)
        await _leaveService.HRActionAsync(hrId, req.Id, new LeaveActionDto { IsApproved = true, Comment = "HR OK" });

        // CHANGED: Verify Queue (3 args)
        _mockNotificationQueue.Verify(n =>
            n.QueueNotificationAsync(
                empId,
                It.Is<string>(s => s.Contains("Final Approved")),
                It.IsAny<string>()),
            Times.Once);

        // Check history
        var history = await _leaveService.GetRequestHistoryAsync(req.Id);
        Assert.Equal(3, history.Count());
        Assert.Contains(history, h => h.Action == "Submitted");
        Assert.Contains(history, h => h.Action == "ManagerApproved");
        Assert.Contains(history, h => h.Action == "HRApproved");
    }

    [Fact]
    public async Task HRAction_FinalApproval_DeductsBalance()
    {
        int hrId = 99;
        int empId = 10;
        int currentYear = DateTime.Now.Year;
        await SeedUser(hrId, null);
        await SeedBalance(empId, LeaveType.Sick, 10, currentYear, 0);

        int reqId = await SeedRequest(empId, 5, LeaveStatus.PendingHR, DateTime.Now, DateTime.Now.AddDays(2), 1, type: LeaveType.Sick);

        await _leaveService.HRActionAsync(hrId, reqId, new LeaveActionDto { IsApproved = true });

        var balance = await _context.LeaveBalances.FirstAsync(b => b.EmployeeId == empId && b.Type == LeaveType.Sick && b.Year == currentYear);
        Assert.Equal(3, balance.UsedDays);
        Assert.Equal(7, balance.RemainingDays);
    }

    [Fact]
    public async Task ManagerAction_WithComment_SavesCommentToAuditLog()
    {
        // Arrange
        int mgrId = 55;
        int empId = 5;
        string comment = "Approved but please finish the report first.";
        int reqId = await SeedRequest(empId, mgrId, LeaveStatus.PendingManager, DateTime.Now, DateTime.Now.AddDays(1), 10);

        var actionDto = new LeaveActionDto
        {
            IsApproved = true,
            Comment = comment
        };

        // Act
        await _leaveService.ManagerActionAsync(mgrId, reqId, actionDto);

        // Assert
        var history = await _leaveService.GetRequestHistoryAsync(reqId);
        var managerLog = history.FirstOrDefault(h => h.Action == "ManagerApproved");

        Assert.NotNull(managerLog);
        Assert.Equal(comment, managerLog.Comment);

        // Also verify the leave status updated
        var request = await _context.LeaveRequests.FindAsync(reqId);
        Assert.Equal(LeaveStatus.PendingHR, request.Status);
    }

    [Fact]
    public async Task ManagerAction_WrongUser_ThrowsUnauthorized()
    {
        int mgrId = 50;
        int imposterId = 60;
        int reqId = await SeedRequest(1, mgrId, LeaveStatus.PendingManager, DateTime.Now, DateTime.Now.AddDays(1), 1);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _leaveService.ManagerActionAsync(imposterId, reqId, new LeaveActionDto { IsApproved = true }));
    }

    [Fact]
    public async Task ManagerAction_WrongStatus_ThrowsInvalidOp()
    {
        int mgrId = 50;
        int reqId = await SeedRequest(1, mgrId, LeaveStatus.PendingHR, DateTime.Now, DateTime.Now.AddDays(1), 1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _leaveService.ManagerActionAsync(mgrId, reqId, new LeaveActionDto { IsApproved = true }));

        Assert.Contains("not pending manager", ex.Message.ToLower());
    }

    [Fact]
    public async Task HRAction_WrongStatus_ThrowsInvalidOp()
    {
        int hrId = 200;
        int reqId = await SeedRequest(1, 50, LeaveStatus.PendingManager, DateTime.Now, DateTime.Now.AddDays(1), 1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _leaveService.HRActionAsync(hrId, reqId, new LeaveActionDto { IsApproved = true }));

        Assert.Contains("not pending hr", ex.Message.ToLower());
    }

    // ==========================================
    // 5. Auto-Approval Logic
    // ==========================================

    [Fact]
    public async Task Submit_AutoApproveEnabled_UnderThreshold_ApprovesImmediately()
    {
        int userId = 200;
        // Even if auto-approved, some logic might check manager existence first. 
        // Best to seed it to be safe.
        int mgrId = 999;
        await SeedUser(mgrId, 1, roleId: 1);
        await SeedUser(userId, 1, roleId: 2, managerId: mgrId);

        await SeedBalance(userId, LeaveType.Sick, 10, DateTime.UtcNow.Year, 0);
        await SeedLeaveSettings(LeaveType.Sick, autoApprove: true, threshold: 3);

        var dto = new SubmitLeaveRequestDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Type = LeaveType.Sick
        };

        var result = await _leaveService.SubmitRequestAsync(userId, dto);

        Assert.Equal(LeaveStatus.Approved.ToString(), result.Status);

        var history = await _leaveService.GetRequestHistoryAsync(result.Id);
        Assert.Contains(history, h => h.Action == "HRApproved" && h.Comment.Contains("Auto-approved"));
    }

    [Fact]
    public async Task Submit_AutoApproveEnabled_OverThreshold_GoesToPendingManager()
    {
        // Arrange
        int userId = 201;
        int deptId = 1;
        int mgrId = 202;
        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(userId, deptId, roleId: 2, managerId: mgrId);

        await SeedBalance(userId, LeaveType.Sick, 10, DateTime.UtcNow.Year, 0);

        // Policy: Auto-approve Casual leave if <= 3 days
        await SeedLeaveSettings(LeaveType.Sick, autoApprove: true, threshold: 3);

        var dto = new SubmitLeaveRequestDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(4), // 5 days duration (Over threshold)
            Type = LeaveType.Sick
        };

        // Act
        var result = await _leaveService.SubmitRequestAsync(userId, dto);

        // Assert
        Assert.Equal(LeaveStatus.PendingManager.ToString(), result.Status);
    }

    [Fact]
    public async Task Submit_AutoApproveDisabled_UnderThreshold_GoesToPendingManager()
    {
        // Arrange
        int userId = 203;
        int deptId = 1;
        int mgrId = 204;
        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(userId, deptId, roleId: 2, managerId: mgrId);

        await SeedBalance(userId, LeaveType.Sick, 10, DateTime.UtcNow.Year, 0);

        // Policy: Disabled, even though threshold is high
        await SeedLeaveSettings(LeaveType.Sick, autoApprove: false, threshold: 10);

        var dto = new SubmitLeaveRequestDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1), // 2 days duration
            Type = LeaveType.Sick
        };

        // Act
        var result = await _leaveService.SubmitRequestAsync(userId, dto);

        // Assert
        Assert.Equal(LeaveStatus.PendingManager.ToString(), result.Status);
    }

    [Fact]
    public async Task Submit_AutoApprove_DeductsBalanceImmediately()
    {
        // Arrange
        int userId = 205;
        // Seed manager just in case
        await SeedUser(999, 1, roleId: 1);
        await SeedUser(userId, 1, roleId: 2, managerId: 999);

        await SeedBalance(userId, LeaveType.Annual, 20, DateTime.UtcNow.Year, 0); // Balance: 20 Remaining

        await SeedLeaveSettings(LeaveType.Annual, autoApprove: true, threshold: 5);

        var dto = new SubmitLeaveRequestDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2), // 3 days
            Type = LeaveType.Annual
        };

        // Act
        await _leaveService.SubmitRequestAsync(userId, dto);

        // Assert
        var balance = await _context.LeaveBalances
            .FirstAsync(b => b.EmployeeId == userId && b.Type == LeaveType.Annual);

        // Should be deducted immediately, unlike normal flow which waits for HR
        Assert.Equal(3, balance.UsedDays);
        Assert.Equal(17, balance.RemainingDays);
    }

    // ==========================================
    // 6. Department Coverage (Corrected)
    // ==========================================

    [Fact]
    public async Task GetDepartmentCoverage_Manager_ReturnsOwnDeptOnly()
    {
        // Arrange
        int managerId = 1001;
        int deptA = 10;
        int deptB = 20;

        await SeedUser(managerId, deptA, roleId: 1);
        await SeedUser(1002, deptA, roleId: 2);
        await SeedUser(1003, deptA, roleId: 2);
        await SeedUser(1004, deptB, roleId: 2);

        // FIX: Use DateTime.UtcNow.Date to ensure StartDate is Midnight (00:00:00)
        // Old: await SeedRequest(1002, managerId, LeaveStatus.Approved, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), deptA);
        var today = DateTime.UtcNow.Date;
        await SeedRequest(1002, managerId, LeaveStatus.Approved, today, today.AddDays(1), deptA);

        // Act
        var result = await _leaveService.GetDepartmentCoverageAsync(managerId, DateTime.UtcNow);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var deptStats = result.First();
        Assert.Equal(deptA, deptStats.DepartmentId);
        Assert.Equal(3, deptStats.TotalEmployees);
        Assert.Equal(1, deptStats.OnLeaveCount); // This will now pass
        Assert.Equal(2, deptStats.AvailableCount);
        Assert.Equal(66.7, deptStats.CapacityPercentage);
    }

    [Fact]
    public async Task GetDepartmentCoverage_HR_ReturnsAllDepts()
    {
        // Arrange
        int hrId = 2001;
        int deptA = 30;
        int deptB = 40;

        await SeedUser(hrId, null, roleId: 3); // HR (No Dept)
        await SeedUser(2002, deptA, roleId: 2);
        await SeedUser(2003, deptB, roleId: 2);

        // Act
        var result = await _leaveService.GetDepartmentCoverageAsync(hrId, DateTime.UtcNow);

        // Assert
        Assert.Equal(2, result.Count); // Dept A & Dept B
        Assert.Contains(result, r => r.DepartmentId == deptA);
        Assert.Contains(result, r => r.DepartmentId == deptB);
    }

    [Fact]
    public async Task GetDepartmentCoverage_Employee_ThrowsUnauthorized()
    {
        int empId = 3001;
        await SeedUser(empId, 1, roleId: 2); // Regular Employee

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _leaveService.GetDepartmentCoverageAsync(empId, DateTime.UtcNow));
    }

    // ==========================================
    // 7. Balance Management - GetUsersWithoutBalances
    // ==========================================

    [Fact]
    public async Task GetUsersWithoutBalances_ReturnsOnlyUsersWithoutBalances()
    {
        // Arrange
        int userWithBalance = 4001;
        int userWithoutBalance1 = 4002;
        int userWithoutBalance2 = 4003;
        int currentYear = DateTime.UtcNow.Year;

        await SeedUser(userWithBalance, 1);
        await SeedUser(userWithoutBalance1, 1);
        await SeedUser(userWithoutBalance2, 1);

        await SeedBalance(userWithBalance, LeaveType.Annual, 21, currentYear);

        // Act
        var result = await _leaveService.GetUsersWithoutBalancesAsync(new PaginationDto { PageNumber = 1, PageSize = 50 });

        // Assert
        Assert.Equal(2, result.Items.Count());
        Assert.Contains(result.Items, u => u.Id == userWithoutBalance1);
        Assert.Contains(result.Items, u => u.Id == userWithoutBalance2);
        Assert.DoesNotContain(result.Items, u => u.Id == userWithBalance);
    }

    [Fact]
    public async Task GetUsersWithoutBalances_Paginated_ReturnsCorrectPage()
    {
        // Arrange - Create multiple users without balances
        for (int i = 5001; i <= 5010; i++)
        {
            await SeedUser(i, 1);
        }

        // Act
        var page1 = await _leaveService.GetUsersWithoutBalancesAsync(new PaginationDto { PageNumber = 1, PageSize = 3 });
        var page2 = await _leaveService.GetUsersWithoutBalancesAsync(new PaginationDto { PageNumber = 2, PageSize = 3 });

        // Assert
        Assert.Equal(3, page1.Items.Count());
        Assert.Equal(3, page2.Items.Count());
        Assert.Equal(10, page1.TotalCount);
        Assert.Equal(4, page1.TotalPages);
    }

    // ==========================================
    // 8. Balance Management - InitializeBalances
    // ==========================================

    [Fact]
    public async Task InitializeBalances_CreatesBalancesForSelectedUsers()
    {
        // Arrange
        int userId1 = 6001;
        int userId2 = 6002;
        await SeedUser(userId1, 1);
        await SeedUser(userId2, 1);

        // Seed leave type config for default balance
        await SeedLeaveSettings(LeaveType.Annual, false, 0);

        // Act
        var request = new InitializeBalancesRequestDto { UserIds = new List<int> { userId1, userId2 } };
        var result = await _leaveService.InitializeBalancesAsync(request);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("initialized", result.Message.ToLower());

        var balances = await _context.LeaveBalances.Where(b => b.EmployeeId == userId1 || b.EmployeeId == userId2).ToListAsync();
        Assert.True(balances.Count >= 2); // At least one balance per user
    }

    [Fact]
    public async Task InitializeBalances_EmptyUserIds_ReturnsZeroCount()
    {
        // Arrange
        var request = new InitializeBalancesRequestDto { UserIds = new List<int>() };

        // Act
        var result = await _leaveService.InitializeBalancesAsync(request);

        // Assert
        Assert.Equal(0, result.Count);
        Assert.Contains("no users", result.Message.ToLower());
    }

    [Fact]
    public async Task InitializeBalances_DoesNotDuplicateExistingBalances()
    {
        // Arrange
        int userId = 6010;
        int currentYear = DateTime.UtcNow.Year;
        await SeedUser(userId, 1);
        await SeedBalance(userId, LeaveType.Annual, 21, currentYear);

        int countBefore = await _context.LeaveBalances.CountAsync(b => b.EmployeeId == userId);

        // Act
        var request = new InitializeBalancesRequestDto { UserIds = new List<int> { userId } };
        await _leaveService.InitializeBalancesAsync(request);

        // Assert - should only add missing types, not duplicate Annual
        var annualBalances = await _context.LeaveBalances.CountAsync(b => b.EmployeeId == userId && b.Type == LeaveType.Annual);
        Assert.Equal(1, annualBalances);
    }

    // ==========================================
    // 9. Balance Management - GetAllUsersWithBalances
    // ==========================================

    [Fact]
    public async Task GetAllUsersWithBalances_ReturnsAllLeaveTypesPerUser()
    {
        // Arrange
        int userId = 7001;
        int currentYear = DateTime.UtcNow.Year;
        await SeedUser(userId, 1);
        await SeedBalance(userId, LeaveType.Annual, 21, currentYear, 5);
        await SeedBalance(userId, LeaveType.Sick, 7, currentYear, 2);

        // Act
        var result = await _leaveService.GetAllUsersWithBalancesAsync(new PaginationDto { PageNumber = 1, PageSize = 50 });

        // Assert
        var userBalance = result.Items.FirstOrDefault(u => u.UserId == userId);
        Assert.NotNull(userBalance);
        Assert.Equal(6, userBalance.Balances.Count); // All leave types should be present

        var annualBalance = userBalance.Balances.First(b => b.LeaveTypeId == (int)LeaveType.Annual);
        Assert.Equal(21, annualBalance.TotalDays);
        Assert.Equal(5, annualBalance.UsedDays);
        Assert.Equal(16, annualBalance.RemainingDays);
    }

    [Fact]
    public async Task GetAllUsersWithBalances_Paginated_ReturnsCorrectData()
    {
        // Arrange
        for (int i = 7100; i <= 7105; i++)
        {
            await SeedUser(i, 1);
        }

        // Act
        var result = await _leaveService.GetAllUsersWithBalancesAsync(new PaginationDto { PageNumber = 1, PageSize = 3 });

        // Assert
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(6, result.TotalCount);
    }

    // ==========================================
    // 10. Balance Management - UpdateBalances
    // ==========================================

    [Fact]
    public async Task UpdateBalances_UpdatesExistingBalances()
    {
        // Arrange
        int userId = 8001;
        int currentYear = DateTime.UtcNow.Year;
        await SeedUser(userId, 1);
        await SeedBalance(userId, LeaveType.Annual, 21, currentYear, 5);

        // Act
        var request = new UpdateBalancesRequestDto
        {
            UserIds = new List<int> { userId },
            Updates = new List<BalanceUpdateItem>
            {
                new BalanceUpdateItem { LeaveTypeId = (int)LeaveType.Annual, NewTotalDays = 30 }
            }
        };
        var result = await _leaveService.UpdateBalancesAsync(request);

        // Assert
        Assert.Equal(1, result.UsersUpdated);
        var balance = await _context.LeaveBalances.FirstAsync(b => b.EmployeeId == userId && b.Type == LeaveType.Annual);
        Assert.Equal(30, balance.TotalDays);
        Assert.Equal(5, balance.UsedDays); // UsedDays should be preserved
    }

    [Fact]
    public async Task UpdateBalances_CreatesNewBalanceIfNotExists()
    {
        // Arrange
        int userId = 8010;
        await SeedUser(userId, 1);
        // No balance seeded

        // Act
        var request = new UpdateBalancesRequestDto
        {
            UserIds = new List<int> { userId },
            Updates = new List<BalanceUpdateItem>
            {
                new BalanceUpdateItem { LeaveTypeId = (int)LeaveType.Annual, NewTotalDays = 25 }
            }
        };
        var result = await _leaveService.UpdateBalancesAsync(request);

        // Assert
        var balance = await _context.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == userId && b.Type == LeaveType.Annual);
        Assert.NotNull(balance);
        Assert.Equal(25, balance.TotalDays);
        Assert.Equal(0, balance.UsedDays);
    }

    [Fact]
    public async Task UpdateBalances_EmptyRequest_ReturnsZero()
    {
        // Arrange
        var request = new UpdateBalancesRequestDto { UserIds = new List<int>(), Updates = new List<BalanceUpdateItem>() };

        // Act
        var result = await _leaveService.UpdateBalancesAsync(request);

        // Assert
        Assert.Equal(0, result.UsersUpdated);
    }

    // ==========================================
    // 11. Pending Approval Count
    // ==========================================

    [Fact]
    public async Task GetPendingApprovalCount_Manager_ReturnsOnlyTheirRequests()
    {
        // Arrange
        int managerId = 9001;
        int otherManagerId = 9002;
        await SeedUser(managerId, 1, roleId: 1);
        await SeedUser(otherManagerId, 1, roleId: 1);

        // Seed requests for this manager
        await SeedRequest(9010, managerId, LeaveStatus.PendingManager, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        await SeedRequest(9011, managerId, LeaveStatus.PendingManager, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 1);

        // Seed request for other manager
        await SeedRequest(9012, otherManagerId, LeaveStatus.PendingManager, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);

        // Act
        var result = await _leaveService.GetPendingApprovalCountAsync(managerId);

        // Assert
        Assert.Equal(2, result.PendingManagerApproval);
        Assert.Equal(0, result.PendingHRApproval);
        Assert.Equal(2, result.TotalPending);
    }

    [Fact]
    public async Task GetPendingApprovalCount_HR_ReturnsOnlyDepartmentPendingHR()
    {
        // Arrange - HR sees only requests from their department
        int hrDeptId = 905;
        int hrId = 9050;
        int mgrId = 9055; // Manager in same department as HR
        
        await SeedUser(hrId, hrDeptId, roleId: 3); // HR with department
        await SeedUser(mgrId, hrDeptId, roleId: 1); // Manager in same department

        // Seed PendingHR requests from HR's department (manager has same deptId)
        await SeedRequest(9060, mgrId, LeaveStatus.PendingHR, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), hrDeptId);
        await SeedRequest(9061, mgrId, LeaveStatus.PendingHR, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), hrDeptId);
        await SeedRequest(9062, mgrId, LeaveStatus.PendingHR, DateTime.UtcNow, DateTime.UtcNow.AddDays(3), hrDeptId);

        // Seed a request from different department (should NOT be counted)
        int otherMgrId = 9099;
        await SeedUser(otherMgrId, 999, roleId: 1); // Manager in different department
        await SeedRequest(9063, otherMgrId, LeaveStatus.PendingHR, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 999);

        // Act
        var result = await _leaveService.GetPendingApprovalCountAsync(hrId);

        // Assert - Should only count the 3 from HR's department, not the 1 from other department
        Assert.Equal(0, result.PendingManagerApproval);
        Assert.Equal(3, result.PendingHRApproval);
        Assert.Equal(3, result.TotalPending);
    }

    [Fact]
    public async Task GetPendingApprovalCount_UserNotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _leaveService.GetPendingApprovalCountAsync(99999)); // Non-existent user
    }

    // ==========================================
    // 12. Year Parameter Tests
    // ==========================================

    [Fact]
    public async Task GetUsersWithoutBalances_WithYear_FiltersCorrectly()
    {
        // Arrange
        int userWithBalance2025 = 10001;
        int userWithBalance2026 = 10002;
        int userWithoutBalance = 10003;

        await SeedUser(userWithBalance2025, 1);
        await SeedUser(userWithBalance2026, 1);
        await SeedUser(userWithoutBalance, 1);

        await SeedBalance(userWithBalance2025, LeaveType.Annual, 21, 2025);
        await SeedBalance(userWithBalance2026, LeaveType.Annual, 21, 2026);

        // Act - Check for 2026
        var result2026 = await _leaveService.GetUsersWithoutBalancesAsync(new PaginationDto { PageNumber = 1, PageSize = 50 }, 2026);

        // Assert - userWithBalance2025 and userWithoutBalance should be in list (no 2026 balance)
        Assert.Contains(result2026.Items, u => u.Id == userWithBalance2025);
        Assert.Contains(result2026.Items, u => u.Id == userWithoutBalance);
        Assert.DoesNotContain(result2026.Items, u => u.Id == userWithBalance2026);
    }

    [Fact]
    public async Task GetAllUsersWithBalances_WithYear_ReturnsCorrectYear()
    {
        // Arrange
        int userId = 10010;
        await SeedUser(userId, 1);
        await SeedBalance(userId, LeaveType.Annual, 21, 2025, 5);
        await SeedBalance(userId, LeaveType.Annual, 25, 2026, 0);

        // Act - Get 2026 balances
        var result = await _leaveService.GetAllUsersWithBalancesAsync(new PaginationDto { PageNumber = 1, PageSize = 50 }, 2026);

        // Assert
        var userBalance = result.Items.FirstOrDefault(u => u.UserId == userId);
        Assert.NotNull(userBalance);
        var annualBalance = userBalance.Balances.First(b => b.LeaveTypeId == (int)LeaveType.Annual);
        Assert.Equal(25, annualBalance.TotalDays); // 2026 balance, not 2025
        Assert.Equal(0, annualBalance.UsedDays);
    }

    [Fact]
    public async Task InitializeBalances_WithYear_CreatesForSpecifiedYear()
    {
        // Arrange
        int userId = 10020;
        await SeedUser(userId, 1);
        await SeedLeaveSettings(LeaveType.Annual, false, 0);

        // Act - Initialize for 2026
        var request = new InitializeBalancesRequestDto { UserIds = new List<int> { userId }, Year = 2026 };
        var result = await _leaveService.InitializeBalancesAsync(request);

        // Assert
        Assert.Contains("2026", result.Message);
        var balance = await _context.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == userId && b.Year == 2026);
        Assert.NotNull(balance);
    }

    [Fact]
    public async Task InitializeBalances_WithoutYear_DefaultsToCurrentYear()
    {
        // Arrange
        int userId = 10021;
        int currentYear = DateTime.UtcNow.Year;
        await SeedUser(userId, 1);

        // Act - Initialize without specifying year
        var request = new InitializeBalancesRequestDto { UserIds = new List<int> { userId } };
        await _leaveService.InitializeBalancesAsync(request);

        // Assert - Should create balance for current year
        var balance = await _context.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == userId && b.Year == currentYear);
        Assert.NotNull(balance);
    }

    [Fact]
    public async Task UpdateBalances_WithYear_UpdatesCorrectYear()
    {
        // Arrange
        int userId = 10030;
        await SeedUser(userId, 1);
        await SeedBalance(userId, LeaveType.Annual, 21, 2025, 5);
        await SeedBalance(userId, LeaveType.Annual, 21, 2026, 0);

        // Act - Update 2026 only
        var request = new UpdateBalancesRequestDto
        {
            UserIds = new List<int> { userId },
            Year = 2026,
            Updates = new List<BalanceUpdateItem>
            {
                new BalanceUpdateItem { LeaveTypeId = (int)LeaveType.Annual, NewTotalDays = 30 }
            }
        };
        await _leaveService.UpdateBalancesAsync(request);

        // Assert
        var balance2025 = await _context.LeaveBalances.FirstAsync(b => b.EmployeeId == userId && b.Year == 2025 && b.Type == LeaveType.Annual);
        var balance2026 = await _context.LeaveBalances.FirstAsync(b => b.EmployeeId == userId && b.Year == 2026 && b.Type == LeaveType.Annual);

        Assert.Equal(21, balance2025.TotalDays); // Unchanged
        Assert.Equal(30, balance2026.TotalDays); // Updated
    }

    [Fact]
    public async Task UpdateBalances_WithYear_CreatesNewBalanceForSpecifiedYear()
    {
        // Arrange
        int userId = 10031;
        await SeedUser(userId, 1);
        // No balance for 2026

        // Act - Update for 2026 (should create)
        var request = new UpdateBalancesRequestDto
        {
            UserIds = new List<int> { userId },
            Year = 2026,
            Updates = new List<BalanceUpdateItem>
            {
                new BalanceUpdateItem { LeaveTypeId = (int)LeaveType.Annual, NewTotalDays = 25 }
            }
        };
        await _leaveService.UpdateBalancesAsync(request);

        // Assert - Should create new balance for 2026
        var balance = await _context.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == userId && b.Year == 2026 && b.Type == LeaveType.Annual);
        Assert.NotNull(balance);
        Assert.Equal(25, balance.TotalDays);
    }

    // ==========================================
    // 13. Conflict Logic Tests
    // ==========================================

    [Fact]
    public async Task CheckConflict_PendingLeave_NoConflict()
    {
        // Arrange - Two users with same manager
        int mgrId = 11001;
        int user1 = 11002;
        int user2 = 11003;
        
        await SeedUser(mgrId, 1, roleId: 1);
        await SeedUser(user1, 1, roleId: 2, managerId: mgrId);
        await SeedUser(user2, 1, roleId: 2, managerId: mgrId);

        // User1 has a pending request
        await SeedRequest(user1, mgrId, LeaveStatus.PendingManager, new DateTime(2025, 6, 1), new DateTime(2025, 6, 5), 1);

        // Act - User2 checks for conflict on overlapping dates
        var result = await _leaveService.CheckConflictAsync(user2, new DateTime(2025, 6, 2), new DateTime(2025, 6, 4));

        // Assert - No conflict because user1's leave is only pending
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task CheckConflict_ApprovedLeave_HasConflict()
    {
        // Arrange - Two users with same manager
        int mgrId = 11010;
        int user1 = 11011;
        int user2 = 11012;
        
        await SeedUser(mgrId, 1, roleId: 1);
        await SeedUser(user1, 1, roleId: 2, managerId: mgrId);
        await SeedUser(user2, 1, roleId: 2, managerId: mgrId);

        // User1 has an APPROVED request
        await SeedRequest(user1, mgrId, LeaveStatus.Approved, new DateTime(2025, 6, 1), new DateTime(2025, 6, 5), 1);

        // Act - User2 checks for conflict on overlapping dates
        var result = await _leaveService.CheckConflictAsync(user2, new DateTime(2025, 6, 2), new DateTime(2025, 6, 4));

        // Assert - Conflict because user1's leave is approved
        Assert.True(result.HasConflict);
        Assert.Contains("approved", result.Message.ToLower());
    }

    [Fact]
    public async Task ManagerAction_Approve_BlockedByPendingHR()
    {
        // Arrange - Two pending requests from different users, same manager
        int mgrId = 11020;
        int user1 = 11021;
        int user2 = 11022;
        int hrId = 11023;

        await SeedUser(mgrId, 1, roleId: 1);
        await SeedUser(user1, 1, roleId: 2, managerId: mgrId);
        await SeedUser(user2, 1, roleId: 2, managerId: mgrId);
        await SeedUser(hrId, null, roleId: 3);

        // Request 1 - already approved by manager (PendingHR)
        await SeedRequest(user1, mgrId, LeaveStatus.PendingHR, new DateTime(2025, 7, 1), new DateTime(2025, 7, 5), 1);
        
        // Request 2 - still pending manager
        int req2Id = await SeedRequest(user2, mgrId, LeaveStatus.PendingManager, new DateTime(2025, 7, 2), new DateTime(2025, 7, 4), 1);

        // Act & Assert - Manager tries to approve request 2, should fail
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _leaveService.ManagerActionAsync(mgrId, req2Id, new LeaveActionDto { IsApproved = true }));
        
        Assert.Contains("pending hr approval or approved", ex.Message.ToLower());
    }

    [Fact]
    public async Task ManagerAction_Approve_BlockedByApproved()
    {
        // Arrange
        int mgrId = 11030;
        int user1 = 11031;
        int user2 = 11032;

        await SeedUser(mgrId, 1, roleId: 1);
        await SeedUser(user1, 1, roleId: 2, managerId: mgrId);
        await SeedUser(user2, 1, roleId: 2, managerId: mgrId);

        // Request 1 - fully approved
        await SeedRequest(user1, mgrId, LeaveStatus.Approved, new DateTime(2025, 8, 1), new DateTime(2025, 8, 5), 1);
        
        // Request 2 - pending manager
        int req2Id = await SeedRequest(user2, mgrId, LeaveStatus.PendingManager, new DateTime(2025, 8, 2), new DateTime(2025, 8, 4), 1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _leaveService.ManagerActionAsync(mgrId, req2Id, new LeaveActionDto { IsApproved = true }));
        
        Assert.Contains("approved", ex.Message.ToLower());
    }

    [Fact]
    public async Task ManagerAction_Reject_NoConflictCheck()
    {
        // Arrange - Manager can reject even if there's a conflict
        int mgrId = 11040;
        int user1 = 11041;
        int user2 = 11042;

        await SeedUser(mgrId, 1, roleId: 1);
        await SeedUser(user1, 1, roleId: 2, managerId: mgrId);
        await SeedUser(user2, 1, roleId: 2, managerId: mgrId);

        await SeedRequest(user1, mgrId, LeaveStatus.Approved, new DateTime(2025, 9, 1), new DateTime(2025, 9, 5), 1);
        int req2Id = await SeedRequest(user2, mgrId, LeaveStatus.PendingManager, new DateTime(2025, 9, 2), new DateTime(2025, 9, 4), 1);

        // Act - Manager rejects (should work even with conflict)
        await _leaveService.ManagerActionAsync(mgrId, req2Id, new LeaveActionDto { IsApproved = false });

        // Assert
        var request = await _context.LeaveRequests.FindAsync(req2Id);
        Assert.Equal(LeaveStatus.Rejected, request!.Status);
    }

    [Fact]
    public async Task HRAction_Approve_BlockedByApproved()
    {
        // Arrange - Two requests both at PendingHR, HR tries to approve second one
        int mgrId = 11050;
        int user1 = 11051;
        int user2 = 11052;
        int hrId = 11053;
        int currentYear = DateTime.UtcNow.Year;

        await SeedUser(mgrId, 1, roleId: 1);
        await SeedUser(user1, 1, roleId: 2, managerId: mgrId);
        await SeedUser(user2, 1, roleId: 2, managerId: mgrId);
        await SeedUser(hrId, null, roleId: 3);

        await SeedBalance(user2, LeaveType.Annual, 20, currentYear);

        // Request 1 - already approved
        await SeedRequest(user1, mgrId, LeaveStatus.Approved, new DateTime(currentYear, 10, 1), new DateTime(currentYear, 10, 5), 1);
        
        // Request 2 - pending HR
        int req2Id = await SeedRequest(user2, mgrId, LeaveStatus.PendingHR, new DateTime(currentYear, 10, 2), new DateTime(currentYear, 10, 4), 1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _leaveService.HRActionAsync(hrId, req2Id, new LeaveActionDto { IsApproved = true }));
        
        Assert.Contains("approved", ex.Message.ToLower());
    }

    [Fact]
    public async Task HRAction_Approve_NoConflict_Succeeds()
    {
        // Arrange - Request with no conflicts
        int mgrId = 11060;
        int userId = 11061;
        int hrId = 11062;
        int currentYear = DateTime.UtcNow.Year;

        await SeedUser(mgrId, 1, roleId: 1);
        await SeedUser(userId, 1, roleId: 2, managerId: mgrId);
        await SeedUser(hrId, null, roleId: 3);

        await SeedBalance(userId, LeaveType.Annual, 20, currentYear);

        int reqId = await SeedRequest(userId, mgrId, LeaveStatus.PendingHR, new DateTime(currentYear, 11, 1), new DateTime(currentYear, 11, 3), 1);

        // Act
        await _leaveService.HRActionAsync(hrId, reqId, new LeaveActionDto { IsApproved = true });

        // Assert
        var request = await _context.LeaveRequests.FindAsync(reqId);
        Assert.Equal(LeaveStatus.Approved, request!.Status);
    }

    // ==========================================
    // 11. Audit Log Download Tests
    // ==========================================

    [Fact]
    public async Task GetAuditLogsForDownload_Manager_ReturnsOnlyOwnApprovals()
    {
        // Arrange
        int mgrId = 12001;
        int empId = 12002;
        int deptId = 120;

        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(empId, deptId, roleId: 2, managerId: mgrId);

        // Create a request and have manager approve it
        int reqId = await SeedRequest(empId, mgrId, LeaveStatus.PendingManager, DateTime.Now, DateTime.Now.AddDays(2), deptId);
        await _leaveService.ManagerActionAsync(mgrId, reqId, new LeaveActionDto { IsApproved = true, Comment = "Approved for vacation" });

        // Act
        var result = await _leaveService.GetAuditLogsForDownloadAsync(mgrId);

        // Assert
        Assert.NotNull(result);
        var logs = result.ToList();
        Assert.Single(logs);
        Assert.Equal("ManagerApproved", logs[0].ActionTaken);
        Assert.Equal("Approved for vacation", logs[0].Comment);
    }

    [Fact]
    public async Task GetAuditLogsForDownload_Manager_IncludesOwnRejections()
    {
        // Arrange
        int mgrId = 12010;
        int empId = 12011;
        int deptId = 121;

        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(empId, deptId, roleId: 2, managerId: mgrId);

        // Create a request and have manager reject it
        int reqId = await SeedRequest(empId, mgrId, LeaveStatus.PendingManager, DateTime.Now, DateTime.Now.AddDays(2), deptId);
        await _leaveService.ManagerActionAsync(mgrId, reqId, new LeaveActionDto { IsApproved = false, Comment = "Busy period" });

        // Act
        var result = await _leaveService.GetAuditLogsForDownloadAsync(mgrId);

        // Assert
        Assert.NotNull(result);
        var logs = result.ToList();
        Assert.Single(logs);
        Assert.Equal("ManagerRejected", logs[0].ActionTaken);
        Assert.Equal("Busy period", logs[0].Comment);
    }

    [Fact]
    public async Task GetAuditLogsForDownload_Manager_ExcludesOtherManagerActions()
    {
        // Arrange
        int mgr1Id = 12020;
        int mgr2Id = 12021;
        int emp1Id = 12022;
        int emp2Id = 12023;
        int deptId = 122;

        await SeedUser(mgr1Id, deptId, roleId: 1);
        await SeedUser(mgr2Id, deptId, roleId: 1);
        await SeedUser(emp1Id, deptId, roleId: 2, managerId: mgr1Id);
        await SeedUser(emp2Id, deptId, roleId: 2, managerId: mgr2Id);

        // Create requests and have different managers approve them
        int req1Id = await SeedRequest(emp1Id, mgr1Id, LeaveStatus.PendingManager, DateTime.Now, DateTime.Now.AddDays(2), deptId);
        int req2Id = await SeedRequest(emp2Id, mgr2Id, LeaveStatus.PendingManager, DateTime.Now, DateTime.Now.AddDays(3), deptId);
        
        await _leaveService.ManagerActionAsync(mgr1Id, req1Id, new LeaveActionDto { IsApproved = true });
        await _leaveService.ManagerActionAsync(mgr2Id, req2Id, new LeaveActionDto { IsApproved = true });

        // Act - Get logs for mgr1 only
        var result = await _leaveService.GetAuditLogsForDownloadAsync(mgr1Id);

        // Assert
        Assert.NotNull(result);
        var logs = result.ToList();
        Assert.Single(logs); // Only mgr1's action
        Assert.Equal(req1Id, logs[0].RequestId);
    }

    [Fact]
    public async Task GetAuditLogsForDownload_HR_ReturnsOnlyOwnApprovals()
    {
        // Arrange
        int hrId = 12030;
        int mgrId = 12031;
        int empId = 12032;
        int deptId = 123;
        int currentYear = DateTime.UtcNow.Year;

        await SeedUser(hrId, null, roleId: 3);
        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(empId, deptId, roleId: 2, managerId: mgrId);
        await SeedBalance(empId, LeaveType.Annual, 20, currentYear);

        // Create a request at PendingHR and have HR approve it
        int reqId = await SeedRequest(empId, mgrId, LeaveStatus.PendingHR, new DateTime(currentYear, 12, 1), new DateTime(currentYear, 12, 3), deptId);
        await _leaveService.HRActionAsync(hrId, reqId, new LeaveActionDto { IsApproved = true, Comment = "All good" });

        // Act
        var result = await _leaveService.GetAuditLogsForDownloadAsync(hrId);

        // Assert
        Assert.NotNull(result);
        var logs = result.ToList();
        Assert.Single(logs);
        Assert.Equal("HRApproved", logs[0].ActionTaken);
        Assert.Equal("All good", logs[0].Comment);
    }

    [Fact]
    public async Task GetAuditLogsForDownload_HR_GroupedByDepartmentThenManager()
    {
        // Arrange
        int hrId = 12040;
        int mgr1Id = 12041;
        int mgr2Id = 12042;
        int emp1Id = 12043;
        int emp2Id = 12044;
        int dept1 = 124;
        int dept2 = 125;
        int currentYear = DateTime.UtcNow.Year;

        await SeedUser(hrId, null, roleId: 3);
        await SeedUser(mgr1Id, dept1, roleId: 1);
        await SeedUser(mgr2Id, dept2, roleId: 1);
        await SeedUser(emp1Id, dept1, roleId: 2, managerId: mgr1Id);
        await SeedUser(emp2Id, dept2, roleId: 2, managerId: mgr2Id);
        
        await SeedBalance(emp1Id, LeaveType.Annual, 20, currentYear);
        await SeedBalance(emp2Id, LeaveType.Annual, 20, currentYear);

        // Create requests and have HR approve them (dept2 first, then dept1)
        int req2Id = await SeedRequest(emp2Id, mgr2Id, LeaveStatus.PendingHR, new DateTime(currentYear, 12, 5), new DateTime(currentYear, 12, 7), dept2);
        int req1Id = await SeedRequest(emp1Id, mgr1Id, LeaveStatus.PendingHR, new DateTime(currentYear, 12, 10), new DateTime(currentYear, 12, 12), dept1);
        
        await _leaveService.HRActionAsync(hrId, req2Id, new LeaveActionDto { IsApproved = true });
        await _leaveService.HRActionAsync(hrId, req1Id, new LeaveActionDto { IsApproved = true });

        // Act
        var result = await _leaveService.GetAuditLogsForDownloadAsync(hrId);

        // Assert
        Assert.NotNull(result);
        var logs = result.ToList();
        Assert.Equal(2, logs.Count);
        
        // Should be grouped by department (dept1=124 comes before dept2=125)
        Assert.Equal(dept1.ToString(), logs[0].DepartmentName);
        Assert.Equal(dept2.ToString(), logs[1].DepartmentName);
    }

    [Fact]
    public async Task GetAuditLogsForDownload_Employee_ThrowsUnauthorized()
    {
        // Arrange
        int empId = 12050;
        await SeedUser(empId, 1, roleId: 2);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _leaveService.GetAuditLogsForDownloadAsync(empId));
    }

    // ==========================================
    // 12. BypassConflictCheck Tests
    // ==========================================

    [Fact]
    public async Task Submit_AutoApprove_WithBypassConflict_IgnoresConflict()
    {
        // Arrange - Two employees under same manager, one already approved
        int mgrId = 13001;
        int emp1Id = 13002;
        int emp2Id = 13003;
        int deptId = 130;
        int currentYear = DateTime.UtcNow.Year;

        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(emp1Id, deptId, roleId: 2, managerId: mgrId);
        await SeedUser(emp2Id, deptId, roleId: 2, managerId: mgrId);

        await SeedBalance(emp2Id, LeaveType.Emergency, 10, currentYear);

        // Enable auto-approve with bypass for Emergency leave
        await SeedLeaveSettings(LeaveType.Emergency, autoApprove: true, threshold: 5, bypassConflictCheck: true);

        // Emp1 already has approved leave (would normally conflict)
        var today = DateTime.UtcNow.Date;
        await SeedRequest(emp1Id, mgrId, LeaveStatus.Approved, today, today.AddDays(2), deptId, LeaveType.Annual);

        // Act - Emp2 submits Emergency leave for same dates (should bypass conflict)
        var dto = new SubmitLeaveRequestDto
        {
            StartDate = today,
            EndDate = today.AddDays(1), // 2 days - within threshold
            Type = LeaveType.Emergency
        };

        var result = await _leaveService.SubmitRequestAsync(emp2Id, dto);

        // Assert - Should succeed and be auto-approved despite conflict
        Assert.Equal(LeaveStatus.Approved.ToString(), result.Status);
    }

    [Fact]
    public async Task Submit_AutoApprove_WithoutBypassConflict_RespectsConflict()
    {
        // Arrange - Same setup but bypass is disabled
        int mgrId = 13010;
        int emp1Id = 13011;
        int emp2Id = 13012;
        int deptId = 131;
        int currentYear = DateTime.UtcNow.Year;

        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(emp1Id, deptId, roleId: 2, managerId: mgrId);
        await SeedUser(emp2Id, deptId, roleId: 2, managerId: mgrId);

        await SeedBalance(emp2Id, LeaveType.Sick, 10, currentYear);

        // Enable auto-approve WITHOUT bypass for Sick leave
        await SeedLeaveSettings(LeaveType.Sick, autoApprove: true, threshold: 5, bypassConflictCheck: false);

        // Emp1 already has approved leave
        var today = DateTime.UtcNow.Date;
        await SeedRequest(emp1Id, mgrId, LeaveStatus.Approved, today, today.AddDays(2), deptId, LeaveType.Annual);

        // Act - Emp2 submits Sick leave for same dates (should fail due to conflict)
        var dto = new SubmitLeaveRequestDto
        {
            StartDate = today,
            EndDate = today.AddDays(1),
            Type = LeaveType.Sick
        };

        // Assert - Should throw conflict error
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _leaveService.SubmitRequestAsync(emp2Id, dto));
        Assert.Contains("conflict", ex.Message.ToLower());
    }

    [Fact]
    public async Task Submit_NoAutoApprove_BypassFlagIgnored()
    {
        // Arrange - Bypass enabled but auto-approve disabled (bypass should be ignored)
        int mgrId = 13020;
        int emp1Id = 13021;
        int emp2Id = 13022;
        int deptId = 132;
        int currentYear = DateTime.UtcNow.Year;

        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(emp1Id, deptId, roleId: 2, managerId: mgrId);
        await SeedUser(emp2Id, deptId, roleId: 2, managerId: mgrId);

        await SeedBalance(emp2Id, LeaveType.Annual, 10, currentYear);

        // Bypass enabled but auto-approve disabled
        await SeedLeaveSettings(LeaveType.Annual, autoApprove: false, threshold: 5, bypassConflictCheck: true);

        // Emp1 already has approved leave
        var today = DateTime.UtcNow.Date;
        await SeedRequest(emp1Id, mgrId, LeaveStatus.Approved, today, today.AddDays(2), deptId, LeaveType.Annual);

        // Act - Emp2 submits Annual leave (bypass should be ignored since not auto-approving)
        var dto = new SubmitLeaveRequestDto
        {
            StartDate = today,
            EndDate = today.AddDays(1),
            Type = LeaveType.Annual
        };

        // Assert - Should still throw conflict error (bypass only works with auto-approve)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _leaveService.SubmitRequestAsync(emp2Id, dto));
        Assert.Contains("conflict", ex.Message.ToLower());
    }

    // ==========================================
    // 13. Self-Conflict Check Tests
    // ==========================================

    [Fact]
    public async Task Submit_SelfConflict_BlocksOverlappingPending()
    {
        // Arrange - User already has a pending leave request
        int mgrId = 14001;
        int empId = 14002;
        int deptId = 140;
        int currentYear = DateTime.UtcNow.Year;

        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(empId, deptId, roleId: 2, managerId: mgrId);
        await SeedBalance(empId, LeaveType.Annual, 20, currentYear);

        // Existing pending request
        var today = DateTime.UtcNow.Date;
        await SeedRequest(empId, mgrId, LeaveStatus.PendingManager, today.AddDays(5), today.AddDays(7), deptId);

        // Act - Try to submit overlapping request
        var dto = new SubmitLeaveRequestDto
        {
            StartDate = today.AddDays(6),
            EndDate = today.AddDays(8),
            Type = LeaveType.Annual
        };

        // Assert - Should block due to self-conflict
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _leaveService.SubmitRequestAsync(empId, dto));
        Assert.Contains("already have a leave request", ex.Message.ToLower());
    }

    [Fact]
    public async Task Submit_SelfConflict_BlocksOverlappingApproved()
    {
        // Arrange - User already has an approved leave request
        int mgrId = 14010;
        int empId = 14011;
        int deptId = 141;
        int currentYear = DateTime.UtcNow.Year;

        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(empId, deptId, roleId: 2, managerId: mgrId);
        await SeedBalance(empId, LeaveType.Annual, 20, currentYear);

        // Existing approved request
        var today = DateTime.UtcNow.Date;
        await SeedRequest(empId, mgrId, LeaveStatus.Approved, today.AddDays(10), today.AddDays(12), deptId);

        // Act - Try to submit overlapping request
        var dto = new SubmitLeaveRequestDto
        {
            StartDate = today.AddDays(11),
            EndDate = today.AddDays(13),
            Type = LeaveType.Annual
        };

        // Assert - Should block due to self-conflict
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _leaveService.SubmitRequestAsync(empId, dto));
        Assert.Contains("already have a leave request", ex.Message.ToLower());
    }

    [Fact]
    public async Task Submit_SelfConflict_AllowsIfPreviousWasCancelled()
    {
        // Arrange - User cancelled a previous request for same dates
        int mgrId = 14020;
        int empId = 14021;
        int deptId = 142;
        int currentYear = DateTime.UtcNow.Year;
        int nextYear = currentYear + 1;

        await SeedUser(mgrId, deptId, roleId: 1);
        await SeedUser(empId, deptId, roleId: 2, managerId: mgrId);
        await SeedBalance(empId, LeaveType.Annual, 20, currentYear);
        await SeedBalance(empId, LeaveType.Annual, 20, nextYear); // Also seed next year in case

        // Use mid-year dates to avoid end-of-year issues
        var targetDate = new DateTime(nextYear, 6, 15);
        await SeedRequest(empId, mgrId, LeaveStatus.Cancelled, targetDate, targetDate.AddDays(2), deptId);

        // Act - Submit new request for same dates (should succeed)
        var dto = new SubmitLeaveRequestDto
        {
            StartDate = targetDate,
            EndDate = targetDate.AddDays(2),
            Type = LeaveType.Annual
        };

        var result = await _leaveService.SubmitRequestAsync(empId, dto);

        // Assert - Should succeed since previous was cancelled
        Assert.NotNull(result);
        Assert.Equal(LeaveStatus.PendingManager.ToString(), result.Status);
    }
}
