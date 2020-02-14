param (
    $darcVersion = $null,
    $versionEndpoint = "https://maestro-prod.westus2.cloudapp.azure.com/api/assets/darc-version?api-version=2019-01-16",
    $verbosity = "m"
)

. $PSScriptRoot\tools.ps1

function InstallDarcCli ($darcVersion) {
  $darcCliPackageName = "microsoft.dotnet.darc"

  $dotnetRoot = InitializeDotNetCli -install:$true
  $dotnet = "$dotnetRoot\dotnet.exe"
  $toolList = & "$dotnet" tool list -g

  if ($toolList -like "*$darcCliPackageName*") {
    & "$dotnet" tool uninstall $darcCliPackageName -g
  }

  # If the user didn't explicitly specify the darc version,
  # query the Maestro API for the correct version of darc to install.
  if (-not $darcVersion) {
    $darcVersion = $(Invoke-WebRequest -Uri $versionEndpoint -UseBasicParsing).Content
  }

  $arcadeServicesSource = 'https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json'

  Write-Host "Installing Darc CLI version $darcVersion..."
  Write-Host "You may need to restart your command window if this is the first dotnet tool you have installed."
  & "$dotnet" tool install $darcCliPackageName --version $darcVersion --add-source "$arcadeServicesSource" -v $verbosity -g --framework netcoreapp2.1
}

InstallDarcCli $darcVersion
