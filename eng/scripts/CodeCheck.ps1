#requires -version 5
<#
.SYNOPSIS
This script runs a quick check for common errors, such as checking that Visual Studio solutions are up to date or that generated code has been committed to source.
#>
param(
    [switch]$ci,
    # Optional arguments that enable downloading an internal
    # runtime or runtime from a non-default location
    [string]$DotNetRuntimeSourceFeed,
    [string]$DotNetRuntimeSourceFeedKey
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
        [string]$FilePath,
        [string]$Code
    )
    if ($env:TF_BUILD) {
        $prefix = "##vso[task.logissue type=error"
        if ($FilePath) {
            $prefix = "${prefix};sourcepath=$FilePath"
        }
        if ($Code) {
            $prefix = "${prefix};code=$Code"
        }
        Write-Host "${prefix}]${message}"
    }
    $fullMessage = "error ${Code}: $message"
    if ($FilePath) {
        $fullMessage += " [$FilePath]"
    }
    Write-Host -f Red $fullMessage
    $script:errors += $fullMessage
}

try {
    if ($ci) {
        # Install dotnet.exe
        if ($DotNetRuntimeSourceFeed -or $DotNetRuntimeSourceFeedKey) {
            & $repoRoot/restore.cmd -ci -NoBuildNodeJS -DotNetRuntimeSourceFeed $DotNetRuntimeSourceFeed -DotNetRuntimeSourceFeedKey $DotNetRuntimeSourceFeedKey
        }
        else{
            & $repoRoot/restore.cmd -ci -NoBuildNodeJS
        }
    }

    . "$repoRoot/activate.ps1"

    #
    # Duplicate .csproj files can cause issues with a shared build output folder
    #

    $projectFileNames = New-Object 'System.Collections.Generic.HashSet[string]'

    # Ignore duplicates in submodules. These should be isolated from the rest of the build.
    # Ignore duplicates in the .ref folder. This is expected.
    Get-ChildItem -Recurse "$repoRoot/src/*.*proj" `
        | ? { $_.FullName -notmatch 'submodules' -and $_.FullName -notmatch 'node_modules' } `
        | ? { (Split-Path -Leaf (Split-Path -Parent $_)) -ne 'ref' } `
        | % {
            $fileName = [io.path]::GetFileNameWithoutExtension($_)
            if (-not ($projectFileNames.Add($fileName))) {
                LogError -code 'BUILD003' -filepath $_ `
                    "Multiple project files named '$fileName' exist. Project files should have a unique name to avoid conflicts in build output."
            }
        }

    #
    # Versions.props and Version.Details.xml
    #

    Write-Host "Checking that Versions.props and Version.Details.xml match"
    [xml] $versionProps = Get-Content "$repoRoot/eng/Versions.props"
    [xml] $versionDetails = Get-Content "$repoRoot/eng/Version.Details.xml"
    $globalJson = Get-Content $repoRoot/global.json | ConvertFrom-Json

    $versionVars = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($vars in $versionProps.SelectNodes("//PropertyGroup[`@Label=`"Automated`"]/*")) {
        $versionVars.Add($vars.Name) | Out-Null
    }

    foreach ($dep in $versionDetails.SelectNodes('//Dependency')) {
        Write-Verbose "Found $dep"

        $expectedVersion = $dep.Version

        if ($dep.Name -in $globalJson.'msbuild-sdks'.PSObject.Properties.Name) {

            $actualVersion = $globalJson.'msbuild-sdks'.($dep.Name)

            if ($expectedVersion -ne $actualVersion) {
                LogError `
                    "MSBuild SDK version '$($dep.Name)' in global.json does not match the value in Version.Details.xml. Expected '$expectedVersion', actual '$actualVersion'" `
                    -filepath "$repoRoot\global.json"
            }
        }
        else {
            $varName = $dep.Name -replace '\.',''
            $varName = $varName -replace '\-',''
            $varName = "${varName}PackageVersion"

            $versionVar = $versionProps.SelectSingleNode("//PropertyGroup[`@Label=`"Automated`"]/$varName")
            $actualVersion = $versionVar.InnerText
            $versionVars.Remove($varName) | Out-Null

            if (-not $versionVar) {
                LogError "Missing version variable '$varName' in the 'Automated' property group in $repoRoot/eng/Versions.props"
                continue
            }

            if ($expectedVersion -ne $actualVersion) {
                LogError `
                    "Version variable '$varName' does not match the value in Version.Details.xml. Expected '$expectedVersion', actual '$actualVersion'" `
                    -filepath "$repoRoot\eng\Versions.props"
            }
        }
    }

    foreach ($unexpectedVar in $versionVars) {
        LogError `
            "Version variable '$unexpectedVar' does not have a matching entry in Version.Details.xml. See https://github.com/dotnet/aspnetcore/blob/master/docs/ReferenceResolution.md for instructions on how to add a new dependency." `
            -filepath "$repoRoot\eng\Versions.props"
    }

    Write-Host "Checking that solutions are up to date"

    Get-ChildItem "$repoRoot/*.sln" -Recurse `
        | ? {
            # These .sln files are used by the templating engine.
            ($_.Name -ne "BlazorServerWeb_CSharp.sln") -and
            ($_.Name -ne "BlazorWasm-CSharp.sln")
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
    Invoke-Block {
        & dotnet run -p "$repoRoot/eng/tools/BaselineGenerator/"
    }

    Write-Host "Run git diff to check for pending changes"

    # Redirect stderr to stdout because PowerShell does not consistently handle output to stderr
    $changedFiles = & cmd /c 'git --no-pager diff --ignore-space-change --name-only 2>nul'

    # Temporary: Disable check for blazor js file and nuget.config (updated automatically for
    # internal builds)
    $changedFilesExclusions = @("src/Components/Web.JS/dist/Release/blazor.server.js", "NuGet.config")

    if ($changedFiles) {
        foreach ($file in $changedFiles) {
            if ($changedFilesExclusions -contains $file) {continue}
            $filePath = Resolve-Path "${repoRoot}/${file}"
            LogError "Generated code is not up to date in $file. You might need to regenerate the reference assemblies or project list (see docs/ReferenceAssemblies.md and docs/ReferenceResolution.md)" -filepath $filePath
            & git --no-pager diff --ignore-space-change $filePath
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
        Write-Host -f Red $err
    }

    if ($errors) {
        exit 1
    }
}
