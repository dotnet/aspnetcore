#!/usr/bin/env pwsh
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$customHive = "$PSScriptRoot/CustomHive"
New-Item -ErrorAction Ignore -Path $customHive -ItemType Directory

dotnet new --uninstall Microsoft.DotNet.Web.Spa.ProjectTemplates --debug:custom-hive $customHive
dotnet new --uninstall Microsoft.DotNet.Web.Spa.ProjectTemplates.2.2 --debug:custom-hive $customHive
./build.cmd /t:Package
dotnet new --install --debug:custom-hive $customHive "$PSScriptRoot/../artifacts/build/Microsoft.DotNet.Web.Spa.ProjectTemplates.2.2.0-preview1-t000.nupkg"

New-Item -ErrorAction Ignore -Path "$PSScriptRoot/tmp" -ItemType Directory
Push-Location "$PSScriptRoot/tmp"
try {
    dotnet new reactredux
    Push-Location "ClientApp"
    try {
        npm install
    }
    finally {
        Pop-Location
    }
    dotnet run
}
finally {
    Pop-Location
}
