#!/usr/bin/env pwsh -c
<#
.PARAMETER BuildXml
    The URL or file path to a build.xml file that defines package versions to be used
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    $BuildXml,
    [string[]]$ConfigVars = @()
)

$ErrorActionPreference = 'Stop'
Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"
Set-StrictMode -Version 1

$depsPath = Resolve-Path "$PSScriptRoot/../build/dependencies.props"
[xml] $dependencies = LoadXml $depsPath

if ($BuildXml -like 'http*') {
    $url = $BuildXml
    New-Item -Type Directory "$PSScriptRoot/../obj/" -ErrorAction Ignore
    $localXml = "$PSScriptRoot/../obj/build.xml"
    Write-Verbose "Downloading from $url to $BuildXml"
    Invoke-WebRequest -OutFile $localXml $url
}

[xml] $remoteDeps = LoadXml $localXml
$count = 0

$variables = @{}

foreach ($package in $remoteDeps.SelectNodes('//Package')) {
    $packageId = $package.Id
    $packageVersion = $package.Version
    $varName = PackageIdVarName $packageId
    Write-Verbose "Found {id: $packageId, version: $packageVersion, varName: $varName }"

    if ($variables[$varName]) {
        if ($variables[$varName].Where( {$_ -eq $packageVersion}, 'First').Count -eq 0) {
            $variables[$varName] += $packageVersion
        }
    }
    else {
        $variables[$varName] = @($packageVersion)
    }
}

$updatedVars = @{}

foreach ($varName in ($variables.Keys | sort)) {
    $packageVersions = $variables[$varName]
    if ($packageVersions.Length -gt 1) {
        Write-Warning "Skipped $varName. Multiple version found. { $($packageVersions -join ', ') }."
        continue
    }

    $packageVersion = $packageVersions | Select-Object -First 1

    $depVarNode = $dependencies.SelectSingleNode("//PropertyGroup[`@Label=`"Package Versions: Auto`"]/$varName")
    if ($depVarNode -and $depVarNode.InnerText -ne $packageVersion) {
        $depVarNode.InnerText = $packageVersion
        $count++
        Write-Host -f DarkGray "   Updating $varName to $packageVersion"
        $updatedVars[$varName] = $packageVersion
    }
}

if ($count -gt 0) {
    Write-Host -f Cyan "Updating $count version variables in $depsPath"
    SaveXml $dependencies $depsPath

    # Ensure dotnet is installed
    & "$PSScriptRoot\..\run.ps1" install-tools

    $ProjectPath = "$PSScriptRoot\update-dependencies\update-dependencies.csproj"

    $ConfigVars += "--BuildXml"
    $ConfigVars += $BuildXml

    $ConfigVars += "--UpdatedVersions"
    $varString = ""
    foreach ($updatedVar in $updatedVars.GetEnumerator()) {
        $varString += "$($updatedVar.Name)=$($updatedVar.Value)+"
    }
    $ConfigVars += $varString

    # Restore and run the app
    Write-Host "Invoking App $ProjectPath..."
    Invoke-Expression "dotnet run -p `"$ProjectPath`" $ConfigVars"
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}
else {
    Write-Host -f Green "No changes found"
}
