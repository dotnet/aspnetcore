<#
 .SYNOPSIS
     Installs Node.js from https://nodejs.org/dist on a machine
 .DESCRIPTION
     This script installs Node.js from https://nodejs.org/dist on a machine.
 .PARAMETER Version
     The version of Node.js to install.
 .LINK
     https://nodejs.org/en/
 #>
param(
    [Parameter(Mandatory = $true)]
    $Version
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138
$InstallDir = $PSScriptRoot + '\nodejs' # Always install to workitem root / nodejs

Set-StrictMode -Version 1

if (Get-Command "node.exe" -ErrorAction SilentlyContinue) {
    Write-Host "Found node.exe in PATH"
    exit
}

if (Test-Path "$InstallDir\node.exe") {
    Write-Host "Node.exe found at $InstallDir\node.exe"
    exit
}

$maxAttempts = 5;
$current = 0;
do {
    $failed = DownloadAnInstall
    $current++;
} while ($current -lt $maxAttempts -and $failed -eq $true);

function DownloadAnInstall () {
    $tempPath = [System.IO.Path]::GetTempPath()
    $tempDir = Join-Path $tempPath nodejs
    New-Item -Path "$tempDir" -ItemType "directory" -Force
    try {
        $nodeFile = "node-v$Version-win-x64"
        $url = "https://nodejs.org/dist/v$Version/$nodeFile.zip"
        Write-Host "Starting download of Node.js ${Version} from $url"
        & $PSScriptRoot\Download.ps1 $url nodejs.zip
        Write-Host "Done downloading NodeJS ${Version}"

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

        Write-Host "Expanded Node.js to $tempDir, moving $tempDir\$nodeFile to $InstallDir subdir"
        Move-Item $tempDir\$nodeFile $InstallDir
        if (Test-Path "$InstallDir\node.exe") {
            Write-Host "Node.exe copied to $InstallDir"
        }
        else {
            Write-Host "Node.exe not copied to $InstallDir"
        }
        return $true;
    }
    catch {
        Remove-Item nodejs.zip -Force
        return $false;
    }
}
