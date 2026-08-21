using System.Runtime.InteropServices;

namespace KeySwitcher.Services;

public static class TextInjector
{
    public static void Backspace(int count)
    {
        var inputs = new List<NativeMethods.Input>(count * 2);
        for (var i = 0; i < count; i++) { inputs.Add(Key(0x08, false)); inputs.Add(Key(0x08, true)); }
        Send(inputs);
    }

    public static void Text(string text)
    {
        var inputs = new List<NativeMethods.Input>(text.Length * 2);
        foreach (var ch in text)
        {
            inputs.Add(Unicode(ch, false));
            inputs.Add(Unicode(ch, true));
        }
        Send(inputs);
    }

    public static void VirtualKey(ushort key)
    {
        Send([Key(key, false), Key(key, true)]);
    }

    public static void Chord(ushort modifier, ushort key)
    {
        Send([Key(modifier, false), Key(key, false), Key(key, true), Key(modifier, true)]);
    }

    private static NativeMethods.Input Key(ushort key, bool up) => new() { type = 1, U = new() { ki = new() { wVk = key, dwFlags = up ? NativeMethods.KEYEVENTF_KEYUP : 0 } } };
    private static NativeMethods.Input Unicode(char ch, bool up) => new() { type = 1, U = new() { ki = new() { wScan = ch, dwFlags = NativeMethods.KEYEVENTF_UNICODE | (up ? NativeMethods.KEYEVENTF_KEYUP : 0) } } };
    private static void Send(IReadOnlyCollection<NativeMethods.Input> values)
    {
        if (values.Count == 0) return;
        var array = values.ToArray();
        NativeMethods.SendInput((uint)array.Length, array, Marshal.SizeOf<NativeMethods.Input>());
    }
}
