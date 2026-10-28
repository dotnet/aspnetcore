#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$fixtureRoot = Join-Path $PSScriptRoot 'review-pull-request/fixtures'
$contractPath = Join-Path $fixtureRoot 'contracts/inventory.json'
$sameWorkerContractPath = Join-Path $fixtureRoot 'contracts/inventory-same-worker.json'
$validRoot = Join-Path $fixtureRoot 'bundle'
$sameWorkerRoot = Join-Path $fixtureRoot 'bundle-same-worker'
$missingRoot = Join-Path $fixtureRoot 'bundle-missing-feedback'
$library = Join-Path $PSScriptRoot 'review-pull-request/fixture-contract.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-review-fixture-test-' + [guid]::NewGuid().ToString('N')
)
$utf8 = [Text.UTF8Encoding]::new($false)

. $library

function Write-JsonLf {
    param([string]$Path, [object]$Value)

    $json = ($Value | ConvertTo-Json -Depth 100).Replace("`r`n", "`n") + "`n"
    [IO.File]::WriteAllText(
        $Path,
        $json,
        $utf8
    )
}

function Read-Json {
    param([string]$Path)

    return Get-Content $Path -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
}

function Assert-Throws {
    param(
        [string]$Name,
        [scriptblock]$Action,
        [string]$Expected
    )

    $message = ''
    try {
        & $Action
    } catch {
        $message = $_.Exception.Message
    }
    if ($message -notlike "*$Expected*") {
        throw "$Name expected an error containing '$Expected'; got '$message'."
    }
    Write-Host "  [OK] $Name"
}

function Update-ControlInventory {
    param(
        [string]$Root,
        [string]$ControlContractPath
    )

    $contract = Read-Json $ControlContractPath
    $original = @{}
    foreach ($entry in $contract.files) {
        $original[[string]$entry.path] = $entry
    }
    $entries = [Collections.Generic.List[object]]::new()
    foreach ($file in Get-ChildItem $Root -Recurse -File -Force) {
        $path = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
        $bytes = [IO.File]::ReadAllBytes($file.FullName)
        $metrics = Get-ReviewFixtureTextMetrics $bytes
        $prior = $original[$path]
        $entry = [ordered]@{
            path = $path
            utf8Bytes = $bytes.Length
            lineCount = $metrics.LineCount
            maxUtf8LineBytes = $metrics.MaxUtf8LineBytes
            sha256 = Get-ReviewFixtureSha256 $bytes
            readRoles = if ($null -ne $prior) {
                @($prior.readRoles)
            } else {
                @('coordinator', 'worker')
            }
            coordinatorRequired = if ($null -ne $prior) {
                [bool]$prior.coordinatorRequired
            } else {
                $false
            }
        }
        if ($null -ne $prior -and
            $null -ne $prior.PSObject.Properties['expectedAbsentIn']) {
            $entry.expectedAbsentIn = @($prior.expectedAbsentIn)
        }
        $entries.Add([pscustomobject]$entry)
    }
    $contract.files = @($entries | Sort-Object path -CaseSensitive)
    Write-JsonLf $ControlContractPath $contract
}

function Reconcile-ControlBundle {
    param(
        [string]$Root,
        [string]$ControlContractPath,
        [switch]$PreserveRequiredDigest
    )

    $manifestPath = Join-Path $Root 'manifest.json'
    $manifest = Read-Json $manifestPath
    foreach ($artifact in @('diff.patch', 'files.json', 'pull.json', 'feedback.json')) {
        $artifactPath = Join-Path $Root $artifact
        if (Test-Path $artifactPath -PathType Leaf) {
            $manifest.artifacts.$artifact = Get-ReviewFixtureSha256 (
                [IO.File]::ReadAllBytes($artifactPath)
            )
        }
    }
    if (-not $PreserveRequiredDigest) {
        $digest = Get-ReviewFixtureDirectoryDigest (Join-Path $Root 'required')
        $manifest.requiredText.files = $digest.files
        $manifest.requiredText.sha256 = $digest.sha256
    }
    Write-JsonLf $manifestPath $manifest
    Update-ControlInventory $Root $ControlContractPath
}

function New-Control {
    param([string]$Name)

    $root = Join-Path $testRoot "$Name/bundle"
    New-Item -ItemType Directory -Path (Split-Path $root -Parent) -Force |
        Out-Null
    Copy-Item $validRoot $root -Recurse
    $controlContract = Join-Path $testRoot "$Name/inventory.json"
    Copy-Item $contractPath $controlContract
    return [pscustomobject]@{
        Root = $root
        Contract = $controlContract
    }
}

function Write-Artifact {
    param(
        [object]$Control,
        [string]$Name,
        [object]$Value
    )

    Write-JsonLf (Join-Path $Control.Root $Name) $Value
}

function Invoke-MutationControl {
    param(
        [string]$Name,
        [scriptblock]$Mutation,
        [string]$Expected,
        [switch]$WithoutReconcile,
        [switch]$PreserveRequiredDigest
    )

    $control = New-Control $Name
    & $Mutation $control
    if (-not $WithoutReconcile) {
        Reconcile-ControlBundle `
            $control.Root `
            $control.Contract `
            -PreserveRequiredDigest:$PreserveRequiredDigest
    }
    Assert-Throws $Name {
        Assert-ReviewFixture `
            -BundleRoot $control.Root `
            -ContractPath $control.Contract `
            -BundleName valid
    } $Expected
}

New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    $valid = Assert-ReviewFixture `
        -BundleRoot $validRoot `
        -ContractPath $contractPath `
        -BundleName valid
    if ($valid.Files -lt 48 -or $valid.RequiredReferences -ne 15) {
        throw "The valid fixture did not exercise the complete inventory and reference set."
    }
    $trustedInventory = Read-Json $contractPath
    $expectedCoordinatorRequired = @(
        'diff.patch'
        'feedback.json'
        'files.json'
        'guidance/.github/skills/review-pull-request/routing.md.source'
        'manifest.json'
        'pull.json'
        'required/coordinator/exclusions/0000-reason.source'
        'required/coordinator/exclusions/0000-scope.source'
        'required/coordinator/exclusions/0001-body.source'
        'required/feedback/comments/101.source'
        'required/feedback/inline/103.source'
        'required/feedback/reviews/102.source'
        'required/pull/body.source'
    ) | Sort-Object -CaseSensitive
    $actualCoordinatorRequired = @(
        $trustedInventory.files |
            Where-Object coordinatorRequired |
            ForEach-Object path |
            Sort-Object -CaseSensitive
    )
    if (@(
        Compare-Object $expectedCoordinatorRequired $actualCoordinatorRequired
    ).Count -ne 0) {
        throw 'The exact coordinator-required fixture role set changed.'
    }
    $expectedCoordinatorOnly = @(
        'feedback.json'
        'guidance/.github/copilot-instructions.md.source'
        'guidance/.github/skills/review-pull-request/routing.md.source'
        'guidance/docs/ComponentsPolicy.md.source'
        'guidance/docs/Policy.md.source'
        'manifest.json'
        'pull.json'
        'required/coordinator/exclusions/0000-reason.source'
        'required/coordinator/exclusions/0000-scope.source'
        'required/coordinator/exclusions/0001-body.source'
        'required/feedback/comments/101.source'
        'required/feedback/inline/103.source'
        'required/feedback/reviews/102.source'
        'required/pull/body.source'
    ) | Sort-Object -CaseSensitive
    $actualCoordinatorOnly = @(
        $trustedInventory.files |
            Where-Object {
                @($_.readRoles).Count -eq 1 -and
                [string]$_.readRoles[0] -ceq 'coordinator'
            } |
            ForEach-Object path |
            Sort-Object -CaseSensitive
    )
    if (@(
        Compare-Object $expectedCoordinatorOnly $actualCoordinatorOnly
    ).Count -ne 0) {
        throw 'The exact coordinator-only fixture role set changed.'
    }
    Write-Host '  [OK] valid-ready-v2-bundle'

    $sameWorker = Assert-ReviewFixture `
        -BundleRoot $sameWorkerRoot `
        -ContractPath $sameWorkerContractPath `
        -BundleName valid
    if ($sameWorker.Files -lt 43 -or $sameWorker.RequiredReferences -ne 11) {
        throw 'The single-guide lifecycle fixture changed unexpectedly.'
    }
    Write-Host '  [OK] same-worker-single-guide-bundle'

    $missing = Assert-ReviewFixture `
        -BundleRoot $missingRoot `
        -ContractPath $contractPath `
        -BundleName missingFeedback
    if ($missing.Files -ne $valid.Files - 1) {
        throw 'The missing-feedback fixture is not exactly the valid fixture minus feedback.json.'
    }
    Write-Host '  [OK] missing-feedback-derived-bundle'

    Invoke-MutationControl extra-fixture-file {
        param($control)
        [IO.File]::WriteAllText(
            (Join-Path $control.Root 'extra.source'),
            "extra`n",
            $utf8
        )
    } 'extra, missing, or orphan fixture file' -WithoutReconcile

    Invoke-MutationControl carriage-return-bytes {
        param($control)
        [IO.File]::WriteAllBytes(
            (Join-Path $control.Root 'diff.patch'),
            $utf8.GetBytes("line 1`r`nline 2`r`n")
        )
    } 'not LF-normalized'

    Invoke-MutationControl projected-file-patch {
        param($control)
        $files = @(Read-Json (Join-Path $control.Root 'files.json'))
        $files[0] | Add-Member -NotePropertyName patch -NotePropertyValue '@@'
        Write-Artifact $control 'files.json' $files
    } 'unexpected projected schema'

    Invoke-MutationControl frozen-target-identity {
        param($control)
        $manifest = Read-Json (Join-Path $control.Root 'manifest.json')
        $manifest.target.head = '0' * 40
        Write-JsonLf (Join-Path $control.Root 'manifest.json') $manifest
    } 'frozen target identity changed'

    Invoke-MutationControl malformed-pull-json {
        param($control)
        [IO.File]::WriteAllText(
            (Join-Path $control.Root 'pull.json'),
            "{ malformed`n",
            $utf8
        )
    } 'malformed JSON'

    Invoke-MutationControl non-object-pull-reference {
        param($control)
        $pull = Read-Json (Join-Path $control.Root 'pull.json')
        $pull.body = 'inline body'
        Write-Artifact $control 'pull.json' $pull
    } 'metadata is malformed'

    Invoke-MutationControl missing-required-text {
        param($control)
        Remove-Item (Join-Path $control.Root 'required/pull/body.source') -Force
    } 'required text is missing'

    Invoke-MutationControl tampered-required-text {
        param($control)
        [IO.File]::AppendAllText(
            (Join-Path $control.Root 'required/pull/body.source'),
            'tampered',
            $utf8
        )
    } 'wrong UTF-8 byte length'

    Invoke-MutationControl wrong-required-length {
        param($control)
        $pull = Read-Json (Join-Path $control.Root 'pull.json')
        $pull.body.utf8Bytes = [int]$pull.body.utf8Bytes + 1
        Write-Artifact $control 'pull.json' $pull
    } 'wrong UTF-8 byte length'

    Invoke-MutationControl wrong-required-hash {
        param($control)
        $pull = Read-Json (Join-Path $control.Root 'pull.json')
        $pull.body.sha256 = '0' * 64
        Write-Artifact $control 'pull.json' $pull
    } 'wrong SHA-256'

    Invoke-MutationControl escaping-required-path {
        param($control)
        $pull = Read-Json (Join-Path $control.Root 'pull.json')
        $pull.body.path = 'required/../escape.source'
        Write-Artifact $control 'pull.json' $pull
    } 'path is invalid'

    Invoke-MutationControl malformed-required-reference {
        param($control)
        $pull = Read-Json (Join-Path $control.Root 'pull.json')
        $pull.body.PSObject.Properties.Remove('sha256')
        Write-Artifact $control 'pull.json' $pull
    } 'unexpected projected schema'

    Invoke-MutationControl reused-required-reference {
        param($control)
        $manifest = Read-Json (Join-Path $control.Root 'manifest.json')
        $pull = Read-Json (Join-Path $control.Root 'pull.json')
        $manifest.exclusions[0].reason = $pull.body
        Write-JsonLf (Join-Path $control.Root 'manifest.json') $manifest
        Remove-Item (
            Join-Path $control.Root 'required/coordinator/exclusions/0000-reason.source'
        ) -Force
    } 'reference is reused'

    Invoke-MutationControl worker-assignment-source-role-order {
        param($control)
        $manifest = Read-Json (Join-Path $control.Root 'manifest.json')
        $manifest.guides[0].assignment.sourceRoles = @('head', 'baseTip', 'mergeBase')
        Write-JsonLf (Join-Path $control.Root 'manifest.json') $manifest
    } 'source roles changed'

    Invoke-MutationControl worker-assignment-sibling-policy {
        param($control)
        $manifest = Read-Json (Join-Path $control.Root 'manifest.json')
        $policyIndex = 0
        while ($manifest.guides[0].assignment.files[$policyIndex].kind -cne 'policy') {
            $policyIndex++
        }
        $manifest.guides[0].assignment.files[$policyIndex] =
            $manifest.guides[1].assignment.files |
                Where-Object kind -CEQ 'policy' |
                Select-Object -First 1
        Write-JsonLf (Join-Path $control.Root 'manifest.json') $manifest
    } 'incorrect policy inputs'

    Invoke-MutationControl worker-policy-guide-index-swap {
        param($control)
        $manifest = Read-Json (Join-Path $control.Root 'manifest.json')
        $firstGuide = [string]$manifest.policies[0].guide
        $manifest.policies[0].guide = [string]$manifest.policies[1].guide
        $manifest.policies[1].guide = $firstGuide
        $firstPolicy = $manifest.guides[0].assignment.files |
            Where-Object kind -CEQ 'policy' |
            Select-Object -First 1
        $secondPolicy = $manifest.guides[1].assignment.files |
            Where-Object kind -CEQ 'policy' |
            Select-Object -First 1
        for ($guideIndex = 0; $guideIndex -lt 2; $guideIndex++) {
            $replacement = if ($guideIndex -eq 0) {
                $secondPolicy
            } else {
                $firstPolicy
            }
            $policyIndex = 0
            while ($manifest.guides[$guideIndex].assignment.files[$policyIndex].kind -cne 'policy') {
                $policyIndex++
            }
            $manifest.guides[$guideIndex].assignment.files[$policyIndex] =
                $replacement
        }
        Write-JsonLf (Join-Path $control.Root 'manifest.json') $manifest
    } 'Policy path does not match guide'

    Invoke-MutationControl worker-policy-guide-case-mismatch {
        param($control)
        $manifest = Read-Json (Join-Path $control.Root 'manifest.json')
        $manifest.policies[0].guide =
            ([string]$manifest.policies[0].guide).ToUpperInvariant()
        $manifest.guides[0].assignment.files = @(
            $manifest.guides[0].assignment.files |
                Where-Object kind -CNE 'policy'
        )
        Write-JsonLf (Join-Path $control.Root 'manifest.json') $manifest
    } 'Policy guide does not match a routed guide'

    Invoke-MutationControl worker-assignment-missing-exclusion {
        param($control)
        $manifest = Read-Json (Join-Path $control.Root 'manifest.json')
        $manifest.guides[0].assignment.files = @(
            $manifest.guides[0].assignment.files |
                Where-Object {
                    [string]$_.path -cne
                    'required/guides/0000/exclusions/0001-body.source'
                }
        )
        Write-JsonLf (Join-Path $control.Root 'manifest.json') $manifest
    } 'incorrect exclusion inputs'

    Invoke-MutationControl orphan-required-text {
        param($control)
        [IO.File]::WriteAllText(
            (Join-Path $control.Root 'required/orphan.source'),
            "orphan`n",
            $utf8
        )
    } 'not a bijection'

    Invoke-MutationControl wrong-required-digest {
        param($control)
        $manifest = Read-Json (Join-Path $control.Root 'manifest.json')
        $manifest.requiredText.sha256 = '0' * 64
        Write-JsonLf (Join-Path $control.Root 'manifest.json') $manifest
    } 'root, file count, or digest changed' -PreserveRequiredDigest

    Invoke-MutationControl required-line-2049-bytes {
        param($control)
        $bytes = $utf8.GetBytes('x' * 2049)
        [IO.File]::WriteAllBytes(
            (Join-Path $control.Root 'required/pull/body.source'),
            $bytes
        )
        $pull = Read-Json (Join-Path $control.Root 'pull.json')
        $pull.body.utf8Bytes = $bytes.Length
        $pull.body.sha256 = Get-ReviewFixtureSha256 $bytes
        Write-Artifact $control 'pull.json' $pull
    } 'line exceeds 2048 UTF-8 bytes'

    Invoke-MutationControl serialized-json-line-2049-bytes {
        param($control)
        $pull = Read-Json (Join-Path $control.Root 'pull.json')
        $pull.title = 'x' * 2049
        Write-Artifact $control 'pull.json' $pull
    } 'Serialized JSON line exceeds'

    $missingExtra = Join-Path $testRoot 'missing-extra/bundle-missing-feedback'
    New-Item -ItemType Directory -Path (Split-Path $missingExtra -Parent) -Force |
        Out-Null
    Copy-Item $missingRoot $missingExtra -Recurse
    Copy-Item (Join-Path $validRoot 'feedback.json') $missingExtra
    Assert-Throws missing-feedback-unexpectedly-present {
        Assert-ReviewFixture `
            -BundleRoot $missingExtra `
            -ContractPath $contractPath `
            -BundleName missingFeedback
    } 'extra, missing, or orphan fixture file'

    Write-Host 'Review ready-v2 fixture controls passed.'
} finally {
    Remove-Item $testRoot -Recurse -Force
}
