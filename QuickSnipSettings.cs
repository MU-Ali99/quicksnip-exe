using System.Windows.Input;

namespace QuickSnip;

internal sealed class QuickSnipSettings
{
    public bool QuickSnipEnabled { get; set; } = true;
    public bool ActiveWindowSnipEnabled { get; set; }
    public bool DragSnipEnabled { get; set; }
    public bool ShowSnipModesInTaskbar { get; set; } = true;
    public bool LockSnipEnabled { get; set; }
    public string LockSnipTarget { get; set; } = "Display";
    public bool LockAutoCaptureAvailable { get; set; }
    public bool LockAutoScrollAvailable { get; set; }
    public string LockAutomationSpeed { get; set; } = "Normal";
    public bool SavePng { get; set; } = true;
    public bool CopyToClipboard { get; set; } = true;
    public bool ShowCaptureToast { get; set; } = true;
    public string ImageFormat { get; set; } = "PNG";
    public string ImageQuality { get; set; } = "High";
    public string FilenameStyle { get; set; } = "ModeDateTime";
    public string CustomFilenamePrefix { get; set; } = "quicksnip";
    public string SaveFolder { get; set; } = SnipFolderService.DefaultPath;
    public WindowPlacementSettings SettingsWindow { get; set; } = new();
    public HotkeySetting QuickSnipHotkey { get; set; } = new();
    public HotkeySetting WindowSnipHotkey { get; set; } = new();
    public HotkeySetting DragSnipHotkey { get; set; } = new();
    public HotkeySetting LockSnipHotkey { get; set; } = new();
    public HotkeySetting LockPreviousHotkey { get; set; } = HotkeySetting.Alt(Key.W);
    public HotkeySetting LockNextHotkey { get; set; } = HotkeySetting.Alt(Key.S);

    public void Normalize()
    {
        var enabledModes =
            (QuickSnipEnabled ? 1 : 0) +
            (ActiveWindowSnipEnabled ? 1 : 0) +
            (DragSnipEnabled ? 1 : 0);

        if (enabledModes != 1)
        {
            QuickSnipEnabled = true;
            ActiveWindowSnipEnabled = false;
            DragSnipEnabled = false;
        }

        if (!SavePng && !CopyToClipboard)
        {
            CopyToClipboard = true;
        }

        if (string.IsNullOrWhiteSpace(SaveFolder))
        {
            SaveFolder = SnipFolderService.DefaultPath;
        }

        if (ImageFormat is not ("PNG" or "JPEG" or "WebP"))
        {
            ImageFormat = "PNG";
        }

        if (ImageQuality is not ("Low" or "Medium" or "High"))
        {
            ImageQuality = "High";
        }

        if (FilenameStyle is not ("ModeDateTime" or "WindowDateTime" or "DateTime" or "Custom"))
        {
            FilenameStyle = "ModeDateTime";
        }

        CustomFilenamePrefix ??= "quicksnip";

        if (LockSnipTarget is not ("Display" or "Window"))
        {
            LockSnipTarget = "Display";
        }

        if (LockAutomationSpeed is not ("Slow" or "Normal" or "Fast"))
        {
            LockAutomationSpeed = "Normal";
        }



        SettingsWindow ??= new WindowPlacementSettings();
        QuickSnipHotkey ??= new HotkeySetting();
        WindowSnipHotkey ??= new HotkeySetting();
        DragSnipHotkey ??= new HotkeySetting();
        LockSnipHotkey ??= new HotkeySetting();
        LockPreviousHotkey ??= HotkeySetting.Alt(Key.W);
        LockNextHotkey ??= HotkeySetting.Alt(Key.S);
    }

    public void RestoreDefaultsPreservingUserData()
    {
        var saveFolder = SaveFolder;
        var placement = SettingsWindow;
        var defaults = new QuickSnipSettings();

        QuickSnipEnabled = defaults.QuickSnipEnabled;
        ActiveWindowSnipEnabled = defaults.ActiveWindowSnipEnabled;
        DragSnipEnabled = defaults.DragSnipEnabled;
        ShowSnipModesInTaskbar = defaults.ShowSnipModesInTaskbar;
        LockSnipEnabled = defaults.LockSnipEnabled;
        LockSnipTarget = defaults.LockSnipTarget;
        LockAutoCaptureAvailable = defaults.LockAutoCaptureAvailable;
        LockAutoScrollAvailable = defaults.LockAutoScrollAvailable;
        LockAutomationSpeed = defaults.LockAutomationSpeed;
        SavePng = defaults.SavePng;
        CopyToClipboard = defaults.CopyToClipboard;
        ShowCaptureToast = defaults.ShowCaptureToast;
        ImageFormat = defaults.ImageFormat;
        ImageQuality = defaults.ImageQuality;
        FilenameStyle = defaults.FilenameStyle;
        CustomFilenamePrefix = defaults.CustomFilenamePrefix;
        SaveFolder = saveFolder;
        SettingsWindow = placement;
        QuickSnipHotkey = new HotkeySetting();
        WindowSnipHotkey = new HotkeySetting();
        DragSnipHotkey = new HotkeySetting();
        LockSnipHotkey = new HotkeySetting();
        LockPreviousHotkey = HotkeySetting.Alt(Key.W);
        LockNextHotkey = HotkeySetting.Alt(Key.S);
    }

    public bool HasAnyHotkey() =>
        QuickSnipHotkey.IsAssigned || WindowSnipHotkey.IsAssigned || DragSnipHotkey.IsAssigned ||
        (LockSnipEnabled && LockSnipHotkey.IsAssigned);
}

internal sealed class HotkeySetting
{
    public int Modifiers { get; set; }
    public int VirtualKey { get; set; }
    public bool IsAssigned => Modifiers != 0 && VirtualKey != 0;

    public HotkeySetting Clone() => new() { Modifiers = Modifiers, VirtualKey = VirtualKey };

    public static HotkeySetting Alt(System.Windows.Input.Key key) => new()
    {
        Modifiers = (int)System.Windows.Input.ModifierKeys.Alt,
        VirtualKey = System.Windows.Input.KeyInterop.VirtualKeyFromKey(key)
    };
}

internal sealed class WindowPlacementSettings
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
}
