using System;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using taskedin_be.src.Modules.Leaves.DTOs;
using taskedin_be.src.Modules.Leaves.Entities;
using taskedin_be.src.Modules.Leaves.Services;
using taskedin_be.src.Infrastructure.Persistence;

namespace taskedin_be.src.Modules.Leaves.Controllers;

[Authorize]
[ApiController]
[Route("leaves")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public LeaveRequestsController(ILeaveService leaveService, AppDbContext context, IWebHostEnvironment environment)
    {
        _leaveService = leaveService;
        _context = context;
        _environment = environment;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitLeaveRequest([FromForm] SubmitLeaveRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _leaveService.SubmitRequestAsync(userId, dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelRequest(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _leaveService.CancelRequestAsync(userId, id);
            return Ok(new { message = "Request cancelled successfully." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Leave request not found." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("check-conflict")]
    public async Task<IActionResult> CheckConflict([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            if (endDate < startDate)
                return BadRequest(new { message = "End date must be after start date." });

            var userId = GetCurrentUserId();
            var result = await _leaveService.CheckConflictAsync(userId, startDate, endDate);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("coverage")]
    [Authorize(Roles = "Manager,HR")]
    public async Task<IActionResult> GetDepartmentCoverage([FromQuery] DateTime? date)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _leaveService.GetDepartmentCoverageAsync(userId, date);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-leaves")]
    public async Task<IActionResult> GetMyLeaves([FromQuery] PaginationDto pagination)
    {
        var userId = GetCurrentUserId();
        var result = await _leaveService.GetMyLeavesAsync(userId, pagination);
        return Ok(result);
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendarData([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var userId = GetCurrentUserId();
        var result = await _leaveService.GetCalendarDataAsync(userId, startDate, endDate);
        return Ok(result);
    }

    [HttpGet("pending-approval")]
    [Authorize(Roles = "Manager,HR")]
    public async Task<IActionResult> GetPendingApproval([FromQuery] string? status, [FromQuery] PaginationDto pagination)
    {
        var userId = GetCurrentUserId();
        var result = await _leaveService.GetTeamRequestsAsync(userId, status, pagination);
        return Ok(result);
    }

    [HttpGet("pending-approval-count")]
    [Authorize(Roles = "Manager,HR,Admin")]
    public async Task<IActionResult> GetPendingApprovalCount()
    {
        var userId = GetCurrentUserId();
        var result = await _leaveService.GetPendingApprovalCountAsync(userId);
        return Ok(result);
    }

    [HttpPost("{id}/manager-action")]
    public async Task<IActionResult> ManagerAction(int id, [FromBody] LeaveActionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _leaveService.ManagerActionAsync(userId, id, dto);
            return Ok(new { message = "Action recorded successfully." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Leave request not found." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/hr-action")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> HRAction(int id, [FromBody] LeaveActionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _leaveService.HRActionAsync(userId, id, dto);
            return Ok(new { message = "Action recorded successfully." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Leave request not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetRequestHistory(int id)
    {
        try
        {
            var history = await _leaveService.GetRequestHistoryAsync(id);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("audit-logs/download")]
    [Authorize(Roles = "Manager,HR")]
    public async Task<IActionResult> DownloadAuditLogs()
    {
        try
        {
            var userId = GetCurrentUserId();
            var logs = await _leaveService.GetAuditLogsForDownloadAsync(userId);

            var csv = GenerateCsv(logs);
            var fileName = $"leave_audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static string GenerateCsv(IEnumerable<AuditLogDownloadDto> logs)
    {
        var sb = new StringBuilder();
        // Header
        sb.AppendLine("Department,Manager,Request ID,Employee,Leave Type,Start Date,End Date,Days,Status,Action,Action Date,Comment");

        foreach (var log in logs)
        {
            sb.AppendLine($"\"{log.DepartmentName}\",\"{log.ManagerName}\",{log.RequestId},\"{log.EmployeeName}\",\"{log.LeaveType}\",{log.StartDate:yyyy-MM-dd},{log.EndDate:yyyy-MM-dd},{log.NumberOfDays},\"{log.CurrentStatus}\",\"{log.ActionTaken}\",{log.ActionDate:yyyy-MM-dd HH:mm},\"{EscapeCsv(log.Comment)}\"");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\"", "\"\"");
    }

    [HttpGet("leave-types")]
    public IActionResult GetAllLeaveTypes()
    {
        var leaveTypes = Enum.GetValues(typeof(LeaveType))
            .Cast<LeaveType>()
            .Select(lt => new EnumDto
            {
                Name = lt.ToString(),
                Value = (int)lt
            })
            .ToList();

        return Ok(leaveTypes);
    }

    [HttpGet("leave-statuses")]
    public IActionResult GetAllLeaveStatuses()
    {
        var leaveStatuses = Enum.GetValues(typeof(LeaveStatus))
            .Cast<LeaveStatus>()
            .Select(ls => new EnumDto
            {
                Name = ls.ToString(),
                Value = (int)ls
            })
            .ToList();

        return Ok(leaveStatuses);
    }

    [HttpGet("remaining-leaves")]
    public async Task<IActionResult> GetRemainingLeavesCount()
    {
        var userId = GetCurrentUserId();
        var result = await _leaveService.GetBalancesAsync(userId);
        return Ok(result);
    }

    [HttpGet("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLeaveSettings()
    {
        var settings = await _leaveService.GetLeaveSettingsAsync();
        return Ok(settings);
    }

    [HttpPut("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateLeaveSettings([FromBody] List<LeaveSettingsDto> settings)
    {
        await _leaveService.UpdateLeaveSettingsAsync(settings);
        return Ok(new { message = "Settings updated successfully." });
    }

    [HttpGet("users-without-balances")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsersWithoutBalances([FromQuery] PaginationDto pagination, [FromQuery] int? year = null)
    {
        var users = await _leaveService.GetUsersWithoutBalancesAsync(pagination, year);
        return Ok(users);
    }

    [HttpPost("initialize-balances")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> InitializeBalances([FromBody] InitializeBalancesRequestDto request)
    {
        var result = await _leaveService.InitializeBalancesAsync(request);
        return Ok(result);
    }

    [HttpGet("user-balances")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsersWithBalances([FromQuery] PaginationDto pagination, [FromQuery] int? year = null)
    {
        var result = await _leaveService.GetAllUsersWithBalancesAsync(pagination, year);
        return Ok(result);
    }

    [HttpPut("update-balances")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBalances([FromBody] UpdateBalancesRequestDto request)
    {
        var result = await _leaveService.UpdateBalancesAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}/attachment")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Get the leave request
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .Include(lr => lr.Employee!.Role)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null)
                return NotFound(new { message = "Leave request not found." });

            // Check authorization: Employee, Manager, or HR can download
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new UnauthorizedAccessException("User not found.");

            bool canDownload = leaveRequest.EmployeeId == userId || // Employee owns the request
                              leaveRequest.ManagerId == userId ||   // Manager of the request
                              user.Role.Name.Equals("HR", StringComparison.OrdinalIgnoreCase) || // HR role
                              user.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase); // Admin role

            if (!canDownload)
                return Forbid("You do not have permission to download this attachment.");

            if (string.IsNullOrEmpty(leaveRequest.AttachmentPath))
                return NotFound(new { message = "No attachment found for this leave request." });

            // Get the physical file path
            var filePath = leaveRequest.AttachmentPath.StartsWith("/")
                ? Path.Combine(_environment.WebRootPath ?? "wwwroot", leaveRequest.AttachmentPath.TrimStart('/'))
                : Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "leaves", leaveRequest.AttachmentPath);

            // Normalize path separators for Windows
            filePath = filePath.Replace('/', Path.DirectorySeparatorChar);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Attachment file not found on server." });

            // Get file info
            var fileInfo = new FileInfo(filePath);
            var fileName = fileInfo.Name;
            
            // Extract original filename (remove GUID prefix if present)
            if (fileName.Contains('_'))
            {
                var parts = fileName.Split('_', 2);
                if (parts.Length > 1 && Guid.TryParse(parts[0], out _))
                {
                    fileName = parts[1];
                }
            }

            // Determine content type based on file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            // Read file as bytes and return with proper headers
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType, fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error downloading attachment.", error = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst("userId")?.Value;

        if (int.TryParse(idClaim, out int userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User ID claim is invalid or missing.");
    }
}