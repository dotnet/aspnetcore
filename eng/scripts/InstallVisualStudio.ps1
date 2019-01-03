<#
.SYNOPSIS
    Installs or updates Visual Studio on a local developer machine.
.DESCRIPTION
    This installs Visual Studio along with all the workloads required to contribute to this repository.
.PARAMETER Edition
    Must be one of these values:

        Community
        Professional
        Enterprise

    Selects which 'offering' of Visual Studio to install.

.PARAMETER InstallPath
    The location of Visual Studio
.PARAMETER Passive
    Run the installer without requiring interaction.
.LINK
    https://visualstudio.com
    https://github.com/aspnet/AspNetCore/blob/master/docs/BuildFromSource.md
.EXAMPLE
    To install VS 2017 Community, run

        InstallVisualStudio.ps1 -Edition Community
#>
[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [ValidateSet('Community', 'Professional', 'Enterprise')]
    [string]$Edition,
    [string]$InstallPath,
    [switch]$Passive
)

if (-not $Edition) {
    Write-Host "You must specify a value for the -Edition parameter which selects the kind of Visual Studio to install." -f Red
    Write-Host "Run ``Get-Help $PSCommandPath`` for more details." -f Red
    Write-Host ""
    Write-Host "Example:  ./InstallVisualStudio -Edition Community" -f Red
    Write-Host ""
    exit 1
}

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$intermedateDir = "$PSScriptRoot\obj"
mkdir $intermedateDir -ErrorAction Ignore | Out-Null

$bootstrapper = "$intermedateDir\vsinstaller.exe"
Invoke-WebRequest -Uri "https://aka.ms/vs/15/release/vs_$($Edition.ToLowerInvariant()).exe" -OutFile $bootstrapper

if (-not $InstallPath) {
    $InstallPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\$Edition"
}

# no backslashes - this breaks the installer
$InstallPath = $InstallPath.TrimEnd('\')

[string[]] $arguments = @()

if (Test-path $InstallPath) {
    $arguments += 'modify'
}

$arguments += `
    '--productId', "Microsoft.VisualStudio.Product.$Edition", `
    '--installPath', "`"$InstallPath`"", `
    '--in', "$PSScriptRoot\vs.json", `
    '--norestart'

if ($Passive) {
    $arguments += '--passive'
}

Write-Host ""
Write-Host "Installing Visual Studio 2017 $Edition" -f Magenta
Write-Host ""
Write-Host "Running '$bootstrapper $arguments'"

& $bootstrapper @arguments