param (
  $sourcelinkCliVersion = $null
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

. $PSScriptRoot\..\tools.ps1

$verbosity = "minimal"

function InstallSourcelinkCli ($sourcelinkCliVersion) {
  $sourcelinkCliPackageName = "sourcelink"

  $dotnetRoot = InitializeDotNetCli -install:$true
  $dotnet = "$dotnetRoot\dotnet.exe"
  $toolList = & "$dotnet" tool list --global

  if (($toolList -like "*$sourcelinkCliPackageName*") -and ($toolList -like "*$sourcelinkCliVersion*")) {
    Write-Host "SourceLink CLI version $sourcelinkCliVersion is already installed."
  }
  else {
    Write-Host "Installing SourceLink CLI version $sourcelinkCliVersion..."
    Write-Host "You may need to restart your command window if this is the first dotnet tool you have installed."
    & "$dotnet" tool install $sourcelinkCliPackageName --version $sourcelinkCliVersion --verbosity $verbosity --global 
  }
}

InstallSourcelinkCli $sourcelinkCliVersion
