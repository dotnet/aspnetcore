param (
  $dotnetsymbolVersion = $null
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

. $PSScriptRoot\..\tools.ps1

$verbosity = "minimal"

function Installdotnetsymbol ($dotnetsymbolVersion) {
  $dotnetsymbolPackageName = "dotnet-symbol"

  $dotnetRoot = InitializeDotNetCli -install:$true
  $dotnet = "$dotnetRoot\dotnet.exe"
  $toolList = & "$dotnet" tool list --global

  if (($toolList -like "*$dotnetsymbolPackageName*") -and ($toolList -like "*$dotnetsymbolVersion*")) {
    Write-Host "dotnet-symbol version $dotnetsymbolVersion is already installed."
  }
  else {
    Write-Host "Installing dotnet-symbol version $dotnetsymbolVersion..."
    Write-Host "You may need to restart your command window if this is the first dotnet tool you have installed."
    & "$dotnet" tool install $dotnetsymbolPackageName --version $dotnetsymbolVersion --verbosity $verbosity --global
  }
}

Installdotnetsymbol $dotnetsymbolVersion
