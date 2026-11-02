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

$errors = [Collections.Generic.List[string]]::new()
$indeterminate = [Collections.Generic.List[string]]::new()
$requiredStimuli = @('reviewer-regression-pr-70106', 'reviewer-regression-pr-69782')

function Test-IsDeterministicGrader {
    param([object]$Detail)

    $graderType = $Detail.PSObject.Properties['graderType']
    return ($null -ne $graderType -and $graderType.Value -eq 'program') -or
        $Detail.name -eq 'reviewer-regression'
}

function Get-DeterministicPassCount {
    param([object[]]$Rows)

    return @($Rows | Where-Object {
        if ($null -eq $_.gradeResult) {
            return $false
        }
        $matches = @($_.gradeResult.details | Where-Object {
            (Test-IsDeterministicGrader $_) -and $_.passed
        })
        return $matches.Count -eq 1
    }).Count
}

foreach ($variant in $BaselineVariant, $CandidateVariant) {
    foreach ($stimulus in $requiredStimuli) {
        $rows = @($records | Where-Object {
            $_.variant -eq $variant -and $_.stimulus -eq $stimulus
        })
        if ($rows.Count -ne 5) {
            $errors.Add("$variant/$stimulus expected 5 rows; found $($rows.Count).")
            continue
        }

        foreach ($row in $rows) {
            if ($row.status -ne 'success') {
                $errors.Add("$variant/$stimulus has non-success status '$($row.status)'.")
                continue
            }
            if ($null -eq $row.gradeResult) {
                $errors.Add("$variant/$stimulus is missing its grade result.")
                continue
            }
            $gradeStatus = $row.gradeResult.PSObject.Properties['status']
            if ($null -ne $gradeStatus -and $gradeStatus.Value -eq 'error') {
                $errors.Add("$variant/$stimulus has a grader error.")
                continue
            }
            $detailErrors = @($row.gradeResult.details | Where-Object {
                $detailStatus = $_.PSObject.Properties['status']
                $null -ne $detailStatus -and $detailStatus.Value -eq 'error'
            })
            if ($detailErrors.Count -gt 0) {
                $errors.Add("$variant/$stimulus has one or more grader-detail errors.")
                continue
            }
            $deterministic = @($row.gradeResult.details | Where-Object {
                Test-IsDeterministicGrader $_
            })
            if ($deterministic.Count -ne 1) {
                $errors.Add("$variant/$stimulus is missing the deterministic grader result.")
                continue
            }
            $graderStatus = $deterministic[0].PSObject.Properties['status']
            if ($null -ne $graderStatus -and $graderStatus.Value -eq 'error') {
                $errors.Add("$variant/$stimulus deterministic grader errored: $($deterministic[0].evidence)")
            }
            if ($variant -eq $CandidateVariant -and -not $deterministic[0].passed) {
                $errors.Add("$variant/$stimulus deterministic grader failed: $($deterministic[0].evidence)")
            }
            if ($deterministic[0].metadata.adjudicationPending) {
                $errors.Add("$variant/$stimulus requires manual adjudication.")
            }
        }
    }
}

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

    $baselinePasses = Get-DeterministicPassCount $baseline
    $candidatePasses = Get-DeterministicPassCount $candidate

    if ($candidatePasses -lt $baselinePasses) {
        $errors.Add("$stimulus candidate passed $candidatePasses/5 versus baseline $baselinePasses/5.")
    } elseif ($candidatePasses -eq $baselinePasses) {
        $indeterminate.Add("$stimulus candidate and baseline both passed $candidatePasses/5.")
    }
}

if ($errors.Count -gt 0) {
    throw "Reviewer regression assertion failed:`n  - $($errors -join "`n  - ")"
}

if ($indeterminate.Count -gt 0) {
    Write-Warning "INDETERMINATE (advisory until null calibration):`n  - $($indeterminate -join "`n  - ")"
}
Write-Host 'Reviewer regression assertion passed.'
