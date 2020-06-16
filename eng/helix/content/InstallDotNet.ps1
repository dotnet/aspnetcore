 <# 
 .SYNOPSIS 
     Installs dotnet sdk and runtime using https://dot.net/v1/dotnet-install.ps1
 .DESCRIPTION
     Installs dotnet sdk and runtime using https://dot.net/v1/dotnet-install.ps1
.PARAMETER arch
    The architecture to install.
.PARAMETER sdkVersion
    The sdk version to install
.PARAMETER runtimeVersion
    The runtime version to install
.PARAMETER installDir
    The directory to install to
#>
param(
    [Parameter(Mandatory = $true)]
    $arch,

    [Parameter(Mandatory = $true)]
    $sdkVersion,
    
    [Parameter(Mandatory = $true)]
    $runtimeVersion,
    
    [Parameter(Mandatory = $true)]
    $installDir    
)

& $PSScriptRoot\Download.ps1 "https://dot.net/v1/dotnet-install.ps1" dotnet-install.ps1
Write-Host "Download of dotnet-install.ps1 complete..."
Write-Host "Installing SDK...& dotnet-install.ps1 -Architecture $arch -Version $sdkVersion -InstallDir $installDir"
Invoke-Expression "& dotnet-install.ps1 -Architecture $arch -Version $sdkVersion -InstallDir $installDir"
Write-Host "Installing Runtime...& dotnet-install.ps1 -Architecture $arch -Runtime dotnet -Version $runtimeVersion -InstallDir $installDir"
Invoke-Expression "& dotnet-install.ps1 -Architecture $arch -Runtime dotnet -Version $runtimeVersion -InstallDir $installDir"
Write-Host "GetDotNetInstall complete..."
 
