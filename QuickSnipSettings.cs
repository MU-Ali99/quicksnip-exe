namespace QuickSnip;

internal sealed class QuickSnipSettings
{
    public bool QuickSnipEnabled { get; set; }
    public bool ActiveWindowSnipEnabled { get; set; }
    public bool DragSnipEnabled { get; set; } = true;
    public bool SavePng { get; set; } = true;
    public bool CopyToClipboard { get; set; } = true;
    public bool ShowCaptureToast { get; set; } = true;
    public string ImageFormat { get; set; } = "PNG";
    public string ImageQuality { get; set; } = "High";
    public string SaveFolder { get; set; } = SnipFolderService.DefaultPath;
    public WindowPlacementSettings SettingsWindow { get; set; } = new();
    public HotkeySetting QuickSnipHotkey { get; set; } = new();
    public HotkeySetting WindowSnipHotkey { get; set; } = new();
    public HotkeySetting DragSnipHotkey { get; set; } = new();

    public void Normalize()
    {
        var enabledModes =
            (QuickSnipEnabled ? 1 : 0) +
            (ActiveWindowSnipEnabled ? 1 : 0) +
            (DragSnipEnabled ? 1 : 0);

        if (enabledModes != 1)
        {
            QuickSnipEnabled = false;
            ActiveWindowSnipEnabled = false;
            DragSnipEnabled = true;
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



        SettingsWindow ??= new WindowPlacementSettings();
        QuickSnipHotkey ??= new HotkeySetting();
        WindowSnipHotkey ??= new HotkeySetting();
        DragSnipHotkey ??= new HotkeySetting();
    }

    public void RestoreDefaultsPreservingUserData()
    {
        var saveFolder = SaveFolder;
        var placement = SettingsWindow;
        var defaults = new QuickSnipSettings();

        QuickSnipEnabled = defaults.QuickSnipEnabled;
        ActiveWindowSnipEnabled = defaults.ActiveWindowSnipEnabled;
        DragSnipEnabled = defaults.DragSnipEnabled;
        SavePng = defaults.SavePng;
        CopyToClipboard = defaults.CopyToClipboard;
        ShowCaptureToast = defaults.ShowCaptureToast;
        ImageFormat = defaults.ImageFormat;
        ImageQuality = defaults.ImageQuality;
        SaveFolder = saveFolder;
        SettingsWindow = placement;
        QuickSnipHotkey = new HotkeySetting();
        WindowSnipHotkey = new HotkeySetting();
        DragSnipHotkey = new HotkeySetting();
    }

    public bool HasAnyHotkey() =>
        QuickSnipHotkey.IsAssigned || WindowSnipHotkey.IsAssigned || DragSnipHotkey.IsAssigned;
}

internal sealed class HotkeySetting
{
    public int Modifiers { get; set; }
    public int VirtualKey { get; set; }
    public bool IsAssigned => Modifiers != 0 && VirtualKey != 0;

    public HotkeySetting Clone() => new() { Modifiers = Modifiers, VirtualKey = VirtualKey };
}

internal sealed class WindowPlacementSettings
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
}
