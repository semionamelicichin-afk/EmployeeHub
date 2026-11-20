namespace EmployeeHub.Domain;

public enum UserRole
{
    Admin,
    HrManager,
    Viewer
}

public sealed record ApplicationUser(int Id, string Username, UserRole Role, bool IsActive);

public sealed record EmployeeRecord(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Department,
    int PositionId,
    string PositionTitle,
    bool IsActive,
    DateOnly HireDate);

public sealed record EmployeeDraft(
    string FirstName,
    string LastName,
    string Email,
    string Department,
    int PositionId,
    DateOnly HireDate);

public sealed record PositionRecord(int Id, string Title, string Department, bool IsActive);

public sealed record PositionDraft(string Title, string Department);

public sealed record UserRecord(int Id, string Username, UserRole Role, bool IsActive);

public sealed record EmployeeFilter(string Search, string Department, string Status);

public sealed class ValidationException(string message) : Exception(message);

public sealed class AuthorizationException(string message) : Exception(message);
