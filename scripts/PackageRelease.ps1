$ErrorActionPreference = "Stop"

$projectRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..")
$releaseDirectory = Join-Path $projectRoot "artifacts\release"
$portableDirectory = Join-Path $projectRoot "artifacts\portable\QuickSnip-0.7.0-win-x64"
$portableArchive = Join-Path $releaseDirectory "QuickSnip-Portable-0.7.0-win-x64.zip"

& (Join-Path $PSScriptRoot "BuildInstaller.ps1")

if ($LASTEXITCODE -ne 0) {
    throw "QuickSnip installer build failed."
}

[System.IO.Directory]::CreateDirectory($releaseDirectory) | Out-Null
[System.IO.Directory]::CreateDirectory($portableDirectory) | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot "artifacts\publish\win-x64\QuickSnip.exe") `
    -Destination (Join-Path $portableDirectory "QuickSnip.exe") -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "README.md") `
    -Destination (Join-Path $portableDirectory "README.md") -Force

if (Test-Path -LiteralPath $portableArchive) {
    Remove-Item -LiteralPath $portableArchive -Force
}

Compress-Archive -Path (Join-Path $portableDirectory "*") -DestinationPath $portableArchive
Copy-Item -LiteralPath (Join-Path $projectRoot "artifacts\installer\QuickSnip-Setup-0.7.0-win-x64.exe") `
    -Destination $releaseDirectory -Force

Get-ChildItem -LiteralPath $releaseDirectory | Select-Object FullName, Length
