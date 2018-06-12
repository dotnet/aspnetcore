#!/usr/bin/env pwsh -c

<#
.SYNOPSIS
    Updates the version.props file in repos to a newer patch version
.PARAMETER Repos
    A list of the repositories that should be patched
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Repos,
    [switch]$NoCommit
)

$ErrorActionPreference = 'Stop'

Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"

function BumpPatch([System.Xml.XmlNode]$node) {
    if (-not $node) {
        return
    }
    [version] $version = $node.InnerText
    $node.InnerText = "{0}.{1}.{2}" -f $version.Major, $version.Minor, ($version.Build + 1)
    return "Bumping version from $version to $($node.InnerText)"
}

foreach ($repo in $Repos) {
    $repoPath = "$PSScriptRoot/../modules/$repo"
    Push-Location $repoPath
    try
    {
        $path = "$repoPath/version.props"
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
        BumpPatch $epxVersionPrefix | write-host
        BumpPatch $exVersionPrefix | write-host
        $message = BumpPatch $versionPrefix
        Write-Host $message
        SaveXml $xml $path
        if (-not $NoCommit) {
            Invoke-Block { & git add $path }
            Invoke-Block { & git commit -m $message }
        }
    }
    finally
    {
        Pop-Location
    }
}

