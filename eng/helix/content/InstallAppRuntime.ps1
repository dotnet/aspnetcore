 <# 
 .SYNOPSIS 
     Installs an AspNetCore shared framework on a machine
 .DESCRIPTION 
     This script installs an AspNetCore shared framework on a machine
 .PARAMETER AppRuntimePath
     The path to the app runtime package to install.
 .PARAMETER InstallDir
     The directory to install the shared framework to.     
 .PARAMETER Framework
     The framework directory to copy the shared framework from.
 .PARAMETER RuntimeIdentifier
     The runtime identifier for the shared framework.
 #> 
param(
    [Parameter(Mandatory = $true)]
    $AppRuntimePath,
    
    [Parameter(Mandatory = $true)]
    $InstallDir,

    [Parameter(Mandatory = $true)]
    $Framework,
    
    [Parameter(Mandatory = $true)]
    $RuntimeIdentifier)

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

Get-ChildItem -Path ".\tmpRuntime" -Recurse

Write-Host "Copying managed files to $InstallDir"
Copy-Item -Path ".\tmpRuntime\runtimes\$RuntimeIdentifier\lib\$Framework\*" $InstallDir
Write-Host "Copying native files to $InstallDir"
Copy-Item -Path ".\tmpRuntime\runtimes\$RuntimeIdentifier\native\*" $InstallDir
