[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot

try
{
    if (-not (Get-Command Compress-Archive -ErrorAction SilentlyContinue))
    {
        throw "Compress-Archive is not available in this PowerShell session."
    }

    $project = Get-ChildItem *.csproj | Select-Object -First 1
    if (-not $project)
    {
        throw "No project file was found in $PSScriptRoot."
    }

    $projectName = $project.BaseName
    $properties = dotnet msbuild $project.FullName -nologo -getProperty:TargetFramework,BarkDeployPath | ConvertFrom-Json
    $targetFramework = $properties.Properties.TargetFramework

    $versionMatch = Select-String -Path "Plugin.cs" -Pattern 'MelonInfo\(typeof\(Plugin\),\s*"[^"]+",\s*"([^"]+)"'
    $version = if ($versionMatch.Matches.Count -gt 0) { $versionMatch.Matches[0].Groups[1].Value } else { "dev" }

    if (-not $SkipBuild)
    {
        & "$PSScriptRoot\Test-PCVR-Environment.ps1"
        if ($LASTEXITCODE -ne 0)
        {
            throw "PCVR environment validation failed. Fix the reported paths before packaging Bark."
        }

        dotnet build $project.FullName -c Release
    }

    $dllPath = Join-Path $PSScriptRoot "bin\Release\$targetFramework\$projectName.dll"
    if (-not (Test-Path $dllPath))
    {
        throw "Build output '$dllPath' was not found."
    }

    $artifactRoot = Join-Path $PSScriptRoot "artifacts\release"
    $stageRoot = Join-Path $artifactRoot $projectName
    $modsRoot = Join-Path $stageRoot "Mods"
    $zipPath = Join-Path $artifactRoot "$projectName-v$version-pcvr.zip"

    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

    New-Item -ItemType Directory -Path $modsRoot -Force | Out-Null
    Copy-Item $dllPath -Destination (Join-Path $modsRoot "$projectName.dll")

    $pdbPath = Join-Path $PSScriptRoot "bin\Release\$targetFramework\$projectName.pdb"
    if (Test-Path $pdbPath)
    {
        Copy-Item $pdbPath -Destination (Join-Path $modsRoot "$projectName.pdb")
    }

    Compress-Archive -Path (Join-Path $stageRoot "*") -DestinationPath $zipPath

    Write-Host "Packaged Bark for PCVR deployment:"
    Write-Host "  Zip: $zipPath"
    Write-Host "  Install: extract the Mods folder into your Gorilla Tag game directory."
}
finally
{
    Pop-Location
}
