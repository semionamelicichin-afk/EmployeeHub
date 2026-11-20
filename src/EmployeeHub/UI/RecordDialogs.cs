using System.Drawing;
using System.Windows.Forms;
using EmployeeHub.Domain;

namespace EmployeeHub.UI;

internal sealed class EmployeeEditorForm : Form
{
    private static readonly string[] Departments = ["Engineering", "HR", "Finance"];
    private readonly IReadOnlyList<PositionRecord> _positions;
    private readonly EmployeeRecord? _employee;
    private readonly ComboBox _department;
    private readonly ComboBox _position;
    private readonly TextBox _firstName;
    private readonly TextBox _lastName;
    private readonly TextBox _email;
    private readonly DateTimePicker _hireDate;

    public EmployeeDraft Draft => new(
        _firstName.Text.Trim(),
        _lastName.Text.Trim(),
        _email.Text.Trim(),
        (string)_department.SelectedItem!,
        ((PositionRecord)_position.SelectedItem!).Id,
        DateOnly.FromDateTime(_hireDate.Value));

    public EmployeeEditorForm(IReadOnlyList<PositionRecord> positions, EmployeeRecord? employee)
    {
        _positions = positions;
        _employee = employee;
        Text = employee is null ? "Create employee" : "Edit employee";
        Name = "EmployeeEditorForm";
        AccessibleName = Text;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(500, 430);
        Size = new Size(560, 480);
        StartPosition = FormStartPosition.CenterParent;

        var layout = FormUtilities.CreateEditorLayout(6);
        _firstName = new TextBox { Name = "FirstNameTextBox", AccessibleName = "First name", MaxLength = 100 };
        _lastName = new TextBox { Name = "LastNameTextBox", AccessibleName = "Last name", MaxLength = 100 };
        _email = new TextBox { Name = "EmailTextBox", AccessibleName = "Email", MaxLength = 254 };
        _department = FormUtilities.CreateDropDown("DepartmentComboBox", "Department", Departments);
        _position = new ComboBox
        {
            Name = "PositionComboBox",
            AccessibleName = "Position",
            AccessibleDescription = "Position in the selected department",
            DropDownStyle = ComboBoxStyle.DropDownList,
            IntegralHeight = false
        };
        _hireDate = new DateTimePicker
        {
            Name = "HireDatePicker",
            AccessibleName = "Hire date",
            Format = DateTimePickerFormat.Short,
            MaxDate = DateTime.Today,
            Value = employee is null ? DateTime.Today : employee.HireDate.ToDateTime(TimeOnly.MinValue)
        };

        FormUtilities.AddEditorField(layout, 0, "First name", _firstName);
        FormUtilities.AddEditorField(layout, 1, "Last name", _lastName);
        FormUtilities.AddEditorField(layout, 2, "Email", _email);
        FormUtilities.AddEditorField(layout, 3, "Department", _department);
        FormUtilities.AddEditorField(layout, 4, "Position", _position);
        FormUtilities.AddEditorField(layout, 5, "Hire date", _hireDate);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };
        var save = FormUtilities.CreateButton("SaveEmployeeButton", "Save", "Save employee details");
        var cancel = FormUtilities.CreateButton("CancelButton", "Cancel", "Discard changes");
        save.Click += (_, _) =>
        {
            if (_department.SelectedItem is null || _position.SelectedItem is null)
            {
                FormUtilities.ShowError(this, "Select a department and position before saving.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
        _department.SelectedIndexChanged += (_, _) => RefreshPositions();
        Controls.Add(layout);
        Controls.Add(buttons);

        if (employee is not null)
        {
            _firstName.Text = employee.FirstName;
            _lastName.Text = employee.LastName;
            _email.Text = employee.Email;
            _department.SelectedItem = employee.Department;
        }
        RefreshPositions(employee?.PositionId);
        if (employee is not null)
        {
            _position.SelectedItem = _positions.FirstOrDefault(position => position.Id == employee.PositionId);
        }
        ActiveControl = _firstName;
    }

    private void RefreshPositions(int? preferredPositionId = null)
    {
        var department = _department.SelectedItem as string;
        var selectedId = preferredPositionId ?? (_position.SelectedItem as PositionRecord)?.Id;
        var choices = _positions.Where(position =>
            position.Department == department &&
            (position.IsActive || position.Id == _employee?.PositionId)).ToArray();
        _position.DataSource = null;
        _position.DisplayMember = nameof(PositionRecord.Title);
        _position.ValueMember = nameof(PositionRecord.Id);
        _position.DataSource = choices;
        _position.SelectedItem = choices.FirstOrDefault(position => position.Id == selectedId);
        if (_position.SelectedIndex < 0 && choices.Length > 0)
        {
            _position.SelectedIndex = 0;
        }
    }
}

internal sealed class PositionEditorForm : Form
{
    private static readonly string[] Departments = ["Engineering", "HR", "Finance"];
    private readonly TextBox _title;
    private readonly ComboBox _department;

    public PositionDraft Draft => new(_title.Text.Trim(), (string)_department.SelectedItem!);

    public PositionEditorForm(PositionRecord? position)
    {
        Text = position is null ? "Create position" : "Edit position";
        Name = "PositionEditorForm";
        AccessibleName = Text;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(440, 250);
        Size = new Size(470, 260);
        StartPosition = FormStartPosition.CenterParent;

        _title = new TextBox
        {
            Name = "PositionTitleTextBox",
            AccessibleName = "Position title",
            MaxLength = 120,
            Text = position?.Title ?? string.Empty
        };
        _department = FormUtilities.CreateDropDown("DepartmentComboBox", "Department", Departments);
        if (position is not null)
        {
            _department.SelectedItem = position.Department;
        }
        var layout = FormUtilities.CreateEditorLayout(2);
        FormUtilities.AddEditorField(layout, 0, "Title", _title);
        FormUtilities.AddEditorField(layout, 1, "Department", _department);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var save = FormUtilities.CreateButton("SavePositionButton", "Save", "Save position details");
        var cancel = FormUtilities.CreateButton("CancelButton", "Cancel", "Discard changes");
        save.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(layout);
        Controls.Add(buttons);
        ActiveControl = _title;
    }
}

internal sealed class UserEditorForm : Form
{
    private readonly TextBox _username;
    private readonly TextBox _password;
    private readonly ComboBox _role;

    public string Username => _username.Text.Trim();
    public string Password => _password.Text;
    public UserRole Role => _role.SelectedIndex switch
    {
        0 => UserRole.Admin,
        1 => UserRole.HrManager,
        _ => UserRole.Viewer
    };

    public UserEditorForm()
    {
        Text = "Create application user";
        Name = "UserEditorForm";
        AccessibleName = Text;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(470, 290);
        Size = new Size(500, 310);
        StartPosition = FormStartPosition.CenterParent;
        _username = new TextBox { Name = "UsernameTextBox", AccessibleName = "Username", MaxLength = 100 };
        _password = new TextBox
        {
            Name = "InitialPasswordTextBox",
            AccessibleName = "Initial password",
            UseSystemPasswordChar = true,
            MaxLength = 256
        };
        _role = FormUtilities.CreateDropDown("RoleComboBox", "Application role", ["Admin", "HR Manager", "Viewer"]);
        _role.SelectedIndex = 1;
        var layout = FormUtilities.CreateEditorLayout(3);
        FormUtilities.AddEditorField(layout, 0, "Username", _username);
        FormUtilities.AddEditorField(layout, 1, "Initial password", _password);
        FormUtilities.AddEditorField(layout, 2, "Role", _role);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var save = FormUtilities.CreateButton("CreateUserButton", "Create", "Create application user");
        var cancel = FormUtilities.CreateButton("CancelButton", "Cancel", "Discard changes");
        save.Click += (_, _) =>
        {
            if (_role.SelectedIndex < 0)
            {
                FormUtilities.ShowError(this, "Select an application role.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(layout);
        Controls.Add(buttons);
        ActiveControl = _username;
    }
}

internal sealed class RoleEditorForm : Form
{
    private readonly ComboBox _role;

    public UserRole Role => _role.SelectedIndex switch
    {
        0 => UserRole.Admin,
        1 => UserRole.HrManager,
        _ => UserRole.Viewer
    };

    public RoleEditorForm(UserRole currentRole)
    {
        Text = "Change application role";
        Name = "RoleEditorForm";
        AccessibleName = Text;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(420, 190);
        Size = new Size(450, 200);
        StartPosition = FormStartPosition.CenterParent;
        _role = FormUtilities.CreateDropDown("RoleComboBox", "Application role", ["Admin", "HR Manager", "Viewer"]);
        _role.SelectedIndex = currentRole switch
        {
            UserRole.Admin => 0,
            UserRole.HrManager => 1,
            _ => 2
        };
        var layout = FormUtilities.CreateEditorLayout(1);
        FormUtilities.AddEditorField(layout, 0, "Role", _role);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var save = FormUtilities.CreateButton("SaveRoleButton", "Save", "Save application role");
        var cancel = FormUtilities.CreateButton("CancelButton", "Cancel", "Discard changes");
        save.DialogResult = DialogResult.OK;
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(layout);
        Controls.Add(buttons);
    }
}

internal sealed class PasswordResetForm : Form
{
    private readonly TextBox _password;
    public string Password => _password.Text;

    public PasswordResetForm()
    {
        Text = "Reset application user password";
        Name = "PasswordResetForm";
        AccessibleName = Text;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(460, 190);
        Size = new Size(480, 200);
        StartPosition = FormStartPosition.CenterParent;
        _password = new TextBox
        {
            Name = "NewPasswordTextBox",
            AccessibleName = "New password",
            UseSystemPasswordChar = true,
            MaxLength = 256
        };
        var layout = FormUtilities.CreateEditorLayout(1);
        FormUtilities.AddEditorField(layout, 0, "New password", _password);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var save = FormUtilities.CreateButton("ResetPasswordButton", "Reset", "Reset application user password");
        var cancel = FormUtilities.CreateButton("CancelButton", "Cancel", "Cancel password reset");
        save.DialogResult = DialogResult.OK;
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(layout);
        Controls.Add(buttons);
        ActiveControl = _password;
    }
}
