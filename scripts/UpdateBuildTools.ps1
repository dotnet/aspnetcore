#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Updates the build tools version and generates a commit message with the list of changes
.PARAMETER RepoRoot
    The directory containing the repo
.PARAMETER GitAuthorName
    The author name to use in the commit message. (Optional)
.PARAMETER GitAuthorEmail
    The author email to use in the commit message. (Optional)
.PARAMETER GitCommitArgs
    Additional arguments to pass into git-commit
.PARAMETER NoCommit
    Make changes without executing git-commit
.PARAMETER ToolsSource
    The location of the build tools
.PARAMETER Force
    Specified this to make a commit with any changes
#>
[cmdletbinding(SupportsShouldProcess = $true)]
param(
    [string]$RepoRoot,
    [string]$GitAuthorName = $null,
    [string]$GitAuthorEmail = $null,
    [string[]]$GitCommitArgs = @(),
    [string]$ToolsSource = 'https://ci.dot.net/buildtools',
    [switch]$NoCommit,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

if (-not $RepoRoot) {
    $RepoRoot = Resolve-Path "$PSScriptRoot\.."
}

Import-Module "$PSScriptRoot/common.psm1" -Scope Local -Force

function Get-KoreBuildVersion {
    $lockFile = "$RepoRoot/korebuild-lock.txt"
    if (!(Test-Path $lockFile)) {
        return ''
    }
    $version = Get-Content $lockFile | Where-Object { $_ -like 'version:*' } | Select-Object -first 1
    if (!$version) {
        Write-Error "Failed to parse version from $lockFile. Expected a line that begins with 'version:'"
    }
    $version = $version.TrimStart('version:').Trim()
    return $version
}

Push-Location $RepoRoot
try {
    Assert-Git

    $oldVersion = Get-KoreBuildVersion

    & "$RepoRoot/run.ps1" -Update -ToolsSource $ToolsSource -Command noop | Out-Null

    $newVersion = Get-KoreBuildVersion

    if ($oldVersion -eq $newVersion) {
        Write-Host -ForegroundColor Magenta 'No changes to build tools'
        exit 0
    }

    Invoke-Block { git add "$RepoRoot/korebuild-lock.txt" }
    Invoke-Block { git add "$RepoRoot/build/dependencies.props" }

    $shortMessage = "Updating BuildTools from $oldVersion to $newVersion"
    # add this to the commit message to make it possible to filter commit triggers based on message
    $message = "$shortMessage`n`n[auto-updated: buildtools]"

    if (-not $NoCommit -and ($Force -or ($PSCmdlet.ShouldContinue($shortMessage, 'Create a new commit with these changes?')))) {

        $gitConfigArgs = @()
        if ($GitAuthorName) {
            $gitConfigArgs += '-c', "user.name=$GitAuthorName"
        }

        if ($GitAuthorEmail) {
            $gitConfigArgs += '-c', "user.email=$GitAuthorEmail"
        }

        Invoke-Block { git @gitConfigArgs commit -m $message @GitCommitArgs }
    }
    else {
        # If composing this script with others, return the message that would have been used
        return @{
            message = $message
        }
    }
}
finally {
    Pop-Location
}
