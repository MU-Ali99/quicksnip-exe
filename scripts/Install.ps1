$ErrorActionPreference = "Stop"

$projectRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..")
$publishedExecutable = Join-Path $projectRoot "artifacts\publish\win-x64\QuickSnip.exe"
$installDirectory = Join-Path $env:LOCALAPPDATA "Programs\QuickSnip"
$installedExecutable = Join-Path $installDirectory "QuickSnip.exe"
$startMenuDirectory = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
$shortcutPath = Join-Path $startMenuDirectory "QuickSnip.lnk"

if (-not (Test-Path -LiteralPath $publishedExecutable)) {
    throw "Publish QuickSnip before installing it. Run .\scripts\Publish.ps1."
}

[System.IO.Directory]::CreateDirectory($installDirectory) | Out-Null
Copy-Item -LiteralPath $publishedExecutable -Destination $installedExecutable -Force

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $installedExecutable
$shortcut.WorkingDirectory = $installDirectory
$shortcut.IconLocation = "$installedExecutable,0"
$shortcut.Description = "Take a screenshot with QuickSnip"
$shortcut.Save()

$onboardingMarker = Join-Path $env:LOCALAPPDATA "QuickSnip\onboarding-complete"
if (Test-Path -LiteralPath $onboardingMarker) {
    Remove-Item -LiteralPath $onboardingMarker -Force
}

$registrationProcess = Start-Process `
    -FilePath $installedExecutable `
    -ArgumentList "--register-jump-list" `
    -Wait `
    -PassThru

if ($registrationProcess.ExitCode -ne 0) {
    throw "QuickSnip was installed, but Jump List registration failed."
}

# Remove only the superseded RightSnip program and shortcut.
$oldShortcut = Join-Path $startMenuDirectory "RightSnip.lnk"
$oldInstallDirectory = Join-Path $env:LOCALAPPDATA "Programs\RightSnip"

if (Test-Path -LiteralPath $oldShortcut) {
    Remove-Item -LiteralPath $oldShortcut -Force
}

if (Test-Path -LiteralPath $oldInstallDirectory) {
    Remove-Item -LiteralPath $oldInstallDirectory -Recurse -Force
}

Write-Output "Installed: $installedExecutable"
Write-Output "Shortcut: $shortcutPath"
Write-Output "Jump List: alternate snip modes, Open Snips Folder, QuickSnip Settings"
Write-Output "Open Start, search for QuickSnip, then choose Pin to taskbar."
Write-Output "Existing Pictures\RightSnip screenshots were left untouched."
