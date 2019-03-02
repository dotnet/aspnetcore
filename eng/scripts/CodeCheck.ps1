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

function LogError {
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$message,
        [string]$FilePath
    )
    if ($env:TF_BUILD) {
        $prefix = "##vso[task.logissue type=error"
        if ($FilePath) {
            $prefix = "${prefix};sourcepath=$FilePath"
        }
        Write-Host "${prefix}]${message}"
    }
    Write-Host -f Red "error: $message"
    $script:errors += $message
}

try {
    if ($ci) {
        # Install dotnet.exe
        & $repoRoot/build.ps1 -ci -norestore /t:InstallDotNet
    }

    #
    # Versions.props and Version.Details.xml
    #

    Write-Host "Checking that Versions.props and Version.Details.xml match"
    [xml] $versionProps = Get-Content "$repoRoot/eng/Versions.props"
    [xml] $versionDetails = Get-Content "$repoRoot/eng/Version.Details.xml"

    $versionVars = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($vars in $versionProps.SelectNodes("//PropertyGroup[`@Label=`"Automated`"]/*")) {
        $versionVars.Add($vars.Name) | Out-Null
    }

    foreach ($dep in $versionDetails.SelectNodes('//Dependency')) {
        Write-Verbose "Found $dep"
        $varName = $dep.Name -replace '\.',''
        $varName = $varName -replace '\-',''
        $varName = "${varName}PackageVersion"
        $versionVar = $versionProps.SelectSingleNode("//PropertyGroup[`@Label=`"Automated`"]/$varName")
        if (-not $versionVar) {
            LogError "Missing version variable '$varName' in the 'Automated' property group in $repoRoot/eng/Versions.props"
            continue
        }

        $versionVars.Remove($varName) | Out-Null

        $expectedVersion = $dep.Version
        $actualVersion = $versionVar.InnerText

        if ($expectedVersion -ne $actualVersion) {
            LogError `
                "Version variable '$varName' does not match the value in Version.Details.xml. Expected '$expectedVersion', actual '$actualVersion'" `
                -filepath "$repoRoot\eng\Versions.props"
        }
    }

    foreach ($unexpectedVar in $versionVars) {
        LogError `
            "Version variable '$unexpectedVar' does not have a matching entry in Version.Details.xml. See https://github.com/aspnet/AspNetCore/blob/master/docs/ReferenceResolution.md for instructions on how to add a new dependency." `
            -filepath "$repoRoot\eng\Versions.props"
    }

    #
    # Solutions
    #

    Write-Host "Checking that solutions are up to date"

    Get-ChildItem "$repoRoot/*.sln" -Recurse `
        | ? {
            # These .sln files are used by the templating engine.
            ($_.Name -ne "RazorComponentsWeb-CSharp.sln") -and ($_.Name -ne "GrpcService-CSharp.sln")
        } `
        | % {
        Write-Host "  Checking $(Split-Path -Leaf $_)"
        $slnDir = Split-Path -Parent $_
        $sln = $_
        & dotnet sln $_ list `
            | ? { $_ -like '*proj' } `
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

    Write-Host "Re-generating references assemblies"
    Invoke-Block {
        & $PSScriptRoot\GenerateReferenceAssemblies.ps1 -ci:$ci
    }

    Write-Host "Re-generating package baselines"
    $dotnet = 'dotnet'
    if ($ci) {
        $dotnet = "$repoRoot/.dotnet/x64/dotnet.exe"
    }
    Invoke-Block {
        & $dotnet run -p "$repoRoot/eng/tools/BaselineGenerator/"
    }

    Write-Host "Run git diff to check for pending changes"

    # Redirect stderr to stdout because PowerShell does not consistently handle output to stderr
    $changedFiles = & cmd /c 'git --no-pager diff --ignore-space-at-eol --name-only 2>nul'

    if ($changedFiles) {
        foreach ($file in $changedFiles) {
            $filePath = Resolve-Path "${repoRoot}/${file}"
            LogError "Generated code is not up to date in $file." -filepath $filePath
            & git --no-pager diff --ignore-space-at-eol $filePath
        }
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
