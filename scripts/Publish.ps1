$ErrorActionPreference = "Stop"

$projectRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..")
$project = Join-Path $projectRoot "QuickSnip.csproj"
$publishDirectory = Join-Path $projectRoot "artifacts\publish\win-x64"

dotnet publish $project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw "QuickSnip publish failed."
}

Write-Output (Join-Path $publishDirectory "QuickSnip.exe")
