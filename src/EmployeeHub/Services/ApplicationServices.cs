using System.Net.Mail;
using System.Text.RegularExpressions;
using EmployeeHub.Domain;
using EmployeeHub.Infrastructure;
using Microsoft.Data.Sqlite;

namespace EmployeeHub.Services;

public sealed class ApplicationServices(SqliteDataStore store, AppLogger logger)
{
    public AuthenticationService Authentication { get; } = new(store, logger);
    public EmployeeService Employees { get; } = new(store);
    public PositionService Positions { get; } = new(store);
    public UserService Users { get; } = new(store);
}

public sealed class AuthenticationService(SqliteDataStore store, AppLogger logger)
{
    public async Task<ApplicationUser?> SignInAsync(string username, string password)
    {
        try
        {
            return await store.AuthenticateAsync(username, password);
        }
        catch (FormatException exception)
        {
            logger.Error("A stored password hash could not be read.", exception);
            throw;
        }
        catch (InvalidDataException exception)
        {
            logger.Error("A stored password hash could not be verified.", exception);
            throw;
        }
    }
}

public sealed class EmployeeService(SqliteDataStore store)
{
    public Task<IReadOnlyList<EmployeeRecord>> ListAsync(EmployeeFilter filter) => store.GetEmployeesAsync(filter);

    public Task<EmployeeRecord?> GetAsync(int id) => store.GetEmployeeAsync(id);

    public async Task CreateAsync(ApplicationUser actor, EmployeeDraft draft)
    {
        EnsureEmployeeEditor(actor);
        await ValidateDraftAsync(draft, null);
        try
        {
            await store.CreateEmployeeAsync(draft);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new ValidationException("An employee with this email address already exists.");
        }
    }

    public async Task UpdateAsync(ApplicationUser actor, int id, EmployeeDraft draft)
    {
        EnsureEmployeeEditor(actor);
        var existing = await store.GetEmployeeAsync(id) ?? throw new ValidationException("The selected employee no longer exists.");
        await ValidateDraftAsync(draft, existing);
        try
        {
            await store.UpdateEmployeeAsync(id, draft);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new ValidationException("An employee with this email address already exists.");
        }
    }

    public async Task SetActiveAsync(ApplicationUser actor, int id, bool isActive)
    {
        EnsureEmployeeEditor(actor);
        await store.SetEmployeeActiveAsync(id, isActive);
    }

    private async Task ValidateDraftAsync(EmployeeDraft draft, EmployeeRecord? existing)
    {
        if (string.IsNullOrWhiteSpace(draft.FirstName))
        {
            throw new ValidationException("First name is required.");
        }
        if (string.IsNullOrWhiteSpace(draft.LastName))
        {
            throw new ValidationException("Last name is required.");
        }
        if (string.IsNullOrWhiteSpace(draft.Email))
        {
            throw new ValidationException("Email is required.");
        }
        var email = draft.Email.Trim();
        try
        {
            var parsed = new MailAddress(email);
            if (!string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase) ||
                !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant))
            {
                throw new ValidationException("Enter a valid email address.");
            }
        }
        catch (FormatException)
        {
            throw new ValidationException("Enter a valid email address.");
        }
        if (!IsDepartment(draft.Department))
        {
            throw new ValidationException("Select a valid department.");
        }
        if (draft.HireDate > DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ValidationException("Hire date cannot be in the future.");
        }

        var position = (await store.GetPositionsAsync()).FirstOrDefault(candidate => candidate.Id == draft.PositionId);
        if (position is null)
        {
            throw new ValidationException("Select a valid position.");
        }
        if (!string.Equals(position.Department, draft.Department, StringComparison.Ordinal))
        {
            throw new ValidationException("The selected position must belong to the selected department.");
        }
        if (!position.IsActive && (existing is null || existing.PositionId != position.Id))
        {
            throw new ValidationException("Only active positions can be assigned to an employee.");
        }
    }

    private static void EnsureEmployeeEditor(ApplicationUser actor)
    {
        if (actor.Role is not (UserRole.Admin or UserRole.HrManager))
        {
            throw new AuthorizationException("Your role does not allow employee changes.");
        }
    }

    private static bool IsDepartment(string department) =>
        department is "Engineering" or "HR" or "Finance";
}

public sealed class PositionService(SqliteDataStore store)
{
    public Task<IReadOnlyList<PositionRecord>> ListAsync() => store.GetPositionsAsync();

    public async Task CreateAsync(ApplicationUser actor, PositionDraft draft)
    {
        EnsureAdmin(actor);
        ValidateDraft(draft);
        try
        {
            await store.CreatePositionAsync(draft);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new ValidationException("A position with this title already exists in the selected department.");
        }
    }

    public async Task UpdateAsync(ApplicationUser actor, int id, PositionDraft draft)
    {
        EnsureAdmin(actor);
        ValidateDraft(draft);
        if (await store.IsPositionAssignedAsync(id, activeEmployeesOnly: false))
        {
            var positions = await store.GetPositionsAsync();
            var current = positions.FirstOrDefault(position => position.Id == id);
            if (current is not null && !string.Equals(current.Department, draft.Department, StringComparison.Ordinal))
            {
                throw new ValidationException("The department cannot be changed while employees are assigned to this position.");
            }
        }
        try
        {
            await store.UpdatePositionAsync(id, draft);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new ValidationException("A position with this title already exists in the selected department.");
        }
    }

    public async Task SetActiveAsync(ApplicationUser actor, int id, bool isActive)
    {
        EnsureAdmin(actor);
        if (!isActive && await store.IsPositionAssignedAsync(id, activeEmployeesOnly: true))
        {
            throw new ValidationException("This position cannot be deactivated while it is assigned to active employees.");
        }
        await store.SetPositionActiveAsync(id, isActive);
    }

    private static void ValidateDraft(PositionDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Title))
        {
            throw new ValidationException("Position title is required.");
        }
        if (draft.Department is not ("Engineering" or "HR" or "Finance"))
        {
            throw new ValidationException("Select a valid department.");
        }
    }

    private static void EnsureAdmin(ApplicationUser actor)
    {
        if (actor.Role != UserRole.Admin)
        {
            throw new AuthorizationException("Only an Admin can manage positions.");
        }
    }
}

public sealed class UserService(SqliteDataStore store)
{
    public async Task<IReadOnlyList<UserRecord>> ListAsync(ApplicationUser actor)
    {
        EnsureAdmin(actor);
        return await store.GetUsersAsync();
    }

    public async Task CreateAsync(ApplicationUser actor, string username, string password, UserRole role)
    {
        EnsureAdmin(actor);
        ValidateUsername(username);
        ValidatePassword(password);
        try
        {
            await store.CreateUserAsync(username, password, role);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new ValidationException("A user with this username already exists.");
        }
    }

    public async Task ChangeRoleAsync(ApplicationUser actor, int userId, UserRole newRole)
    {
        EnsureAdmin(actor);
        var users = await store.GetUsersAsync();
        var target = users.FirstOrDefault(user => user.Id == userId) ?? throw new ValidationException("The selected user no longer exists.");
        if (target.IsActive && target.Role == UserRole.Admin && newRole != UserRole.Admin &&
            users.Count(user => user.IsActive && user.Role == UserRole.Admin) <= 1)
        {
            throw new ValidationException("The application must have at least one active Admin.");
        }
        await store.UpdateUserRoleAsync(userId, newRole);
    }

    public async Task SetActiveAsync(ApplicationUser actor, int userId, bool isActive)
    {
        EnsureAdmin(actor);
        if (!isActive && actor.Id == userId)
        {
            throw new ValidationException("You cannot deactivate your own account.");
        }
        var users = await store.GetUsersAsync();
        var target = users.FirstOrDefault(user => user.Id == userId) ?? throw new ValidationException("The selected user no longer exists.");
        if (!isActive && target.IsActive && target.Role == UserRole.Admin &&
            users.Count(user => user.IsActive && user.Role == UserRole.Admin) <= 1)
        {
            throw new ValidationException("The application must have at least one active Admin.");
        }
        await store.SetUserActiveAsync(userId, isActive);
    }

    public async Task ResetPasswordAsync(ApplicationUser actor, int userId, string password)
    {
        EnsureAdmin(actor);
        ValidatePassword(password);
        if (!(await store.GetUsersAsync()).Any(user => user.Id == userId))
        {
            throw new ValidationException("The selected user no longer exists.");
        }
        await store.ResetUserPasswordAsync(userId, password);
    }

    private static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ValidationException("Username is required.");
        }
    }

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required.");
        }
        if (password.Length < 8)
        {
            throw new ValidationException("Password must contain at least 8 characters.");
        }
    }

    private static void EnsureAdmin(ApplicationUser actor)
    {
        if (actor.Role != UserRole.Admin)
        {
            throw new AuthorizationException("Only an Admin can manage application users and roles.");
        }
    }
}
