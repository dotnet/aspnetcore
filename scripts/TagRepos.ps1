#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Tags each repo according to VersionPrefix in version.props of that repo
.PARAMETER Push
    Push all updated tags
.PARAMETER ForceUpdateTag
    This will call git tag --force
#>
[cmdletbinding(SupportsShouldProcess = $true)]
param(
    [switch]$Push = $false,
    [switch]$ForceUpdateTag = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"

Assert-Git

$RepoRoot = Resolve-Path "$PSScriptRoot/../"

Get-Submodules $RepoRoot -Shipping | % {
    Push-Location $_.path | Out-Null
    try {

        if (-not $_.versionPrefix) {
            Write-Warning "Could not determine tag version for $(_.path)"
        }
        else {
            $tag = $_.versionPrefix
            Write-Host "$($_.module) => $tag"

            $gitTagArgs = @()
            if ($ForceUpdateTag) {
                $gitTagArgs += '--force'
            }

            Invoke-Block { & git tag @gitTagArgs $tag }

            if ($Push) {
                $gitPushArgs = @()
                if ($WhatIfPreference) {
                    $gitPushArgs += '--dry-run'
                }
                Invoke-Block { & git push @gitPushArgs origin "refs/tags/${tag}"  }
            }

            if ($WhatIfPreference) {
                Invoke-Block { & git tag -d $tag } | Out-Null
            }
        }
    }
    catch {
        Write-Host -ForegroundColor Red "Could not update $_"
        throw
    }
    finally {
        Pop-Location
    }
}


