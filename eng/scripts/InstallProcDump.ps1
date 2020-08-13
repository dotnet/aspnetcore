<#
.SYNOPSIS
    Installs ProcDump into a folder in this repo.
.DESCRIPTION
    This script downloads and extracts the ProcDump.
.PARAMETER Force
    Overwrite the existing installation
#>
param(
    [switch]$Force
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$installDir = "$repoRoot\.tools\ProcDump\"
$tempDir = "$repoRoot\obj"

if (Test-Path $installDir) {
    if ($Force) {
        Remove-Item -Force -Recurse $installDir
    }
    else {
        Write-Host "ProcDump already installed to $installDir. Exiting without action. Call this script again with -Force to overwrite."
        exit 0
    }
}

Remove-Item -Force -Recurse $tempDir -ErrorAction Ignore | out-null
mkdir $tempDir -ea Ignore | out-null
mkdir $installDir -ea Ignore | out-null
Write-Host "Starting ProcDump download"
Invoke-WebRequest -UseBasicParsing -Uri "https://download.sysinternals.com/files/Procdump.zip" -Out "$tempDir/ProcDump.zip"
Write-Host "Done downloading ProcDump"
Expand-Archive "$tempDir/ProcDump.zip" -d "$tempDir/ProcDump/"
Write-Host "Expanded ProcDump to $tempDir"
Write-Host "Installing ProcDump to $installDir"
Move-Item "$tempDir/ProcDump/*" $installDir
Write-Host "Done installing ProcDump to $installDir"

if ($env:TF_BUILD) {
    Write-Host "##vso[task.setvariable variable=ProcDumpPath]$installDir"
    Write-Host "##vso[task.prependpath]$installDir"
}
