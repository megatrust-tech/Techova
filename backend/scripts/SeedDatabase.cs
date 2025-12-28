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

        // Build connection string
        var connectionString = BuildConnectionString(configuration);

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("ERROR: Connection string not found!");
            return;
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var context = new AppDbContext(optionsBuilder.Options);

        try
        {
            // Ensure database is cleaned and recreated to guarantee schema integrity
            // This fixes the issue where SQL DELETE FROM might leave the schema in a bad state
            Console.WriteLine("Dropping database if exists...");
            await context.Database.EnsureDeletedAsync();
            
            Console.WriteLine("Applying migrations and creating database...");
            await context.Database.MigrateAsync();
            Console.WriteLine("✓ Database created and migrations applied successfully");

            // Step 1: Create/Ensure Roles
            var roles = await EnsureRolesAsync(context);
            Console.WriteLine($"✓ Roles ensured ({roles.Count} roles)");

            // Step 2: Create Departments (Logical IDs 1-100)
            Console.WriteLine($"✓ Using {TOTAL_DEPARTMENTS} department IDs (1-{TOTAL_DEPARTMENTS})");

            // Step 3: Create Users
            await CreateUsersAsync(context, roles);
            Console.WriteLine($"✓ Created {TOTAL_USERS:N0} users");

            // Step 4: Assign Managers (Chain of Command)
            await AssignManagersAsync(context);
            Console.WriteLine("✓ Assigned managers to users (Chain of Command built)");

            // Step 5: Create Leave Balances
            await CreateLeaveBalancesAsync(context);
            Console.WriteLine("✓ Created leave balances for all users");

            // Step 6: Create Random Leaves
            await CreateRandomLeavesAsync(context);
            Console.WriteLine("✓ Created random leave requests with notes and statuses");

            Console.WriteLine();
            Console.WriteLine("=== Seeding Complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static string? BuildConnectionString(IConfiguration configuration)
    {
        var fullConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(fullConnectionString)) return fullConnectionString;

        var server = Environment.GetEnvironmentVariable("DB_SERVER");
        var port = Environment.GetEnvironmentVariable("DB_PORT");
        var database = Environment.GetEnvironmentVariable("DB_NAME");
        var userId = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (!string.IsNullOrEmpty(server) || !string.IsNullOrEmpty(database) ||
            !string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(password))
        {
            server ??= "localhost";
            port ??= "1433";
            database ??= "TaskedInDb";
            userId ??= "sa";

            var connectionString = $"Server={server},{port};Database={database};User Id={userId};";
            if (!string.IsNullOrEmpty(password)) connectionString += $"Password={password};";
            connectionString += "TrustServerCertificate=True";
            return connectionString;
        }

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
        var random = new Random(42);
        var now = DateTime.UtcNow;

        var adminCount = (int)(TOTAL_USERS * ADMIN_PERCENT / 100);
        var hrCount = (int)(TOTAL_USERS * HR_PERCENT / 100);
        var managerCount = (int)(TOTAL_USERS * MANAGER_PERCENT / 100);
        var employeeCount = TOTAL_USERS - adminCount - hrCount - managerCount;

        Console.WriteLine($"  Admin: {adminCount:N0}, HR: {hrCount:N0}, Manager: {managerCount:N0}, Employee: {employeeCount:N0}");

        var userCounter = 0;
        var batch = new List<User>();

        // Helper to process batches
        async Task AddUserToBatch(User u) {
            batch.Add(u);
            if (batch.Count >= BATCH_SIZE) {
                await SaveBatchAsync(context, batch);
                batch.Clear();
                if (userCounter % 10000 == 0) Console.Write($"\r  Created {userCounter:N0} users...");
            }
        }

        // Create Admins
        for (int i = 0; i < adminCount; i++) await AddUserToBatch(CreateUser(++userCounter, "Admin", roles["Admin"], random, now));
        
        // Create HR
        for (int i = 0; i < hrCount; i++) await AddUserToBatch(CreateUser(++userCounter, "HR", roles["HR"], random, now));
        
        // Create Managers
        for (int i = 0; i < managerCount; i++) await AddUserToBatch(CreateUser(++userCounter, "Manager", roles["Manager"], random, now));

        // Create Employees
        Console.WriteLine("\n  Creating Employees...");
        for (int i = 0; i < employeeCount; i++) await AddUserToBatch(CreateUser(++userCounter, "Employee", roles["Employee"], random, now));

        // Save remaining
        if (batch.Any()) await SaveBatchAsync(context, batch);
        Console.WriteLine($"\n  Finished creating {userCounter:N0} users.");
    }

    private static User CreateUser(int userNumber, string roleName, Role role, Random random, DateTime now)
    {
        var firstName = $"User{userNumber}";
        var lastName = $"LastName{userNumber}";
        var email = $"user{userNumber}_{roleName.ToLower()}@taskedinbe.com";
        var password = $"user{userNumber}_{roleName.ToLower()}";

        // WorkFactor 4 is faster for seeding
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
        context.ChangeTracker.Clear();
    }

    private static async Task AssignManagersAsync(AppDbContext context)
    {
        Console.WriteLine("  Assigning managers (Building Chain of Command)...");
        var random = new Random(42);

        for (int deptId = 1; deptId <= TOTAL_DEPARTMENTS; deptId++)
        {
            // 1. Get Managers for this department
            var managers = await context.Users
                .Include(u => u.Role)
                .Where(u => u.DepartmentId == deptId && u.Role.Name == "Manager")
                .ToListAsync();

            if (!managers.Any()) continue;

            // 2. Build Hierarchy
            // Shuffle to randomize assignments
            managers = managers.OrderBy(x => random.Next()).ToList();

            // Tier 1: Department Head (1 Person)
            var deptHead = managers.First();
            
            // Tier 2: Senior Managers (~20% of remaining managers)
            var remainingManagers = managers.Skip(1).ToList();
            var seniorCount = (int)Math.Ceiling(remainingManagers.Count * 0.2);
            var seniorManagers = remainingManagers.Take(seniorCount).ToList();

            // Tier 3: Team Leads (Everyone else)
            var teamLeads = remainingManagers.Skip(seniorCount).ToList();

            var usersToUpdate = new List<User>();

            // Assign Tier 2 -> Tier 1
            foreach (var sm in seniorManagers)
            {
                sm.ManagerId = deptHead.Id;
                usersToUpdate.Add(sm);
            }

            // Assign Tier 3 -> Tier 2 (or Tier 1 if no Tier 2)
            foreach (var tl in teamLeads)
            {
                if (seniorManagers.Any())
                    tl.ManagerId = seniorManagers[random.Next(seniorManagers.Count)].Id;
                else
                    tl.ManagerId = deptHead.Id;
                
                usersToUpdate.Add(tl);
            }

            // 3. Assign Employees
            var employees = await context.Users
                .Include(u => u.Role)
                .Where(u => u.DepartmentId == deptId && 
                       u.Role.Name == "Employee" && 
                       u.ManagerId == null)
                .ToListAsync();

            if (employees.Any())
            {
                // Pool of supervisors: Prefer Team Leads -> Senior Managers -> Dept Head
                var supervisorPool = teamLeads.Any() ? teamLeads :
                                     seniorManagers.Any() ? seniorManagers :
                                     new List<User> { deptHead };

                int poolIndex = 0;
                foreach (var emp in employees)
                {
                    emp.ManagerId = supervisorPool[poolIndex % supervisorPool.Count].Id;
                    usersToUpdate.Add(emp);
                    poolIndex++;
                }
            }

            if (usersToUpdate.Any())
            {
                context.Users.UpdateRange(usersToUpdate);
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
            }

            if (deptId % 10 == 0) Console.Write($"\r  Processed Department {deptId}/{TOTAL_DEPARTMENTS}...");
        }
        Console.WriteLine("\n  Manager assignment complete.");
    }

    private static async Task CreateLeaveBalancesAsync(AppDbContext context)
    {
        Console.WriteLine("  Creating leave balances...");
        var currentYear = DateTime.UtcNow.Year;
        var now = DateTime.UtcNow;

        // Simplified logic: Give everyone 21 Annual and 7 Sick days
        // Fetching IDs only to save memory
        var userIds = await context.Users.Select(u => u.Id).ToListAsync();
        
        var batch = new List<LeaveBalance>();
        int count = 0;

        foreach (var userId in userIds)
        {
            batch.Add(new LeaveBalance { EmployeeId = userId, Type = LeaveType.Annual, Year = currentYear, TotalDays = 21, UsedDays = 0, CreatedAt = now, UpdatedAt = now });
            batch.Add(new LeaveBalance { EmployeeId = userId, Type = LeaveType.Sick, Year = currentYear, TotalDays = 7, UsedDays = 0, CreatedAt = now, UpdatedAt = now });

            if (batch.Count >= BATCH_SIZE)
            {
                context.LeaveBalances.AddRange(batch);
                await context.SaveChangesAsync();
                batch.Clear();
                context.ChangeTracker.Clear();
                count += BATCH_SIZE / 2;
                if (count % 50000 == 0) Console.Write($"\r  Created balances for {count:N0} users...");
            }
        }
        if (batch.Any())
        {
            context.LeaveBalances.AddRange(batch);
            await context.SaveChangesAsync();
        }
        Console.WriteLine("\n  Leave balances created.");
    }

    private static async Task CreateRandomLeavesAsync(AppDbContext context)
    {
        Console.WriteLine("  Creating random leave requests...");
        
        // Only fetch users who have managers
        var usersWithManagers = await context.Users
            .Where(u => u.ManagerId != null)
            .Select(u => new { u.Id, u.ManagerId })
            .ToListAsync();

        if (!usersWithManagers.Any()) return;

        var random = new Random(42);
        var batch = new List<LeaveRequest>();
        var now = DateTime.UtcNow;
        
        // 1. Create Conflicts (Same Manager, Same Date)
        var usersByManager = usersWithManagers.GroupBy(u => u.ManagerId).Where(g => g.Count() >= 2).ToList();
        int conflictCount = 0;
        
        // Create 50 conflict scenarios
        for (int i = 0; i < 50 && i < usersByManager.Count; i++)
        {
            var group = usersByManager[i];
            var employees = group.OrderBy(x => random.Next()).Take(3).ToList();
            var conflictDate = now.AddDays(random.Next(1, 100));

            foreach (var emp in employees)
            {
                batch.Add(new LeaveRequest {
                    EmployeeId = emp.Id, ManagerId = emp.ManagerId!.Value, Type = LeaveType.Annual,
                    StartDate = conflictDate, EndDate = conflictDate.AddDays(2), NumberOfDays = 2,
                    Status = LeaveStatus.PendingManager, Notes = "Conflict Test", CreatedAt = now, UpdatedAt = now
                });
            }
            conflictCount++;
        }
        
        await context.LeaveRequests.AddRangeAsync(batch);
        await context.SaveChangesAsync();
        batch.Clear();
        Console.WriteLine($"  Created {conflictCount} conflict scenarios.");

        // 2. Regular Leaves
        // 30% of users request leave
        foreach (var user in usersWithManagers)
        {
            if (random.NextDouble() > 0.3) continue;

            int days = random.Next(1, 5);
            var start = now.AddDays(random.Next(-30, 30));
            
            // Random Status
            var p = random.NextDouble();
            var status = p < 0.1 ? LeaveStatus.Cancelled : p < 0.2 ? LeaveStatus.Rejected : p < 0.4 ? LeaveStatus.Approved : p < 0.7 ? LeaveStatus.PendingHR : LeaveStatus.PendingManager;

            batch.Add(new LeaveRequest {
                EmployeeId = user.Id, ManagerId = user.ManagerId!.Value, Type = LeaveType.Annual,
                StartDate = start, EndDate = start.AddDays(days), NumberOfDays = days,
                Status = status, Notes = "Regular Leave", CreatedAt = now, UpdatedAt = now
            });

            if (batch.Count >= BATCH_SIZE) {
                await context.LeaveRequests.AddRangeAsync(batch);
                await context.SaveChangesAsync();
                batch.Clear();
                context.ChangeTracker.Clear();
            }
        }
        if (batch.Any()) await context.LeaveRequests.AddRangeAsync(batch);
        
        Console.WriteLine("  Random leaves created.");
    }
}