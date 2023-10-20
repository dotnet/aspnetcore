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

function DownloadAnInstall {
    $tempPath = [System.IO.Path]::GetTempPath()
    $tempDir = Join-Path $tempPath nodejs
    New-Item -Path "$tempDir" -ItemType "directory" -Force
    $nodeFile = "node-v$Version-win-x64"
    $url = "https://nodejs.org/dist/v$Version/$nodeFile.zip"
    $integrityUrl = "https://nodejs.org/dist/v$Version/SHASUMS256.txt.asc"
    Write-Host "Starting download of Node.js ${Version} from $url"
    & $PSScriptRoot\Download.ps1 $url nodejs.zip
    Write-Host "Done downloading NodeJS ${Version}"
    Write-Host "Starting download of Node.js integrity file for ${Version} from $integrityUrl"
    & $PSScriptRoot\Download.ps1 $integrityUrl nodejs.integrity.txt
    Write-Host "Done downloading NodeJS ${Version} integrity file"

    $match = Select-String -Path nodejs.integrity.txt -Pattern "$nodeFile.zip";
    if ($null -eq $match) {
        Write-Host "Could not find $nodeFile.zip in integrity file"
        return $false;
    }else {
        Write-Host "Found $nodeFile.zip in integrity file"
    }

    $signature = ($match.Line -split ' ')[0];
    if ($null -eq $signature) {
        Write-Host "Could not find signature for $nodeFile.zip in integrity file"
        return $false;
    }else {
        Write-Host "Found signature $signature for $nodeFile.zip in integrity file";
    }

    $hash = Get-FileHash nodejs.zip -Algorithm SHA256;
    if ($hash.Hash.ToLower() -ne $signature.ToLower()) {
        Write-Host "Hashes do not match, expected $signature, got $hash"
        # Cleanup for next try
        Remove-Item nodejs.zip;
        return $false;
    }else {
        $hashString = $hash.Hash.ToLower();
        Write-Host "Hashes match, expected $signature, got $hashString";
    }

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
    Write-Host "Attempt $current of $maxAttempts to download and install Node.js"
    $succeeded = DownloadAnInstall;
    if ($succeeded -eq $false) {
        Write-Host "Failed to download and install Node.js"
    }else {
        Write-Host "Successfully downloaded and installed Node.js"
    }
    $current++;
} while ($current -lt $maxAttempts -and $succeeded -eq $false);
