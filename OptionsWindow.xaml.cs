using System.Reflection;
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

        // Capture modes are exclusive: one mode is always the left-click default.
        if (_settings.QuickSnipEnabled == _settings.ActiveWindowSnipEnabled)
        {
            _settings.QuickSnipEnabled = true;
            _settings.ActiveWindowSnipEnabled = false;
            SettingsService.Save(_settings);
            JumpListService.Register(_settings);
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Build {version?.Major}.{version?.Minor}.{version?.Build}";

        QuickSnipToggle.IsChecked = _settings.QuickSnipEnabled;
        ActiveWindowToggle.IsChecked = _settings.ActiveWindowSnipEnabled;
        SavePngToggle.IsChecked = _settings.SavePng;
        ClipboardToggle.IsChecked = _settings.CopyToClipboard;
        SaveLocationText.Text = _settings.SaveFolder;

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
                              ReferenceEquals(sender, ActiveWindowToggle);

        if (isCaptureToggle)
        {
            if (((CheckBox)sender).IsChecked == true)
            {
                _loading = true;

                if (ReferenceEquals(sender, QuickSnipToggle))
                {
                    ActiveWindowToggle.IsChecked = false;
                }
                else
                {
                    QuickSnipToggle.IsChecked = false;
                }

                _loading = false;
            }
            else if (QuickSnipToggle.IsChecked != true && ActiveWindowToggle.IsChecked != true)
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
        _settings.SavePng = SavePngToggle.IsChecked == true;
        _settings.CopyToClipboard = ClipboardToggle.IsChecked == true;
        SettingsService.Save(_settings);
        JumpListService.Register(_settings);
    }

    private void UpdateActionButtons()
    {
        QuickSnipButton.Visibility = _settings.QuickSnipEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;

        ActiveWindowButton.Visibility = _settings.ActiveWindowSnipEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void QuickSnipButton_Click(object sender, RoutedEventArgs e) =>
        await CaptureAndCloseAsync(CaptureTarget.Display);

    private async void ActiveWindowButton_Click(object sender, RoutedEventArgs e) =>
        await CaptureAndCloseAsync(CaptureTarget.ActiveWindow);

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

    private void InformationButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        var guide = new WelcomeWindow(isFirstRun: false) { Owner = this };
        guide.ContinueRequested += (_, _) => guide.Close();
        guide.Closed += (_, _) => { Show(); Activate(); };
        guide.Show();
        App.CenterOnPrimaryWorkArea(guide);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

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
}
