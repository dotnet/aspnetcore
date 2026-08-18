Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$script:SkillRoot = Split-Path -Parent $PSScriptRoot
$script:ModelPolicyPath = Join-Path $script:SkillRoot 'references/model-policy.v1.json'

function Read-JsonDocument
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -Depth 100
}

function ConvertTo-CanonicalJson
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline)]
        [AllowNull()]
        $InputObject
    )

    process
    {
        if ($null -eq $InputObject)
        {
            return 'null'
        }

        if ($InputObject -is [string])
        {
            return ConvertTo-Json -InputObject $InputObject -Compress
        }

        if ($InputObject -is [bool])
        {
            return $InputObject.ToString().ToLowerInvariant()
        }

        if ($InputObject -is [System.Collections.IDictionary])
        {
            $properties = foreach ($key in @($InputObject.Keys) | Sort-Object)
            {
                "$(ConvertTo-CanonicalJson ([string]$key)):$(ConvertTo-CanonicalJson $InputObject[$key])"
            }

            return "{$($properties -join ',')}"
        }

        if ($InputObject -is [pscustomobject])
        {
            $properties = [ordered]@{}
            foreach ($property in $InputObject.PSObject.Properties)
            {
                $properties[$property.Name] = $property.Value
            }

            return ConvertTo-CanonicalJson $properties
        }

        if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [string])
        {
            $items = foreach ($item in $InputObject)
            {
                ConvertTo-CanonicalJson $item
            }

            return "[$($items -join ',')]"
        }

        if ($InputObject -is [double] -or $InputObject -is [single] -or $InputObject -is [decimal])
        {
            return $InputObject.ToString('G', [Globalization.CultureInfo]::InvariantCulture)
        }

        return [Convert]::ToString($InputObject, [Globalization.CultureInfo]::InvariantCulture)
    }
}

function Get-Sha256
{
    [CmdletBinding(DefaultParameterSetName = 'Text')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'Text')]
        [string] $Text,

        [Parameter(Mandatory, ParameterSetName = 'Path')]
        [string] $Path
    )

    $sha = [Security.Cryptography.SHA256]::Create()
    try
    {
        if ($PSCmdlet.ParameterSetName -eq 'Path')
        {
            $stream = [IO.File]::OpenRead((Resolve-Path -LiteralPath $Path))
            try
            {
                $hash = $sha.ComputeHash($stream)
            }
            finally
            {
                $stream.Dispose()
            }
        }
        else
        {
            $hash = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text))
        }

        return [Convert]::ToHexString($hash).ToLowerInvariant()
    }
    finally
    {
        $sha.Dispose()
    }
}

function Test-NonEmptyString
{
    param($Value)

    return $Value -is [string] -and -not [string]::IsNullOrWhiteSpace($Value)
}

function Get-PropertyValue
{
    param(
        $Object,
        [string] $Name
    )

    if ($null -eq $Object)
    {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property)
    {
        return $null
    }

    return $property.Value
}

function Get-ReviewerModelPolicy
{
    [CmdletBinding()]
    param(
        [string] $Path = $script:ModelPolicyPath
    )

    return Read-JsonDocument -Path $Path
}

function Test-ReviewerModelPolicy
{
    [CmdletBinding()]
    param(
        [string] $Path = $script:ModelPolicyPath
    )

    $errors = [Collections.Generic.List[string]]::new()
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf))
    {
        return @("missing reviewer model policy: $Path")
    }

    try
    {
        $policy = Get-ReviewerModelPolicy -Path $Path
    }
    catch
    {
        return @("invalid reviewer model policy JSON: $($_.Exception.Message)")
    }

    if ((Get-PropertyValue $policy 'schema_version') -ne 'fix-challenge-model-policy/v1')
    {
        $errors.Add('model policy schema_version must be fix-challenge-model-policy/v1')
    }
    if ((Get-PropertyValue $policy 'status') -ne 'provisional')
    {
        $errors.Add('model policy status must remain provisional')
    }

    $orchestrator = Get-PropertyValue $policy 'orchestrator'
    foreach ($field in @('model', 'family', 'role', 'invocation_mode', 'reasoning_effort', 'context_tier'))
    {
        if (-not (Test-NonEmptyString (Get-PropertyValue $orchestrator $field)))
        {
            $errors.Add("model policy orchestrator requires $field")
        }
    }
    if ((Get-PropertyValue $orchestrator 'reasoning_effort') -ne 'high')
    {
        $errors.Add('model policy orchestrator reasoning_effort must remain high')
    }

    $roles = @(Get-PropertyValue $policy 'roles')
    $roleIds = @($roles | ForEach-Object { Get-PropertyValue $_ 'id' })
    if ($roles.Count -ne 4 -or @($roleIds | Sort-Object -Unique).Count -ne 4)
    {
        $errors.Add('model policy requires four unique candidate roles')
    }
    foreach ($role in $roles)
    {
        if (-not (Test-NonEmptyString (Get-PropertyValue $role 'id')) -or
            -not (Test-NonEmptyString (Get-PropertyValue $role 'focus')))
        {
            $errors.Add('model policy roles require nonempty id and focus')
        }
    }

    $matrices = Get-PropertyValue $policy 'matrices'
    $bounded = Get-PropertyValue $matrices 'bounded'
    $full = Get-PropertyValue $matrices 'full'
    $boundedVoting = @(Get-PropertyValue $bounded 'voting')
    $boundedShadow = @(Get-PropertyValue $bounded 'shadow')
    $fullVoting = @(Get-PropertyValue $full 'voting')
    $fullShadow = @(Get-PropertyValue $full 'shadow')
    if ($boundedVoting.Count -ne 2 -or $boundedShadow.Count -ne 0)
    {
        $errors.Add('model policy bounded matrix requires two voting candidates and no shadows')
    }
    if ($fullVoting.Count -ne 4 -or $fullShadow.Count -ne 1)
    {
        $errors.Add('model policy full matrix requires four voting candidates and one shadow')
    }

    $allEntries = @($fullVoting) + @($fullShadow)
    foreach ($entry in $allEntries)
    {
        foreach ($field in @('id', 'role', 'model', 'family', 'invocation_mode', 'reasoning_effort', 'context_tier'))
        {
            if (-not (Test-NonEmptyString (Get-PropertyValue $entry $field)))
            {
                $errors.Add("model policy candidate requires $field")
            }
        }
        if ((Get-PropertyValue $entry 'role') -notin $roleIds)
        {
            $errors.Add("model policy candidate has unknown role: $(Get-PropertyValue $entry 'role')")
        }
        if ((Get-PropertyValue $entry 'reasoning_effort') -ne 'high')
        {
            $errors.Add("model policy candidate reasoning_effort must remain high: $(Get-PropertyValue $entry 'id')")
        }
    }
    foreach ($entry in $fullVoting)
    {
        if ((Get-PropertyValue $entry 'voting') -ne $true)
        {
            $errors.Add("model policy voting candidate must set voting true: $(Get-PropertyValue $entry 'id')")
        }
    }
    foreach ($entry in $fullShadow)
    {
        if ((Get-PropertyValue $entry 'voting') -ne $false)
        {
            $errors.Add("model policy shadow candidate must set voting false: $(Get-PropertyValue $entry 'id')")
        }
    }

    $fullIds = @($allEntries | ForEach-Object { Get-PropertyValue $_ 'id' })
    $fullModels = @($fullVoting | ForEach-Object { Get-PropertyValue $_ 'model' })
    if (@($fullIds | Sort-Object -Unique).Count -ne $fullIds.Count)
    {
        $errors.Add('model policy candidate IDs must be unique')
    }
    if (@($fullModels | Sort-Object -Unique).Count -ne $fullModels.Count)
    {
        $errors.Add('model policy voting models must be unique')
    }
    if (@($boundedVoting | ForEach-Object { Get-PropertyValue $_ 'family' } | Sort-Object -Unique).Count -ne 2)
    {
        $errors.Add('model policy bounded candidates must use two model families')
    }
    for ($index = 0; $index -lt [Math]::Min($boundedVoting.Count, 2); $index++)
    {
        if ((ConvertTo-CanonicalJson $boundedVoting[$index]) -ne
            (ConvertTo-CanonicalJson $fullVoting[$index]))
        {
            $errors.Add("model policy bounded candidate $index must match the corresponding full candidate")
        }
    }
    if ((Get-PropertyValue $orchestrator 'model') -in $fullModels)
    {
        $errors.Add('model policy orchestrator must remain independent from routine voting candidates')
    }

    $comparison = Get-PropertyValue $policy 'comparison'
    if ((Get-PropertyValue $comparison 'configured_model_mismatch') -ne 'fail-closed' -or
        (Get-PropertyValue $comparison 'runtime_identity_without_authoritative_telemetry') -ne 'unverified' -or
        (Get-PropertyValue $comparison 'hosted_run_comparable_without_authoritative_telemetry') -ne $false)
    {
        $errors.Add('model policy comparison settings must fail closed and keep unverified hosted runs non-comparable')
    }

    $selection = Get-PropertyValue $policy 'selection_evidence'
    if ((Get-PropertyValue $selection 'source_commit') -notmatch '^[0-9a-f]{40}$' -or
        (Get-PropertyValue $selection 'trials_per_model_case') -ne 1 -or
        @(Get-PropertyValue $selection 'cases').Count -ne 5)
    {
        $errors.Add('model policy selection evidence must retain the frozen source, five cases, and one-trial limit')
    }
    $evaluatedModels = @(Get-PropertyValue $selection 'evaluated_models')
    foreach ($model in @($fullModels) + @($fullShadow | ForEach-Object { Get-PropertyValue $_ 'model' }))
    {
        if ($model -notin $evaluatedModels)
        {
            $errors.Add("model policy selected unevaluated model: $model")
        }
    }
    if (@(Get-PropertyValue $selection 'limitations').Count -lt 4)
    {
        $errors.Add('model policy selection evidence must retain material limitations')
    }

    return @($errors)
}

function Test-HostedReviewerModelEvidence
{
    param([string] $Root)

    $errors = [Collections.Generic.List[string]]::new()
    $policyPath = Join-Path $Root 'evidence/model-policy.v1.json'
    $reviewInputPath = Join-Path $Root 'evidence/review-input.json'
    $hasPolicy = Test-Path -LiteralPath $policyPath -PathType Leaf
    $hasReviewInput = Test-Path -LiteralPath $reviewInputPath -PathType Leaf
    if (-not $hasPolicy)
    {
        return @('missing required artifact: evidence/model-policy.v1.json')
    }

    foreach ($policyError in @(Test-ReviewerModelPolicy -Path $policyPath))
    {
        $errors.Add($policyError)
    }
    if ((Get-Sha256 -Path $policyPath) -ne (Get-Sha256 -Path $script:ModelPolicyPath))
    {
        $errors.Add('hosted model policy differs from the canonical policy bytes')
    }
    if (-not $hasReviewInput)
    {
        return @($errors)
    }

    try
    {
        $policy = Get-ReviewerModelPolicy -Path $policyPath
        $reviewInput = Read-JsonDocument -Path $reviewInputPath
    }
    catch
    {
        $errors.Add("invalid hosted model evidence JSON: $($_.Exception.Message)")
        return @($errors)
    }

    $policyRecord = Get-PropertyValue $reviewInput 'model_policy'
    if ((Get-PropertyValue $policyRecord 'version') -ne (Get-PropertyValue $policy 'policy_version') -or
        (Get-PropertyValue $policyRecord 'sha256') -ne (Get-Sha256 -Path $policyPath))
    {
        $errors.Add('hosted review input model-policy identity does not match retained policy bytes')
    }

    $panel = Get-PropertyValue $reviewInput 'panel'
    if ((Get-PropertyValue $panel 'status') -ne 'policy-pinned' -or
        (Get-PropertyValue $panel 'comparable') -ne $false -or
        (Get-PropertyValue $panel 'runtime_identity') -ne 'unverified')
    {
        $errors.Add('hosted panel must remain policy-pinned, runtime-unverified, and non-comparable')
    }
    $matrix = Get-PropertyValue (Get-PropertyValue $policy 'matrices') (Get-PropertyValue $panel 'path')
    if ((ConvertTo-CanonicalJson (Get-PropertyValue $panel 'candidates')) -ne
        (ConvertTo-CanonicalJson (Get-PropertyValue $matrix 'voting')) -or
        (ConvertTo-CanonicalJson (Get-PropertyValue $panel 'shadows')) -ne
        (ConvertTo-CanonicalJson (Get-PropertyValue $matrix 'shadow')) -or
        (ConvertTo-CanonicalJson (Get-PropertyValue $panel 'orchestrator')) -ne
        (ConvertTo-CanonicalJson (Get-PropertyValue $policy 'orchestrator')))
    {
        $errors.Add('hosted panel configuration does not match the retained model policy')
    }

    return @($errors)
}

function Normalize-DirectoryPath
{
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $fullPath = [IO.Path]::GetFullPath($Path)
    $root = [IO.Path]::GetPathRoot($fullPath)
    if ($fullPath.Length -eq $root.Length)
    {
        return $root
    }

    return $fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Resolve-CanonicalDirectoryPath
{
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Collections.Generic.HashSet[string]] $Visited
    )

    $fullPath = Normalize-DirectoryPath $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container))
    {
        throw "directory does not exist: $fullPath"
    }

    if ($null -eq $Visited)
    {
        $comparer = if ([OperatingSystem]::IsWindows() -or [OperatingSystem]::IsMacOS())
        {
            [StringComparer]::OrdinalIgnoreCase
        }
        else
        {
            [StringComparer]::Ordinal
        }
        $Visited = [Collections.Generic.HashSet[string]]::new($comparer)
    }

    if (-not $Visited.Add($fullPath))
    {
        throw "symbolic-link cycle detected while resolving: $fullPath"
    }

    try
    {
        $root = [IO.Path]::GetPathRoot($fullPath)
        $current = Get-Item -LiteralPath $root -Force
        $relativePath = [IO.Path]::GetRelativePath($root, $fullPath)
        if ($relativePath -eq '.')
        {
            return Normalize-DirectoryPath $current.FullName
        }

        foreach ($segment in $relativePath.Split(
            [char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar),
            [StringSplitOptions]::RemoveEmptyEntries))
        {
            $item = Get-Item -LiteralPath (Join-Path $current.FullName $segment) -Force
            if ($item.Attributes.HasFlag([IO.FileAttributes]::ReparsePoint) -or $null -ne $item.LinkType)
            {
                $target = $item.ResolveLinkTarget($true)
                if ($null -eq $target)
                {
                    throw "unable to resolve symbolic-link path component: $($item.FullName)"
                }

                $canonicalTarget = Resolve-CanonicalDirectoryPath -Path $target.FullName -Visited $Visited
                $item = Get-Item -LiteralPath $canonicalTarget -Force
            }

            if (-not $item.Attributes.HasFlag([IO.FileAttributes]::Directory))
            {
                throw "path component is not a directory: $($item.FullName)"
            }

            $current = $item
        }

        return Normalize-DirectoryPath $current.FullName
    }
    finally
    {
        $Visited.Remove($fullPath) | Out-Null
    }
}

function Resolve-CanonicalFilePath
{
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf))
    {
        throw "file does not exist: $fullPath"
    }

    $canonicalParent = Resolve-CanonicalDirectoryPath (Split-Path -Parent $fullPath)
    $item = Get-Item -LiteralPath (Join-Path $canonicalParent (Split-Path -Leaf $fullPath)) -Force
    if ($item.Attributes.HasFlag([IO.FileAttributes]::ReparsePoint) -or $null -ne $item.LinkType)
    {
        $target = $item.ResolveLinkTarget($true)
        if ($null -eq $target)
        {
            throw "unable to resolve symbolic-link file: $($item.FullName)"
        }
        $item = $target
    }

    if ($item.Attributes.HasFlag([IO.FileAttributes]::Directory))
    {
        throw "path is not a file: $($item.FullName)"
    }

    return [IO.Path]::GetFullPath($item.FullName)
}

function Get-PathComparison
{
    if ([OperatingSystem]::IsWindows() -or [OperatingSystem]::IsMacOS())
    {
        return [StringComparison]::OrdinalIgnoreCase
    }

    return [StringComparison]::Ordinal
}

function Test-PathContainedBy
{
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $Root,

        [switch] $AllowEqual
    )

    $candidate = Normalize-DirectoryPath $Path
    $container = Normalize-DirectoryPath $Root
    $comparison = Get-PathComparison
    if ([string]::Equals($candidate, $container, $comparison))
    {
        return $AllowEqual.IsPresent
    }

    $boundary = if ($container.EndsWith([IO.Path]::DirectorySeparatorChar))
    {
        $container
    }
    else
    {
        "$container$([IO.Path]::DirectorySeparatorChar)"
    }

    return $candidate.StartsWith($boundary, $comparison)
}

function Test-ReviewArtifacts
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Root
    )

    $errors = [Collections.Generic.List[string]]::new()
    foreach ($modelError in @(Test-HostedReviewerModelEvidence -Root $Root))
    {
        $errors.Add($modelError)
    }
    $reviewPath = Join-Path $Root 'final/review.md'
    $declaredPath = $null
    if (-not (Test-Path -LiteralPath $reviewPath -PathType Leaf))
    {
        $errors.Add('missing required artifact: final/review.md')
    }
    else
    {
        $reviewContent = Get-Content -LiteralPath $reviewPath -Raw
        if ([string]::IsNullOrWhiteSpace($reviewContent))
        {
            $errors.Add('required artifact is empty: final/review.md')
        }
        else
        {
            $pathMatches = [regex]::Matches($reviewContent, '(?m)^\*\*Path:\*\*\s*(.+?)\s*$')
            if ($pathMatches.Count -eq 0)
            {
                $errors.Add('final review missing marker: **Path:**')
            }
            elseif ($pathMatches.Count -gt 1)
            {
                $errors.Add('final review contains duplicate marker: **Path:**')
            }
            else
            {
                $candidatePath = $pathMatches[0].Groups[1].Value.Trim().ToLowerInvariant()
                if ($candidatePath -notin @('bounded', 'full'))
                {
                    $errors.Add("invalid calibrated value for Path: $candidatePath")
                }
                else
                {
                    $declaredPath = $candidatePath
                }
            }
        }
    }

    $requiredNonEmpty = [Collections.Generic.List[string]]::new()
    @(
        'evidence/manifest.md', 'evidence/product-oracle.md', 'evidence/head-drift.md',
        'evidence/impact-map.md', 'candidates/candidate-a.md', 'candidates/candidate-b.md',
        'final/repository-oracle.md', 'final/review.md'
    ) | ForEach-Object { $requiredNonEmpty.Add($_) }
    $requiredExisting = [Collections.Generic.List[string]]::new()
    $requiredExisting.Add('evidence/tracked.diff')

    if ($declaredPath -eq 'bounded')
    {
        $requiredNonEmpty.Add('evidence/skipped-phases.md')
    }
    elseif ($declaredPath -eq 'full')
    {
        @(
            'candidates/candidate-c.md', 'candidates/candidate-d.md',
            'cross-examination/candidate-a.md', 'cross-examination/candidate-b.md',
            'cross-examination/candidate-c.md', 'cross-examination/candidate-d.md',
            'empirical/manifest.md', 'empirical/head.log', 'empirical/claim-matrix.md',
            'empirical/boundary-matrix.md', 'empirical/stress-matrix.md',
            'empirical/result.md'
        ) | ForEach-Object { $requiredNonEmpty.Add($_) }
        @(
            'empirical/before.diff', 'empirical/diagnostic.diff',
            'empirical/implementation.diff', 'empirical/red.log',
            'empirical/candidate.diff', 'empirical/green.log'
        ) | ForEach-Object { $requiredExisting.Add($_) }
    }

    foreach ($relativePath in $requiredNonEmpty)
    {
        $path = Join-Path $Root $relativePath
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $errors.Add("missing required artifact: $relativePath") }
        elseif ([string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $path -Raw))) { $errors.Add("required artifact is empty: $relativePath") }
    }
    foreach ($relativePath in $requiredExisting)
    {
        if (-not (Test-Path -LiteralPath (Join-Path $Root $relativePath) -PathType Leaf)) { $errors.Add("missing required artifact: $relativePath") }
    }

    $impactPath = Join-Path $Root 'evidence/impact-map.md'
    if (Test-Path -LiteralPath $impactPath -PathType Leaf)
    {
        $impact = Get-Content -LiteralPath $impactPath -Raw
        $authorityMatches = [regex]::Matches($impact, '(?m)^\*\*Authority-handoff mapping:\*\*\s*(.+?)\s*$')
        if ($authorityMatches.Count -eq 0)
        {
            $errors.Add('impact map missing marker: **Authority-handoff mapping:**')
        }
        elseif ($authorityMatches.Count -gt 1)
        {
            $errors.Add('impact map contains duplicate marker: **Authority-handoff mapping:**')
        }
        else
        {
            $authorityDisposition = $authorityMatches[0].Groups[1].Value.Trim()
            if ($authorityDisposition -eq 'required')
            {
                $sections = [regex]::Matches($impact, '(?ms)^## Authority handoffs\s*(.*?)(?=^## |\z)')
                if ($sections.Count -ne 1)
                {
                    $errors.Add('required authority mapping needs exactly one Authority handoffs section')
                }
                else
                {
                    $lines = @($sections[0].Groups[1].Value -split "`r?`n" | Where-Object { $_.Trim().StartsWith('|') })
                    $expectedHeader = '| Stage/handoff | Input authority | Effective authority | Transformation | Downstream observable | Governing contract | Disagreement risk |'
                    $expectedSeparator = '|---|---|---|---|---|---|---|'
                    if ($lines.Count -lt 3 -or $lines[0].Trim() -ne $expectedHeader -or $lines[1].Trim() -ne $expectedSeparator)
                    {
                        $errors.Add('required authority mapping needs the canonical seven-column table')
                    }
                    else
                    {
                        $dataRows = @($lines[2..($lines.Count - 1)])
                        $invalidRows = @($dataRows | Where-Object {
                            $cells = @($_.Trim().Trim('|') -split '\|' | ForEach-Object { $_.Trim() })
                            $cells.Count -ne 7 -or
                                @($cells | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0 -or
                                $cells[0] -in @('Stage/handoff', '---')
                        })
                        if ($dataRows.Count -eq 0 -or $invalidRows.Count -gt 0)
                        {
                            $errors.Add('required authority mapping needs only complete, nonduplicate handoff rows')
                        }
                    }
                }
            }
            elseif ($authorityDisposition -notmatch '^not applicable\s*-\s*\S.+;\s*source:\s*\S.+$')
            {
                $errors.Add('authority mapping must be required or a justified not-applicable disposition with a source')
            }
        }
    }

    if (-not (Test-Path -LiteralPath $reviewPath -PathType Leaf)) { return @($errors) }
    $content = Get-Content -LiteralPath $reviewPath -Raw
    foreach ($heading in @(
        '# Multi-Model Review',
        '## Current fix',
        '## Independent candidates',
        '## Adversarial consensus',
        '## Test assessment',
        '## Implementation selection',
        '## Proof status',
        '## Final recommendation',
        '## Required follow-ups',
        '## Repository oracle gaps',
        '## Suggested review comments'
    ))
    {
        $matches = [regex]::Matches($content, "(?m)^$([regex]::Escape($heading))\s*$")
        if ($matches.Count -eq 0) { $errors.Add("final review missing marker: $heading") }
        elseif ($matches.Count -gt 1) { $errors.Add("final review contains duplicate marker: $heading") }
    }

    $orchestratorMatches = [regex]::Matches($content, '(?m)^\*\*Orchestrator:\*\*\s*(.+?)\s*$')
    if ($orchestratorMatches.Count -eq 0)
    {
        $errors.Add('final review missing marker: **Orchestrator:**')
    }
    elseif ($orchestratorMatches.Count -gt 1)
    {
        $errors.Add('final review contains duplicate marker: **Orchestrator:**')
    }
    else
    {
        $orchestrator = $orchestratorMatches[0].Groups[1].Value.Trim()
        if ($orchestrator -notmatch '(?i)^gpt(?:-|$)')
        {
            $errors.Add("final review orchestrator must be GPT-family: $orchestrator")
        }
    }

    $labels = [ordered]@{
        'Review goal' = @('defect-adjudication', 'solution-selection')
        'Panel provenance' = @('policy-pinned')
        'Comparable run' = @('no')
        'Candidate runtime identity' = @('unverified')
        'Frozen-head result' = @('behavioral-fail', 'structural-defect', 'pass', 'blocked', 'not-applicable')
        'Finding proof' = @('empirical', 'structural', 'missing')
        'Scenario proof' = @('empirical', 'structural', 'missing')
        'Candidate proof' = @('production-proven', 'targeted-proven', 'diagnostic-only', 'rejected', 'blocked', 'none')
        'Changed path execution' = @('demonstrated', 'structural', 'blocked', 'missing', 'not-applicable')
        'Final observable' = @('inspected', 'structural', 'blocked', 'missing', 'not-applicable')
        'Boundary controls' = @('passed', 'partial', 'blocked', 'missing', 'not-applicable')
        'Pre-existing disposition' = @('same-path-same-behavior', 'not-pre-existing', 'unresolved', 'not-applicable')
        'Changed reachability' = @('newly-reachable', 'multiplicity-altered', 'unchanged', 'unresolved', 'not-applicable')
        'Multiplicity oracle' = @('requires-unique', 'permits-multiple', 'accepted-exception', 'unresolved', 'not-applicable')
        'Multiplicity evidence' = @('duplicate-observed', 'single-observed', 'masked', 'missing', 'not-applicable')
        'Multiplicity disposition' = @('blocker', 'unresolved', 'harmless', 'not-applicable')
        'Product oracle' = @('documented', 'author-confirmed', 'test-encoded', 'inferred', 'unknown')
        'Oracle fidelity' = @('authoritative', 'corroborated', 'hypothesis', 'unknown')
        'Mechanism fidelity' = @('reproduced', 'structural', 'inferred', 'unknown')
        'Scenario fidelity' = @('exact', 'proxy', 'synthetic', 'missing')
        'Regression assertion disposition' = @('required-regression', 'optional-regression', 'rejected')
        'Diagnostic mutation disposition' = @('diagnostic-only', 'rejected', 'not-applicable')
        'Selection status' = @('not-requested', 'unadjudicated', 'compared', 'preferred')
        'Alternative closure' = @('not-required', 'open', 'structural', 'empirical')
        'Implementation verdict' = @('keep current fix', 'revise', 'replace')
        'Behavioral evidence' = @('empirical', 'structural', 'missing')
        'Merge readiness' = @('ready', 'recommendation only', 'blocked on evidence', 'blocked on product oracle', 'blocked on implementation')
        'Implementation confidence' = @('high', 'medium', 'low')
    }
    $values = @{}
    foreach ($label in $labels.Keys)
    {
        $matches = [regex]::Matches($content, "(?m)^\*\*$([regex]::Escape($label)):\*\*\s*(.+?)\s*$")
        if ($matches.Count -eq 0) { $errors.Add("final review missing marker: **$label`:**"); continue }
        if ($matches.Count -gt 1) { $errors.Add("final review contains duplicate marker: **$label`:**"); continue }
        $value = $matches[0].Groups[1].Value.Trim().ToLowerInvariant()
        $values[$label] = $value
        if ($value -notin $labels[$label]) { $errors.Add("invalid calibrated value for $label`: $value") }
    }

    if ($values.Count -eq $labels.Count)
    {
        $freeform = @{}
        foreach ($label in @('Proof candidate', 'Preferred production candidate'))
        {
            $matches = [regex]::Matches($content, "(?m)^\*\*$([regex]::Escape($label)):\*\*\s*(.+?)\s*$")
            if ($matches.Count -eq 0)
            {
                $errors.Add("final review missing marker: **$label`:**")
                continue
            }
            if ($matches.Count -gt 1)
            {
                $errors.Add("final review contains duplicate marker: **$label`:**")
                continue
            }
            $freeform[$label] = $matches[0].Groups[1].Value.Trim()
        }

        $weak = $values['Oracle fidelity'] -in @('hypothesis', 'unknown') -or
            $values['Mechanism fidelity'] -in @('inferred', 'unknown') -or
            $values['Scenario fidelity'] -in @('synthetic', 'missing')
        $provenHead = $values['Frozen-head result'] -in @('behavioral-fail', 'structural-defect')
        $proofMatches = ($values['Frozen-head result'] -eq 'behavioral-fail' -and $values['Finding proof'] -eq 'empirical' -and $values['Scenario proof'] -eq 'empirical') -or
            ($values['Frozen-head result'] -eq 'structural-defect' -and $values['Finding proof'] -in @('empirical', 'structural') -and $values['Scenario proof'] -in @('empirical', 'structural'))
        if ($values['Merge readiness'] -eq 'blocked on implementation' -and ($weak -or -not $provenHead -or -not $proofMatches))
        {
            $errors.Add('blocked on implementation requires a proven frozen-head defect and stronger oracle, mechanism, scenario, and finding proof')
        }
        if ($values['Merge readiness'] -eq 'blocked on implementation')
        {
            if ($values['Boundary controls'] -notin @('passed', 'partial'))
            {
                $errors.Add('blocked on implementation requires declared boundary evidence')
            }
            if ($values['Frozen-head result'] -eq 'behavioral-fail')
            {
                foreach ($relativePath in @('empirical/boundary-matrix.md', 'empirical/result.md'))
                {
                    $path = Join-Path $Root $relativePath
                    if (
                        -not (Test-Path -LiteralPath $path -PathType Leaf) -or
                        [string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $path -Raw))
                    )
                    {
                        $errors.Add("behavioral implementation blocker missing declared evidence artifact: $relativePath")
                    }
                }
            }
        }
        if ($values['Implementation confidence'] -eq 'high' -and $weak) { $errors.Add('high confidence is incompatible with weak oracle, mechanism, or scenario fidelity') }
        if ($values['Candidate proof'] -eq 'diagnostic-only' -and $values['Implementation confidence'] -eq 'high') { $errors.Add('diagnostic-only candidate proof is incompatible with high confidence') }
        if ($values['Candidate proof'] -eq 'diagnostic-only' -and $values['Merge readiness'] -eq 'ready') { $errors.Add('diagnostic-only candidate proof is incompatible with ready') }
        if ($values['Review goal'] -eq 'defect-adjudication' -and $values['Selection status'] -in @('compared', 'preferred'))
        {
            $errors.Add('compared or preferred implementation selection requires the solution-selection review goal')
        }
        if ($values['Review goal'] -eq 'solution-selection' -and $values['Selection status'] -eq 'not-requested')
        {
            $errors.Add('solution-selection review goal cannot use not-requested selection status')
        }
        if ($values['Selection status'] -eq 'not-requested' -and $values['Alternative closure'] -ne 'not-required')
        {
            $errors.Add('not-requested selection requires not-required alternative closure')
        }
        if ($values['Selection status'] -eq 'unadjudicated' -and $values['Alternative closure'] -ne 'open')
        {
            $errors.Add('unadjudicated selection requires open alternative closure')
        }
        if ($values['Selection status'] -in @('compared', 'preferred') -and $values['Alternative closure'] -notin @('structural', 'empirical'))
        {
            $errors.Add('compared or preferred selection requires structural or empirical alternative closure')
        }
        if ($freeform.Count -eq 2)
        {
            $preferredCandidate = $freeform['Preferred production candidate'].ToLowerInvariant()
            if ($values['Selection status'] -eq 'preferred')
            {
                if ($preferredCandidate -eq 'none')
                {
                    $errors.Add('preferred selection requires a named preferred production candidate')
                }
                if ($freeform['Proof candidate'].ToLowerInvariant() -eq 'none')
                {
                    $errors.Add('preferred selection requires a named proof candidate')
                }
                if ($values['Candidate proof'] -notin @('targeted-proven', 'production-proven'))
                {
                    $errors.Add('preferred selection requires a proven proof candidate')
                }
            }
            elseif ($preferredCandidate -ne 'none')
            {
                $errors.Add('non-preferred selection must not name a preferred production candidate')
            }
        }

        if ($values['Selection status'] -in @('compared', 'preferred'))
        {
            $selectionPath = Join-Path $Root 'final/implementation-selection.md'
            if (-not (Test-Path -LiteralPath $selectionPath -PathType Leaf))
            {
                $errors.Add('compared or preferred selection missing required artifact: final/implementation-selection.md')
            }
            else
            {
                $selection = Get-Content -LiteralPath $selectionPath -Raw
                if ($selection -notmatch '(?m)^# Implementation Selection[ \t]*$')
                {
                    $errors.Add('implementation selection missing marker: # Implementation Selection')
                }
                if ($selection -notmatch '(?m)^## Candidate comparison[ \t]*$')
                {
                    $errors.Add('implementation selection missing marker: ## Candidate comparison')
                }
                foreach ($marker in @('Shared comparison contract', 'Pre-change base'))
                {
                    if ($selection -notmatch "(?m)^\*\*$([regex]::Escape($marker)):\*\*[ \t]+\S.*$")
                    {
                        $errors.Add("implementation selection missing nonempty marker: **$marker`:**")
                    }
                }

                $sections = [regex]::Matches($selection, '(?ms)^## Candidate comparison\s*(.*?)(?=^## |\z)')
                $rows = @()
                if ($sections.Count -eq 1)
                {
                    $rows = @($sections[0].Groups[1].Value -split "`r?`n" | Where-Object { $_.Trim().StartsWith('|') })
                }
                $expectedHeader = '| Candidate | Mechanism | Literal result | Refinement | Equal-matrix result | Net surface | Caller compatibility | Closure |'
                $expectedSeparator = '|---|---|---|---|---|---|---|---|'
                if ($rows.Count -lt 4 -or $rows[0].Trim() -ne $expectedHeader -or $rows[1].Trim() -ne $expectedSeparator)
                {
                    $errors.Add('implementation selection requires the canonical comparison table with at least two candidates')
                }
                else
                {
                    $dataRows = @($rows[2..($rows.Count - 1)])
                    $candidateRows = [Collections.Generic.List[object]]::new()
                    $invalidRows = [Collections.Generic.List[string]]::new()
                    foreach ($row in $dataRows)
                    {
                        $cells = @($row.Trim().Trim('|') -split '\|' | ForEach-Object { $_.Trim() })
                        if (
                            $cells.Count -ne 8 -or
                            @($cells | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0 -or
                            $cells[0] -in @('Candidate', '---') -or
                            $cells[3].ToLowerInvariant() -notin @('not-applicable', 'bounded-refinement', 'fundamental', 'unresolved') -or
                            $cells[4].ToLowerInvariant() -notin @('passed', 'failed', 'not-run', 'blocked', 'not-applicable') -or
                            $cells[7].ToLowerInvariant() -notin @('open', 'structural', 'empirical')
                        )
                        {
                            $invalidRows.Add($row)
                            continue
                        }
                        $candidateRows.Add([pscustomobject]@{
                            Candidate = $cells[0]
                            Refinement = $cells[3].ToLowerInvariant()
                            EqualMatrixResult = $cells[4]
                            Closure = $cells[7].ToLowerInvariant()
                        })
                    }
                    $uniqueCandidates = @($candidateRows.Candidate | ForEach-Object { $_.ToLowerInvariant() } | Sort-Object -Unique)
                    if ($dataRows.Count -lt 2 -or $invalidRows.Count -gt 0 -or $uniqueCandidates.Count -lt 2)
                    {
                        $errors.Add('implementation selection requires complete rows for at least two distinct candidates')
                    }
                    elseif ($freeform.Count -eq 2)
                    {
                        foreach ($candidateRow in $candidateRows)
                        {
                            if ($candidateRow.Closure -eq 'structural' -and $candidateRow.Refinement -ne 'fundamental')
                            {
                                $errors.Add('structural candidate closure requires a fundamental refinement disposition')
                            }
                            if (
                                $candidateRow.Closure -eq 'empirical' -and
                                $candidateRow.EqualMatrixResult -notin @('passed', 'failed')
                            )
                            {
                                $errors.Add('empirical candidate closure requires a passed or failed equal-matrix result')
                            }
                        }

                        $proofCandidate = $freeform['Proof candidate']
                        $preferredCandidate = $freeform['Preferred production candidate']
                        if (
                            $proofCandidate.ToLowerInvariant() -ne 'none' -and
                            @($candidateRows | Where-Object { $_.Candidate -ieq $proofCandidate }).Count -ne 1
                        )
                        {
                            $errors.Add('proof candidate must identify exactly one implementation selection row')
                        }
                        $comparisonRows = if ($values['Selection status'] -eq 'preferred')
                        {
                            @($candidateRows | Where-Object { $_.Candidate -ine $preferredCandidate })
                        }
                        else
                        {
                            @($candidateRows)
                        }
                        if (@($comparisonRows | Where-Object Closure -eq 'open').Count -gt 0)
                        {
                            $errors.Add("$($values['Selection status']) selection cannot retain an open alternative candidate")
                        }
                        if (@($comparisonRows | Where-Object Closure -eq $values['Alternative closure']).Count -eq 0)
                        {
                            $errors.Add('declared alternative closure must match a compared candidate row')
                        }
                        if ($values['Selection status'] -eq 'preferred')
                        {
                            $preferredRows = @($candidateRows | Where-Object { $_.Candidate -ieq $preferredCandidate })
                            if ($preferredRows.Count -ne 1)
                            {
                                $errors.Add('preferred production candidate must identify exactly one implementation selection row')
                            }
                            else
                            {
                                if ($preferredRows[0].EqualMatrixResult -ne 'passed')
                                {
                                    $errors.Add('preferred production candidate requires a passed equal-matrix result')
                                }
                                if ($proofCandidate -ine $preferredCandidate)
                                {
                                    if ($values['Alternative closure'] -ne 'empirical')
                                    {
                                        $errors.Add('a preferred candidate distinct from the proof candidate requires empirical alternative closure')
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        if (
            $values['Pre-existing disposition'] -eq 'same-path-same-behavior' -and
            $values['Changed reachability'] -in @('newly-reachable', 'multiplicity-altered')
        )
        {
            $errors.Add('pre-existing same-path disposition cannot coexist with newly reachable or altered multiplicity')
        }
        if (
            $values['Multiplicity evidence'] -eq 'duplicate-observed' -and
            $values['Multiplicity disposition'] -eq 'harmless' -and
            $values['Multiplicity oracle'] -notin @('permits-multiple', 'accepted-exception')
        )
        {
            $errors.Add('observed duplicate execution cannot be harmless without an explicit permitting oracle disposition')
        }
        if (
            $values['Multiplicity evidence'] -eq 'masked' -and
            $values['Multiplicity oracle'] -eq 'requires-unique' -and
            $values['Multiplicity disposition'] -eq 'harmless'
        )
        {
            $errors.Add('masked multiplicity under a uniqueness oracle must remain unresolved')
        }
        if ($values['Multiplicity disposition'] -eq 'blocker')
        {
            if ($values['Implementation verdict'] -eq 'keep current fix')
            {
                $errors.Add('multiplicity blocker is incompatible with keeping the current fix')
            }
            if ($values['Merge readiness'] -ne 'blocked on implementation')
            {
                $errors.Add('multiplicity blocker requires blocked on implementation readiness')
            }
            if ($values['Multiplicity oracle'] -ne 'requires-unique')
            {
                $errors.Add('multiplicity blocker requires an explicit uniqueness oracle')
            }
            if ($values['Multiplicity evidence'] -ne 'duplicate-observed')
            {
                $errors.Add('multiplicity blocker requires observed duplicate execution')
            }
            if ($values['Changed reachability'] -notin @('newly-reachable', 'multiplicity-altered'))
            {
                $errors.Add('multiplicity blocker requires newly reachable or altered multiplicity')
            }
            if (-not $provenHead -or $values['Boundary controls'] -notin @('passed', 'partial'))
            {
                $errors.Add('multiplicity blocker requires declared frozen-head defect and boundary evidence')
            }
        }
        if ($values['Multiplicity evidence'] -eq 'not-applicable' -and (
            $values['Multiplicity oracle'] -ne 'not-applicable' -or
            $values['Multiplicity disposition'] -ne 'not-applicable'
        ))
        {
            $errors.Add('not-applicable multiplicity evidence requires matching oracle and disposition')
        }
        if ($declaredPath -eq 'bounded' -and $values['Candidate proof'] -eq 'production-proven')
        {
            $errors.Add('production-proven candidate proof requires the full review path')
        }
        if ($declaredPath -eq 'bounded' -and $values['Candidate proof'] -eq 'targeted-proven')
        {
            if (
                $values['Frozen-head result'] -ne 'behavioral-fail' -or
                $values['Finding proof'] -ne 'empirical' -or
                $values['Scenario proof'] -ne 'empirical' -or
                $values['Behavioral evidence'] -ne 'empirical' -or
                $values['Changed path execution'] -ne 'demonstrated' -or
                $values['Final observable'] -ne 'inspected' -or
                $values['Boundary controls'] -ne 'passed' -or
                $values['Regression assertion disposition'] -ne 'required-regression'
            )
            {
                $errors.Add('bounded targeted-proven requires empirical behavioral red/green, demonstrated path execution, final observable inspection, passed boundary controls, and a required-regression assertion')
            }
            foreach ($relativePath in @('empirical/head.log', 'empirical/green.log', 'empirical/boundary-matrix.md', 'empirical/result.md'))
            {
                $path = Join-Path $Root $relativePath
                if (-not (Test-Path -LiteralPath $path -PathType Leaf))
                {
                    $errors.Add("bounded targeted-proven missing required artifact: $relativePath")
                }
                elseif ([string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $path -Raw)))
                {
                    $errors.Add("bounded targeted-proven artifact is empty: $relativePath")
                }
            }
        }
        if ($values['Candidate proof'] -eq 'production-proven' -and $declaredPath -eq 'full')
        {
            if (-not $provenHead) { $errors.Add('production-proven requires a proven frozen-head defect') }
            if ($weak) { $errors.Add('production-proven is incompatible with weak oracle, mechanism, or scenario fidelity') }
            if ($values['Finding proof'] -ne 'empirical' -or $values['Scenario proof'] -ne 'empirical') { $errors.Add('production-proven requires empirical finding and scenario proof') }
            if ($values['Changed path execution'] -ne 'demonstrated') { $errors.Add('production-proven requires demonstrated changed-path execution') }
            if ($values['Final observable'] -ne 'inspected') { $errors.Add('production-proven requires final observable inspection') }
            if ($values['Boundary controls'] -ne 'passed') { $errors.Add('production-proven requires passed boundary controls') }
            if ($values['Regression assertion disposition'] -ne 'required-regression') { $errors.Add('production-proven requires a required-regression assertion disposition') }
            $stressPath = Join-Path $Root 'empirical/stress-matrix.md'
            if (Test-Path -LiteralPath $stressPath -PathType Leaf)
            {
                $stress = Get-Content -LiteralPath $stressPath -Raw
                foreach ($dimension in @('Real producer/runtime boundary', 'Varied falsification dimensions', 'Applicable configurations/platforms', 'Neighboring suite', 'Cleanup/interruption paths'))
                {
                    if ($stress -notmatch "(?im)^\*\*$([regex]::Escape($dimension)):\*\*\s*(?:passed|not applicable\s*[-:]\s*\S)")
                    {
                        $errors.Add("production-proven requires an explicit passed or justified not-applicable status for: $dimension")
                    }
                }
                $sections = [regex]::Matches($stress, '(?ms)^## Executed cases\s*(.*?)(?=^## |\z)')
                if ($sections.Count -ne 1)
                {
                    $errors.Add('production-proven requires exactly one Executed cases section')
                }
                $rows = @()
                if ($sections.Count -eq 1)
                {
                    $rows = @($sections[0].Groups[1].Value -split "`r?`n" | Where-Object { $_.Trim().StartsWith('|') -and $_ -notmatch '---' })
                }
                if ($rows.Count -lt 3 -or @($rows[1..($rows.Count - 1)] | Sort-Object -Unique).Count -lt 2)
                {
                    $errors.Add('production-proven requires multiple distinct executed cases')
                }
            }
        }

        if ($values['Candidate proof'] -in @('targeted-proven', 'production-proven'))
        {
            $resultPath = Join-Path $Root 'empirical/result.md'
            if (Test-Path -LiteralPath $resultPath -PathType Leaf)
            {
                $result = Get-Content -LiteralPath $resultPath -Raw
                foreach ($label in @(
                    'Frozen path witness',
                    'Candidate path witness',
                    'Frozen final observable',
                    'Candidate final observable'
                ))
                {
                    $matches = [regex]::Matches($result, "(?m)^\*\*$([regex]::Escape($label)):\*\*\s*(.+?)\s*$")
                    if ($matches.Count -eq 0)
                    {
                        $errors.Add("proven candidate empirical result missing evidence reference: $label")
                        continue
                    }
                    if ($matches.Count -gt 1)
                    {
                        $errors.Add("proven candidate empirical result contains duplicate evidence reference: $label")
                        continue
                    }

                    $relativePath = $matches[0].Groups[1].Value.Trim()
                    if ([IO.Path]::IsPathRooted($relativePath))
                    {
                        $errors.Add("proven candidate empirical result has invalid evidence reference for $label`: $relativePath")
                    }
                    else
                    {
                        $evidencePath = Join-Path $Root $relativePath
                        if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf))
                        {
                            $errors.Add("proven candidate empirical result evidence reference is missing or empty for $label`: $relativePath")
                        }
                        else
                        {
                            try
                            {
                                $canonicalRoot = Resolve-CanonicalDirectoryPath $Root
                                $canonicalEvidence = Resolve-CanonicalFilePath $evidencePath
                                if (-not (Test-PathContainedBy -Path $canonicalEvidence -Root $canonicalRoot) -or
                                    [string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $canonicalEvidence -Raw)))
                                {
                                    $errors.Add("proven candidate empirical result evidence reference is missing or outside the artifact root for $label`: $relativePath")
                                }
                            }
                            catch
                            {
                                $errors.Add("proven candidate empirical result has invalid evidence reference for $label`: $relativePath")
                            }
                        }
                    }
                }
            }

            $boundaryPath = Join-Path $Root 'empirical/boundary-matrix.md'
            if (Test-Path -LiteralPath $boundaryPath -PathType Leaf)
            {
                $boundary = Get-Content -LiteralPath $boundaryPath -Raw
                $lines = @($boundary -split "`r?`n" | Where-Object { $_.Trim().StartsWith('|') })
                $expectedHeader = '| Case ID | Role | Trigger/path | Final observable | Result | Evidence artifact |'
                $expectedSeparator = '|---|---|---|---|---|---|'
                if ($lines.Count -lt 2 -or $lines[0].Trim() -ne $expectedHeader -or $lines[1].Trim() -ne $expectedSeparator)
                {
                    $errors.Add('proven candidate boundary matrix needs the canonical six-column table and three role rows')
                }
                else
                {
                    $rows = [Collections.Generic.List[object]]::new()
                    $dataLines = @()
                    if ($lines.Count -gt 2)
                    {
                        $dataLines = @($lines[2..($lines.Count - 1)])
                    }
                    if ($dataLines.Count -ne 3)
                    {
                        $errors.Add('proven candidate boundary matrix requires exactly three role rows')
                    }
                    foreach ($line in $dataLines)
                    {
                        $cells = @($line.Trim().Trim('|') -split '\|' | ForEach-Object { $_.Trim() })
                        if ($cells.Count -ne 6 -or @($cells | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0)
                        {
                            $errors.Add('proven candidate boundary matrix contains an incomplete row')
                            continue
                        }
                        $rows.Add([pscustomobject]@{
                            Id = $cells[0]
                            Role = $cells[1].ToLowerInvariant()
                            Trigger = $cells[2]
                            Observable = $cells[3]
                            Result = $cells[4].ToLowerInvariant()
                            Evidence = $cells[5]
                        })
                    }

                    if (@($rows | ForEach-Object { $_.Id } | Sort-Object -Unique).Count -ne $rows.Count)
                    {
                        $errors.Add('proven candidate boundary matrix requires distinct case IDs')
                    }
                    $unknownRoles = @($rows | Where-Object Role -notin @('defect', 'opposite', 'adjacent'))
                    if ($unknownRoles.Count -gt 0)
                    {
                        $errors.Add('proven candidate boundary matrix contains an unrecognized role')
                    }
                    foreach ($role in @('defect', 'opposite', 'adjacent'))
                    {
                        $roleRows = @($rows | Where-Object Role -eq $role)
                        if ($roleRows.Count -ne 1)
                        {
                            $errors.Add("proven candidate boundary matrix requires exactly one $role row")
                            continue
                        }
                        $row = $roleRows[0]
                        $validResult = if ($role -eq 'defect')
                        {
                            $row.Result -eq 'passed'
                        }
                        else
                        {
                            $row.Result -eq 'passed' -or $row.Result -match '^not applicable\s*-\s*\S.+$'
                        }
                        if (-not $validResult)
                        {
                            $errors.Add("proven candidate boundary matrix has invalid $role result: $($row.Result)")
                        }
                        if ($row.Result -eq 'passed' -and ($row.Trigger -eq 'not-applicable' -or $row.Observable -eq 'not-applicable'))
                        {
                            $errors.Add("proven candidate boundary matrix $role row lacks executed trigger or observable evidence")
                        }

                        if ([IO.Path]::IsPathRooted($row.Evidence))
                        {
                            $errors.Add("proven candidate boundary matrix has invalid evidence artifact for $role`: $($row.Evidence)")
                        }
                        else
                        {
                            $evidencePath = Join-Path $Root $row.Evidence
                            if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf))
                            {
                                $errors.Add("proven candidate boundary matrix evidence artifact is missing or empty for $role`: $($row.Evidence)")
                            }
                            else
                            {
                                try
                                {
                                    $canonicalRoot = Resolve-CanonicalDirectoryPath $Root
                                    $canonicalEvidence = Resolve-CanonicalFilePath $evidencePath
                                    if (-not (Test-PathContainedBy -Path $canonicalEvidence -Root $canonicalRoot) -or
                                        [string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $canonicalEvidence -Raw)))
                                    {
                                        $errors.Add("proven candidate boundary matrix evidence artifact is missing or outside the artifact root for $role`: $($row.Evidence)")
                                    }
                                }
                                catch
                                {
                                    $errors.Add("proven candidate boundary matrix has invalid evidence artifact for $role`: $($row.Evidence)")
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    return @($errors)
}

Export-ModuleMember -Function @(
    'ConvertTo-CanonicalJson'
    'Get-PathComparison'
    'Get-PropertyValue'
    'Get-ReviewerModelPolicy'
    'Get-Sha256'
    'Normalize-DirectoryPath'
    'Read-JsonDocument'
    'Resolve-CanonicalDirectoryPath'
    'Resolve-CanonicalFilePath'
    'Test-HostedReviewerModelEvidence'
    'Test-NonEmptyString'
    'Test-PathContainedBy'
    'Test-ReviewerModelPolicy'
    'Test-ReviewArtifacts'
)
