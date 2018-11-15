#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Generates a tag on this repo and adds a tag for each submodule that corresponds
    to the value in version.props
.PARAMETER Push
    Push the tag to origin
.PARAMETER OutFile
    When specified, generate a .csv with repo names and tags
.PARAMETER WhatIf
    Dry run
#>
[cmdletbinding(PositionalBinding = $false, SupportsShouldProcess = $true)]
param(
    [switch]$Push,
    [string]$OutFile
)

$ErrorActionPreference = 'Stop'
Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"
Set-StrictMode -Version 1

function New-GitTag {
    [cmdletbinding(SupportsShouldProcess = $true)]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repo,
        [Parameter(Mandatory = $true)]
        [string]$Tag
    )

    Push-Location $Repo
    try {
        git show-ref --tags --verify "refs/tags/$Tag" -q
        $existingTag = $?

        if ($existingTag) {
            Write-Warning "${Repo}: Tag '$Tag' already exists. Skipped adding tag"
        }
        else {
            if ($PSCmdlet.ShouldProcess($Repo, "Tag $Tag")) {
                Invoke-Block { & git tag -m "v$Tag" $Tag HEAD }
                Write-Host -f Magenta "${Repo}: added tag '$Tag'"
            }

            if ($Push -and $PSCmdlet.ShouldProcess($Repo, "Push tag $Tag to origin")) {
                Invoke-Block { & git push origin refs/tags/$Tag }
            }
        }
    }
    finally {
        Pop-Location
    }
}

#
# Gets the package version by invoking KoreBuild on a repo with a custom target that spits out the package version
#
function Get-PackageVersion([string]$repoRoot) {
    $buildScript = if (-not $IsCoreCLR -or $IsWindows) { 'build.ps1' } else { 'build.sh' }
    $inspectTarget = "/p:CustomAfterKoreBuildTargets=$PSScriptRoot/GetPackageVersion.targets"
    Write-Verbose "Running `"$repoRoot/$buildScript`" $inspectTarget /v:m /p:IsFinalBuild=true /t:Noop /t:GetPackageVersion"
    # Add the /t:Noop target which may be used by the bootstrapper to skip unimportant initialization
    $output = & "$repoRoot/$buildScript" $inspectTarget /v:m /p:IsFinalBuild=true /t:Noop /t:GetPackageVersion
    $output | out-string | Write-Verbose
    if (-not $? -or $LASTEXITCODE -ne 0) {
        throw "$buildScript failed on $repoRoot. Exit code $LASTEXITCODE"
    }
    $packageVersion = $output | where-object { $_ -like '*PackageVersion=*' } | select-object -first 1
    $packageVersion = $packageVersion -replace 'PackageVersion=', ''
    if ($packageVersion) { $packageVersion = $packageVersion.Trim() }
    if (-not $packageVersion) {
        throw "Could not determine final package version for $repoRoot"
    }
    return $packageVersion.Trim()
}

$repoRoot = Resolve-Path "$PSScriptRoot/../"

Write-Warning "Make sure you have run ``git submodule update`` first to pin the submodules to the correct commit"
if (-not $PSCmdlet.ShouldContinue("Continue?", "This will apply tags to all submodules")) {
    Write-Host "Exiting"
    exit 1
}


$repoTag = Get-PackageVersion $repoRoot
New-GitTag $repoRoot $repoTag -WhatIf:$WhatIfPreference

$tags = @([pscustomobject] @{
        repo   = $(git config remote.origin.url)
        tag    = $repoTag
        commit = $(git rev-parse HEAD)
    })

Get-Submodules $repoRoot | ForEach-Object {
    $modPath = $_.path
    $module = $_.module
    if (-not (Test-Path (Join-Path $_.path 'version.props'))) {
        Write-Warning "$module does not have a version.props file. Skipping"
        return
    }

    try {
        $tag = Get-PackageVersion $_.path
        if ($tag -ne $repoTag) {
            Write-Warning "${module}: version ($tag) does not match repo ($repoTag)"
        }
        $tags += [pscustomobject] @{
            repo   = $_.remote
            tag    = $tag
            commit = $_.commit
        }
    }
    catch {
        Write-Warning "${module}: Could not automatically determine tag for $modPath. Skipping"
        return
    }

    New-GitTag $_.path $tag -WhatIf:$WhatIfPreference
}

$tags | Format-Table

if ($OutFile) {
    $tags | Select-Object -Property * | Export-Csv -Path $OutFile -WhatIf:$false -NoTypeInformation
}
