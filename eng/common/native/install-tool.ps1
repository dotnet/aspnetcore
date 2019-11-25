<#
.SYNOPSIS
Install native tool

.DESCRIPTION
Install cmake native tool from Azure blob storage

.PARAMETER InstallPath
Base directory to install native tool to

.PARAMETER BaseUri
Base file directory or Url from which to acquire tool archives

.PARAMETER CommonLibraryDirectory
Path to folder containing common library modules

.PARAMETER Force
Force install of tools even if they previously exist

.PARAMETER Clean
Don't install the tool, just clean up the current install of the tool

.PARAMETER DownloadRetries
Total number of retry attempts

.PARAMETER RetryWaitTimeInSeconds
Wait time between retry attempts in seconds

.NOTES
Returns 0 if install succeeds, 1 otherwise
#>
[CmdletBinding(PositionalBinding=$false)]
Param (
  [Parameter(Mandatory=$True)]
  [string] $ToolName,
  [Parameter(Mandatory=$True)]
  [string] $InstallPath,
  [Parameter(Mandatory=$True)]
  [string] $BaseUri,
  [Parameter(Mandatory=$True)]
  [string] $Version,
  [string] $CommonLibraryDirectory = $PSScriptRoot,
  [switch] $Force = $False,
  [switch] $Clean = $False,
  [int] $DownloadRetries = 5,
  [int] $RetryWaitTimeInSeconds = 30
)

. $PSScriptRoot\..\pipeline-logging-functions.ps1

# Import common library modules
Import-Module -Name (Join-Path $CommonLibraryDirectory "CommonLibrary.psm1")

try {
  # Define verbose switch if undefined
  $Verbose = $VerbosePreference -Eq "Continue"

  $Arch = CommonLibrary\Get-MachineArchitecture
  $ToolOs = "win64"
  if($Arch -Eq "x32") {
    $ToolOs = "win32"
  }
  $ToolNameMoniker = "$ToolName-$Version-$ToolOs-$Arch"
  $ToolInstallDirectory = Join-Path $InstallPath "$ToolName\$Version\"
  $Uri = "$BaseUri/windows/$ToolName/$ToolNameMoniker.zip"
  $ShimPath = Join-Path $InstallPath "$ToolName.exe"

  if ($Clean) {
    Write-Host "Cleaning $ToolInstallDirectory"
    if (Test-Path $ToolInstallDirectory) {
      Remove-Item $ToolInstallDirectory -Force -Recurse
    }
    Write-Host "Cleaning $ShimPath"
    if (Test-Path $ShimPath) {
      Remove-Item $ShimPath -Force
    }
    $ToolTempPath = CommonLibrary\Get-TempPathFilename -Path $Uri
    Write-Host "Cleaning $ToolTempPath"
    if (Test-Path $ToolTempPath) {
      Remove-Item $ToolTempPath -Force
    }
    exit 0
  }

  # Install tool
  if ((Test-Path $ToolInstallDirectory) -And (-Not $Force)) {
    Write-Verbose "$ToolName ($Version) already exists, skipping install"
  }
  else {
    $InstallStatus = CommonLibrary\DownloadAndExtract -Uri $Uri `
                                                      -InstallDirectory $ToolInstallDirectory `
                                                      -Force:$Force `
                                                      -DownloadRetries $DownloadRetries `
                                                      -RetryWaitTimeInSeconds $RetryWaitTimeInSeconds `
                                                      -Verbose:$Verbose

    if ($InstallStatus -Eq $False) {
      Write-PipelineTelemetryError "Installation failed" -Category "NativeToolsetBootstrapping"
      exit 1
    }
  }

  $ToolFilePath = Get-ChildItem $ToolInstallDirectory -Recurse -Filter "$ToolName.exe" | % { $_.FullName }
  if (@($ToolFilePath).Length -Gt 1) {
    Write-Error "There are multiple copies of $ToolName in $($ToolInstallDirectory): `n$(@($ToolFilePath | out-string))"
    exit 1
  } elseif (@($ToolFilePath).Length -Lt 1) {
    Write-Error "$ToolName was not found in $ToolFilePath."
    exit 1
  }

  # Generate shim
  # Always rewrite shims so that we are referencing the expected version
  $GenerateShimStatus = CommonLibrary\New-ScriptShim -ShimName $ToolName `
                                                     -ShimDirectory $InstallPath `
                                                     -ToolFilePath "$ToolFilePath" `
                                                     -BaseUri $BaseUri `
                                                     -Force:$Force `
                                                     -Verbose:$Verbose

  if ($GenerateShimStatus -Eq $False) {
    Write-PipelineTelemetryError "Generate shim failed" -Category "NativeToolsetBootstrapping"
    return 1
  }

  exit 0
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category "NativeToolsetBootstrapping" -Message $_
  exit 1
}
