#
# RunAutobahnTests.ps1
#
param([Parameter(Mandatory=$true)][string]$ServerUrl, [string[]]$Cases = @("*"), [string]$OutputDir)

if(!(Get-Command wstest -ErrorAction SilentlyContinue)) {
    throw "Missing required command 'wstest'. See README.md in Microsoft.AspNetCore.WebSockets.Server.Test project for information on installing Autobahn Test Suite."
}

if(!$OutputDir) {
    $OutputDir = Convert-Path "."
    $OutputDir = Join-Path $OutputDir "autobahnreports"
}

$Spec = Convert-Path (Join-Path $PSScriptRoot "autobahn.spec.json")

$CasesArray = [string]::Join(",", @($Cases | ForEach-Object { "`"$_`"" }))

$SpecJson = [IO.File]::ReadAllText($Spec).Replace("OUTPUTDIR", $OutputDir.Replace("\", "\\")).Replace("WEBSOCKETURL", $ServerUrl).Replace("`"CASES`"", $CasesArray)

$TempFile = [IO.Path]::GetTempFileName()

try {
    [IO.File]::WriteAllText($TempFile, $SpecJson)
    & wstest -m fuzzingclient -s $TempFile
} finally {
    if(Test-Path $TempFile) {
        rm $TempFile
    }
}