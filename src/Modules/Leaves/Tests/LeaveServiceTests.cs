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

    private async Task SeedLeaveSettings(LeaveType type, bool autoApprove, int threshold)
    {
        _context.LeaveTypeConfigs.Add(new LeaveTypeConfig
        {
            LeaveTypeId = (int)type,
            Name = type.ToString(),
            AutoApproveEnabled = autoApprove,
            AutoApproveThresholdDays = threshold,
            DefaultBalance = 21
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

        await SeedUser(colleagueId, deptId);
        await SeedUser(myId, deptId);
        await SeedUser(mgrId, deptId, roleId: 1);

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
    public async Task Submit_RequesterIsManager_GoesDirectlyToHR()
    {
        // Arrange
        int managerUserId = 500;
        int deptId = 5;
        // RoleId 1 = Manager (Managers generally don't need a direct manager for leave in this logic, or report to HR/Director)
        await SeedUser(managerUserId, deptId, roleId: 1);
        await SeedBalance(managerUserId, LeaveType.Annual, 20, DateTime.UtcNow.Year);

        // Seed an HR user to receive notification
        int hrId = 900;
        await SeedUser(hrId, null, roleId: 3);

        var dto = new SubmitLeaveRequestDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2),
            Type = LeaveType.Annual
        };

        // Act
        var result = await _leaveService.SubmitRequestAsync(managerUserId, dto);

        // Assert
        Assert.Equal(LeaveStatus.PendingHR.ToString(), result.Status);

        // Check Notification sent to HR
        // CHANGED: Verify QueueNotificationAsync (3 args)
        _mockNotificationQueue.Verify(n =>
            n.QueueNotificationAsync(
                hrId,
                It.Is<string>(s => s.Contains("Action Required") || s.Contains("New")),
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
}