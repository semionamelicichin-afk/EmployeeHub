using EmployeeHub.Domain;
using Microsoft.Data.Sqlite;

namespace EmployeeHub.Infrastructure;

public sealed class SqliteDataStore
{
    private readonly string _connectionString;

    public SqliteDataStore(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
            Pooling = true
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                PRAGMA journal_mode = WAL;
                CREATE TABLE IF NOT EXISTS roles (
                    role_id TEXT PRIMARY KEY,
                    role_name TEXT NOT NULL UNIQUE CHECK (role_name IN ('Admin', 'HR Manager', 'Viewer'))
                );
                CREATE TABLE IF NOT EXISTS app_users (
                    user_id INTEGER PRIMARY KEY,
                    username TEXT NOT NULL,
                    username_normalized TEXT NOT NULL UNIQUE,
                    password_hash TEXT NOT NULL,
                    role_id TEXT NOT NULL REFERENCES roles(role_id),
                    is_active INTEGER NOT NULL CHECK (is_active IN (0, 1))
                );
                CREATE TABLE IF NOT EXISTS positions (
                    position_id INTEGER PRIMARY KEY,
                    title TEXT NOT NULL,
                    department TEXT NOT NULL CHECK (department IN ('Engineering', 'HR', 'Finance')),
                    title_normalized TEXT NOT NULL,
                    is_active INTEGER NOT NULL CHECK (is_active IN (0, 1)),
                    UNIQUE (department, title_normalized)
                );
                CREATE TABLE IF NOT EXISTS employees (
                    employee_id INTEGER PRIMARY KEY,
                    first_name TEXT NOT NULL,
                    last_name TEXT NOT NULL,
                    email TEXT NOT NULL,
                    email_normalized TEXT NOT NULL UNIQUE,
                    department TEXT NOT NULL CHECK (department IN ('Engineering', 'HR', 'Finance')),
                    position_id INTEGER NOT NULL REFERENCES positions(position_id) ON DELETE RESTRICT,
                    is_active INTEGER NOT NULL CHECK (is_active IN (0, 1)),
                    hire_date TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_employees_department ON employees(department);
                CREATE INDEX IF NOT EXISTS ix_employees_status ON employees(is_active);
                """;
            await command.ExecuteNonQueryAsync();
        }

        await SeedAsync(connection);
    }

    public async Task<ApplicationUser?> AuthenticateAsync(string username, string password)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT user_id, username, role_name, is_active, password_hash
            FROM app_users INNER JOIN roles USING (role_id)
            WHERE username_normalized = $username
            """;
        command.Parameters.AddWithValue("$username", Normalize(username));
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || reader.GetInt64(3) != 1)
        {
            return null;
        }

        var hash = reader.GetString(4);
        if (!PasswordHasher.Verify(password, hash))
        {
            return null;
        }

        return new ApplicationUser(
            reader.GetInt32(0),
            reader.GetString(1),
            ParseRole(reader.GetString(2)),
            true);
    }

    public async Task<IReadOnlyList<EmployeeRecord>> GetEmployeesAsync(EmployeeFilter filter)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("(e.first_name LIKE $search COLLATE NOCASE OR e.last_name LIKE $search COLLATE NOCASE OR e.email LIKE $search COLLATE NOCASE)");
            command.Parameters.AddWithValue("$search", $"%{filter.Search.Trim()}%");
        }
        if (filter.Department != "All departments")
        {
            conditions.Add("e.department = $department");
            command.Parameters.AddWithValue("$department", filter.Department);
        }
        if (filter.Status != "All statuses")
        {
            conditions.Add("e.is_active = $active");
            command.Parameters.AddWithValue("$active", filter.Status == "Active" ? 1 : 0);
        }

        command.CommandText = $"""
            SELECT e.employee_id, e.first_name, e.last_name, e.email, e.department,
                   p.position_id, p.title, e.is_active, e.hire_date
            FROM employees e JOIN positions p ON p.position_id = e.position_id
            {(conditions.Count == 0 ? "" : "WHERE " + string.Join(" AND ", conditions))}
            ORDER BY e.last_name COLLATE NOCASE, e.first_name COLLATE NOCASE, e.employee_id
            """;

        var employees = new List<EmployeeRecord>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            employees.Add(new EmployeeRecord(
                reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetInt32(5), reader.GetString(6),
                reader.GetInt64(7) == 1, DateOnly.Parse(reader.GetString(8))));
        }
        return employees;
    }

    public async Task<EmployeeRecord?> GetEmployeeAsync(int employeeId)
    {
        var all = await GetEmployeesAsync(new EmployeeFilter(string.Empty, "All departments", "All statuses"));
        return all.FirstOrDefault(employee => employee.Id == employeeId);
    }

    public async Task<IReadOnlyList<PositionRecord>> GetPositionsAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT position_id, title, department, is_active FROM positions ORDER BY department, title COLLATE NOCASE";
        await using var reader = await command.ExecuteReaderAsync();
        var positions = new List<PositionRecord>();
        while (await reader.ReadAsync())
        {
            positions.Add(new PositionRecord(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3) == 1));
        }
        return positions;
    }

    public async Task<IReadOnlyList<UserRecord>> GetUsersAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.user_id, u.username, r.role_name, u.is_active
            FROM app_users u JOIN roles r USING (role_id)
            ORDER BY u.username COLLATE NOCASE
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var users = new List<UserRecord>();
        while (await reader.ReadAsync())
        {
            users.Add(new UserRecord(reader.GetInt32(0), reader.GetString(1), ParseRole(reader.GetString(2)), reader.GetInt64(3) == 1));
        }
        return users;
    }

    public async Task CreateEmployeeAsync(EmployeeDraft draft)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = """
            INSERT INTO employees (first_name, last_name, email, email_normalized, department, position_id, is_active, hire_date)
            VALUES ($first, $last, $email, $emailNormalized, $department, $position, 1, $hireDate)
            """;
        AddEmployeeParameters(command, draft);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    public async Task UpdateEmployeeAsync(int employeeId, EmployeeDraft draft)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = """
            UPDATE employees
            SET first_name = $first, last_name = $last, email = $email,
                email_normalized = $emailNormalized, department = $department,
                position_id = $position, hire_date = $hireDate
            WHERE employee_id = $id
            """;
        AddEmployeeParameters(command, draft);
        command.Parameters.AddWithValue("$id", employeeId);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    public async Task SetEmployeeActiveAsync(int employeeId, bool isActive)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE employees SET is_active = $active WHERE employee_id = $id";
        command.Parameters.AddWithValue("$active", isActive ? 1 : 0);
        command.Parameters.AddWithValue("$id", employeeId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task CreatePositionAsync(PositionDraft draft)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO positions (title, department, title_normalized, is_active)
            VALUES ($title, $department, $normalized, 1)
            """;
        AddPositionParameters(command, draft);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdatePositionAsync(int positionId, PositionDraft draft)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = """
            UPDATE positions
            SET title = $title, department = $department, title_normalized = $normalized
            WHERE position_id = $id
            """;
        AddPositionParameters(command, draft);
        command.Parameters.AddWithValue("$id", positionId);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    public async Task<bool> IsPositionAssignedAsync(int positionId, bool activeEmployeesOnly)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = activeEmployeesOnly
            ? "SELECT EXISTS(SELECT 1 FROM employees WHERE position_id = $id AND is_active = 1)"
            : "SELECT EXISTS(SELECT 1 FROM employees WHERE position_id = $id)";
        command.Parameters.AddWithValue("$id", positionId);
        return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
    }

    public async Task SetPositionActiveAsync(int positionId, bool isActive)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE positions SET is_active = $active WHERE position_id = $id";
        command.Parameters.AddWithValue("$active", isActive ? 1 : 0);
        command.Parameters.AddWithValue("$id", positionId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task CreateUserAsync(string username, string password, UserRole role)
    {
        var passwordHash = await Task.Run(() => PasswordHasher.Hash(password));
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO app_users (username, username_normalized, password_hash, role_id, is_active)
            VALUES ($username, $normalized, $hash, $role, 1)
            """;
        command.Parameters.AddWithValue("$username", username.Trim());
        command.Parameters.AddWithValue("$normalized", Normalize(username));
        command.Parameters.AddWithValue("$hash", passwordHash);
        command.Parameters.AddWithValue("$role", RoleId(role));
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateUserRoleAsync(int userId, UserRole role)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE app_users SET role_id = $role WHERE user_id = $id";
        command.Parameters.AddWithValue("$role", RoleId(role));
        command.Parameters.AddWithValue("$id", userId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task SetUserActiveAsync(int userId, bool isActive)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE app_users SET is_active = $active WHERE user_id = $id";
        command.Parameters.AddWithValue("$active", isActive ? 1 : 0);
        command.Parameters.AddWithValue("$id", userId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task ResetUserPasswordAsync(int userId, string password)
    {
        var passwordHash = await Task.Run(() => PasswordHasher.Hash(password));
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE app_users SET password_hash = $hash WHERE user_id = $id";
        command.Parameters.AddWithValue("$hash", passwordHash);
        command.Parameters.AddWithValue("$id", userId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task SeedAsync(SqliteConnection connection)
    {
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO roles(role_id, role_name) VALUES ('admin', 'Admin'), ('hr-manager', 'HR Manager'), ('viewer', 'Viewer');
            INSERT OR IGNORE INTO app_users(user_id, username, username_normalized, password_hash, role_id, is_active)
            VALUES
                (1, 'admin', 'admin', $adminHash, 'admin', 1),
                (2, 'hr.manager', 'hr.manager', $hrHash, 'hr-manager', 1),
                (3, 'viewer', 'viewer', $viewerHash, 'viewer', 1);
            """;
        command.Parameters.AddWithValue("$adminHash", PasswordHasher.Hash("Admin123!"));
        command.Parameters.AddWithValue("$hrHash", PasswordHasher.Hash("Hr123!"));
        command.Parameters.AddWithValue("$viewerHash", PasswordHasher.Hash("Viewer123!"));
        await command.ExecuteNonQueryAsync();

        var positions = new[]
        {
            new PositionRecord(1, "QA Engineer", "Engineering", true),
            new PositionRecord(2, "Senior QA Engineer", "Engineering", true),
            new PositionRecord(3, "Software Developer", "Engineering", true),
            new PositionRecord(4, "Engineering Manager", "Engineering", true),
            new PositionRecord(5, "HR Specialist", "HR", true),
            new PositionRecord(6, "HR Manager", "HR", true),
            new PositionRecord(7, "Accountant", "Finance", true),
            new PositionRecord(8, "Financial Analyst", "Finance", true)
        };
        foreach (var position in positions)
        {
            await using var positionCommand = connection.CreateCommand();
            positionCommand.Transaction = (SqliteTransaction)transaction;
            positionCommand.CommandText = """
                INSERT OR IGNORE INTO positions(position_id, title, department, title_normalized, is_active)
                VALUES ($id, $title, $department, $normalized, 1)
                """;
            positionCommand.Parameters.AddWithValue("$id", position.Id);
            positionCommand.Parameters.AddWithValue("$title", position.Title);
            positionCommand.Parameters.AddWithValue("$department", position.Department);
            positionCommand.Parameters.AddWithValue("$normalized", Normalize(position.Title));
            await positionCommand.ExecuteNonQueryAsync();
        }

        var employees = new[]
        {
            (1, "Ava", "Morgan", "ava.morgan@example.test", "Engineering", 1, true, "2021-03-15"),
            (2, "Liam", "Chen", "liam.chen@example.test", "Engineering", 3, true, "2020-07-06"),
            (3, "Mia", "Patel", "mia.patel@example.test", "Engineering", 2, true, "2022-01-10"),
            (4, "Noah", "Morgan", "noah.morgan@example.test", "Engineering", 3, true, "2019-05-20"),
            (5, "Olivia", "Reed", "olivia.reed@example.test", "Engineering", 4, true, "2018-11-12"),
            (6, "Ethan", "Brooks", "ethan.brooks@example.test", "Engineering", 1, false, "2017-02-13"),
            (7, "Sophia", "Chen", "sophia.chen@example.test", "Engineering", 3, true, "2023-04-03"),
            (8, "Lucas", "Morgan", "lucas.morgan@example.test", "Engineering", 2, false, "2016-09-19"),
            (9, "Isla", "Bennett", "isla.bennett@example.test", "HR", 5, true, "2021-06-07"),
            (10, "Mason", "Diaz", "mason.diaz@example.test", "HR", 6, true, "2019-08-26"),
            (11, "Amelia", "Reed", "amelia.reed@example.test", "HR", 5, true, "2022-10-17"),
            (12, "Logan", "Patel", "logan.patel@example.test", "HR", 5, false, "2015-03-02"),
            (13, "Harper", "Brooks", "harper.brooks@example.test", "Finance", 7, true, "2020-02-24"),
            (14, "James", "Bennett", "james.bennett@example.test", "Finance", 8, true, "2021-12-01"),
            (15, "Evelyn", "Diaz", "evelyn.diaz@example.test", "Finance", 7, true, "2018-04-16"),
            (16, "Benjamin", "Reed", "benjamin.reed@example.test", "Finance", 8, false, "2014-07-21"),
            (17, "Charlotte", "Chen", "charlotte.chen@example.test", "Finance", 7, true, "2023-02-06"),
            (18, "Henry", "Morgan", "henry.morgan@example.test", "HR", 5, true, "2024-01-15")
        };
        foreach (var employee in employees)
        {
            await using var employeeCommand = connection.CreateCommand();
            employeeCommand.Transaction = (SqliteTransaction)transaction;
            employeeCommand.CommandText = """
                INSERT OR IGNORE INTO employees
                    (employee_id, first_name, last_name, email, email_normalized, department, position_id, is_active, hire_date)
                VALUES ($id, $first, $last, $email, $normalized, $department, $position, $active, $hireDate)
                """;
            employeeCommand.Parameters.AddWithValue("$id", employee.Item1);
            employeeCommand.Parameters.AddWithValue("$first", employee.Item2);
            employeeCommand.Parameters.AddWithValue("$last", employee.Item3);
            employeeCommand.Parameters.AddWithValue("$email", employee.Item4);
            employeeCommand.Parameters.AddWithValue("$normalized", Normalize(employee.Item4));
            employeeCommand.Parameters.AddWithValue("$department", employee.Item5);
            employeeCommand.Parameters.AddWithValue("$position", employee.Item6);
            employeeCommand.Parameters.AddWithValue("$active", employee.Item7 ? 1 : 0);
            employeeCommand.Parameters.AddWithValue("$hireDate", employee.Item8);
            await employeeCommand.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    private async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static void AddEmployeeParameters(SqliteCommand command, EmployeeDraft draft)
    {
        command.Parameters.AddWithValue("$first", draft.FirstName.Trim());
        command.Parameters.AddWithValue("$last", draft.LastName.Trim());
        command.Parameters.AddWithValue("$email", draft.Email.Trim());
        command.Parameters.AddWithValue("$emailNormalized", Normalize(draft.Email));
        command.Parameters.AddWithValue("$department", draft.Department);
        command.Parameters.AddWithValue("$position", draft.PositionId);
        command.Parameters.AddWithValue("$hireDate", draft.HireDate.ToString("yyyy-MM-dd"));
    }

    private static void AddPositionParameters(SqliteCommand command, PositionDraft draft)
    {
        command.Parameters.AddWithValue("$title", draft.Title.Trim());
        command.Parameters.AddWithValue("$department", draft.Department);
        command.Parameters.AddWithValue("$normalized", Normalize(draft.Title));
    }

    public static string Normalize(string value) => value.Trim().ToLowerInvariant();

    public static string RoleId(UserRole role) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.HrManager => "hr-manager",
        UserRole.Viewer => "viewer",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    public static string RoleName(UserRole role) => role switch
    {
        UserRole.Admin => "Admin",
        UserRole.HrManager => "HR Manager",
        UserRole.Viewer => "Viewer",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    public static UserRole ParseRole(string role) => role switch
    {
        "Admin" => UserRole.Admin,
        "HR Manager" => UserRole.HrManager,
        "Viewer" => UserRole.Viewer,
        _ => throw new InvalidDataException($"Unsupported application role: {role}")
    };
}
