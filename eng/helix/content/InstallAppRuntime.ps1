 <# 
 .SYNOPSIS 
     Installs an AspNetCore shared framework on a machine
 .DESCRIPTION 
     This script installs an AspNetCore shared framework on a machine
 .PARAMETER AppRuntimePath
     The path to the app runtime package to install.
 .PARAMETER InstallDir
     The directory to install the shared framework to.     
 #> 
param(
    [Parameter(Mandatory = $true)]
    $AppRuntimePath,
    
    [Parameter(Mandatory = $true)]
    $InstallDir)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

Write-Host "Extracting to $InstallDir"

$zipPackage = [io.path]::ChangeExtension($AppRuntimePath, ".zip")
Write-Host "Renaming to $zipPackage"
Rename-Item -Path $AppRuntimePath -NewName $zipPackage
if (Get-Command -Name 'Microsoft.PowerShell.Archive\Expand-Archive' -ErrorAction Ignore) {
    # Use built-in commands where possible as they are cross-plat compatible
    Microsoft.PowerShell.Archive\Expand-Archive -Path $zipPackage -DestinationPath ".\tmpRuntime" -Force
}
else {
    Remove-Item ".\tmpRuntime" -Recurse -ErrorAction Ignore
    # Fallback to old approach for old installations of PowerShell
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPackage, ".\tmpRuntime")
}

Write-Host "Expanded App Runtime to tmpRuntime"
Get-ChildItem -Path ".\tmpRuntime"

Write-Host "Copying managed files to $InstallDir"
Copy-Item -Path ".\tmpRuntime\win-x64\lib\netcoreapp5.0\*" $InstallDir
Write-Host "Copying native files to $InstallDir"
Copy-Item -Path ".\tmpRuntime\win-x64\native\*" $InstallDir
Get-ChildItem -Path $InstallDir
