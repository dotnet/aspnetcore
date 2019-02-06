#requires -version 5
<#
.SYNOPSIS
This script runs a quick check for common errors, such as checking that Visual Studio solutions are up to date or that generated code has been committed to source.
#>
param(
    [switch]$ci
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1
Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"

$repoRoot = Resolve-Path "$PSScriptRoot/../.."

[string[]] $errors = @()

function LogError([string]$message) {
    Write-Host -f Red "error: $message"
    $script:errors += $message
}

try {
    #
    # Solutions
    #

    if ($ci) {
        & $repoRoot/build.ps1 -ci /t:InstallDotNet
    }

    Write-Host "Checking that Versions.props and Version.Details.xml match"
    [xml] $versionProps = Get-Content "$repoRoot/eng/Versions.props"
    [xml] $versionDetails = Get-Content "$repoRoot/eng/Version.Details.xml"
    foreach ($dep in $versionDetails.SelectNodes('//ProductDependencies/Dependency')) {
        Write-Verbose "Found $dep"
        $varName = $dep.Name -replace '\.',''
        $varName = $varName -replace '\-',''
        $varName = "${varName}PackageVersion"
        $versionVar = $versionProps.SelectSingleNode("//PropertyGroup[`@Label=`"Automated`"]/$varName")
        if (-not $versionVar) {
            LogError "Missing version variable '$varName' in the 'Automated' property group in $repoRoot/eng/Versions.props"
            continue
        }

        $expectedVersion = $dep.Version
        $actualVersion = $versionVar.InnerText

        if ($expectedVersion -ne $actualVersion) {
            LogError "Version variable '$varName' does not match the value in Version.Details.xml. Expected '$expectedVersion', actual '$actualVersion'"
        }
    }

    Write-Host "Checking that solutions are up to date"

    Get-ChildItem "$repoRoot/*.sln" -Recurse `
        | ? {
            # This .sln file is used by the templating engine.
            $_.Name -ne "RazorComponentsWeb-CSharp.sln"
        } `
        | % {
        Write-Host "  Checking $(Split-Path -Leaf $_)"
        $slnDir = Split-Path -Parent $_
        $sln = $_
        & dotnet sln $_ list `
            | ? { $_ -ne 'Project(s)' -and $_ -ne '----------' } `
            | % {
                $proj = Join-Path $slnDir $_
                if (-not (Test-Path $proj)) {
                    LogError "Missing project. Solution references a project which does not exist: $proj. [$sln] "
                }
            }
        }

    #
    # Generated code check
    #

    Write-Host "Re-running code generation"

    Write-Host "Re-generating project lists"
    Invoke-Block {
        & $PSScriptRoot\GenerateProjectList.ps1 -ci:$ci
    }

    Write-Host "Re-generating package baselines"
    $dotnet = 'dotnet'
    if ($ci) {
        $dotnet = "$repoRoot/.dotnet/x64/dotnet.exe"
    }
    Invoke-Block {
        & $dotnet run -p "$repoRoot/eng/tools/BaselineGenerator/"
    }

    Write-Host "git diff"
    & git diff --ignore-space-at-eol --exit-code
    if ($LastExitCode -ne 0) {
        $status = git status -s | Out-String
        $status = $status -replace "`n","`n    "
        LogError "Generated code is not up to date."
    }
}
finally {
    Write-Host ""
    Write-Host "Summary:"
    Write-Host ""
    Write-Host "   $($errors.Length) error(s)"
    Write-Host ""

    foreach ($err in $errors) {
        Write-Host -f Red "error : $err"
    }

    if ($errors) {
        exit 1
    }
}
