using System.Windows.Interop;

namespace KeySwitcher.Services;

public sealed class HotkeyManager : IDisposable
{
    private readonly nint _handle;
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = [];

    public HotkeyManager(System.Windows.Window window)
    {
        _handle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_handle);
        _source.AddHook(Hook);
    }

    public bool Register(int id, string gesture, Action action)
    {
        var (modifiers, key) = Parse(gesture);
        _actions[id] = action;
        return NativeMethods.RegisterHotKey(_handle, id, modifiers | NativeMethods.MOD_NOREPEAT, key);
    }

    public void Clear()
    {
        foreach (var id in _actions.Keys) NativeMethods.UnregisterHotKey(_handle, id);
        _actions.Clear();
    }

    private nint Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == 0x0312 && _actions.TryGetValue(wParam.ToInt32(), out var action)) { handled = true; action(); }
        return 0;
    }

    private static (uint modifiers, uint key) Parse(string gesture)
    {
        uint mod = 0, key = 0;
        foreach (var part in gesture.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) mod |= NativeMethods.MOD_CONTROL;
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) mod |= NativeMethods.MOD_ALT;
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) mod |= NativeMethods.MOD_SHIFT;
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase)) mod |= NativeMethods.MOD_WIN;
            else if (part.Equals("Space", StringComparison.OrdinalIgnoreCase)) key = 0x20;
            else if (part.Length == 1) key = char.ToUpperInvariant(part[0]);
            else if (part.StartsWith('F') && uint.TryParse(part[1..], out var f) && f is >= 1 and <= 24) key = 0x6F + f;
        }
        if (key == 0) throw new FormatException($"Неизвестная горячая клавиша: {gesture}");
        return (mod, key);
    }

    public void Dispose() { Clear(); _source.RemoveHook(Hook); }
}
