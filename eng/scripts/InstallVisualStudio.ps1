<#
.SYNOPSIS
    Installs or updates Visual Studio on a local developer machine.
.DESCRIPTION
    This installs Visual Studio along with all the workloads required to contribute to this repository.
.PARAMETER Edition
    Selects which 'offering' of Visual Studio to install. Must be one of these values:
        BuildTools
        Community (the default)
        Professional
        Enterprise
.PARAMETER Channel
    Selects which channel of Visual Studio to install. Must be one of these values:
        Release (the default)
        Preview
.PARAMETER Version
    Selects which version of Visual Studio to install. Must be one of these values:
        2022
.PARAMETER InstallPath
    The location on disk where Visual Studio should be installed or updated. Default path is location of latest
    existing installation of the specified edition, if any. If that VS edition is not currently installed, default
    path is '${env:ProgramFiles}\Microsoft Visual Studio\`$Version\`$Edition".
.PARAMETER Passive
    Run the installer without requiring interaction.
.PARAMETER Quiet
    Run the installer without UI and wait for installation to complete.
.LINK
    https://visualstudio.com
    https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md
.EXAMPLE
    To install VS 2022 Community, run this command in PowerShell:

        .\InstallVisualStudio.ps1
#>
param(
    [ValidateSet('BuildTools','Community', 'Professional', 'Enterprise')]
    [string]$Edition = 'Community',
    [ValidateSet('Release', 'Preview', 'IntPreview', 'Dogfood')]
    [string]$Channel = 'Release',
    [ValidateSet('2022')]
    [string]$Version = '2022',
    [string]$InstallPath,
    [switch]$Passive,
    [switch]$Quiet
)

if ($env:TF_BUILD) {
    Write-Error 'This script is not intended for use on CI. It is only meant to be used to install a local developer environment. If you need to change Visual Studio requirements in CI agents, contact the @aspnet/build team.'
    exit 1
}

if ($Passive -and $Quiet) {
    Write-Host -ForegroundColor Red "Error: The -Passive and -Quiet options cannot be used together."
    Write-Host -ForegroundColor Red "Run ``Get-Help $PSCommandPath`` for more details."
    exit 1
}

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$intermedateDir = "$PSScriptRoot\obj"
mkdir $intermedateDir -ErrorAction Ignore | Out-Null

$bootstrapper = "$intermedateDir\vsinstaller.exe"
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

if ("$Version" -eq "2022") {
    $vsversion = 17;
}
$channelUri = "https://aka.ms/vs/$vsversion/release"
$responseFileName = "vs.$vsversion"
if ("$Edition" -eq "BuildTools") {
    $responseFileName += ".buildtools"
}
if ("$Channel" -eq "Dogfood") {
    $Channel = "IntPreview"
}
if ("$Channel" -eq "Preview") {
    $responseFileName += ".preview"
    $channelUri = "https://aka.ms/vs/$vsversion/pre"
} elseif ("$Channel" -eq "IntPreview") {
    $responseFileName += ".intpreview"
    $channelUri = "https://aka.ms/vs/$vsversion/intpreview"
}

$responseFile = "$PSScriptRoot\$responseFileName.json"
$channelId = (Get-Content $responseFile | ConvertFrom-Json).channelId

$bootstrapperUri = "$channelUri/vs_$($Edition.ToLowerInvariant()).exe"
Write-Host "Downloading Visual Studio $Version $Edition ($Channel) bootstrapper from $bootstrapperUri"
Invoke-WebRequest -Uri $bootstrapperUri -OutFile $bootstrapper

$productId = "Microsoft.VisualStudio.Product.$Edition"
if (-not $InstallPath) {
    $vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vsWhere)
    {
        $installations = & $vsWhere -version "[$vsversion,$($vsversion+1))" -format json -prerelease -products $productId | ConvertFrom-Json |Sort-Object -Descending -Property installationVersion, installDate
        foreach ($installation in $installations) {
            Write-Host "Found '$($installation.installationName)' in '$($installation.installationPath)', channel = '$($installation.channelId)'"
            if ($installation.channelId -eq $channelId) {
                $InstallPath = $installation.installationPath
                break
            }
        }
    }
}

if (-not $InstallPath) {
    if ($vsversion -eq "17") {
        $pathPrefix = "${env:ProgramFiles}";
    }
    if ("$Channel" -eq "Preview") {
        $InstallPath = "$pathPrefix\Microsoft Visual Studio\$Version\${Edition}_Pre"
    } elseif ("$Channel" -eq "IntPreview") {
        $InstallPath = "$pathPrefix\Microsoft Visual Studio\$Version\${Edition}_IntPre"
    } else {
        $InstallPath = "$pathPrefix\Microsoft Visual Studio\$Version\$Edition"
    }
}

# no backslashes - this breaks the installer
$InstallPath = $InstallPath.TrimEnd('\')

[string[]] $arguments = @()
if (Test-path $InstallPath) {
    $arguments += 'modify'
}

$arguments += `
    '--productId', $productId, `
    '--installPath', "`"$InstallPath`"", `
    '--in', "`"$responseFile`""

if ($Passive) {
    $arguments += '--passive', '--norestart'
}
if ($Quiet) {
    $arguments += '--quiet', '--wait', '--norestart'
}

Write-Host
Write-Host "Installing Visual Studio $Version $Edition ($Channel)" -f Magenta
Write-Host
Write-Host "Running '$bootstrapper $arguments'"

foreach ($i in 0, 1, 2) {
    if ($i -ne 0) {
        Write-Host "Retrying..."
    }

    $process = Start-Process -FilePath "$bootstrapper" -ArgumentList $arguments -ErrorAction Continue -PassThru `
        -RedirectStandardError "$intermedateDir\errors.txt" -Verbose -Wait
    Write-Host "Exit code = $($process.ExitCode)."
    if ($process.ExitCode -eq 0) {
        break
    } else {
        # https://learn.microsoft.com/visualstudio/install/use-command-line-parameters-to-install-visual-studio#error-codes
        if ($process.ExitCode -eq 3010) {
            Write-Host -ForegroundColor Red "Error: Installation requires restart to finish the VS update."
            break
        }
        elseif ($process.ExitCode -eq 5007) {
            Write-Host -ForegroundColor Red "Error: Operation was blocked - the computer does not meet the requirements."
            break
        }
        elseif (($process.ExitCode -eq 5004) -or ($process.ExitCode -eq 1602)) {
            Write-Host -ForegroundColor Red "Error: Operation was canceled."
        }
        else {
            Write-Host -ForegroundColor Red "Error: Installation failed for an unknown reason."
        }

        Write-Host
        Write-Host "Errors:"
        Get-Content "$intermedateDir\errors.txt" | Write-Warning
        Write-Host

        Get-ChildItem $env:Temp\dd_bootstrapper_*.log |Sort-Object CreationTime -Descending |Select-Object -First 1 |% {
            Write-Host "${_}:"
            Get-Content "$_"
            Write-Host
        }

        $clientLogs = Get-ChildItem $env:Temp\dd_client_*.log |Sort-Object CreationTime -Descending |Select-Object -First 1 |% {
            Write-Host "${_}:"
            Get-Content "$_"
            Write-Host
        }

        $setupLogs = Get-ChildItem $env:Temp\dd_setup_*.log |Sort-Object CreationTime -Descending |Select-Object -First 1 |% {
            Write-Host "${_}:"
            Get-Content "$_"
            Write-Host
        }
    }
}

Remove-Item "$intermedateDir\errors.txt" -errorAction SilentlyContinue
exit $process.ExitCode
