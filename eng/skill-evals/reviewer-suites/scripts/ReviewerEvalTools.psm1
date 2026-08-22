Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
Import-Module (Join-Path $script:RepoRoot '.github/skills/fix-challenge/scripts/ReviewArtifactTools.psm1') -Force -DisableNameChecking

$script:ReviewerEvals = @(
    (Join-Path $script:RepoRoot 'eng/skill-evals/fix-challenge/regression.vally.yaml')
    (Join-Path $script:RepoRoot 'eng/skill-evals/fix-challenge/model-guardrail.vally.yaml')
)
$script:TryFixEvals = @(
    (Join-Path $script:RepoRoot 'eng/skill-evals/try-fix/regression.vally.yaml')
)
$script:VallyPackage = '@microsoft/vally-cli@0.13.0'
$script:ModelGuardrailMechanism = 'orchestrator-model-guardrail'
$script:ModelPolicyPath = Join-Path $script:RepoRoot '.github/skills/fix-challenge/references/model-policy.v1.json'
$script:EvalGovernanceTags = @(
    'eval_id'
    'skill_name'
    'mechanism'
    'executor_model'
    'expected_runs'
    'area'
    'score_family'
    'tier'
    'provenance_kind'
    'provenance_source'
    'discovery_mode'
    'controls_positive'
    'controls_negative'
    'forbidden_prompt_terms'
    'fixture_hashes'
    'frozen_hash'
)
$script:SanitizedSourcePaths = @(
    'eng/skill-evals/fix-challenge'
    'eng/skill-evals/try-fix'
)
$script:CommonSourcePaths = @(
    '.github/instructions'
    'eng/common/AGENTS.md'
    '.editorconfig'
    '.gitignore'
    '.globalconfig'
    'Directory.Build.props'
    'Directory.Build.targets'
    'global.json'
)
$script:VallyOutputs = [ordered]@{
    'fix-challenge' = Join-Path $script:RepoRoot 'eng/skill-evals/fix-challenge/regression.vally.yaml'
    'fix-challenge-model-guardrail' = Join-Path $script:RepoRoot 'eng/skill-evals/fix-challenge/model-guardrail.vally.yaml'
    'try-fix' = Join-Path $script:RepoRoot 'eng/skill-evals/try-fix/regression.vally.yaml'
}
$script:StagedSkillFiles = [ordered]@{
    'fix-challenge' = @(
        'SKILL.md'
        'references/evidence-and-orchestration.md'
        'references/empirical-proof.md'
        'references/model-policy.v1.json'
        'references/output-contract.md'
        'references/proof-calibration.md'
        'scripts/Validate-ReviewArtifacts.ps1'
        'scripts/ReviewArtifactTools.psm1'
    )
    'try-fix' = @(
        'SKILL.md'
        'references/candidate-protocol.md'
        'references/empirical-protocol.md'
        'references/output-contract.md'
    )
}

function Get-ReviewerEvalConfiguration
{
    [CmdletBinding()]
    param()

    return @{
        RepoRoot = $script:RepoRoot
        ReviewerEvals = $script:ReviewerEvals
        TryFixEvals = $script:TryFixEvals
        VallyPackage = $script:VallyPackage
        ModelGuardrailMechanism = $script:ModelGuardrailMechanism
        ModelPolicyPath = $script:ModelPolicyPath
        SanitizedSourcePaths = $script:SanitizedSourcePaths
        CommonSourcePaths = $script:CommonSourcePaths
        VallyOutputs = $script:VallyOutputs
        StagedSkillFiles = $script:StagedSkillFiles
    }
}

function Get-HeldOutHash
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        $Eval
    )

    $copy = $Eval | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
    if ($null -ne $copy.eval_metadata.PSObject.Properties['frozen_hash'])
    {
        $copy.eval_metadata.PSObject.Properties.Remove('frozen_hash')
    }

    return Get-Sha256 -Text (ConvertTo-CanonicalJson $copy)
}

function Resolve-EvalFixture
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $EvalPath,

        [Parameter(Mandatory)]
        [string] $Fixture
    )

    if ([IO.Path]::IsPathRooted($Fixture) -and (Test-Path -LiteralPath $Fixture -PathType Leaf))
    {
        return (Resolve-Path -LiteralPath $Fixture).Path
    }

    $directory = Split-Path -Parent (Resolve-Path -LiteralPath $EvalPath)
    while (-not [string]::IsNullOrEmpty($directory))
    {
        $candidate = Join-Path $directory $Fixture
        if (Test-Path -LiteralPath $candidate -PathType Leaf)
        {
            return (Resolve-Path -LiteralPath $candidate).Path
        }

        $parent = Split-Path -Parent $directory
        if ($parent -eq $directory)
        {
            break
        }

        $directory = $parent
    }

    return $null
}

function Test-KebabCase
{
    param($Value)

    return (Test-NonEmptyString $Value) -and $Value -match '^[a-z0-9]+(?:-[a-z0-9]+)*$'
}

function Test-Integer
{
    param($Value)

    return $Value -is [sbyte] -or $Value -is [byte] -or
        $Value -is [int16] -or $Value -is [uint16] -or
        $Value -is [int32] -or $Value -is [uint32] -or
        $Value -is [int64] -or $Value -is [uint64]
}

function ConvertFrom-VallyScalar
{
    param([string] $Value)

    $value = $Value.Trim()
    if ($value.StartsWith('"'))
    {
        return $value | ConvertFrom-Json
    }
    if ($value.StartsWith("'") -and $value.EndsWith("'"))
    {
        return $value.Substring(1, $value.Length - 2).Replace("''", "'")
    }

    return $value
}

function ConvertFrom-VallyIndexList
{
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value))
    {
        return @()
    }

    return @($Value -split ',' | ForEach-Object { [int]$_ })
}

function ConvertFrom-VallyStimulus
{
    param($Stimulus)

    $tags = $Stimulus.Tags
    $idText = [string](Get-PropertyValue $tags 'eval_id')
    $id = 0
    if (-not [int]::TryParse($idText, [ref]$id))
    {
        $id = $idText
    }

    $rubric = @($Stimulus.Rubric)
    $expectedOutput = if ($rubric.Count -gt 0)
    {
        $rubric[0] -replace '^Overall response matches this expected outcome:\s*', ''
    }
    else
    {
        ''
    }
    $expectations = if ($rubric.Count -gt 1) { @($rubric[1..($rubric.Count - 1)]) } else { @() }
    $forbiddenTerms = @()
    $forbiddenJson = Get-PropertyValue $tags 'forbidden_prompt_terms'
    if (Test-NonEmptyString $forbiddenJson)
    {
        $forbiddenTerms = @($forbiddenJson | ConvertFrom-Json)
    }
    $fixtureHashes = [pscustomobject]@{}
    $fixtureHashesJson = Get-PropertyValue $tags 'fixture_hashes'
    if (Test-NonEmptyString $fixtureHashesJson)
    {
        $fixtureHashes = $fixtureHashesJson | ConvertFrom-Json
    }

    return [pscustomobject]@{
        stimulus_name = $Stimulus.Name
        id = $id
        prompt = ($Stimulus.PromptLines -join "`n").TrimEnd()
        expected_output = $expectedOutput
        files = @($Stimulus.Files)
        expectations = $expectations
        eval_metadata = [pscustomobject]@{
            mechanism = Get-PropertyValue $tags 'mechanism'
            provenance = [pscustomobject]@{
                kind = Get-PropertyValue $tags 'provenance_kind'
                source = Get-PropertyValue $tags 'provenance_source'
            }
            area = Get-PropertyValue $tags 'area'
            score_family = Get-PropertyValue $tags 'score_family'
            tier = Get-PropertyValue $tags 'tier'
            discovery_mode = Get-PropertyValue $tags 'discovery_mode'
            controls = [pscustomobject]@{
                positive = @(ConvertFrom-VallyIndexList (Get-PropertyValue $tags 'controls_positive'))
                negative = @(ConvertFrom-VallyIndexList (Get-PropertyValue $tags 'controls_negative'))
            }
            forbidden_prompt_terms = $forbiddenTerms
            fixture_hashes = $fixtureHashes
            frozen_hash = Get-PropertyValue $tags 'frozen_hash'
            skill_name = Get-PropertyValue $tags 'skill_name'
            executor_model = Get-PropertyValue $tags 'executor_model'
            expected_runs = Get-PropertyValue $tags 'expected_runs'
        }
    }
}

function Read-VallyEvalDocument
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $skillName = $null
    $defaultModel = $null
    $defaultRuns = $null
    $stimuli = [Collections.Generic.List[object]]::new()
    $current = $null
    $section = $null
    foreach ($line in Get-Content -LiteralPath $Path)
    {
        if ($null -eq $current -and $line -match '^name:\s*(.+)$')
        {
            $skillName = ConvertFrom-VallyScalar $Matches[1]
            continue
        }
        if ($null -eq $current -and $line -match '^  runs:\s*(.+)$')
        {
            $defaultRuns = [string](ConvertFrom-VallyScalar $Matches[1])
            continue
        }
        if ($null -eq $current -and $line -match '^  model:\s*(.+)$')
        {
            $defaultModel = [string](ConvertFrom-VallyScalar $Matches[1])
            continue
        }
        if ($line -match '^  - name:\s*(.+)$')
        {
            if ($null -ne $current)
            {
                $stimuli.Add((ConvertFrom-VallyStimulus $current))
            }
            $current = @{
                Name = ConvertFrom-VallyScalar $Matches[1]
                PromptLines = [Collections.Generic.List[string]]::new()
                Tags = [pscustomobject][ordered]@{}
                Files = [Collections.Generic.List[string]]::new()
                Rubric = [Collections.Generic.List[string]]::new()
            }
            $section = $null
            continue
        }
        if ($null -eq $current)
        {
            continue
        }

        if ($section -eq 'prompt')
        {
            if ([string]::IsNullOrEmpty($line))
            {
                $current.PromptLines.Add('')
                continue
            }
            if ($line.StartsWith('      '))
            {
                $current.PromptLines.Add($line.Substring(6))
                continue
            }
            $section = $null
        }

        if ($line -eq '    prompt: |-')
        {
            $section = 'prompt'
        }
        elseif ($line -eq '    tags:')
        {
            $section = 'tags'
        }
        elseif ($line -eq '    rubric:')
        {
            $section = 'rubric'
        }
        elseif ($section -eq 'tags' -and $line -match '^      ([a-z0-9_]+):\s*(.+)$')
        {
            $tagName = $Matches[1]
            if ($tagName -notin $script:EvalGovernanceTags)
            {
                throw "$Path`: unsupported stimulus governance tag '$tagName'"
            }
            $current.Tags | Add-Member -NotePropertyName $tagName -NotePropertyValue (ConvertFrom-VallyScalar $Matches[2])
        }
        elseif ($line -match '^        - src:\s*(.+)$')
        {
            $source = [string](ConvertFrom-VallyScalar $Matches[1])
            if ($source.StartsWith('../../../'))
            {
                $source = $source.Substring(9)
            }
            $current.Files.Add($source)
        }
        elseif ($section -eq 'rubric' -and $line -match '^      -\s*(.+)$')
        {
            $current.Rubric.Add([string](ConvertFrom-VallyScalar $Matches[1]))
        }
        elseif ($line -match '^    [a-z]')
        {
            $section = $null
        }
    }
    if ($null -ne $current)
    {
        $stimuli.Add((ConvertFrom-VallyStimulus $current))
    }

    return [pscustomobject]@{
        skill_name = $skillName
        default_model = $defaultModel
        default_runs = $defaultRuns
        evals = @($stimuli)
    }
}

function Get-PromptExpectationOverlap
{
    param(
        [string] $Prompt,
        [object[]] $Expectations
    )

    $promptTokens = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $expectationTokens = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($match in [regex]::Matches($Prompt.ToLowerInvariant(), '[a-z0-9][a-z0-9_-]{3,}'))
    {
        $promptTokens.Add($match.Value) | Out-Null
    }
    foreach ($match in [regex]::Matches((($Expectations -join ' ').ToLowerInvariant()), '[a-z0-9][a-z0-9_-]{3,}'))
    {
        $expectationTokens.Add($match.Value) | Out-Null
    }
    if ($promptTokens.Count -eq 0 -or $expectationTokens.Count -eq 0)
    {
        return 0.0
    }

    $intersection = 0
    foreach ($token in $expectationTokens)
    {
        if ($promptTokens.Contains($token)) { $intersection++ }
    }
    return $intersection / $expectationTokens.Count
}

function Test-EvalSuites
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]] $Paths
    )

    $errors = [Collections.Generic.List[string]]::new()
    $warnings = [Collections.Generic.List[string]]::new()
    $records = [Collections.Generic.List[object]]::new()

    foreach ($path in $Paths)
    {
        try
        {
            $document = Read-VallyEvalDocument $path
        }
        catch
        {
            $errors.Add("$path`: unable to read evals: $($_.Exception.Message)")
            continue
        }

        $evals = @(Get-PropertyValue $document 'evals')
        if (-not (Test-KebabCase $document.skill_name))
        {
            $errors.Add("$path.name must be nonempty kebab-case")
        }
        if ($evals.Count -eq 0)
        {
            $errors.Add("$path.evals must be a nonempty array")
            continue
        }

        $duplicateIds = @($evals | Group-Object id | Where-Object Count -gt 1 | ForEach-Object Name)
        if ($duplicateIds.Count -gt 0)
        {
            $errors.Add("$path.evals contains duplicate ids: $($duplicateIds -join ', ')")
        }

        for ($index = 0; $index -lt $evals.Count; $index++)
        {
            $eval = $evals[$index]
            $name = "$path`: evals[$index]"
            $id = Get-PropertyValue $eval 'id'
            $prompt = Get-PropertyValue $eval 'prompt'
            $files = @(Get-PropertyValue $eval 'files')
            $expectations = @(Get-PropertyValue $eval 'expectations')
            $metadata = Get-PropertyValue $eval 'eval_metadata'

            if (-not (Test-Integer $id) -or $id -le 0)
            {
                $errors.Add("$name.id must be a positive integer")
            }
            if (-not (Test-NonEmptyString $prompt))
            {
                $errors.Add("$name.prompt must be a nonempty string")
            }
            if ($expectations.Count -eq 0 -or @($expectations | Where-Object { -not (Test-NonEmptyString $_) }).Count -gt 0)
            {
                $errors.Add("$name.expectations must be a nonempty array of strings")
            }
            if (@($files | Where-Object { -not (Test-NonEmptyString $_) }).Count -gt 0)
            {
                $errors.Add("$name.files must contain only nonempty strings")
            }
            foreach ($fixture in $files)
            {
                if ($null -eq (Resolve-EvalFixture -EvalPath $path -Fixture $fixture))
                {
                    $errors.Add("$name.files fixture does not exist: $fixture")
                }
            }
            if ($null -eq $metadata)
            {
                $errors.Add("$name.eval_metadata must be an object")
                continue
            }

            $mechanism = Get-PropertyValue $metadata 'mechanism'
            $area = Get-PropertyValue $metadata 'area'
            $family = Get-PropertyValue $metadata 'score_family'
            $tier = Get-PropertyValue $metadata 'tier'
            $discoveryMode = Get-PropertyValue $metadata 'discovery_mode'
            $provenance = Get-PropertyValue $metadata 'provenance'
            $controls = Get-PropertyValue $metadata 'controls'
            $forbiddenTerms = @(Get-PropertyValue $metadata 'forbidden_prompt_terms')
            $taggedSkillName = Get-PropertyValue $metadata 'skill_name'
            $executorModel = Get-PropertyValue $metadata 'executor_model'
            $expectedRuns = Get-PropertyValue $metadata 'expected_runs'

            if (-not (Test-KebabCase $mechanism))
            {
                $errors.Add("$name.eval_metadata.mechanism must be nonempty kebab-case")
            }
            if (Test-Integer $id -and (Test-KebabCase $mechanism))
            {
                $expectedName = "eval-$(([int]$id).ToString('00'))-$mechanism"
                if ($eval.stimulus_name -ne $expectedName)
                {
                    $errors.Add("$name.name must be '$expectedName'")
                }
            }
            if (-not (Test-NonEmptyString $area))
            {
                $errors.Add("$name.eval_metadata.area must be a nonempty string")
            }
            if (-not (Test-KebabCase $family))
            {
                $errors.Add("$name.eval_metadata.score_family must be nonempty kebab-case")
            }
            if ($tier -notin @('train', 'held_out'))
            {
                $errors.Add("$name.eval_metadata.tier must be train or held_out")
            }
            if ($discoveryMode -notin @('discovery', 'verification'))
            {
                $errors.Add("$name.eval_metadata.discovery_mode must be discovery or verification")
            }
            if ($taggedSkillName -ne $document.skill_name)
            {
                $errors.Add("$name.tags.skill_name must match the suite name")
            }
            if (-not (Test-NonEmptyString $executorModel))
            {
                $errors.Add("$name.tags.executor_model must be a nonempty string")
            }
            elseif ($executorModel -ne $document.default_model)
            {
                $errors.Add("$name.tags.executor_model must match defaults.model")
            }
            if ($expectedRuns -notmatch '^\d+$' -or [int]$expectedRuns -le 0)
            {
                $errors.Add("$name.tags.expected_runs must be a positive integer")
            }
            elseif ($expectedRuns -ne $document.default_runs)
            {
                $errors.Add("$name.tags.expected_runs must match defaults.runs")
            }
            $provenanceKind = Get-PropertyValue $provenance 'kind'
            $provenanceSource = Get-PropertyValue $provenance 'source'
            if ($provenanceKind -notin @('pr', 'historical', 'synthetic'))
            {
                $errors.Add("$name.eval_metadata.provenance.kind must be pr, historical, or synthetic")
            }
            if (-not (Test-NonEmptyString $provenanceSource))
            {
                $errors.Add("$name.eval_metadata.provenance.source must be a nonempty string")
            }

            $positive = @(Get-PropertyValue $controls 'positive')
            $negative = @(Get-PropertyValue $controls 'negative')
            foreach ($control in @(@{ Name = 'positive'; Values = $positive }, @{ Name = 'negative'; Values = $negative }))
            {
                if ($control.Values.Count -eq 0 -or @($control.Values | Where-Object { -not (Test-Integer $_) }).Count -gt 0)
                {
                    $errors.Add("$name.eval_metadata.controls.$($control.Name) must be a nonempty integer array")
                    continue
                }
                if (@($control.Values | Sort-Object -Unique).Count -ne $control.Values.Count)
                {
                    $errors.Add("$name.eval_metadata.controls.$($control.Name) must not repeat indexes")
                }
                foreach ($value in $control.Values)
                {
                    if ($value -lt 0 -or $value -ge $expectations.Count)
                    {
                        $errors.Add("$name.eval_metadata.controls.$($control.Name) index $value must reference expectations")
                    }
                }
            }
            if (@($positive | Where-Object { $_ -in $negative }).Count -gt 0)
            {
                $errors.Add("$name.eval_metadata.controls positive and negative must be disjoint")
            }

            if (@($forbiddenTerms | Where-Object { -not (Test-NonEmptyString $_) }).Count -gt 0)
            {
                $errors.Add("$name.eval_metadata.forbidden_prompt_terms must contain only nonempty strings")
            }
            if ($discoveryMode -eq 'discovery' -and $forbiddenTerms.Count -eq 0)
            {
                $errors.Add("$name.eval_metadata.forbidden_prompt_terms must be nonempty for discovery")
            }
            if ($discoveryMode -eq 'discovery')
            {
                if ($files.Count -eq 0)
                {
                    $errors.Add("$name.files must provide a discovery fixture")
                }
                if ($prompt -match '(?i)(?:\b(?:pull request|pr|issue)\s*#?\d+|#\d{3,})' -or $prompt -match '(?i)\b(?=[0-9a-f]{7,40}\b)(?=[0-9a-f]*\d)[0-9a-f]{7,40}\b')
                {
                    $errors.Add("$name.prompt must not expose issue, pull request, or commit identities in discovery mode")
                }
            }
            foreach ($term in $forbiddenTerms)
            {
                if ($prompt.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0)
                {
                    $errors.Add("$name.eval_metadata.forbidden_prompt_terms contains prompt term: '$term'")
                }
            }

            if ($tier -eq 'held_out')
            {
                $fixtureHashes = Get-PropertyValue $metadata 'fixture_hashes'
                foreach ($fixture in $files)
                {
                    $expectedHash = Get-PropertyValue $fixtureHashes $fixture
                    if ($expectedHash -notmatch '^[0-9a-f]{64}$')
                    {
                        $errors.Add("$name.eval_metadata.fixture_hashes['$fixture'] must be a lowercase SHA-256")
                        continue
                    }
                    $fixturePath = Resolve-EvalFixture -EvalPath $path -Fixture $fixture
                    if ($null -ne $fixturePath -and (Get-Sha256 -Path $fixturePath) -ne $expectedHash)
                    {
                        $errors.Add("$name.eval_metadata.fixture_hashes['$fixture'] does not match the fixture")
                    }
                }

                $frozenHash = Get-PropertyValue $metadata 'frozen_hash'
                if ($frozenHash -notmatch '^[0-9a-f]{64}$' -or $frozenHash -ne (Get-HeldOutHash $eval))
                {
                    $errors.Add("$name.eval_metadata.frozen_hash does not match the held-out eval")
                }
            }

            $records.Add([pscustomobject]@{
                Source = $path
                SkillName = [string]$document.skill_name
                Id = [string]$id
                Tier = $tier
                Family = $family
                Provenance = "$provenanceKind`:$provenanceSource"
                Area = $area
                PromptOverlap = Get-PromptExpectationOverlap -Prompt $prompt -Expectations $expectations
            })
        }
    }

    foreach ($duplicate in $records | Group-Object SkillName, Id | Where-Object Count -gt 1)
    {
        $errors.Add("$($duplicate.Group[0].SkillName): duplicate eval id $($duplicate.Group[0].Id)")
    }

    foreach ($sourceGroup in $records | Group-Object SkillName)
    {
        $train = @($sourceGroup.Group | Where-Object Tier -eq 'train' | ForEach-Object Provenance | Sort-Object -Unique)
        $heldOut = @($sourceGroup.Group | Where-Object Tier -eq 'held_out' | ForEach-Object Provenance | Sort-Object -Unique)
        $overlap = @($train | Where-Object { $_ -in $heldOut })
        if ($overlap.Count -gt 0)
        {
            $errors.Add("$($sourceGroup.Name): train and held_out provenance must be disjoint: $($overlap -join ', ')")
        }

        $total = $sourceGroup.Count
        $heldOutCount = @($sourceGroup.Group | Where-Object Tier -eq 'held_out').Count
        if ($heldOutCount / $total -lt 0.20 -or $heldOutCount / $total -gt 0.50)
        {
            $warnings.Add("$($sourceGroup.Name): held-out share is $heldOutCount/$total; review tier balance")
        }
        foreach ($tierGroup in $sourceGroup.Group | Group-Object Tier)
        {
            $family = $tierGroup.Group | Group-Object Family | Sort-Object Count -Descending | Select-Object -First 1
            if ($family.Count / $tierGroup.Count -gt 0.50)
            {
                $warnings.Add("$($sourceGroup.Name): $($tierGroup.Name) family concentration is $($family.Name) ($($family.Count)/$($tierGroup.Count)); review diversity")
            }
        }
        $provenance = $sourceGroup.Group | Group-Object Provenance | Sort-Object Count -Descending | Select-Object -First 1
        if ($provenance.Count / $total -gt 0.50)
        {
            $warnings.Add("$($sourceGroup.Name): provenance concentration is $($provenance.Name) ($($provenance.Count)/$total); review independence")
        }
        foreach ($record in $sourceGroup.Group | Where-Object PromptOverlap -ge 0.60)
        {
            $warnings.Add("$($record.Source): eval $($record.Id) prompt/expectation term overlap is $($record.PromptOverlap.ToString('P1')); review for answer leakage")
        }
    }

    $weights = foreach ($sourceTier in $records | Group-Object SkillName, Tier)
    {
        $families = @($sourceTier.Group | Group-Object Family)
        foreach ($family in $families)
        {
            foreach ($record in $family.Group)
            {
                [pscustomobject]@{
                    source = $record.Source
                    eval_id = $record.Id
                    tier = $record.Tier
                    score_family = $record.Family
                    weight = 1.0 / ($families.Count * $family.Count)
                }
            }
        }
    }

    return [pscustomobject]@{
        Errors = @($errors)
        Warnings = @($warnings)
        Records = @($records)
        Summary = [pscustomobject]@{
            raw_count = $records.Count
            held_out_count = @($records | Where-Object Tier -eq 'held_out').Count
            family_weights = @($weights)
        }
    }
}

function Copy-SanitizedSkills
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Destination
    )

    $destinationPath = [IO.Path]::GetFullPath($Destination)
    $resolvedParent = Resolve-CanonicalDirectoryPath (Split-Path -Parent $destinationPath)
    $resolvedDestination = Normalize-DirectoryPath (Join-Path $resolvedParent (Split-Path -Leaf $destinationPath))
    if (Test-Path -LiteralPath $resolvedDestination)
    {
        $destinationItem = Get-Item -LiteralPath $resolvedDestination -Force
        if ($destinationItem.Attributes.HasFlag([IO.FileAttributes]::ReparsePoint) -or $null -ne $destinationItem.LinkType)
        {
            throw "refusing symbolic-link staging root: $resolvedDestination"
        }
        $resolvedDestination = Resolve-CanonicalDirectoryPath $resolvedDestination
    }

    $canonicalRepoRoot = Resolve-CanonicalDirectoryPath $script:RepoRoot
    $homePath = [Environment]::GetFolderPath('UserProfile')
    $forbidden = @(
        Normalize-DirectoryPath ([IO.Path]::GetPathRoot($canonicalRepoRoot))
        Resolve-CanonicalDirectoryPath $homePath
        $canonicalRepoRoot
    )
    $candidate = Normalize-DirectoryPath $resolvedDestination
    $comparison = Get-PathComparison
    if (@($forbidden | Where-Object { [string]::Equals($candidate, $_, $comparison) }).Count -gt 0 -or
        (Test-PathContainedBy -Path $candidate -Root $canonicalRepoRoot))
    {
        throw "refusing unsafe staging root: $candidate"
    }

    New-Item -ItemType Directory -Path $candidate -Force | Out-Null
    $destinations = [ordered]@{}
    foreach ($skill in $script:StagedSkillFiles.Keys)
    {
        $skillDestination = Normalize-DirectoryPath (Join-Path $candidate $skill)
        if (-not (Test-PathContainedBy -Path $skillDestination -Root $candidate))
        {
            throw "refusing staging path outside root: $skillDestination"
        }
        if (Test-Path -LiteralPath $skillDestination)
        {
            $destinationItem = Get-Item -LiteralPath $skillDestination -Force
            if ($destinationItem.Attributes.HasFlag([IO.FileAttributes]::ReparsePoint) -or $null -ne $destinationItem.LinkType)
            {
                throw "refusing symbolic-link skill destination: $skillDestination"
            }

            if ($destinationItem.Attributes.HasFlag([IO.FileAttributes]::Directory))
            {
                $skillDestination = Resolve-CanonicalDirectoryPath $skillDestination
                if (-not (Test-PathContainedBy -Path $skillDestination -Root $candidate))
                {
                    throw "refusing staging path outside root: $skillDestination"
                }
            }
        }
        $destinations[$skill] = $skillDestination
    }

    foreach ($skill in $script:StagedSkillFiles.Keys)
    {
        $skillDestination = $destinations[$skill]
        if (Test-Path -LiteralPath $skillDestination)
        {
            Remove-Item -LiteralPath $skillDestination -Recurse -Force
        }

        foreach ($relativePath in $script:StagedSkillFiles[$skill])
        {
            $source = Join-Path $script:RepoRoot ".github/skills/$skill/$relativePath"
            $destinationPath = Join-Path $skillDestination $relativePath
            New-Item -ItemType Directory -Path (Split-Path -Parent $destinationPath) -Force | Out-Null
            Copy-Item -LiteralPath $source -Destination $destinationPath
        }
    }

    return $candidate
}

function Get-Mean
{
    param([double[]] $Values)

    if ($Values.Count -eq 0)
    {
        return 0.0
    }

    return ($Values | Measure-Object -Average).Average
}

function Get-MacroAverage
{
    param(
        [object[]] $Evals,
        [hashtable] $Scores,
        [string] $Field
    )

    $groups = $Evals | Group-Object {
        if ($Field -eq 'provenance')
        {
            "$($_.eval_metadata.provenance.kind):$($_.eval_metadata.provenance.source)"
        }
        else
        {
            $_.eval_metadata.$Field
        }
    }
    $means = foreach ($group in $groups)
    {
        Get-Mean @($group.Group | ForEach-Object { [double]$Scores[[string]$_.id] })
    }

    return Get-Mean @($means)
}

function Get-EvalScoreAggregate
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        $Document,

        [Parameter(Mandatory)]
        [hashtable] $Scores
    )

    $errors = [Collections.Generic.List[string]]::new()
    $expectedIds = @($Document.evals | ForEach-Object { [string]$_.id })
    foreach ($id in $Scores.Keys)
    {
        if ($Scores[$id] -isnot [ValueType] -or [double]$Scores[$id] -lt 0 -or [double]$Scores[$id] -gt 1)
        {
            $errors.Add("score for eval $id must be numeric between 0 and 1")
        }
    }
    $missing = @($expectedIds | Where-Object { -not $Scores.ContainsKey($_) })
    $extra = @($Scores.Keys | Where-Object { $_ -notin $expectedIds })
    if ($missing.Count -gt 0) { $errors.Add("missing eval scores: $($missing -join ', ')") }
    if ($extra.Count -gt 0) { $errors.Add("unknown eval scores: $($extra -join ', ')") }
    if ($errors.Count -gt 0)
    {
        return [pscustomobject]@{ Result = $null; Errors = @($errors) }
    }

    $tiers = [ordered]@{}
    foreach ($tier in @('train', 'held_out'))
    {
        $tierEvals = @($Document.evals | Where-Object { $_.eval_metadata.tier -eq $tier })
        if ($tierEvals.Count -gt 0)
        {
            $tiers[$tier] = [ordered]@{
                eval_count = $tierEvals.Count
                raw_mean = Get-Mean @($tierEvals | ForEach-Object { [double]$Scores[[string]$_.id] })
                family_macro = Get-MacroAverage -Evals $tierEvals -Scores $Scores -Field 'score_family'
                provenance_macro = Get-MacroAverage -Evals $tierEvals -Scores $Scores -Field 'provenance'
            }
        }
    }
    $familyGap = $null
    $provenanceGap = $null
    if ($tiers.Contains('train') -and $tiers.Contains('held_out'))
    {
        $familyGap = $tiers.train.family_macro - $tiers.held_out.family_macro
        $provenanceGap = $tiers.train.provenance_macro - $tiers.held_out.provenance_macro
    }

    return [pscustomobject]@{
        Result = [ordered]@{
            raw_mean = Get-Mean @($Scores.Values | ForEach-Object { [double]$_ })
            tiers = $tiers
            transfer_gap = [ordered]@{
                family_macro = $familyGap
                provenance_macro = $provenanceGap
            }
        }
        Errors = @()
    }
}

function Test-GraderError
{
    param($Grade)

    if ($null -eq $Grade)
    {
        return $false
    }
    if ($null -ne (Get-PropertyValue (Get-PropertyValue $Grade 'metadata') 'error'))
    {
        return $true
    }
    return @((Get-PropertyValue $Grade 'details') | Where-Object { Test-GraderError $_ }).Count -gt 0
}

function Read-VallyScores
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]] $Paths,

        [string] $ExpectedSkillName
    )

    $errors = [Collections.Generic.List[string]]::new()
    $scores = @{}
    $expectedRuns = @{}
    $trajectoryStates = @{}
    $graderErrors = @{}

    foreach ($path in $Paths)
    {
        $lineNumber = 0
        foreach ($line in Get-Content -LiteralPath $path)
        {
            $lineNumber++
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            try { $outcome = $line | ConvertFrom-Json -Depth 100 }
            catch
            {
                $errors.Add("$path`:$lineNumber`: invalid JSON")
                continue
            }
            if ($outcome.type -eq 'run-summary') { continue }

            $grade = Get-PropertyValue $outcome 'gradeResult'
            $trajectory = Get-PropertyValue $outcome 'trajectory'
            $stimulus = Get-PropertyValue $trajectory 'stimulus'
            $stimulusName = Get-PropertyValue $grade 'stimulusName'
            if (-not (Test-NonEmptyString $stimulusName)) { $stimulusName = Get-PropertyValue $outcome 'stimulus' }
            if (-not (Test-NonEmptyString $stimulusName)) { $stimulusName = Get-PropertyValue $stimulus 'name' }
            if ($stimulusName -notmatch '^eval-(\d+)(?:-.+)?$')
            {
                $errors.Add("$path`:$lineNumber`: unsupported or missing stimulus name")
                continue
            }
            $id = [string][int]$Matches[1]
            if ($outcome.status -ne 'success')
            {
                $errors.Add("$path`:$lineNumber`: $stimulusName did not complete successfully")
                continue
            }
            $trajectoryId = Get-PropertyValue $trajectory 'id'
            if (-not (Test-NonEmptyString $trajectoryId))
            {
                $errors.Add("$path`:$lineNumber`: missing trajectory id")
                continue
            }

            $tags = Get-PropertyValue $stimulus 'tags'
            if (Test-NonEmptyString $ExpectedSkillName)
            {
                $taggedSkill = Get-PropertyValue $tags 'skill_name'
                $runCountText = Get-PropertyValue $tags 'expected_runs'
                $expectedModel = Get-PropertyValue $tags 'executor_model'
                if ($taggedSkill -ne $ExpectedSkillName -or $runCountText -notmatch '^\d+$' -or [int]$runCountText -le 0 -or -not (Test-NonEmptyString $expectedModel))
                {
                    $errors.Add("$path`:$lineNumber`: $stimulusName has missing or invalid Vally governance tags")
                    continue
                }
                $expectedRuns[$id] = [int]$runCountText
                if ((Get-PropertyValue (Get-PropertyValue $trajectory 'metadata') 'model') -ne $expectedModel)
                {
                    $errors.Add("$path`:$lineNumber`: $stimulusName ran with the wrong model")
                    continue
                }
                $loadedSkills = @(Get-PropertyValue (Get-PropertyValue $trajectory 'metadata') 'skillsLoaded')
                if ($ExpectedSkillName -notin $loadedSkills)
                {
                    $errors.Add("$path`:$lineNumber`: $stimulusName did not load skill '$ExpectedSkillName'")
                    continue
                }
            }

            if ($null -eq $grade)
            {
                $errors.Add("$path`:$lineNumber`: $stimulusName has no grade")
                continue
            }
            if ($trajectoryStates[$trajectoryId] -eq 'success')
            {
                $errors.Add("$path`:$lineNumber`: duplicate trajectory id '$trajectoryId'")
                continue
            }
            if (Test-GraderError $grade)
            {
                $trajectoryStates[$trajectoryId] = 'grader-error'
                $graderErrors[$trajectoryId] = "$path`:$lineNumber`: $stimulusName"
                continue
            }
            if ($trajectoryStates[$trajectoryId] -eq 'grader-error')
            {
                $graderErrors.Remove($trajectoryId)
            }
            $trajectoryStates[$trajectoryId] = 'success'
            $score = Get-PropertyValue $grade 'score'
            if ($score -isnot [ValueType] -or [double]$score -lt 0 -or [double]$score -gt 1)
            {
                $errors.Add("$path`:$lineNumber`: $stimulusName has invalid score")
                continue
            }
            if (-not $scores.ContainsKey($id)) { $scores[$id] = [Collections.Generic.List[double]]::new() }
            $scores[$id].Add([double]$score)
        }
    }

    foreach ($source in $graderErrors.Values) { $errors.Add("$source contains a grader infrastructure error") }
    foreach ($id in $expectedRuns.Keys)
    {
        $actual = if ($scores.ContainsKey($id)) { $scores[$id].Count } else { 0 }
        if ($actual -ne $expectedRuns[$id])
        {
            $errors.Add("eval $id has $actual completed trials; expected $($expectedRuns[$id])")
        }
    }
    if ($errors.Count -gt 0)
    {
        return [pscustomobject]@{ Scores = @{}; Errors = @($errors) }
    }

    $averages = @{}
    foreach ($id in $scores.Keys) { $averages[$id] = Get-Mean @($scores[$id]) }
    return [pscustomobject]@{ Scores = $averages; Errors = @() }
}

Export-ModuleMember -Function @(
    'Copy-SanitizedSkills'
    'Get-EvalScoreAggregate'
    'Get-HeldOutHash'
    'Get-ReviewerEvalConfiguration'
    'Read-VallyEvalDocument'
    'Read-VallyScores'
    'Resolve-EvalFixture'
    'Test-EvalSuites'
)
