# Lifted from https://github.com/dotnet/arcade/blob/main/eng/common/tools.ps1

[CmdletBinding(DefaultParameterSetName='Groups')]
param(
    [string]$Version = '17.4.1'
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

function Create-Directory ([string[]] $path) {
  New-Item -Path $path -Force -ItemType 'Directory' | Out-Null
}

function Unzip([string]$zipfile, [string]$outpath) {
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

function InitializeXCopyMSBuild([string]$packageVersion, [bool]$install, [string]$ToolsDir) {
  $packageName = 'RoslynTools.MSBuild'
  $packageDir = Join-Path $ToolsDir $packageVersion
  $packagePath = Join-Path $packageDir "$packageName.$packageVersion.nupkg"

  if (!(Test-Path $packageDir)) {
    if (!$install) {
      return $null
    }

    Create-Directory $packageDir

    Write-Host "Downloading $packageName $packageVersion"
    $ProgressPreference = 'SilentlyContinue' # Don't display the console progress UI - it's a huge perf hit
    Invoke-WebRequest "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/flat2/$packageName/$packageVersion/$packageName.$packageVersion.nupkg" -OutFile $packagePath

    Unzip $packagePath $packageDir
  }

  return Join-Path $packageDir 'tools'
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\')
InitializeXCopyMSBuild -packageVersion $Version -install $true -ToolsDir (join-path $RepoRoot .msbuild)
