[CmdletBinding()]
param(
    [string]$SetupRoot = "$env:USERPROFILE\Downloads\Bark-Quest-Setup"
)

$ErrorActionPreference = "Stop"

$questPatcherExe = Join-Path $SetupRoot "QuestPatcher\QuestPatcher.exe"
$adbExe = Join-Path $SetupRoot "platform-tools\adb.exe"

$checks = @(
    @{
        Item = "QuestPatcher"
        Path = $questPatcherExe
        Help = "QuestPatcher should exist at $questPatcherExe."
    },
    @{
        Item = "adb"
        Path = $adbExe
        Help = "Android platform-tools should exist at $adbExe."
    }
)

$results = foreach ($check in $checks)
{
    [pscustomobject]@{
        Item = $check.Item
        Exists = Test-Path $check.Path
        Path = $check.Path
        Help = $check.Help
    }
}

Write-Host "Bark Quest standalone environment check"
$results | Format-Table -AutoSize

$missing = $results | Where-Object { -not $_.Exists }
if ($missing)
{
    Write-Host ""
    Write-Host "Missing local tools:"
    foreach ($item in $missing)
    {
        Write-Host "  - $($item.Item): $($item.Help)"
    }
    exit 1
}

$adbOutput = & $adbExe devices
Write-Host ""
Write-Host "adb devices"
$adbOutput | ForEach-Object { Write-Host "  $_" }

$deviceLines = $adbOutput | Select-Object -Skip 1 | Where-Object { $_.Trim() }
if (-not $deviceLines)
{
    Write-Host ""
    Write-Host "No Quest device detected."
    Write-Host "  - Enable Developer Mode in the Meta Horizon mobile app."
    Write-Host "  - Connect the headset by USB."
    Write-Host "  - Accept the USB debugging prompt inside the headset."
    exit 1
}

$unauthorized = $deviceLines | Where-Object { $_ -match '\sunauthorized$' }
if ($unauthorized)
{
    Write-Host ""
    Write-Host "Quest detected but USB debugging is not authorized yet."
    Write-Host "  - Put on the headset and accept the USB debugging prompt."
    exit 1
}

Write-Host ""
Write-Host "Connected Quest device details"
& $adbExe shell getprop ro.product.model | ForEach-Object { Write-Host "  Model: $_" }
& $adbExe shell getprop ro.build.version.release | ForEach-Object { Write-Host "  Android: $_" }

Write-Host ""
Write-Host "Installed Gorilla Tag packages"
$packages = & $adbExe shell pm list packages
$packages |
    Where-Object { $_ -match 'AnotherAxiom|gorilla' } |
    ForEach-Object { Write-Host "  $_" }
