using Microsoft.EntityFrameworkCore;
using taskedin_be.src.Infrastructure.Persistence;
using taskedin_be.src.Modules.Auth.DTOs.Request;
using taskedin_be.src.Modules.Auth.DTOs.Response;
using taskedin_be.src.Modules.Auth.Utilities;
using taskedin_be.src.Modules.Users.Entities;
using taskedin_be.src.Modules.Common.Services;

namespace taskedin_be.src.Modules.Auth.Services
{
    /// <summary>
    /// Service handling authentication operations: login, refresh, logout.
    /// </summary>
    public class AuthService(AppDbContext context, JwtTokenService jwtTokenService, IConfiguration configuration, ICacheService cache)
    {
        private readonly AppDbContext _context = context;
        private readonly JwtTokenService _jwtTokenService = jwtTokenService;
        private readonly IConfiguration _configuration = configuration;
        private readonly ICacheService _cache = cache;
        private const string CACHE_KEY_USER_TOKEN_VERSION = "user_token_version_{0}"; // {0} = userId


        /// <summary>
        /// Authenticates user and generates access + refresh tokens.
        /// </summary>
        public async Task<TokenResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                return null;
            }


            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshToken);

            var refreshTokenDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays);


            user.RefreshTokenHash = refreshTokenHash;
            user.RefreshTokenExpiresAt = refreshTokenExpiresAt;

            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RoleId = user.Role.Id,
                RoleName = user.Role.Name
            };
        }

        /// <summary>
        /// Refreshes access token using a valid refresh token.
        /// Rotates refresh token for security (one active token per user).
        /// </summary>
        public async Task<TokenResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RefreshTokenHash != null && u.RefreshTokenExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            User? user = null;
            foreach (var u in users)
            {
                if (u.RefreshTokenHash != null &&
                    _jwtTokenService.VerifyRefreshToken(refreshToken, u.RefreshTokenHash))
                {
                    user = u;
                    break;
                }
            }

            if (user == null)
            {
                return null; // Invalid or expired refresh token
            }

            // Validate refresh token expiry
            if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            {
                // Clear expired token
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                await _context.SaveChangesAsync();
                return null; // Expired refresh token
            }

            // Generate new tokens (rotate refresh token)
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshToken);

            var refreshTokenDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
            var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays);

            // Rotate refresh token (security: one active token per user)
            user.RefreshTokenHash = newRefreshTokenHash;
            user.RefreshTokenExpiresAt = newRefreshTokenExpiresAt;

            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
        }

        /// <summary>
        /// Logs out user by removing refresh token and incrementing tokenVersion.
        /// Incrementing tokenVersion invalidates all existing access tokens (global logout).
        /// </summary>
        public async Task<bool> LogoutAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Remove refresh token
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;

            // Increment tokenVersion to invalidate all existing access tokens
            // Security: This provides global logout functionality
            user.TokenVersion++;

            await _context.SaveChangesAsync();

            // Invalidate cache when user logs out (TokenVersion changed)
            var cacheKey = string.Format(CACHE_KEY_USER_TOKEN_VERSION, userId);
            _cache.Remove(cacheKey);

            return true;
        }

        /// <summary>
        /// Gets user ID from access token claims.
        /// </summary>
        public int? GetUserIdFromToken(string token)
        {
            var principal = _jwtTokenService.ValidateAccessToken(token);
            if (principal == null) return null;

            var userIdClaim = principal.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            return userId;
        }

        /// <summary>
        /// Validates token version against user's current tokenVersion.
        /// Used to check if token was invalidated by logout.
        /// CACHED for performance - this is called on EVERY authenticated request.
        /// </summary>
        public async Task<bool> ValidateTokenVersionAsync(int userId, int tokenVersion)
        {
            var cacheKey = string.Format(CACHE_KEY_USER_TOKEN_VERSION, userId);

            // Get token version from cache or database
            // Using GetOrSetValueAsync for value types (int)
            var currentTokenVersion = await _cache.GetOrSetValueAsync(
                cacheKey,
                async () =>
                {
                    var user = await _context.Users
                        .AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => (int?)u.TokenVersion)
                        .FirstOrDefaultAsync();
                    return user ?? -1; // Return -1 if user not found (invalid)
                },
                TimeSpan.FromMinutes(5) // Cache for 5 minutes - TokenVersion rarely changes
            );

            if (currentTokenVersion == -1) return false; // User not found

            return currentTokenVersion == tokenVersion;
        }
    }

}
