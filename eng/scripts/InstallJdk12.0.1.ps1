

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

if (-not $env:JAVA_HOME) {
    throw 'You must set the JAVA_HOME environment variable to the destination of the JDK.'
}

$repoRoot = Resolve-Path "$PSScriptRoot/../.."
$tempDir = "$repoRoot/obj"
mkdir $tempDir -ea Ignore | out-null
Write-Host "Starting download of JDK 12.0.1"
Invoke-WebRequest -UseBasicParsing -Uri "https://download.java.net/java/GA/jdk12.0.1/69cfe15208a647278a19ef0990eea691/12/GPL/openjdk-12.0.1_windows-x64_bin.zip" -Out "$tempDir/jdk.zip"
Write-Host "Done downloading JDK 12.0.1"
Expand-Archive "$tempDir/jdk.zip" -d "$tempDir/jdk/"
Write-Host "Expanded JDK to $tempDir"
mkdir (split-path -parent $env:JAVA_HOME) -ea ignore | out-null
Write-Host "Installing JDK to $env:JAVA_HOME"
Move-Item "$tempDir/jdk/jdk-12.0.1" $env:JAVA_HOME
Write-Host "Done installing JDK to $env:JAVA_HOME"
