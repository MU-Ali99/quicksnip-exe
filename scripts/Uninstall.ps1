$ErrorActionPreference = "Stop"

$installDirectory = Join-Path $env:LOCALAPPDATA "Programs\RightSnip"
$shortcutPath = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\RightSnip.lnk"

if (Test-Path -LiteralPath $shortcutPath) {
    Remove-Item -LiteralPath $shortcutPath -Force
}

if (Test-Path -LiteralPath $installDirectory) {
    Remove-Item -LiteralPath $installDirectory -Recurse -Force
}

Write-Output "RightSnip was removed from the current user profile."
