using Microsoft.EntityFrameworkCore;
using taskedin_be.src.Infrastructure.Persistence;
using taskedin_be.src.Modules.Users.Entities;

namespace taskedin_be.src.Modules.Users.Services;

public class UserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        var now = DateTime.UtcNow;
        user.CreatedAt = now;
        user.UpdatedAt = now;
        user.DeletedAt = null;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
