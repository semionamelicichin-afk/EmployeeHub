using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using EmployeeHub.Infrastructure;
using EmployeeHub.Services;
using EmployeeHub.UI;
using Microsoft.Data.Sqlite;

namespace EmployeeHub;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var windowsUser = $"{Environment.UserDomainName}\\{Environment.UserName}";
        var userKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(windowsUser.ToUpperInvariant())));
        using var singleInstance = new Mutex(true, $@"Global\EmployeeHub-{userKey}", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "EmployeeHub is already running for this Windows user.",
                "EmployeeHub",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var appDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EmployeeHub");
        var logger = new AppLogger(Path.Combine(appDirectory, "Logs"));
        try
        {
            var store = new SqliteDataStore(Path.Combine(appDirectory, "EmployeeHub.db"));
            store.InitializeAsync().GetAwaiter().GetResult();
            var services = new ApplicationServices(store, logger);
            Application.ThreadException += (_, args) =>
            {
                logger.Error("An unexpected UI error occurred.", args.Exception);
                MessageBox.Show(
                    "EmployeeHub encountered an unexpected error. Check the Logs folder for technical details.",
                    "EmployeeHub",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };
            Application.Run(new EmployeeHubApplicationContext(services, logger));
        }
        catch (Exception exception) when (exception is SqliteException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            logger.Error("EmployeeHub could not initialize its local data.", exception);
            MessageBox.Show(
                "EmployeeHub could not initialize its local data. Check the Logs folder or contact your administrator.",
                "EmployeeHub",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

internal sealed class EmployeeHubApplicationContext : ApplicationContext
{
    private readonly ApplicationServices _services;
    private readonly AppLogger _logger;
    private readonly LoginForm _login;
    private MainForm? _main;

    public EmployeeHubApplicationContext(ApplicationServices services, AppLogger logger)
    {
        _services = services;
        _logger = logger;
        _login = new LoginForm(services.Authentication, logger);
        _login.SignInSucceeded += ShowMainWindow;
        MainForm = _login;
        _login.Show();
    }

    private void ShowMainWindow(Domain.ApplicationUser user)
    {
        if (_main is not null)
        {
            _main.Close();
        }
        _main = new MainForm(user, _services, _logger);
        _main.LogoutRequested += ReturnToLogin;
        MainForm = _main;
        _main.Show();
        _login.Hide();
    }

    private void ReturnToLogin()
    {
        if (_main is null)
        {
            return;
        }
        MainForm = _login;
        _main.Close();
        _main = null;
        _login.Show();
        _login.Activate();
    }
}
