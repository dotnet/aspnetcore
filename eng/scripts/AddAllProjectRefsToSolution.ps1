<#
.SYNOPSIS
This adds the complete closure of project references to a .sln file

.EXAMPLE
Let's say you have a folder of projects in src/Banana/, and a file src/Banana/Banana.sln.
To traverse the ProjectReference graph to add all dependency projects, run this script:

    ./eng/scripts/AddAllProjectRefsToSolution.ps1 -WorkingDir ./src/Banana/

.EXAMPLE
If src/Banana/ has multiple .sln files, use the -sln parameter.

    ./eng/scripts/AddAllProjectRefsToSolution.ps1 -WorkingDir ./src/Banana/ -SolutionFile src/Banana/Solution1.sln
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string]$WorkingDir,
    [Alias('sln')]
    [string]$SolutionFile
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path "$PSScriptRoot/../../"
$listFile = New-TemporaryFile

if (-not $WorkingDir) {
    $WorkingDir = Get-Location
}

Push-Location $WorkingDir
try {
    if (-not $SolutionFile) {

        $slnCount = Get-ChildItem *.sln | Measure

        if ($slnCount.count -eq 0) {
            Write-Error "Could not find a solution in this directory. Specify one with -sln <PATH>"
            exit 1
        }
        if ($slnCount.count -gt 1) {
            Write-Error "Multiple solutions found in this directory. Specify which one to modify with -sln <PATH>"
            exit 1
        }
        $SolutionFile = Get-ChildItem *.sln | select -first 1
    }

    & "$repoRoot\build.ps1" -projects "$(Get-Location)\**\*.*proj" /t:ShowProjectClosure "/p:ProjectsReferencedOutFile=$listFile"

    foreach ($proj in (Get-Content $listFile)) {
        & dotnet sln $SolutionFile add $proj
        if ($lastexitcode -ne 0) {
            Write-Warning "Failed to add $proj to $SolutionFile"
        }
    }
}
finally {
    Pop-Location
    rm $listFile -ea ignore
}
