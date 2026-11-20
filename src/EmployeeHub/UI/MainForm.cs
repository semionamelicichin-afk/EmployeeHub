using System.Drawing;
using System.Windows.Forms;
using EmployeeHub.Domain;
using EmployeeHub.Infrastructure;
using EmployeeHub.Services;
using Microsoft.Data.Sqlite;

namespace EmployeeHub.UI;

internal sealed class MainForm : Form
{
    private static readonly string[] Departments = ["All departments", "Engineering", "HR", "Finance"];
    private static readonly string[] Statuses = ["All statuses", "Active", "Inactive"];
    private readonly ApplicationUser _user;
    private readonly ApplicationServices _services;
    private readonly AppLogger _logger;
    private readonly DataGridView _employeeGrid;
    private readonly DataGridView _positionGrid;
    private readonly DataGridView? _userGrid;
    private readonly TextBox _search;
    private readonly ComboBox _departmentFilter;
    private readonly ComboBox _statusFilter;
    private readonly Label _employeeStatus;
    private readonly Label _positionStatus;
    private readonly Label? _userStatus;
    private readonly Button _editEmployee;
    private readonly Button _deactivateEmployee;
    private readonly Button _reactivateEmployee;
    private readonly Button? _editPosition;
    private readonly Button? _deactivatePosition;
    private readonly Button? _reactivatePosition;
    private readonly Button? _changeUserRole;
    private readonly Button? _activateUser;
    private readonly Button? _deactivateUser;
    private readonly Button? _resetUserPassword;

    public event Action? LogoutRequested;

    public MainForm(ApplicationUser user, ApplicationServices services, AppLogger logger)
    {
        _user = user;
        _services = services;
        _logger = logger;
        Text = "EmployeeHub";
        Name = "MainForm";
        AccessibleName = "EmployeeHub main window";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        Size = new Size(1180, 760);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var header = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8) };
        var currentUser = new Label
        {
            Name = "CurrentUserLabel",
            AccessibleName = "Signed-in user",
            Text = $"{user.Username}  |  {SqliteDataStore.RoleName(user.Role)}",
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold)
        };
        var logout = FormUtilities.CreateButton("LogoutButton", "Log out", "Sign out and return to the login window");
        logout.Dock = DockStyle.Right;
        logout.Click += (_, _) => LogoutRequested?.Invoke();
        header.Controls.Add(currentUser);
        header.Controls.Add(logout);

        var tabs = new TabControl
        {
            Name = "MainNavigationTabs",
            AccessibleName = "Main navigation",
            Dock = DockStyle.Fill,
            Padding = new Point(14, 5)
        };
        var employeesPage = new TabPage("Employees") { Name = "EmployeesTab", AccessibleName = "Employees" };
        var positionsPage = new TabPage("Positions") { Name = "PositionsTab", AccessibleName = "Positions" };
        _employeeGrid = FormUtilities.CreateGrid("EmployeeGrid");
        AddEmployeeColumns(_employeeGrid);
        _employeeGrid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                await ShowEmployeeDetailsAsync();
            }
        };
        _employeeGrid.SelectionChanged += (_, _) => UpdateEmployeeActions();
        _positionGrid = FormUtilities.CreateGrid("PositionGrid");
        AddPositionColumns(_positionGrid);
        _positionGrid.SelectionChanged += (_, _) => UpdatePositionActions();

        var employeeLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10, 4, 10, 4) };
        employeeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        employeeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        employeeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var employeeToolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        employeeToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        employeeToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var employeeFilterRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        var employeeActionRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _search = new TextBox
        {
            Name = "EmployeeSearchTextBox",
            AccessibleName = "Search employees by name or email",
            AccessibleDescription = "Enter a first name, last name, or email address",
            Width = 220,
            Margin = new Padding(4)
        };
        _departmentFilter = FormUtilities.CreateDropDown("DepartmentFilterComboBox", "Filter by department", Departments);
        _departmentFilter.Width = 150;
        _statusFilter = FormUtilities.CreateDropDown("EmploymentStatusFilterComboBox", "Filter by employment status", Statuses);
        _statusFilter.Width = 150;
        var searchButton = FormUtilities.CreateButton("SearchEmployeesButton", "Search", "Apply employee search and filters");
        var refreshEmployees = FormUtilities.CreateButton("RefreshEmployeesButton", "Refresh", "Refresh the employee list");
        var createEmployee = FormUtilities.CreateButton("CreateEmployeeButton", "New employee", "Create an employee record");
        _editEmployee = FormUtilities.CreateButton("EditEmployeeButton", "Edit", "Edit the selected employee");
        var detailsEmployee = FormUtilities.CreateButton("ViewEmployeeButton", "Details", "View the selected employee details");
        _deactivateEmployee = FormUtilities.CreateButton("DeactivateEmployeeButton", "Deactivate", "Deactivate the selected employee");
        _reactivateEmployee = FormUtilities.CreateButton("ReactivateEmployeeButton", "Reactivate", "Reactivate the selected employee");
        searchButton.Click += async (_, _) => await LoadEmployeesAsync();
        refreshEmployees.Click += async (_, _) => await LoadEmployeesAsync();
        createEmployee.Click += async (_, _) => await CreateEmployeeAsync();
        _editEmployee.Click += async (_, _) => await EditEmployeeAsync();
        detailsEmployee.Click += async (_, _) => await ShowEmployeeDetailsAsync();
        _deactivateEmployee.Click += async (_, _) => await ChangeEmployeeStatusAsync(false);
        _reactivateEmployee.Click += async (_, _) => await ChangeEmployeeStatusAsync(true);
        employeeFilterRow.Controls.AddRange([_search, _departmentFilter, _statusFilter, searchButton, refreshEmployees]);
        employeeActionRow.Controls.AddRange([
            createEmployee, _editEmployee, detailsEmployee, _deactivateEmployee, _reactivateEmployee
        ]);
        employeeToolbar.Controls.Add(employeeFilterRow, 0, 0);
        employeeToolbar.Controls.Add(employeeActionRow, 0, 1);
        _employeeStatus = CreateStatusLabel();
        employeeLayout.Controls.Add(employeeToolbar, 0, 0);
        employeeLayout.Controls.Add(_employeeGrid, 0, 1);
        employeeLayout.Controls.Add(_employeeStatus, 0, 2);
        employeesPage.Controls.Add(employeeLayout);

        var positionLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
        positionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        positionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        positionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var positionToolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var refreshPositions = FormUtilities.CreateButton("RefreshPositionsButton", "Refresh", "Refresh the position list");
        _positionStatus = CreateStatusLabel();
        positionToolbar.Controls.Add(refreshPositions);
        if (user.Role == UserRole.Admin)
        {
            var createPosition = FormUtilities.CreateButton("CreatePositionButton", "New position", "Create a position");
            _editPosition = FormUtilities.CreateButton("EditPositionButton", "Edit", "Edit the selected position");
            _deactivatePosition = FormUtilities.CreateButton("DeactivatePositionButton", "Deactivate", "Deactivate the selected position");
            _reactivatePosition = FormUtilities.CreateButton("ReactivatePositionButton", "Reactivate", "Reactivate the selected position");
            createPosition.Click += async (_, _) => await CreatePositionAsync();
            _editPosition.Click += async (_, _) => await EditPositionAsync();
            _deactivatePosition.Click += async (_, _) => await ChangePositionStatusAsync(false);
            _reactivatePosition.Click += async (_, _) => await ChangePositionStatusAsync(true);
            positionToolbar.Controls.AddRange([createPosition, _editPosition, _deactivatePosition, _reactivatePosition]);
        }
        else
        {
            positionToolbar.Controls.Add(new Label
            {
                Text = "Positions are read-only for your role.",
                AccessibleName = "Position permissions",
                AutoSize = true,
                Margin = new Padding(12, 12, 4, 4)
            });
        }
        refreshPositions.Click += async (_, _) => await LoadPositionsAsync();
        positionLayout.Controls.Add(positionToolbar, 0, 0);
        positionLayout.Controls.Add(_positionGrid, 0, 1);
        positionLayout.Controls.Add(_positionStatus, 0, 2);
        positionsPage.Controls.Add(positionLayout);

        tabs.TabPages.Add(employeesPage);
        tabs.TabPages.Add(positionsPage);
        if (user.Role == UserRole.Admin)
        {
            var usersPage = new TabPage("Users & Roles") { Name = "UsersRolesTab", AccessibleName = "Users and roles" };
            _userGrid = FormUtilities.CreateGrid("UserGrid");
            AddUserColumns(_userGrid);
            _userGrid.SelectionChanged += (_, _) => UpdateUserActions();
            var userLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
            userLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            userLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            userLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            var userToolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            var createUser = FormUtilities.CreateButton("CreateUserButton", "New user", "Create an application user");
            _changeUserRole = FormUtilities.CreateButton("ChangeUserRoleButton", "Change role", "Change the selected user's role");
            _activateUser = FormUtilities.CreateButton("ActivateUserButton", "Activate", "Activate the selected application user");
            _deactivateUser = FormUtilities.CreateButton("DeactivateUserButton", "Deactivate", "Deactivate the selected application user");
            _resetUserPassword = FormUtilities.CreateButton("ResetUserPasswordButton", "Reset password", "Set a new password for the selected user");
            var refreshUsers = FormUtilities.CreateButton("RefreshUsersButton", "Refresh", "Refresh the application user list");
            createUser.Click += async (_, _) => await CreateUserAsync();
            _changeUserRole.Click += async (_, _) => await ChangeUserRoleAsync();
            _activateUser.Click += async (_, _) => await ChangeUserStatusAsync(true);
            _deactivateUser.Click += async (_, _) => await ChangeUserStatusAsync(false);
            _resetUserPassword.Click += async (_, _) => await ResetUserPasswordAsync();
            refreshUsers.Click += async (_, _) => await LoadUsersAsync();
            userToolbar.Controls.AddRange([createUser, _changeUserRole, _activateUser, _deactivateUser, _resetUserPassword, refreshUsers]);
            _userStatus = CreateStatusLabel();
            userLayout.Controls.Add(userToolbar, 0, 0);
            userLayout.Controls.Add(_userGrid, 0, 1);
            userLayout.Controls.Add(_userStatus, 0, 2);
            usersPage.Controls.Add(userLayout);
            tabs.TabPages.Add(usersPage);
        }

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(tabs, 0, 1);
        Controls.Add(root);
        UpdateEmployeeActions();
        UpdatePositionActions();
        UpdateUserActions();
        Shown += async (_, _) =>
        {
            await LoadEmployeesAsync();
            await LoadPositionsAsync();
            if (_userGrid is not null)
            {
                await LoadUsersAsync();
            }
        };
    }

    private async Task LoadEmployeesAsync()
    {
        _employeeStatus.Text = "Loading employees...";
        try
        {
            var filter = new EmployeeFilter(_search.Text, (string)_departmentFilter.SelectedItem!, (string)_statusFilter.SelectedItem!);
            var employees = await _services.Employees.ListAsync(filter);
            _employeeGrid.Rows.Clear();
            foreach (var employee in employees)
            {
                _employeeGrid.Rows.Add(
                    employee.Id, employee.FirstName, employee.LastName, employee.Email,
                    employee.Department, employee.PositionTitle, employee.IsActive ? "Active" : "Inactive",
                    employee.HireDate.ToDateTime(TimeOnly.MinValue));
            }
            _employeeStatus.Text = employees.Count == 0 ? "No employees match the current search and filters." : $"{employees.Count} employee record(s).";
            UpdateEmployeeActions();
        }
        catch (Exception exception) when (IsExpectedStorageError(exception))
        {
            ShowOperationError("load employees", exception);
        }
    }

    private async Task LoadPositionsAsync()
    {
        _positionStatus.Text = "Loading positions...";
        try
        {
            var positions = await _services.Positions.ListAsync();
            _positionGrid.Rows.Clear();
            foreach (var position in positions)
            {
                _positionGrid.Rows.Add(position.Id, position.Title, position.Department, position.IsActive ? "Active" : "Inactive");
            }
            _positionStatus.Text = positions.Count == 0 ? "No positions are available." : $"{positions.Count} position(s).";
            UpdatePositionActions();
        }
        catch (Exception exception) when (IsExpectedStorageError(exception))
        {
            ShowOperationError("load positions", exception);
        }
    }

    private async Task LoadUsersAsync()
    {
        if (_userGrid is null || _userStatus is null)
        {
            return;
        }
        _userStatus.Text = "Loading users...";
        try
        {
            var users = await _services.Users.ListAsync(_user);
            _userGrid.Rows.Clear();
            foreach (var user in users)
            {
                _userGrid.Rows.Add(user.Id, user.Username, SqliteDataStore.RoleName(user.Role), user.IsActive ? "Active" : "Inactive");
            }
            _userStatus.Text = users.Count == 0 ? "No application users are available." : $"{users.Count} application user(s).";
            UpdateUserActions();
        }
        catch (Exception exception) when (IsExpectedStorageError(exception))
        {
            ShowOperationError("load application users", exception);
        }
    }

    private async Task CreateEmployeeAsync()
    {
        try
        {
            var positions = await _services.Positions.ListAsync();
            using var editor = new EmployeeEditorForm(positions, null);
            if (editor.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            await _services.Employees.CreateAsync(_user, editor.Draft);
            await LoadEmployeesAsync();
            _employeeStatus.Text = "Employee created successfully.";
        }
        catch (Exception exception) when (IsExpectedOperationError(exception))
        {
            ShowOperationError("create employee", exception);
        }
    }

    private async Task EditEmployeeAsync()
    {
        if (SelectedEmployeeId() is not int id)
        {
            return;
        }
        try
        {
            var employee = await _services.Employees.GetAsync(id);
            if (employee is null)
            {
                FormUtilities.ShowError(this, "The selected employee no longer exists.");
                await LoadEmployeesAsync();
                return;
            }
            var positions = await _services.Positions.ListAsync();
            using var editor = new EmployeeEditorForm(positions, employee);
            if (editor.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            await _services.Employees.UpdateAsync(_user, id, editor.Draft);
            await LoadEmployeesAsync();
            _employeeStatus.Text = "Employee details updated successfully.";
        }
        catch (Exception exception) when (IsExpectedOperationError(exception))
        {
            ShowOperationError("update employee", exception);
        }
    }

    private async Task ShowEmployeeDetailsAsync()
    {
        if (SelectedEmployeeId() is not int id)
        {
            return;
        }
        try
        {
            var employee = await _services.Employees.GetAsync(id);
            if (employee is null)
            {
                FormUtilities.ShowError(this, "The selected employee no longer exists.");
                await LoadEmployeesAsync();
                return;
            }
            using var details = new EmployeeDetailsForm(employee);
            details.ShowDialog(this);
        }
        catch (Exception exception) when (IsExpectedStorageError(exception))
        {
            ShowOperationError("view employee details", exception);
        }
    }

    private async Task ChangeEmployeeStatusAsync(bool isActive)
    {
        if (SelectedEmployeeId() is not int id)
        {
            return;
        }
        var action = isActive ? "reactivate" : "deactivate";
        if (!isActive && MessageBox.Show(this, "Deactivate the selected employee? The record will remain in the system.", "Confirm employee deactivation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }
        try
        {
            await _services.Employees.SetActiveAsync(_user, id, isActive);
            await LoadEmployeesAsync();
            _employeeStatus.Text = isActive ? "Employee reactivated successfully." : "Employee deactivated successfully.";
        }
        catch (Exception exception) when (IsExpectedOperationError(exception))
        {
            ShowOperationError(action + " employee", exception);
        }
    }

    private async Task CreatePositionAsync()
    {
        using var editor = new PositionEditorForm(null);
        if (editor.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        try
        {
            await _services.Positions.CreateAsync(_user, editor.Draft);
            await LoadPositionsAsync();
            _positionStatus.Text = "Position created successfully.";
        }
        catch (Exception exception) when (IsExpectedOperationError(exception))
        {
            ShowOperationError("create position", exception);
        }
    }

    private async Task EditPositionAsync()
    {
        if (SelectedPositionId() is not int id)
        {
            return;
        }
        try
        {
            var position = (await _services.Positions.ListAsync()).FirstOrDefault(candidate => candidate.Id == id);
            if (position is null)
            {
                FormUtilities.ShowError(this, "The selected position no longer exists.");
                await LoadPositionsAsync();
                return;
            }
            using var editor = new PositionEditorForm(position);
            if (editor.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            await _services.Positions.UpdateAsync(_user, id, editor.Draft);
            await LoadPositionsAsync();
            _positionStatus.Text = "Position details updated successfully.";
        }
        catch (Exception exception) when (IsExpectedOperationError(exception))
        {
            ShowOperationError("update position", exception);
        }
    }

    private async Task ChangePositionStatusAsync(bool isActive)
    {
        if (SelectedPositionId() is not int id)
        {
            return;
        }
        var action = isActive ? "reactivate" : "deactivate";
        if (!isActive && MessageBox.Show(this, "Deactivate the selected position?", "Confirm position deactivation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }
        try
        {
            await _services.Positions.SetActiveAsync(_user, id, isActive);
            await LoadPositionsAsync();
            _positionStatus.Text = isActive ? "Position reactivated successfully." : "Position deactivated successfully.";
        }
        catch (Exception exception) when (IsExpectedOperationError(exception))
        {
            ShowOperationError(action + " position", exception);
        }
    }

    private async Task CreateUserAsync()
    {
        using var editor = new UserEditorForm();
        if (editor.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        try
        {
            await _services.Users.CreateAsync(_user, editor.Username, editor.Password, editor.Role);
            await LoadUsersAsync();
            if (_userStatus is not null)
            {
                _userStatus.Text = "Application user created successfully.";
            }
        }
        catch (Exception exception) when (IsExpectedOperationError(exception))
        {
            ShowOperationError("create application user", exception);
        }
    }

    private async Task ChangeUserRoleAsync()
    {
        if (SelectedUserId() is not int id || _userGrid is null)
        {
            return;
        }
        var currentRole = SqliteDataStore.ParseRole(_userGrid.SelectedRows[0].Cells["Role"].Value?.ToString() ?? string.Empty);
        using var editor = new RoleEditorForm(currentRole);
        if (editor.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        try
        {
            await _services.Users.ChangeRoleAsync(_user, id, editor.Role);
            await LoadUsersAsync();
            if (_userStatus is not null)
            {
                _userStatus.Text = "Application role updated successfully.";
            }
        }
        catch (Exception exception) when (IsExpectedOperationError(exception))
        {
            ShowOperationError("change application role", exception);
        }
    }

    private async Task ChangeUserStatusAsync(bool isActive)
    {
        if (SelectedUserId() is not int id)
        {
            return;
        }
        if (!isActive && MessageBox.Show(this, "Deactivate this application user? They will no longer be able to sign in.", "Confirm user deactivation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }
        try
        {
            await _services.Users.SetActiveAsync(_user, id, isActive);
            await LoadUsersAsync();
            if (_userStatus is not null)
            {
                _userStatus.Text = isActive ? "Application user activated successfully." : "Application user deactivated successfully.";
            }
        }
        catch (Exception exception) when (IsExpectedOperationError(exception))
        {
            ShowOperationError(isActive ? "activate application user" : "deactivate application user", exception);
        }
    }

    private async Task ResetUserPasswordAsync()
    {
        if (SelectedUserId() is not int id)
        {
            return;
        }
        using var editor = new PasswordResetForm();
        if (editor.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        try
        {
            await _services.Users.ResetPasswordAsync(_user, id, editor.Password);
            if (_userStatus is not null)
            {
                _userStatus.Text = "Application user password reset successfully.";
            }
        }
        catch (Exception exception) when (IsExpectedOperationError(exception))
        {
            ShowOperationError("reset application user password", exception);
        }
    }

    private void UpdateEmployeeActions()
    {
        var selected = SelectedEmployeeStatus();
        var canEdit = _user.Role is UserRole.Admin or UserRole.HrManager;
        _editEmployee.Enabled = canEdit && selected.HasValue;
        _deactivateEmployee.Enabled = canEdit && selected == true;
        _reactivateEmployee.Enabled = canEdit && selected == false;
        foreach (var button in FindFormButtons("CreateEmployeeButton"))
        {
            button.Enabled = canEdit;
        }
    }

    private void UpdatePositionActions()
    {
        if (_editPosition is null || _deactivatePosition is null || _reactivatePosition is null)
        {
            return;
        }
        var active = SelectedPositionStatus();
        _editPosition.Enabled = active.HasValue;
        _deactivatePosition.Enabled = active == true;
        _reactivatePosition.Enabled = active == false;
    }

    private void UpdateUserActions()
    {
        if (_changeUserRole is null || _activateUser is null || _deactivateUser is null ||
            _resetUserPassword is null || _userGrid is null)
        {
            return;
        }
        var active = SelectedUserStatus();
        _changeUserRole.Enabled = active.HasValue;
        _activateUser.Enabled = active == false;
        _deactivateUser.Enabled = active == true && SelectedUserId() != _user.Id;
        _resetUserPassword.Enabled = active.HasValue;
    }

    private IEnumerable<Button> FindFormButtons(string name) =>
        Controls.Find(name, true).OfType<Button>();

    private int? SelectedEmployeeId() => ReadSelectedId(_employeeGrid);
    private int? SelectedPositionId() => ReadSelectedId(_positionGrid);
    private int? SelectedUserId() => _userGrid is null ? null : ReadSelectedId(_userGrid);

    private bool? SelectedEmployeeStatus() =>
        _employeeGrid.SelectedRows.Count == 0 ? null : _employeeGrid.SelectedRows[0].Cells["EmploymentStatus"].Value?.ToString() == "Active";

    private bool? SelectedPositionStatus() =>
        _positionGrid.SelectedRows.Count == 0 ? null : _positionGrid.SelectedRows[0].Cells["Status"].Value?.ToString() == "Active";

    private bool? SelectedUserStatus() =>
        _userGrid is null || _userGrid.SelectedRows.Count == 0 ? null : _userGrid.SelectedRows[0].Cells["Status"].Value?.ToString() == "Active";

    private static int? ReadSelectedId(DataGridView grid)
    {
        if (grid.SelectedRows.Count == 0)
        {
            return null;
        }
        return int.TryParse(grid.SelectedRows[0].Cells["Id"].Value?.ToString(), out var id) ? id : null;
    }

    private static Label CreateStatusLabel() => new()
    {
        Dock = DockStyle.Fill,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        AccessibleName = "List status"
    };

    private static void AddEmployeeColumns(DataGridView grid)
    {
        FormUtilities.AddTextColumn(grid, "FirstName", "First name");
        FormUtilities.AddTextColumn(grid, "LastName", "Last name");
        FormUtilities.AddTextColumn(grid, "Email", "Email", 24);
        FormUtilities.AddTextColumn(grid, "Department", "Department");
        FormUtilities.AddTextColumn(grid, "Position", "Position", 18);
        FormUtilities.AddTextColumn(grid, "EmploymentStatus", "Employment status");
        FormUtilities.AddTextColumn(grid, "HireDate", "Hire date").DefaultCellStyle.Format = "d";
    }

    private static void AddPositionColumns(DataGridView grid)
    {
        FormUtilities.AddTextColumn(grid, "Title", "Title", 24);
        FormUtilities.AddTextColumn(grid, "Department", "Department");
        FormUtilities.AddTextColumn(grid, "Status", "Status");
    }

    private static void AddUserColumns(DataGridView grid)
    {
        FormUtilities.AddTextColumn(grid, "Username", "Username", 25);
        FormUtilities.AddTextColumn(grid, "Role", "Role");
        FormUtilities.AddTextColumn(grid, "Status", "Status");
    }

    private static bool IsExpectedStorageError(Exception exception) =>
        exception is SqliteException or IOException or UnauthorizedAccessException;

    private static bool IsExpectedOperationError(Exception exception) =>
        exception is ValidationException or AuthorizationException or SqliteException or IOException or UnauthorizedAccessException;

    private void ShowOperationError(string operation, Exception exception)
    {
        switch (exception)
        {
            case ValidationException:
            case AuthorizationException:
                FormUtilities.ShowError(this, exception.Message);
                break;
            case SqliteException:
            case IOException:
            case UnauthorizedAccessException:
                _logger.Error($"Could not {operation}.", exception);
                FormUtilities.ShowError(this, $"EmployeeHub could not {operation}. Check the Logs folder or contact your administrator.");
                break;
            default:
                throw exception;
        }
    }
}
