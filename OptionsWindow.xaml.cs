using System.Reflection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;

namespace QuickSnip;

public partial class OptionsWindow : Window
{
    private QuickSnipSettings _settings;
    private bool _loading = true;
    private IntPtr _windowHandle;

    public OptionsWindow()
    {
        InitializeComponent();
        _settings = SettingsService.Load();
        WindowPlacementService.Restore(this, _settings.SettingsWindow, 504, 760);
        Closing += OptionsWindow_Closing;
        SourceInitialized += OptionsWindow_SourceInitialized;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Build {version?.Major}.{version?.Minor}.{version?.Build}";

        QuickSnipToggle.IsChecked = _settings.QuickSnipEnabled;
        ActiveWindowToggle.IsChecked = _settings.ActiveWindowSnipEnabled;
        DragSnipToggle.IsChecked = _settings.DragSnipEnabled;
        TaskbarModesToggle.IsChecked = _settings.ShowSnipModesInTaskbar;
        LockSnipToggle.IsChecked = _settings.LockSnipEnabled;
        LockDisplayChoice.IsChecked = _settings.LockSnipTarget == "Display";
        LockWindowChoice.IsChecked = _settings.LockSnipTarget == "Window";
        SavePngToggle.IsChecked = _settings.SavePng;
        ClipboardToggle.IsChecked = _settings.CopyToClipboard;
        ToastToggle.IsChecked = _settings.ShowCaptureToast;
        SelectSaveChoices();
        SaveLocationText.Text = _settings.SaveFolder;
        UpdateSaveFormatState();
        UpdateHotkeyBoxes();
        UpdateLockSnipState();

        _loading = false;
        UpdateActionButtons();
    }

    private void LockSnipSetting_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        SaveSettingsFromControls();
        UpdateLockSnipState();
        RebindAllHotkeys();
        HotkeyService.UpdateStartup(_settings);
        StatusText.Text = LockSnipToggle.IsChecked == true
            ? "Lock Snip enabled. Start it from the taskbar right-click menu."
            : "Lock Snip disabled.";
    }

    private void UpdateLockSnipState()
    {
        var enabled = LockSnipToggle.IsChecked == true;
        LockSnipControls.IsEnabled = enabled;
        LockSnipControls.Opacity = enabled ? 1 : 0.55;
    }

    private void SettingToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (SavePngToggle.IsChecked != true && ClipboardToggle.IsChecked != true)
        {
            _loading = true;
            ((CheckBox)sender).IsChecked = true;
            _loading = false;
            StatusText.Text = "Keep at least one output enabled.";
            return;
        }

        SaveSettingsFromControls();
        UpdateActionButtons();
        StatusText.Text = "Preferences saved.";
    }

    private void DefaultSnipMode_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        SaveSettingsFromControls();
        StatusText.Text = $"{((RadioButton)sender).Content} is now the taskbar left-click mode.";
    }

    private void SaveSettingsFromControls()
    {
        _settings.QuickSnipEnabled = QuickSnipToggle.IsChecked == true;
        _settings.ActiveWindowSnipEnabled = ActiveWindowToggle.IsChecked == true;
        _settings.DragSnipEnabled = DragSnipToggle.IsChecked == true;
        _settings.ShowSnipModesInTaskbar = TaskbarModesToggle.IsChecked == true;
        _settings.LockSnipEnabled = LockSnipToggle.IsChecked == true;
        _settings.LockSnipTarget = LockWindowChoice.IsChecked == true ? "Window" : "Display";
        _settings.SavePng = SavePngToggle.IsChecked == true;
        _settings.CopyToClipboard = ClipboardToggle.IsChecked == true;
        _settings.ShowCaptureToast = ToastToggle.IsChecked == true;
        _settings.ImageFormat = JpegFormatChoice.IsChecked == true
            ? "JPEG"
            : WebpFormatChoice.IsChecked == true ? "WebP" : "PNG";
        _settings.ImageQuality = LowQualityChoice.IsChecked == true
            ? "Low"
            : MediumQualityChoice.IsChecked == true ? "Medium" : "High";
        SettingsService.Save(_settings);
        JumpListService.Register(_settings);
        UpdateSaveFormatState();
    }

    private void UpdateActionButtons()
    {
        // Capture actions live on the taskbar and global hotkeys.
    }

    private void ChangeFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose where QuickSnip saves PNG files",
            InitialDirectory = _settings.SaveFolder,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _settings.SaveFolder = dialog.FolderName;
        SettingsService.Save(_settings);
        SaveLocationText.Text = _settings.SaveFolder;
        StatusText.Text = "Save location updated.";
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SnipFolderService.Open(_settings.SaveFolder);
        }
        catch (Exception exception)
        {
            AppLogger.Error("Open snips folder", exception);
            StatusText.Text = "The snips folder could not be opened.";
        }
    }

    private void SaveChoice_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        SaveSettingsFromControls();
        StatusText.Text = "Save preferences updated.";
    }

    private void ResetWindowButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.SettingsWindow = new WindowPlacementSettings();
        WindowPlacementService.Restore(this, _settings.SettingsWindow, 504, 760);
        SettingsService.Save(_settings);
        StatusText.Text = "Window size and position reset.";
    }

    private void RestoreDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            this,
            "Restore QuickSnip preferences to their defaults? Your save folder and screenshots will be preserved.",
            "Restore QuickSnip defaults",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes) return;

        _settings.RestoreDefaultsPreservingUserData();
        SettingsService.Save(_settings);
        LoadControlsFromSettings();
        RebindAllHotkeys();
        HotkeyService.UpdateStartup(_settings);
        JumpListService.Register(_settings);
        StatusText.Text = "Default preferences restored. Screenshots were preserved.";
    }

    private void RecycleSnipsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = SnipCleanupService.FindSnips(_settings.SaveFolder);
            if (files.Length == 0)
            {
                StatusText.Text = "No QuickSnip images were found in the current save folder.";
                return;
            }

            var answer = MessageBox.Show(
                this,
                $"Move {files.Length} QuickSnip image(s) from:\n{_settings.SaveFolder}\n\nto the Windows Recycle Bin?",
                "Move QuickSnip images",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer != MessageBoxResult.Yes) return;

            var moved = SnipCleanupService.MoveToRecycleBin(files);
            StatusText.Text = $"Moved {moved} QuickSnip image(s) to the Recycle Bin.";
        }
        catch (Exception exception)
        {
            AppLogger.Error("Move snips to Recycle Bin", exception);
            StatusText.Text = "Some images could not be moved. Details were saved to the diagnostic log.";
        }
    }

    private void InformationButton_Click(object sender, RoutedEventArgs e)
    {
        // Persist the currently visible bounds before the Information window reads them.
        WindowPlacementService.Save(this, _settings.SettingsWindow);
        SettingsService.Save(_settings);

        Hide();
        var guide = new WelcomeWindow(isFirstRun: false) { Owner = this };
        guide.ContinueRequested += (_, _) => guide.Close();
        guide.Closed += (_, _) =>
        {
            var latest = SettingsService.Load();
            _settings.SettingsWindow = latest.SettingsWindow;
            WindowPlacementService.Restore(this, _settings.SettingsWindow, 504, 760);
            Show();
            Activate();
        };
        guide.Show();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void OptionsWindow_Closing(object? sender, CancelEventArgs e)
    {
        UnregisterAllHotkeys();
        WindowPlacementService.Save(this, _settings.SettingsWindow);
        SettingsService.Save(_settings);
        HotkeyService.UpdateStartup(_settings);
    }

    private void OptionsWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        RebindAllHotkeys();
    }

    private void HotkeyBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox box) box.Text = "Press shortcut…";
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        if (sender is not TextBox box) return;
        if (e.Key == Key.Escape)
        {
            UpdateHotkeyBoxes();
            Keyboard.ClearFocus();
            return;
        }

        var proposed = HotkeyService.FromKeyboard(e);
        if (proposed is null)
        {
            StatusText.Text = "Use at least one modifier: Ctrl, Alt, Shift, or Windows.";
            return;
        }

        var (id, current, assign) = GetHotkeyBinding(box);
        HotkeyService.Unregister(_windowHandle, id);
        if (!HotkeyService.TryRegister(_windowHandle, id, proposed))
        {
            HotkeyService.TryRegister(_windowHandle, id, current);
            box.Text = HotkeyService.Display(current);
            StatusText.Text = $"{HotkeyService.Display(proposed)} is already used. The previous shortcut was kept.";
            Keyboard.ClearFocus();
            return;
        }

        assign(proposed);
        SettingsService.Save(_settings);
        HotkeyService.UpdateStartup(_settings);
        UpdateHotkeyBoxes();
        StatusText.Text = $"{HotkeyService.Display(proposed)} registered immediately.";
        Keyboard.ClearFocus();
    }

    private void ClearHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string name }) return;
        var box = name switch
        {
            "QuickSnip" or "LockScreenSnip" => QuickSnipHotkeyBox,
            "WindowSnip" => WindowSnipHotkeyBox,
            "LockWindowSnip" => WindowSnipHotkeyBox,
            "DragSnip" => DragSnipHotkeyBox,
            "LockSnip" => LockSnipHotkeyBox,
            "LockPrevious" => LockPreviousHotkeyBox,
            _ => LockNextHotkeyBox
        };
        var (id, _, assign) = GetHotkeyBinding(box);
        HotkeyService.Unregister(_windowHandle, id);
        assign(new HotkeySetting());
        SettingsService.Save(_settings);
        HotkeyService.UpdateStartup(_settings);
        UpdateHotkeyBoxes();
        StatusText.Text = $"{name.Replace("Snip", " Snip").Trim()} hotkey cleared.";
    }

    private void ResetLockSnipHotkeysButton_Click(object sender, RoutedEventArgs e)
    {
        HotkeyService.Unregister(_windowHandle, HotkeyService.LockPreviousId);
        HotkeyService.Unregister(_windowHandle, HotkeyService.LockNextId);
        _settings.LockPreviousHotkey = HotkeySetting.Alt(Key.W);
        _settings.LockNextHotkey = HotkeySetting.Alt(Key.S);
        SettingsService.Save(_settings);
        RebindAllHotkeys();
        UpdateHotkeyBoxes();
        StatusText.Text = "Lock Snip hotkeys reset.";
    }

    private void ResetHotkeysButton_Click(object sender, RoutedEventArgs e)
    {
        UnregisterAllHotkeys();
        _settings.QuickSnipHotkey = new HotkeySetting();
        _settings.WindowSnipHotkey = new HotkeySetting();
        _settings.DragSnipHotkey = new HotkeySetting();
        _settings.LockSnipHotkey = new HotkeySetting();
        SettingsService.Save(_settings);
        HotkeyService.UpdateStartup(_settings);
        UpdateHotkeyBoxes();
        StatusText.Text = "All hotkeys reset and disabled.";
    }

    private (int Id, HotkeySetting Current, Action<HotkeySetting> Assign) GetHotkeyBinding(TextBox box)
    {
        if (ReferenceEquals(box, QuickSnipHotkeyBox) || ReferenceEquals(box, LockScreenSnipHotkeyBox))
            return (HotkeyService.QuickSnipId, _settings.QuickSnipHotkey, value => _settings.QuickSnipHotkey = value);
        if (ReferenceEquals(box, WindowSnipHotkeyBox) || ReferenceEquals(box, LockWindowSnipHotkeyBox))
            return (HotkeyService.WindowSnipId, _settings.WindowSnipHotkey, value => _settings.WindowSnipHotkey = value);
        if (ReferenceEquals(box, DragSnipHotkeyBox))
            return (HotkeyService.DragSnipId, _settings.DragSnipHotkey, value => _settings.DragSnipHotkey = value);
        if (ReferenceEquals(box, LockSnipHotkeyBox))
            return (HotkeyService.LockSnipId, _settings.LockSnipHotkey, value => _settings.LockSnipHotkey = value);
        if (ReferenceEquals(box, LockPreviousHotkeyBox))
            return (HotkeyService.LockPreviousId, _settings.LockPreviousHotkey, value => _settings.LockPreviousHotkey = value);
        return (HotkeyService.LockNextId, _settings.LockNextHotkey, value => _settings.LockNextHotkey = value);
    }

    private void UpdateHotkeyBoxes()
    {
        QuickSnipHotkeyBox.Text = HotkeyService.Display(_settings.QuickSnipHotkey);
        WindowSnipHotkeyBox.Text = HotkeyService.Display(_settings.WindowSnipHotkey);
        DragSnipHotkeyBox.Text = HotkeyService.Display(_settings.DragSnipHotkey);
        LockSnipHotkeyBox.Text = HotkeyService.Display(_settings.LockSnipHotkey);
        LockScreenSnipHotkeyBox.Text = HotkeyService.Display(_settings.QuickSnipHotkey);
        LockWindowSnipHotkeyBox.Text = HotkeyService.Display(_settings.WindowSnipHotkey);
        LockPreviousHotkeyBox.Text = HotkeyService.Display(_settings.LockPreviousHotkey);
        LockNextHotkeyBox.Text = HotkeyService.Display(_settings.LockNextHotkey);
    }

    private void RebindAllHotkeys()
    {
        if (_windowHandle == IntPtr.Zero) return;
        UnregisterAllHotkeys();
        var registered = HotkeyService.RegisterAll(_windowHandle, _settings);
        RegisterLock(HotkeyService.LockPreviousId, _settings.LockPreviousHotkey);
        RegisterLock(HotkeyService.LockNextId, _settings.LockNextHotkey);
        var expected = new[] { _settings.QuickSnipHotkey, _settings.WindowSnipHotkey, _settings.DragSnipHotkey }
            .Count(value => value.IsAssigned);
        if (_settings.LockSnipEnabled && _settings.LockSnipHotkey.IsAssigned) expected++;
        if (_settings.LockSnipEnabled)
            expected += new[] { _settings.LockPreviousHotkey, _settings.LockNextHotkey }
                .Count(value => value.IsAssigned);
        if (registered.Count != expected)
            StatusText.Text = "One or more saved hotkeys are currently used by another application.";

        void RegisterLock(int id, HotkeySetting setting)
        {
            if (_settings.LockSnipEnabled && setting.IsAssigned && HotkeyService.TryRegister(_windowHandle, id, setting))
                registered.Add(id);
        }
    }

    private void UnregisterAllHotkeys()
    {
        if (_windowHandle == IntPtr.Zero) return;
        HotkeyService.Unregister(_windowHandle, HotkeyService.QuickSnipId);
        HotkeyService.Unregister(_windowHandle, HotkeyService.WindowSnipId);
        HotkeyService.Unregister(_windowHandle, HotkeyService.DragSnipId);
        HotkeyService.Unregister(_windowHandle, HotkeyService.LockSnipId);
        HotkeyService.Unregister(_windowHandle, HotkeyService.LockPreviousId);
        HotkeyService.Unregister(_windowHandle, HotkeyService.LockNextId);
    }

    private void Header_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed &&
            e.OriginalSource is not Button)
        {
            DragMove();
        }
    }

    private void LoadControlsFromSettings()
    {
        _loading = true;
        QuickSnipToggle.IsChecked = _settings.QuickSnipEnabled;
        ActiveWindowToggle.IsChecked = _settings.ActiveWindowSnipEnabled;
        DragSnipToggle.IsChecked = _settings.DragSnipEnabled;
        TaskbarModesToggle.IsChecked = _settings.ShowSnipModesInTaskbar;
        LockSnipToggle.IsChecked = _settings.LockSnipEnabled;
        LockDisplayChoice.IsChecked = _settings.LockSnipTarget == "Display";
        LockWindowChoice.IsChecked = _settings.LockSnipTarget == "Window";
        SavePngToggle.IsChecked = _settings.SavePng;
        ClipboardToggle.IsChecked = _settings.CopyToClipboard;
        ToastToggle.IsChecked = _settings.ShowCaptureToast;
        UpdateHotkeyBoxes();
        SelectSaveChoices();
        SaveLocationText.Text = _settings.SaveFolder;
        _loading = false;
        UpdateActionButtons();
        UpdateSaveFormatState();
        UpdateLockSnipState();
    }

    private void UpdateSaveFormatState()
    {
        SaveFileLabel.Text = $"Save {_settings.ImageFormat}";
        var isPng = _settings.ImageFormat == "PNG";
        LosslessBadge.Visibility = isPng ? Visibility.Visible : Visibility.Collapsed;
        QualityChoices.Visibility = isPng ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SelectSaveChoices()
    {
        PngFormatChoice.IsChecked = _settings.ImageFormat == "PNG";
        JpegFormatChoice.IsChecked = _settings.ImageFormat == "JPEG";
        WebpFormatChoice.IsChecked = _settings.ImageFormat == "WebP";
        LowQualityChoice.IsChecked = _settings.ImageQuality == "Low";
        MediumQualityChoice.IsChecked = _settings.ImageQuality == "Medium";
        HighQualityChoice.IsChecked = _settings.ImageQuality == "High";
    }
}
