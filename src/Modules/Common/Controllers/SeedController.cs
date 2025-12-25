using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using taskedin_be.scripts;

namespace taskedin_be.src.Modules.Common.Controllers;

[ApiController]
[Route("api/seed")]
public class SeedController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SeedController> _logger;

    public SeedController(IConfiguration configuration, ILogger<SeedController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("run")]
    public IActionResult RunSeed()
    {
        try
        {
            _logger.LogInformation("Seed endpoint called - starting database seeding...");

            // Run seed in background to avoid blocking the request
            _ = Task.Run(async () =>
            {
                try
                {
                    var seedScript = new SeedDatabase();
                    await seedScript.Run();
                    _logger.LogInformation("Database seeding completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during database seeding");
                }
            });

            return Ok(new { message = "Database seeding started. Check logs for progress." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting seed process");
            return StatusCode(500, new { error = "Failed to start seed process", message = ex.Message });
        }
    }
}

