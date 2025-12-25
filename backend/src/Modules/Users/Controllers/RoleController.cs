using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using taskedin_be.src.Modules.Users.Services;

namespace taskedin_be.src.Modules.Users.Controllers
{
    [ApiController]
    [Route("v1/roles")]
    public class RoleController(RoleService roleService) : ControllerBase
    {
        private readonly RoleService _roleService = roleService;

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }
    }
}
