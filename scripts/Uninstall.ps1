$ErrorActionPreference = "Stop"

$installDirectory = Join-Path $env:LOCALAPPDATA "Programs\QuickSnip"
$shortcutPath = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\QuickSnip.lnk"

if (Test-Path -LiteralPath $shortcutPath) {
    Remove-Item -LiteralPath $shortcutPath -Force
}

if (Test-Path -LiteralPath $installDirectory) {
    Remove-Item -LiteralPath $installDirectory -Recurse -Force
}

Write-Output "QuickSnip was removed from the current user profile."
