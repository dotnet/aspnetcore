
param(
    [Parameter(Mandatory = $true)]
    $JdkVersion
    )

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

if (-not $env:JAVA_HOME) {
    throw 'You must set the JAVA_HOME environment variable to the destination of the JDK.'
}

$repoRoot = Resolve-Path "$PSScriptRoot/../.."
$tempDir = "$repoRoot/obj"
mkdir $tempDir -ea Ignore | out-null
Invoke-WebRequest -UseBasicParsing -Uri "https://netcorenativeassets.blob.core.windows.net/resource-packages/external/windows/java/jdk-${JdkVersion}_windows-x64_bin.zip" -Out "$tempDir/jdk.zip"
Expand-Archive "$tempDir/jdk.zip" -d "$tempDir/jdk/"
mkdir (split-path -parent $env:JAVA_HOME) -ea ignore | out-null
Move-Item "$tempDir/jdk/jdk-${jdkVersion}" $env:JAVA_HOME
