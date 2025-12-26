using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using taskedin_be.src.Modules.Auth.DTOs.Request;
using taskedin_be.src.Modules.Auth.Services;

namespace taskedin_be.src.Modules.Auth.Controllers
{
    [ApiController]
    [Route("v1/auth")]
    public class AuthController(AuthService authService) : ControllerBase
    {
        private readonly AuthService _authService = authService;

        /// <summary>
        /// Login endpoint - validates credentials and returns access + refresh tokens.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tokenResponse = await _authService.LoginAsync(loginDto);
            if (tokenResponse == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            return Ok(tokenResponse);
        }

        /// <summary>
        /// Refresh endpoint - generates new access token using refresh token.
        /// Rotates refresh token for security.
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto refreshTokenDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tokenResponse = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);
            if (tokenResponse == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            return Ok(tokenResponse);
        }

        /// <summary>
        /// Logout endpoint - removes refresh token and invalidates all access tokens.
        /// Requires authentication.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var success = await _authService.LogoutAsync(userId);
            if (!success)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Protected endpoint - requires authentication.
        /// Demonstrates basic [Authorize] attribute usage.
        /// </summary>
        [HttpGet("user-info")]
        public IActionResult GetUserInfo()
        {
            var userId = User.FindFirst("userId")?.Value;
            var roleId = User.FindFirst("roleId")?.Value;
            var tokenVersion = User.FindFirst("tokenVersion")?.Value;
            var jti = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Ok(new
            {
                message = "This is a protected endpoint",
                userId,
                roleId,
                tokenVersion,
                jti,
                allClaims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        /// <summary>
        /// Role-based protected endpoint - requires specific roleId.
        /// Demonstrates role-based authorization using roleId claim.
        /// </summary>
        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return Ok(new
            {
                message = "This endpoint is only accessible to users with Admin role",
                userId = User.FindFirst("userId")?.Value,
                role = User.FindFirst(ClaimTypes.Role)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value
            });
        }
    }

}
