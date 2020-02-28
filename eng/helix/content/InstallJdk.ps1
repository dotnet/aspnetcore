<#
.SYNOPSIS
    Installs JDK into a folder in this repo.
.DESCRIPTION
    This script downloads an extracts the JDK.
.PARAMETER JdkVersion
    The version of the JDK to install. If not set, the default value is read from global.json
.PARAMETER Force
    Overwrite the existing installation
#>
param(
    [string]$JdkVersion,
    [Parameter(Mandatory = $false)]
    $InstallDir
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

if ($InstallDir) {
    $installDir = $InstallDir;
}
else {
    $repoRoot = Resolve-Path "$PSScriptRoot\..\.."
    $installDir = "$repoRoot\.tools\jdk\win-x64\"
}
$tempDir = "$installDir\obj"
if (-not $JdkVersion) {
    $globalJson = Get-Content "$repoRoot\global.json" | ConvertFrom-Json
    $JdkVersion = $globalJson.tools.jdk
}

if (Test-Path $installDir) {
    if ($Force) {
        Remove-Item -Force -Recurse $installDir
    }
    else {
        Write-Host "The JDK already installed to $installDir. Exiting without action. Call this script again with -Force to overwrite."
        exit 0
    }
}

Remove-Item -Force -Recurse $tempDir -ErrorAction Ignore | out-null
mkdir $tempDir -ea Ignore | out-null
mkdir $installDir -ea Ignore | out-null
Write-Host "Starting download of JDK ${JdkVersion}"
Invoke-WebRequest -UseBasicParsing -Uri "https://netcorenativeassets.blob.core.windows.net/resource-packages/external/windows/java/jdk-${JdkVersion}_windows-x64_bin.zip" -OutFile "$tempDir/jdk.zip"
Write-Host "Done downloading JDK ${JdkVersion}"

Add-Type -assembly "System.IO.Compression.FileSystem"
[System.IO.Compression.ZipFile]::ExtractToDirectory("$tempDir/jdk.zip", "$tempDir/jdk/")

Write-Host "Expanded JDK to $tempDir"
Write-Host "Installing JDK to $installDir"
Move-Item "$tempDir/jdk/jdk-${JdkVersion}/*" $installDir
Write-Host "Done installing JDK to $installDir"
Remove-Item -Force -Recurse $tempDir -ErrorAction Ignore | out-null

if ($env:TF_BUILD) {
    Write-Host "##vso[task.prependpath]$installDir\bin"
}
