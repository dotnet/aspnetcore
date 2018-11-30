$verbosity = "m"
. $PSScriptRoot\tools.ps1

function InstallDarcCli {
  $darcCliPackageName = "microsoft.dotnet.darc"
  $dotnet = "$env:DOTNET_INSTALL_DIR\dotnet.exe"
  $toolList = Invoke-Expression "& `"$dotnet`" tool list -g"

  if ($toolList -like "*$darcCliPackageName*") {
    Invoke-Expression "& `"$dotnet`" tool uninstall $darcCliPackageName -g"
  }

  $toolsetVersion = $GlobalJson.'msbuild-sdks'.'Microsoft.DotNet.Arcade.Sdk'

  Write-Host "Installing Darc CLI version $toolsetVersion..."
  Write-Host "You may need to restart your command window if this is the first dotnet tool you have installed."
  Invoke-Expression "& `"$dotnet`" tool install $darcCliPackageName --version $toolsetVersion -v $verbosity -g"
}

InitializeTools
InstallDarcCli
