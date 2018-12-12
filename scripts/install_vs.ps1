<#
.SYNOPSIS
    Installs or updates Visual Studio on a local developer machine
.PARAMETER Update
    Update VS to latest version instead of modifying the installation to include new workloads.
.PARAMETER Quiet
    Whether to run installer in the background
#>
[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [switch]$Update,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$intermedateDir = "$PSScriptRoot\obj"
mkdir $intermedateDir -ErrorAction Ignore | Out-Null

$bootstrapper = "$intermedateDir\vs_enterprise1.exe"
Invoke-WebRequest -Uri 'https://aka.ms/vs/15/release/vs_enterprise.exe' -OutFile $bootstrapper

$vsJson = "$PSScriptRoot\VsRequirements\vs.json"
# no backslashes - this breaks the installer
$vsInstallPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Enterprise"
$arguments = @(
    '--installPath', "`"$vsInstallPath`"",
    '--in', $vsJson,
    '--wait',
    '--norestart')

if ($Update) {
    $arguments = ,'update' + $arguments
}
else {
    $arguments = ,'modify' + $arguments
}

if ($Quiet) {
    $arguments += '--quiet'
}

Write-Host "Running '$bootstrapper $arguments' on $(hostname)"
$process = Start-Process -FilePath $bootstrapper `
    -ArgumentList $arguments `
    -Verb runas `
    -PassThru `
    -ErrorAction Stop
Write-Host "pid = $($process.Id)"
Wait-Process -InputObject $process
Write-Host "exit code = $($process.ExitCode)"

# https://docs.microsoft.com/en-us/visualstudio/install/use-command-line-parameters-to-install-visual-studio#error-codes
if ($process.ExitCode -eq 3010) {
    Write-Warning "Agent $(hostname) requires restart to finish the VS update"
}
elseif ($process.ExitCode -eq 5007) {
    Write-Error "Operation was blocked - the computer does not meet the requirements"
}
elseif (($process.ExitCode -eq 5004) -or ($process.ExitCode -eq 1602)) {
    Write-Error "Operation was canceled"
}
elseif ($process.ExitCode -ne 0) {
    Write-Error "Installation failed on $(hostname) for unknown reason"
}