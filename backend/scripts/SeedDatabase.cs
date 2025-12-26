using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;
using taskedin_be.src.Infrastructure.Persistence;
using taskedin_be.src.Modules.Users.Entities;
using taskedin_be.src.Modules.Leaves.Entities;
using DotNetEnv;

namespace taskedin_be.scripts;

public class SeedDatabase
{
    private const int TOTAL_DEPARTMENTS = 100;
    private const int TOTAL_USERS = 500_000;
    private const int BATCH_SIZE = 1000; // Batch size for inserts

    // Role distribution percentages
    private const double ADMIN_PERCENT = 0.1;      // 0.1%
    private const double HR_PERCENT = 0.5;         // 0.5%
    private const double MANAGER_PERCENT = 5.0;    // 5%
    private const double EMPLOYEE_PERCENT = 94.4; // 94.4%

    public async Task Run()
    {
        Console.WriteLine("=== Database Seeding Script ===");
        Console.WriteLine($"Creating {TOTAL_DEPARTMENTS} departments and {TOTAL_USERS:N0} users...");
        Console.WriteLine();

        // Load environment variables
        var rootEnvPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        var srcEnvPath = Path.Combine(Directory.GetCurrentDirectory(), "src", ".env");
        var envPath = File.Exists(rootEnvPath) ? rootEnvPath :
                      File.Exists(srcEnvPath) ? srcEnvPath : null;

        if (envPath != null)
        {
            Env.Load(envPath);
        }

        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("src/appsettings.json", optional: false)
            .AddJsonFile("src/appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Build connection string using same logic as Program.cs
        // Priority: 1) DB_CONNECTION_STRING env var, 2) Individual DB_* env vars, 3) appsettings.json
        var connectionString = BuildConnectionString(configuration);

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("ERROR: Connection string not found!");
            Console.WriteLine("Set DB_CONNECTION_STRING (full string) or individual variables (DB_SERVER, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD)");
            return;
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var context = new AppDbContext(optionsBuilder.Options);

        try
        {


            // Step 4: Assign Managers
            await AssignManagersAsync(context);
            Console.WriteLine("✓ Assigned managers to users");

            // Step 5: Create Leave Balances
            await CreateLeaveBalancesAsync(context);
            Console.WriteLine("✓ Created leave balances for all users");

            // Step 6: Create Random Leaves
            await CreateRandomLeavesAsync(context);
            Console.WriteLine("✓ Created random leave requests with notes and statuses");

            Console.WriteLine();
            Console.WriteLine("=== Seeding Complete ===");
            Console.WriteLine($"Total Users: {TOTAL_USERS:N0}");
            Console.WriteLine($"Total Departments: {TOTAL_DEPARTMENTS}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static string? BuildConnectionString(IConfiguration configuration)
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

    private static async Task<Dictionary<string, Role>> EnsureRolesAsync(AppDbContext context)
    {
        var roleNames = new[] { "Admin", "HR", "Manager", "Employee", "Director", "VP", "CEO", "Intern" };
        var roles = new Dictionary<string, Role>();

        foreach (var roleName in roleNames)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                role = new Role
                {
                    Name = roleName,
                    Description = $"{roleName} role",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Roles.Add(role);
                await context.SaveChangesAsync();
            }
            roles[roleName] = role;
        }

        return roles;
    }

    private static async Task CreateUsersAsync(AppDbContext context, Dictionary<string, Role> roles)
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var currentYear = DateTime.UtcNow.Year;
        var now = DateTime.UtcNow;

        // Calculate user counts per role
        var adminCount = (int)(TOTAL_USERS * ADMIN_PERCENT / 100);
        var hrCount = (int)(TOTAL_USERS * HR_PERCENT / 100);
        var managerCount = (int)(TOTAL_USERS * MANAGER_PERCENT / 100);
        var employeeCount = TOTAL_USERS - adminCount - hrCount - managerCount;

        Console.WriteLine($"  Admin: {adminCount:N0}");
        Console.WriteLine($"  HR: {hrCount:N0}");
        Console.WriteLine($"  Manager: {managerCount:N0}");
        Console.WriteLine($"  Employee: {employeeCount:N0}");

        var userCounter = 0;
        var batch = new List<User>();

        // Create Admins
        Console.WriteLine("  Creating Admins...");
        for (int i = 0; i < adminCount; i++)
        {
            userCounter++;
            var user = CreateUser(userCounter, "Admin", roles["Admin"], random, now);
            batch.Add(user);

            if (batch.Count >= BATCH_SIZE)
            {
                await SaveBatchAsync(context, batch);
                batch.Clear();
                Console.WriteLine($"  Created {userCounter:N0} users...");
            }
        }
        if (batch.Any())
        {
            await SaveBatchAsync(context, batch);
            batch.Clear();
            Console.WriteLine($"  Created {userCounter:N0} users...");
        }

        // Create HR
        Console.WriteLine("  Creating HR users...");
        for (int i = 0; i < hrCount; i++)
        {
            userCounter++;
            var user = CreateUser(userCounter, "HR", roles["HR"], random, now);
            batch.Add(user);

            if (batch.Count >= BATCH_SIZE)
            {
                await SaveBatchAsync(context, batch);
                batch.Clear();
                Console.WriteLine($"  Created {userCounter:N0} users...");
            }
        }
        if (batch.Any())
        {
            await SaveBatchAsync(context, batch);
            batch.Clear();
            Console.WriteLine($"  Created {userCounter:N0} users...");
        }

        // Create Managers
        Console.WriteLine("  Creating Managers...");
        for (int i = 0; i < managerCount; i++)
        {
            userCounter++;
            var user = CreateUser(userCounter, "Manager", roles["Manager"], random, now);
            batch.Add(user);

            if (batch.Count >= BATCH_SIZE)
            {
                await SaveBatchAsync(context, batch);
                batch.Clear();
                Console.WriteLine($"  Created {userCounter:N0} users...");
            }
        }
        if (batch.Any())
        {
            await SaveBatchAsync(context, batch);
            batch.Clear();
            Console.WriteLine($"  Created {userCounter:N0} users...");
        }

        // Create Employees
        Console.WriteLine("  Creating Employees (this will take a while for 472,000 users)...");
        var employeeStartTime = DateTime.UtcNow;
        for (int i = 0; i < employeeCount; i++)
        {
            userCounter++;
            var user = CreateUser(userCounter, "Employee", roles["Employee"], random, now);
            batch.Add(user);

            if (batch.Count >= BATCH_SIZE)
            {
                await SaveBatchAsync(context, batch);
                batch.Clear();
                var elapsed = (DateTime.UtcNow - employeeStartTime).TotalSeconds;
                var rate = userCounter / elapsed;
                var remaining = (TOTAL_USERS - userCounter) / rate;
                Console.WriteLine($"  Created {userCounter:N0} users... ({rate:F0} users/sec, ~{remaining / 60:F1} min remaining)");
            }
        }

        // Save remaining users
        if (batch.Any())
        {
            await SaveBatchAsync(context, batch);
        }
    }

    private static User CreateUser(int userNumber, string roleName, Role role, Random random, DateTime now)
    {
        var firstName = $"User{userNumber}";
        var lastName = $"LastName{userNumber}";
        var email = $"user{userNumber}_{roleName.ToLower()}@taskedinbe.com";
        var password = $"user{userNumber}_{roleName.ToLower()}";

        // Use lower workFactor (4) for faster seeding - still secure but much faster for bulk operations
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);

        return new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            RoleId = role.Id,
            DepartmentId = random.Next(1, TOTAL_DEPARTMENTS + 1),
            TokenVersion = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static async Task SaveBatchAsync(AppDbContext context, List<User> batch)
    {
        context.Users.AddRange(batch);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear(); // Clear tracking to free memory
    }

    private static async Task AssignManagersAsync(AppDbContext context)
{
    Console.WriteLine("  Assigning managers...");

    // Get manager IDs only (no Include, no tracking)
    var managerIds = await context.Users
        .Where(u => u.Role.Name == "Manager")
        .OrderBy(u => u.Id)
        .Select(u => u.Id)
        .ToListAsync();

    if (!managerIds.Any())
    {
        Console.WriteLine("  WARNING: No managers found!");
        return;
    }

    const int PAGE_SIZE = 5_000;
    var managerIndex = 0;
    var processed = 0;

    while (true)
    {
        var employeeIds = await context.Users
            .Where(u => u.ManagerId == null &&
                        u.Role.Name != "Admin" &&
                        u.Role.Name != "Manager")
            .OrderBy(u => u.Id)
            .Select(u => u.Id)
            .Take(PAGE_SIZE)
            .ToListAsync();

        if (!employeeIds.Any())
            break;

        var updates = new List<User>(employeeIds.Count);

        foreach (var id in employeeIds)
        {
            updates.Add(new User
            {
                Id = id,
                ManagerId = managerIds[managerIndex++ % managerIds.Count]
            });
        }

        context.Users.AttachRange(updates);
        foreach (var u in updates)
            context.Entry(u).Property(x => x.ManagerId).IsModified = true;

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        processed += updates.Count;
        Console.WriteLine($"  Assigned managers to {processed:N0} users...");
    }

    // Manager hierarchy (10%)
    var managersNeedingManagers = await context.Users
        .Where(u => u.Role.Name == "Manager" && u.ManagerId == null)
        .OrderBy(u => u.Id)
        .Skip(managerIds.Count / 10)
        .Select(u => u.Id)
        .ToListAsync();

    var hierarchyUpdates = new List<User>();
    for (int i = 0; i < managersNeedingManagers.Count; i++)
    {
        hierarchyUpdates.Add(new User
        {
            Id = managersNeedingManagers[i],
            ManagerId = managerIds[i % managerIds.Count]
        });
    }

    if (hierarchyUpdates.Any())
    {
        context.Users.AttachRange(hierarchyUpdates);
        foreach (var u in hierarchyUpdates)
            context.Entry(u).Property(x => x.ManagerId).IsModified = true;

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}   
 private static async Task CreateLeaveBalancesAsync(AppDbContext context)
{
    Console.WriteLine("  Creating leave balances...");
    var year = DateTime.UtcNow.Year;
    var now = DateTime.UtcNow;

    var policies = await context.LeaveTypeConfigs.ToListAsync();
    var leaveTypes = Enum.GetValues(typeof(LeaveType)).Cast<LeaveType>().ToList();

    const int PAGE_SIZE = 5_000;
    var processed = 0;

    while (true)
    {
        var users = await context.Users
            .Where(u => !context.LeaveBalances
                .Any(b => b.EmployeeId == u.Id && b.Year == year))
            .OrderBy(u => u.Id)
            .Select(u => u.Id)
            .Take(PAGE_SIZE)
            .ToListAsync();

        if (!users.Any())
            break;

        var batch = new List<LeaveBalance>();

        foreach (var userId in users)
        {
            foreach (var leaveType in leaveTypes)
            {
                var policy = policies.FirstOrDefault(p => p.LeaveTypeId == (int)leaveType);
                var totalDays = policy?.DefaultBalance ??
                                (leaveType == LeaveType.Annual ? 21 : 7);

                batch.Add(new LeaveBalance
                {
                    EmployeeId = userId,
                    Type = leaveType,
                    Year = year,
                    TotalDays = totalDays,
                    UsedDays = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await context.LeaveBalances.AddRangeAsync(batch);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        processed += users.Count;
        Console.WriteLine($"  Created balances for {processed:N0} users...");
    }
}


    private static async Task CreateRandomLeavesAsync(AppDbContext context)
{
    Console.WriteLine("  Creating random leave requests...");

    const int PAGE_SIZE = 5_000;
    var random = new Random(42);
    var now = DateTime.UtcNow;
    var generated = 0;

    var sickNotes = new[] { "Flu", "Doctor visit", "Migraine" };
    var annualNotes = new[] { "Vacation", "Family trip", "Personal leave" };
    var otherNotes = new[] { "Emergency", "Personal reasons" };

    while (true)
    {
        var users = await context.Users
            .Where(u => u.ManagerId != null)
            .OrderBy(u => u.Id)
            .Select(u => new { u.Id, u.ManagerId })
            .Take(PAGE_SIZE)
            .ToListAsync();

        if (!users.Any())
            break;

        var batch = new List<LeaveRequest>();

        foreach (var user in users)
        {
            if (random.NextDouble() > 0.4) continue;

            int count = random.Next(1, 4);
            for (int i = 0; i < count; i++)
            {
                var type = (LeaveType)random.Next(Enum.GetValues(typeof(LeaveType)).Length);
                var note = type.ToString().Contains("Sick")
                    ? sickNotes[random.Next(sickNotes.Length)]
                    : type.ToString().Contains("Annual")
                        ? annualNotes[random.Next(annualNotes.Length)]
                        : otherNotes[random.Next(otherNotes.Length)];

                var start = new DateTime(now.Year, random.Next(1, 13), random.Next(1, 28));
                var days = random.Next(1, 6);

                batch.Add(new LeaveRequest
                {
                    EmployeeId = user.Id,
                    ManagerId = user.ManagerId!.Value,
                    Type = type,
                    StartDate = start,
                    EndDate = start.AddDays(days),
                    NumberOfDays = days,
                    Status = LeaveStatus.PendingManager,
                    Notes = note,
                    CreatedAt = now.AddDays(-random.Next(1, 30)),
                    UpdatedAt = now
                });

                generated++;
            }
        }

        if (batch.Any())
        {
            await context.LeaveRequests.AddRangeAsync(batch);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            Console.WriteLine($"  Generated {generated:N0} leave requests...");
        }
    }
}

}