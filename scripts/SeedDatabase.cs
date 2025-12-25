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
    private const int TOTAL_USERS = 500;
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

        var connectionString = configuration.GetConnectionString("DefaultConnection");

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
            // Ensure database is cleaned
            await context.Database.EnsureDeletedAsync();
            Console.WriteLine("✓ Old database deleted");

            // CRITICAL FIX: Use MigrateAsync instead of EnsureCreatedAsync
            // EnsureCreatedAsync creates tables but ignores Migrations History, causing future updates to fail.
            // MigrateAsync properly applies all migrations and records them in __EFMigrationsHistory.
            Console.WriteLine("Applying migrations...");
            await context.Database.MigrateAsync();
            Console.WriteLine("✓ Database created and migrations applied successfully");

            // Step 1: Create/Ensure Roles
            var roles = await EnsureRolesAsync(context);
            Console.WriteLine($"✓ Roles ensured ({roles.Count} roles)");

            // Step 2: Create Departments (we'll just use IDs 1-500, no actual Department table)
            Console.WriteLine($"✓ Using {TOTAL_DEPARTMENTS} department IDs (1-{TOTAL_DEPARTMENTS})");

            // Step 3: Create Users
            await CreateUsersAsync(context, roles);
            Console.WriteLine($"✓ Created {TOTAL_USERS:N0} users");

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

        // Get all managers (users with Manager role)
        var managers = await context.Users
            .Include(u => u.Role)
            .Where(u => u.Role.Name == "Manager")
            .OrderBy(u => u.Id)
            .ToListAsync();

        if (!managers.Any())
        {
            Console.WriteLine("  WARNING: No managers found to assign!");
            return;
        }

        // Get all employees and non-manager users who should have managers
        var employees = await context.Users
            .Include(u => u.Role)
            .Where(u => u.Role.Name != "Admin" && u.Role.Name != "Manager" && u.ManagerId == null)
            .OrderBy(u => u.Id)
            .ToListAsync();

        var random = new Random(42);
        var managerIndex = 0;
        var batch = new List<User>();

        foreach (var employee in employees)
        {
            // Assign a manager (round-robin through managers)
            var manager = managers[managerIndex % managers.Count];
            employee.ManagerId = manager.Id;
            batch.Add(employee);
            managerIndex++;

            if (batch.Count >= BATCH_SIZE)
            {
                context.Users.UpdateRange(batch);
                await context.SaveChangesAsync();
                batch.Clear();
                context.ChangeTracker.Clear();
                Console.WriteLine($"  Assigned managers to {managerIndex:N0} users...");
            }
        }

        // Update remaining
        if (batch.Any())
        {
            context.Users.UpdateRange(batch);
            await context.SaveChangesAsync();
        }

        // Some managers should also have managers (hierarchy)
        var managersWithoutManagers = await context.Users
            .Include(u => u.Role)
            .Where(u => u.Role.Name == "Manager" && u.ManagerId == null)
            .OrderBy(u => u.Id)
            .Skip(managers.Count / 10) // 10% of managers have managers
            .ToListAsync();

        var seniorManagers = managers.Take(managersWithoutManagers.Count).ToList();
        batch.Clear();

        for (int i = 0; i < managersWithoutManagers.Count && i < seniorManagers.Count; i++)
        {
            managersWithoutManagers[i].ManagerId = seniorManagers[i].Id;
            batch.Add(managersWithoutManagers[i]);
        }

        if (batch.Any())
        {
            context.Users.UpdateRange(batch);
            await context.SaveChangesAsync();
        }
    }

    private static async Task CreateLeaveBalancesAsync(AppDbContext context)
    {
        Console.WriteLine("  Creating leave balances...");
        var currentYear = DateTime.UtcNow.Year;

        var allUsers = await context.Users
            .Select(u => u.Id)
            .ToListAsync();

        var existingBalances = await context.LeaveBalances
            .Where(b => b.Year == currentYear)
            .Select(b => b.EmployeeId)
            .Distinct()
            .ToListAsync();

        var usersNeedingBalances = allUsers.Except(existingBalances).ToList();
        var batch = new List<LeaveBalance>();

        // Get leave type configurations (same logic as UserService)
        var policies = await context.LeaveTypeConfigs.ToListAsync();
        var leaveTypes = Enum.GetValues(typeof(LeaveType)).Cast<LeaveType>().ToList();
        var now = DateTime.UtcNow;

        foreach (var userId in usersNeedingBalances)
        {
            foreach (var leaveType in leaveTypes)
            {
                int totalDays;

                // Check if there's a policy for this leave type (same logic as UserService)
                if (policies.Any())
                {
                    var policy = policies.FirstOrDefault(p => p.LeaveTypeId == (int)leaveType);
                    if (policy != null)
                    {
                        totalDays = policy.DefaultBalance;
                    }
                    else
                    {
                        // Fallback if policy doesn't exist for this type
                        totalDays = leaveType == LeaveType.Annual ? 21 : 7;
                    }
                }
                else
                {
                    // Fallback if no policies exist (same as UserService)
                    totalDays = leaveType == LeaveType.Annual ? 21 : 7;
                }

                batch.Add(new LeaveBalance
                {
                    EmployeeId = userId,
                    Type = leaveType,
                    Year = currentYear,
                    TotalDays = totalDays,
                    UsedDays = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (batch.Count >= BATCH_SIZE)
            {
                context.LeaveBalances.AddRange(batch);
                await context.SaveChangesAsync();
                batch.Clear();
                context.ChangeTracker.Clear();
                Console.WriteLine($"  Created balances for {usersNeedingBalances.IndexOf(userId) + 1:N0} users...");
            }
        }

        if (batch.Any())
        {
            context.LeaveBalances.AddRange(batch);
            await context.SaveChangesAsync();
        }
    }

    private static async Task CreateRandomLeavesAsync(AppDbContext context)
    {
        Console.WriteLine("  Creating random leave requests...");

        // We need users who have managers to create valid leave flows
        // For performance, we'll select a subset of users to have leaves (e.g. 30%)
        // or just iterate all, but assign randomly.
        var usersWithManagers = await context.Users
            .Where(u => u.ManagerId != null)
            .Select(u => new { u.Id, u.ManagerId })
            .ToListAsync();

        if (!usersWithManagers.Any())
        {
            Console.WriteLine("  No users with managers found. Skipping leave generation.");
            return;
        }

        var random = new Random(42);
        var batch = new List<LeaveRequest>();
        var generatedCount = 0;
        var now = DateTime.UtcNow;

        // Sample notes for realism
        var sickNotes = new[] { "Not feeling well", "Flu symptoms", "Doctor appointment", "Migraine", "Food poisoning", "Fever", "I'm so sick", "Need rest" };
        var annualNotes = new[] { "Summer vacation", "Family trip", "Personal time off", "Traveling abroad", "Wedding attendance", "Burnout recovery", "Handling personal matters" };
        var otherNotes = new[] { "Emergency", "Personal reasons", "Unforeseen circumstances" };

        foreach (var user in usersWithManagers)
        {
            // 60% chance a user has no leave requests, 40% chance they have 1-3 requests
            if (random.NextDouble() > 0.4) continue;

            int leavesToCreate = random.Next(1, 4); // 1 to 3 leaves

            for (int i = 0; i < leavesToCreate; i++)
            {
                // Determine Leave Type
                var leaveTypes = Enum.GetValues(typeof(LeaveType));
                var leaveType = (LeaveType)leaveTypes.GetValue(random.Next(leaveTypes.Length))!;

                // Generate Note based on Type
                string note;
                if (leaveType.ToString().Contains("Sick"))
                    note = sickNotes[random.Next(sickNotes.Length)];
                else if (leaveType.ToString().Contains("Annual"))
                    note = annualNotes[random.Next(annualNotes.Length)];
                else
                    note = otherNotes[random.Next(otherNotes.Length)];

                // Generate Dates (randomly in the current year, past or future)
                int month = random.Next(1, 13);
                int day = random.Next(1, 28); // Safe day range
                var startDate = new DateTime(now.Year, month, day);
                var numberOfDays = random.Next(1, 6); // 1 to 5 days
                var endDate = startDate.AddDays(numberOfDays);

                // Determine Status
                // Distribution: 
                // 10% Cancelled
                // 10% Rejected
                // 30% Approved
                // 20% PendingHR
                // 30% PendingManager
                var statusRoll = random.NextDouble();
                LeaveStatus status;

                if (statusRoll < 0.1) status = LeaveStatus.Cancelled;
                else if (statusRoll < 0.2) status = LeaveStatus.Rejected;
                else if (statusRoll < 0.5) status = LeaveStatus.Approved;
                else if (statusRoll < 0.7) status = LeaveStatus.PendingHR;
                else status = LeaveStatus.PendingManager;

                var leaveRequest = new LeaveRequest
                {
                    EmployeeId = user.Id,
                    ManagerId = user.ManagerId!.Value,
                    Type = leaveType,
                    StartDate = startDate,
                    EndDate = endDate,
                    NumberOfDays = numberOfDays,
                    Status = status,
                    Notes = note,
                    AttachmentPath = null,
                    CreatedAt = now.AddDays(-random.Next(1, 30)), // Created slightly before now
                    UpdatedAt = now
                };

                batch.Add(leaveRequest);
                generatedCount++;
            }

            if (batch.Count >= BATCH_SIZE)
            {
                // We use Set<LeaveRequest>() or context.LeaveRequests depending on property availability
                // Assuming standard DbSet property name 'LeaveRequests' or generic Set method
                await context.Set<LeaveRequest>().AddRangeAsync(batch);
                await context.SaveChangesAsync();
                batch.Clear();
                context.ChangeTracker.Clear();
                Console.WriteLine($"  Generated {generatedCount:N0} leave requests...");
            }
        }

        if (batch.Any())
        {
            await context.Set<LeaveRequest>().AddRangeAsync(batch);
            await context.SaveChangesAsync();
            Console.WriteLine($"  Generated {generatedCount:N0} leave requests...");
        }
    }
}