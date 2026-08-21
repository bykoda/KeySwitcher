using Microsoft.Win32;

namespace KeySwitcher.Services;

public static class StartupManager
{
    private const string Path = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(Path, true) ?? Registry.CurrentUser.CreateSubKey(Path);
        if (enabled) key.SetValue("KeySwitcher", $"\"{Environment.ProcessPath}\""); else key.DeleteValue("KeySwitcher", false);
    }
}
