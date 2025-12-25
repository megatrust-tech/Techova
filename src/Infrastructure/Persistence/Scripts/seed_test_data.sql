-- =============================================
-- SETUP: Variables
-- =============================================
DECLARE @CurrentYear INT = DATEPART(YEAR, GETUTCDATE());
-- Password: 'Admin@123'
DECLARE @PasswordHash NVARCHAR(MAX) = '$2b$10$t7oxiwchWGHa/B9w0AzrYO2WH2rQbA86YSuQjSTmwIrpC/0ZXN7V2'; 

-- =============================================
-- 1. Create Roles
-- =============================================

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Admin')
    INSERT INTO Roles (Name, Description, CreatedAt, UpdatedAt) VALUES ('Admin', 'System Administrator', GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'HR')
    INSERT INTO Roles (Name, Description, CreatedAt, UpdatedAt) VALUES ('HR', 'Human Resources', GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Manager')
    INSERT INTO Roles (Name, Description, CreatedAt, UpdatedAt) VALUES ('Manager', 'Department Manager', GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Employee')
    INSERT INTO Roles (Name, Description, CreatedAt, UpdatedAt) VALUES ('Employee', 'Standard Employee', GETUTCDATE(), GETUTCDATE());

-- Get Role IDs
DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Admin');
DECLARE @HRRoleId INT = (SELECT Id FROM Roles WHERE Name = 'HR');
DECLARE @ManagerRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Manager');
DECLARE @EmployeeRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Employee');

-- =============================================
-- 2. Create Users & Balances
-- =============================================

DECLARE @UsersToCreate TABLE (
    FirstName NVARCHAR(50),
    LastName NVARCHAR(50),
    Email NVARCHAR(100),
    RoleId INT,
    DepartmentId INT
);

INSERT INTO @UsersToCreate (FirstName, LastName, Email, RoleId, DepartmentId)
VALUES 
-- HQ (Dept 1)
('Mohamed', 'Ahmed', 'admin@taskedin.com', @AdminRoleId, 1),
('Rana', 'Ali', 'hr_rana@taskedin.com', @HRRoleId, 1),

-- IT Dept (Dept 2) - Manager: Sherif, Employees: Zeyad, Amr, Tarek
('Sherif', 'Hassan', 'manager_sherif@taskedin.com', @ManagerRoleId, 2), 
('Zeyad', 'Ibrahim', 'emp_zeyad@taskedin.com', @EmployeeRoleId, 2),     
('Amr', 'Youssef', 'emp_amr@taskedin.com', @EmployeeRoleId, 2),
('Tarek', 'Nabil', 'emp_tarek@taskedin.com', @EmployeeRoleId, 2),

-- Sales Dept (Dept 3) - Manager: Khaled, Employees: Mourad, Omar, Hany
('Khaled', 'Mahmoud', 'manager_khaled@taskedin.com', @ManagerRoleId, 3),
('Mourad', 'Said', 'emp_mourad@taskedin.com', @EmployeeRoleId, 3),
('Omar', 'Fathy', 'emp_omar@taskedin.com', @EmployeeRoleId, 3),
('Hany', 'Adel', 'emp_hany@taskedin.com', @EmployeeRoleId, 3);

-- Iterators
DECLARE @TargetFirstName NVARCHAR(50);
DECLARE @TargetLastName NVARCHAR(50); 
DECLARE @TargetEmail NVARCHAR(100); 
DECLARE @TargetRoleId INT;
DECLARE @TargetDeptId INT;

DECLARE UserCursor CURSOR FOR 
SELECT FirstName, LastName, Email, RoleId, DepartmentId FROM @UsersToCreate;

OPEN UserCursor;
FETCH NEXT FROM UserCursor INTO @TargetFirstName, @TargetLastName, @TargetEmail, @TargetRoleId, @TargetDeptId;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @InsertedUserId INT;

    -- 1. Insert User if not exists
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = @TargetEmail)
    BEGIN
        INSERT INTO Users (FirstName, LastName, Email, PasswordHash, RoleId, DepartmentId, TokenVersion, CreatedAt, UpdatedAt)
        VALUES (
            @TargetFirstName, 
            @TargetLastName, 
            @TargetEmail, 
            @PasswordHash, 
            @TargetRoleId, 
            @TargetDeptId, 
            0, 
            GETUTCDATE(), 
            GETUTCDATE()
        );
        
        SET @InsertedUserId = SCOPE_IDENTITY();
        PRINT 'Created User: ' + @TargetFirstName + ' (Dept ' + CAST(@TargetDeptId AS NVARCHAR) + ')';
    END
    ELSE
    BEGIN
        -- Update existing user's DepartmentId if it is null
        UPDATE Users 
        SET DepartmentId = @TargetDeptId 
        WHERE Email = @TargetEmail AND DepartmentId IS NULL;

        SELECT @InsertedUserId = Id FROM Users WHERE Email = @TargetEmail;
        PRINT 'User already exists: ' + @TargetFirstName;
    END

    -- 2. Insert Leave Balances (Enum: 0=Annual, 1=Sick, 2=Emergency, 3=Unpaid, 4=Maternity, 5=Paternity)
    IF NOT EXISTS (SELECT 1 FROM LeaveBalances WHERE EmployeeId = @InsertedUserId AND Year = @CurrentYear)
    BEGIN
        INSERT INTO LeaveBalances (EmployeeId, Year, Type, TotalDays, UsedDays, CreatedAt, UpdatedAt)
        VALUES 
        (@InsertedUserId, @CurrentYear, 0, 21, 0, GETUTCDATE(), GETUTCDATE()), -- Annual
        (@InsertedUserId, @CurrentYear, 1, 7, 0, GETUTCDATE(), GETUTCDATE()),  -- Sick
        (@InsertedUserId, @CurrentYear, 2, 7, 0, GETUTCDATE(), GETUTCDATE()),  -- Emergency
        (@InsertedUserId, @CurrentYear, 3, 7, 0, GETUTCDATE(), GETUTCDATE()),  -- Unpaid
        (@InsertedUserId, @CurrentYear, 4, 7, 0, GETUTCDATE(), GETUTCDATE()),  -- Maternity
        (@InsertedUserId, @CurrentYear, 5, 7, 0, GETUTCDATE(), GETUTCDATE());  -- Paternity

        PRINT '  -> Created Balances for: ' + @TargetFirstName;
    END

    FETCH NEXT FROM UserCursor INTO @TargetFirstName, @TargetLastName, @TargetEmail, @TargetRoleId, @TargetDeptId;
END;

CLOSE UserCursor;
DEALLOCATE UserCursor;

-- =============================================
-- 3. Verification
-- =============================================

SELECT 'Users' as [Table], Id, FirstName, LastName, Email, RoleId, DepartmentId FROM Users;
SELECT 'Balances' as [Table], * FROM LeaveBalances ORDER BY EmployeeId, Type;