#!/usr/bin/env pwsh -c

<#
.SYNOPSIS
    Updates the version.props file in repos to a newer patch version
.PARAMETER Repos
    A list of the repositories that should be patched
.PARAMETER Mode
    Version bump options: Major, Minor, Patch
.PARAMETER VersionSuffix
    The version suffix to use
#>
[cmdletbinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Repos,
    [Parameter(Mandatory = $true)]
    [ValidateSet('Major', 'Minor', 'Patch')]
    [string]$Mode,
    [string]$VersionSuffix = $null,
    [switch]$NoCommit
)

$ErrorActionPreference = 'Stop'

Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"

function SetVersionSuffix([System.Xml.XmlNode]$node) {
    if (-not $node) {
        return
    }
    $node.InnerText = $VersionSuffix
    return "Setting $($node.Name) to $VersionSuffix"
}

function BumpVersion([System.Xml.XmlNode]$node) {
    if (-not $node) {
        return
    }
    [version] $version = $node.InnerText

    $experimental = $version.Major -eq 0

    switch ($mode) {
        { ($_ -ne 'Patch') -and $experimental} {
            $node.InnerText = "{0}.{1}.{2}" -f $version.Major, ($version.Minor + 1), 0
        }
        { ($_ -eq 'Major') -and -not $experimental } {
            $node.InnerText = "{0}.{1}.{2}" -f ($version.Major + 1), 0, 0
        }
        { ($_ -eq 'Minor') -and -not $experimental } {
            $node.InnerText = "{0}.{1}.{2}" -f $version.Major, ($version.Minor + 1), 0
        }
        'Patch' {
            $node.InnerText = "{0}.{1}.{2}" -f $version.Major, $version.Minor, ($version.Build + 1)
        }
        default {
            throw "Could not figure out how to apply patch policy $mode"
        }
    }
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

        if ($VersionSuffix) {
            SetVersionSuffix $xml.SelectSingleNode('/Project/PropertyGroup/VersionSuffix') | write-host
            SetVersionSuffix $xml.SelectSingleNode('/Project/PropertyGroup/ExperimentalProjectVersionSuffix') | write-host
            SetVersionSuffix $xml.SelectSingleNode('/Project/PropertyGroup/ExperimentalVersionSuffix') | write-host
        }

        $versionPrefix = $xml.SelectSingleNode('/Project/PropertyGroup/VersionPrefix')
        $epxVersionPrefix = $xml.SelectSingleNode('/Project/PropertyGroup/ExperimentalProjectVersionPrefix')
        $exVersionPrefix = $xml.SelectSingleNode('/Project/PropertyGroup/ExperimentalVersionPrefix')
        BumpVersion $epxVersionPrefix | write-host
        BumpVersion $exVersionPrefix | write-host
        $message = BumpVersion $versionPrefix
        Write-Host $message

        if ($PSCmdlet.ShouldProcess("Update $path")) {
            SaveXml $xml $path
            if (-not $NoCommit) {
                Invoke-Block { & git add $path }
                Invoke-Block { & git commit -m $message }
            }
        }
    }
    finally
    {
        Pop-Location
    }
}

