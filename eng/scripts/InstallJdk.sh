#!/usr/bin/env bash

__error() {
    echo -e "${RED}error: $*${RESET}" 1>&2
}

__warn() {
    echo -e "${YELLOW}warning: $*${RESET}"
}

__script_dir() {
    echo "$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
}

if [ $# != 1 ]; then
    __error "Your command line contains $# arguments, needs only the JDK version"
    exit 1
fi

if [ -z $JAVA_HOME ]; then
    __error 'You must set the JAVA_HOME environment variable to the destination of the JDK.'
    exit 1
fi

jdkVersion=$0

scriptDir="$( __script_dir )"
repoRoot=$( realpath "$scriptDir/../.." )
tempDir="$repoRoot/obj"
mkdir $tempDir
echo "Starting download of JDK $jdkVersion"
Invoke-WebRequest -UseBasicParsing -Uri "https://netcorenativeassets.blob.core.windows.net/resource-packages/external/windows/java/jdk-${JdkVersion}_windows-x64_bin.zip" -Out "$tempDir/jdk.zip"
Write-Host "Done downloading JDK ${JdkVersion}"
Expand-Archive "$tempDir/jdk.zip" -d "$tempDir/jdk/"
Write-Host "Expanded JDK to $tempDir"
mkdir (split-path -parent $env:JAVA_HOME) -ea ignore | out-null
Write-Host "Installing JDK to $env:JAVA_HOME"
Move-Item "$tempDir/jdk/jdk-${jdkVersion}" $env:JAVA_HOME
Write-Host "Done installing JDK to $env:JAVA_HOME"
