using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace QuickSnip;

public partial class LockSnipWindow : Window
{
    private readonly QuickSnipSettings _settings;
    private LockedCaptureTarget? _target;
    private IntPtr _handle;
    private HwndSource? _source;
    private bool _capturing;
    private IntPtr _mouseHook;
    private readonly MouseHookCallback _mouseHookCallback;

    internal LockSnipWindow(QuickSnipSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _mouseHookCallback = MouseHook;
        PreviousHotkeyText.Text = HotkeyService.Display(_settings.LockPreviousHotkey);
        NextHotkeyText.Text = HotkeyService.Display(_settings.LockNextHotkey);
        CaptureHotkeyText.Text = HotkeyService.Display(CaptureHotkey);
        SourceInitialized += Window_SourceInitialized;
        Loaded += Window_Loaded;
        Closed += (_, _) => Cleanup();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!await PickTargetAsync(closeWhenCancelled: true))
        {
            return;
        }
        Show();
        Activate();
        InstallMouseHook();
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        _handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WindowHook);
        RegisterSessionHotkeys();
    }

    private void RegisterSessionHotkeys()
    {
        TryRegister(HotkeyService.LockCaptureId, CaptureHotkey);
        TryRegister(HotkeyService.LockPreviousId, _settings.LockPreviousHotkey);
        TryRegister(HotkeyService.LockNextId, _settings.LockNextHotkey);
    }

    private HotkeySetting CaptureHotkey => _settings.LockSnipTarget == "Window"
        ? _settings.WindowSnipHotkey
        : _settings.QuickSnipHotkey;

    private void TryRegister(int id, HotkeySetting setting)
    {
        if (setting.IsAssigned && !HotkeyService.TryRegister(_handle, id, setting))
            AppLogger.Information("Lock Snip hotkey", $"Could not register {HotkeyService.Display(setting)}");
    }

    private IntPtr WindowHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != 0x0312) return IntPtr.Zero;
        handled = true;
        switch (wParam.ToInt32())
        {
            case HotkeyService.LockCaptureId: _ = CaptureSectionAsync(); break;
            case HotkeyService.LockPreviousId: Move(false); break;
            case HotkeyService.LockNextId: Move(true); break;
        }
        return IntPtr.Zero;
    }

    private async Task CaptureSectionAsync()
    {
        if (_target is null || _capturing) return;
        _capturing = true;
        Hide();
        await Task.Delay(100);
        try
        {
            using var bitmap = NativeScreenCapture.CaptureLockedTarget(_target);
            await ScreenCaptureService.OutputBitmapAsync(
                bitmap,
                CaptureTarget.Lock,
                _settings,
                _target.IsWindow ? _target.Name : null);
        }
        finally
        {
            _capturing = false;
            Show();
            Activate();
        }
    }

    private void Move(bool next)
    {
        if (_target is null || _capturing) return;
        NativeScreenCapture.ScrollLockedTarget(_target, next);
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e) => _ = CaptureSectionAsync();
    private void PreviousButton_Click(object sender, RoutedEventArgs e) => Move(false);
    private void NextButton_Click(object sender, RoutedEventArgs e) => Move(true);
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    private async void ResetWindowButton_Click(object sender, RoutedEventArgs e) =>
        await PickTargetAsync(closeWhenCancelled: false);

    private async Task<bool> PickTargetAsync(bool closeWhenCancelled)
    {
        Hide();
        var picker = new LockTargetPickerWindow();
        if (picker.ShowDialog() != true || picker.SelectedScreenPoint is not { } point)
        {
            if (closeWhenCancelled) Close();
            else { Show(); Activate(); }
            return false;
        }

        await Task.Delay(120);
        _target = NativeScreenCapture.GetLockedTarget(
            (int)Math.Round(point.X), (int)Math.Round(point.Y),
            _settings.LockSnipTarget == "Window");
        TargetNameText.Text = _settings.LockSnipTarget == "Window"
            ? $"Locked window  •  {_target.Name}"
            : $"Locked display  •  {_target.Name}";
        Show();
        Activate();
        return true;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Cleanup()
    {
        HotkeyService.Unregister(_handle, HotkeyService.LockCaptureId);
        HotkeyService.Unregister(_handle, HotkeyService.LockPreviousId);
        HotkeyService.Unregister(_handle, HotkeyService.LockNextId);
        _source?.RemoveHook(WindowHook);
        if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
    }

    private void InstallMouseHook()
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        _mouseHook = SetWindowsHookEx(14, _mouseHookCallback,
            module is null ? IntPtr.Zero : GetModuleHandle(module.ModuleName), 0);
    }

    private IntPtr MouseHook(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && wParam.ToInt32() == 0x0207 && !_capturing)
        {
            Dispatcher.BeginInvoke(Close);
            return new IntPtr(1);
        }
        return CallNextHookEx(_mouseHook, code, wParam, lParam);
    }

    private delegate IntPtr MouseHookCallback(int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hook, MouseHookCallback callback, IntPtr module, uint threadId);
    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
