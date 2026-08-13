using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;

namespace QuickSnip;

internal static class HotkeyService
{
    public const int QuickSnipId = 4101;
    public const int WindowSnipId = 4102;
    public const int DragSnipId = 4103;
    public const int LockSnipId = 4104;
    public const int LockCaptureId = 4201;
    public const int LockPreviousId = 4202;
    public const int LockNextId = 4203;
    private const int WmHotkey = 0x0312;
    private const int WmClose = 0x0010;
    private const uint NoRepeat = 0x4000;
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "QuickSnipHotkeys";
    private static readonly string HostMutexName = @"Local\QuickSnip.HotkeyHost";

    public static bool TryRegister(IntPtr handle, int id, HotkeySetting setting) =>
        !setting.IsAssigned || RegisterHotKey(
            handle, id, (uint)setting.Modifiers | NoRepeat, (uint)setting.VirtualKey);

    public static void Unregister(IntPtr handle, int id) => UnregisterHotKey(handle, id);

    public static string Display(HotkeySetting setting)
    {
        if (!setting.IsAssigned) return "Not assigned";
        var parts = new List<string>();
        var modifiers = (ModifierKeys)setting.Modifiers;
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(DisplayKey(KeyInterop.KeyFromVirtualKey(setting.VirtualKey)));
        return string.Join(" + ", parts);
    }

    private static string DisplayKey(Key key)
    {
        if (key is >= Key.D0 and <= Key.D9)
            return ((int)key - (int)Key.D0).ToString();
        return key switch
        {
            Key.Oem3 => "`",
            Key.OemMinus => "-",
            Key.OemPlus => "=",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe => "\\",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            _ => key.ToString()
        };
    }

    public static HotkeySetting? FromKeyboard(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.None)
            return null;
        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None) return null;
        return new HotkeySetting
        {
            Modifiers = (int)modifiers,
            VirtualKey = KeyInterop.VirtualKeyFromKey(key)
        };
    }

    public static void UpdateStartup(QuickSnipSettings settings)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (settings.HasAnyHotkey())
            key.SetValue(RunValue, $"\"{Environment.ProcessPath}\" --hotkey-host");
        else
            key.DeleteValue(RunValue, false);
    }

    public static void StartHostIfNeeded(QuickSnipSettings settings)
    {
        UpdateStartup(settings);
        if (!settings.HasAnyHotkey()) return;
        Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--hotkey-host")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    public static void StopHost()
    {
        var handle = FindWindow(null, "QuickSnip Hotkeys");
        if (handle == IntPtr.Zero) return;
        PostMessage(handle, WmClose, IntPtr.Zero, IntPtr.Zero);
        for (var attempt = 0; attempt < 20 && FindWindow(null, "QuickSnip Hotkeys") != IntPtr.Zero; attempt++)
            Thread.Sleep(25);
    }

    public static void RunHost(QuickSnipSettings settings)
    {
        using var mutex = new Mutex(true, HostMutexName, out var ownsMutex);
        if (!ownsMutex) return;
        var source = new HwndSource(new HwndSourceParameters("QuickSnip Hotkeys")
        {
            Width = 0,
            Height = 0,
            WindowStyle = unchecked((int)0x80000000)
        });
        source.AddHook(HostHook);
        var registered = RegisterAll(source.Handle, settings);
        if (registered.Count == 0)
        {
            source.Dispose();
            return;
        }
        System.Windows.Threading.Dispatcher.Run();
        foreach (var id in registered) Unregister(source.Handle, id);
        source.Dispose();
    }

    public static List<int> RegisterAll(IntPtr handle, QuickSnipSettings settings)
    {
        var result = new List<int>();
        RegisterOne(QuickSnipId, settings.QuickSnipHotkey);
        RegisterOne(WindowSnipId, settings.WindowSnipHotkey);
        RegisterOne(DragSnipId, settings.DragSnipHotkey);
        if (settings.LockSnipEnabled) RegisterOne(LockSnipId, settings.LockSnipHotkey);
        return result;
        void RegisterOne(int id, HotkeySetting value)
        {
            if (value.IsAssigned && TryRegister(handle, id, value)) result.Add(id);
        }
    }

    private static IntPtr HostHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmClose)
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvokeShutdown(
                System.Windows.Threading.DispatcherPriority.Normal);
            handled = true;
            return IntPtr.Zero;
        }
        if (message != WmHotkey) return IntPtr.Zero;
        var argument = wParam.ToInt32() switch
        {
            QuickSnipId => "--display",
            WindowSnipId => "--active-window",
            DragSnipId => "--drag",
            LockSnipId => "--lock-snip",
            _ => null
        };
        if (argument is not null)
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, argument)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            handled = true;
        }
        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string windowName);
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
}
