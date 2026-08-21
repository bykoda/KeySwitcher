using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;
using KeySwitcher.Models;

namespace KeySwitcher.Services;

public sealed class KeyboardEngine : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly Func<AppSettings> _settings;
    private readonly NativeMethods.HookProc _callback;
    private nint _hook;
    private string _word = "";
    private volatile bool _injecting;

    public KeyboardEngine(Dispatcher dispatcher, Func<AppSettings> settings)
    {
        _dispatcher = dispatcher; _settings = settings; _callback = Hook;
    }

    public string CurrentWord => _word;
    public void Start() => _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _callback, NativeMethods.GetModuleHandle(null), 0);

    public void CorrectCurrentWord()
    {
        var original = _word;
        if (original.Length == 0) return;
        var replacement = LayoutConverter.Convert(original);
        _word = replacement;
        Replace(original, replacement, null);
    }

    private nint Hook(int code, nint wParam, nint lParam)
    {
        if (code < 0 || wParam != NativeMethods.WM_KEYDOWN && wParam != NativeMethods.WM_SYSKEYDOWN) return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
        var key = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
        if (_injecting || (key.flags & NativeMethods.LLKHF_INJECTED) != 0 || IsExcluded()) return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);

        if (key.vkCode is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C) return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
        if ((NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0 || (NativeMethods.GetAsyncKeyState(0x5B) & 0x8000) != 0 || (NativeMethods.GetAsyncKeyState(0x5C) & 0x8000) != 0)
            return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
        if (key.vkCode == 0x08) { if (_word.Length > 0) _word = _word[..^1]; return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam); }

        var ch = Translate(key);
        if (ch is not null && (char.IsLetterOrDigit(ch.Value) || ch is '-' or '_')) { _word += ch; return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam); }

        var boundary = key.vkCode is 0x20 or 0x0D or 0x09;
        if (!boundary) { _word = ""; return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam); }
        if (_word.Length == 0) return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);

        var original = _word; _word = "";
        var settings = _settings();
        var ignored = settings.IgnoredWords.Contains(original, StringComparer.OrdinalIgnoreCase);
        string? replacement = null;
        if (settings.Snippets.TryGetValue(original, out var snippet)) replacement = snippet;
        else if (!ignored && (settings.ForceSwapWords.Contains(original, StringComparer.OrdinalIgnoreCase) || settings.AutoCorrect && LayoutConverter.IsWrong(original))) replacement = LayoutConverter.Convert(original);
        if (replacement is null) return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
        Replace(original, replacement, key.vkCode);
        return 1;
    }

    private void Replace(string original, string replacement, uint? delimiter) => _dispatcher.BeginInvoke(() =>
    {
        _injecting = true;
        try
        {
            TextInjector.Backspace(original.Length);
            TextInjector.Text(replacement);
            if (delimiter is uint vk) TextInjector.VirtualKey((ushort)vk);
        }
        finally { _injecting = false; }
    });

    private static char? Translate(NativeMethods.KbdLlHookStruct key)
    {
        var state = new byte[256]; NativeMethods.GetKeyboardState(state);
        var window = NativeMethods.GetForegroundWindow();
        var thread = NativeMethods.GetWindowThreadProcessId(window, out _);
        var buffer = new StringBuilder(4);
        return NativeMethods.ToUnicodeEx(key.vkCode, key.scanCode, state, buffer, 4, 0, NativeMethods.GetKeyboardLayout(thread)) == 1 ? buffer[0] : null;
    }

    private bool IsExcluded()
    {
        try
        {
            NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out var pid);
            var name = Process.GetProcessById((int)pid).ProcessName + ".exe";
            return _settings().ExcludedApps.Contains(name, StringComparer.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public void Dispose() { if (_hook != 0) NativeMethods.UnhookWindowsHookEx(_hook); }
}
