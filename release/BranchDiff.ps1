# This script checks every repository in Universe for commits which exist in the $targetBranch but which do no exist in dev.
# A common use case for this script would be running it after a release to ensure that all commits in "patch" branches made it back into dev.
Param(
    [Parameter(Mandatory=$true)][string]$targetBranch
)

$outputFile = "PreviewCommitsNotInDev.txt"
if (Test-Path $outputFile)
{
    Remove-Item $outputFile
}
New-Item $outputFile -type file

Invoke-Expression "git clone git@github.com:aspnet/Universe.git"
Set-Location "Universe"
Invoke-Expression "./build.cmd /t:CloneRepositories"

Set-Location ".r"
$dirs = Get-ChildItem -Directory
foreach ($d in $dirs){
    Set-Location $d
    $format = "$d;%h;%an"
    Invoke-Expression "git log origin/dev..$targetBranch --pretty=format:'$format' | Out-File ../../../$outputFile -Encoding UTF8 -Append"
    Set-Location ..
}

Set-Location $PSScriptRoot
Remove-Item "Universe" -Recurse -Force
