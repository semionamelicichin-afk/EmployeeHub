# EmployeeHub

EmployeeHub is a small Windows desktop application for managing employee records, job positions, and application users. It uses C# WinForms, application services for validation and access control, and a local SQLite database. The UI uses standard Windows Forms controls; 

## Quick start

1. Extract `EmployeeHub-Windows-x64.zip` to a folder.
2. Double-click `EmployeeHub.exe` (or `Start-EmployeeHub.cmd`).
3. Sign in with one of the demo accounts below.

The published Windows x64 application is self-contained and does not require the .NET SDK, Visual Studio, Docker, an installed database, Internet access, or administrator privileges. Windows 10 or later is required.

## Demo accounts

| Username | Password | Role |
| --- | --- | --- |
| `admin` | `Admin123!` | Admin |
| `hr.manager` | `Hr123!` | HR Manager |
| `viewer` | `Viewer123!` | Viewer |

Change demo passwords from **Users & Roles** after signing in as an Admin.

## Role permissions

| Role | Employees | Positions | Application users |
| --- | --- | --- | --- |
| Admin | View, create, edit, deactivate, and reactivate | View, create, edit, deactivate, and reactivate | Create users, change roles, activate/deactivate accounts, and reset passwords |
| HR Manager | View, create, edit, deactivate, and reactivate | View only | No access |
| Viewer | View, search, filter, and sort | View only | No access |

Permissions are enforced by the application services as well as by the interface. An employee record is separate from an application user account.

## Data and reset

The database and log files are stored under `%LOCALAPPDATA%\EmployeeHub`; logs are in its `Logs` subdirectory. The database is created and seeded automatically on the first launch. EmployeeHub uses Windows' built-in SQLite library (`winsqlite3.dll`) and does not install a database.

To restore the initial demo dataset:

1. Close EmployeeHub.
2. Run `Reset-Demo.cmd`.
3. Type `RESET` when prompted.
4. Start EmployeeHub again.

The reset removes only EmployeeHub's database and SQLite journal files. Logs are retained. The next launch recreates the original demo data; back up the database first if you need to keep changes.

## Build from source

Source builds require the .NET 10 SDK and Windows with the Windows Desktop targeting pack. Run `dotnet restore .\EmployeeHub.sln` and `dotnet build .\EmployeeHub.sln --configuration Release`; see [Build and publish](docs/Build-and-Publish.md) to publish a self-contained release, and [Architecture](docs/Architecture.md) for the application layers.

## Documentation

- [User guide](docs/User-Guide.md)
- [Architecture](docs/Architecture.md)
- [Build and publish](docs/Build-and-Publish.md)
- [Troubleshooting](docs/Troubleshooting.md)
