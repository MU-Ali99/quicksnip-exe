using System.Reflection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace QuickSnip;

public partial class OptionsWindow : Window
{
    private QuickSnipSettings _settings;
    private bool _loading = true;

    public OptionsWindow()
    {
        InitializeComponent();
        _settings = SettingsService.Load();
        WindowPlacementService.Restore(this, _settings.SettingsWindow, 504, 760);
        Closing += OptionsWindow_Closing;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Build {version?.Major}.{version?.Minor}.{version?.Build}";

        QuickSnipToggle.IsChecked = _settings.QuickSnipEnabled;
        ActiveWindowToggle.IsChecked = _settings.ActiveWindowSnipEnabled;
        DragSnipToggle.IsChecked = _settings.DragSnipEnabled;
        SavePngToggle.IsChecked = _settings.SavePng;
        ClipboardToggle.IsChecked = _settings.CopyToClipboard;
        ToastToggle.IsChecked = _settings.ShowCaptureToast;
        SelectSaveChoices();
        SaveLocationText.Text = _settings.SaveFolder;
        UpdateSaveFormatState();

        _loading = false;
        UpdateActionButtons();
    }

    private void SettingToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        var isCaptureToggle = ReferenceEquals(sender, QuickSnipToggle) ||
                              ReferenceEquals(sender, ActiveWindowToggle) ||
                              ReferenceEquals(sender, DragSnipToggle);

        if (isCaptureToggle)
        {
            if (((CheckBox)sender).IsChecked == true)
            {
                _loading = true;

                if (ReferenceEquals(sender, QuickSnipToggle))
                {
                    ActiveWindowToggle.IsChecked = false;
                    DragSnipToggle.IsChecked = false;
                }
                else if (ReferenceEquals(sender, ActiveWindowToggle))
                {
                    QuickSnipToggle.IsChecked = false;
                    DragSnipToggle.IsChecked = false;
                }
                else
                {
                    QuickSnipToggle.IsChecked = false;
                    ActiveWindowToggle.IsChecked = false;
                }

                _loading = false;
            }
            else if (QuickSnipToggle.IsChecked != true &&
                     ActiveWindowToggle.IsChecked != true &&
                     DragSnipToggle.IsChecked != true)
            {
                _loading = true;
                QuickSnipToggle.IsChecked = true;
                _loading = false;
                StatusText.Text = "QuickSnip is the default capture mode.";
            }
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
        if (string.IsNullOrWhiteSpace(StatusText.Text) ||
            !StatusText.Text.Contains("default capture mode"))
        {
            StatusText.Text = "Preferences saved.";
        }
    }

    private void SaveSettingsFromControls()
    {
        _settings.QuickSnipEnabled = QuickSnipToggle.IsChecked == true;
        _settings.ActiveWindowSnipEnabled = ActiveWindowToggle.IsChecked == true;
        _settings.DragSnipEnabled = DragSnipToggle.IsChecked == true;
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
        QuickSnipButton.Visibility = _settings.QuickSnipEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;

        ActiveWindowButton.Visibility = _settings.ActiveWindowSnipEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;

        DragSnipButton.Visibility = _settings.DragSnipEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void QuickSnipButton_Click(object sender, RoutedEventArgs e) =>
        await CaptureAndCloseAsync(CaptureTarget.Display);

    private async void ActiveWindowButton_Click(object sender, RoutedEventArgs e) =>
        await CaptureAndCloseAsync(CaptureTarget.ActiveWindow);

    private async void DragSnipButton_Click(object sender, RoutedEventArgs e) =>
        await CaptureAndCloseAsync(CaptureTarget.Drag);

    private async Task CaptureAndCloseAsync(CaptureTarget target)
    {
        try
        {
            Hide();
            await Task.Delay(150);
            await ScreenCaptureService.CaptureAsync(target, _settings);
            Close();
        }
        catch (Exception exception) when (
            exception is CaptureAlreadyRunningException or CaptureCooldownException)
        {
            Close();
        }
        catch (Exception exception)
        {
            AppLogger.Error("Options capture", exception);
            Show();
            StatusText.Text = "The snip could not complete. Details were saved to the diagnostic log.";
        }
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
        WindowPlacementService.Save(this, _settings.SettingsWindow);
        SettingsService.Save(_settings);
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
        SavePngToggle.IsChecked = _settings.SavePng;
        ClipboardToggle.IsChecked = _settings.CopyToClipboard;
        ToastToggle.IsChecked = _settings.ShowCaptureToast;
        SelectSaveChoices();
        SaveLocationText.Text = _settings.SaveFolder;
        _loading = false;
        UpdateActionButtons();
        UpdateSaveFormatState();
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
