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
#>
[cmdletbinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory=$true)]
    [string]$Source,
    [Parameter(Mandatory=$true)]
    [string]$LineupID,
    [Parameter(Mandatory=$true)]
    [string]$LineupVersion,
    [switch]$NoPush,
    [string[]]$GitCommitArgs = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

Import-Module "$PSScriptRoot/common.psm1" -Scope Local -Force

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ModuleDirectory = Join-Path $RepoRoot "modules"

Push-Location $ModuleDirectory
try {
    # Init all submodules
    Invoke-Block { & git submodule update --init }

    $update_errors = @()
    $submodules = Get-Submodules $RepoRoot
    $updated_submodules = @()
    foreach($submodule in $submodules)
    {
        Push-Location $submodule.path
        try {
            $depsFile = Join-Path (Join-Path $($submodule.path) "build") "dependencies.props"

            if (!Test-Path $depsFile)
            {
                Write-Warning "No build\dependencies.props file exists for $($submodule.module). "
                continue
            }

            # Move to latest commit on tracked branch
            Invoke-Block { & git checkout --quiet $submodule.branch }

            Invoke-Block { & .\run.ps1 -Update upgrade deps --source $Source --id $LineupID --version $LineupVersion --deps-file $depsFile }
            Invoke-Block { & git add $depsFile }

            Invoke-Block { & git commit --quiet -m "Update dependencies.props`n`n[auto-updated: dependencies]" @GitCommitArgs }
            $sshUrl = "git@github.com:aspnet/$($submodule.module)"
            Invoke-Block { & git remote set-url --push origin $sshUrl }
            $updated_submodules += $submodule
        }
        catch
        {
            $update_errors += $_
        }
        finally {
            Pop-Location
        }
    }

    if ($update_errors.Count -gt 0 )
    {
        throw 'Failed to update'
    }

    if (-not $NoPush -and ($Force -or ($PSCmdlet.ShouldContinue($shortMessage, 'Push the changes to these repos?'))))
    {
        $push_errors = @()
        foreach($submodule in $updated_submodules)
        {
            Push-Location $submodule.path
            try {
                Invoke-Block { & git push origin $submodule.branch}
            }
            catch
            {
                $push_errors += $_
            }
            finally {
                Pop-Location
            }
        }

        if ($push_errors.Count -gt 0 )
        {
            throw 'Failed to push'
        }
    }
}
finally {
    Pop-Location
}
