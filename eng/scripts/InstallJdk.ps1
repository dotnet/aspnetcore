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
    [switch]$Force
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$installDir = "$repoRoot\.tools\jdk\win-x64\"
$javacExe = "$installDir\bin\javac.exe"
$tempDir = "$repoRoot\obj"
if (-not $JdkVersion) {
    $globalJson = Get-Content "$repoRoot\global.json" | ConvertFrom-Json
    $JdkVersion = $globalJson.'native-tools'.jdk
}

if (Test-Path $javacExe) {
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
& $PSScriptRoot\Download.ps1 "https://netcorenativeassets.blob.core.windows.net/resource-packages/external/windows/java/microsoft-jdk-${JdkVersion}-windows-x64.zip" "$tempDir/jdk.zip"
Write-Host "Done downloading JDK ${JdkVersion}"
Expand-Archive "$tempDir/jdk.zip" -d "$tempDir/jdk/"
Write-Host "Expanded JDK to $tempDir"
Write-Host "Installing JDK to $installDir"
# The name of the file directory within the zip is based on the version, but may contain a +N for build number.
Move-Item "$(Get-ChildItem -Path "$tempDir/jdk" | Select-Object -First 1)/*" $installDir
Write-Host "Done installing JDK to $installDir"

if ($env:TF_BUILD) {
    Write-Host "##vso[task.prependpath]$installDir\bin"
}
