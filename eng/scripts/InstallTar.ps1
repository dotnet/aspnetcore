<#
.SYNOPSIS
    Finds or installs the Tar command on this system.
.DESCRIPTION
    This script searches for Tar on this system. If not found, downloads and extracts Git to use its tar.exe. Prefers
    global installation locations even if Git has been downloaded into this repo.
.PARAMETER GitVersion
    The version of the Git to install. If not set, the default value is read from global.json.
.PARAMETER Force
    Overwrite the existing installation if one exists in this repo and Tar isn't installed globally.
#>
param(
    [string]$GitVersion,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

# Find tar. If not found, install Git to get it.
$repoRoot = (Join-Path $PSScriptRoot "..\.." -Resolve)
$installDir = "$repoRoot\.tools\Git\win-x64"
$tarCommand = "$installDir\usr\bin\tar.exe"
$finalCommand = "$repoRoot\.tools\tar.exe"

Write-Host "Windows version and other information..."
cmd.exe /c ver
systeminfo.exe
Write-Host "Processor Architecture: $env:PROCESSOR_ARCHITECTURE"

Write-Host "Checking $env:SystemRoot\System32\tar.exe"
Get-ChildItem "$env:SystemRoot\System32\ta*.exe"
if (Test-Path "$env:SystemRoot\System32\tar.exe") {
    Write-Host "Found $env:SystemRoot\System32\tar.exe"
    $tarCommand = "$env:SystemRoot\System32\tar.exe"
}
elseif (Test-Path "$env:ProgramFiles\Git\usr\bin\tar.exe") {
    $tarCommand = "$env:ProgramFiles\Git\usr\bin\tar.exe"
}
elseif (Test-Path "${env:ProgramFiles(x86)}\Git\usr\bin\tar.exe") {
    $tarCommand = "${env:ProgramFiles(x86)}\Git\usr\bin\tar.exe"
}
elseif (Test-Path "$env:AGENT_HOMEDIRECTORY\externals\git\usr\bin\tar.exe") {
    $tarCommand = "$env:AGENT_HOMEDIRECTORY\externals\git\usr\bin\tar.exe"
}
elseif ((Test-Path $tarCommand) -And (-Not $Force)) {
    Write-Verbose "Repo-local Git installation and $tarCommand already exist, skipping Git install."
}
else {
    if (-not $GitVersion) {
        $globalJson = Get-Content "$repoRoot\global.json" | ConvertFrom-Json
        $GitVersion = $globalJson.tools.Git
    }

    $Uri = "https://netcorenativeassets.blob.core.windows.net/resource-packages/external/windows/git/Git-${GitVersion}-64-bit.zip"

    Import-Module -Name (Join-Path $PSScriptRoot "..\common\native\CommonLibrary.psm1" -Resolve)
    $InstallStatus = CommonLibrary\DownloadAndExtract -Uri $Uri -InstallDirectory "$installDir\" -Force:$Force -Verbose

    if ($InstallStatus -Eq $False) {
        Write-Error "Installation failed"
        exit 1
    }
}

New-Item "$repoRoot\.tools\" -ErrorAction SilentlyContinue -ItemType Directory
Copy-Item "$tarCommand" "$finalCommand" -Verbose
Write-Host "Tar now available at '$finalCommand'"

if ($tarCommand -like '*\Git\*') {
    $null >.\.tools\tar.fromGit
}
