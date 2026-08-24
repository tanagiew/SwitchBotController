[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$')]
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $repoRoot 'src\SwitchBotController.App\SwitchBotController.App.csproj'
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$publishRoot = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot 'publish'))
$publishDirectory = [System.IO.Path]::GetFullPath((Join-Path $publishRoot 'SwitchBotController-win-x64'))
$archivePath = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot "SwitchBotController-v$Version-win-x64.zip"))

function Assert-UnderArtifacts {
    param([Parameter(Mandatory)][string]$Path)

    $prefix = $artifactsRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $Path.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the artifacts directory: $Path"
    }
}

Assert-UnderArtifacts -Path $publishDirectory
Assert-UnderArtifacts -Path $archivePath

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --output $publishDirectory `
    -p:Platform=x64 `
    -p:PublishProfile=win-x64-portable

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath (Join-Path $repoRoot 'config.json.example') -Destination $publishDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $publishDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE.md') -Destination $publishDirectory
$releaseImagesDirectory = Join-Path $publishDirectory 'docs\images'
New-Item -ItemType Directory -Path $releaseImagesDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $repoRoot 'docs\images\*') -Destination $releaseImagesDirectory

Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $archivePath -CompressionLevel Optimal

Write-Host "Release archive created: $archivePath"
