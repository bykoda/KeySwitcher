using System.IO;

namespace KeySwitcher.Services;

public static class AppLog
{
    private static readonly object Sync = new();
    public static string DirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "by koda", "KeySwitcher", "Logs");
    public static string FilePath => Path.Combine(DirectoryPath, "KeySwitcher.log");

    public static void Write(string message, Exception? exception = null)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var text = $"{DateTimeOffset.Now:O}  {message}{Environment.NewLine}";
            if (exception is not null) text += exception + Environment.NewLine;
            lock (Sync) File.AppendAllText(FilePath, text);
        }
        catch { }
    }
}
