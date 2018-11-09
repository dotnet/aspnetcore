#!/usr/bin/env pwsh -c
<#
.PARAMETER BuildXml
    The URL or file path to a build.xml file that defines package versions to be used
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    $BuildXml,
    [switch]$NoCommit,
    [string]$GithubUpstreamBranch,
    [string]$GithubEmail,
    [string]$GithubUsername,
    [string]$GithubToken
)

$ErrorActionPreference = 'Stop'
Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"
Set-StrictMode -Version 1
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

if (-not $NoCommit) {
    Set-GitHubInfo $GithubToken $GithubUsername $GithubEmail
}

$depsPath = Resolve-Path "$PSScriptRoot/../build/dependencies.props"
[xml] $dependencies = LoadXml $depsPath

if ($BuildXml -like 'http*') {
    $url = $BuildXml
    New-Item -Type Directory "$PSScriptRoot/../obj/" -ErrorAction Ignore
    $BuildXml = "$PSScriptRoot/../obj/build.xml"
    Write-Verbose "Downloading from $url to $BuildXml"
    Invoke-WebRequest -OutFile $BuildXml $url
}

[xml] $remoteDeps = LoadXml $BuildXml

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

if (-not $NoCommit) {
    $currentBranch = Invoke-Block { & git rev-parse --abbrev-ref HEAD }
    $destinationBranch = "dotnetbot/UpdateDeps"
    Invoke-Block { & git checkout -tb $destinationBranch "origin/$GithubUpstreamBranch" }
}

try {
    $updatedVars = UpdateVersions $variables $dependencies $depsPath

    if (-not $NoCommit) {
        $body = CommitUpdatedVersions $updatedVars $dependencies $depsPath

        if ($body) {
            CreatePR "aspnet" $GithubUsername $GithubUpstreamBranch $destinationBranch $body $GithubToken
        }
    }
}
finally {
    if (-not $NoCommit) {
        Invoke-Block { & git checkout $currentBranch }
    }
}
