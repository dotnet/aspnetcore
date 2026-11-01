#requires -Version 7.0
param(
    [ValidateSet('Layout', 'Discovery')]
    [string]$Mode = 'Layout',

    [ValidateSet('pr-70106', 'pr-57972')]
    [string]$Case,

    [string]$ResultsDirectory,

    [string]$ExpectedRunId,

    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$fixtureRoot = Join-Path $PSScriptRoot 'fixtures/reviewer-capability'
$baselineChecker = Join-Path $PSScriptRoot 'check_reviewer_capability.ps1'
$baselineSpec = Join-Path $PSScriptRoot 'reviewer-capability.vally.yaml'
$discoverySpec = Join-Path (
    $PSScriptRoot
) 'reviewer-capability-evidence-scout.vally.yaml'
$defaultSkill = Join-Path $repoRoot '.github/skills/review-pull-request/SKILL.md'
$discoverySkillRoot = Join-Path (
    $PSScriptRoot
) 'treatments/evidence-scout/discovery/review-pull-request'
$discoverySkill = Join-Path $discoverySkillRoot 'SKILL.md'
$finalizerSkillRoot = Join-Path (
    $PSScriptRoot
) 'treatments/evidence-scout/finalizer/review-pull-request'
$finalizerSkill = Join-Path $finalizerSkillRoot 'SKILL.md'
$finalizerReference = Join-Path (
    $finalizerSkillRoot
) 'references/evidence-scout.md'
$labelsPath = Join-Path $PSScriptRoot 'reviewer-capability-labels.json'
$rubricPath = Join-Path $PSScriptRoot 'reviewer-capability-rubric.md'

$baselineSkillBlob = 'b11fe9fe79b5d848687ba1c93362f4aaf22bfe53'
$discoverySpecSha256 = 'c5473b451bb4d9112b953dcc02303afa0dc0e40c55ec9fc28b30a550e13e6cba'
$discoverySkillSha256 = '8bf14cfbc9995421108db556d9610aa0b8cb78279ff80bfacf3e99f35c0564f5'
$discoveryNativeSkillBytes = 5577
$discoveryNativeSkillSha256 = '3c94a1d68a66b80729b103ee3596c78adcf373054af4da22e58470cb71402fad'
$finalizerSkillSha256 = '01077e03797a58e55d9748234444ecc4b458afb01bfae06a41e2314b23a5f1b2'
$finalizerReferenceSha256 = 'f5049152eabeabf90072beaea0fd5eb5c52dcce8fab6f5f5e65087a0ac94bb5e'
$discoveryPromptSha256 = '6c6958d6801d56c1f0c709520920354ed2586cd91533ee531915c27484f84986'
$expectedModel = 'gpt-5.6-sol'
$categories = @(
    'concurrency_test',
    'lifetime_or_registration',
    'producer_consumer_path',
    'pull_scope_statement',
    'resource_access',
    'resource_declaration',
    'synchronization_or_token'
)
$roles = @('baseTip', 'head', 'mergeBase')
$cases = [ordered]@{
    'pr-70106' = @{
        Pull = 70106
        Head = '2edf3761692afe15475075eb2fc0088566162045'
        MergeBase = '10bc886bfa321718276619d1077cc14eaf48456f'
        BaseTip = '4f556f73a610b386e5ec29355a300e493d30c4c5'
        DiscoveryStimulus = 'reviewer-evidence-scout-pr-70106'
        FinalizerStimulus = 'reviewer-capability-pr-70106'
        Grader = 'semantic-review-pr-70106'
        UseCommonOverlay = $true
    }
    'pr-57972' = @{
        Pull = 57972
        Head = '0a8373f036c151ae2773f4e5a1cb4aeccade6bce'
        MergeBase = '542f2b9877da67e90cca6ce1b65aa9557fbb2aa8'
        BaseTip = '542f2b9877da67e90cca6ce1b65aa9557fbb2aa8'
        DiscoveryStimulus = 'reviewer-evidence-scout-pr-57972'
        FinalizerStimulus = 'reviewer-capability-pr-57972'
        Grader = 'semantic-review-pr-57972'
        UseCommonOverlay = $false
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

    return [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData(
            [Text.Encoding]::UTF8.GetBytes($Text)
        )
    ).ToLowerInvariant()
}

function Get-NormalizedText {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Get-Content $Path -Raw).Replace("`r`n", "`n").Replace("`r", "`n")
}

function Get-NativeSkillBody {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [regex]::Replace(
        (Get-NormalizedText $Path),
        '\A---\n.*?\n---\n+',
        '',
        [Text.RegularExpressions.RegexOptions]::Singleline
    )
}

function Get-GitBlob {
    param([Parameter(Mandatory = $true)][string]$Path)

    $relative = [IO.Path]::GetRelativePath(
        $repoRoot,
        (Resolve-Path $Path).Path
    ).Replace('\', '/')
    $output = & git -C $repoRoot hash-object "--path=$relative" -- $relative 2>&1
    Assert-Condition ($LASTEXITCODE -eq 0) "git hash-object failed for '$relative'."
    return ([string]$output).Trim()
}

function Assert-Properties {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string[]]$Expected,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $actual = @($Value.PSObject.Properties.Name)
    Assert-Condition (
        $actual.Count -eq $Expected.Count -and
        ($actual -join "`n") -ceq ($Expected -join "`n")
    ) "$Description has missing, reordered, or unexpected properties."
}

function Resolve-BundleFile {
    param(
        [Parameter(Mandatory = $true)][string]$CaseName,
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    $bundle = Join-Path $fixtureRoot "cases/$CaseName/bundle"
    $path = Join-Path $bundle $RelativePath
    if (Test-Path $path -PathType Leaf) {
        return $path
    }
    if ($RelativePath.StartsWith('guidance/', [StringComparison]::Ordinal)) {
        $path = Join-Path (
            Join-Path $fixtureRoot 'common/guidance'
        ) $RelativePath.Substring('guidance/'.Length)
    } elseif ($RelativePath.StartsWith('required/', [StringComparison]::Ordinal)) {
        $path = Join-Path (
            Join-Path $fixtureRoot 'common/required'
        ) $RelativePath.Substring('required/'.Length)
    }
    Assert-Condition (Test-Path $path -PathType Leaf) (
        "Cannot resolve frozen bundle file '$RelativePath'."
    )
    return $path
}

function Get-CompleteReads {
    param(
        [Parameter(Mandatory = $true)]$Events,
        [Parameter(Mandatory = $true)][string]$Suffix,
        [Parameter(Mandatory = $true)][string]$File
    )

    $expected = Get-Content $File -Raw
    $hash = Get-TextSha256 $expected
    $bytes = [Text.Encoding]::UTF8.GetByteCount($expected)
    $suffixLower = $Suffix.Replace('\', '/').ToLowerInvariant()
    return @(
        foreach ($call in @($Events | Where-Object type -eq 'tool_call')) {
            if ($call.data.toolName -ne 'view' -or
                -not $call.data.arguments.PSObject.Properties['path'] -or
                -not $call.data.arguments.PSObject.Properties['forceReadLargeFiles'] -or
                $call.data.arguments.forceReadLargeFiles -ne $true -or
                -not ([string]$call.data.arguments.path).
                    Replace('\', '/').
                    ToLowerInvariant().
                    EndsWith($suffixLower, [StringComparison]::Ordinal)) {
                continue
            }
            $result = @(
                $Events |
                    Where-Object {
                        $_.type -eq 'tool_result' -and
                        $_.data.toolCallId -eq $call.data.toolCallId
                    }
            )
            if ($result.Count -ne 1 -or $result[0].data.success -ne $true) {
                continue
            }
            $property = $result[0].data.result.PSObject.Properties['content']
            if (-not $property -or $null -eq $property.Value) {
                continue
            }
            $content = [string]$property.Value
            if ([Text.Encoding]::UTF8.GetByteCount($content) -eq $bytes -and
                (Get-TextSha256 $content) -eq $hash) {
                [pscustomobject]@{
                    Call = $call
                    Result = $result[0]
                }
            }
        }
    )
}

function Assert-Read {
    param(
        [Parameter(Mandatory = $true)]$Events,
        [Parameter(Mandatory = $true)][string]$Suffix,
        [Parameter(Mandatory = $true)][string]$File
    )

    Assert-Condition (@(Get-CompleteReads $Events $Suffix $File).Count -gt 0) (
        "No complete read receipt exists for '$Suffix'."
    )
}

function Assert-ToolPolicy {
    param([Parameter(Mandatory = $true)]$Events)

    $prohibited = @(
        'add_pr_review_comment',
        'apply_patch',
        'create',
        'create_pull_request',
        'edit',
        'powershell',
        'read_file',
        'reply_and_resolve_review_thread',
        'reply_to_review_thread',
        'send_session_message',
        'task'
    )
    $toolCalls = @($Events | Where-Object type -eq 'tool_call')
    Assert-Condition (@(
        $toolCalls | Where-Object { $_.data.toolName -in $prohibited }
    ).Count -eq 0) 'The evidence scout used a prohibited tool.'
    foreach ($call in $toolCalls) {
        $arguments = $call.data.arguments
        if (-not $arguments) {
            continue
        }
        $values = [Collections.Generic.List[string]]::new()
        if ($call.data.toolName -in @('view', 'read_file') -and
            $arguments.PSObject.Properties['path']) {
            $values.Add([string]$arguments.path)
        }
        if ($call.data.toolName -in @('rg', 'glob') -and
            $arguments.PSObject.Properties['paths']) {
            foreach ($value in @($arguments.paths)) {
                $values.Add([string]$value)
            }
        }
        foreach ($value in $values) {
            $normalized = $value.Replace('\', '/').ToLowerInvariant()
            Assert-Condition (
                -not $normalized.Contains('.reviewer-eval/') -and
                -not $normalized.Contains('reviewer-capability-labels') -and
                -not $normalized.Contains('reviewer-capability-rubric')
            ) 'The evidence scout accessed grader-only answer material.'
        }
    }
}

function Assert-Activation {
    param([Parameter(Mandatory = $true)]$Events)

    $calls = @($Events | Where-Object type -eq 'tool_call')
    Assert-Condition (
        $calls.Count -gt 0 -and
        $calls[0].data.toolName -eq 'skill' -and
        $calls[0].data.arguments.skill -eq 'review-pull-request'
    ) 'The evidence scout did not invoke the native skill first.'
    $activation = @(
        $Events |
            Where-Object {
                $_.type -eq 'skill_activation' -and
                $_.data.name -eq 'review-pull-request'
            }
    )
    Assert-Condition ($activation.Count -eq 1) (
        'The evidence scout has missing or duplicate native activation evidence.'
    )
    $body = ([string]$activation[0].data.content).
        Replace("`r`n", "`n").
        Replace("`r", "`n")
    Assert-Condition (
        [Text.Encoding]::UTF8.GetByteCount($body) -eq
            $discoveryNativeSkillBytes -and
        (Get-TextSha256 $body) -eq $discoveryNativeSkillSha256
    ) 'The evidence scout activated the wrong native skill body.'
}

function Get-ExpectedSourcePaths {
    param(
        [Parameter(Mandatory = $true)][string]$CaseName,
        [Parameter(Mandatory = $true)]$Manifest
    )

    $root = Join-Path (
        Join-Path $fixtureRoot "cases/$CaseName/bundle"
    ) $Manifest.sources.head.root
    return @(
        Get-ChildItem $root -Recurse -File |
            ForEach-Object {
                $relative = [IO.Path]::GetRelativePath(
                    $root,
                    $_.FullName
                ).Replace('\', '/')
                $relative.Substring(0, $relative.Length - '.source'.Length)
            } |
            Sort-Object
    )
}

function Resolve-CitationFile {
    param(
        [Parameter(Mandatory = $true)][string]$CaseName,
        [Parameter(Mandatory = $true)]$Manifest,
        [Parameter(Mandatory = $true)]$Pull,
        [Parameter(Mandatory = $true)]$Citation
    )

    if ($Citation.role -eq 'pull') {
        Assert-Condition ($Citation.path -eq $Pull.body.path) (
            'A pull citation has the wrong frozen path.'
        )
        return Resolve-BundleFile $CaseName ([string]$Pull.body.path)
    }
    Assert-Condition ($Citation.role -in $roles) (
        "Unknown citation role '$($Citation.role)'."
    )
    $relative = [string]$Citation.path
    Assert-Condition (
        -not [string]::IsNullOrWhiteSpace($relative) -and
        -not [IO.Path]::IsPathRooted($relative) -and
        -not $relative.Contains('\') -and
        -not $relative.StartsWith('/', [StringComparison]::Ordinal) -and
        -not @($relative.Split('/') | Where-Object { $_ -in @('', '.', '..') })
    ) "Citation path '$relative' is not a canonical projected path."
    $root = Join-Path (
        Join-Path $fixtureRoot "cases/$CaseName/bundle"
    ) $Manifest.sources.([string]$Citation.role).root
    $root = [IO.Path]::GetFullPath($root)
    $path = [IO.Path]::GetFullPath((Join-Path $root "$relative.source"))
    Assert-Condition (
        $path.StartsWith(
            $root.TrimEnd(
                [IO.Path]::DirectorySeparatorChar,
                [IO.Path]::AltDirectorySeparatorChar
            ) + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase
        ) -and
        (Test-Path $path -PathType Leaf)
    ) (
        "Cannot resolve citation '$($Citation.role):$($Citation.path)'."
    )
    return $path
}

function Assert-Inventory {
    param(
        [Parameter(Mandatory = $true)][string]$CaseName,
        [Parameter(Mandatory = $true)][string]$Raw
    )

    Assert-Condition ([Text.Encoding]::UTF8.GetByteCount($Raw) -le 131072) (
        'The evidence inventory exceeds its byte cap.'
    )
    try {
        $value = $Raw | ConvertFrom-Json -Depth 100
    } catch {
        throw "The evidence inventory is not one JSON object: $($_.Exception.Message)"
    }
    Assert-Properties $value @(
        'schemaVersion',
        'role',
        'target',
        'completedGuides',
        'coverage',
        'evidence',
        'unresolvedReferences'
    ) 'The evidence inventory'
    Assert-Condition (
        $value.schemaVersion -eq 1 -and
        $value.role -eq 'claim-neutral-evidence-scout'
    ) 'The evidence inventory has the wrong schema role.'
    $expected = $cases[$CaseName]
    Assert-Properties $value.target @('pr', 'head', 'mergeBase', 'baseTip') (
        'The evidence inventory target'
    )
    Assert-Condition (
        $value.target.pr -eq $expected.Pull -and
        $value.target.head -eq $expected.Head -and
        $value.target.mergeBase -eq $expected.MergeBase -and
        $value.target.baseTip -eq $expected.BaseTip
    ) 'The evidence inventory has the wrong target.'

    $bundle = Join-Path $fixtureRoot "cases/$CaseName/bundle"
    $manifest = Get-Content (Join-Path $bundle 'manifest.json') -Raw |
        ConvertFrom-Json -Depth 100
    $pull = Get-Content (Join-Path $bundle 'pull.json') -Raw |
        ConvertFrom-Json -Depth 100
    $expectedGuides = @($manifest.guides.path | Sort-Object)
    Assert-Condition (
        (@($value.completedGuides) -join "`n") -ceq
        ($expectedGuides -join "`n")
    ) 'The evidence inventory has the wrong routed-guide receipt.'

    $expectedPaths = Get-ExpectedSourcePaths $CaseName $manifest
    $coverage = @($value.coverage)
    Assert-Condition ($coverage.Count -eq $expectedPaths.Count) (
        'The evidence inventory has the wrong projected-file coverage count.'
    )
    $coverageIds = @{}
    for ($index = 0; $index -lt $coverage.Count; $index++) {
        $row = $coverage[$index]
        Assert-Properties $row @(
            'path',
            'head',
            'mergeBase',
            'baseTip',
            'evidenceIds'
        ) "Coverage row $index"
        Assert-Condition (
            $row.path -ceq $expectedPaths[$index] -and
            $row.head -eq 'scanned'
        ) 'Coverage rows are not the exact sorted projected-head paths.'
        foreach ($role in @('mergeBase', 'baseTip')) {
            $root = Join-Path $bundle $manifest.sources.$role.root
            $status = if (Test-Path (
                Join-Path $root "$($row.path).source"
            ) -PathType Leaf) {
                'scanned'
            } else {
                'absent'
            }
            Assert-Condition ($row.$role -eq $status) (
                "Coverage has the wrong $role status for '$($row.path)'."
            )
        }
        $ids = @($row.evidenceIds)
        Assert-Condition (
            ($ids -join "`n") -ceq (($ids | Sort-Object -Unique) -join "`n")
        ) 'Coverage evidence IDs are not sorted and unique.'
        $coverageIds[[string]$row.path] = $ids
    }

    $evidence = @($value.evidence)
    Assert-Condition ($evidence.Count -le 64) 'The inventory exceeds 64 evidence items.'
    $citationById = @{}
    $itemSortKeys = [Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $evidence.Count; $index++) {
        $item = $evidence[$index]
        Assert-Properties $item @('id', 'category', 'symbol', 'citations') (
            "Evidence item $index"
        )
        $id = 'E{0:D3}' -f ($index + 1)
        Assert-Condition (
            $item.id -ceq $id -and
            $item.category -in $categories -and
            -not [string]::IsNullOrWhiteSpace([string]$item.symbol) -and
            ([string]$item.symbol).Length -le 200 -and
            -not ([string]$item.symbol).Contains("`n")
        ) "$id has an invalid ID, category, or exact source token."
        $citations = @($item.citations)
        Assert-Condition ($citations.Count -ge 1 -and $citations.Count -le 8) (
            "$id has the wrong citation count."
        )
        $citationSortKeys = [Collections.Generic.List[string]]::new()
        $symbolObserved = $false
        for ($citationIndex = 0; $citationIndex -lt $citations.Count; $citationIndex++) {
            $citation = $citations[$citationIndex]
            Assert-Properties $citation @(
                'id',
                'role',
                'path',
                'startLine',
                'endLine',
                'excerpt',
                'excerptSha256'
            ) "$id citation $citationIndex"
            $citationId = '{0}-C{1:D2}' -f $id, ($citationIndex + 1)
            Assert-Condition (
                $citation.id -ceq $citationId -and
                $citation.startLine -is [ValueType] -and
                $citation.endLine -is [ValueType] -and
                [long]$citation.startLine -gt 0 -and
                [long]$citation.endLine -ge [long]$citation.startLine
            ) "$citationId has an invalid ID or line range."
            $file = Resolve-CitationFile $CaseName $manifest $pull $citation
            $lines = @(Get-Content $file)
            Assert-Condition ([long]$citation.endLine -le $lines.Count) (
                "$citationId exceeds the frozen source."
            )
            $excerpt = (
                $lines[
                    ([long]$citation.startLine - 1)..
                    ([long]$citation.endLine - 1)
                ] -join "`n"
            )
            Assert-Condition (
                [string]$citation.excerpt -ceq $excerpt -and
                [string]$citation.excerptSha256 -ceq (Get-TextSha256 $excerpt)
            ) "$citationId does not bind exact frozen bytes."
            $symbolObserved = $symbolObserved -or $excerpt.Contains(
                [string]$item.symbol,
                [StringComparison]::Ordinal
            )
            $citationSortKeys.Add(
                "$($citation.role)`0$($citation.path)`0" +
                ('{0:D9}' -f [long]$citation.startLine) + "`0" +
                ('{0:D9}' -f [long]$citation.endLine)
            )
            Assert-Condition (-not $citationById.ContainsKey($citationId)) (
                "Duplicate citation ID '$citationId'."
            )
            $citationById[$citationId] = $citation
        }
        Assert-Condition (
            ($citationSortKeys -join "`n") -ceq
            (@($citationSortKeys | Sort-Object) -join "`n") -and
            $symbolObserved
        ) "$id citations are not canonical or do not contain its source token."
        $first = $citations[0]
        $itemSortKeys.Add(
            "$($first.role)`0$($first.path)`0" +
            ('{0:D9}' -f [long]$first.startLine) + "`0" +
            ('{0:D9}' -f [long]$first.endLine) + "`0" +
            "$($item.category)`0$($item.symbol)"
        )
    }
    Assert-Condition (
        ($itemSortKeys -join "`n") -ceq
        (@($itemSortKeys | Sort-Object) -join "`n")
    ) 'Evidence items are not in canonical source order.'
    foreach ($path in $expectedPaths) {
        $ids = @(
            $evidence |
                Where-Object {
                    @(
                        $_.citations |
                            Where-Object {
                                $_.role -ne 'pull' -and $_.path -ceq $path
                            }
                    ).Count -gt 0
                } |
                ForEach-Object { [string]$_.id } |
                Sort-Object -Unique
        )
        Assert-Condition (
            (@($coverageIds[$path]) -join "`n") -ceq ($ids -join "`n")
        ) "Coverage evidence IDs do not match citations for '$path'."
    }

    $unresolved = @($value.unresolvedReferences)
    Assert-Condition ($unresolved.Count -le 32) (
        'The inventory exceeds 32 unresolved references.'
    )
    $unresolvedKeys = [Collections.Generic.List[string]]::new()
    foreach ($item in $unresolved) {
        Assert-Properties $item @('symbol', 'citationIds') 'An unresolved reference'
        $ids = @($item.citationIds)
        Assert-Condition (
            -not [string]::IsNullOrWhiteSpace([string]$item.symbol) -and
            $ids.Count -gt 0 -and
            ($ids -join "`n") -ceq (($ids | Sort-Object -Unique) -join "`n")
        ) 'An unresolved reference has an invalid token or citation list.'
        $observed = $false
        foreach ($citationId in $ids) {
            Assert-Condition ($citationById.ContainsKey([string]$citationId)) (
                "Unknown unresolved-reference citation '$citationId'."
            )
            $containsSymbol = (
                [string]$citationById[[string]$citationId].excerpt
            ).Contains([string]$item.symbol, [StringComparison]::Ordinal)
            Assert-Condition $containsSymbol (
                "Unresolved-reference citation '$citationId' does not contain its token."
            )
            $observed = $observed -or $containsSymbol
        }
        Assert-Condition $observed (
            'An unresolved token is absent from its cited source.'
        )
        $unresolvedKeys.Add("$($item.symbol)`0$($ids -join ',')")
    }
    Assert-Condition (
        ($unresolvedKeys -join "`n") -ceq
        (@($unresolvedKeys | Sort-Object) -join "`n")
    ) 'Unresolved references are not in canonical order.'
    return $manifest
}

function Get-FinalizerSpecText {
    param(
        [Parameter(Mandatory = $true)][string]$CaseName,
        [Parameter(Mandatory = $true)][string]$InventoryPath
    )

    $config = $cases[$CaseName]
    $files = [Collections.Generic.List[string]]::new()
    if ($config.UseCommonOverlay) {
        $files.Add(
            "        - src: '$(Join-Path $fixtureRoot 'common/guidance')'"
        )
        $files.Add('          dest: review/bundle/guidance')
        $files.Add(
            "        - src: '$(Join-Path $fixtureRoot 'common/required')'"
        )
        $files.Add('          dest: review/bundle/required')
    }
    $files.Add(
        "        - src: '$(Join-Path $fixtureRoot "cases/$CaseName/bundle")'"
    )
    $files.Add('          dest: review/bundle')
    $files.Add(
        "        - src: '$(Join-Path $fixtureRoot "cases/$CaseName/projection-map.json")'"
    )
    $files.Add('          dest: review/bundle/projection-map.json')
    $fileBlock = $files -join "`n"
    return @"
version: 2
name: review-pull-request-capability-development
description: >
  Generated one-case opt-in treatment that gives a fresh ordinary reviewer a
  validated claim-neutral evidence inventory over the unchanged frozen source
  projection. Only the ordinary review output is semantically graded.
defaults:
  trials: 1
  timeout: 15m
  model: gpt-5.6-sol
  constraints:
    max_turns: 25
    max_duration: 12m
stimuli:
  - name: $($config.FinalizerStimulus)
    tags:
      case: $CaseName
      phase: evidence-scout-finalizer
    prompt: >
      This is an explicitly configured offline one-pass comparison. Use the
      review-pull-request skill's single-reviewer path: apply the exact routed
      guide texts and gates in this one context, do not spawn workers or
      subagents, and report PATH: single-reviewer. Review the frozen pull
      request represented by review/bundle. Treat it as the revision presented
      for review, use the included source, diff, changed-file metadata, pull
      description, existing feedback, guidance, policies, exclusions, and
      context, and report only actionable changed-line findings not already
      covered by feedback. If there are no findings, say NO_FINDINGS. Do not
      modify files or contact GitHub.
    rubric:
      - Grade the $CaseName case.
    agent_environment:
      skills:
        - '$finalizerSkillRoot'
      files:
$fileBlock
        - src: '$InventoryPath'
          dest: .reviewer-eval-input/evidence-inventory.json
    grading_environment:
      files:
        - src: '$labelsPath'
          dest: .reviewer-eval/labels.json
        - src: '$rubricPath'
          dest: .reviewer-eval/rubric.md
    graders:
      - type: prompt
        name: $($config.Grader)
        config:
          model: gemini-4-argon
          reasoning_effort: medium
          scoring: binary
          threshold: 1
          evidence:
            - trajectory
            - repo
          prompt: >
            Evaluate the review outcome for the named stimulus using the
            source-backed case label in .reviewer-eval/labels.json and the
            adjudication rules in .reviewer-eval/rubric.md. Judge the claims
            the reviewer actually made. Do not award semantic credit for
            process receipts, formatting, or tool usage. A plausible finding
            outside the label key requires manual source adjudication: return
            a binary failure and include MANUAL_ADJUDICATION_REQUIRED in the
            rationale, even when a required finding is also present.
"@
}

& $baselineChecker -SpecPath $baselineSpec
Assert-Condition ((Get-GitBlob $defaultSkill) -eq $baselineSkillBlob) (
    'The live default reviewer skill no longer matches its frozen blob.'
)
Assert-Condition (
    (Get-TextSha256 (Get-NormalizedText $discoverySpec)) -eq
        $discoverySpecSha256 -and
    (Get-Sha256 $discoverySkill) -eq $discoverySkillSha256 -and
    (Get-Sha256 $finalizerSkill) -eq $finalizerSkillSha256 -and
    (Get-Sha256 $finalizerReference) -eq $finalizerReferenceSha256
) 'The evidence-scout package does not match its frozen identities.'
$nativeBody = Get-NativeSkillBody $discoverySkill
Assert-Condition (
    [Text.Encoding]::UTF8.GetByteCount($nativeBody) -eq
        $discoveryNativeSkillBytes -and
    (Get-TextSha256 $nativeBody) -eq $discoveryNativeSkillSha256
) 'The evidence-scout native discovery body has changed.'
$finalizerText = Get-NormalizedText $finalizerSkill
$sectionStart = $finalizerText.IndexOf(
    '### Opt-in evidence-scout input',
    [StringComparison]::Ordinal
)
$sectionEnd = $finalizerText.IndexOf(
    'The coordinator requires',
    $sectionStart,
    [StringComparison]::Ordinal
)
Assert-Condition ($sectionStart -gt 0 -and $sectionEnd -gt $sectionStart) (
    'The opt-in finalizer omits its allowlisted section.'
)
Assert-Condition (
    $finalizerText.Remove($sectionStart, $sectionEnd - $sectionStart) -ceq
        (Get-NormalizedText $defaultSkill)
) 'The opt-in finalizer differs from the default beyond its allowlisted section.'
foreach ($path in @($discoverySkill, $finalizerSkill, $finalizerReference)) {
    $text = Get-Content $path -Raw
    foreach ($cue in @(
        '70106',
        '57972',
        '_operationTransformerContextCache',
        'AuthorizationMessageHandler',
        'OpenApiSchemaStore',
        'line77',
        'line-77'
    )) {
        Assert-Condition (-not $text.Contains($cue)) (
            "The subject-visible treatment contains case-specific cue '$cue'."
        )
    }
}
Assert-Condition (
    ([regex]::Matches(
        (Get-NormalizedText $discoverySpec),
        '(?m)^  - name: reviewer-evidence-scout-pr-'
    )).Count -eq 2 -and
    -not (Get-NormalizedText $discoverySpec).Contains('graders:')
) 'The discovery spec must retain exactly two ungraded stimuli.'

if ($Mode -eq 'Layout') {
    Write-Host 'Evidence-scout treatment layout and frozen identities passed.'
    return
}

Assert-Condition (
    $Case -and $ResultsDirectory -and $ExpectedRunId -and $OutputDirectory
) '-Mode Discovery requires case, results, run ID, and output directory.'
$resultFiles = @(Get-ChildItem $ResultsDirectory -Recurse -File -Filter 'results.jsonl')
Assert-Condition ($resultFiles.Count -gt 0) 'No discovery results.jsonl was found.'
$results = @(
    foreach ($file in $resultFiles) {
        Get-Content $file.FullName |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_ | ConvertFrom-Json -Depth 100 } |
            Where-Object { $_.type -eq 'trial-result' }
    }
)
Assert-Condition ($results.Count -eq 1) 'Expected exactly one scout execution.'
$result = $results[0]
$expected = $cases[$Case]
Assert-Condition (
    $result.status -eq 'success' -and
    $result.evalName -eq 'review-pull-request-evidence-scout-development' -and
    $result.model -eq $expectedModel -and
    $result.stimulus -eq $expected.DiscoveryStimulus -and
    $result.runId -eq $ExpectedRunId -and
    $null -eq $result.gradeResult -and
    [IO.Path]::GetFullPath([string]$result.evalFilePath) -eq
        [IO.Path]::GetFullPath($discoverySpec) -and
    (Get-TextSha256 ([string]$result.trajectory.stimulus.prompt)) -eq
        $discoveryPromptSha256
) 'The scout execution has the wrong status, identity, prompt, or grading state.'
$events = @($result.trajectory.events)
Assert-Activation $events
Assert-ToolPolicy $events
$manifest = Get-Content (
    Join-Path $fixtureRoot "cases/$Case/bundle/manifest.json"
) -Raw | ConvertFrom-Json -Depth 100
foreach ($relative in @('manifest.json', 'pull.json', 'diff.patch', 'files.json')) {
    Assert-Read $events "/review/bundle/$relative" (
        Resolve-BundleFile $Case $relative
    )
}
$pull = Get-Content (
    Resolve-BundleFile $Case 'pull.json'
) -Raw | ConvertFrom-Json -Depth 100
Assert-Read $events "/review/bundle/$($pull.body.path)" (
    Resolve-BundleFile $Case ([string]$pull.body.path)
)
$routingRelative = "guidance/$($manifest.routing.path).source"
Assert-Read $events "/review/bundle/$routingRelative" (
    Resolve-BundleFile $Case $routingRelative
)
foreach ($relative in @(
    $manifest.guides |
        ForEach-Object { $_.assignment.files } |
        ForEach-Object { [string]$_.path } |
        Sort-Object -Unique
)) {
    Assert-Read $events "/review/bundle/$relative" (
        Resolve-BundleFile $Case $relative
    )
}
foreach ($role in $roles) {
    $root = Join-Path (
        Join-Path $fixtureRoot "cases/$Case/bundle"
    ) $manifest.sources.$role.root
    foreach ($file in Get-ChildItem $root -Recurse -File) {
        $relative = [IO.Path]::GetRelativePath(
            $root,
            $file.FullName
        ).Replace('\', '/')
        Assert-Read $events (
            "/review/bundle/$($manifest.sources.$role.root)/$relative"
        ) $file.FullName
    }
}
Assert-Inventory $Case ([string]$result.trajectory.output) | Out-Null

New-Item $OutputDirectory -ItemType Directory -Force | Out-Null
$inventoryPath = Join-Path $OutputDirectory 'evidence-inventory.json'
[IO.File]::WriteAllText(
    $inventoryPath,
    [string]$result.trajectory.output,
    [Text.UTF8Encoding]::new($false)
)
$finalizerSpecPath = Join-Path (
    $OutputDirectory
) "reviewer-capability-evidence-scout-finalizer-$Case.vally.yaml"
[IO.File]::WriteAllText(
    $finalizerSpecPath,
    (Get-FinalizerSpecText $Case $inventoryPath),
    [Text.UTF8Encoding]::new($false)
)
$binding = [ordered]@{
    schemaVersion = 1
    case = $Case
    discoveryRunId = [string]$result.runId
    discoveryTrajectoryId = [string]$result.trajectory.id
    discoverySpecSha256 = $discoverySpecSha256
    discoverySkillSha256 = $discoverySkillSha256
    generatorPath = $PSCommandPath
    generatorSha256 = Get-Sha256 $PSCommandPath
    inventoryPath = $inventoryPath
    inventoryUtf8Bytes = (Get-Item $inventoryPath).Length
    inventorySha256 = Get-Sha256 $inventoryPath
    finalizerSkillPath = $finalizerSkillRoot
    finalizerSkillSha256 = $finalizerSkillSha256
    finalizerReferenceSha256 = $finalizerReferenceSha256
    finalizerSpecPath = $finalizerSpecPath
    finalizerSpecSha256 = Get-Sha256 $finalizerSpecPath
}
$bindingPath = Join-Path $OutputDirectory 'post-discovery-binding.json'
[IO.File]::WriteAllText(
    $bindingPath,
    ($binding | ConvertTo-Json -Depth 100),
    [Text.UTF8Encoding]::new($false)
)
Write-Host (
    "Evidence-scout discovery integrity passed. Finalizer spec: " +
    "$finalizerSpecPath; binding: $bindingPath."
)
