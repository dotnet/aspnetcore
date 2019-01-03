#requires -version 5
<#
.SYNOPSIS
This script runs a quick check for common errors, such as checking that Visual Studio solutions are up to date or that generated code has been committed to source.
#>

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1
Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"

$repoRoot = Resolve-Path "$PSScriptRoot/../../"

[string[]] $errors = @()

try {
    #
    # Solutions
    #

    Write-Host "Checking that solutions are up to date"

    Get-ChildItem "$repoRoot/*.sln" -Recurse | % {
        Write-Host "  Checking $(Split-Path -Leaf $_)"
        $slnDir = Split-Path -Parent $_
        $sln = $_
        & dotnet sln $_ list `
            | ? { $_ -ne 'Project(s)' -and $_ -ne '----------' } `
            | % {
                $proj = Join-Path $slnDir $_
                if (-not (Test-Path $proj)) {
                    $errors += "Missing project. Solution references a project which does not exist: $proj. [$sln] "
                }
            }
        }

    #
    # Generated code check
    #

    Write-Host "Re-running code generation"

    Invoke-Block {
        & $repoRoot/build.cmd /t:GenerateProjectList
    }

    & git diff-index --quiet HEAD --
    if ($LastExitCode -ne 0) {
        $status = git status -s | Out-String
        $status = $status -replace "`n","`n    "
        $errors += "Generated code is not up to date:`n    $status"
    }
}
finally {
    foreach ($err in $errors) {
        Write-Host -f Red "error : $err"
    }

    if ($errors) {
        exit 1
    }
}
