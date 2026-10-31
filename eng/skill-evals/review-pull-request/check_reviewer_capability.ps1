#requires -Version 7.0
param(
    [string]$FixtureRoot = (Join-Path $PSScriptRoot 'fixtures/reviewer-capability'),

    [string]$LabelsPath = (Join-Path $PSScriptRoot 'reviewer-capability-labels.json'),

    [string]$HistoricalLabelsPath,

    [string]$ControlsPath = (Join-Path $PSScriptRoot 'reviewer-capability-controls.json'),

    [string]$ControlTrajectoriesPath = (
        Join-Path $PSScriptRoot 'reviewer-capability-control-trajectories.jsonl'
    ),

    [string]$SpecPath = (Join-Path $PSScriptRoot 'reviewer-capability.vally.yaml'),

    [string]$ExpectedEvalFilePath,

    [string]$ResultsDirectory,

    [string]$GradeResultsDirectory,

    [string]$ControlResultsDirectory,

    [string[]]$ExpectedControlId,

    [string]$ExpectedStimulus,

    [string]$ExpectedRunId,

    [ValidateSet('Integrity', 'Acceptance')]
    [string]$ResultMode = 'Integrity'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$criteriaCommit = '4f556f73a610b386e5ec29355a300e493d30c4c5'
$baselineReviewSkillBlob = 'b11fe9fe79b5d848687ba1c93362f4aaf22bfe53'
$baselineNativeReviewSkillBodyBytes = 18527
$baselineNativeReviewSkillBodyHash = '3a38f1385e455e104ebec8bffe280b6b90f6ef08f43131d4fe559c61d3ce1225'
$treatmentReviewSkillBlob = 'a8c9856cd260ad22938a2e1bfe638294abe635f3'
$treatmentNativeReviewSkillBodyBytes = 19704
$treatmentNativeReviewSkillBodyHash = 'f6f5c4106bfcc93c71f2bb99ba04b66098cef923f2d8c115d6fe186e0aa6ef14'
$treatmentReferenceBlob = 'f9fa346b2001a5cb551748a369c7fd78d8ca22a7'
$treatmentReferenceBytes = 2321
$treatmentReferenceHash = 'b0d17d576f8d6d5854f3c0bfb81fb65ae0ceea648cd9bff9a117b530c37ce08c'
$treatmentRubricHash = '9315527697ebb40bc9073d2639128301a6308faf011fd798e72c243f14be109d'
$treatmentReferenceNormalizedBytes = 2285
$treatmentReferenceNormalizedHash = 'a81c8f5ea560a3247b91f1362839e580bdf58601c9a046e37f4233f370307f21'
$treatmentRubricNormalizedHash = '057d3a6ae9001f16eafa421a51274c19a4de4393cec8b236747d86e8006612f8'
$expectedEvalName = 'review-pull-request-capability-development'
$expectedModel = 'gpt-5.6-sol'
$expectedJudge = 'gemini-4-argon'
$expectedSpecSha256 = 'a233bf722ee41628ca6a1c4f7aca10c4d38f223309b26fe47235ecce9e4e1467'
$packagedTreatmentSpecSha256 = '7ae60e45840bee76546f113ef6550696f42d17e56c0b44f448791c8bde961396'
$measuredSpecSha256 = '1f20439b7579ff1b403570cec81592ea9b3c82afe50b06b6d3048ee1cbcdda9f'
$baselineComparisonSpecSha256 = 'eeac867b74835283dd82ae94a4e7ea77ed0c50a613f3dc2a55b96b7227e89e32'
$treatmentComparisonSpecSha256 = '60e6ce10db6a97dca3206f7f7ac8cd2c134f8b16269362f4d733322ea838052e'
$expectedPromptSha256 = '2ddc25b8b9224ec179498e821056dbed58cbf8fe2cf6d67abe0aef0d45a3aec3'
$cases = [ordered]@{
    'pr-70106' = @{
        Stimulus = 'reviewer-capability-pr-70106'
        Grader = 'semantic-review-pr-70106'
        Pilot = $true
        UseCommonOverlay = $true
        GuideCount = 2
        SuiteRole = 'primary'
        Pull = 70106
        Head = '2edf3761692afe15475075eb2fc0088566162045'
        MergeBase = '10bc886bfa321718276619d1077cc14eaf48456f'
        BaseTip = '4f556f73a610b386e5ec29355a300e493d30c4c5'
        FeedbackMode = 'historical-feedback-cutoff'
        Cutoff = '2026-10-31T12:50:58Z'
        ExcludedFeedbackId = 4431751648
    }
    'pr-57972' = @{
        Stimulus = 'reviewer-capability-pr-57972'
        Grader = 'semantic-review-pr-57972'
        Pilot = $true
        UseCommonOverlay = $false
        GuideCount = 1
        SuiteRole = 'primary'
        Pull = 57972
        Head = '0a8373f036c151ae2773f4e5a1cb4aeccade6bce'
        MergeBase = '542f2b9877da67e90cca6ce1b65aa9557fbb2aa8'
        BaseTip = '542f2b9877da67e90cca6ce1b65aa9557fbb2aa8'
        FeedbackMode = 'historical-feedback-cutoff'
        Cutoff = '2024-09-19T20:14:43Z'
        ExcludedFeedbackId = 1767539860
    }
}

function Assert-Condition {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-Sha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Get-FileHash -Algorithm SHA256 $Path).Hash.ToLowerInvariant()
}

function Get-TextSha256 {
    param([Parameter(Mandatory = $true)][string]$Text)

    $bytes = [Text.Encoding]::UTF8.GetBytes($Text)
    return [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData($bytes)
    ).ToLowerInvariant()
}

function Get-NormalizedText {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Get-Content $Path -Raw).Replace("`r`n", "`n").Replace("`r", "`n")
}

function Get-NativeSkillBody {
    param([Parameter(Mandatory = $true)][string]$Path)

    $text = Get-NormalizedText $Path
    return [regex]::Replace(
        $text,
        '\A---\n.*?\n---\n+',
        '',
        [Text.RegularExpressions.RegexOptions]::Singleline
    )
}

function Test-NumericValue {
    param($Value)

    return $null -ne $Value -and $Value.GetType().Name -in @(
        'Byte',
        'SByte',
        'Int16',
        'UInt16',
        'Int32',
        'UInt32',
        'Int64',
        'UInt64',
        'Single',
        'Double',
        'Decimal'
    )
}

function Assert-FileReference {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)]$Reference,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $path = Join-Path $Root ([string]$Reference.path)
    Assert-Condition (Test-Path $path -PathType Leaf) "Missing $Description at '$path'."
    Assert-Condition ((Get-Item $path).Length -eq [long]$Reference.utf8Bytes) (
        "$Description has the wrong byte count: $($Reference.path)."
    )
    Assert-Condition ((Get-Sha256 $path) -eq [string]$Reference.sha256) (
        "$Description has the wrong SHA-256: $($Reference.path)."
    )
}

function Assert-OverlayReference {
    param(
        [Parameter(Mandatory = $true)][string]$BundleRoot,
        [Parameter(Mandatory = $true)]$Reference,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $relative = ([string]$Reference.path).Replace('\', '/')
    $path = Join-Path $BundleRoot $relative
    if (-not (Test-Path $path -PathType Leaf)) {
        if ($relative.StartsWith('guidance/', [StringComparison]::Ordinal)) {
            $path = Join-Path (
                Join-Path $FixtureRoot 'common/guidance'
            ) $relative.Substring('guidance/'.Length)
        } elseif ($relative.StartsWith('required/', [StringComparison]::Ordinal)) {
            $path = Join-Path (
                Join-Path $FixtureRoot 'common/required'
            ) $relative.Substring('required/'.Length)
        }
    }
    Assert-Condition (Test-Path $path -PathType Leaf) "Missing $Description at '$path'."
    Assert-Condition ((Get-Item $path).Length -eq [long]$Reference.utf8Bytes) (
        "$Description has the wrong byte count: $($Reference.path)."
    )
    Assert-Condition ((Get-Sha256 $path) -eq [string]$Reference.sha256) (
        "$Description has the wrong SHA-256: $($Reference.path)."
    )
}

function Get-DirectoryDigest {
    param([Parameter(Mandatory = $true)][string]$Root)

    $records = @(
        foreach ($file in Get-ChildItem $Root -Recurse -File) {
        $relative = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
            "$relative`0$(Get-Sha256 $file.FullName)`n"
        }
    )
    [Array]::Sort($records, [StringComparer]::Ordinal)
    $text = $records -join ''
    return @{
        Files = @($records).Count
        Sha256 = Get-TextSha256 $text
    }
}

function Get-OverlayDirectoryDigest {
    param([Parameter(Mandatory = $true)][string[]]$Roots)

    $files = [Collections.Generic.Dictionary[string, string]]::new(
        [StringComparer]::Ordinal
    )
    foreach ($root in $Roots) {
        foreach ($file in Get-ChildItem $root -Recurse -File) {
            $relative = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
            $files[$relative] = Get-Sha256 $file.FullName
        }
    }
    $names = @($files.Keys)
    [Array]::Sort($names, [StringComparer]::Ordinal)
    $text = ($names | ForEach-Object { "$_`0$($files[$_])`n" }) -join ''
    return @{
        Files = $names.Count
        Sha256 = Get-TextSha256 $text
    }
}

function Assert-OverlayFilesMatch {
    param(
        [Parameter(Mandatory = $true)][string]$CommonRoot,
        [Parameter(Mandatory = $true)][string]$CaseRoot,
        [Parameter(Mandatory = $true)][string]$Description
    )

    if (-not (Test-Path $CaseRoot -PathType Container)) {
        return
    }
    foreach ($caseFile in Get-ChildItem $CaseRoot -Recurse -File) {
        $relative = [IO.Path]::GetRelativePath(
            $CaseRoot,
            $caseFile.FullName
        ).Replace('\', '/')
        $commonFile = Join-Path $CommonRoot $relative
        if (Test-Path $commonFile -PathType Leaf) {
            Assert-Condition ((Get-Sha256 $caseFile.FullName) -eq (Get-Sha256 $commonFile)) (
                "$Description overlays different bytes at '$relative'."
            )
        }
    }
}

function Get-GitBlob {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [switch]$ApplyAttributes
    )

    $relative = [IO.Path]::GetRelativePath($repoRoot, (Resolve-Path $Path).Path).Replace('\', '/')
    $arguments = @('-C', $repoRoot, 'hash-object')
    if ($ApplyAttributes) {
        $arguments += "--path=$relative"
    } else {
        $arguments += '--no-filters'
    }
    $arguments += @('--', $relative)
    $output = & git @arguments 2>&1
    Assert-Condition ($LASTEXITCODE -eq 0) "git hash-object failed for '$relative': $output"
    return ([string]$output).Trim()
}

function Get-EventTime {
    param(
        [Parameter(Mandatory = $true)]$Item,
        [Parameter(Mandatory = $true)][string]$Category
    )

    $value = if ($Category -eq 'reviews') {
        $Item.submittedAt
    } else {
        $Item.createdAt
    }
    Assert-Condition ($null -ne $value -and -not [string]::IsNullOrWhiteSpace([string]$value)) (
        "A $Category feedback item omits its timestamp."
    )
    return [DateTimeOffset]$value
}

function Assert-OutputReceipt {
    param(
        [Parameter(Mandatory = $true)][string]$Output,
        [Parameter(Mandatory = $true)]$Expected,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $receipts = [ordered]@{
        PR = [string]$Expected.Pull
        HEAD_SHA = [string]$Expected.Head
        MERGE_BASE_SHA = [string]$Expected.MergeBase
        BASE_TIP_SHA = [string]$Expected.BaseTip
        PATH = 'single-reviewer'
    }
    foreach ($receipt in $receipts.GetEnumerator()) {
        $valuePattern = if ($receipt.Key -eq 'PR') {
            '(?:dotnet/aspnetcore#)?' +
                [regex]::Escape([string]$receipt.Value)
        } else {
            [regex]::Escape([string]$receipt.Value)
        }
        Assert-Condition (
            $Output -match (
                '(?im)^\s*' +
                [regex]::Escape([string]$receipt.Key) +
                ':\s*' +
                $valuePattern +
                '\s*$'
            )
        ) "$Description has the wrong $($receipt.Key) receipt."
    }
}

$skillPath = Join-Path $repoRoot '.github/skills/review-pull-request/SKILL.md'
Assert-Condition ((Get-GitBlob $skillPath -ApplyAttributes) -eq $baselineReviewSkillBlob) (
    "The default reviewer criteria source is not frozen blob $baselineReviewSkillBlob."
)
$baselineNativeBody = Get-NativeSkillBody $skillPath
Assert-Condition (
    [Text.Encoding]::UTF8.GetByteCount($baselineNativeBody) -eq
        $baselineNativeReviewSkillBodyBytes -and
    (Get-TextSha256 $baselineNativeBody) -eq $baselineNativeReviewSkillBodyHash
) 'The default reviewer native body does not match its frozen baseline identity.'

$treatmentRoot = Join-Path (
    $PSScriptRoot
) 'treatments/claim-validation/review-pull-request'
$treatmentSkillPath = Join-Path $treatmentRoot 'SKILL.md'
$treatmentNativeBody = Get-NativeSkillBody $treatmentSkillPath
Assert-Condition (
    (Get-GitBlob $treatmentSkillPath -ApplyAttributes) -eq $treatmentReviewSkillBlob -and
    [Text.Encoding]::UTF8.GetByteCount($treatmentNativeBody) -eq
        $treatmentNativeReviewSkillBodyBytes -and
    (Get-TextSha256 $treatmentNativeBody) -eq $treatmentNativeReviewSkillBodyHash
) 'The opt-in claim-validation treatment skill does not match its measured identity.'

$treatmentReferencePath = Join-Path $treatmentRoot 'references/claim-validation.md'
$treatmentReferenceText = Get-NormalizedText $treatmentReferencePath
Assert-Condition (
    (Get-GitBlob $treatmentReferencePath -ApplyAttributes) -eq $treatmentReferenceBlob -and
    [Text.Encoding]::UTF8.GetByteCount($treatmentReferenceText) -eq
        $treatmentReferenceNormalizedBytes -and
    (Get-TextSha256 $treatmentReferenceText) -eq $treatmentReferenceNormalizedHash
) (
    'The claim-validation treatment reference does not match its frozen identity.'
)
$packagedTreatmentSpecPath = Join-Path (
    $PSScriptRoot
) 'reviewer-capability-treatment.vally.yaml'
Assert-Condition (
    (Get-TextSha256 (Get-NormalizedText $packagedTreatmentSpecPath)) -eq
        $packagedTreatmentSpecSha256
) 'The opt-in claim-validation treatment spec does not match its packaged identity.'

$caseDirectories = @(
    Get-ChildItem (Join-Path $FixtureRoot 'cases') -Directory |
        Select-Object -ExpandProperty Name |
        Sort-Object
)
Assert-Condition (
    ($caseDirectories -join "`n") -eq ((@($cases.Keys) | Sort-Object) -join "`n")
) 'The reviewer-capability fixture set has missing or unexpected cases.'

Assert-Condition (-not (
    Get-ChildItem $FixtureRoot -Recurse -File |
        Where-Object Name -in @(
            'reviewer-capability-labels.json',
            'reviewer-capability-rubric.md',
            'reviewer-capability-controls.json',
            'reviewer-capability-control-trajectories.jsonl'
        )
)) 'The subject fixture tree contains grader-only answer material.'
Assert-Condition (-not (
    Get-ChildItem $FixtureRoot -Recurse -File |
        Select-String -SimpleMatch 'line77'
)) 'The subject fixture tree contains a historical finding-location cue.'

$expectedReviewCapabilityProperties = @(
    'coordinatorReplay',
    'executionMode',
    'feedback',
    'feedbackCutoffUtc',
    'guidanceCommit',
    'kind',
    'partition',
    'projectionMap',
    'skill',
    'suiteRole'
) | Sort-Object
$expectedProjectionProperties = @(
    'changedFiles',
    'diff',
    'feedback',
    'labels',
    'limitation',
    'pull',
    'source'
) | Sort-Object

foreach ($caseName in $cases.Keys) {
    $expected = $cases[$caseName]
    $caseRoot = Join-Path $FixtureRoot "cases/$caseName"
    $bundleRoot = Join-Path $caseRoot 'bundle'
    $manifestPath = Join-Path $bundleRoot 'manifest.json'
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json -Depth 100

    Assert-Condition ($manifest.version -eq 2 -and $manifest.ready -eq $true) (
        "$caseName is not a ready version-2 bundle."
    )
    Assert-Condition (
        $manifest.target.head -eq $expected.Head -and
        $manifest.target.mergeBase -eq $expected.MergeBase -and
        $manifest.target.baseTip -eq $expected.BaseTip
    ) "$caseName has the wrong frozen target identities."
    $pull = Get-Content (Join-Path $bundleRoot 'pull.json') -Raw | ConvertFrom-Json -Depth 100
    Assert-Condition ($pull.number -eq $expected.Pull) (
        "$caseName has the wrong pull-request identity."
    )
    Assert-Condition (
        $pull.PSObject.Properties['retrieval'] -and
        $pull.retrieval.frozenReviewHead -eq $expected.Head
    ) "$caseName pull metadata has the wrong frozen review head."
    Assert-FileReference $bundleRoot $pull.body "$caseName pull body"
    Assert-Condition ($manifest.reviewCapability.kind -eq 'natural-review-source-projection') (
        "$caseName has the wrong review-capability role."
    )
    Assert-Condition (
        (@(
            $manifest.reviewCapability.PSObject.Properties.Name |
                Sort-Object
        ) -join "`n") -eq ($expectedReviewCapabilityProperties -join "`n")
    ) "$caseName exposes asymmetric or answer-class review metadata."
    Assert-Condition ($manifest.reviewCapability.partition -eq 'development') (
        "$caseName must remain a development case."
    )
    Assert-Condition (    $manifest.reviewCapability.skill.gitBlob -eq $baselineReviewSkillBlob) (
        "$caseName has the wrong reviewer criteria blob."
    )
    Assert-Condition ($manifest.reviewCapability.guidanceCommit -eq $criteriaCommit) (
        "$caseName has the wrong guidance criteria commit."
    )
    Assert-Condition ($manifest.reviewCapability.feedback -eq $expected.FeedbackMode) (
        "$caseName has the wrong feedback projection mode."
    )
    Assert-Condition (
        [DateTimeOffset]$manifest.reviewCapability.feedbackCutoffUtc -eq
        [DateTimeOffset]::Parse($expected.Cutoff)
    ) (
        "$caseName has the wrong feedback cutoff."
    )
    Assert-Condition ($manifest.reviewCapability.coordinatorReplay -eq $false) (
        "$caseName incorrectly claims coordinator replay."
    )
    Assert-Condition (
        $manifest.reviewCapability.executionMode -eq 'trusted-offline-single-reviewer'
    ) "$caseName does not declare the trusted one-context execution mode."
    Assert-Condition ($manifest.reviewCapability.suiteRole -eq $expected.SuiteRole) (
        "$caseName has the wrong primary/secondary suite role."
    )
    Assert-Condition (-not $manifest.PSObject.Properties['calibration']) (
        "$caseName retains obsolete supplied-candidate calibration metadata."
    )

    Assert-FileReference $caseRoot $manifest.reviewCapability.projectionMap (
        "$caseName projection map"
    )
    $projectionMap = Get-Content (
        Join-Path $caseRoot $manifest.reviewCapability.projectionMap.path
    ) -Raw | ConvertFrom-Json -Depth 100
    Assert-Condition (
        (@($projectionMap.PSObject.Properties.Name | Sort-Object) -join "`n") -eq
        ($expectedProjectionProperties -join "`n")
    ) "$caseName exposes asymmetric or answer-class projection metadata."

    foreach ($artifact in $manifest.artifacts.PSObject.Properties) {
        $path = Join-Path $bundleRoot $artifact.Name
        Assert-Condition (Test-Path $path -PathType Leaf) (
            "$caseName is missing $($artifact.Name)."
        )
        Assert-Condition ((Get-Sha256 $path) -eq [string]$artifact.Value) (
            "$caseName has a stale artifact hash for $($artifact.Name)."
        )
    }

    Assert-OverlayFilesMatch (
        Join-Path $FixtureRoot 'common/guidance'
    ) (
        Join-Path $bundleRoot 'guidance'
    ) "$caseName guidance"
    Assert-OverlayFilesMatch (
        Join-Path $FixtureRoot 'common/required'
    ) (
        Join-Path $bundleRoot 'required'
    ) "$caseName required text"

    $guidanceRoots = if ($expected.UseCommonOverlay) {
        @((Join-Path $FixtureRoot 'common/guidance'))
    } else {
        @((Join-Path $bundleRoot 'guidance'))
    }
    $guidance = Get-OverlayDirectoryDigest $guidanceRoots
    Assert-Condition (
        $guidance.Files -eq $manifest.guidance.files -and
        $guidance.Sha256 -eq $manifest.guidance.sha256
    ) "$caseName has stale guidance integrity metadata."

    foreach ($role in @('head', 'mergeBase', 'baseTip')) {
        $sourceSet = $manifest.sources.$role
        $sourceRoot = Join-Path $bundleRoot $sourceSet.root
        Assert-Condition (
            @(Get-ChildItem $sourceRoot -Recurse -File).Count -eq $sourceSet.files
        ) "$caseName has the wrong $role source file count."
        Assert-Condition ($sourceSet.commit -eq $manifest.target.$role) (
            "$caseName $role source role has the wrong commit."
        )
        foreach ($sourceFile in Get-ChildItem $sourceRoot -Recurse -File) {
            $relative = [IO.Path]::GetRelativePath(
                $sourceRoot,
                $sourceFile.FullName
            ).Replace('\', '/')
            Assert-Condition ($relative.EndsWith('.source', [StringComparison]::Ordinal)) (
                "$caseName $role source lacks the .source suffix: $relative."
            )
            $gitPath = $relative.Substring(0, $relative.Length - '.source'.Length)
            $expectedBlob = & git -C $repoRoot rev-parse "$($sourceSet.commit):$gitPath" 2>&1
            Assert-Condition ($LASTEXITCODE -eq 0) (
                "$caseName cannot resolve $role Git blob '$gitPath': $expectedBlob"
            )
            Assert-Condition (
                (Get-GitBlob $sourceFile.FullName) -eq ([string]$expectedBlob).Trim()
            ) "$caseName $role source is not the claimed ordinary Git blob: $gitPath."
        }
    }
    foreach ($guide in @($manifest.guides)) {
        foreach ($file in @($guide.assignment.files)) {
            Assert-OverlayReference $bundleRoot $file "$caseName assigned $($file.kind)"
        }
    }
    foreach ($policy in @($manifest.policies)) {
        Assert-OverlayReference $bundleRoot $policy.body "$caseName policy"
    }
    foreach ($exclusion in @($manifest.exclusions)) {
        foreach ($property in @('scope', 'reason', 'body')) {
            if ($exclusion.PSObject.Properties[$property]) {
                Assert-OverlayReference $bundleRoot $exclusion.$property "$caseName exclusion"
            }
        }
    }
    if ($null -ne $manifest.context) {
        foreach ($context in @($manifest.context)) {
            $path = Join-Path $bundleRoot "guidance/$($context.path).source"
            if (-not (Test-Path $path -PathType Leaf)) {
                $path = Join-Path (
                    Join-Path $FixtureRoot 'common/guidance'
                ) "$($context.path).source"
            }
            Assert-Condition (Test-Path $path -PathType Leaf) (
                "$caseName is missing context '$($context.path)'."
            )
            Assert-Condition ((Get-Sha256 $path) -eq [string]$context.sha256) (
                "$caseName has a stale context hash for '$($context.path)'."
            )
        }
    }

    $requiredRoots = if ($expected.UseCommonOverlay) {
        @(
            (Join-Path $FixtureRoot 'common/required'),
            (Join-Path $bundleRoot $manifest.requiredText.root)
        )
    } else {
        @((Join-Path $bundleRoot $manifest.requiredText.root))
    }
    $required = Get-OverlayDirectoryDigest $requiredRoots
    Assert-Condition (
        $required.Files -eq $manifest.requiredText.files -and
        $required.Sha256 -eq $manifest.requiredText.sha256
    ) "$caseName has stale required-text integrity metadata."

    Assert-Condition (
        @($pull.labels).Count -eq 0 -and
        -not $pull.PSObject.Properties['updatedAt'] -and
        -not $pull.PSObject.Properties['closedAt'] -and
        -not $pull.PSObject.Properties['mergedAt'] -and
        -not $pull.PSObject.Properties['reviewDecision'] -and
        -not $pull.PSObject.Properties['mergeCommit']
    ) "$caseName exposes post-cutoff labels or decision metadata."
    Assert-Condition (
        $pull.retrieval.descriptionHistoricalAtCutoff -eq $true -and
        [DateTimeOffset]$pull.retrieval.descriptionSnapshotEditedAt -lt
            [DateTimeOffset]::Parse($expected.Cutoff)
    ) "$caseName does not bind its pull description to a pre-cutoff snapshot."

    $feedbackPath = Join-Path $bundleRoot 'feedback.json'
    $feedback = Get-Content $feedbackPath -Raw | ConvertFrom-Json -Depth 100
    $allFeedbackIds = [Collections.Generic.List[long]]::new()
    $cutoff = [DateTimeOffset]::Parse($expected.Cutoff)
    foreach ($category in @('comments', 'reviews', 'inline')) {
        foreach ($item in @($feedback.$category)) {
            $allFeedbackIds.Add([long]$item.id)
            Assert-Condition ((Get-EventTime $item $category) -lt $cutoff) (
                "$caseName contains $category feedback at or after its cutoff."
            )
            Assert-FileReference $bundleRoot $item.body "$caseName $category feedback body"
        }
    }
    if ($expected.ExcludedFeedbackId -ne 0) {
        Assert-Condition ($expected.ExcludedFeedbackId -notin $allFeedbackIds) (
            "$caseName leaks its label-source feedback item."
        )
    }
    if ($expected.ContainsKey('RequiredPriorFeedbackId')) {
        Assert-Condition ($expected.RequiredPriorFeedbackId -in $allFeedbackIds) (
            "$caseName omits the prior feedback required for deduplication."
        )
    }
}

$labels = Get-Content $LabelsPath -Raw | ConvertFrom-Json -Depth 100
Assert-Condition (
    $labels.revision -eq 4 -and
    $labels.priorPrunedRevision.revision -eq 3 -and
    $labels.priorPrunedRevision.gitCommit -eq 'ebac3e30f5551a676ea7c309c2891c3f82f2777d' -and
    $labels.priorPrunedRevision.gitBlob -eq '85769d8d5a614feb7416d4e0f5ca92e74d7afe0f' -and
    $labels.priorPrunedRevision.sha256 -eq 'b4fbecb94860b7f2c8bc64e1be2cacb6d5b0688799acc4ba928b1e6cf106e319' -and
    $labels.priorMeasuredRevision.revision -eq 2 -and
    $labels.priorMeasuredRevision.gitCommit -eq 'b34fe32c517ec4e1d8ce8cd0e3085b610a98b3b6' -and
    $labels.priorMeasuredRevision.gitBlob -eq '99b92613a8608f88e3438d91b3115274cadbf149' -and
    $labels.priorMeasuredRevision.sourceSha256 -eq '044c19252892b0cae2c34d171093baa3dd66bea7e7178cd8a8cf9995f1a58cce' -and
    $labels.priorMeasuredRevision.runArtifactSha256 -eq 'b0b998084069ad68c5d65322ec2e8e24385c316a619a4448e614ed00801e813c' -and
    $labels.priorMeasuredRevision.runId -eq '3cf0bd32-d93e-41ca-8650-547caafe9379' -and
    $labels.treatment.name -eq 'claim-triggered-source-validation' -and
    $labels.treatment.reviewSkillGitBlob -eq $treatmentReviewSkillBlob -and
    $labels.treatment.reviewSkillNormalizedUtf8Bytes -eq $treatmentNativeReviewSkillBodyBytes -and
    $labels.treatment.reviewSkillNormalizedSha256 -eq $treatmentNativeReviewSkillBodyHash -and
    $labels.treatment.referenceGitBlob -eq $treatmentReferenceBlob -and
    $labels.treatment.referenceUtf8Bytes -eq $treatmentReferenceBytes -and
    $labels.treatment.referenceSha256 -eq $treatmentReferenceHash -and
    $labels.treatment.rubricSha256 -eq $treatmentRubricHash
) 'The pruned labels no longer identify the measured revision-2 label artifact.'
Assert-Condition (
    (
        Get-TextSha256 (
            Get-NormalizedText (
                Join-Path $PSScriptRoot 'reviewer-capability-rubric.md'
            )
        )
    ) -eq $treatmentRubricNormalizedHash
) 'The treatment rubric no longer matches its frozen identity.'
Assert-Condition (
    $labels.criteria.defaultBranchCommit -eq $criteriaCommit -and
    $labels.criteria.reviewSkillGitBlob -eq $baselineReviewSkillBlob
) 'The label file has the wrong frozen criteria identities.'
$labelCases = @($labels.cases)
Assert-Condition ($labelCases.Count -eq $cases.Count) 'The label file has the wrong case count.'
Assert-Condition (
    (@($labelCases.id | Sort-Object) -join "`n") -eq ((@($cases.Keys) | Sort-Object) -join "`n")
) 'The label file does not cover the exact fixture cases.'

if ($HistoricalLabelsPath) {
    Assert-Condition (-not [string]::IsNullOrWhiteSpace($ExpectedStimulus)) (
        '-HistoricalLabelsPath requires -ExpectedStimulus.'
    )
    Assert-Condition ((Get-Sha256 $HistoricalLabelsPath) -eq (
        $labels.priorMeasuredRevision.runArtifactSha256
    )) 'The historical label file does not match the recorded measured revision.'
    $historicalLabels = Get-Content $HistoricalLabelsPath -Raw |
        ConvertFrom-Json -Depth 100
    Assert-Condition (
        $historicalLabels.revision -eq $labels.priorMeasuredRevision.revision -and
        @($historicalLabels.cases).Count -eq 6
    ) 'The historical label file has the wrong measured revision or case count.'
    $currentSelectedLabel = @(
        $labelCases | Where-Object stimulus -eq $ExpectedStimulus
    )
    $historicalSelectedLabel = @(
        $historicalLabels.cases | Where-Object stimulus -eq $ExpectedStimulus
    )
    Assert-Condition (
        $currentSelectedLabel.Count -eq 1 -and
        $historicalSelectedLabel.Count -eq 1 -and
        (($currentSelectedLabel[0] | ConvertTo-Json -Depth 100 -Compress) -ceq (
            $historicalSelectedLabel[0] | ConvertTo-Json -Depth 100 -Compress
        ))
    ) 'The selected historical label differs from the pruned current label.'
}
Assert-Condition (
    @($labelCases | Where-Object partition -ne 'development').Count -eq 0
) 'The label file incorrectly claims a non-development partition.'
$primaryLabels = @($labelCases | Where-Object suiteRole -eq 'primary')
Assert-Condition (
    $primaryLabels.Count -eq 2 -and
    @($primaryLabels | Where-Object expectedDisposition -eq 'actionable-finding-required').Count -eq 1 -and
    @($primaryLabels | Where-Object expectedDisposition -eq 'no-required-hidden-diagnosis').Count -eq 1
) 'The development suite must retain one restraint and one actionable concurrency case.'
Assert-Condition (
    @($labelCases | Where-Object suiteRole -eq 'secondary').Count -eq 0
) 'The reduced suite must not retain optional secondary cases.'
foreach ($labelCase in $labelCases) {
    Assert-Condition (
        $labelCase.suiteRole -eq $cases[[string]$labelCase.id].SuiteRole
    ) "$($labelCase.id) has the wrong primary/secondary label role."
}

$controls = @(Get-Content $ControlsPath -Raw | ConvertFrom-Json -Depth 100)
Assert-Condition ($controls.Count -eq 5) 'The reduced judge-development control set must contain five controls.'
Assert-Condition (@($controls.id | Sort-Object -Unique).Count -eq 5) (
    'Judge-development control IDs are not unique.'
)
Assert-Condition (
    @($controls | Where-Object { $_.expectedScore -eq 1 }).Count -eq 2 -and
    @($controls | Where-Object { $_.expectedScore -eq 0 }).Count -eq 3
) 'Judge-development controls must retain two positive and three negative expectations.'
foreach ($control in $controls) {
    Assert-Condition ($control.kind -eq 'judge-development-control') (
        "$($control.id) is not labelled as a judge-development control."
    )
    Assert-Condition ($control.case -in $cases.Keys) "$($control.id) names an unknown case."
    Assert-Condition ($control.stimulus -eq $cases[[string]$control.case].Stimulus) (
        "$($control.id) has the wrong stimulus."
    )
    Assert-Condition (
        (Test-NumericValue $control.expectedScore) -and
        $control.expectedScore -in @(0, 1)
    ) "$($control.id) has an invalid expected score."
}

$controlTrajectories = @(
    Get-Content $ControlTrajectoriesPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_ | ConvertFrom-Json -Depth 100 }
)
Assert-Condition ($controlTrajectories.Count -eq $controls.Count) (
    'The control trajectory count does not match the control definitions.'
)
foreach ($control in $controls) {
    $trajectory = @(
        $controlTrajectories |
            Where-Object { $_.trajectory.id -eq "control-$($control.id)" }
    )
    Assert-Condition ($trajectory.Count -eq 1) (
        "$($control.id) has a missing or duplicate control trajectory."
    )
    Assert-Condition (
        $trajectory[0].status -eq 'success' -and
        $trajectory[0].trajectory.stimulus.name -eq $control.stimulus -and
        ([string]$trajectory[0].trajectory.stimulus.prompt).Contains(
            'PATH: single-reviewer'
        ) -and
        -not [string]::IsNullOrWhiteSpace([string]$trajectory[0].trajectory.output)
    ) "$($control.id) has malformed saved-trajectory input."
    $controlEvents = @($trajectory[0].trajectory.events)
    Assert-Condition (
        $controlEvents.Count -eq 2 -and
        $controlEvents[0].type -eq 'assistant_message' -and
        $controlEvents[0].data.content -eq $trajectory[0].trajectory.output -and
        (
            ([string]$trajectory[0].trajectory.output) -match
                '(?im)^\s*PATH:\s*single-reviewer\s*$'
        ) -and
        $controlEvents[1].type -eq 'token_usage' -and
        $controlEvents[1].data.model -eq $expectedModel
    ) "$($control.id) does not preserve the common semantic-control envelope."
    $expectedSuffix = (
        "fixtures/reviewer-capability/cases/$($control.case)/bundle"
    )
    Assert-Condition (
        ([string]$trajectory[0].trajectory.workDir).Replace('\', '/').EndsWith(
            $expectedSuffix,
            [StringComparison]::Ordinal
        )
    ) "$($control.id) grades against the wrong source workspace."
    Assert-OutputReceipt (
        [string]$trajectory[0].trajectory.output
    ) $cases[[string]$control.case] $control.id
}

$specText = Get-Content $SpecPath -Raw
$specSha256 = Get-TextSha256 (
    $specText.Replace("`r`n", "`n").Replace("`r", "`n")
)
Assert-Condition ($specSha256 -in @(
    $expectedSpecSha256,
    $packagedTreatmentSpecSha256,
    $measuredSpecSha256,
    $baselineComparisonSpecSha256,
    $treatmentComparisonSpecSha256
)) (
    'The specialized Vally spec no longer matches its frozen launch identity.'
)
$expectedStimulusCount = if ($specSha256 -eq $measuredSpecSha256) { 3 } else { 2 }
Assert-Condition ($specText.Contains("name: $expectedEvalName")) (
    'The specialized Vally spec has the wrong eval identity.'
)
Assert-Condition ($specText.Contains('model: gpt-5.6-sol')) (
    'The specialized Vally spec has the wrong subject identity.'
)
Assert-Condition ($specText.Contains('model: gemini-4-argon')) (
    'The specialized Vally spec has the wrong judge identity.'
)
Assert-Condition ($specText.Contains('evidence:') -and
    $specText.Contains('          - trajectory') -and
    $specText.Contains('          - repo')) (
    'The semantic grader does not retain trajectory and repository evidence.'
)
Assert-Condition ($specText.Contains('grading_environment:') -and
    $specText.Contains('reviewer-capability-labels.json') -and
    $specText.Contains('reviewer-capability-rubric.md')) (
    'The specialized Vally spec does not stage hidden grader references.'
)
Assert-Condition (-not $specText.Contains('candidate-calibration')) (
    'The natural-review spec reuses supplied-candidate calibration inputs.'
)
foreach ($caseName in @($cases.Keys | Where-Object { $cases[$_].Pilot })) {
    Assert-Condition ($specText.Contains("name: $($cases[$caseName].Stimulus)")) (
        "The specialized Vally spec omits $caseName."
    )
    Assert-Condition ($specText.Contains("name: $($cases[$caseName].Grader)")) (
        "The specialized Vally spec omits the named grader for $caseName."
    )
}
$secondaryStimuli = @(
    $cases.Keys |
        Where-Object { -not $cases[$_].Pilot } |
        ForEach-Object { $cases[$_].Stimulus }
)
foreach ($stimulus in $secondaryStimuli) {
    Assert-Condition (-not $specText.Contains("name: $stimulus")) (
        "The first paid pilot incorrectly includes secondary stimulus $stimulus."
    )
}
Assert-Condition (
    ([regex]::Matches(
        $specText,
        '(?m)^  - name: reviewer-capability-pr-'
    )).Count -eq $expectedStimulusCount
) 'The Vally spec has the wrong target-aligned stimulus count.'
Assert-Condition (
    ([regex]::Matches(
        $specText,
        'PATH: single-reviewer'
    )).Count -eq $expectedStimulusCount -and
    ([regex]::Matches(
        $specText,
        'do not spawn workers or'
    )).Count -eq $expectedStimulusCount
) 'Every primary prompt must explicitly configure the trusted one-context path.'
Assert-Condition (
    ([regex]::Matches(
        $specText,
        '(?m)^\s+threshold:\s+1\s*$'
    )).Count -eq $expectedStimulusCount -and
    ([regex]::Matches(
        $specText,
        'MANUAL_ADJUDICATION_REQUIRED'
    )).Count -eq $expectedStimulusCount
) 'Every primary semantic grader must retain binary threshold 1 and manual-adjudication handling.'
Assert-Condition (-not $specText.Contains('line77')) (
    'The subject-visible spec contains a historical finding-location cue.'
)

if ($ControlResultsDirectory) {
    Assert-Condition (
        $ExpectedControlId.Count -gt 0 -and
        @($ExpectedControlId | Sort-Object -Unique).Count -eq $ExpectedControlId.Count
    ) '-ControlResultsDirectory requires unique -ExpectedControlId values.'
    $unknownControlIds = @(
        $ExpectedControlId |
            Where-Object { $_ -notin @($controls.id) }
    )
    Assert-Condition ($unknownControlIds.Count -eq 0) (
        "Unknown expected control IDs: $($unknownControlIds -join ', ')."
    )
    $controlResultFiles = @(
        Get-ChildItem $ControlResultsDirectory -Recurse -File -Filter 'results.jsonl'
    )
    Assert-Condition ($controlResultFiles.Count -gt 0) (
        "No control results.jsonl exists under '$ControlResultsDirectory'."
    )
    $controlResults = @(
        foreach ($file in $controlResultFiles) {
            Get-Content $file.FullName |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                ForEach-Object { $_ | ConvertFrom-Json -Depth 100 } |
                Where-Object {
                    $_.status -and $_.trajectory -and $_.gradeResult
                }
        }
    )
    Assert-Condition ($controlResults.Count -eq $ExpectedControlId.Count) (
        'The control result count does not match the explicitly expected controls.'
    )
    foreach ($controlId in $ExpectedControlId) {
        $control = @($controls | Where-Object id -eq $controlId)
        $result = @(
            $controlResults |
                Where-Object {
                    $_.trajectory.id -eq "control-$controlId"
                }
        )
        Assert-Condition ($result.Count -eq 1) (
            "$controlId has a missing or duplicate grade result."
        )
        $result = $result[0]
        Assert-Condition (
            $result.status -eq 'success' -and
            $result.trajectory.stimulus.name -eq $control[0].stimulus
        ) "$controlId has an error or mismatched case result."
        $expectedGrader = $cases[[string]$control[0].case].Grader
        $grades = @(
            @($result.gradeResult.details) |
                Where-Object {
                    $null -ne $_ -and $_.name -eq $expectedGrader
                }
        )
        Assert-Condition ($grades.Count -eq 1) (
            "$controlId does not retain its required named semantic grader."
        )
        $grade = $grades[0]
        Assert-Condition (
            (Test-NumericValue $grade.score) -and
            [double]$grade.score -in @(0, 1) -and
            $grade.passed -is [bool] -and
            $grade.passed -eq ([double]$grade.score -eq 1) -and
            (Test-NumericValue $result.gradeResult.score) -and
            [double]$result.gradeResult.score -eq [double]$grade.score -and
            $result.gradeResult.passed -is [bool] -and
            $result.gradeResult.passed -eq $grade.passed
        ) "$controlId has inconsistent binary grade evidence."
        Assert-Condition (
            [double]$grade.score -eq [double]$control[0].expectedScore
        ) "$controlId did not produce its expected semantic score."
        Assert-Condition (
            $grade.metadata.model -eq $expectedJudge -and
            $grade.metadata.scoring -eq 'binary'
        ) "$controlId has the wrong semantic grader provenance."
    }
    Write-Host (
        'Reviewer-capability control discrimination passed for: ' +
        ($ExpectedControlId -join ', ') +
        '.'
    )
}

Write-Host 'Reviewer-capability fixture, label, control, and spec integrity passed.'

Assert-Condition (
    $ResultMode -ne 'Acceptance' -or
    -not [string]::IsNullOrWhiteSpace($ResultsDirectory)
) '-ResultMode Acceptance requires -ResultsDirectory.'
Assert-Condition (
    -not $GradeResultsDirectory -or $ResultsDirectory
) '-GradeResultsDirectory requires -ResultsDirectory.'

if ($ResultsDirectory) {
    Assert-Condition (-not [string]::IsNullOrWhiteSpace($ExpectedStimulus)) (
        '-ExpectedStimulus is required with -ResultsDirectory.'
    )
    Assert-Condition ($ExpectedStimulus -in @($cases.Values.Stimulus)) (
        '-ExpectedStimulus does not identify a frozen case.'
    )
    $parsedExpectedRunId = [guid]::Empty
    Assert-Condition ([guid]::TryParse($ExpectedRunId, [ref]$parsedExpectedRunId)) (
        '-ExpectedRunId must identify the exact run being checked.'
    )
    $resultFiles = @(Get-ChildItem $ResultsDirectory -Recurse -File -Filter 'results.jsonl')
    Assert-Condition ($resultFiles.Count -gt 0) (
        "Expected at least one results.jsonl under '$ResultsDirectory'."
    )
    $results = @(
        foreach ($file in $resultFiles) {
            Get-Content $file.FullName |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                ForEach-Object { $_ | ConvertFrom-Json -Depth 100 } |
                Where-Object {
                    $_.PSObject.Properties['type'] -and
                    $_.type -eq 'trial-result'
                }
        }
    )
    Assert-Condition ($results.Count -eq 1) 'Expected exactly one subject attempt.'
    $result = $results[0]
    Assert-Condition ($result.status -eq 'success') 'The subject attempt did not complete.'
    Assert-Condition (
        $result.evalName -eq $expectedEvalName -and
        $result.variant -eq 'main' -and
        $result.model -eq $expectedModel -and
        $result.stimulus -eq $ExpectedStimulus -and
        $result.runId -eq $ExpectedRunId
    ) 'The subject attempt has the wrong run, eval, variant, model, or stimulus provenance.'
    Assert-Condition (
        [string]$result.itemId -like "*::main::$expectedModel::$ExpectedStimulus::trial-0"
    ) 'The subject attempt has the wrong item identity.'

    $trajectory = $result.trajectory
    Assert-Condition ($null -ne $trajectory) 'The subject attempt omits its trajectory.'
    $caseName = [string](
        $cases.GetEnumerator() |
            Where-Object { $_.Value.Stimulus -eq $ExpectedStimulus } |
            Select-Object -First 1 -ExpandProperty Key
    )
    Assert-Condition ($trajectory.stimulus.name -eq $ExpectedStimulus) (
        'The trajectory has the wrong stimulus identity.'
    )
    $resolvedEvalPath = [IO.Path]::GetFullPath([string]$result.evalFilePath)
    $trustedEvalPath = [IO.Path]::GetFullPath(
        $(if ($ExpectedEvalFilePath) { $ExpectedEvalFilePath } else { $SpecPath })
    )
    Assert-Condition (
        $resolvedEvalPath -eq $trustedEvalPath -and
        ([string]$result.itemId).StartsWith(
            "$resolvedEvalPath::",
            [StringComparison]::OrdinalIgnoreCase
        )
    ) 'The subject attempt is not bound to the frozen eval specification.'
    Assert-Condition (
        (Get-TextSha256 ([string]$trajectory.stimulus.prompt)) -eq
            $expectedPromptSha256
    ) 'The subject attempt is not bound to the frozen trusted caller prompt.'
    $launchSkills = @($trajectory.stimulus.environment.skills)
    $expectedLaunchSkill = switch ($specSha256) {
        $baselineComparisonSpecSha256 {
            'skills/baseline/review-pull-request'
            break
        }
        $treatmentComparisonSpecSha256 {
            'skills/treatment/review-pull-request'
            break
        }
        $packagedTreatmentSpecSha256 {
            'treatments/claim-validation/review-pull-request'
            break
        }
        default {
            '../../../.github/skills/review-pull-request'
        }
    }
    Assert-Condition (
        $launchSkills.Count -eq 1 -and
        ([string]$launchSkills[0]).Replace('\', '/') -eq
            $expectedLaunchSkill
    ) 'The subject attempt has the wrong staged native skill.'
    $expectedLaunchFiles = @(
        if ($cases[$caseName].UseCommonOverlay) {
            'fixtures/reviewer-capability/common/guidance|review/bundle/guidance',
            'fixtures/reviewer-capability/common/required|review/bundle/required',
            "fixtures/reviewer-capability/cases/$caseName/bundle|review/bundle"
        } else {
            "fixtures/reviewer-capability/cases/$caseName/bundle|review/bundle"
        }
    )
    if ($specSha256 -ne $measuredSpecSha256) {
        $expectedLaunchFiles += (
            "fixtures/reviewer-capability/cases/$caseName/projection-map.json|" +
            'review/bundle/projection-map.json'
        )
    }
    $actualLaunchFiles = @(
        $trajectory.stimulus.environment.files |
            ForEach-Object {
                (
                    ([string]$_.src).Replace('\', '/') + '|' +
                    ([string]$_.dest).Replace('\', '/')
                )
            } |
            Sort-Object
    )
    Assert-Condition (
        $actualLaunchFiles.Count -eq $expectedLaunchFiles.Count -and
        ($actualLaunchFiles -join "`n") -eq
            (($expectedLaunchFiles | Sort-Object) -join "`n")
    ) 'The subject attempt has the wrong staged source bundle.'
    Assert-Condition (-not [string]::IsNullOrWhiteSpace([string]$trajectory.output)) (
        'The subject output is empty.'
    )
    $models = @(
        $trajectory.events |
            Where-Object type -eq 'token_usage' |
            ForEach-Object { $_.data.model } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique
    )
    Assert-Condition ($models.Count -eq 1 -and $models[0] -eq $expectedModel) (
        'The trajectory does not retain the exact subject model identity.'
    )

    $gradeResult = $result.gradeResult
    if ($GradeResultsDirectory) {
        Assert-Condition ($null -eq $gradeResult) (
            'A separate grade result was supplied for an already graded subject result.'
        )
        $gradeFiles = @(
            Get-ChildItem $GradeResultsDirectory -Recurse -File -Filter 'results.jsonl'
        )
        Assert-Condition ($gradeFiles.Count -eq 1) (
            "Expected exactly one saved grade results.jsonl under '$GradeResultsDirectory'; found $($gradeFiles.Count)."
        )
        $savedOutcomes = @(
            Get-Content $gradeFiles[0].FullName |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                ForEach-Object { $_ | ConvertFrom-Json -Depth 100 } |
                Where-Object {
                    $_.status -and $_.trajectory
                }
        )
        Assert-Condition ($savedOutcomes.Count -eq 1) (
            'Expected exactly one saved associated grade result.'
        )
        $savedGrade = $savedOutcomes[0]
        Assert-Condition (
            $savedGrade.status -eq 'success' -and
            $null -ne $savedGrade.gradeResult -and
            $savedGrade.runId -eq $result.runId -and
            $savedGrade.trajectory.id -eq $trajectory.id -and
            $savedGrade.trajectory.stimulus.name -eq $trajectory.stimulus.name -and
            $savedGrade.gradeResult.trajectoryId -eq $trajectory.id -and
            $savedGrade.gradeResult.stimulusName -eq $ExpectedStimulus -and
            $savedGrade.trajectory.output -ceq $trajectory.output -and
            (($savedGrade.trajectory.events | ConvertTo-Json -Depth 100 -Compress) -ceq
                ($trajectory.events | ConvertTo-Json -Depth 100 -Compress))
        ) 'The saved associated grade is not bound to the exact subject trajectory.'
        $gradeResult = $savedGrade.gradeResult
    }
    Assert-Condition (
        $null -ne $gradeResult -and (Test-NumericValue $gradeResult.score)
    ) 'The subject attempt has no numeric associated semantic score.'
    Assert-Condition ([double]$gradeResult.score -in @(0, 1)) (
        'The subject attempt has a non-binary aggregate semantic score.'
    )
    $semanticGrades = @(
        @($gradeResult.details) |
            Where-Object {
                $null -ne $_ -and $_.name -eq $cases[$caseName].Grader
            }
    )
    Assert-Condition ($semanticGrades.Count -eq 1) (
        'The subject attempt does not retain the named semantic grader.'
    )
    Assert-Condition (Test-NumericValue $semanticGrades[0].score) (
        'The semantic grader does not retain a numeric score.'
    )
    Assert-Condition ([double]$semanticGrades[0].score -in @(0, 1)) (
        'The semantic grader does not retain a binary score.'
    )
    Assert-Condition (
        $semanticGrades[0].passed -is [bool] -and
        $semanticGrades[0].passed -eq ([double]$semanticGrades[0].score -eq 1) -and
        $gradeResult.passed -is [bool] -and
        $gradeResult.passed -eq ([double]$gradeResult.score -eq 1) -and
        [double]$gradeResult.score -eq [double]$semanticGrades[0].score
    ) 'The semantic score and pass flags are inconsistent.'
    Assert-Condition ($semanticGrades[0].metadata.scoring -eq 'binary') (
        'The semantic grader did not retain binary-scoring provenance.'
    )

    $events = @($trajectory.events)
    $toolCalls = @($events | Where-Object type -eq 'tool_call')
    Assert-Condition ($toolCalls.Count -gt 0) 'The subject attempt has no tool-call evidence.'
    $skillCall = $toolCalls[0]
    Assert-Condition (
        $skillCall.data.toolName -eq 'skill' -and
        $skillCall.data.arguments.skill -eq 'review-pull-request'
    ) 'The subject did not invoke the native review skill first.'
    $skillResult = @(
        $events |
            Where-Object {
                $_.type -eq 'tool_result' -and
                $_.data.toolCallId -eq $skillCall.data.toolCallId
            }
    )
    Assert-Condition (
        $skillResult.Count -eq 1 -and
        $skillResult[0].data.toolName -eq 'skill' -and
        $skillResult[0].data.success -eq $true
    ) 'The first native review skill call has no successful result.'
    $skillActivations = @(
        $events |
            Where-Object {
                $_.type -eq 'skill_activation' -and
                $_.data.name -eq 'review-pull-request' -and
                -not [string]::IsNullOrWhiteSpace([string]$_.data.content)
            }
    )
    Assert-Condition ($skillActivations.Count -eq 1) (
        'The subject attempt has no complete native skill activation evidence.'
    )
    $nativeBody = ([string]$skillActivations[0].data.content).
        Replace("`r`n", "`n").
        Replace("`r", "`n")
    $nativeBodyBytes = [Text.Encoding]::UTF8.GetBytes($nativeBody)
    $usesBaselineSkill = $specSha256 -in @(
        $expectedSpecSha256,
        $measuredSpecSha256,
        $baselineComparisonSpecSha256
    )
    $expectedNativeSkillBlob = if ($usesBaselineSkill) {
        $baselineReviewSkillBlob
    } else {
        $treatmentReviewSkillBlob
    }
    $expectedNativeSkillBytes = if ($usesBaselineSkill) {
        $baselineNativeReviewSkillBodyBytes
    } else {
        $treatmentNativeReviewSkillBodyBytes
    }
    $expectedNativeSkillHash = if ($usesBaselineSkill) {
        $baselineNativeReviewSkillBodyHash
    } else {
        $treatmentNativeReviewSkillBodyHash
    }
    Assert-Condition (
        $nativeBodyBytes.Length -eq $expectedNativeSkillBytes -and
        (Get-TextSha256 $nativeBody) -eq $expectedNativeSkillHash
    ) (
        "The native activation body does not match frozen skill blob " +
        "$expectedNativeSkillBlob (actual bytes $($nativeBodyBytes.Length), " +
        "SHA-256 $(Get-TextSha256 $nativeBody))."
    )
    $activationPath = ([string]$skillActivations[0].data.path).Replace('\', '/')
    Assert-Condition (
        $activationPath.EndsWith(
            '/review-pull-request/SKILL.md',
            [StringComparison]::OrdinalIgnoreCase
        )
    ) 'The subject activated the wrong native skill path.'

    $skillCallIndex = [Array]::IndexOf($events, $skillCall)
    $skillResultIndex = [Array]::IndexOf($events, $skillResult[0])
    $skillActivationIndex = [Array]::IndexOf($events, $skillActivations[0])
    $nextToolCallIndex = -1
    for ($index = $skillCallIndex + 1; $index -lt $events.Count; $index++) {
        if ($events[$index].type -eq 'tool_call') {
            $nextToolCallIndex = $index
            break
        }
    }
    Assert-Condition (
        $skillResultIndex -gt $skillCallIndex -and
        $skillActivationIndex -gt $skillResultIndex -and
        ($nextToolCallIndex -eq -1 -or $skillActivationIndex -lt $nextToolCallIndex) -and
        $skillCall.turn -eq $skillResult[0].turn -and
        $skillCall.turn -eq $skillActivations[0].turn
    ) 'The native activation is not bound to the first successful skill call.'

    $manifest = Get-Content (
        Join-Path $FixtureRoot "cases/$caseName/bundle/manifest.json"
    ) -Raw | ConvertFrom-Json -Depth 100
    $guideReferences = @(
        $manifest.guides |
            ForEach-Object {
                $_.assignment.files |
                    Where-Object kind -eq 'guide'
            }
    )
    Assert-Condition ($guideReferences.Count -eq $cases[$caseName].GuideCount) (
        'The fixture does not assign the expected routed guides.'
    )
    foreach ($reference in $guideReferences) {
        $expectedSuffix = (
            '/review/bundle/' + ([string]$reference.path).Replace('\', '/')
        ).ToLowerInvariant()
        $completeReads = @()
        foreach ($read in @(
            $toolCalls |
                Where-Object {
                    $_.data.toolName -in @('view', 'read_file') -and
                    $_.data.arguments -and
                    ([string]$_.data.arguments.path).Replace('\', '/').ToLowerInvariant().EndsWith(
                        $expectedSuffix,
                        [StringComparison]::Ordinal
                    )
                }
        )) {
            $readResult = @(
                $events |
                    Where-Object {
                        $_.type -eq 'tool_result' -and
                        $_.data.toolCallId -eq $read.data.toolCallId
                    }
            )
            if ($readResult.Count -ne 1 -or $readResult[0].data.success -ne $true) {
                continue
            }
            $contentProperty = $readResult[0].data.result.PSObject.Properties['content']
            if (-not $contentProperty -or $null -eq $contentProperty.Value) {
                continue
            }
            $content = [string]$contentProperty.Value
            $bytes = [Text.Encoding]::UTF8.GetBytes($content)
            if ($bytes.Length -eq [long]$reference.utf8Bytes -and
                (Get-TextSha256 $content) -eq [string]$reference.sha256) {
                $completeReads += $read
            }
        }
        Assert-Condition ($completeReads.Count -gt 0) (
            "The subject has no complete successful read of '$($reference.path)'."
        )
    }

    if ($specSha256 -in @(
        $packagedTreatmentSpecSha256,
        $treatmentComparisonSpecSha256
    )) {
        Assert-Condition (
            [string]$trajectory.output -match
                '(?im)^\s*VALIDATION:\s*claim-triggered(?:\s|\(|$)'
        ) 'The treatment output omits the claim-triggered validation receipt.'
        $ledgerMessages = @(
            $events |
                Where-Object {
                    $_.type -eq 'assistant_message' -and
                    -not [string]::IsNullOrWhiteSpace([string]$_.data.content) -and
                    [string]$_.data.content -match
                        '(?s)^\s*CANDIDATE_LEDGER:?(?:\s|\r|\n|$)'
                }
        )
        Assert-Condition ($ledgerMessages.Count -eq 1) (
            'The treatment trajectory does not retain one pre-validation candidate ledger.'
        )
        $ledger = ([string]$ledgerMessages[0].data.content).Trim()
        $emptyLedger = $ledger -match '^CANDIDATE_LEDGER:?\s+none$'
        if (-not $emptyLedger) {
            $ledgerLines = @(
                $ledger -split "`r?`n" |
                    ForEach-Object { $_.Trim().Replace('`', '') } |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
            )
            $bulletLedger = (
                $ledgerLines.Count -gt 1 -and
                $ledgerLines[0] -match '^CANDIDATE_LEDGER:?$' -and
                @(
                    $ledgerLines |
                        Select-Object -Skip 1 |
                        Where-Object {
                            $_ -notmatch
                                '^- C[0-9]+ \| [A-Za-z0-9][A-Za-z0-9 /_-]* \| [^|:\r\n]+:[0-9]+$'
                        }
                ).Count -eq 0
            )
            $tableLedger = (
                $ledgerLines.Count -gt 3 -and
                $ledgerLines[0] -match '^CANDIDATE_LEDGER:?$' -and
                $ledgerLines[1] -ceq '| ID | Claim type | File:line |' -and
                $ledgerLines[2] -match '^\|[-: ]+\|[-: ]+\|[-: ]+\|$' -and
                @(
                    $ledgerLines |
                        Select-Object -Skip 3 |
                        Where-Object {
                            $_ -notmatch
                                '^\| C[0-9]+ \| [A-Za-z0-9][A-Za-z0-9 /_-]* \| [^|:\r\n]+:[0-9]+ \|$'
                        }
                ).Count -eq 0
            )
            Assert-Condition (
                $bulletLedger -or $tableLedger
            ) 'The pre-validation candidate ledger contains more than ID, claim type, and file:line.'
        }

        $expectedTreatmentReferencePath = (
            (Split-Path $activationPath -Parent).Replace('\', '/').TrimEnd('/') +
            '/references/claim-validation.md'
        )
        $treatmentReads = @()
        foreach ($read in @(
            $toolCalls |
                Where-Object {
                    $_.data.toolName -in @('view', 'read_file') -and
                    $_.data.arguments -and
                    ([string]$_.data.arguments.path).Replace('\', '/').Equals(
                        $expectedTreatmentReferencePath,
                        [StringComparison]::OrdinalIgnoreCase
                    )
                }
        )) {
            $readResult = @(
                $events |
                    Where-Object {
                        $_.type -eq 'tool_result' -and
                        $_.data.toolCallId -eq $read.data.toolCallId
                    }
            )
            if ($readResult.Count -ne 1 -or $readResult[0].data.success -ne $true) {
                continue
            }
            $contentProperty = $readResult[0].data.result.PSObject.Properties['content']
            if (-not $contentProperty -or $null -eq $contentProperty.Value) {
                continue
            }
            $content = ([string]$contentProperty.Value).
                Replace("`r`n", "`n").
                Replace("`r", "`n")
            $bytes = [Text.Encoding]::UTF8.GetBytes($content)
            if ($bytes.Length -eq $treatmentReferenceNormalizedBytes -and
                (Get-TextSha256 $content) -eq $treatmentReferenceNormalizedHash) {
                $treatmentReads += [pscustomobject]@{
                    Call = $read
                    Result = $readResult[0]
                }
            }
        }
        if ($emptyLedger) {
            Assert-Condition (
                $treatmentReads.Count -eq 0 -and
                [string]$trajectory.output -match
                    '(?im)^\s*VALIDATION:\s*claim-triggered\s+\(0 candidates\)\s*$' -and
                [string]$trajectory.output -match
                    '(?im)^\s*NEW_FINDINGS:\s*none\s*$' -and
                [string]$trajectory.output -match
                    '(?im)^\s*UNRESOLVED:\s*none\s*$' -and
                [string]$trajectory.output -match
                    '(?im)^\s*DISCARDED:\s*none\s*$'
            ) 'A no-candidate treatment must skip the reference and retain the zero-candidate receipt.'
        } else {
            Assert-Condition ($treatmentReads.Count -eq 1) (
                'The subject has no single complete successful read of the claim-validation reference.'
            )
            Assert-Condition (
                [Array]::IndexOf($events, $ledgerMessages[0]) -lt
                    [Array]::IndexOf($events, $treatmentReads[0].Call) -and
                [Array]::IndexOf($events, $treatmentReads[0].Call) -lt
                    [Array]::IndexOf($events, $treatmentReads[0].Result)
            ) 'The installed claim-validation reference was not read after the candidate ledger.'
        }
    }

    $prohibitedTools = @(
        'edit',
        'create',
        'apply_patch',
        'powershell',
        'task',
        'send_session_message',
        'create_pull_request',
        'add_pr_review_comment',
        'reply_to_review_thread',
        'reply_and_resolve_review_thread'
    )
    $usedProhibited = @(
        $toolCalls |
            Where-Object { $_.data.toolName -in $prohibitedTools } |
            ForEach-Object { $_.data.toolName } |
            Sort-Object -Unique
    )
    Assert-Condition ($usedProhibited.Count -eq 0) (
        "The subject used prohibited tools: $($usedProhibited -join ', ')."
    )
    $answerPaths = @(
        foreach ($call in $toolCalls) {
            $arguments = $call.data.arguments
            if (-not $arguments) {
                continue
            }
            $toolName = ([string]$call.data.toolName).ToLowerInvariant()
            $values = [Collections.Generic.List[string]]::new()
            if ($toolName -in @('view', 'read_file') -and
                $arguments.PSObject.Properties['path']) {
                $values.Add([string]$arguments.path)
            }
            if ($toolName -in @('rg', 'glob') -and
                $arguments.PSObject.Properties['paths']) {
                foreach ($value in @($arguments.paths)) {
                    $values.Add([string]$value)
                }
            }
            if ($toolName -eq 'lsp' -and
                $arguments.PSObject.Properties['file']) {
                $values.Add([string]$arguments.file)
            }
            foreach ($value in $values) {
                $normalized = $value.Replace('\', '/').ToLowerInvariant()
                if ($normalized.Contains('.reviewer-eval/') -or
                    $normalized.Contains('reviewer-capability-labels') -or
                    $normalized.Contains('reviewer-capability-rubric')) {
                    $call
                    break
                }
            }
        }
    )
    Assert-Condition ($answerPaths.Count -eq 0) (
        'The subject trajectory contains observed access to grader-only answer material.'
    )
    Assert-OutputReceipt ([string]$trajectory.output) $cases[$caseName] (
        'The subject attempt'
    )

    if ($ResultMode -eq 'Acceptance') {
        Assert-Condition (
            $semanticGrades[0].passed -eq $true -and
            [double]$semanticGrades[0].score -eq 1 -and
            $gradeResult.passed -eq $true -and
            [double]$gradeResult.score -eq 1
        ) 'The subject attempt did not pass the automated semantic gate.'
        Assert-Condition (
            -not (($semanticGrades[0] | ConvertTo-Json -Depth 100 -Compress).
                Contains('MANUAL_ADJUDICATION_REQUIRED'))
        ) 'The semantic grader withheld automated acceptance for manual source adjudication.'
        Assert-Condition ($semanticGrades[0].metadata.model -eq $expectedJudge) (
            'The semantic grade has the wrong judge identity.'
        )
        Write-Host 'Reviewer-capability automated acceptance passed; manual source adjudication remains required.'
    } else {
        Write-Host 'Reviewer-capability result integrity passed.'
    }
}
