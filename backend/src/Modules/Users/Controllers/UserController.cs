using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using taskedin_be.src.Modules.Users.DTOs;
using taskedin_be.src.Modules.Users.Entities;
using taskedin_be.src.Modules.Users.Services;

namespace taskedin_be.src.Modules.Users.Controllers
{
    [ApiController]
    [Route("v1/users")]
    public class UserController(UserService userService) : ControllerBase
    {
        private readonly UserService _userService = userService;

        [HttpPost]
        [AllowAnonymous]
        [Authorize(Roles = "Admin,HR")]

        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var createdUser = await _userService.CreateUserAsync(createUserDto);
            return StatusCode(StatusCodes.Status201Created, createdUser);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Employee,Admin,Manager,HR")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        [HttpPost("devices")]
        [Authorize]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto)
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            await _userService.RegisterDeviceAsync(userId, dto);
            return Ok(new { message = "Device registered successfully" });
        }

        [HttpDelete("devices/{token}")]
        [Authorize]
        public async Task<IActionResult> RemoveDevice(string token)
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            await _userService.RemoveDeviceAsync(userId, WebUtility.UrlDecode(token));
            return Ok(new { message = "Device removed" });
        }

        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Search query is required" });

            var users = await _userService.SearchUsersAsync(query);
            return Ok(users);
        }
    }
}