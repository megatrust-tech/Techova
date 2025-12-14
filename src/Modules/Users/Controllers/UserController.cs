using Microsoft.AspNetCore.Mvc;
using taskedin_be.src.Modules.Users.DTOs;
using taskedin_be.src.Modules.Users.Entities;
using taskedin_be.src.Modules.Users.Services;

namespace taskedin_be.src.Modules.Users.Controllers;

[ApiController]
[Route("users")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        Console.WriteLine("Users module is running");
        return Ok("Users module is running successfully");
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        var user = new User
        {
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            Email = createUserDto.Email,
            PasswordHash = createUserDto.PasswordHash,
            RoleId = createUserDto.RoleId
        };

        var createdUser = await _userService.CreateUserAsync(user);
        return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }
}