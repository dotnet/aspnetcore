#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Updates each submodule this repo builds to new dependencies.props.
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
.PARAMETER Force
    Specified this to push commits without prompting.
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
    [switch]$Force,
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

    $build_errors = @()
    $submodules = Get-Submodules $RepoRoot
    foreach ($submodule in $submodules) {
        Push-Location $submodule.path
        try {
            Invoke-Block { & git fetch }
            Invoke-Block { & git checkout origin/$($submodule.branch) }
            $depsFile = Join-Path (Join-Path $($submodule.path) "build") "dependencies.props"

            if (!(Test-Path $depsFile)) {
                Write-Warning "No build\dependencies.props file exists for '$($submodule.module)'."
                continue
            }

            $koreBuildLock = "korebuild-lock.txt"

            $repoKoreBuildLock = (Join-Path $RepoRoot $koreBuildLock)
            $submoduleKoreBuildLock = (Join-Path $submodule.path $koreBuildLock)

            Copy-Item $repoKoreBuildLock $submoduleKoreBuildLock -Force

            Write-Verbose "About to update dependencies.props for $($submodule.module)"
            & .\run.ps1 upgrade deps --source $Source --id $LineupID --version $LineupVersion --deps-file $depsFile

            Invoke-Block { & git @gitConfigArgs add $depsFile $koreBuildLock }

            # If there were any changes test and push.
            & git diff --cached --quiet ./
            if ($LASTEXITCODE -ne 0) {
                Invoke-Block { & git @gitConfigArgs commit --quiet -m "Update dependencies.props`n`n[auto-updated: dependencies]" @GitCommitArgs }

                # Prepare this submodule for push
                $sshUrl = "git@github.com:aspnet/$($submodule.module)"
                Invoke-Block { & git remote set-url --push origin $sshUrl }

                # Test the submodule
                try {
                    Invoke-Block { & .\run.ps1 default-build /p:SkipTests=true }
                }
                catch {
                    Write-Warning "Error in $($submodule.module): $_"
                    $build_errors += @{
                        Repo    = $submodule.module
                        Message = $_
                    }
                    continue
                }

                # Push the changes
                if (-not $NoPush -and ($Force -or ($PSCmdlet.ShouldContinue("Pushing updates to repos.", 'Push the changes to these repos?')))) {
                    try {
                        Invoke-Block { & git @gitConfigArgs push origin HEAD:$($submodule.branch)}
                    }
                    catch {
                        Write-Warning "Error in pushing $($submodule.module): $_"
                        $build_errors += @{
                            Repo    = $submodule.module
                            Message = $_
                        }
                        continue
                    }
                }
            }
            else {
                Write-Host "No changes in $($submodule.module)"
            }
        }
        catch {
            Write-Warning "Error in $($submodule.module): $_"
            $build_errors += @{
                Repo    = $submodule.module
                Message = $_
            }
        }
        finally {
            Pop-Location
        }
    }

    if ($build_errors.Count -gt 0 ) {
        Write-Warning "The following repos failed:"
        foreach ($error in $build_errors) {
            Write-Warning "   - $($error.Repo)"
        }
        throw "Failed to build"
    }
}
finally {
    Pop-Location
}
