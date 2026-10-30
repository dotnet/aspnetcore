#requires -Version 7.0

Set-StrictMode -Version 3

$script:Utf8NoBom = [Text.UTF8Encoding]::new($false, $true)

function Get-ReviewFixtureSha256 {
    param([byte[]]$Bytes)

    return ConvertTo-HexStringLower ([Security.Cryptography.SHA256]::HashData($Bytes))
}

function ConvertTo-HexStringLower {
    param([byte[]]$Bytes)

    return [Convert]::ToHexString($Bytes).ToLowerInvariant()
}

function Get-ReviewFixtureTextMetrics {
    param([byte[]]$Bytes)

    $text = $script:Utf8NoBom.GetString($Bytes)
    $lines = @($text -split "`n")
    if ($text.EndsWith("`n", [StringComparison]::Ordinal)) {
        $lines = @($lines[0..([Math]::Max(0, $lines.Count - 2))])
        if ($text -eq "`n") {
            $lines = @('')
        }
    }
    $maximum = 0
    foreach ($line in $lines) {
        $maximum = [Math]::Max(
            $maximum,
            $script:Utf8NoBom.GetByteCount([string]$line)
        )
    }
    return [pscustomobject]@{
        Text = $text
        LineCount = $lines.Count
        MaxUtf8LineBytes = $maximum
    }
}

function Get-ReviewFixtureDirectoryDigest {
    param([Parameter(Mandatory = $true)][string]$Path)

    $root = [IO.Path]::GetFullPath($Path)
    $names = @(
        Get-ChildItem $root -Recurse -File -Force |
            ForEach-Object {
                [IO.Path]::GetRelativePath($root, $_.FullName).Replace('\', '/')
            } |
            Sort-Object -CaseSensitive
    )
    $digest = [Security.Cryptography.IncrementalHash]::CreateHash(
        [Security.Cryptography.HashAlgorithmName]::SHA256
    )
    try {
        foreach ($name in $names) {
            $bytes = [IO.File]::ReadAllBytes(
                (Join-Path $root $name.Replace('/', [IO.Path]::DirectorySeparatorChar))
            )
            $line = "$name`0$(Get-ReviewFixtureSha256 $bytes)`n"
            $digest.AppendData($script:Utf8NoBom.GetBytes($line))
        }
        return [pscustomobject]@{
            files = $names.Count
            sha256 = ConvertTo-HexStringLower $digest.GetHashAndReset()
        }
    } finally {
        $digest.Dispose()
    }
}

function Assert-ReviewFixtureCondition {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-ReviewFixtureProperties {
    param(
        [object]$Value,
        [string[]]$Required,
        [string[]]$Optional = @(),
        [string]$Description
    )

    Assert-ReviewFixtureCondition (
        $null -ne $Value -and $Value -isnot [string]
    ) "$Description must be an object."
    $actual = @($Value.PSObject.Properties.Name)
    $allowed = @($Required + $Optional)
    Assert-ReviewFixtureCondition (
        @($actual | Where-Object { $_ -notin $allowed }).Count -eq 0 -and
        @($Required | Where-Object { $_ -notin $actual }).Count -eq 0
    ) "$Description has an unexpected projected schema."
}

function Test-ReviewFixtureReferenceObject {
    param([object]$Value)

    return $null -ne $Value -and
        $Value -isnot [string] -and
        @($Value.PSObject.Properties.Name).Count -gt 0
}

function Resolve-ReviewFixtureReference {
    param(
        [string]$BundleRoot,
        [object]$Reference,
        [string]$Description,
        [int]$MaximumLineBytes
    )

    Assert-ReviewFixtureProperties $Reference @('path', 'utf8Bytes', 'sha256') @() "$Description required-text reference"
    $relative = [string]$Reference.path
    Assert-ReviewFixtureCondition (
        $relative -match '^required/(?:[^/]+/)*[^/]+\.source$' -and
        -not [IO.Path]::IsPathFullyQualified($relative) -and
        -not $relative.Contains('\', [StringComparison]::Ordinal) -and
        -not @($relative -split '/').Contains('..') -and
        @($relative -split '/' | Where-Object {
            $_ -notmatch '^[A-Za-z0-9._-]+$'
        }).Count -eq 0
    ) "$Description required-text path is invalid."
    $root = [IO.Path]::GetFullPath($BundleRoot)
    $path = [IO.Path]::GetFullPath(
        (Join-Path $root $relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
    )
    Assert-ReviewFixtureCondition (
        $path.StartsWith(
            $root + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase
        )
    ) "$Description required-text path escapes the bundle."
    Assert-ReviewFixtureCondition (
        Test-Path $path -PathType Leaf
    ) "$Description required text is missing."
    $item = Get-Item $path -Force
    Assert-ReviewFixtureCondition (
        $null -eq $item.LinkType -and
        ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0
    ) "$Description required text is not an ordinary file."
    $bytes = [IO.File]::ReadAllBytes($path)
    $metrics = Get-ReviewFixtureTextMetrics $bytes
    Assert-ReviewFixtureCondition (
        $bytes.Length -eq [int]$Reference.utf8Bytes
    ) "$Description required text has the wrong UTF-8 byte length."
    Assert-ReviewFixtureCondition (
        (Get-ReviewFixtureSha256 $bytes) -ceq [string]$Reference.sha256
    ) "$Description required text has the wrong SHA-256."
    Assert-ReviewFixtureCondition (
        $metrics.MaxUtf8LineBytes -le $MaximumLineBytes
    ) "$Description required text line exceeds $MaximumLineBytes UTF-8 bytes."
    return [pscustomobject]@{
        Path = $relative
        Metrics = $metrics
    }
}

function Read-ReviewFixtureJson {
    param([string]$Path, [string]$Description)

    try {
        return Get-Content $Path -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    } catch {
        throw "$Description is malformed JSON: $($_.Exception.Message)"
    }
}

function Assert-ReviewFixtureUser {
    param([object]$User, [string]$Description)

    if ($null -eq $User) {
        return
    }
    Assert-ReviewFixtureProperties $User @('login', 'id', 'type') @() "$Description author"
    Assert-ReviewFixtureCondition (
        -not [string]::IsNullOrEmpty([string]$User.login) -and
        [long]$User.id -gt 0 -and
        -not [string]::IsNullOrEmpty([string]$User.type)
    ) "$Description author is malformed."
}

function Assert-ReviewFixtureSchemas {
    param(
        [string]$BundleRoot,
        [object]$Manifest,
        [bool]$FeedbackExpected
    )

    $pull = Read-ReviewFixtureJson (Join-Path $BundleRoot 'pull.json') 'pull.json'
    Assert-ReviewFixtureProperties $pull @(
        'number', 'state', 'title', 'body', 'draft', 'author', 'labels',
        'milestone', 'createdAt', 'updatedAt'
    ) @() 'pull.json'
    Assert-ReviewFixtureCondition (
        [int]$pull.number -gt 0 -and
        [string]$pull.state -ceq 'open' -and
        -not [string]::IsNullOrEmpty([string]$pull.title) -and
        (Test-ReviewFixtureReferenceObject $pull.body)
    ) 'pull.json metadata is malformed.'
    Assert-ReviewFixtureUser $pull.author 'pull'
    foreach ($label in @($pull.labels)) {
        Assert-ReviewFixtureProperties $label @('name') @() 'pull label'
    }
    if ($null -ne $pull.milestone) {
        Assert-ReviewFixtureProperties $pull.milestone @('number', 'title') @() 'pull milestone'
    }

    $filesPath = Join-Path $BundleRoot 'files.json'
    $filesText = Get-Content $filesPath -Raw -Encoding utf8
    Assert-ReviewFixtureCondition (
        $filesText.TrimStart().StartsWith('[', [StringComparison]::Ordinal)
    ) 'files.json must be an array.'
    $files = Read-ReviewFixtureJson $filesPath 'files.json'
    Assert-ReviewFixtureCondition (@($files).Count -gt 0) 'files.json is empty.'
    foreach ($file in @($files)) {
        Assert-ReviewFixtureProperties $file @(
            'filename', 'status', 'sha', 'additions', 'deletions', 'changes'
        ) @('previousFilename') 'changed-file metadata'
        Assert-ReviewFixtureCondition (
            -not [string]::IsNullOrEmpty([string]$file.filename) -and
            [string]$file.sha -match '^[0-9a-f]{40}$' -and
            [int]$file.additions -ge 0 -and
            [int]$file.deletions -ge 0 -and
            [int]$file.changes -ge 0
        ) 'Changed-file metadata is malformed.'
    }
    Assert-ReviewFixtureCondition (
        $filesText -notmatch '"patch"\s*:' -and
        $filesText -notmatch '"[^"]*_url"\s*:'
    ) 'files.json contains patch or REST URL bloat.'

    $feedback = $null
    if ($FeedbackExpected) {
        $feedback = Read-ReviewFixtureJson (Join-Path $BundleRoot 'feedback.json') 'feedback.json'
        Assert-ReviewFixtureProperties $feedback @('comments', 'reviews', 'inline') @() 'feedback.json'
        foreach ($comment in @($feedback.comments)) {
            Assert-ReviewFixtureProperties $comment @(
                'id', 'author', 'body', 'createdAt', 'updatedAt'
            ) @() 'issue-comment metadata'
            Assert-ReviewFixtureCondition (
                Test-ReviewFixtureReferenceObject $comment.body
            ) 'Issue-comment body reference must be a non-null object.'
            Assert-ReviewFixtureUser $comment.author 'issue-comment'
        }
        foreach ($review in @($feedback.reviews)) {
            Assert-ReviewFixtureProperties $review @(
                'id', 'author', 'body', 'state', 'submittedAt', 'commitId'
            ) @() 'review metadata'
            Assert-ReviewFixtureCondition (
                Test-ReviewFixtureReferenceObject $review.body
            ) 'Review body reference must be a non-null object.'
            Assert-ReviewFixtureUser $review.author 'review'
        }
        foreach ($comment in @($feedback.inline)) {
            Assert-ReviewFixtureProperties $comment @(
                'id', 'author', 'body', 'path', 'line', 'side', 'startLine',
                'startSide', 'originalLine', 'originalStartLine', 'commitId',
                'originalCommitId', 'inReplyToId', 'createdAt', 'updatedAt'
            ) @() 'inline-comment metadata'
            Assert-ReviewFixtureCondition (
                Test-ReviewFixtureReferenceObject $comment.body
            ) 'Inline-comment body reference must be a non-null object.'
            Assert-ReviewFixtureUser $comment.author 'inline-comment'
        }
    }

    return [pscustomobject]@{
        Pull = $pull
        Feedback = $feedback
    }
}

function Assert-ReviewFixture {
    param(
        [Parameter(Mandatory = $true)][string]$BundleRoot,
        [Parameter(Mandatory = $true)][string]$ContractPath,
        [Parameter(Mandatory = $true)]
        [ValidateSet('valid', 'missingFeedback')]
        [string]$BundleName
    )

    $root = [IO.Path]::GetFullPath($BundleRoot)
    $contract = Read-ReviewFixtureJson $ContractPath 'fixture inventory'
    Assert-ReviewFixtureCondition (
        [int]$contract.version -eq 1
    ) 'Fixture inventory has an unsupported version.'
    $expectedDirectory = [string]$contract.bundles.$BundleName

    $expected = @(
        $contract.files |
            Where-Object {
                $absence = $_.PSObject.Properties['expectedAbsentIn']?.Value
                $expectedDirectory -notin @($absence)
            }
    )
    $expectedNames = @($expected | ForEach-Object { [string]$_.path })
    $actualNames = @(
        Get-ChildItem $root -Recurse -File -Force |
            ForEach-Object {
                [IO.Path]::GetRelativePath($root, $_.FullName).Replace('\', '/')
            } |
            Sort-Object -CaseSensitive
    )
    Assert-ReviewFixtureCondition (
        @(Compare-Object $expectedNames $actualNames).Count -eq 0
    ) 'Fixture inventory path set differs from the bundle; an extra, missing, or orphan fixture file exists.'

    foreach ($entry in $expected) {
        $path = Join-Path $root ([string]$entry.path).Replace(
            '/',
            [IO.Path]::DirectorySeparatorChar
        )
        $bytes = [IO.File]::ReadAllBytes($path)
        $metrics = Get-ReviewFixtureTextMetrics $bytes
        if ([IO.Path]::GetExtension($path) -in @('.json', '.source', '.patch')) {
            Assert-ReviewFixtureCondition (
                [Array]::IndexOf($bytes, [byte]13) -lt 0
            ) "Fixture file '$($entry.path)' is not LF-normalized."
        }
        Assert-ReviewFixtureCondition (
            $bytes.Length -eq [int]$entry.utf8Bytes -and
            $metrics.LineCount -eq [int]$entry.lineCount -and
            $metrics.MaxUtf8LineBytes -eq [int]$entry.maxUtf8LineBytes -and
            (Get-ReviewFixtureSha256 $bytes) -ceq [string]$entry.sha256
        ) "Fixture file '$($entry.path)' does not match its trusted byte inventory."
        Assert-ReviewFixtureCondition (
            @($entry.readRoles).Count -gt 0 -and
            @($entry.readRoles | Where-Object { $_ -notin @('coordinator', 'worker') }).Count -eq 0
        ) "Fixture file '$($entry.path)' has invalid read-role metadata."
    }

    $manifest = Read-ReviewFixtureJson (Join-Path $root 'manifest.json') 'manifest.json'
    Assert-ReviewFixtureProperties $manifest @(
        'version', 'ready', 'producer', 'target', 'suffix', 'sources',
        'guidance', 'routing', 'guides', 'policies', 'context',
        'skippedLinks', 'exclusions', 'artifacts', 'requiredText',
        'timingsMs', 'limitations'
    ) @() 'manifest.json'
    Assert-ReviewFixtureProperties $manifest.requiredText @(
        'root', 'files', 'sha256'
    ) @() 'manifest requiredText'
    Assert-ReviewFixtureProperties $manifest.artifacts @(
        'diff.patch', 'files.json', 'pull.json', 'feedback.json'
    ) @() 'manifest artifacts'
    Assert-ReviewFixtureProperties $manifest.guidance @(
        'mode', 'originalRoot', 'checkoutCommit', 'workingTreeChanges',
        'sha256', 'root', 'files'
    ) @() 'manifest guidance'
    Assert-ReviewFixtureProperties $manifest.routing @(
        'path', 'sha256'
    ) @() 'manifest routing'
    Assert-ReviewFixtureCondition (
        [int]$manifest.version -eq 2 -and $manifest.ready -eq $true
    ) 'Manifest is not ready version 2.'
    Assert-ReviewFixtureCondition (
        [string]$manifest.producer -ceq [string]$contract.invariants.producer
    ) 'Manifest producer provenance changed.'
    Assert-ReviewFixtureCondition (
        ($manifest.target | ConvertTo-Json -Compress -Depth 20) -ceq
        ($contract.invariants.target | ConvertTo-Json -Compress -Depth 20)
    ) 'Manifest frozen target identity changed.'
    foreach ($role in @('head', 'mergeBase', 'baseTip')) {
        $actual = $manifest.sources.$role
        $pinned = $contract.invariants.sourceRoles.$role
        Assert-ReviewFixtureProperties $actual @(
            'root', 'commit', 'tree', 'files', 'sha256', 'pointers'
        ) @() "manifest $role source"
        foreach ($name in @('root', 'commit', 'tree')) {
            Assert-ReviewFixtureCondition (
                [string]$actual.$name -ceq [string]$pinned.$name
            ) "Manifest frozen $role source $name changed."
        }
        Assert-ReviewFixtureCondition (
            ($actual.pointers | ConvertTo-Json -Compress -Depth 20) -ceq
            ($pinned.pointers | ConvertTo-Json -Compress -Depth 20)
        ) "Manifest frozen $role pointer metadata changed."
        $digest = Get-ReviewFixtureDirectoryDigest (Join-Path $root $actual.root)
        Assert-ReviewFixtureCondition (
            $digest.files -eq [int]$actual.files -and
            $digest.sha256 -ceq [string]$actual.sha256
        ) "Manifest $role source digest changed."
    }
    foreach ($name in @('mode', 'originalRoot', 'checkoutCommit', 'workingTreeChanges')) {
        Assert-ReviewFixtureCondition (
            $manifest.guidance.$name -ceq $contract.invariants.guidance.$name
        ) "Manifest guidance provenance '$name' changed."
    }
    $guidanceDigest = Get-ReviewFixtureDirectoryDigest (Join-Path $root $manifest.guidance.root)
    Assert-ReviewFixtureCondition (
        $guidanceDigest.files -eq [int]$manifest.guidance.files -and
        $guidanceDigest.sha256 -ceq [string]$manifest.guidance.sha256
    ) 'Manifest guidance digest changed.'
    $routingPath = Join-Path $root (
        "guidance/$($manifest.routing.path).source".Replace(
            '/',
            [IO.Path]::DirectorySeparatorChar
        )
    )
    Assert-ReviewFixtureCondition (
        (Get-ReviewFixtureSha256 ([IO.File]::ReadAllBytes($routingPath))) -ceq
        [string]$manifest.routing.sha256
    ) 'Manifest routing digest changed.'

    foreach ($artifact in @('diff.patch', 'files.json', 'pull.json', 'feedback.json')) {
        Assert-ReviewFixtureCondition (
            $manifest.artifacts.PSObject.Properties.Name -contains $artifact
        ) "Manifest omits canonical artifact '$artifact'."
        $path = Join-Path $root $artifact
        if (Test-Path $path -PathType Leaf) {
            Assert-ReviewFixtureCondition (
                (Get-ReviewFixtureSha256 ([IO.File]::ReadAllBytes($path))) -ceq
                [string]$manifest.artifacts.$artifact
            ) "Manifest artifact digest changed for '$artifact'."
        } else {
            Assert-ReviewFixtureCondition (
                $BundleName -eq 'missingFeedback' -and $artifact -eq 'feedback.json'
            ) "Manifest artifact '$artifact' is unavailable unexpectedly."
        }
    }

    $schema = Assert-ReviewFixtureSchemas $root $manifest ($BundleName -eq 'valid')
    $references = [Collections.Generic.List[object]]::new()
    $references.Add([pscustomobject]@{ Description = 'pull body'; Value = $schema.Pull.body })
    if ($BundleName -eq 'valid') {
        foreach ($group in @('comments', 'reviews', 'inline')) {
            foreach ($item in @($schema.Feedback.$group)) {
                $references.Add([pscustomobject]@{
                    Description = "$group body"
                    Value = $item.body
                })
            }
        }
    } else {
        foreach ($name in @(
            'required/feedback/comments/101.source',
            'required/feedback/reviews/102.source',
            'required/feedback/inline/103.source'
        )) {
            $entry = $contract.files | Where-Object path -CEQ $name
            $references.Add([pscustomobject]@{
                Description = 'missing-feedback retained body'
                Value = [pscustomobject]@{
                    path = $name
                    utf8Bytes = $entry.utf8Bytes
                    sha256 = $entry.sha256
                }
            })
        }
    }
    $guidePaths = @($manifest.guides | ForEach-Object { [string]$_.path })
    foreach ($policy in @($manifest.policies)) {
        Assert-ReviewFixtureProperties $policy @(
            'path', 'anchor', 'guide', 'body'
        ) @() 'manifest policy'
        Assert-ReviewFixtureCondition (
            Test-ReviewFixtureReferenceObject $policy.body
        ) 'Policy body reference must be a non-null object.'
        Assert-ReviewFixtureCondition (
            $guidePaths -ccontains [string]$policy.guide
        ) 'Policy guide does not match a routed guide.'
        $references.Add([pscustomobject]@{ Description = 'policy body'; Value = $policy.body })
    }
    foreach ($exclusion in @($manifest.exclusions)) {
        $found = 0
        foreach ($name in @('scope', 'reason', 'body')) {
            $value = $exclusion.PSObject.Properties[$name]?.Value
            if ($null -ne $value) {
                Assert-ReviewFixtureCondition (
                    Test-ReviewFixtureReferenceObject $value
                ) "Exclusion $name reference must be an object."
                $references.Add([pscustomobject]@{
                    Description = "exclusion $name"
                    Value = $value
                })
                $found++
            }
        }
        Assert-ReviewFixtureCondition ($found -gt 0) 'Exclusion has no required text.'
    }

    for ($guideIndex = 0; $guideIndex -lt @($manifest.guides).Count; $guideIndex++) {
        $guide = $manifest.guides[$guideIndex]
        Assert-ReviewFixtureProperties $guide @(
            'path', 'topics', 'assignment'
        ) @() 'manifest guide'
        Assert-ReviewFixtureProperties $guide.assignment @(
            'files', 'sourceRoles'
        ) @() 'manifest worker assignment'
        Assert-ReviewFixtureCondition (
            (@($guide.assignment.sourceRoles) -join "`n") -ceq
            (@('head', 'mergeBase', 'baseTip') -join "`n")
        ) "Worker assignment source roles changed for '$($guide.path)'."

        $assignedFiles = @($guide.assignment.files)
        Assert-ReviewFixtureCondition (
            @($assignedFiles | ForEach-Object { [string]$_.path } |
                Sort-Object -Unique -CaseSensitive).Count -eq $assignedFiles.Count
        ) "Worker assignment reuses an input path for '$($guide.path)'."
        foreach ($assigned in $assignedFiles) {
            Assert-ReviewFixtureProperties $assigned @(
                'kind', 'path', 'utf8Bytes', 'sha256'
            ) @() 'manifest worker assignment file'
            Assert-ReviewFixtureCondition (
                [string]$assigned.kind -cin @(
                    'guide', 'diff', 'changedFiles', 'policy', 'exclusion'
                )
            ) "Worker assignment has an invalid input kind for '$($guide.path)'."
            $inventoryEntry = $contract.files |
                Where-Object path -CEQ ([string]$assigned.path)
            Assert-ReviewFixtureCondition (
                @($inventoryEntry).Count -eq 1 -and
                [int]$assigned.utf8Bytes -eq [int]$inventoryEntry.utf8Bytes -and
                [string]$assigned.sha256 -ceq [string]$inventoryEntry.sha256
            ) "Worker assignment file metadata changed for '$($guide.path)'."
        }

        $guideInput = @($assignedFiles | Where-Object kind -CEQ 'guide')
        Assert-ReviewFixtureCondition (
            $guideInput.Count -eq 1 -and
            [string]$guideInput[0].path -ceq "guidance/$($guide.path).source"
        ) "Worker assignment has the wrong guide input for '$($guide.path)'."
        foreach ($expectedArtifact in @(
            [pscustomobject]@{ Kind = 'diff'; Path = 'diff.patch' },
            [pscustomobject]@{ Kind = 'changedFiles'; Path = 'files.json' }
        )) {
            $input = @($assignedFiles | Where-Object kind -CEQ $expectedArtifact.Kind)
            Assert-ReviewFixtureCondition (
                $input.Count -eq 1 -and
                [string]$input[0].path -ceq $expectedArtifact.Path
            ) "Worker assignment has the wrong $($expectedArtifact.Kind) input for '$($guide.path)'."
        }

        $guidePolicies = @(
            $manifest.policies |
                Where-Object guide -CEQ ([string]$guide.path)
        )
        for ($policyIndex = 0; $policyIndex -lt $guidePolicies.Count; $policyIndex++) {
            $expectedPolicyPath = (
                "required/guides/$($guideIndex.ToString('D4'))/" +
                "policies/$($policyIndex.ToString('D4')).source"
            )
            Assert-ReviewFixtureCondition (
                [string]$guidePolicies[$policyIndex].body.path -ceq
                $expectedPolicyPath
            ) "Policy path does not match guide '$($guide.path)' and its index."
        }
        $expectedPolicies = @($guidePolicies | ForEach-Object { $_.body })
        $assignedPolicies = @(
            $assignedFiles |
                Where-Object kind -CEQ 'policy' |
                ForEach-Object {
                    [pscustomobject]@{
                        path = $_.path
                        utf8Bytes = $_.utf8Bytes
                        sha256 = $_.sha256
                    }
                }
        )
        Assert-ReviewFixtureCondition (
            ($expectedPolicies | ConvertTo-Json -Compress -Depth 20) -ceq
            ($assignedPolicies | ConvertTo-Json -Compress -Depth 20)
        ) "Worker assignment has incorrect policy inputs for '$($guide.path)'."

        $assignedExclusions = @(
            $assignedFiles | Where-Object kind -CEQ 'exclusion'
        )
        $expectedExclusionPaths = @(
            "required/guides/$($guideIndex.ToString('D4'))/exclusions/0000-scope.source"
            "required/guides/$($guideIndex.ToString('D4'))/exclusions/0000-reason.source"
            "required/guides/$($guideIndex.ToString('D4'))/exclusions/0001-body.source"
        )
        Assert-ReviewFixtureCondition (
            (@($assignedExclusions | ForEach-Object { [string]$_.path }) -join "`n") -ceq
            ($expectedExclusionPaths -join "`n")
        ) "Worker assignment has incorrect exclusion inputs for '$($guide.path)'."
        foreach ($exclusion in $assignedExclusions) {
            $references.Add([pscustomobject]@{
                Description = "worker assignment exclusion for $($guide.path)"
                Value = [pscustomobject]@{
                    path = $exclusion.path
                    utf8Bytes = $exclusion.utf8Bytes
                    sha256 = $exclusion.sha256
                }
            })
        }
    }

    $names = [Collections.Generic.List[string]]::new()
    foreach ($item in $references) {
        $resolved = Resolve-ReviewFixtureReference `
            $root $item.Value $item.Description `
            ([int]$contract.invariants.maximumUtf8LineBytes)
        Assert-ReviewFixtureCondition (
            -not $names.Contains($resolved.Path)
        ) "Required text reference is reused: $($resolved.Path)."
        $names.Add($resolved.Path)
    }
    $actualRequired = @(
        Get-ChildItem (Join-Path $root 'required') -Recurse -File -Force |
            ForEach-Object {
                'required/' + [IO.Path]::GetRelativePath(
                    (Join-Path $root 'required'),
                    $_.FullName
                ).Replace('\', '/')
            } |
            Sort-Object -CaseSensitive
    )
    Assert-ReviewFixtureCondition (
        @(Compare-Object @($names | Sort-Object -CaseSensitive) $actualRequired).Count -eq 0
    ) 'Required-text references and files are not a bijection; an orphan or unreferenced file exists.'
    $requiredDigest = Get-ReviewFixtureDirectoryDigest (Join-Path $root $manifest.requiredText.root)
    Assert-ReviewFixtureCondition (
        [string]$manifest.requiredText.root -ceq 'required' -and
        [int]$manifest.requiredText.files -eq $requiredDigest.files -and
        [string]$manifest.requiredText.sha256 -ceq $requiredDigest.sha256
    ) 'Manifest required-text root, file count, or digest changed.'

    $body = Resolve-ReviewFixtureReference `
        $root $schema.Pull.body 'pull body' `
        ([int]$contract.invariants.maximumUtf8LineBytes)
    foreach ($name in @('utf8Bytes', 'lineCount', 'maxUtf8LineBytes')) {
        $actual = if ($name -eq 'utf8Bytes') {
            [int]$schema.Pull.body.utf8Bytes
        } else {
            [int]$body.Metrics.$name
        }
        Assert-ReviewFixtureCondition (
            $actual -eq [int]$contract.invariants.pullBody.$name
        ) "The #70032-shaped pull body has the wrong $name."
    }

    foreach ($entry in $expected | Where-Object { $_.path -like '*.json' }) {
        Assert-ReviewFixtureCondition (
            [int]$entry.maxUtf8LineBytes -le [int]$contract.invariants.maximumUtf8LineBytes
        ) "Serialized JSON line exceeds the native-reader bound: $($entry.path)."
    }

    return [pscustomobject]@{
        Bundle = $BundleName
        Files = $actualNames.Count
        RequiredReferences = $names.Count
    }
}
