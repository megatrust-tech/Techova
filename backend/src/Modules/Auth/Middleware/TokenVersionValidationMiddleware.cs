using taskedin_be.src.Modules.Auth.Services;

namespace taskedin_be.src.Modules.Auth.Middleware
{
    /// <summary>
    /// Middleware to validate tokenVersion claim against user's current tokenVersion.
    /// Invalidates tokens that were issued before a logout (global logout).
    /// </summary>
    public class TokenVersionValidationMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context, AuthService authService)
        {
            // Only validate if user is authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst("userId");
                var tokenVersionClaim = context.User.FindFirst("tokenVersion");

                if (userIdClaim != null && tokenVersionClaim != null &&
                    int.TryParse(userIdClaim.Value, out var userId) &&
                    int.TryParse(tokenVersionClaim.Value, out var tokenVersion))
                {
                    // Validate token version (check if token was invalidated by logout)
                    var isValid = await authService.ValidateTokenVersionAsync(userId, tokenVersion);
                    if (!isValid)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Token has been invalidated. Please login again.");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

}
