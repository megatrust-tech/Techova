using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using taskedin_be.src.Modules.Leaves.DTOs;
using taskedin_be.src.Modules.Leaves.Entities;
using taskedin_be.src.Modules.Leaves.Services;

namespace taskedin_be.src.Modules.Leaves.Controllers;

[Authorize]
[ApiController]
[Route("leaves")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveRequestsController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
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

    [HttpGet("pending-approval")]
    [Authorize(Roles = "Manager,HR")]
    public async Task<IActionResult> GetPendingApproval([FromQuery] string? status, [FromQuery] PaginationDto pagination)
    {
        var userId = GetCurrentUserId();
        var result = await _leaveService.GetTeamRequestsAsync(userId, status, pagination);
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