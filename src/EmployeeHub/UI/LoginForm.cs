using System.Drawing;
using System.Windows.Forms;
using EmployeeHub.Domain;
using EmployeeHub.Infrastructure;
using EmployeeHub.Services;

namespace EmployeeHub.UI;

internal sealed class LoginForm : Form
{
    private readonly AuthenticationService _authentication;
    private readonly AppLogger _logger;
    private readonly TextBox _username;
    private readonly TextBox _password;
    private readonly Label _message;
    private readonly Button _signIn;

    public event Action<ApplicationUser>? SignInSucceeded;

    public LoginForm(AuthenticationService authentication, AppLogger logger)
    {
        _authentication = authentication;
        _logger = logger;
        Text = "EmployeeHub sign in";
        Name = "LoginForm";
        AccessibleName = "EmployeeHub sign in";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(420, 340);
        Size = new Size(470, 370);
        MaximizeBox = false;

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(28, 20, 28, 20)
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "EmployeeHub",
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AccessibleName = "EmployeeHub"
        };
        _username = new TextBox { Name = "UsernameTextBox", AccessibleName = "Username", Dock = DockStyle.Fill, MaxLength = 100 };
        _password = new TextBox
        {
            Name = "PasswordTextBox",
            AccessibleName = "Password",
            Dock = DockStyle.Fill,
            UseSystemPasswordChar = true,
            MaxLength = 256
        };
        _message = new Label
        {
            Name = "LoginMessageLabel",
            AccessibleName = "Sign-in message",
            Dock = DockStyle.Fill,
            ForeColor = Color.Firebrick,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false
        };
        _signIn = FormUtilities.CreateButton("SignInButton", "Sign in", "Sign in to EmployeeHub");
        _signIn.Dock = DockStyle.Fill;
        _signIn.Click += async (_, _) => await SignInAsync();

        content.Controls.Add(title, 0, 0);
        content.Controls.Add(CreateLabel("Username"), 0, 1);
        content.Controls.Add(_username, 0, 2);
        content.Controls.Add(CreateLabel("Password"), 0, 3);
        content.Controls.Add(_password, 0, 4);
        var buttonArea = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        buttonArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        buttonArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        buttonArea.Controls.Add(_signIn, 0, 0);
        buttonArea.Controls.Add(_message, 0, 1);
        content.Controls.Add(buttonArea, 0, 5);
        Controls.Add(content);
        AcceptButton = _signIn;
        ActiveControl = _username;
    }

    private async Task SignInAsync()
    {
        _message.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(_username.Text) || string.IsNullOrEmpty(_password.Text))
        {
            _message.Text = "Enter your username and password.";
            return;
        }

        _signIn.Enabled = false;
        _message.ForeColor = SystemColors.ControlText;
        _message.Text = "Signing in...";
        try
        {
            var username = _username.Text;
            var password = _password.Text;
            var user = await Task.Run(() => _authentication.SignInAsync(username, password));
            if (user is null)
            {
                _message.ForeColor = Color.Firebrick;
                _message.Text = "The username or password is incorrect, or the account is inactive.";
                _password.Clear();
                _password.Focus();
                return;
            }
            _password.Clear();
            SignInSucceeded?.Invoke(user);
        }
        catch (Exception exception) when (exception is Microsoft.Data.Sqlite.SqliteException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _logger.Error("Sign-in could not be completed.", exception);
            _message.ForeColor = Color.Firebrick;
            _message.Text = "EmployeeHub could not access its local data. Check the Logs folder or contact your administrator.";
        }
        finally
        {
            _signIn.Enabled = true;
        }
    }

    private static Label CreateLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.BottomLeft,
        AccessibleName = text
    };
}
