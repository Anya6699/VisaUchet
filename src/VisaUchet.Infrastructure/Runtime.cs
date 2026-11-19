using System.Text.Json;
using VisaUchet.Application;

namespace VisaUchet.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}

// Plain-text log; callers pass only application numbers and logins (SEC-04)
public sealed class FileAppLog : IAppLog
{
    private readonly string _path;
    private readonly object _sync = new();

    public FileAppLog(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void Info(string message) => Write("INFO", message);

    // Only the exception type and message are written, never parameter values
    public void Error(string message, Exception exception) =>
        Write("ERROR", $"{message}: {exception.GetType().Name}: {exception.Message}");

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {level} {message}{Environment.NewLine}";
        lock (_sync)
        {
            File.AppendAllText(_path, line);
        }
    }
}

// Settings from appsettings.json next to the executable
public sealed class AppSettings
{
    public string ConnectionString { get; init; } = string.Empty;

    public string LogFile { get; init; } = "logs/visauchet.log";

    public int IdleLogoutMinutes { get; init; } = 30;

    public static AppSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Не найден файл настроек {path}.", path);
        }

        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Файл настроек пуст.");
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidDataException("В appsettings.json не задана строка подключения ConnectionString.");
        }

        if (settings.IdleLogoutMinutes <= 0)
        {
            throw new InvalidDataException("IdleLogoutMinutes должен быть больше нуля.");
        }

        return settings;
    }
}
