namespace QuickSnip;

internal sealed class QuickSnipSettings
{
    public bool QuickSnipEnabled { get; set; } = true;
    public bool ActiveWindowSnipEnabled { get; set; } = true;
    public bool DragSnipEnabled { get; set; }
    public bool SavePng { get; set; } = true;
    public bool CopyToClipboard { get; set; } = true;
    public string SaveFolder { get; set; } = SnipFolderService.DefaultPath;
    public WindowPlacementSettings SettingsWindow { get; set; } = new();

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


        SettingsWindow ??= new WindowPlacementSettings();
    }
}

internal sealed class WindowPlacementSettings
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
}
