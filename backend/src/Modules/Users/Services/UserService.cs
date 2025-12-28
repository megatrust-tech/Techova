using Microsoft.EntityFrameworkCore;
using taskedin_be.src.Infrastructure.Persistence;
using taskedin_be.src.Modules.Users.DTOs;
using taskedin_be.src.Modules.Users.Entities;
using taskedin_be.src.Modules.Leaves.Entities;

namespace taskedin_be.src.Modules.Users.Services
{
    public class UserService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task<CreateUserResponseDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);
            var user = new User
            {
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Email = createUserDto.Email,
                PasswordHash = passwordHash,
                RoleId = createUserDto.RoleId
            };
            var now = DateTime.UtcNow;
            user.CreatedAt = now;
            user.UpdatedAt = now;
            user.DeletedAt = null;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await InitializeLeaveBalancesAsync(user.Id);

            return new CreateUserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                RoleId = user.RoleId
            };
        }

        public async Task<UserByIdDto?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Where(u => u.Id == id)
                .Include(u => u.Role)
                .Select(u => new UserByIdDto
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Role = u.Role
                })
                .FirstOrDefaultAsync();
        }

        private async Task InitializeLeaveBalancesAsync(int userId)
        {
            var policies = await _context.LeaveTypeConfigs.ToListAsync();
            var currentYear = DateTime.UtcNow.Year;
            var balances = new List<LeaveBalance>();

            if (policies.Any())
            {
                foreach (var policy in policies)
                {
                    balances.Add(new LeaveBalance
                    {
                        EmployeeId = userId,
                        Type = (LeaveType)policy.LeaveTypeId,
                        Year = currentYear,
                        TotalDays = policy.DefaultBalance,
                        UsedDays = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                var allTypes = Enum.GetValues(typeof(LeaveType)).Cast<LeaveType>();
                foreach (var type in allTypes)
                {
                    var defaultDays = type == LeaveType.Annual ? 21 : 7; // Example default fallback
                    balances.Add(new LeaveBalance
                    {
                        EmployeeId = userId,
                        Type = type,
                        Year = currentYear,
                        TotalDays = defaultDays,
                        UsedDays = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            if (balances.Any())
            {
                _context.LeaveBalances.AddRange(balances);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RegisterDeviceAsync(int userId, RegisterDeviceDto dto)
        {
            var existingDevice = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == dto.Token);

            if (existingDevice != null)
            {
                existingDevice.LastUsedAt = DateTime.UtcNow;
                existingDevice.Platform = dto.Platform;
            }
            else
            {
                var device = new UserDevice
                {
                    UserId = userId,
                    DeviceToken = dto.Token,
                    Platform = dto.Platform,
                    LastUsedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserDevices.Add(device);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveDeviceAsync(int userId, string token)
        {
            var device = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == token);

            if (device != null)
            {
                _context.UserDevices.Remove(device);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<UserSearchResultDto>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<UserSearchResultDto>();

            var searchTerm = query.ToLower().Trim();

            var users = await _context.Users
                .Include(u => u.Role)
                .Where(u => 
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    (u.FirstName + " " + u.LastName).ToLower().Contains(searchTerm))
                .Take(20) // Limit results
                .Select(u => new UserSearchResultDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    RoleName = u.Role.Name,
                    DepartmentId = u.DepartmentId
                })
                .ToListAsync();

            return users;
        }
    }
}