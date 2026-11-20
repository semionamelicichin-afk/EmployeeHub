# EmployeeHub Architecture

EmployeeHub is a single-process Windows Forms application with three small layers:

1. **Desktop UI** — WinForms forms and dialogs under `src/EmployeeHub/UI` display sign-in, employee, position, and application-user workflows. Standard WinForms controls are used; no Telerik binaries or license are required.
2. **Application services** — `src/EmployeeHub/Services/ApplicationServices.cs` validates input and enforces employee, position, and user-role permissions before data changes.
3. **Data access** — `src/EmployeeHub/Infrastructure/SqliteDataStore.cs` opens parameterized SQLite commands and keeps related writes in transactions. The SQLite database is local to the current Windows user.

`Program.cs` creates `%LOCALAPPDATA%\EmployeeHub`, initializes and seeds the database, and keeps one application instance per Windows user. The app uses Windows' built-in `winsqlite3.dll` through the official `SQLitePCLRaw.bundle_winsqlite3` provider, avoiding a separately bundled vulnerable native SQLite build. Database foreign keys, fixed-department checks, normalized unique keys, and restricted position deletion provide additional consistency safeguards.

Password hashes are generated with PBKDF2-SHA256 and a unique random salt per account. Hashes are never shown in the UI. Logs are written to `%LOCALAPPDATA%\EmployeeHub\Logs`; authentication inputs and passwords are not logged.

## Data model

- `roles` contains the fixed Admin, HR Manager, and Viewer role names.
- `app_users` stores unique normalized usernames, password hashes, role references, and account status.
- `positions` stores unique title/department pairs and active status.
- `employees` stores personnel records and references positions independently of application accounts.

Seed inserts use stable IDs and `INSERT OR IGNORE`, so subsequent starts preserve edits instead of duplicating or overwriting records. Removing the database with `Reset-Demo.cmd` recreates the original seed dataset.
