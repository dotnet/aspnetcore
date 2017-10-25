#!/usr/bin/env powershell

<#
.SYNOPSIS
    Updates git submodules and generates a commit message with the list of changes
.PARAMETER GitCommitArgs
    Additional arguments to pass into git-commit
.PARAMETER NoCommit
    Make changes without executing git-commit
.PARAMETER Force
    Specified this to make a commit with any changes
#>
[cmdletbinding(SupportsShouldProcess = $true)]
param(
    [string[]]$GitCommitArgs = @(),
    [switch]$NoCommit,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$RepoRoot = Resolve-Path "$PSScriptRoot\.."

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

try {
    Assert-Git

    if (Get-GitChanges "$RepoRoot/modules") {
        Write-Error "$RepoRoot/modules is in an unclean state. Reset submodules first by running ``git submodule update``"
        exit 1
    }

    Invoke-Block { & git submodule update --init }

    $submodules = @()

    Get-ChildItem "$RepoRoot/modules/*" -Directory | % {
        Push-Location $_
        try {
            $data = @{
                path      = $_
                module    = $_.Name
                commit    = $(git rev-parse HEAD)
                newCommit = $null
                changed   = $false
            }
            Write-Verbose "$($data.module) is at $($data.commit)"
            $submodules += $data
        }
        finally {
            Pop-Location
        }
    }

    $changes = $submodules `
    | % {
        Push-Location $_.path
        try {
            $vcs_name = "BUILD_VCS_NUMBER_" + $_.module
            $newCommit = [environment]::GetEnvironmentVariable($vcs_name)

            if($newCommit -eq $null)
            {
                Write-Warning "TeamCity env variable '$vcs_name' not found."
                Write-Warning "git submodule update --remote"
                Invoke-Block { & git submodule update --remote }
                $newCommit = $(git rev-parse HEAD)
            }
            else
            {
                Invoke-Block { & git checkout $newCommit }
            }

            $_.newCommit = $newCommit
            if ($newCommit -ne $_.commit) {
                $_.changed = $true
                Write-Verbose "$($_.module) updated to $($_.newCommit)"
            }
            else {
                Write-Verbose "$($_.module) did not change"
            }
            return $_
        }
        finally {
            Pop-Location
        }
    } `
    | ? { $_.changed } `
    | % { "$($_.module) to $($_.newCommit.Substring(0, 8))" }

    $submodules `
        | ? { $_.changed } `
        | % {
            Invoke-Block { & git add $_.path }
        }

    if ($changes) {
        $shortMessage = "Updating submodule(s) $( $changes -join ' ,' )"
        # add this to the commit message to make it possible to filter commit triggers based on message
        $message = "$shortMessage`n`n[auto-updated: submodules]"
        if (-not $NoCommit -and ($Force -or ($PSCmdlet.ShouldContinue($shortMessage, 'Create a new commit with these changes?')))) {
            Invoke-Block { & git commit -m $message @GitCommitArgs }
        } else {
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
