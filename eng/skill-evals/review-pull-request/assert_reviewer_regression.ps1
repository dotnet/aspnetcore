#requires -Version 7.0
param(
    [Parameter(Mandatory)]
    [string]$Results,

    [string]$BaselineVariant = 'current',

    [string]$CandidateVariant = 'candidate'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$files = @(
    if (Test-Path $Results -PathType Container) {
        Get-ChildItem $Results -Recurse -File -Filter '*.jsonl'
    } else {
        Get-Item $Results
    }
)
if ($files.Count -eq 0) {
    throw "No JSONL results found under $Results."
}

$allRecords = @(
    foreach ($file in $files) {
        Get-Content $file.FullName | Where-Object { $_.Trim() } | ForEach-Object {
            $_ | ConvertFrom-Json -Depth 100
        }
    }
)
$records = @($allRecords | Where-Object type -eq 'trial-result')
if ($records.Count -eq 0) {
    throw 'The result set contains no trial-result records.'
}

$absoluteGateErrors = [Collections.Generic.List[string]]::new()
$comparisonErrors = [Collections.Generic.List[string]]::new()
$semanticWarnings = [Collections.Generic.List[string]]::new()
$requiredStimuli = @('reviewer-regression-pr-70106', 'reviewer-regression-pr-69782')

function Test-IsDeterministicGrader {
    param([object]$Detail)

    $graderType = $Detail.PSObject.Properties['graderType']
    return ($null -ne $graderType -and $graderType.Value -eq 'program') -or
        $Detail.name -eq 'reviewer-regression'
}

function Test-IsPromptGrader {
    param([object]$Detail)

    $graderType = $Detail.PSObject.Properties['graderType']
    return $null -ne $graderType -and $graderType.Value -eq 'prompt'
}

function Get-DeterministicGrader {
    param([object]$Row)

    if ($null -eq $Row.gradeResult) {
        return $null
    }

    $matches = @($Row.gradeResult.details | Where-Object {
        Test-IsDeterministicGrader $_
    })
    if ($matches.Count -ne 1) {
        return $null
    }

    return $matches[0]
}

function Test-DetailPassed {
    param(
        [object]$Deterministic,
        [string]$Name
    )

    $detail = @($Deterministic.details | Where-Object name -eq $Name)
    return $detail.Count -eq 1 -and [bool]$detail[0].passed
}

function Get-DeterministicPassCount {
    param([object[]]$Rows)

    return @($Rows | Where-Object {
        $deterministic = Get-DeterministicGrader $_
        $null -ne $deterministic -and [bool]$deterministic.passed
    }).Count
}

function Get-CasePassCount {
    param(
        [object[]]$Rows,
        [string]$CaseId
    )

    return @($Rows | Where-Object {
        $deterministic = Get-DeterministicGrader $_
        $null -ne $deterministic -and
            (Test-DetailPassed $deterministic "acceptance/$CaseId")
    }).Count
}

foreach ($variant in $BaselineVariant, $CandidateVariant) {
    foreach ($stimulus in $requiredStimuli) {
        $rows = @($records | Where-Object {
            $_.variant -eq $variant -and $_.stimulus -eq $stimulus
        })
        if ($rows.Count -ne 5) {
            $absoluteGateErrors.Add("$variant/$stimulus expected 5 rows; found $($rows.Count).")
            continue
        }

        $semanticPasses = 0
        $semanticCompleted = 0
        foreach ($row in $rows) {
            if ($row.status -ne 'success') {
                $absoluteGateErrors.Add("$variant/$stimulus has non-success status '$($row.status)'.")
                continue
            }
            if ($null -eq $row.gradeResult) {
                $absoluteGateErrors.Add("$variant/$stimulus is missing its grade result.")
                continue
            }
            $gradeStatus = $row.gradeResult.PSObject.Properties['status']
            if ($null -ne $gradeStatus -and $gradeStatus.Value -eq 'error') {
                $absoluteGateErrors.Add("$variant/$stimulus has a grader error.")
                continue
            }

            $deterministic = Get-DeterministicGrader $row
            if ($null -eq $deterministic) {
                $absoluteGateErrors.Add("$variant/$stimulus is missing the deterministic grader result.")
                continue
            }
            $graderStatus = $deterministic.PSObject.Properties['status']
            if ($null -ne $graderStatus -and $graderStatus.Value -eq 'error') {
                $absoluteGateErrors.Add(
                    "$variant/$stimulus deterministic grader errored: $($deterministic.evidence)"
                )
                continue
            }

            $integrity = $deterministic.metadata.integrity
            $invalidNovel = @($deterministic.metadata.invalidNovelFindingIndexes)
            if (@($integrity.failedIds).Count -gt 0 -or
                @($integrity.missingIds).Count -gt 0 -or
                @($integrity.duplicateIds).Count -gt 0 -or
                @($integrity.unexpectedIds).Count -gt 0 -or
                $invalidNovel.Count -gt 0) {
                $absoluteGateErrors.Add(
                    "$variant/$stimulus has a deterministic integrity or result-contract failure: $($deterministic.evidence)"
                )
            }
            if ($deterministic.metadata.adjudicationPending) {
                $absoluteGateErrors.Add("$variant/$stimulus requires manual adjudication.")
            }
            if ($variant -eq $CandidateVariant -and
                $stimulus -eq 'reviewer-regression-pr-70106' -and
                -not $deterministic.passed) {
                $absoluteGateErrors.Add(
                    "$variant/$stimulus failed the required restraint gate: $($deterministic.evidence)"
                )
            }

            foreach ($prompt in @($row.gradeResult.details | Where-Object {
                Test-IsPromptGrader $_
            })) {
                $promptStatus = $prompt.PSObject.Properties['status']
                if ($null -ne $promptStatus -and $promptStatus.Value -eq 'error') {
                    $semanticWarnings.Add("$variant/$stimulus prompt judge errored: $($prompt.evidence)")
                    continue
                }

                $semanticCompleted++
                if ([double]$prompt.score -gt 0) {
                    $semanticPasses++
                }
            }
        }

        Write-Host (
            "Semantic secondary evidence for ${variant}/${stimulus}: " +
            "$semanticPasses/$semanticCompleted completed prompt scores were greater than 0."
        )
    }
}

$comparisonRows = [Collections.Generic.List[object]]::new()
foreach ($stimulus in $requiredStimuli) {
    $baseline = @($records | Where-Object {
        $_.variant -eq $BaselineVariant -and $_.stimulus -eq $stimulus
    })
    $candidate = @($records | Where-Object {
        $_.variant -eq $CandidateVariant -and $_.stimulus -eq $stimulus
    })
    if ($baseline.Count -ne 5 -or $candidate.Count -ne 5) {
        continue
    }

    if ($stimulus -eq 'reviewer-regression-pr-69782') {
        $metric = 'acceptance/V23'
        $baselinePasses = Get-CasePassCount $baseline 'V23'
        $candidatePasses = Get-CasePassCount $candidate 'V23'
    } else {
        $metric = 'deterministic trial'
        $baselinePasses = Get-DeterministicPassCount $baseline
        $candidatePasses = Get-DeterministicPassCount $candidate
    }

    $comparisonRows.Add([pscustomobject]@{
        stimulus = $stimulus
        metric = $metric
        baselinePasses = $baselinePasses
        candidatePasses = $candidatePasses
    })
    if ($candidatePasses -lt $baselinePasses) {
        $comparisonErrors.Add(
            "$stimulus candidate $metric passed $candidatePasses/5 versus baseline $baselinePasses/5."
        )
    }
}

$comparisonVerdict = if ($comparisonRows.Count -ne $requiredStimuli.Count) {
    'INDETERMINATE'
} elseif ($comparisonErrors.Count -gt 0) {
    'REGRESSION'
} elseif (@($comparisonRows | Where-Object {
    $_.candidatePasses -gt $_.baselinePasses
}).Count -gt 0) {
    'NO_REGRESSION'
} else {
    'INDETERMINATE'
}

Write-Host "Reviewer regression comparison verdict: $comparisonVerdict"
foreach ($comparison in $comparisonRows) {
    Write-Host (
        "  $($comparison.stimulus) [$($comparison.metric)]: " +
        "candidate $($comparison.candidatePasses)/5; baseline $($comparison.baselinePasses)/5."
    )
}
foreach ($warning in $semanticWarnings) {
    Write-Warning $warning
}

if ($comparisonVerdict -eq 'REGRESSION') {
    throw "REGRESSION:`n  - $($comparisonErrors -join "`n  - ")"
}
if ($absoluteGateErrors.Count -gt 0) {
    throw "ABSOLUTE_GATE_FAIL:`n  - $($absoluteGateErrors -join "`n  - ")"
}

if ($comparisonVerdict -eq 'INDETERMINATE') {
    Write-Warning 'INDETERMINATE: candidate and baseline are equal on every directional comparison.'
}
Write-Host 'Reviewer regression assertion passed.'
