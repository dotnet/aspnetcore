#!/usr/bin/env pwsh -c

<#
.SYNOPSIS
    Updates git submodules and generates a commit message with the list of changes
.PARAMETER GitAuthorName
    The author name to use in the commit message. (Optional)
.PARAMETER GitAuthorEmail
    The author email to use in the commit message. (Optional)
.PARAMETER GitCommitArgs
    Additional arguments to pass into git-commit
.PARAMETER NoCommit
    Make changes without executing git-commit
.PARAMETER Force
    Specified this to make a commit with any changes
.PARAMETER IgnoredRepos
    Repos to not update (likely because they are temporarily broken).
#>
[cmdletbinding(SupportsShouldProcess = $true)]
param(
    [string]$GitAuthorName = $null,
    [string]$GitAuthorEmail = $null,
    [string[]]$GitCommitArgs = @(),
    [switch]$NoCommit,
    [switch]$Force,
    [string[]]$IgnoredRepos = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ModuleDirectory = Join-Path $RepoRoot "modules"

Import-Module "$PSScriptRoot/common.psm1" -Scope Local -Force

function Get-GitChanges([string]$Path) {
    Write-Verbose "git diff --cached --quiet $Path"
    & git diff --cached --quiet $Path | Out-Null
    if ($LastExitCode -ne 0) {
        return $true
    }
    Write-Verbose "git diff --quiet $Path"
    & git diff --quiet $Path | Out-Null
    return $LastExitCode -ne 0
}

Push-Location $RepoRoot | Out-Null
try {
    Assert-Git

    Write-Host "Checking that submodules are in a clean state first..."
    if (Get-GitChanges $ModuleDirectory) {
        Write-Error "$RepoRoot/modules is in an unclean state. Reset submodules first by running ``git submodule update``"
        exit 1
    }

    $submodules = Get-Submodules $RepoRoot -Verbose:$VerbosePreference

    foreach ($submodule in  $submodules) {
        $submoduleName = $submodule.module
        if ($IgnoredRepos.Contains($submoduleName))
        {
            Write-Host "Skipping $submoduleName due to IgnoredRepos."
            continue
        }

        $submodulePath = $submodule.path
        Write-Host "Updating $submodulePath"

        $vcs_name = "BUILD_VCS_NUMBER_" + ($submodule.module -replace '\.','_')
        $newCommit = [environment]::GetEnvironmentVariable($vcs_name)

        if (-not $newCommit) {
            if ($env:TEAMCITY_PROJECT_NAME) {
                throw "TeamCity env variable '$vcs_name' not found. Make sure to configure a VCS root for $submodulePath"
            }
            Invoke-Block { & git submodule update --remote $submodulePath }
            Push-Location $submodulePath | Out-Null
            try {
                $newCommit = $(git rev-parse HEAD)
            }
            finally {
                Pop-Location | Out-Null
            }
        }
        else {
            Push-Location $submodulePath | Out-Null
            try {
                Invoke-Block { & git checkout $newCommit }
            }
            finally {
                Pop-Location | Out-Null
            }
        }

        $submodule.newCommit = $newCommit
        if ($newCommit -ne $submodule.commit) {
            $submodule.changed = $true
            Write-Host -ForegroundColor Cyan "`t=> $($submodule.module) updated to $($submodule.newCommit)"
        }
        else {
            Write-Host -ForegroundColor Magenta "`t$($submodule.module) did not change"
        }
    }

    $changes = $submodules `
        | ? { $_.changed } `
        | % {
            Invoke-Block { & git add $_.path }
            "$($_.module) => $($_.newCommit)"
        }

    if ($changes) {
        $shortMessage = "Updating submodule(s) `n`n$( $changes -join "`n" )"
        # add this to the commit message to make it possible to filter commit triggers based on message
        $message = "$shortMessage`n`n[auto-updated: submodules]"
        if (-not $NoCommit -and ($Force -or ($PSCmdlet.ShouldContinue($shortMessage, 'Create a new commit with these changes?')))) {

            $gitConfigArgs = @()
            if ($GitAuthorName) {
                $gitConfigArgs += '-c',"user.name=$GitAuthorName"
            }

            if ($GitAuthorEmail) {
                $gitConfigArgs += '-c',"user.email=$GitAuthorEmail"
            }

            Invoke-Block { & git @gitConfigArgs commit -m $message @GitCommitArgs }
        }
        else {
            # If composing this script with others, return the message that would have been used
            return @{
                message = $message
            }
        }
    }
    else {
        Write-Host -ForegroundColor Magenta 'No changes detected in git submodules'
    }
}
finally {
    Pop-Location
}
