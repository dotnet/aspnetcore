#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Updates the version.props file in repos to a newer patch version
.PARAMETER Repos
    A list of the repositories that should be patched
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Repos
)

$ErrorActionPreference = 'Stop'

Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"

function BumpPatch([System.Xml.XmlNode]$node) {
    if (-not $node) {
        return
    }
    [version] $version = $node.InnerText
    $node.InnerText = "{0}.{1}.{2}" -f $version.Major, $version.Minor, ($version.Build + 1)
    Write-Host "Changing $version to $($node.InnerText)"
}

foreach ($repo in $Repos) {
    $path = "$PSScriptRoot/../modules/$repo/version.props"
    Write-Host -ForegroundColor Magenta "Updating $repo"
    if (-not (Test-Path $path)) {
        Write-Warning "$path does not exist"
        continue
    }
    $path = Resolve-Path $path
    Write-Verbose "$path"
    [xml] $xml = LoadXml $path

    $suffix = $xml.SelectSingleNode('/Project/PropertyGroup/VersionSuffix')
    if (-not $suffix) {
        write-error "$path does not have VersionSuffix"
    }

    $versionPrefix = $xml.SelectSingleNode('/Project/PropertyGroup/VersionPrefix')
    $epxVersionPrefix = $xml.SelectSingleNode('/Project/PropertyGroup/ExperimentalProjectVersionPrefix')
    $exVersionPrefix = $xml.SelectSingleNode('/Project/PropertyGroup/ExperimentalVersionPrefix')
    BumpPatch $epxVersionPrefix
    BumpPatch $exVersionPrefix
    BumpPatch $versionPrefix
    SaveXml $xml $path
}

