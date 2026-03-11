[CmdletBinding()]
param(
    [switch]$Build
)

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot

try
{
    $project = Get-ChildItem *.csproj | Select-Object -First 1
    if (-not $project)
    {
        throw "No .csproj file was found in $PSScriptRoot."
    }

    $propertyNames = @(
        "TargetFramework",
        "GamePath",
        "GameAssemblyPath",
        "MelonLoaderAssemblyPath",
        "ModsPath",
        "BarkDeployPath",
        "GorillaLibraryPath",
        "GorillaLibraryGameModesPath"
    )

    $propertyJson = dotnet msbuild $project.FullName -nologo "-getProperty:$($propertyNames -join ',')"
    $properties = ($propertyJson | ConvertFrom-Json).Properties

    $checks = @(
        @{ Label = "GamePath"; Path = $properties.GamePath; Required = $true; Help = "Install the Steam build of Gorilla Tag or override GamePath in Directory.Build.user.props." },
        @{ Label = "GameAssemblyPath"; Path = $properties.GameAssemblyPath; Required = $true; Help = "This should point to Gorilla Tag_Data\\Managed." },
        @{ Label = "MelonLoaderAssemblyPath"; Path = $properties.MelonLoaderAssemblyPath; Required = $true; Help = "Install the PC loader/runtime required by this branch or override MelonLoaderAssemblyPath." },
        @{ Label = "ModsPath"; Path = $properties.ModsPath; Required = $true; Help = "This is Bark's standardized deploy folder for PCVR." },
        @{ Label = "BarkDeployPath"; Path = $properties.BarkDeployPath; Required = $true; Help = "This is where post-build deploy copies Bark.dll." },
        @{ Label = "GorillaLibraryPath"; Path = $properties.GorillaLibraryPath; Required = $true; Help = "Place GorillaLibrary.dll in Mods or override GorillaLibraryPath." },
        @{ Label = "GorillaLibraryGameModesPath"; Path = $properties.GorillaLibraryGameModesPath; Required = $true; Help = "Place GorillaLibrary.GameModes.dll in Mods or override GorillaLibraryGameModesPath." }
    )

    $results = foreach ($check in $checks)
    {
        [pscustomobject]@{
            Item = $check.Label
            Exists = Test-Path $check.Path
            Path = $check.Path
            Help = $check.Help
        }
    }

    Write-Host "Bark PCVR environment check"
    Write-Host "  Project: $($project.Name)"
    Write-Host "  TargetFramework: $($properties.TargetFramework)"
    $results | Format-Table -AutoSize

    $missing = $results | Where-Object { -not $_.Exists }
    if ($missing)
    {
        Write-Host ""
        Write-Host "Missing prerequisites:"
        foreach ($item in $missing)
        {
            Write-Host "  - $($item.Item): $($item.Help)"
        }

        exit 1
    }

    if ($Build)
    {
        dotnet build $project.FullName -c Release
    }
}
finally
{
    Pop-Location
}
