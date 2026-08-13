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
    private CancellationTokenSource? _scrollCaptureDebounce;
    private bool _autoCaptureEnabled;
    private bool _autoScrollEnabled;

    internal LockSnipWindow(QuickSnipSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _mouseHookCallback = MouseHook;
        PreviousHotkeyText.Text = HotkeyService.Display(_settings.LockPreviousHotkey);
        NextHotkeyText.Text = HotkeyService.Display(_settings.LockNextHotkey);
        CaptureHotkeyText.Text = HotkeyService.Display(CaptureHotkey);
        AutomationControls.Visibility = _settings.LockAutoCaptureAvailable || _settings.LockAutoScrollAvailable
            ? Visibility.Visible
            : Visibility.Collapsed;
        AutoCaptureButton.Visibility = _settings.LockAutoCaptureAvailable
            ? Visibility.Visible
            : Visibility.Collapsed;
        AutoScrollButton.Visibility = _settings.LockAutoScrollAvailable
            ? Visibility.Visible
            : Visibility.Collapsed;
        if (_settings.LockAutoCaptureAvailable && !_settings.LockAutoScrollAvailable)
        {
            System.Windows.Controls.Grid.SetColumnSpan(AutoCaptureButton, 3);
        }
        else if (!_settings.LockAutoCaptureAvailable && _settings.LockAutoScrollAvailable)
        {
            System.Windows.Controls.Grid.SetColumn(AutoScrollButton, 0);
            System.Windows.Controls.Grid.SetColumnSpan(AutoScrollButton, 3);
        }
        AutomationStatus.Visibility = AutomationControls.Visibility;
        Height = AutomationControls.Visibility == Visibility.Visible ? 278 : 181;
        UpdateAutomationButtons();
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

    private HotkeySetting CaptureHotkey => _settings.WindowSnipHotkey;

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

    private async Task<bool> CaptureSectionAsync(bool automated = false)
    {
        if (_target is null || _capturing) return false;
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
            if (_autoScrollEnabled)
            {
                NativeScreenCapture.ScrollLockedTarget(_target, true);
                if (_autoCaptureEnabled)
                {
                    ScheduleCaptureAfterDownwardScroll();
                }
            }
            return true;
        }
        catch (Exception exception)
        {
            AppLogger.Error("Lock Snip capture", exception);
            if (automated) StopAutomation("Stopped after a capture error");
            return false;
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

    private void AutoScrollButton_Click(object sender, RoutedEventArgs e)
    {
        _autoScrollEnabled = !_autoScrollEnabled;
        UpdateAutomationButtons();
    }

    private void AutoCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        _autoCaptureEnabled = !_autoCaptureEnabled;
        UpdateAutomationButtons();
    }

    private void ScheduleCaptureAfterDownwardScroll()
    {
        if (!_autoCaptureEnabled || _target is null) return;
        _scrollCaptureDebounce?.Cancel();
        _scrollCaptureDebounce?.Dispose();
        _scrollCaptureDebounce = new CancellationTokenSource();
        _ = CaptureAfterScrollAsync(_scrollCaptureDebounce.Token);
    }

    private async Task CaptureAfterScrollAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AutomationDelay(), cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
                await CaptureSectionAsync(automated: true);
        }
        catch (OperationCanceledException)
        {
            // A newer downward scroll or Stop replaced this pending capture.
        }
    }

    private TimeSpan AutomationDelay() => TimeSpan.FromMilliseconds(_settings.LockAutomationSpeed switch
    {
        "Fast" => 350,
        "Slow" => 1400,
        _ => 750
    });

    private void StopButton_Click(object sender, RoutedEventArgs e) => StopAutomation();

    private void StopAutomation(string? status = null)
    {
        _scrollCaptureDebounce?.Cancel();
        _scrollCaptureDebounce?.Dispose();
        _scrollCaptureDebounce = null;
        _autoCaptureEnabled = false;
        _autoScrollEnabled = false;
        UpdateAutomationButtons();
        AutomationStatusText.Text = status ?? "Automation stopped";
    }

    private void UpdateAutomationButtons()
    {
        AutoCaptureButtonText.Text = _autoCaptureEnabled ? "Auto Capture On" : "Auto Capture";
        AutoScrollButtonText.Text = _autoScrollEnabled ? "Auto Scroll On" : "Auto Scroll";
        AutoCaptureButton.Background = AutomationBrush(_autoCaptureEnabled);
        AutoScrollButton.Background = AutomationBrush(_autoScrollEnabled);
        AutomationStatusText.Text = _autoCaptureEnabled && _autoScrollEnabled
            ? "Scroll down or capture to start"
            : _autoCaptureEnabled
                ? "Auto Capture requires a downward scroll to start"
                : _autoScrollEnabled
                    ? "Auto Scroll requires a capture to start"
                    : "Automation is off";
    }

    private static System.Windows.Media.Brush AutomationBrush(bool enabled) =>
        new System.Windows.Media.SolidColorBrush(enabled
            ? System.Windows.Media.Color.FromRgb(83, 104, 232)
            : System.Windows.Media.Color.FromArgb(0x55, 0x4F, 0x63, 0xC5));

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
            true);
        TargetNameText.Text = $"Locked window  •  {_target.Name}";
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
        StopAutomation();
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
        if (code >= 0 && wParam.ToInt32() == 0x020A && _autoCaptureEnabled && _target is not null)
        {
            var mouse = Marshal.PtrToStructure<LowLevelMouseData>(lParam);
            var delta = unchecked((short)(mouse.MouseData >> 16));
            if (delta < 0 && IsInsideLockedTarget(mouse.Point))
                Dispatcher.BeginInvoke(ScheduleCaptureAfterDownwardScroll);
        }
        return CallNextHookEx(_mouseHook, code, wParam, lParam);
    }

    private bool IsInsideLockedTarget(NativeMousePoint point)
    {
        if (_target is null) return false;
        var bounds = _target.Bounds;
        return point.X >= bounds.Left && point.X < bounds.Left + bounds.Width &&
               point.Y >= bounds.Top && point.Y < bounds.Top + bounds.Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMousePoint { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct LowLevelMouseData
    {
        public NativeMousePoint Point;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
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
