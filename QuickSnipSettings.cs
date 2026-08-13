namespace QuickSnip;

internal sealed class QuickSnipSettings
{
    public bool QuickSnipEnabled { get; set; } = true;
    public bool ActiveWindowSnipEnabled { get; set; } = true;
    public bool SavePng { get; set; } = true;
    public bool CopyToClipboard { get; set; } = true;
    public string SaveFolder { get; set; } = SnipFolderService.DefaultPath;

    public void Normalize()
    {
        if (!QuickSnipEnabled && !ActiveWindowSnipEnabled)
        {
            QuickSnipEnabled = true;
        }

        if (!SavePng && !CopyToClipboard)
        {
            CopyToClipboard = true;
        }

        if (string.IsNullOrWhiteSpace(SaveFolder))
        {
            SaveFolder = SnipFolderService.DefaultPath;
        }
    }
}
