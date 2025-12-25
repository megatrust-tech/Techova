using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using taskedin_be.src.Modules.Auth.Options;
using taskedin_be.src.Modules.Users.Entities;

namespace taskedin_be.src.Modules.Auth.Utilities
{
    /// <summary>
    /// Service for generating and validating JWT tokens.
    /// Handles access token generation and refresh token hashing.
    /// Uses JwtOptions as the single source of truth for configuration.
    /// </summary>
    public class JwtTokenService(JwtOptions jwtOptions)
    {
        private readonly JwtOptions _jwtOptions = jwtOptions;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        /// <summary>
        /// Generates a short-lived access token (15 minutes).
        /// Contains userId, roleId, tokenVersion, jti, iss, and aud claims.
        /// Uses JwtOptions as the single source of truth for issuer and audience.
        /// </summary>
        public string GenerateAccessToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.AccessTokenSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // JWT Claims:
            // - userId: Identifies the user making the request
            // - role: Used for role-based authorization
            // - tokenVersion: Incremented on logout to invalidate all tokens (global logout)
            // - email: User's email address (using standard ClaimTypes.Email)
            // - jti: Unique token identifier for token revocation tracking
            // - iss: Issuer claim (from JwtOptions) - identifies who created the token
            // - aud: Audience claim (from JwtOptions) - identifies the intended recipient
            // - iat, exp: Automatically handled by JWT library
            var claims = new[]
            {
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("tokenVersion", user.TokenVersion.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, 
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), 
                    ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15), // Short-lived access token
                signingCredentials: credentials
            );

            return _tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Generates a cryptographically secure random refresh token.
        /// Should be hashed before storing in database.
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64]; // 512 bits
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Hashes a refresh token using PBKDF2 (more secure than BCrypt for tokens).
        /// Only the hash should be stored in the database.
        /// </summary>
        public string HashRefreshToken(string refreshToken)
        {
            var salt = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                refreshToken,
                salt,
                100000, // 100k iterations for security
                HashAlgorithmName.SHA256
            );

            var hash = pbkdf2.GetBytes(32); // 256 bits
            var hashBytes = new byte[48]; // 16 (salt) + 32 (hash)
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verifies a refresh token against its hash.
        /// </summary>
        public bool VerifyRefreshToken(string refreshToken, string hash)
        {
            try
            {
                var hashBytes = Convert.FromBase64String(hash);
                if (hashBytes.Length != 48) return false;

                var salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                using var pbkdf2 = new Rfc2898DeriveBytes(
                    refreshToken,
                    salt,
                    100000,
                    HashAlgorithmName.SHA256
                );

                var computedHash = pbkdf2.GetBytes(32);
                var storedHash = new byte[32];
                Array.Copy(hashBytes, 16, storedHash, 0, 32);

                // Constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates and extracts claims from an access token.
        /// Returns null if token is invalid.
        /// Uses JwtOptions as the single source of truth for validation.
        /// </summary>
        public ClaimsPrincipal? ValidateAccessToken(string token)
        {
            try
            {
                var secretBytes = Encoding.UTF8.GetBytes(_jwtOptions.AccessTokenSecret);
                if (secretBytes.Length < 32)
                {
                    return null; // Invalid key length
                }

                var key = new SymmetricSecurityKey(secretBytes);
            
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidAudience = _jwtOptions.Audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                };

                return _tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }
    }

}
