# Troubleshooting

## EmployeeHub says it is already running

Only one instance is allowed per Windows user. Close the other EmployeeHub window before launching another.

## Sign-in is rejected

Check the username and password, and confirm that the account is active. The initial demo accounts are listed in the README. An Admin can reset another account's password from **Users & Roles**.

## The database cannot be opened

Confirm that the current Windows user can write to `%LOCALAPPDATA%\EmployeeHub`. Do not move the database into Program Files or the extracted release folder. Technical details are written to `%LOCALAPPDATA%\EmployeeHub\Logs`.

## A position cannot be deactivated or moved

A position assigned to an active employee cannot be deactivated. Its department cannot be changed while any employee record is assigned to it. Reassign or deactivate the affected employee records first.

## A user cannot be deactivated or changed from Admin

At least one active Admin must remain. An Admin also cannot deactivate their own account.

## Resetting the demo

Close EmployeeHub and run `Reset-Demo.cmd`. Type `RESET` when prompted. If the application is open, close it and run the script again. Reset removes the database and SQLite journal files, not the Logs folder.

## Source build or publish errors

Install the .NET 10 SDK and restore packages from the configured official NuGet source. See [Build and publish](Build-and-Publish.md). The published Windows x64 folder runs without the SDK or a separately installed .NET runtime.
