#
# RunAutobahnTests.ps1
#
param([Parameter(Mandatory=$true)][string]$ServerUrl, [string[]]$Cases = @("*"), [string]$OutputDir, [int]$Iterations = 1)

if(!(Get-Command wstest -ErrorAction SilentlyContinue)) {
    throw "Missing required command 'wstest'. See README.md in Microsoft.AspNetCore.WebSockets.Server.Test project for information on installing Autobahn Test Suite."
}

if(!$OutputDir) {
    $OutputDir = Convert-Path "."
    $OutputDir = Join-Path $OutputDir "autobahnreports"
}

Write-Host "Launching Autobahn Test Suite ($Iterations iteration(s))..."

0..($Iterations-1) | % {
    $iteration = $_

    $Spec = Convert-Path (Join-Path $PSScriptRoot "autobahn.spec.json")

    $CasesArray = [string]::Join(",", @($Cases | ForEach-Object { "`"$_`"" }))

    $SpecJson = [IO.File]::ReadAllText($Spec).Replace("OUTPUTDIR", $OutputDir.Replace("\", "\\")).Replace("WEBSOCKETURL", $ServerUrl).Replace("`"CASES`"", $CasesArray)

    $TempFile = [IO.Path]::GetTempFileName()

    try {
        [IO.File]::WriteAllText($TempFile, $SpecJson)
        $wstestOutput = & wstest -m fuzzingclient -s $TempFile
    } finally {
        if(Test-Path $TempFile) {
            rm $TempFile
        }
    }

    $report = ConvertFrom-Json ([IO.File]::ReadAllText((Convert-Path (Join-Path $OutputDir "index.json"))))

    $report.Server | gm | ? { $_.MemberType -eq "NoteProperty" } | % {
        $case = $report.Server."$($_.Name)"
        Write-Host "[#$($iteration.ToString().PadRight(2))] [$($case.behavior.PadRight(6))] Case $($_.Name)"
    }
}