using Microsoft.AspNetCore.Mvc;

namespace taskedin_be.src.Modules.Leaves.Controllers;

[ApiController]
[Route("leaves")]
public class LeavesController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        Console.WriteLine("Leaves module is running");
        return Ok("Leaves module is running successfully");
    }
}
