#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Updates each repo Universe builds to new dependencies.props.
.PARAMETER Source
    The NuGet package source to find the lineup on.
.PARAMETER LineupID
    The ID of the Lineup to determine which versions to use.
.PARAMETER LineupVersion
    The version of the Lineup to be used.
.PARAMETER NoPush
    Make commits without pusing.
.PARAMETER GitAuthorName
    The author name to use in the commit message. (Optional)
.PARAMETER GitAuthorEmail
    The author email to use in the commit message. (Optional)
.PARAMETER GitCommitArgs
    Any remaining arguments are passed as arguments to 'git commit' actions in each repo.
#>
[cmdletbinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$Source,
    [Parameter(Mandatory = $true)]
    [string]$LineupID,
    [Parameter(Mandatory = $true)]
    [string]$LineupVersion,
    [switch]$NoPush,
    [string]$GitAuthorName = $null,
    [string]$GitAuthorEmail = $null,
    [string[]]$GitCommitArgs = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

Import-Module "$PSScriptRoot/common.psm1" -Scope Local -Force

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ModuleDirectory = Join-Path $RepoRoot "modules"

$gitConfigArgs = @()
if ($GitAuthorName) {
    $gitConfigArgs += '-c', "user.name=$GitAuthorName"
}

if ($GitAuthorEmail) {
    $gitConfigArgs += '-c', "user.email=$GitAuthorEmail"
}

Push-Location $ModuleDirectory
try {
    # Init all submodules
    Write-Verbose "Updating submodules..."
    Invoke-Block { & git submodule update --init } | Out-Null
    Write-Verbose "Submodules updated."

    $update_errors = @()
    $submodules = Get-Submodules $RepoRoot
    $updated_submodules = @()
    foreach ($submodule in $submodules) {
        Push-Location $submodule.path
        try {
            $depsFile = Join-Path (Join-Path $($submodule.path) "build") "dependencies.props"

            if (!(Test-Path $depsFile)) {
                Write-Warning "No build\dependencies.props file exists for $($submodule.module)."
                continue
            }

            Write-Verbose "About to update dependencies.props for $($submodule.module)"
            & .\run.ps1 -Update upgrade deps --source $Source --id $LineupID --version $LineupVersion --deps-file $depsFile

            Invoke-Block { & git @gitConfigArgs commit --quiet -m "Update dependencies.props`n`n[auto-updated: dependencies]" @GitCommitArgs }

            $sshUrl = "git@github.com:aspnet/$($submodule.module)"
            Invoke-Block { & git remote set-url --push origin $sshUrl }
            $updated_submodules += $submodule
        }
        catch {
            Write-Warning "Error in $($submodule.module)"
            $update_errors += $_
        }
        finally {
            Pop-Location
        }
    }

    if ($update_errors.Count -gt 0 ) {
        foreach ($update_error in $update_errors) {
            Write-Error "$update_error"
        }

        throw 'Failed to update'
    }
    else {
        Write-Verbose "All updates successful!"
    }

    if (-not $NoPush -and ($Force -or ($PSCmdlet.ShouldContinue($shortMessage, 'Push the changes to these repos?')))) {
        $push_errors = @()
        foreach ($submodule in $updated_submodules) {
            Push-Location $submodule.path
            try {
                Invoke-Block { & git @gitConfigArgs push origin HEAD:$submodule.branch}
            }
            catch {
                $push_errors += $_
            }
            finally {
                Pop-Location
            }
        }

        if ($push_errors.Count -gt 0 ) {
            throw 'Failed to push'
        }
    }
}
finally {
    Pop-Location
}
