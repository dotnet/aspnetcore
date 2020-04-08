 <# 
 .SYNOPSIS 
     Unzips an AspNetCore.App.Ref nupkg
 .DESCRIPTION 
     This script unzips an AspNetCore.App.Ref nupkg
 .PARAMETER RefPath
     The path to the AspNetCore.App.Ref package to install.
 .PARAMETER InstallDir
     The directory to install to.     
 #> 
param(
    [Parameter(Mandatory = $true)]
    $RefPath,
    
    [Parameter(Mandatory = $true)]
    $InstallDir
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

Write-Host "Extracting to $InstallDir"

$zipPackage = [io.path]::ChangeExtension($RefPath, ".zip")
Write-Host "Renaming to $zipPackage"
Rename-Item -Path $RefPath -NewName $zipPackage
if (Get-Command -Name 'Microsoft.PowerShell.Archive\Expand-Archive' -ErrorAction Ignore) {
    # Use built-in commands where possible as they are cross-plat compatible
    Microsoft.PowerShell.Archive\Expand-Archive -Path $zipPackage -DestinationPath "$InstallDir" -Force
}
else {
    Remove-Item "$InstallDir" -Recurse -ErrorAction Ignore
    # Fallback to old approach for old installations of PowerShell
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPackage, "$InstallDir")
}

Get-ChildItem -Path "$InstallDir" -Recurse
