using System.Text;

namespace PMSIntegration.Application.Logging;

public class FileLogger
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    private readonly Dictionary<LogLevel, string> _fileNames = new()
    {
        [LogLevel.Debug] = "debug.log",
        [LogLevel.Info] = "info.log",
        [LogLevel.Warn] = "warn.log",
        [LogLevel.Error] = "error.log"
    };

    public FileLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory); // Ensure directory exists
    }
    
    public void LogDebug(string message) => Log(LogLevel.Debug, message);
    public void LogError(Exception ex, string message) => Log(LogLevel.Error, message + ".More info: " + ex.Message);
    public void LogInfo(string message) => Log(LogLevel.Info, message);
    public void LogWarn(string message) => Log(LogLevel.Warn, message);

    private void Log(LogLevel level, string message)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var formattedMessage = $"[{timestamp}] [{level.ToString().ToUpper()}] {message}{Environment.NewLine}";
        var filePath = Path.Combine(_logDirectory, _fileNames[level]);

        lock (_lock)
        {
            File.AppendAllText(filePath, formattedMessage, Encoding.UTF8);
        }
    }
}