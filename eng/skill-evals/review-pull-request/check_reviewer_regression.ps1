#requires -Version 7.0
param(
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$cases = Get-Content (Join-Path $PSScriptRoot 'reviewer-regression.cases.json') -Raw |
    ConvertFrom-Json -Depth 100
$labels = Get-Content (Join-Path $PSScriptRoot 'reviewer-regression.labels.json') -Raw |
    ConvertFrom-Json -Depth 100
$errors = [Collections.Generic.List[string]]::new()

function Get-RelativeFiles {
    param([string]$Root)

    return @(
        Get-ChildItem $Root -Recurse -File |
            ForEach-Object {
                [IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\', '/')
            } |
            Sort-Object
    )
}

function Test-FileDescriptor {
    param(
        [string]$Root,
        [string]$FallbackRoot,
        [string]$Name,
        [object]$Descriptor,
        [string]$Description
    )

    $relativePath = if ($Descriptor -is [string]) {
        $Name
    } else {
        [string]$Descriptor.path
    }
    $expectedHash = if ($Descriptor -is [string]) {
        $Descriptor
    } else {
        [string]$Descriptor.sha256
    }
    $path = Join-Path $Root $relativePath.Replace(
        '/',
        [IO.Path]::DirectorySeparatorChar
    )
    if (-not (Test-Path $path -PathType Leaf) -and $FallbackRoot) {
        $path = Join-Path $FallbackRoot $relativePath.Replace(
            '/',
            [IO.Path]::DirectorySeparatorChar
        )
    }
    if (-not (Test-Path $path -PathType Leaf)) {
        $errors.Add("$Description is missing: $relativePath.")
        return
    }

    $file = Get-Item $path
    $hash = (Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant()
    $sizeMatches = $Descriptor -is [string] -or $file.Length -eq [long]$Descriptor.utf8Bytes
    if (-not $sizeMatches -or $hash -ne $expectedHash) {
        $errors.Add("$Description does not match its recorded size or SHA-256: $relativePath.")
    }
}

if (@($cases.cases).Count -ne 49) {
    $errors.Add("Expected 49 corpus rows; found $(@($cases.cases).Count).")
}

$categoryCounts = @{}
foreach ($group in @($labels.rows | Group-Object category)) {
    $categoryCounts[$group.Name] = $group.Count
}
foreach ($entry in @{ GOOD = 29; BAD = 13; OTHER = 7 }.GetEnumerator()) {
    if ($categoryCounts[$entry.Key] -ne $entry.Value) {
        $errors.Add("Expected $($entry.Value) $($entry.Key) rows; found $($categoryCounts[$entry.Key]).")
    }
}

$caseIds = @($cases.cases.id)
$labelIds = @($labels.rows.id)
if (@(Compare-Object $caseIds $labelIds).Count -ne 0) {
    $errors.Add('Corpus and label IDs do not match.')
}
if (@($caseIds | Group-Object | Where-Object Count -gt 1).Count -ne 0) {
    $errors.Add('Corpus IDs are not unique.')
}
foreach ($label in $labels.rows) {
    $case = $cases.cases | Where-Object id -eq $label.id
    if ($label.path -ne $case.path -or [int]$label.line -ne [int]$case.line) {
        $errors.Add("$($label.id) hidden label anchor does not match the corpus.")
    }
}

foreach ($id in 'V21', 'L69') {
    $row = $labels.rows | Where-Object id -eq $id
    $allowedTiers = @($row.allowedTiers)
    if (-not $row.required -or [bool]$row.expectedSurfaced -or
        $allowedTiers.Count -ne 1 -or $allowedTiers[0] -ne 'DISCARDED') {
        $errors.Add("$id must be required, not surfaced, and allowed only as DISCARDED.")
    }
}

$v23 = $labels.rows | Where-Object id -eq 'V23'
if (-not $v23.required -or $v23.expectedSurfaced -ne $true -or
    @(Compare-Object @($v23.allowedTiers) @('VERIFIED', 'QUESTION')).Count -ne 0) {
    $errors.Add('V23 must be required, surfaced, and allowed only as VERIFIED or QUESTION.')
}

$other = @($labels.rows | Where-Object category -eq 'OTHER')
if (@($other | Where-Object { -not $_.reportOnly -or $_.status -ne 'pending-adjudication' }).Count -ne 0) {
    $errors.Add('All OTHER rows must remain pending and report-only.')
}

foreach ($pr in 70106, 69782, 57972) {
    $caseRoot = Join-Path $PSScriptRoot "fixtures/reviewer-regression/cases/pr-$pr"
    $manifest = Join-Path $caseRoot 'bundle/manifest.json'
    if (-not (Test-Path $manifest -PathType Leaf)) {
        $errors.Add("PR $pr is missing review/manifest.json.")
        continue
    }

    $bundleRoot = Join-Path $caseRoot 'bundle'
    $commonRoot = Join-Path $PSScriptRoot 'fixtures/reviewer-regression/common'
    $manifestData = Get-Content $manifest -Raw | ConvertFrom-Json -Depth 100
    if (-not $manifestData.ready -or [int]$manifestData.version -ne 2 -or
        [int]$manifestData.target.pr -ne $pr) {
        $errors.Add("PR $pr does not have a ready-v2 manifest bound to the expected PR.")
    }
    foreach ($role in 'head', 'mergeBase', 'baseTip') {
        $source = $manifestData.sources.$role
        $sourceRoot = Join-Path $bundleRoot ([string]$source.root)
        if (-not (Test-Path $sourceRoot -PathType Container)) {
            $errors.Add("PR $pr is missing its $role source projection.")
            continue
        }
        $sourceFiles = @(Get-RelativeFiles $sourceRoot)
        if ($sourceFiles.Count -ne [int]$source.files) {
            $errors.Add("PR $pr $role source projection count does not match its manifest.")
        }
    }
    foreach ($projection in 'guidance', 'requiredText') {
        $entry = $manifestData.$projection
        $projectionRoots = @(
            @(
                Join-Path $bundleRoot ([string]$entry.root)
                if ($pr -eq 70106) {
                    Join-Path $commonRoot ([string]$entry.root)
                }
            ) | Where-Object { Test-Path $_ -PathType Container }
        )
        if ($projectionRoots.Count -eq 0) {
            $errors.Add("PR $pr is missing its $projection projection.")
            continue
        }
        $projectionFiles = @(
            foreach ($projectionRoot in $projectionRoots) {
                Get-RelativeFiles $projectionRoot
            }
        ) | Sort-Object -Unique
        if ($projectionFiles.Count -ne [int]$entry.files) {
            $errors.Add("PR $pr $projection projection count does not match its manifest.")
        }
    }
    foreach ($artifactProperty in $manifestData.artifacts.PSObject.Properties) {
        Test-FileDescriptor $bundleRoot $null $artifactProperty.Name $artifactProperty.Value "PR $pr artifact"
    }
    $assignmentFallback = if ($pr -eq 70106) { $commonRoot } else { $null }
    foreach ($guide in @($manifestData.guides)) {
        foreach ($descriptor in @($guide.assignment.files)) {
            Test-FileDescriptor $bundleRoot $assignmentFallback ([string]$descriptor.path) $descriptor "PR $pr assignment"
        }
    }
}

foreach ($pr in 70106, 69782) {
    $candidates = Join-Path $PSScriptRoot "fixtures/reviewer-regression/cases/pr-$pr/candidates.json"
    if (-not (Test-Path $candidates -PathType Leaf)) {
        $errors.Add("PR $pr is missing candidates.json.")
    }
}

$controls = @(Get-Content (Join-Path $PSScriptRoot 'reviewer-regression-controls.json') -Raw |
    ConvertFrom-Json -Depth 20)
$controlTrajectories = @(
    Get-Content (Join-Path $PSScriptRoot 'reviewer-regression-control-trajectories.jsonl') |
        Where-Object { $_.Trim() } |
        ForEach-Object { $_ | ConvertFrom-Json -Depth 100 }
)
if ($controls.Count -ne 5 -or $controlTrajectories.Count -ne 5) {
    $errors.Add('Expected exactly five judge-development controls and trajectories.')
}
$trajectoryIds = @($controlTrajectories.trajectory.id -replace '^control-', '')
if (@(Compare-Object @($controls.id) $trajectoryIds).Count -ne 0) {
    $errors.Add('Judge-development control IDs do not match their trajectories.')
}

$fixtureRoot = Join-Path $PSScriptRoot 'fixtures/reviewer-regression'
$fixtureFiles = @(Get-ChildItem $fixtureRoot -Recurse -File)
$fixtureBytes = ($fixtureFiles | Measure-Object Length -Sum).Sum
$sizeBudget = 2MB
if ($fixtureBytes -gt $sizeBudget) {
    $errors.Add("Fixture size $fixtureBytes exceeds the $sizeBudget-byte first-PR budget.")
}

try {
    & (Join-Path $PSScriptRoot 'generate_reviewer_regression.ps1') -Check | Out-Null
} catch {
    $errors.Add($_.Exception.Message)
}

if ($errors.Count -gt 0) {
    throw "Reviewer regression check failed:`n  - $($errors -join "`n  - ")"
}

if (-not $Quiet) {
    Write-Host "Reviewer regression check passed: 49 rows, $($fixtureFiles.Count) fixture files, $fixtureBytes bytes."
}
