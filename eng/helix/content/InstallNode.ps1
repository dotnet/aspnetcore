 <# 
 .SYNOPSIS 
     Installs NodeJs from http://nodejs.org/dist on a machine
 .DESCRIPTION 
     This script installs NodeJs from http://nodejs.org/dist on a machine. 
 .PARAMETER Version
     The version of NodeJS to install.
 .LINK 
     https://nodejs.org/en/
 #> 
param(
    [Parameter(Mandatory = $true)]
    $Version
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138
$InstallDir = '.' # Always install to workitem root

Set-StrictMode -Version 1

if (Get-Command "node.exe" -ErrorAction SilentlyContinue)
{
    Write-Host "Found node.exe in PATH"
    exit
}

if (Test-Path "$InstallDir\node.exe")
{
    Write-Host "Node.exe found at $InstallDir"
    exit
}

$nodeFile="node-v$Version-win-x64"
$url="http://nodejs.org/dist/v$Version/$nodeFile.zip"
Write-Host "Starting download of NodeJs ${Version} from $url"
& $PSScriptRoot\Download.ps1 $url nodejs.zip
Write-Host "Done downloading NodeJS ${Version}"

$tempPath = [System.IO.Path]::GetTempPath()
$tempDir = Join-Path $tempPath nodejs
New-Item -Path "$tempDir" -ItemType "directory" -Force
Write-Host "Extracting to $tempDir"

if (Get-Command -Name 'Microsoft.PowerShell.Archive\Expand-Archive' -ErrorAction Ignore) {
    # Use built-in commands where possible as they are cross-plat compatible
    Microsoft.PowerShell.Archive\Expand-Archive -Path "nodejs.zip" -DestinationPath $tempDir -Force
}
else {
    Remove-Item $tempDir -Recurse -ErrorAction Ignore
    # Fallback to old approach for old installations of PowerShell
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory("nodejs.zip", $tempDir)
}

Write-Host "Expanded NodeJs"
New-Item -Path "$InstallDir" -ItemType "directory" -Force
Write-Host "Copying $tempDir\$nodeFile\* to $InstallDir"
Copy-Item "$tempDir\$nodeFile\*" "$InstallDir" -Recurse
if (Test-Path "$InstallDir\node.exe")
{
    Write-Host "Node.exe copied to $InstallDir"
}
else
{
    Write-Host "Node.exe not copied to $InstallDir"
}
if (Test-Path "package-lock.json")
{
    $Env:Path += ";" + $Env:HELIX_CORRELATION_PAYLOAD + "\jdk\bin"
    Write-Host "Found package-lock.json, running $InstallDir\npm install"
    Invoke-Expression "$InstallDir\npm.cmd install"
}
