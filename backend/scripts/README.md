# Database Seeding Script

This script seeds the database with:

- **500 departments** (using IDs 1-500)
- **500,000 users** with various roles
- **Manager assignments** (hierarchical structure)
- **Leave balances** for all users

## User Distribution

- **Admin**: ~500 users (0.1%)
- **HR**: ~2,500 users (0.5%)
- **Manager**: ~25,000 users (5%)
- **Employee**: ~472,000 users (94.4%)

## User Credentials

All users follow this pattern:

- **Email**: `user{number}_{role}@taskedinbe.com`
  - Example: `user1_admin@taskedinbe.com`, `user25000_manager@taskedinbe.com`
- **Password**: `user{number}_{role}` (BCrypt hashed)
  - Example: `user1_admin`, `user25000_manager`

## Roles Created

The script creates the following roles if they don't exist:

- Admin
- HR
- Manager
- Employee
- Director
- VP
- CEO
- Intern

## Running the Script

### Option 1: Using dotnet run (Recommended)

```bash
dotnet run --project taskedin-be.csproj -- seed
```

### Option 2: Using PowerShell script

```powershell
.\scripts\run-seed.ps1
```

### Option 3: Using Bash script

```bash
chmod +x scripts/run-seed.sh
./scripts/run-seed.sh
```

## Performance Notes

- The script uses batch inserts (1000 records per batch) for optimal performance
- Progress is displayed during execution
- Estimated runtime: 10-30 minutes depending on database performance
- Memory usage is optimized by clearing change tracker after each batch

## Important Notes

1. **Database Connection**: Ensure your connection string is correctly configured in `src/appsettings.json`
2. **Existing Data**: The script will create roles if they don't exist, but will skip creating users/balances if they already exist
3. **Manager Assignment**: Employees and non-admin users are assigned managers. Some managers also have managers (hierarchical structure)
4. **Department Assignment**: Users are randomly assigned to departments 1-500
5. **Leave Balances**: All users get leave balances for the current year:
   - Annual: 21 days
   - Other types: 7 days each

## Verification

After running the script, you can verify the data:

```sql
-- Count users by role
SELECT r.Name, COUNT(u.Id) as UserCount
FROM Users u
INNER JOIN Roles r ON u.RoleId = r.Id
GROUP BY r.Name;

-- Count users with managers
SELECT COUNT(*) as UsersWithManagers
FROM Users
WHERE ManagerId IS NOT NULL;

-- Count leave balances
SELECT COUNT(*) as TotalLeaveBalances
FROM LeaveBalances;
```
