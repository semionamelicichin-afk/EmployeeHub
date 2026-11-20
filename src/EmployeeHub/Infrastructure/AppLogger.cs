namespace EmployeeHub.Infrastructure;

public sealed class AppLogger(string logDirectory)
{
    private readonly object _gate = new();
    private readonly string _logFile = Path.Combine(logDirectory, $"employeehub-{DateTime.Now:yyyy-MM-dd}.log");

    public void Information(string message) => Write("INFO", message, null);

    public void Error(string message, Exception exception) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFile)!);
                var details = exception is null ? string.Empty : Environment.NewLine + exception;
                File.AppendAllText(
                    _logFile,
                    $"{DateTimeOffset.Now:O} [{level}] {message}{details}{Environment.NewLine}");
            }
        }
        catch (IOException)
        {
            System.Diagnostics.Debug.WriteLine("Unable to write the EmployeeHub log file.");
        }
        catch (UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine("Unable to write the EmployeeHub log file.");
        }
    }
}
