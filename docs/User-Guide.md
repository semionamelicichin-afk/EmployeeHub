# EmployeeHub User Guide

## Sign in and sign out

Enter an active application user's username and password on the sign-in window. Press **Enter** or choose **Sign in**. Invalid or inactive accounts receive a clear sign-in message. Passwords are masked and stored as PBKDF2 password hashes. Choose **Log out** in the main window to clear the signed-in session and return to sign-in. Closing the sign-in window exits the application.

## Employees

The Employees tab lists employee ID, name, email, department, position, employment status, and hire date. Double-click a row or choose **Details** to see the full record. Use the search box for a first name, last name, or email, and combine it with the department and employment-status filters. Choose **Search** to apply them together; **Refresh** reloads the list without clearing them. Select a column heading to sort that column.

Admins and HR Managers can create and edit records, and deactivate or reactivate them. Deactivation retains the employee record. First name, last name, email, department, position, and a non-future hire date are required. Email addresses are unique without regard to case or surrounding whitespace. A position must belong to the selected department. New assignments require an active position; an existing inactive position can remain assigned when editing other employee details.

## Positions

All roles can view positions. Admins can create, edit, deactivate, and reactivate positions. Departments are fixed to Engineering, HR, and Finance. A title must be unique within its department. A position assigned to an active employee cannot be deactivated; its department cannot be changed while any employee is assigned to it.

## Users & Roles

Only Admins can open this tab. Create an application user with a unique username, initial password, and one of the fixed roles: Admin, HR Manager, or Viewer. Passwords must contain at least eight characters. Admins can change roles, activate/deactivate accounts, and reset passwords. Inactive users cannot sign in. The application protects the last active Admin and prevents an Admin from deactivating their own account.

Creating an application user does not create an employee record. Employee status and application-account status are independent.

## Reset demo data

Close the app and run `Reset-Demo.cmd` from the extracted folder. The script refuses to run while EmployeeHub is open and requires typing `RESET`. It deletes only the app database and related SQLite files under `%LOCALAPPDATA%\EmployeeHub`; logs are retained. The initial data is recreated the next time EmployeeHub starts.
