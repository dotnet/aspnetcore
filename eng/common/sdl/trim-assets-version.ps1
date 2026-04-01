<#
.SYNOPSIS
Install and run the 'Microsoft.DotNet.VersionTools.Cli' tool with the 'trim-artifacts-version' command to trim the version from the NuGet assets file name.

.PARAMETER InputPath
Full path to directory where artifact packages are stored

.PARAMETER Recursive
Search for NuGet packages recursively

#>

Param(
  [string] $InputPath,
  [bool] $Recursive = $true
)

$CliToolName = "Microsoft.DotNet.VersionTools.Cli"

function Install-VersionTools-Cli {
  param(
      [Parameter(Mandatory=$true)][string]$Version
  )

  Write-Host "Installing the package '$CliToolName' with a version of '$version' ..."
  $feed = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json"

  $argumentList = @("tool", "install", "--local", "$CliToolName", "--add-source $feed", "--no-cache", "--version $Version", "--create-manifest-if-needed")
  Start-Process "$dotnet" -Verbose -ArgumentList $argumentList -NoNewWindow -Wait
}

# -------------------------------------------------------------------

if (!(Test-Path $InputPath)) {
  Write-Host "Input Path '$InputPath' does not exist"
  ExitWithExitCode 1
}

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$disableConfigureToolsetImport = $true
$global:LASTEXITCODE = 0

# `tools.ps1` checks $ci to perform some actions. Since the SDL
# scripts don't necessarily execute in the same agent that run the
# build.ps1/sh script this variable isn't automatically set.
$ci = $true
. $PSScriptRoot\..\tools.ps1

try {
  $dotnetRoot = InitializeDotNetCli -install:$true
  $dotnet = "$dotnetRoot\dotnet.exe"

  $toolsetVersion = Read-ArcadeSdkVersion
  Install-VersionTools-Cli -Version $toolsetVersion

  $cliToolFound = (& "$dotnet" tool list --local | Where-Object {$_.Split(' ')[0] -eq $CliToolName})
  if ($null -eq $cliToolFound) {
    Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "The '$CliToolName' tool is not installed."
    ExitWithExitCode 1
  }

  Exec-BlockVerbosely {
    & "$dotnet" $CliToolName trim-assets-version `
      --assets-path $InputPath `
      --recursive $Recursive
    Exit-IfNZEC "Sdl"
  }
}
catch {
  Write-Host $_
  Write-PipelineTelemetryError -Force -Category 'Sdl' -Message $_
  ExitWithExitCode 1
}
