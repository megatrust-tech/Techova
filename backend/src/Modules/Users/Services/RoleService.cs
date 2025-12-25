using Microsoft.EntityFrameworkCore;
using taskedin_be.src.Infrastructure.Persistence;
using taskedin_be.src.Modules.Users.DTOs.Response;

namespace taskedin_be.src.Modules.Users.Services
{
    public class RoleService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Select(r => new RoleDto
                {
                    RoleId = r.Id,
                    RoleName = r.Name
                })
                .ToListAsync();
        }
    }
}
