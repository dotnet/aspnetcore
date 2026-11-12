#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$suiteRoot = (Resolve-Path (Join-Path $PSScriptRoot '../review-pull-request')).Path
$specPath = Join-Path $suiteRoot 'eval.vally.yaml'
$json = Get-Content $specPath -Raw | & npx `
    --yes `
    --registry=https://packagefeedproxy.microsoft.io/npm/ `
    'yaml@2.9.1' --json --single --strict
if ($LASTEXITCODE -ne 0) {
    throw 'Could not parse the review fixture specification.'
}
$spec = $json | ConvertFrom-Json -AsHashtable -Depth 100
$script:checks = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)

    $script:checks++
    if (-not $Condition) {
        throw $Message
    }
}

function Test-FileHash {
    param([string]$Path, [string]$Hash)

    return (Get-FileHash $Path -Algorithm SHA256).Hash -eq $Hash
}

Assert-True ($spec.stimuli.Count -eq 6) 'The suite must retain five PR cases and one catch-policy case.'
Assert-True (-not (Test-Path (Join-Path $suiteRoot 'catch-policy.vally.yaml'))) (
    'The superseded standalone catch-policy specification remains.'
)
Assert-True (-not (Test-Path (Join-Path $suiteRoot 'fixtures/catch-policy'))) (
    'The superseded standalone catch-policy fixtures remain.'
)
Assert-True (
    @(Get-ChildItem (Join-Path $suiteRoot 'fixtures') -Recurse -File -Filter '*.txt').Count -eq 0
) 'The synthetic text fixtures remain.'

$supportFiles = @($spec.agent_environment.files)
Assert-True (
    @($supportFiles | Where-Object { $_.src -match 'SKILL\.md$|\.agent\.md$' }).Count -eq 0
) 'The standard suite stages treatment instructions into its baseline.'
Assert-True ($spec.scoring.weights.program -eq 1) 'Disposition is not the primary deterministic gate.'
foreach ($key in 'skill-invocation', 'tool-calls', 'prompt') {
    Assert-True ($spec.scoring.weights[$key] -eq 0) "The $key grader confounds disposition quality."
}

foreach ($stimulus in $spec.stimuli) {
    $caseRoot = Join-Path $suiteRoot "fixtures/$($stimulus.name)"
    $provenance = Get-Content (Join-Path $caseRoot 'provenance.json') -Raw |
        ConvertFrom-Json -AsHashtable -Depth 100
    Assert-True ($stimulus.name -match '^case-\d\d$') 'A stimulus name reveals its source or answer.'
    Assert-True ($provenance.repository -eq 'dotnet/aspnetcore') 'A fixture comes from another repository.'
    if ($stimulus.name -ne 'case-06') {
        Assert-True (
            @($provenance.sourceRoles.Keys | Sort-Object) -join ',' -eq 'baseTip,head,mergeBase'
        ) 'A frozen source role is missing.'
    } else {
        Assert-True ($provenance.fixtureKind -like 'synthetic public*') 'The catch example is misrepresented as production source.'
        foreach ($caseFile in Get-ChildItem $caseRoot -File) {
            $text = Get-Content $caseFile.FullName -Raw
            Assert-True (
                $text -notmatch 'No target-base policy|No additional primary contract|unspecified\.txt|suppression-policy absence'
            ) "The catch case file $($caseFile.Name) includes an answer hint."
        }
    }

    foreach ($file in $provenance.files) {
        $path = Join-Path $caseRoot $file.file
        Assert-True (Test-FileHash $path $file.sha256) "Frozen fixture hash mismatch: $path."
        if ($file.ContainsKey('sources')) {
            foreach ($source in $file.sources) {
                Assert-True (
                    $source.commit -eq $provenance.sourceRoles[$source.role]
                ) "Source role mismatch: $path."
                Assert-True (
                    $source.blob -match '^[0-9a-f]{40}$' -and $source.ranges.Count -gt 0
                ) "Source blob or ranges are missing: $path."
            }
        }
    }

    $trialFiles = @($supportFiles) + @($stimulus.agent_environment.files)
    $requiredReads = @(
        $stimulus.graders |
            Where-Object { $_.type -eq 'tool-calls' } |
            ForEach-Object { $_.config.required }
    )
    foreach ($file in $trialFiles) {
        $path = Join-Path $suiteRoot $file.src
        $content = Get-Content $path -Raw
        $matchingReads = @($requiredReads | Where-Object {
            $file.dest -match $_.pattern -and $content -match $_.result
        })
        Assert-True ($matchingReads.Count -gt 0) "No actual-byte read guard covers $($stimulus.name)/$($file.dest)."
    }
    Assert-True (
        @($requiredReads | Where-Object {
            $_.pattern -eq 'review-pull-request' -and $_.result -eq 'Return result; never publish'
        }).Count -eq 1
    ) 'The skill acquisition needs one real-content receipt, not two conjunctive tool choices.'
}

$sampleRoot = Join-Path $suiteRoot 'fixtures/case-01'
$sample = Get-Content (Join-Path $sampleRoot 'provenance.json') -Raw |
    ConvertFrom-Json -AsHashtable -Depth 100
$file = $sample.files[0]
Assert-True (
    -not (Test-FileHash (Join-Path $sampleRoot $file.file) ('0' * 64))
) 'A wrong snapshot hash passes the integrity check.'

$repoRoot = (Resolve-Path (Join-Path $suiteRoot '../../..')).Path
$evalRoot = Split-Path $suiteRoot -Parent
foreach ($experimentName in 'review-vs-baseline', 'review-smoke') {
    $experimentText = Get-Content (Join-Path $evalRoot "$experimentName.experiment.yaml") -Raw |
        & npx --yes --registry=https://packagefeedproxy.microsoft.io/npm/ 'yaml@2.9.1' --json --single --strict
    if ($LASTEXITCODE -ne 0) {
        throw "Could not parse $experimentName."
    }
    $experiment = $experimentText | ConvertFrom-Json -AsHashtable -Depth 100
    $baseline = $experiment.variants.baseline.environment
    $skilled = $experiment.variants.skilled.environment
    Assert-True ($baseline.skills.Count -eq 0 -and -not $baseline.ContainsKey('files')) (
        'Baseline receives additional treatment files.'
    )
    Assert-True ($skilled.skills.Count -eq 1) 'The skilled variant lacks its skill.'
    Assert-True (@($skilled.files | Where-Object dest -like '*.agent.md').Count -eq 2) (
        'Only the skilled variant must receive both reviewer profiles.'
    )
}

$componentRoot = Join-Path $evalRoot 'review-candidate-verifier'
$componentText = Get-Content (Join-Path $componentRoot 'eval.vally.yaml') -Raw |
    & npx --yes --registry=https://packagefeedproxy.microsoft.io/npm/ 'yaml@2.9.1' --json --single --strict
if ($LASTEXITCODE -ne 0) {
    throw 'Could not parse verifier component specification.'
}
$component = $componentText | ConvertFrom-Json -AsHashtable -Depth 100
Assert-True (($component.stimuli.name -join ',') -eq 'a,b,c') 'Verifier component cases must be exactly a, b, and c.'
Assert-True ($component.defaults.runs -eq 5 -and $component.scoring.threshold -eq 0.8) 'The component gate must require 4/5.'
$componentProvenance = Get-Content (Join-Path $componentRoot 'provenance.json') -Raw |
    ConvertFrom-Json -AsHashtable -Depth 100
Assert-True ($componentProvenance.sourceRevisions.head -eq 'c0f86a6d725473f7a6f4c56d816ef941591053d2') (
    'The component source does not use the pinned head.'
)
foreach ($file in $componentProvenance.files) {
    Assert-True (Test-FileHash (Join-Path $componentRoot "fixtures/$($file.file)") $file.sha256) (
        "Component fixture hash mismatch: $($file.file)."
    )
}
foreach ($stimulus in $component.stimuli) {
    foreach ($file in @($component.agent_environment.files) + @($stimulus.agent_environment.files)) {
        Assert-True (Test-Path (Join-Path $componentRoot $file.src)) "Missing component input: $($file.src)."
    }
}
$checkoutRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-review-fixture-checkout-' + [guid]::NewGuid().ToString('N')
)
New-Item -ItemType Directory -Path $checkoutRoot | Out-Null
try {
    $fixturePaths = @(
        Get-ChildItem (Join-Path $suiteRoot 'fixtures') -Recurse -File -Force |
            ForEach-Object { [IO.Path]::GetRelativePath($repoRoot, $_.FullName).Replace('\', '/') }
    )
    & git -C $repoRoot -c core.autocrlf=true checkout-index `
        "--prefix=$checkoutRoot$([IO.Path]::DirectorySeparatorChar)" -- @fixturePaths
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not materialize the tracked fixture files through Git.'
    }
    foreach ($stimulus in $spec.stimuli) {
        $relativeRoot = "eng/skill-evals/review-pull-request/fixtures/$($stimulus.name)"
        $checkedOutRoot = Join-Path $checkoutRoot $relativeRoot
        $provenance = Get-Content (Join-Path $checkedOutRoot 'provenance.json') -Raw |
            ConvertFrom-Json -AsHashtable -Depth 100
        foreach ($file in $provenance.files) {
            Assert-True (
                Test-FileHash (Join-Path $checkedOutRoot $file.file) $file.sha256
            ) "Git checkout changed the frozen bytes of $relativeRoot/$($file.file)."
        }
    }
    $componentRelative = 'eng/skill-evals/review-candidate-verifier/fixtures'
    $componentPaths = @(
        Get-ChildItem (Join-Path $componentRoot 'fixtures') -Recurse -File -Force |
            ForEach-Object { [IO.Path]::GetRelativePath($repoRoot, $_.FullName).Replace('\', '/') }
    )
    & git -C $repoRoot -c core.autocrlf=true checkout-index `
        "--prefix=$checkoutRoot$([IO.Path]::DirectorySeparatorChar)" -- @componentPaths
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not materialize component fixtures through Git.'
    }
    foreach ($file in $componentProvenance.files) {
        Assert-True (
            Test-FileHash (Join-Path $checkoutRoot "$componentRelative/$($file.file)") $file.sha256
        ) "Git checkout changed component fixture bytes: $($file.file)."
    }
} finally {
    Remove-Item -LiteralPath $checkoutRoot -Recurse -Force
}

Write-Host "Review fixture self-test: $script:checks ran, $script:checks passed, 0 failed."
