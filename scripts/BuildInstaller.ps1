$ErrorActionPreference = "Stop"

$projectRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..")
$compilerCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
$script = Join-Path $projectRoot "installer\QuickSnip.iss"

if (-not $compiler) {
    throw "Inno Setup 6 is required. Install JRSoftware.InnoSetup with winget."
}

& (Join-Path $PSScriptRoot "Publish.ps1")

if ($LASTEXITCODE -ne 0) {
    throw "QuickSnip publish failed."
}

& $compiler $script

if ($LASTEXITCODE -ne 0) {
    throw "QuickSnip installer build failed."
}

Write-Output (Join-Path $projectRoot "artifacts\installer\QuickSnip-Setup-0.8.0-win-x64.exe")
