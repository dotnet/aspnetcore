param (
    $darcVersion = $null
)

$verbosity = "m"
. $PSScriptRoot\tools.ps1

function InstallDarcCli ($darcVersion) {
  $darcCliPackageName = "microsoft.dotnet.darc"

  $dotnetRoot = InitializeDotNetCli -install:$true
  $dotnet = "$dotnetRoot\dotnet.exe"
  $toolList = Invoke-Expression "& `"$dotnet`" tool list -g"

  if ($toolList -like "*$darcCliPackageName*") {
    Invoke-Expression "& `"$dotnet`" tool uninstall $darcCliPackageName -g"
  }

  # Until we can anonymously query the BAR API for the latest arcade-services
  # build applied to the PROD channel, this is hardcoded.
  if (-not $darcVersion) {
    $darcVersion = '1.1.0-beta.19205.4'
  }
  
  $arcadeServicesSource = 'https://dotnetfeed.blob.core.windows.net/dotnet-arcade/index.json'

  Write-Host "Installing Darc CLI version $darcVersion..."
  Write-Host "You may need to restart your command window if this is the first dotnet tool you have installed."
  Invoke-Expression "& `"$dotnet`" tool install $darcCliPackageName --version $darcVersion --add-source '$arcadeServicesSource' -v $verbosity -g"
}

InstallDarcCli $darcVersion
