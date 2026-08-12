$ErrorActionPreference = "Stop"

$projectRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..")
$publishedExecutable = Join-Path $projectRoot "artifacts\publish\win-x64\RightSnip.exe"
$installDirectory = Join-Path $env:LOCALAPPDATA "Programs\RightSnip"
$installedExecutable = Join-Path $installDirectory "RightSnip.exe"
$startMenuDirectory = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
$shortcutPath = Join-Path $startMenuDirectory "RightSnip.lnk"

if (-not (Test-Path -LiteralPath $publishedExecutable)) {
    throw "Publish RightSnip before installing it. Run .\scripts\Publish.ps1."
}

[System.IO.Directory]::CreateDirectory($installDirectory) | Out-Null
Copy-Item -LiteralPath $publishedExecutable -Destination $installedExecutable -Force

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $installedExecutable
$shortcut.WorkingDirectory = $installDirectory
$shortcut.IconLocation = "$installedExecutable,0"
$shortcut.Description = "Take a screenshot with RightSnip"
$shortcut.Save()

Write-Output "Installed: $installedExecutable"
Write-Output "Shortcut: $shortcutPath"
Write-Output "Open Start, search for RightSnip, then choose Pin to taskbar."
