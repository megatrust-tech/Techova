using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using taskedin_be.src.Infrastructure.Persistence;
using taskedin_be.src.Modules.Auth.Middleware;
using taskedin_be.src.Modules.Auth.Options;
using taskedin_be.src.Modules.Auth.Services;
using taskedin_be.src.Modules.Auth.Utilities;
using System.IdentityModel.Tokens.Jwt;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Check if seed command is provided
if (args.Length > 0 && args[0].Equals("seed", StringComparison.OrdinalIgnoreCase))
{
    await RunSeedScript();
    return;
}

// Load .env file if it exists (for local development)
// In production, use actual environment variables
// Try multiple locations: root directory and src directory
var rootEnvPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
var srcEnvPath = Path.Combine(Directory.GetCurrentDirectory(), "src", ".env");
var envPath = File.Exists(rootEnvPath) ? rootEnvPath :
              File.Exists(srcEnvPath) ? srcEnvPath : null;


if (envPath != null)
{
    Env.Load(envPath);
    Console.WriteLine($"Loaded .env file from: {envPath}");
}

var builder = WebApplication.CreateBuilder(args);

// Build connection string from environment variables, or fall back to appsettings.json
string? connectionString = BuildConnectionString(builder.Configuration);

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured. " +
        "Set DB_CONNECTION_STRING (full string) or individual variables (DB_SERVER, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD) " +
        "or configure DefaultConnection in appsettings.json");
}

static string? BuildConnectionString(IConfiguration configuration)
{
    // Option 1: Full connection string from environment variable
    var fullConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
    if (!string.IsNullOrEmpty(fullConnectionString))
    {
        return fullConnectionString;
    }

    // Option 2: Build from individual components
    var server = Environment.GetEnvironmentVariable("DB_SERVER");
    var port = Environment.GetEnvironmentVariable("DB_PORT");
    var database = Environment.GetEnvironmentVariable("DB_NAME");
    var userId = Environment.GetEnvironmentVariable("DB_USER");
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

    // If any individual component is set, build the connection string
    if (!string.IsNullOrEmpty(server) || !string.IsNullOrEmpty(database) ||
        !string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(password))
    {
        server ??= "localhost";
        port ??= "1433";
        database ??= "TaskedInDb";
        userId ??= "sa";

        var connectionString = $"Server={server},{port};Database={database};User Id={userId};";

        if (!string.IsNullOrEmpty(password))
        {
            connectionString += $"Password={password};";
        }

        connectionString += "TrustServerCertificate=True";
        return connectionString;
    }

    // Option 3: Fall back to appsettings.json
    return configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Configure JWT Options - Single source of truth
// Read from environment variables (preferred) or fallback to appsettings.json
var accessTokenSecret = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_SECRET")
    ?? builder.Configuration["Jwt:AccessTokenSecret"]
    ?? throw new InvalidOperationException("JWT access token secret is not configured. Set JWT_ACCESS_TOKEN_SECRET environment variable or configure Jwt:AccessTokenSecret in appsettings.json");

var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured. Set JWT_ISSUER environment variable or configure Jwt:Issuer in appsettings.json");

var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured. Set JWT_AUDIENCE environment variable or configure Jwt:Audience in appsettings.json");

// Validate all values are not empty
if (string.IsNullOrWhiteSpace(accessTokenSecret))
    throw new InvalidOperationException("JWT access token secret cannot be empty.");
if (string.IsNullOrWhiteSpace(issuer))
    throw new InvalidOperationException("JWT issuer cannot be empty.");
if (string.IsNullOrWhiteSpace(audience))
    throw new InvalidOperationException("JWT audience cannot be empty.");

// Validate secret key length: HS256 requires at least 32 bytes (256 bits)
var secretBytes = Encoding.UTF8.GetBytes(accessTokenSecret);
if (secretBytes.Length < 32)
{
    throw new InvalidOperationException(
        $"JWT access token secret is too short. HS256 requires at least 32 bytes (256 bits), but the current secret is only {secretBytes.Length} bytes ({secretBytes.Length * 8} bits). " +
        "Please generate a secure random string of at least 32 characters. " +
        "You can generate one using: [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 })) in PowerShell, " +
        "or: openssl rand -base64 32 in bash.");
}

// Register JwtOptions as Singleton - single source of truth for JWT configuration
var jwtOptions = new JwtOptions
{
    AccessTokenSecret = accessTokenSecret,
    Issuer = issuer,
    Audience = audience
};
builder.Services.AddSingleton(jwtOptions);

// Add in-memory caching (built-in, no extra package needed)
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache size (optional)
});

// For distributed caching (Redis) in the future, use:
// builder.Services.AddStackExchangeRedisCache(options => { ... });

// Register cache service
builder.Services.AddSingleton<taskedin_be.src.Modules.Common.Services.ICacheService, taskedin_be.src.Modules.Common.Services.CacheService>();

builder.Services.AddScoped<taskedin_be.src.Modules.Users.Services.UserService>();
builder.Services.AddScoped<taskedin_be.src.Modules.Users.Services.RoleService>();
builder.Services.AddScoped<taskedin_be.src.Modules.Notifications.Interfaces.IEmailService, taskedin_be.src.Modules.Notifications.Services.EmailService>();
builder.Services.AddScoped<taskedin_be.src.Modules.Notifications.Interfaces.IFirebaseNotificationService, taskedin_be.src.Modules.Notifications.Services.FirebaseNotificationService>();
builder.Services.AddScoped<taskedin_be.src.Modules.Notifications.Interfaces.INotificationService, taskedin_be.src.Modules.Notifications.Services.NotificationService>();
builder.Services.AddSingleton<taskedin_be.src.Modules.Notifications.Interfaces.INotificationQueue, taskedin_be.src.Modules.Notifications.Services.InMemoryNotificationQueue>();
builder.Services.AddHostedService<taskedin_be.src.Modules.Notifications.Services.NotificationBackgroundWorker>();
builder.Services.AddScoped<taskedin_be.src.Modules.Leaves.Services.ILeaveService, taskedin_be.src.Modules.Leaves.Services.LeaveService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Use JwtOptions as the single source of truth - ensures consistency with token generation
    // Capture values from the configured JwtOptions instance
    var configuredIssuer = jwtOptions.Issuer;
    var configuredAudience = jwtOptions.Audience;
    var configuredSecret = jwtOptions.AccessTokenSecret;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuredIssuer,
        ValidAudience = configuredAudience,
        // When DefaultInboundClaimTypeMap is cleared, ensure audience is validated correctly
        // The audience is stored in the 'aud' claim, which should be preserved
        AudienceValidator = (audiences, securityToken, validationParameters) =>
        {
            // If audiences is null or empty, check if the token has an 'aud' claim
            if (audiences == null || !audiences.Any())
            {
                if (securityToken is JwtSecurityToken jwtToken)
                {
                    // Try to get audience from the 'aud' claim directly
                    var audClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "aud" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/audience");
                    if (audClaim != null && audClaim.Value == configuredAudience)
                    {
                        return true;
                    }
                }
                return false;
            }
            // Standard validation: check if any of the audiences match
            return audiences != null && audiences.Contains(configuredAudience);
        },
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuredSecret)),
        ClockSkew = builder.Environment.IsDevelopment() ? TimeSpan.FromMinutes(1) : TimeSpan.Zero // Small tolerance in development
    };

    // Add event handlers to log authentication failures for debugging
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Authentication Failed: {context.Exception.Message}");
            if (context.Exception is SecurityTokenExpiredException)
            {
                Console.WriteLine("Token has expired.");
            }
            else if (context.Exception is SecurityTokenInvalidSignatureException)
            {
                Console.WriteLine("Token signature is invalid. Check if the signing key matches.");
            }
            else if (context.Exception is SecurityTokenInvalidAudienceException)
            {
                Console.WriteLine($"Token audience is invalid. Expected: {configuredAudience}");
            }
            else if (context.Exception is SecurityTokenInvalidIssuerException)
            {
                Console.WriteLine($"Token issuer is invalid. Expected: {configuredIssuer}");
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"JWT Challenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Default policy requires authentication
    options.DefaultPolicy = options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Role-based policies (example: require specific roleId)
    // Usage: [Authorize(Policy = "RequireAdminRole")]
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireClaim("roleId", "1")); // Assuming roleId 1 is admin

    options.AddPolicy("RequireManagerRole", policy =>
        policy.RequireClaim("roleId", "2")); // Assuming roleId 2 is manager
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskedIn API", Version = "v1" });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();
Console.WriteLine($"ENV = {builder.Environment.EnvironmentName}");
Console.WriteLine($"CS  = {builder.Configuration.GetConnectionString("DefaultConnection")}");

// Apply database migrations on startup
// This will create the database if it doesn't exist and apply all pending migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        Console.WriteLine("Applying database migrations...");
        context.Database.Migrate();
        Console.WriteLine("✓ Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while applying database migrations: {ex.Message}");
        throw;
    }
}

// Security: Use HTTPS redirection in production
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure static files with CORS support
// Note: CORS must be applied to static files via OnPrepareResponse
// because static file middleware short-circuits the pipeline before CORS middleware runs
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Add CORS headers to static file responses
        var headers = ctx.Context.Response.Headers;
        var origin = ctx.Context.Request.Headers["Origin"].ToString();
        
        // Allow any origin (matching DevCors policy)
        if (!string.IsNullOrEmpty(origin))
        {
            headers.Append("Access-Control-Allow-Origin", origin);
        }
        else
        {
            headers.Append("Access-Control-Allow-Origin", "*");
        }
        headers.Append("Access-Control-Allow-Methods", "GET, OPTIONS");
        headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
        headers.Append("Access-Control-Allow-Credentials", "true");
    }
});

// Middleware order is important
// app.UseHttpsRedirection();
app.UseCors("DevCors");
app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();

// Custom middleware to validate tokenVersion (global logout support)
// app.UseMiddleware<TokenVersionValidationMiddleware>();

app.MapControllers();

app.Run();

// Seed script function
static async Task RunSeedScript()
{
    try
    {
        var seedScript = new taskedin_be.scripts.SeedDatabase();
        await seedScript.Run();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error running seed script: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        Environment.Exit(1);
    }
    Environment.Exit(0);
}
