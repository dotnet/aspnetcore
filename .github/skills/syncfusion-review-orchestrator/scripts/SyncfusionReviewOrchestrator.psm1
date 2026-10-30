#Requires -Version 7.0

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:SchemaVersion = 1
$script:ArmOrder = @("G", "M", "S", "O")
$script:ArmModels = [ordered]@{
    G = "gpt-5.6-sol"
    M = "gemini-4-argon"
    S = "gpt-6.1-sol"
    O = "claude-opus-5.5"
}
$script:ConsultationModels = @("gemini-4-argon", "claude-haiku-5.5")
$script:FalsifierModels = @("gpt-5.6-sol", "gemini-4-argon", "claude-haiku-5.5")
$script:TerminalArmOutcomes = @("FINDINGS", "NO_FINDINGS", "INCOMPLETE", "BLOCKED")
$script:ReceiptStatuses = @("accepted", "absent", "failed", "timed-out", "unknown")
$script:ReviewSkillPath = ".github/skills/review-pull-request/SKILL.md"
$script:NativeSkillTransform = "yaml-frontmatter-stripped-crlf-to-lf-v1"

function Assert-Sfro {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Test-SfroSha {
    param([object]$Value)

    return $Value -is [string] -and $Value -cmatch "^[0-9a-f]{40}$"
}

function Test-SfroSha256 {
    param([object]$Value)

    return $Value -is [string] -and $Value -cmatch "^[0-9a-f]{64}$"
}

function Test-SfroRfc3339 {
    param([object]$Value)

    if ($Value -isnot [string]) {
        return $false
    }

    $parsed = [datetimeoffset]::MinValue
    return [datetimeoffset]::TryParse(
        $Value,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind,
        [ref]$parsed)
}

function Assert-SfroProofArtifacts {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Receipt,
        [Parameter(Mandatory)][Collections.IDictionary]$Action
    )

    $Evidence = $Receipt.evidence
    Assert-Sfro ($Evidence.proofs -is [array] -and @($Evidence.proofs).Count -gt 0) "Accepted receipts require retained proof artifacts."
    $claims = Copy-SfroValue $Evidence
    $claims.Remove("proofs")
    $claimsSha256 = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $claims)
    $matchedEnvelope = $false
    foreach ($proof in @($Evidence.proofs)) {
        Assert-Sfro ($proof.path -is [string] -and (Test-Path -LiteralPath $proof.path -PathType Leaf)) "Receipt proof artifact is unavailable."
        Assert-Sfro (Test-SfroSha256 $proof.sha256) "Receipt proof artifact hash is invalid."
        Assert-Sfro ((Get-SfroFileSha256 $proof.path) -ceq $proof.sha256) "Receipt proof artifact hash mismatch."
        $envelope = ConvertFrom-SfroJson (Get-Content -LiteralPath $proof.path -Raw)
        $requiredKeys = @("schemaVersion", "intentId", "actionId", "kind", "source", "objectIds", "claimsSha256")
        if ($envelope -is [Collections.IDictionary] -and
            @($requiredKeys | Where-Object { -not $envelope.Contains($_) }).Count -eq 0 -and
            $envelope.schemaVersion -eq 1 -and
            $envelope.intentId -ceq $Receipt.intentId -and
            $envelope.actionId -ceq $Receipt.actionId -and
            $envelope.kind -ceq $Action.kind -and
            $envelope.source -in @("app-session-events", "github-adapter", "operator-import") -and
            $envelope.objectIds -is [array] -and
            @($envelope.objectIds).Count -gt 0 -and
            $envelope.claimsSha256 -ceq $claimsSha256) {
            $matchedEnvelope = $true
        }
    }
    Assert-Sfro $matchedEnvelope "No proof envelope is bound to this intent, action, and claim set."
}

function ConvertTo-SfroCanonicalValue {
    param([AllowNull()][object]$Value)

    if ($null -eq $Value) {
        return $null
    }

    if ($Value -is [datetime]) {
        return $Value.ToUniversalTime().ToString("O")
    }

    if ($Value -is [datetimeoffset]) {
        return $Value.ToUniversalTime().ToString("O")
    }

    if ($Value -is [Collections.IDictionary]) {
        $result = [ordered]@{}
        foreach ($key in @($Value.Keys | ForEach-Object { [string]$_ } | Sort-Object -CaseSensitive)) {
            $result[$key] = ConvertTo-SfroCanonicalValue $Value[$key]
        }

        return $result
    }

    if ($Value -is [Management.Automation.PSCustomObject]) {
        $result = [ordered]@{}
        foreach ($property in @($Value.PSObject.Properties | Sort-Object Name -CaseSensitive)) {
            $result[$property.Name] = ConvertTo-SfroCanonicalValue $property.Value
        }

        return $result
    }

    if ($Value -is [Collections.IEnumerable] -and $Value -isnot [string]) {
        $items = [object[]]@($Value | ForEach-Object { ConvertTo-SfroCanonicalValue $_ })
        return ,$items
    }

    return $Value
}

function ConvertTo-SfroCanonicalJson {
    param([AllowNull()][object]$Value)

    $canonical = ConvertTo-SfroCanonicalValue $Value
    return ConvertTo-Json -InputObject $canonical -Depth 100 -Compress
}

function Resolve-SfroProviderPath {
    param([Parameter(Mandatory)][string]$Path)

    return $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

function ConvertFrom-SfroJsonElement {
    param([Parameter(Mandatory)][Text.Json.JsonElement]$Element)

    switch ($Element.ValueKind) {
        ([Text.Json.JsonValueKind]::Object) {
            $result = [ordered]@{}
            foreach ($property in $Element.EnumerateObject()) {
                $result[$property.Name] = ConvertFrom-SfroJsonElement $property.Value
            }
            return $result
        }
        ([Text.Json.JsonValueKind]::Array) {
            $items = [object[]]@($Element.EnumerateArray() | ForEach-Object { ConvertFrom-SfroJsonElement $_ })
            return ,$items
        }
        ([Text.Json.JsonValueKind]::String) {
            return $Element.GetString()
        }
        ([Text.Json.JsonValueKind]::Number) {
            $integer = [int64]0
            if ($Element.TryGetInt64([ref]$integer)) {
                return $integer
            }
            return $Element.GetDecimal()
        }
        ([Text.Json.JsonValueKind]::True) {
            return $true
        }
        ([Text.Json.JsonValueKind]::False) {
            return $false
        }
        ([Text.Json.JsonValueKind]::Null) {
            return $null
        }
        default {
            throw "Unsupported JSON value kind '$($Element.ValueKind)'."
        }
    }
}

function ConvertFrom-SfroJson {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Json)

    $document = [Text.Json.JsonDocument]::Parse($Json)
    try {
        return ConvertFrom-SfroJsonElement $document.RootElement
    }
    finally {
        $document.Dispose()
    }
}

function Get-SfroSha256 {
    param([Parameter(Mandatory)][string]$Text)

    $bytes = [Text.UTF8Encoding]::new($false).GetBytes($Text)
    $hash = [Security.Cryptography.SHA256]::HashData($bytes)
    return [Convert]::ToHexString($hash).ToLowerInvariant()
}

function Get-SfroFileSha256 {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = (Resolve-Path -LiteralPath $Path).Path
    $stream = [IO.File]::OpenRead($resolved)
    try {
        $hash = [Security.Cryptography.SHA256]::HashData($stream)
        return [Convert]::ToHexString($hash).ToLowerInvariant()
    }
    finally {
        $stream.Dispose()
    }
}

function Get-SfroNativeSkillIdentity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [string]$RepositoryRelativePath = $script:ReviewSkillPath
    )

    Assert-Sfro ($RepositoryRelativePath -ceq $script:ReviewSkillPath) "The native identity path must be '$script:ReviewSkillPath'."
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    $normalizedResolved = $resolved.Replace("\", "/")
    Assert-Sfro ($normalizedResolved.EndsWith("/$RepositoryRelativePath", [StringComparison]::Ordinal)) "The skill file path does not match '$RepositoryRelativePath'."

    $bytes = [IO.File]::ReadAllBytes($resolved)
    Assert-Sfro (-not ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)) "The skill file has a UTF-8 BOM."
    try {
        $text = [Text.UTF8Encoding]::new($false, $true).GetString($bytes)
    }
    catch {
        throw "The skill file is not strict UTF-8."
    }

    $normalized = $text.Replace("`r`n", "`n")
    Assert-Sfro (-not $normalized.Contains("`r")) "The skill file has a lone CR byte."
    Assert-Sfro ($normalized.StartsWith("---`n", [StringComparison]::Ordinal)) "The skill file must start with YAML frontmatter."
    $closing = $normalized.IndexOf("`n---`n", 4, [StringComparison]::Ordinal)
    Assert-Sfro ($closing -ge 4) "The skill file has no exact YAML frontmatter closing delimiter."
    $bodyStart = $closing + 5
    Assert-Sfro ($bodyStart -lt $normalized.Length -and $normalized[$bodyStart] -eq "`n") "The skill frontmatter must be followed by one blank separator line."
    $nativeBody = $normalized.Substring($bodyStart + 1)
    Assert-Sfro ($nativeBody.Length -gt 0) "The native skill body is empty."
    $normalizedBytes = [Text.UTF8Encoding]::new($false).GetBytes($normalized)
    $headerBytes = [Text.Encoding]::ASCII.GetBytes("blob $($normalizedBytes.Length)`0")
    $gitObjectBytes = [byte[]]::new($headerBytes.Length + $normalizedBytes.Length)
    [Array]::Copy($headerBytes, 0, $gitObjectBytes, 0, $headerBytes.Length)
    [Array]::Copy($normalizedBytes, 0, $gitObjectBytes, $headerBytes.Length, $normalizedBytes.Length)

    return [ordered]@{
        repositoryRelativePath = $RepositoryRelativePath
        transform = $script:NativeSkillTransform
        rawFileSha256 = Get-SfroFileSha256 $resolved
        normalizedSkillFileSha256 = Get-SfroSha256 $normalized
        loadedBodySha256 = Get-SfroSha256 $nativeBody
        normalizedGitBlob = [Convert]::ToHexString([Security.Cryptography.SHA1]::HashData($gitObjectBytes)).ToLowerInvariant()
        normalizedUtf8Bytes = [Text.UTF8Encoding]::new($false).GetByteCount($normalized)
        loadedBodyUtf8Bytes = [Text.UTF8Encoding]::new($false).GetByteCount($nativeBody)
    }
}

function Get-SfroGitBlobIdentity {
    param(
        [Parameter(Mandatory)][string]$SkillPath,
        [Parameter(Mandatory)][string]$ReviewerCommit,
        [string]$RepositoryRelativePath = $script:ReviewSkillPath
    )

    Assert-Sfro (Test-SfroSha $ReviewerCommit) "The reviewer commit is invalid."
    $resolved = (Resolve-Path -LiteralPath $SkillPath).Path
    $normalizedResolved = $resolved.Replace("\", "/")
    Assert-Sfro ($normalizedResolved.EndsWith("/$RepositoryRelativePath", [StringComparison]::Ordinal)) "The skill file is outside the expected repository path."
    $repositoryRoot = $normalizedResolved.Substring(0, $normalizedResolved.Length - $RepositoryRelativePath.Length - 1)
    $blob = (& git -C $repositoryRoot rev-parse "$ReviewerCommit`:$RepositoryRelativePath").Trim()
    Assert-Sfro ($LASTEXITCODE -eq 0 -and (Test-SfroSha $blob)) "The reviewer skill Git blob could not be resolved."
    return $blob
}

function Get-SfroStableId {
    param(
        [Parameter(Mandatory)][string]$Prefix,
        [Parameter(Mandatory)][object]$Value
    )

    $hash = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $Value)
    return "$Prefix-$($hash.Substring(0, 24))"
}

function Resolve-SfroReviewTriggerDecision {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$CampaignId,
        [Parameter(Mandatory)][int]$Pr,
        [Parameter(Mandatory)][string]$Head,
        [Collections.IEnumerable]$KnownTriggers = @()
    )

    Assert-Sfro (Test-SfroSha $Head) "Review-trigger head must be a 40-character SHA."
    $matching = @($KnownTriggers | Where-Object {
        $_.campaignId -ceq $CampaignId -and
        [int]$_.pr -eq $Pr -and
        $_.head -ceq $Head -and
        $_.command -ceq "/review" -and
        [bool]$_.readbackVerified
    })
    Assert-Sfro ($matching.Count -le 1) "More than one verified /review trigger exists for the exact head."

    if ($matching.Count -eq 1) {
        return [ordered]@{
            decision = "reused"
            trigger = Copy-SfroValue $matching[0]
            key = "${CampaignId}:${Pr}:${Head}:review-trigger"
        }
    }

    return [ordered]@{
        decision = "post"
        trigger = $null
        key = "${CampaignId}:${Pr}:${Head}:review-trigger"
    }
}

function Copy-SfroValue {
    param([AllowNull()][object]$Value)

    if ($null -eq $Value) {
        return $null
    }

    return ConvertFrom-SfroJson (ConvertTo-SfroCanonicalJson $Value)
}

function Assert-SfroCampaignConfiguration {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Configuration,
        [switch]$AllowFixtureQueue
    )

    Assert-Sfro ($Configuration.schemaVersion -eq $script:SchemaVersion) "Unsupported campaign configuration schema."
    Assert-Sfro ($Configuration.campaignId -is [string] -and $Configuration.campaignId.Length -gt 0) "campaignId is required."
    Assert-Sfro ($Configuration.repository -is [string] -and $Configuration.repository -cmatch "^[^/]+/[^/]+$") "repository must be owner/name."
    Assert-Sfro ($Configuration.rootSessionId -is [string] -and $Configuration.rootSessionId.Length -gt 0) "rootSessionId is required."
    Assert-Sfro ($Configuration.queue -is [Collections.IEnumerable]) "queue is required."

    $queue = @($Configuration.queue)
    if (-not $AllowFixtureQueue) {
        Assert-Sfro ($queue.Count -eq 33) "The production campaign queue must contain exactly 33 entries."
    }
    else {
        Assert-Sfro ($queue.Count -ge 1) "A fixture queue must contain at least one entry."
    }

    $ordinals = @($queue | ForEach-Object { [int]$_.ordinal })
    $prs = @($queue | ForEach-Object { [int]$_.pr })
    Assert-Sfro (@($ordinals | Sort-Object -Unique).Count -eq $queue.Count) "Queue ordinals must be unique."
    Assert-Sfro (@($prs | Sort-Object -Unique).Count -eq $queue.Count) "Queue PR numbers must be unique."

    for ($index = 0; $index -lt $queue.Count; $index++) {
        $entry = $queue[$index]
        Assert-Sfro ([int]$entry.ordinal -eq ($index + 1)) "Queue ordinals must be contiguous and ordered."
        Assert-Sfro ([int]$entry.pr -gt 0) "Queue PR numbers must be positive."
        Assert-Sfro ($entry.lane -in @("ReviewNow", "NeedsRescue", "WaitingOnCI")) "Queue lane '$($entry.lane)' is unsupported."
    }
}

function New-SfroEmptyState {
    return [ordered]@{
        schemaVersion = $script:SchemaVersion
        revision = 0
        campaignId = $null
        repository = $null
        rootSessionId = $null
        configurationHash = $null
        importMetadata = $null
        authority = $false
        queue = @()
        activePr = $null
        entries = [ordered]@{}
        intents = [ordered]@{}
        blockers = @()
        lastEventHash = ("0" * 64)
    }
}

function New-SfroEntry {
    param([Parameter(Mandatory)][Collections.IDictionary]$QueueEntry)

    return [ordered]@{
        ordinal = [int]$QueueEntry.ordinal
        pr = [int]$QueueEntry.pr
        lane = [string]$QueueEntry.lane
        status = "pending"
        consultations = [ordered]@{}
        coordinatorSessionId = $null
        epoch = $null
        hostedReview = $null
        arms = [ordered]@{}
        assessment = $null
        falsifiers = [ordered]@{}
        finalFindings = $null
        publication = $null
        cleanup = $null
        handoff = $null
        coordinatorArchived = $false
        blocker = $null
        abortReason = $null
        imported = $false
    }
}

function Get-SfroActionId {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$State,
        [Parameter(Mandatory)][int]$Pr,
        [Parameter(Mandatory)][string]$Kind,
        [string]$Discriminator = ""
    )

    $entry = $State.entries[[string]$Pr]
    $head = if ($null -ne $entry.epoch) { $entry.epoch.head } else { "unfrozen" }
    return Get-SfroStableId "action" ([ordered]@{
        campaignId = $State.campaignId
        pr = $Pr
        head = $head
        kind = $Kind
        discriminator = $Discriminator
    })
}

function New-SfroAction {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$State,
        [Parameter(Mandatory)][int]$Pr,
        [Parameter(Mandatory)][ValidateSet("campaign-root", "pr-coordinator")][string]$Actor,
        [Parameter(Mandatory)][string]$Kind,
        [string]$Discriminator = "",
        [Collections.IDictionary]$Payload = [ordered]@{}
    )

    $actionId = Get-SfroActionId -State $State -Pr $Pr -Kind $Kind -Discriminator $Discriminator
    return [ordered]@{
        schemaVersion = $script:SchemaVersion
        actionId = $actionId
        campaignId = $State.campaignId
        pr = $Pr
        actor = $Actor
        kind = $Kind
        discriminator = $Discriminator
        payload = $Payload
    }
}

function Get-SfroLedgerEventsFromText {
    param([AllowEmptyString()][string]$Text)

    if ($Text.Length -eq 0) {
        return @()
    }

    Assert-Sfro ($Text.EndsWith("`n")) "The ledger has a partial trailing line."
    $lines = @($Text -split "`n" | Where-Object { $_.Length -gt 0 })
    $events = @()
    $previousHash = "0" * 64

    for ($index = 0; $index -lt $lines.Count; $index++) {
        $event = ConvertFrom-SfroJson $lines[$index]
        $expectedSequence = $index + 1
        Assert-Sfro ($event.schemaVersion -eq $script:SchemaVersion) "Ledger event $expectedSequence has an unsupported schema."
        Assert-Sfro ([int]$event.sequence -eq $expectedSequence) "Ledger sequence gap at event $expectedSequence."
        Assert-Sfro ($event.previousHash -ceq $previousHash) "Ledger previous-hash mismatch at event $expectedSequence."
        Assert-Sfro (Test-SfroSha256 $event.hash) "Ledger event $expectedSequence has an invalid hash."

        $storedHash = [string]$event.hash
        $event.Remove("hash")
        $calculated = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $event)
        $event.hash = $storedHash
        Assert-Sfro ($calculated -ceq $event.hash) "Ledger event-hash mismatch at event $expectedSequence."
        $previousHash = $event.hash
        $events += ,$event
    }

    return $events
}

function Read-SfroLedger {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$LedgerPath)

    if (-not (Test-Path -LiteralPath $LedgerPath)) {
        return @()
    }

    $text = [IO.File]::ReadAllText(
        (Resolve-Path -LiteralPath $LedgerPath).Path,
        [Text.UTF8Encoding]::new($false))
    return @(Get-SfroLedgerEventsFromText $text)
}

function Add-SfroLedgerEvent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LedgerPath,
        [Parameter(Mandatory)][string]$Type,
        [Parameter(Mandatory)][Collections.IDictionary]$Payload,
        [Parameter(Mandatory)][int]$ExpectedRevision
    )

    $resolvedLedgerPath = Resolve-SfroProviderPath $LedgerPath
    $parent = Split-Path -Parent $resolvedLedgerPath
    if ($parent -and -not (Test-Path -LiteralPath $parent)) {
        [IO.Directory]::CreateDirectory($parent) | Out-Null
    }

    $stream = [IO.File]::Open(
        $resolvedLedgerPath,
        [IO.FileMode]::OpenOrCreate,
        [IO.FileAccess]::ReadWrite,
        [IO.FileShare]::None)
    try {
        $reader = [IO.StreamReader]::new(
            $stream,
            [Text.UTF8Encoding]::new($false),
            $true,
            4096,
            $true)
        try {
            $text = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $events = @(Get-SfroLedgerEventsFromText $text)
        Assert-Sfro ($events.Count -eq $ExpectedRevision) "Stale ledger revision. Expected $ExpectedRevision, found $($events.Count)."
        $previousHash = if ($events.Count -eq 0) { "0" * 64 } else { $events[-1].hash }
        $event = [ordered]@{
            schemaVersion = $script:SchemaVersion
            sequence = $events.Count + 1
            eventId = [guid]::NewGuid().ToString("D")
            recordedAtUtc = [datetimeoffset]::UtcNow.ToString("O")
            type = $Type
            previousHash = $previousHash
            payload = Copy-SfroValue $Payload
        }
        $event.hash = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $event)
        $line = (ConvertTo-SfroCanonicalJson $event) + "`n"
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes($line)
        $stream.Seek(0, [IO.SeekOrigin]::End) | Out-Null
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush($true)
        return $event
    }
    finally {
        $stream.Dispose()
    }
}

function Set-SfroArmFailure {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Entry,
        [Parameter(Mandatory)][string]$Arm,
        [Parameter(Mandatory)][Collections.IDictionary]$Receipt
    )

    $failedSessionId = if ($Receipt.evidence.Contains("sessionId")) {
        $Receipt.evidence.sessionId
    }
    else {
        $null
    }
    if (-not $Entry.arms.Contains($Arm)) {
        $Entry.arms[$Arm] = [ordered]@{
            arm = $Arm
            model = $script:ArmModels[$Arm]
            sessionId = $failedSessionId
            nonce = $null
            status = "terminal"
            outcome = "INCOMPLETE"
            handshake = $null
            assignment = $null
            terminal = $null
            failure = $Receipt.evidence
        }
    }
    else {
        $Entry.arms[$Arm].status = "terminal"
        $Entry.arms[$Arm].outcome = "INCOMPLETE"
        $Entry.arms[$Arm].failure = $Receipt.evidence
    }
}

function Apply-SfroReceiptToState {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$State,
        [Parameter(Mandatory)][Collections.IDictionary]$Action,
        [Parameter(Mandatory)][Collections.IDictionary]$Receipt
    )

    $entry = $State.entries[[string][int]$Action.pr]
    if ($Receipt.status -eq "unknown") {
        return
    }

    if ($Receipt.status -eq "absent") {
        return
    }

    if ($Receipt.status -ne "accepted") {
        if ($Action.kind -in @("create-arm", "verify-arm", "assign-arm", "observe-arm")) {
            Set-SfroArmFailure -Entry $entry -Arm $Action.payload.arm -Receipt $Receipt
        }
        elseif ($Action.kind -eq "accept-import" -or $Action.kind -in @("cleanup-children", "coordinator-handoff", "archive-coordinator")) {
            $entry.blocker = [ordered]@{
                kind = $Action.kind
                status = $Receipt.status
                evidence = $Receipt.evidence
            }
            $State.blockers += ,([ordered]@{
                pr = [int]$Action.pr
                kind = $Action.kind
                status = $Receipt.status
            })
        }
        elseif ($null -ne $entry.coordinatorSessionId) {
            if ($null -eq $entry.abortReason) {
                $entry.abortReason = [ordered]@{
                    kind = $Action.kind
                    status = $Receipt.status
                    evidence = $Receipt.evidence
                }
            }
            $entry.status = "aborting"
            if ($Action.kind -in @("publish-review", "publish-clean")) {
                $entry.publication = [ordered]@{
                    kind = "withheld"
                    evidence = Copy-SfroValue $Receipt.evidence
                }
            }
        }
        else {
            $entry.abortReason = [ordered]@{
                kind = $Action.kind
                status = $Receipt.status
                evidence = $Receipt.evidence
            }
            $entry.status = "terminal"
            $entry.coordinatorArchived = $true
            if ($State.activePr -eq [int]$Action.pr) {
                $State.activePr = $null
            }
        }

        return
    }

    switch ($Action.kind) {
        "accept-import" {
            $State.authority = $true
            if ($State.importMetadata.Contains("completedFacts") -and $null -ne $State.importMetadata.completedFacts) {
                foreach ($property in @($State.importMetadata.completedFacts.Keys)) {
                    if ($State.entries.Contains([string]$property)) {
                        $completedEntry = $State.entries[[string]$property]
                        $completedEntry.status = "terminal"
                        $completedEntry.imported = $true
                        $completedEntry.handoff = [ordered]@{
                            imported = $true
                            legacy = Copy-SfroValue $State.importMetadata.completedFacts[$property]
                        }
                        $completedEntry.coordinatorArchived = $true
                    }
                }
            }
            if ($State.importMetadata.Contains("activeCase") -and $null -ne $State.importMetadata.activeCase) {
                $importedActivePr = [int]$State.importMetadata.activeCase.pr
                Assert-Sfro ($State.entries.Contains([string]$importedActivePr)) "Imported active PR is not in the queue."
                $State.activePr = $importedActivePr
                $State.entries[[string]$importedActivePr].status = "imported-active"
                $State.entries[[string]$importedActivePr].imported = $true
            }
        }
        "reconcile-imported-active" {
            if ($Receipt.evidence.disposition -eq "terminal") {
                $entry.status = "terminal"
                $entry.handoff = Copy-SfroValue $Receipt.evidence
                $entry.coordinatorArchived = $true
                $State.activePr = $null
            }
            else {
                $entry.status = "active"
                $entry.coordinatorSessionId = [string]$Receipt.evidence.coordinatorSessionId
                foreach ($model in $script:ConsultationModels) {
                    $entry.consultations[$model] = [ordered]@{
                        imported = $true
                        verdict = "ACCEPT"
                        artifactSha256 = $Receipt.evidence.artifactSha256
                    }
                }
            }
        }
        "launch-consultation" {
            $entry.status = "preparing"
            $State.activePr = [int]$Action.pr
            $entry.consultations[$Action.payload.model] = Copy-SfroValue $Receipt.evidence
        }
        "create-coordinator" {
            $entry.status = "active"
            $entry.coordinatorSessionId = [string]$Receipt.evidence.sessionId
        }
        "freeze-epoch" {
            $entry.epoch = [ordered]@{
                reviewerCommit = [string]$Receipt.evidence.reviewerCommit
                skillBlob = [string]$Receipt.evidence.skillBlob
                skillPath = [string]$Receipt.evidence.skillPath
                skillIdentityTransform = [string]$Receipt.evidence.skillIdentityTransform
                normalizedSkillFileSha256 = [string]$Receipt.evidence.normalizedSkillFileSha256
                loadedBodySha256 = [string]$Receipt.evidence.loadedBodySha256
                head = [string]$Receipt.evidence.head
                mergeBase = [string]$Receipt.evidence.mergeBase
                baseRef = [string]$Receipt.evidence.baseRef
                baseTip = [string]$Receipt.evidence.baseTip
                feedbackSha256 = [string]$Receipt.evidence.feedbackSha256
                diffSha256 = [string]$Receipt.evidence.diffSha256
                guidanceMode = [string]$Receipt.evidence.guidanceMode
                readyV2BundleSha256 = $null
                bundleFeedbackSha256 = $null
            }
        }
        "ensure-review-trigger" {
            $entry.hostedReview = Copy-SfroValue $Receipt.evidence
            $entry.hostedReview.terminalObserved = $false
        }
        "observe-hosted-review" {
            $entry.hostedReview.terminalObserved = $true
            $entry.hostedReview.hostedIssueCommentIds = Copy-SfroValue $Receipt.evidence.hostedIssueCommentIds
            $entry.hostedReview.hostedReviewIds = Copy-SfroValue $Receipt.evidence.hostedReviewIds
            $entry.hostedReview.hostedReviewCommentIds = Copy-SfroValue $Receipt.evidence.hostedReviewCommentIds
            $entry.hostedReview.resultSha256 = [string]$Receipt.evidence.resultSha256
            $entry.hostedReview.rawFeedbackSha256 = [string]$Receipt.evidence.rawFeedbackSha256
            $entry.hostedReview.effectiveFeedbackSha256 = [string]$Receipt.evidence.effectiveFeedbackSha256
            $entry.epoch.feedbackSha256 = [string]$Receipt.evidence.effectiveFeedbackSha256
        }
        "prepare-review-bundle" {
            $entry.epoch.readyV2BundleSha256 = [string]$Receipt.evidence.readyV2BundleSha256
            $entry.epoch.bundleFeedbackSha256 = [string]$Receipt.evidence.bundleFeedbackSha256
        }
        "create-arm" {
            $arm = [string]$Action.payload.arm
            $entry.arms[$arm] = [ordered]@{
                arm = $arm
                model = [string]$Action.payload.model
                sessionId = [string]$Receipt.evidence.sessionId
                nonce = [string]$Receipt.evidence.nonce
                status = "created"
                outcome = $null
                handshake = $null
                assignment = $null
                terminal = $null
            }
        }
        "verify-arm" {
            $arm = [string]$Action.payload.arm
            $entry.arms[$arm].status = "verified"
            $entry.arms[$arm].handshake = Copy-SfroValue $Receipt.evidence
        }
        "assign-arm" {
            $arm = [string]$Action.payload.arm
            $entry.arms[$arm].status = "assigned"
            $entry.arms[$arm].assignment = Copy-SfroValue $Receipt.evidence
        }
        "observe-arm" {
            $arm = [string]$Action.payload.arm
            $entry.arms[$arm].status = "terminal"
            $entry.arms[$arm].outcome = [string]$Receipt.evidence.outcome
            $entry.arms[$arm].terminal = Copy-SfroValue $Receipt.evidence
        }
        "run-assessment" {
            $entry.assessment = Copy-SfroValue $Receipt.evidence
        }
        "run-falsifier" {
            $entry.falsifiers[$Action.payload.model] = Copy-SfroValue $Receipt.evidence
        }
        "finalize-findings" {
            $entry.finalFindings = Copy-SfroValue $Receipt.evidence
        }
        "publish-review" {
            $entry.publication = [ordered]@{
                kind = "review"
                evidence = Copy-SfroValue $Receipt.evidence
            }
        }
        "publish-clean" {
            $entry.publication = [ordered]@{
                kind = "clean"
                evidence = Copy-SfroValue $Receipt.evidence
            }
        }
        "cleanup-children" {
            $entry.cleanup = Copy-SfroValue $Receipt.evidence
            $entry.cleanup.controllerReceiptSha256 = $Receipt.receiptSha256
        }
        "coordinator-handoff" {
            $entry.handoff = Copy-SfroValue $Receipt.evidence
            $entry.handoff.controllerReceiptSha256 = $Receipt.receiptSha256
        }
        "archive-coordinator" {
            $entry.coordinatorArchived = $true
            $entry.status = "terminal"
            $State.activePr = $null
        }
        default {
            throw "Unsupported accepted action '$($Action.kind)'."
        }
    }
}

function Get-SfroState {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$LedgerPath)

    $events = @(Read-SfroLedger -LedgerPath $LedgerPath)
    $state = New-SfroEmptyState

    foreach ($event in $events) {
        switch ($event.type) {
            "campaign.created" {
                Assert-Sfro ($state.revision -eq 0) "campaign.created must be the first event."
                $configuration = $event.payload.configuration
                Assert-SfroCampaignConfiguration -Configuration $configuration -AllowFixtureQueue:([bool]$event.payload.allowFixtureQueue)
                $state.campaignId = [string]$configuration.campaignId
                $state.repository = [string]$configuration.repository
                $state.rootSessionId = [string]$configuration.rootSessionId
                $state.configurationHash = [string]$event.payload.configurationHash
                $state.importMetadata = if ($configuration.Contains("import")) {
                    Copy-SfroValue $configuration.import
                }
                else {
                    [ordered]@{ sourceHashes = [ordered]@{} }
                }
                $state.authority = -not [bool]$event.payload.importRequired
                $state.queue = @($configuration.queue | ForEach-Object { [int]$_.pr })
                foreach ($queueEntry in $configuration.queue) {
                    $state.entries[[string][int]$queueEntry.pr] = New-SfroEntry $queueEntry
                }
            }
            "intent.reserved" {
                $intent = $event.payload.intent
                Assert-Sfro (-not $state.intents.Contains([string]$intent.intentId)) "Duplicate intent id '$($intent.intentId)'."
                $state.intents[[string]$intent.intentId] = [ordered]@{
                    intentId = [string]$intent.intentId
                    action = Copy-SfroValue $intent.action
                    reservedAtUtc = [string]$intent.reservedAtUtc
                    deadlineUtc = [string]$intent.deadlineUtc
                    status = "reserved"
                    receipts = @()
                }
            }
            "intent.receipted" {
                $receipt = $event.payload.receipt
                Assert-Sfro ($state.intents.Contains([string]$receipt.intentId)) "Receipt references an unknown intent."
                $intent = $state.intents[[string]$receipt.intentId]
                $intent.receipts += ,(Copy-SfroValue $receipt)
                $intent.status = [string]$receipt.status
                Apply-SfroReceiptToState -State $state -Action $intent.action -Receipt $receipt
            }
            default {
                throw "Unsupported ledger event type '$($event.type)'."
            }
        }

        $state.revision = [int]$event.sequence
        $state.lastEventHash = [string]$event.hash
    }

    return $state
}

function New-SfroCampaign {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LedgerPath,
        [Parameter(Mandatory)][string]$ConfigPath,
        [switch]$ImportRequired,
        [switch]$AllowFixtureQueue,
        [switch]$DryRun
    )

    Assert-Sfro (-not (Test-Path -LiteralPath $LedgerPath) -or (Get-Item -LiteralPath $LedgerPath).Length -eq 0) "The target ledger already contains data."
    $configuration = ConvertFrom-SfroJson (Get-Content -LiteralPath $ConfigPath -Raw)
    Assert-SfroCampaignConfiguration -Configuration $configuration -AllowFixtureQueue:$AllowFixtureQueue
    $payload = [ordered]@{
        configuration = $configuration
        importRequired = [bool]$ImportRequired -or
            ($configuration.Contains("import") -and [bool]$configuration.import.requiresExplicitAcceptance)
        allowFixtureQueue = [bool]$AllowFixtureQueue
        configurationHash = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $configuration)
    }

    if ($DryRun) {
        return [ordered]@{
            dryRun = $true
            eventType = "campaign.created"
            payload = $payload
        }
    }

    Add-SfroLedgerEvent -LedgerPath $LedgerPath -Type "campaign.created" -Payload $payload -ExpectedRevision 0 | Out-Null
    return Get-SfroState -LedgerPath $LedgerPath
}

function Get-SfroPendingIntent {
    param([Parameter(Mandatory)][Collections.IDictionary]$State)

    $pending = @($State.intents.Values | Where-Object { $_.status -in @("reserved", "unknown") })
    Assert-Sfro ($pending.Count -le 1) "More than one unresolved intent exists."
    if ($pending.Count -eq 1) {
        return $pending[0]
    }

    return $null
}

function Test-SfroAllArmsTerminal {
    param([Parameter(Mandatory)][Collections.IDictionary]$Entry)

    foreach ($arm in $script:ArmOrder) {
        if (-not $Entry.arms.Contains($arm) -or $Entry.arms[$arm].status -ne "terminal") {
            return $false
        }
    }

    return $true
}

function Test-SfroCleanPanel {
    param([Parameter(Mandatory)][Collections.IDictionary]$Entry)

    if (-not (Test-SfroAllArmsTerminal $Entry)) {
        return $false
    }

    foreach ($arm in $script:ArmOrder) {
        if ($Entry.arms[$arm].outcome -ne "NO_FINDINGS") {
            return $false
        }
    }

    return $null -ne $Entry.assessment -and @($Entry.assessment.survivors).Count -eq 0
}

function Get-SfroNextAction {
    [CmdletBinding()]
    param([Parameter(Mandatory)][Collections.IDictionary]$State)

    Assert-Sfro ($null -ne $State.campaignId) "The campaign is not initialized."

    $pendingIntent = Get-SfroPendingIntent $State
    if ($null -ne $pendingIntent) {
        return [ordered]@{
            schemaVersion = $script:SchemaVersion
            actionId = $pendingIntent.action.actionId
            intentId = $pendingIntent.intentId
            campaignId = $State.campaignId
            pr = [int]$pendingIntent.action.pr
            actor = [string]$pendingIntent.action.actor
            kind = [string]$pendingIntent.action.kind
            reconcile = $true
            intentReservedAtUtc = $pendingIntent.reservedAtUtc
            payload = [ordered]@{
                originalAction = $pendingIntent.action
                priorReceipts = $pendingIntent.receipts
                rule = "Observe the original effect. Never repeat it while the outcome is unknown."
            }
        }
    }

    if ($State.blockers.Count -gt 0) {
        return $null
    }

    if (-not $State.authority) {
        $firstPr = [int]$State.queue[0]
        return New-SfroAction -State $State -Pr $firstPr -Actor "campaign-root" -Kind "accept-import" -Payload ([ordered]@{
            queueHash = Get-SfroSha256 (ConvertTo-SfroCanonicalJson @($State.queue | ForEach-Object {
                $queueEntry = $State.entries[[string]$_]
                [ordered]@{
                    ordinal = $queueEntry.ordinal
                    pr = $queueEntry.pr
                    lane = $queueEntry.lane
                }
            }))
            entryCount = $State.queue.Count
            configurationHash = $State.configurationHash
            sourceHashes = $State.importMetadata.sourceHashes
        })
    }

    $activePr = $State.activePr
    if ($null -eq $activePr) {
        $next = @($State.queue | Where-Object { $State.entries[[string]$_].status -eq "pending" } | Select-Object -First 1)
        if ($next.Count -eq 0) {
            return $null
        }

        $activePr = [int]$next[0]
    }

    $entry = $State.entries[[string][int]$activePr]
    if ($entry.status -eq "imported-active") {
        return New-SfroAction -State $State -Pr $activePr -Actor "campaign-root" -Kind "reconcile-imported-active" -Payload ([ordered]@{
            importedFacts = $State.importMetadata
            allowedDispositions = @("terminal", "adopt")
            rule = "Do not create a replacement coordinator until the imported active session is reconciled."
        })
    }

    if ($entry.status -eq "aborting") {
        if ($null -eq $entry.cleanup) {
            $sessionIds = @($entry.arms.Values | ForEach-Object { $_.sessionId } | Where-Object { $_ })
            return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "cleanup-children" -Payload ([ordered]@{
                coordinatorSessionId = $entry.coordinatorSessionId
                requiredTerminalSessionIds = $sessionIds
                preserveEvidence = $true
                abortReason = $entry.abortReason
            })
        }
        if ($null -eq $entry.handoff) {
            return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "coordinator-handoff" -Payload ([ordered]@{
                coordinatorSessionId = $entry.coordinatorSessionId
                head = $entry.epoch?.head
                childCleanupHash = $entry.cleanup.controllerReceiptSha256
                requireTerminal = $true
                abortReason = $entry.abortReason
            })
        }
        if (-not $entry.coordinatorArchived) {
            return New-SfroAction -State $State -Pr $activePr -Actor "campaign-root" -Kind "archive-coordinator" -Payload ([ordered]@{
                coordinatorSessionId = $entry.coordinatorSessionId
                handoffSha256 = $entry.handoff.controllerReceiptSha256
                requireNoActiveChildren = $true
            })
        }
    }

    if ($null -eq $entry.coordinatorSessionId) {
        foreach ($model in $script:ConsultationModels) {
            if (-not $entry.consultations.Contains($model)) {
                return New-SfroAction -State $State -Pr $activePr -Actor "campaign-root" -Kind "launch-consultation" -Discriminator $model -Payload ([ordered]@{
                    model = $model
                    readOnly = $true
                    scope = @(
                        "hierarchy",
                        "frozen-identities",
                        "review-trigger-dedupe",
                        "arm-model-and-ownership",
                        "startup",
                        "failure-handling",
                        "assessment",
                        "publication",
                        "cleanup"
                    )
                })
            }
        }

        $nonce = Get-SfroStableId "nonce" ([ordered]@{
            campaignId = $State.campaignId
            pr = $activePr
            kind = "coordinator"
        })
        return New-SfroAction -State $State -Pr $activePr -Actor "campaign-root" -Kind "create-coordinator" -Payload ([ordered]@{
            deterministicName = "Syncfusion PR $activePr coordinator"
            nonce = $nonce
            coordinateWithCreator = $true
            mode = "autopilot"
            requiredParentSessionId = $State.rootSessionId
        })
    }

    if ($null -eq $entry.epoch) {
        return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "freeze-epoch" -Payload ([ordered]@{
            coordinatorSessionId = $entry.coordinatorSessionId
            guidanceMode = "target-base"
            skillIdentityTransform = $script:NativeSkillTransform
            requiredIdentities = @(
                "reviewerCommit",
                "skillBlob",
                "skillPath",
                "normalizedSkillFileSha256",
                "loadedBodySha256",
                "head",
                "mergeBase",
                "baseRef",
                "baseTip",
                "feedbackSha256",
                "diffSha256"
            )
        })
    }

    if ($null -eq $entry.hostedReview) {
        return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "ensure-review-trigger" -Payload ([ordered]@{
            coordinatorSessionId = $entry.coordinatorSessionId
            head = $entry.epoch.head
            baseRef = $entry.epoch.baseRef
            baseTip = $entry.epoch.baseTip
            diffSha256 = $entry.epoch.diffSha256
            feedbackSha256 = $entry.epoch.feedbackSha256
            command = "/review"
            key = "$($State.campaignId):${activePr}:$($entry.epoch.head):review-trigger"
            maximum = 1
        })
    }
    if (-not [bool]$entry.hostedReview.terminalObserved) {
        return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "observe-hosted-review" -Payload ([ordered]@{
            head = $entry.epoch.head
            baseRef = $entry.epoch.baseRef
            baseTip = $entry.epoch.baseTip
            diffSha256 = $entry.epoch.diffSha256
            feedbackSha256 = $entry.epoch.feedbackSha256
            triggerCommentId = [int64]$entry.hostedReview.commentId
            feedbackExclusions = [ordered]@{
                issueCommentIds = @([int64]$entry.hostedReview.commentId)
                reviewIds = @()
                reviewCommentIds = @()
            }
            rule = "Wait for the hosted run to reach a terminal state, then establish the post-trigger feedback baseline including any hosted output."
        })
    }
    if (-not $entry.epoch.Contains("readyV2BundleSha256") -or $null -eq $entry.epoch.readyV2BundleSha256) {
        return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "prepare-review-bundle" -Payload ([ordered]@{
            head = $entry.epoch.head
            mergeBase = $entry.epoch.mergeBase
            baseRef = $entry.epoch.baseRef
            baseTip = $entry.epoch.baseTip
            feedbackSha256 = $entry.epoch.feedbackSha256
            diffSha256 = $entry.epoch.diffSha256
            guidanceMode = "target-base"
            rule = "Prepare one ready-v2 bundle after hosted feedback is terminal and bind its feedback projection to this exact baseline."
        })
    }

    foreach ($arm in $script:ArmOrder) {
        $model = $script:ArmModels[$arm]
        if (-not $entry.arms.Contains($arm)) {
            $nonce = Get-SfroStableId "nonce" ([ordered]@{
                campaignId = $State.campaignId
                pr = $activePr
                head = $entry.epoch.head
                arm = $arm
            })
            $prompt = "Return exactly READY:${arm}:${model}:$($entry.epoch.head):$nonce and use zero tools. Then go idle."
            return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "create-arm" -Discriminator $arm -Payload ([ordered]@{
                arm = $arm
                model = $model
                coordinatorSessionId = $entry.coordinatorSessionId
                requiredParentSessionId = $entry.coordinatorSessionId
                deterministicName = "Syncfusion PR $activePr $arm"
                nonce = $nonce
                mode = "interactive"
                kickoffPrompt = $prompt
            })
        }

        $armState = $entry.arms[$arm]
        if ($armState.status -eq "created") {
            return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "verify-arm" -Discriminator $arm -Payload ([ordered]@{
                arm = $arm
                model = $model
                sessionId = $armState.sessionId
                expectedToken = "READY:${arm}:${model}:$($entry.epoch.head):$($armState.nonce)"
                requireIdle = $true
                requireZeroTools = $true
            })
        }

        if ($armState.status -eq "verified") {
            return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "assign-arm" -Discriminator $arm -Payload ([ordered]@{
                arm = $arm
                model = $model
                sessionId = $armState.sessionId
                head = $entry.epoch.head
                reviewerCommit = $entry.epoch.reviewerCommit
                skillBlob = $entry.epoch.skillBlob
                loadedBodySha256 = $entry.epoch.loadedBodySha256
                bundleSha256 = $entry.epoch.readyV2BundleSha256
                firstTool = [ordered]@{
                    name = "skill"
                    skill = "review-pull-request"
                }
                guideWorker = [ordered]@{
                    agentType = "general-purpose"
                    mode = "background"
                    model = $model
                    count = "one-per-routed-guide"
                }
            })
        }

        if ($armState.status -eq "assigned") {
            return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "observe-arm" -Discriminator $arm -Payload ([ordered]@{
                arm = $arm
                model = $model
                sessionId = $armState.sessionId
                loadedBodySha256 = $entry.epoch.loadedBodySha256
                timeoutMinutes = 60
                requireTerminal = $true
                requireIdle = $true
                preserveFailure = $true
            })
        }
    }

    Assert-Sfro (Test-SfroAllArmsTerminal $entry) "The arm panel is in an unsupported state."

    if ($null -eq $entry.assessment) {
        $originals = @($script:ArmOrder | ForEach-Object {
            $resultSha256 = if ($null -ne $entry.arms[$_].terminal) {
                $entry.arms[$_].terminal.resultSha256
            }
            else {
                $entry.arms[$_].failure.artifactSha256
            }
            [ordered]@{
                arm = $_
                model = $entry.arms[$_].model
                outcome = $entry.arms[$_].outcome
                resultSha256 = $resultSha256
            }
        })
        return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "run-assessment" -Payload ([ordered]@{
            head = $entry.epoch.head
            originals = $originals
            preserveOriginals = $true
        })
    }

    if (@($entry.assessment.survivors).Count -gt 0) {
        foreach ($model in $script:FalsifierModels) {
            if (-not $entry.falsifiers.Contains($model)) {
                return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "run-falsifier" -Discriminator $model -Payload ([ordered]@{
                    model = $model
                    head = $entry.epoch.head
                    feedbackSha256 = $entry.epoch.feedbackSha256
                    diffSha256 = $entry.epoch.diffSha256
                    findings = $entry.assessment.survivors
                    checks = @(
                        "correctness",
                        "materiality",
                        "changed-line",
                        "current-head",
                        "existing-feedback",
                        "hosted-review-dedupe"
                    )
                })
            }
        }

        if ($null -eq $entry.finalFindings) {
            return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "finalize-findings" -Payload ([ordered]@{
                head = $entry.epoch.head
                assessment = $entry.assessment
                falsifiers = $entry.falsifiers
                rule = "Evidence decides; do not use majority vote."
            })
        }
    }

    [object[]]$finalSurvivors = @($entry.assessment.survivors)
    if ($null -ne $entry.finalFindings) {
        $finalSurvivors = @($entry.finalFindings.survivors)
    }

    if ($null -eq $entry.publication -and $finalSurvivors.Count -gt 0) {
        $findingsSha256 = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $finalSurvivors)
        return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "publish-review" -Payload ([ordered]@{
            head = $entry.epoch.head
            event = "COMMENT"
            atomic = $true
            findings = $finalSurvivors
            findingsSha256 = $findingsSha256
            marker = "<!-- sf-review:$($State.campaignId):${activePr}:$($entry.epoch.head):$findingsSha256 -->"
            diffSha256 = $entry.epoch.diffSha256
            feedbackSha256 = $entry.epoch.feedbackSha256
            baseRef = $entry.epoch.baseRef
            baseTip = $entry.epoch.baseTip
            feedbackExclusions = [ordered]@{
                issueCommentIds = @([int64]$entry.hostedReview.commentId)
                reviewIds = @()
                reviewCommentIds = @()
            }
            requireCompleteFeedback = $true
            requireChangedRightLines = $true
            requireAgentAttribution = $true
            requireReadback = $true
        })
    }

    if ($null -eq $entry.publication -and (Test-SfroCleanPanel $entry)) {
        return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "publish-clean" -Payload ([ordered]@{
            head = $entry.epoch.head
            label = "agent-reviewed"
            visibleComment = "Copilot's local review panel found no issues to raise at this commit."
            marker = "<!-- agent-reviewed-local-panel:$($entry.epoch.head) -->"
            baseRef = $entry.epoch.baseRef
            baseTip = $entry.epoch.baseTip
            diffSha256 = $entry.epoch.diffSha256
            feedbackSha256 = $entry.epoch.feedbackSha256
            feedbackExclusions = [ordered]@{
                issueCommentIds = @([int64]$entry.hostedReview.commentId)
                reviewIds = @()
                reviewCommentIds = @()
            }
            requireLabelAbsent = $true
            requireMarkerAbsent = $true
        })
    }

    if ($null -eq $entry.cleanup) {
        $sessionIds = @($script:ArmOrder | ForEach-Object { $entry.arms[$_].sessionId } | Where-Object { $_ })
        return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "cleanup-children" -Payload ([ordered]@{
            coordinatorSessionId = $entry.coordinatorSessionId
            requiredTerminalSessionIds = $sessionIds
            preserveEvidence = $true
        })
    }

    if ($null -eq $entry.handoff) {
        return New-SfroAction -State $State -Pr $activePr -Actor "pr-coordinator" -Kind "coordinator-handoff" -Payload ([ordered]@{
            coordinatorSessionId = $entry.coordinatorSessionId
            head = $entry.epoch.head
            childCleanupHash = $entry.cleanup.controllerReceiptSha256
            requireTerminal = $true
        })
    }

    if (-not $entry.coordinatorArchived) {
        return New-SfroAction -State $State -Pr $activePr -Actor "campaign-root" -Kind "archive-coordinator" -Payload ([ordered]@{
            coordinatorSessionId = $entry.coordinatorSessionId
            handoffSha256 = $entry.handoff.controllerReceiptSha256
            requireNoActiveChildren = $true
        })
    }

    throw "The active PR is terminal but was not released."
}

function Reserve-SfroAction {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LedgerPath,
        [Parameter(Mandatory)][Collections.IDictionary]$Action
    )

    $state = Get-SfroState -LedgerPath $LedgerPath
    Assert-Sfro ($null -eq (Get-SfroPendingIntent $state)) "An unresolved intent already exists."
    $current = Get-SfroNextAction $state
    Assert-Sfro ($null -ne $current -and $current.actionId -ceq $Action.actionId) "The action is stale."
    $attempt = @($state.intents.Values | Where-Object { $_.action.actionId -ceq $Action.actionId }).Count + 1
    $intentId = "$($Action.actionId)-attempt-$attempt"
    Assert-Sfro (-not $state.intents.Contains($intentId)) "Intent '$intentId' already exists."
    $intent = [ordered]@{
        intentId = $intentId
        action = Copy-SfroValue $Action
        reservedAtRevision = $state.revision
        reservedAtUtc = [datetimeoffset]::UtcNow.ToString("O")
        deadlineUtc = [datetimeoffset]::UtcNow.AddHours(2).ToString("O")
    }
    Add-SfroLedgerEvent -LedgerPath $LedgerPath -Type "intent.reserved" -Payload ([ordered]@{
        intent = $intent
    }) -ExpectedRevision $state.revision | Out-Null

    $result = Copy-SfroValue $Action
    $result.intentId = $intentId
    $result.intentReservedAtUtc = $intent.reservedAtUtc
    return $result
}

function Assert-SfroReceiptBase {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Receipt,
        [Parameter(Mandatory)][Collections.IDictionary]$Intent
    )

    Assert-Sfro ($Receipt.schemaVersion -eq $script:SchemaVersion) "Unsupported receipt schema."
    Assert-Sfro ($Receipt.intentId -ceq $Intent.intentId) "Receipt intentId mismatch."
    Assert-Sfro ($Receipt.actionId -ceq $Intent.action.actionId) "Receipt actionId mismatch."
    Assert-Sfro ($Receipt.kind -ceq $Intent.action.kind) "Receipt kind mismatch."
    Assert-Sfro ($Receipt.status -in $script:ReceiptStatuses) "Receipt status is unsupported."
    Assert-Sfro ($Receipt.actorSessionId -is [string] -and $Receipt.actorSessionId.Length -gt 0) "Receipt actorSessionId is required."
    Assert-Sfro (Test-SfroRfc3339 $Receipt.observedAtUtc) "Receipt observedAtUtc must be RFC 3339."
    Assert-Sfro ($Receipt.evidence -is [Collections.IDictionary]) "Receipt evidence must be an object."
}

function Assert-SfroIdentitySnapshot {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Identity,
        [Parameter(Mandatory)][Collections.IDictionary]$Expected,
        [Parameter(Mandatory)][string]$Label
    )

    Assert-Sfro ($Identity.state -ceq "open" -and -not [bool]$Identity.draft -and -not [bool]$Identity.merged) "$Label PR state is invalid."
    Assert-Sfro ($Identity.head -ceq $Expected.head) "$Label head mismatch."
    Assert-Sfro ($Identity.baseRef -ceq $Expected.baseRef) "$Label base ref mismatch."
    Assert-Sfro ($Identity.baseTip -ceq $Expected.baseTip) "$Label base tip mismatch."
    Assert-Sfro ([int]$Identity.changedFileCount -ge 0) "$Label changed-file count is invalid."
    Assert-Sfro ($Identity.diffSha256 -ceq $Expected.diffSha256) "$Label diff mismatch."
    Assert-Sfro (Test-SfroSha256 $Identity.rawFeedbackSha256) "$Label raw feedback hash is invalid."
    Assert-Sfro ($Identity.effectiveFeedbackSha256 -ceq $Expected.feedbackSha256) "$Label effective feedback mismatch."
    Assert-Sfro ($Identity.feedbackExclusions -is [Collections.IDictionary]) "$Label exclusions are missing."
    foreach ($field in @("files", "issueComments", "reviews", "reviewComments")) {
        Assert-Sfro ($Identity[$field] -is [array]) "$Label $field collection is missing."
    }
    Assert-Sfro ([int]$Identity.changedFileCount -eq @($Identity.files).Count) "$Label changed-file count mismatch."
    Assert-Sfro ([int]$Identity.changedFileCount -eq @($Identity.files).Count) "$Label changed-file count mismatch."
    $collections = [ordered]@{
        files = $Identity.files
        issueComments = $Identity.issueComments
        reviews = $Identity.reviews
        reviewComments = $Identity.reviewComments
    }
    Assert-Sfro ($Identity.collectionsSha256 -ceq (Get-SfroSha256 (ConvertTo-SfroCanonicalJson $collections))) "$Label collection hash mismatch."
}

function Assert-SfroIdentityStructuralSnapshot {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Identity,
        [Parameter(Mandatory)][Collections.IDictionary]$Expected,
        [Parameter(Mandatory)][string]$Label
    )

    Assert-Sfro ($Identity.state -ceq "open" -and -not [bool]$Identity.draft -and -not [bool]$Identity.merged) "$Label PR state is invalid."
    Assert-Sfro ($Identity.head -ceq $Expected.head) "$Label head mismatch."
    Assert-Sfro ($Identity.baseRef -ceq $Expected.baseRef) "$Label base ref mismatch."
    Assert-Sfro ($Identity.baseTip -ceq $Expected.baseTip) "$Label base tip mismatch."
    Assert-Sfro ([int]$Identity.changedFileCount -ge 0) "$Label changed-file count is invalid."
    Assert-Sfro ($Identity.diffSha256 -ceq $Expected.diffSha256) "$Label diff mismatch."
    Assert-Sfro ((Test-SfroSha256 $Identity.rawFeedbackSha256) -and (Test-SfroSha256 $Identity.effectiveFeedbackSha256)) "$Label feedback hashes are invalid."
    foreach ($field in @("files", "issueComments", "reviews", "reviewComments")) {
        Assert-Sfro ($Identity[$field] -is [array]) "$Label $field collection is missing."
    }
    $collections = [ordered]@{
        files = $Identity.files
        issueComments = $Identity.issueComments
        reviews = $Identity.reviews
        reviewComments = $Identity.reviewComments
    }
    Assert-Sfro ($Identity.collectionsSha256 -ceq (Get-SfroSha256 (ConvertTo-SfroCanonicalJson $collections))) "$Label collection hash mismatch."
}

function Assert-SfroAcceptedReceipt {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$State,
        [Parameter(Mandatory)][Collections.IDictionary]$Action,
        [Parameter(Mandatory)][Collections.IDictionary]$Receipt
    )

    $evidence = $Receipt.evidence
    $entry = $State.entries[[string][int]$Action.pr]

    switch ($Action.kind) {
        "accept-import" {
            Assert-Sfro ($evidence.queueHash -ceq $Action.payload.queueHash) "Import queue hash mismatch."
            Assert-Sfro ($evidence.configurationHash -ceq $Action.payload.configurationHash) "Import configuration hash mismatch."
            Assert-Sfro ((ConvertTo-SfroCanonicalJson $evidence.sourceHashes) -ceq (ConvertTo-SfroCanonicalJson $Action.payload.sourceHashes)) "Import source hashes mismatch."
            Assert-Sfro ([bool]$evidence.accepted) "Import acceptance must be explicit."
        }
        "reconcile-imported-active" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $State.rootSessionId) "Imported active reconciliation belongs to the campaign root."
            Assert-Sfro ($evidence.disposition -in @("terminal", "adopt")) "Imported active disposition must be terminal or adopt."
            Assert-Sfro (Test-SfroSha256 $evidence.artifactSha256) "Imported active reconciliation requires an artifact hash."
            if ($evidence.disposition -eq "adopt") {
                Assert-Sfro ($evidence.coordinatorSessionId -is [string] -and $evidence.coordinatorSessionId.Length -gt 0) "Adoption requires the existing coordinator session id."
                Assert-Sfro ([bool]$evidence.parentVerified) "Adopted coordinator parentage must be verified."
                Assert-Sfro ([bool]$evidence.noArmsCreated) "Adoption is allowed only when no original arm was created."
                Assert-Sfro ([bool]$evidence.consultationsAccepted) "Adoption requires accepted pre-coordinator consultations."
                if ($State.importMetadata.activeCase.Contains("coordinator")) {
                    Assert-Sfro ($evidence.coordinatorSessionId -ceq $State.importMetadata.activeCase.coordinator) "Adopted coordinator does not match the imported coordinator."
                }
            }
        }
        "launch-consultation" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $State.rootSessionId) "Consultations must be launched by the campaign root."
            Assert-Sfro ($evidence.model -ceq $Action.payload.model) "Consultation model mismatch."
            Assert-Sfro ([bool]$evidence.readOnly) "Consultation must be read-only."
            Assert-Sfro ([bool]$evidence.terminal) "Consultation must be terminal."
            Assert-Sfro ($evidence.verdict -ceq "ACCEPT") "Both launch consultations must accept the corrected plan."
            Assert-Sfro (Test-SfroSha256 $evidence.resultSha256) "Consultation result hash is required."
        }
        "create-coordinator" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $State.rootSessionId) "Only the campaign root may create a coordinator."
            Assert-Sfro ($evidence.sessionId -is [string] -and $evidence.sessionId.Length -gt 0) "Coordinator sessionId is required."
            Assert-Sfro ($evidence.parentSessionId -ceq $State.rootSessionId) "Coordinator parent mismatch."
            Assert-Sfro ($evidence.sessionId -cne $State.rootSessionId) "Coordinator cannot reuse the campaign root session id."
            Assert-Sfro (-not [bool]$evidence.detached -and [bool]$evidence.coordinateWithCreator) "Coordinator must remain coordinated and nested."
            Assert-Sfro ($evidence.nonce -ceq $Action.payload.nonce) "Coordinator nonce mismatch."
            Assert-Sfro ($evidence.deterministicName -ceq $Action.payload.deterministicName) "Coordinator name mismatch."
            Assert-Sfro (Test-SfroSha256 $evidence.observationArtifactSha256) "Coordinator creation requires an observation artifact hash."
        }
        "freeze-epoch" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may freeze the epoch."
            foreach ($field in @("reviewerCommit", "skillBlob", "head", "mergeBase", "baseTip")) {
                Assert-Sfro (Test-SfroSha $evidence[$field]) "freeze-epoch requires a valid $field."
            }
            Assert-Sfro ($evidence.baseRef -is [string] -and $evidence.baseRef.Length -gt 0) "freeze-epoch requires baseRef."
            foreach ($field in @("normalizedSkillFileSha256", "loadedBodySha256", "feedbackSha256", "diffSha256")) {
                Assert-Sfro (Test-SfroSha256 $evidence[$field]) "freeze-epoch requires a valid $field."
            }
            Assert-Sfro ($evidence.skillPath -is [string]) "freeze-epoch requires skillPath."
            Assert-Sfro ($evidence.skillIdentityTransform -ceq $script:NativeSkillTransform) "The native skill identity transform is unsupported."
            $skillIdentity = Get-SfroNativeSkillIdentity -Path $evidence.skillPath
            $skillBlob = Get-SfroGitBlobIdentity -SkillPath $evidence.skillPath -ReviewerCommit $evidence.reviewerCommit
            Assert-Sfro ($skillBlob -ceq $evidence.skillBlob) "The reviewer commit/path Git blob does not match the frozen skill blob."
            Assert-Sfro ($skillIdentity.normalizedGitBlob -ceq $skillBlob) "The normalized working-tree skill bytes do not match the frozen Git blob."
            Assert-Sfro ($skillIdentity.normalizedSkillFileSha256 -ceq $evidence.normalizedSkillFileSha256) "The normalized skill file identity does not match."
            Assert-Sfro ($skillIdentity.loadedBodySha256 -ceq $evidence.loadedBodySha256) "The native delivered-body identity does not match."
            Assert-Sfro ($evidence.guidanceMode -ceq "target-base") "The guidance mode must be target-base."
            Assert-Sfro ([bool]$evidence.cleanWorktree) "The frozen review worktree must be clean."
            Assert-Sfro ([bool]$evidence.crlfNormalizedIdentity) "The loaded skill identity must match after CRLF-to-LF normalization only."
            Assert-Sfro (-not [bool]$evidence.hasBom -and -not [bool]$evidence.hasLoneCr) "BOMs and lone CR bytes are forbidden."
            Assert-Sfro (Test-SfroSha256 $evidence.verificationArtifactSha256) "Frozen identity verification requires an artifact hash."
        }
        "ensure-review-trigger" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may ensure /review."
            Assert-Sfro ($evidence.head -ceq $entry.epoch.head) "Hosted review head mismatch."
            Assert-Sfro ($evidence.command -ceq "/review") "The hosted trigger must be literal /review."
            Assert-Sfro ($evidence.decision -in @("reused", "posted")) "Hosted review decision must be reused or posted."
            Assert-Sfro ([bool]$evidence.readbackVerified) "Hosted review trigger readback is required."
            Assert-Sfro ([int]$evidence.triggerCountForHead -eq 1) "There must be exactly one trigger for the exact head."
            Assert-Sfro ($evidence.baseRef -ceq $Action.payload.baseRef -and $evidence.baseTip -ceq $Action.payload.baseTip) "Hosted trigger base identity mismatch."
            Assert-Sfro ($evidence.diffSha256 -ceq $Action.payload.diffSha256) "Hosted trigger diff identity mismatch."
            Assert-Sfro ($evidence.rawFeedbackSha256 -ne $evidence.effectiveFeedbackSha256) "Hosted trigger must record its exact feedback delta."
            Assert-Sfro (@($evidence.excludedIssueCommentIds).Count -eq 1 -and [int64]$evidence.excludedIssueCommentIds[0] -eq [int64]$evidence.commentId) "Hosted trigger must exclude exactly its own comment ID."
            Assert-Sfro ([int64]$evidence.commentId -gt 0) "Hosted trigger comment ID is invalid."
            Assert-SfroIdentityStructuralSnapshot -Identity $evidence.preIdentity -Expected $Action.payload -Label "Hosted trigger pre"
            Assert-SfroIdentityStructuralSnapshot -Identity $evidence.postIdentity -Expected $Action.payload -Label "Hosted trigger post"
        }
        "observe-hosted-review" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may observe hosted review."
            Assert-Sfro ([bool]$evidence.terminal) "Hosted review observation must be terminal."
            Assert-Sfro ($evidence.head -ceq $Action.payload.head -and $evidence.baseRef -ceq $Action.payload.baseRef -and $evidence.baseTip -ceq $Action.payload.baseTip) "Hosted review identity mismatch."
            Assert-Sfro ($evidence.diffSha256 -ceq $Action.payload.diffSha256) "Hosted review diff mismatch."
            Assert-Sfro (Test-SfroSha256 $evidence.rawFeedbackSha256) "Hosted review raw feedback hash is invalid."
            Assert-Sfro (Test-SfroSha256 $evidence.effectiveFeedbackSha256) "Hosted review feedback baseline is invalid."
            Assert-Sfro ($evidence.preTriggerFeedbackSha256 -ceq $Action.payload.feedbackSha256) "Hosted review delta does not preserve the pre-trigger feedback."
            Assert-Sfro ((ConvertTo-SfroCanonicalJson $evidence.feedbackExclusions) -ceq (ConvertTo-SfroCanonicalJson $Action.payload.feedbackExclusions)) "Hosted review exclusions mismatch."
            Assert-Sfro ($evidence.hostedIssueCommentIds -is [array] -and $evidence.hostedReviewIds -is [array] -and $evidence.hostedReviewCommentIds -is [array]) "Hosted review output IDs must be explicit arrays."
            Assert-Sfro ([int64]$evidence.hostedRunId -gt 0) "Hosted review run ID is required."
            Assert-Sfro ($evidence.hostedWorkflowPath -ceq ".github/workflows/pull-request-review.lock.yml" -and $evidence.hostedRunEvent -ceq "issue_comment") "Hosted review run identity is invalid."
            Assert-Sfro (Test-SfroSha256 $evidence.hostedCorrelationArtifactSha256) "Hosted run correlation artifact hash is required."
            $hostedOutputCount = @($evidence.hostedIssueCommentIds).Count + @($evidence.hostedReviewIds).Count + @($evidence.hostedReviewCommentIds).Count
            Assert-Sfro ($hostedOutputCount -gt 0 -or [bool]$evidence.hostedNoFeedback) "Empty hosted output requires a no-feedback result."
            Assert-Sfro (Test-SfroSha256 $evidence.resultSha256) "Hosted review observation result hash is required."
            $hostedExpected = Copy-SfroValue $Action.payload
            $hostedExpected.feedbackSha256 = $evidence.effectiveFeedbackSha256
            Assert-SfroIdentitySnapshot -Identity $evidence.preIdentity -Expected $hostedExpected -Label "Hosted review pre"
            Assert-SfroIdentitySnapshot -Identity $evidence.postIdentity -Expected $hostedExpected -Label "Hosted review post"
        }
        "prepare-review-bundle" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may prepare the review bundle."
            Assert-Sfro ($evidence.head -ceq $Action.payload.head -and $evidence.baseTip -ceq $Action.payload.baseTip) "Review bundle identity mismatch."
            Assert-Sfro ($evidence.bundleFeedbackSha256 -ceq $Action.payload.feedbackSha256) "Review bundle feedback does not match the post-trigger baseline."
            Assert-Sfro ($evidence.diffSha256 -ceq $Action.payload.diffSha256) "Review bundle diff mismatch."
            Assert-Sfro (Test-SfroSha256 $evidence.readyV2BundleSha256) "Ready-v2 bundle hash is required."
            Assert-Sfro ([bool]$evidence.ready) "The review bundle is not ready."
        }
        "create-arm" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may create an arm."
            Assert-Sfro ($evidence.callerSessionId -ceq $entry.coordinatorSessionId) "Arm caller mismatch."
            Assert-Sfro ($evidence.parentSessionId -ceq $entry.coordinatorSessionId) "Arm parent mismatch."
            Assert-Sfro ($evidence.model -ceq $Action.payload.model) "Arm requested model mismatch."
            Assert-Sfro ($evidence.sessionId -is [string] -and $evidence.sessionId.Length -gt 0) "Arm sessionId is required."
            Assert-Sfro ($evidence.sessionId -cne $State.rootSessionId -and $evidence.sessionId -cne $entry.coordinatorSessionId) "Arm session id must be unique."
            foreach ($existingArm in $entry.arms.Values) {
                Assert-Sfro ($evidence.sessionId -cne $existingArm.sessionId) "Arm session id was already used."
            }
            Assert-Sfro (-not [bool]$evidence.detached -and [bool]$evidence.coordinateWithCreator) "Arm must remain coordinated and nested."
            Assert-Sfro ($evidence.nonce -ceq $Action.payload.nonce) "Arm nonce mismatch."
            Assert-Sfro (Test-SfroSha256 $evidence.observationArtifactSha256) "Arm creation requires an observation artifact hash."
        }
        "verify-arm" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may verify an arm."
            Assert-Sfro ($evidence.sessionId -ceq $Action.payload.sessionId) "Arm session mismatch."
            Assert-Sfro ($evidence.actualModel -ceq $Action.payload.model) "Arm actual model mismatch."
            Assert-Sfro ([bool]$evidence.zeroToolInitialization) "Arm initialization used a tool."
            Assert-Sfro ([bool]$evidence.idle) "Arm must be idle after initialization."
            Assert-Sfro ([bool]$evidence.eventEvidenceVerified) "Model and zero-tool claims require session-event evidence."
            Assert-Sfro ($evidence.readyToken -ceq $Action.payload.expectedToken) "Arm ready token mismatch."
            Assert-Sfro (Test-SfroSha256 $evidence.eventArtifactSha256) "Arm verification requires a session-event artifact hash."
            Assert-Sfro ($evidence.eventIds -is [array] -and @($evidence.eventIds).Count -gt 0) "Arm verification requires event ids."
        }
        "assign-arm" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may assign an arm."
            Assert-Sfro ($evidence.sessionId -ceq $Action.payload.sessionId) "Arm assignment session mismatch."
            Assert-Sfro ($evidence.assignmentMessageId -is [string] -and $evidence.assignmentMessageId.Length -gt 0) "Arm assignment message id is required."
            Assert-Sfro (Test-SfroSha256 $evidence.assignmentSha256) "Arm assignment hash is required."
        }
        "observe-arm" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may observe an arm."
            Assert-Sfro ($evidence.sessionId -ceq $Action.payload.sessionId) "Arm terminal session mismatch."
            Assert-Sfro ($evidence.outcome -in $script:TerminalArmOutcomes) "Arm outcome is unsupported."
            Assert-Sfro ([bool]$evidence.terminal -and [bool]$evidence.idle) "Arm and guide workers must be terminal and idle."
            Assert-Sfro (Test-SfroSha256 $evidence.resultSha256) "Arm result hash is required."
            Assert-Sfro ($evidence.firstToolName -ceq "skill" -and $evidence.firstToolSkill -ceq "review-pull-request") "The first review-turn tool must be review-pull-request."
            Assert-Sfro ($evidence.loadedBodySha256 -ceq $Action.payload.loadedBodySha256) "The runtime-delivered skill body identity mismatch."
            Assert-Sfro (Test-SfroSha256 $evidence.eventArtifactSha256) "Arm terminal observation requires a session-event artifact hash."
            Assert-Sfro ($evidence.guideWorkers -is [array]) "Guide workers must be an array."
            foreach ($worker in @($evidence.guideWorkers)) {
                Assert-Sfro ($worker.agentType -ceq "general-purpose" -and $worker.mode -ceq "background") "Guide worker shape mismatch."
                Assert-Sfro ($worker.model -ceq $Action.payload.model) "Guide worker model mismatch."
                Assert-Sfro ([bool]$worker.terminal -and [bool]$worker.idle) "Guide workers must be terminal and idle."
            }
        }
        "run-assessment" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may run assessment."
            Assert-Sfro ($evidence.head -ceq $entry.epoch.head) "Assessment head mismatch."
            Assert-Sfro ([bool]$evidence.originalsUnchanged) "Assessment must preserve all originals unchanged."
            $survivorType = if ($null -eq $evidence.survivors) { "null" } else { $evidence.survivors.GetType().FullName }
            Assert-Sfro ($evidence.survivors -is [array]) "Assessment survivors must be an array, not $survivorType."
            Assert-Sfro (Test-SfroSha256 $evidence.resultSha256) "Assessment result hash is required."
        }
        "run-falsifier" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may run falsifiers."
            Assert-Sfro ($evidence.model -ceq $Action.payload.model) "Falsifier model mismatch."
            Assert-Sfro ([bool]$evidence.readOnly -and [bool]$evidence.terminal) "Falsifier must be read-only and terminal."
            Assert-Sfro ($evidence.head -ceq $entry.epoch.head) "Falsifier head mismatch."
            Assert-Sfro ($evidence.feedbackSha256 -ceq $entry.epoch.feedbackSha256) "Falsifier feedback identity mismatch."
            Assert-Sfro ($evidence.diffSha256 -ceq $entry.epoch.diffSha256) "Falsifier diff identity mismatch."
            Assert-Sfro ($evidence.verdicts -is [array]) "Falsifier verdicts must be an array."
            Assert-Sfro (@($evidence.verdicts).Count -eq @($Action.payload.findings).Count) "Falsifier must return one verdict per finding."
            Assert-Sfro (Test-SfroSha256 $evidence.resultSha256) "Falsifier result hash is required."
        }
        "finalize-findings" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may finalize findings."
            Assert-Sfro ($evidence.head -ceq $entry.epoch.head) "Final finding head mismatch."
            Assert-Sfro ($evidence.survivors -is [array]) "Final survivors must be an array."
            Assert-Sfro ([bool]$evidence.evidenceBasedNotMajority) "Finding finalization must be evidence-based."
            Assert-Sfro (Test-SfroSha256 $evidence.resultSha256) "Final finding result hash is required."
            $assessmentIds = @($entry.assessment.survivors | ForEach-Object { $_.id })
            foreach ($survivor in @($evidence.survivors)) {
                Assert-Sfro ($survivor.id -in $assessmentIds) "Final findings must be a subset of assessment survivors."
                Assert-Sfro ($survivor.path -is [string] -and [int]$survivor.line -gt 0 -and $survivor.side -ceq "RIGHT") "Final findings require changed-line anchors."
                Assert-Sfro ($survivor.fingerprint -is [string] -and $survivor.fingerprint.Length -gt 0) "Final findings require deterministic fingerprints."
            }
        }
        "publish-review" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may publish a review."
            Assert-Sfro ($evidence.liveHead -ceq $entry.epoch.head) "Publication live head mismatch."
            Assert-Sfro ($evidence.event -ceq "COMMENT") "Only COMMENT reviews are allowed."
            Assert-Sfro ([bool]$evidence.atomic -and [bool]$evidence.readbackVerified) "Review publication must be atomic and verified."
            Assert-Sfro ([bool]$evidence.feedbackComplete -and [bool]$evidence.deduplicated) "Feedback collection and dedupe must be complete."
            Assert-Sfro ([bool]$evidence.changedLinesValid) "Every inline anchor must be a changed RIGHT-side line."
            Assert-Sfro ([bool]$evidence.agentAttributed) "Published findings must be agent-attributed."
            Assert-Sfro ([int64]$evidence.reviewId -gt 0) "Published reviewId is required."
            Assert-Sfro ($evidence.publishedFindingsSha256 -ceq (Get-SfroSha256 (ConvertTo-SfroCanonicalJson $Action.payload.findings))) "Published findings do not match the finalized set."
            Assert-Sfro ($evidence.diffSha256 -ceq $Action.payload.diffSha256 -and $evidence.feedbackSha256 -ceq $Action.payload.feedbackSha256) "Publication evidence is not bound to the frozen diff and feedback."
            Assert-Sfro ($evidence.baseRef -ceq $Action.payload.baseRef -and $evidence.baseTip -ceq $Action.payload.baseTip) "Publication base identity mismatch."
            Assert-Sfro ((ConvertTo-SfroCanonicalJson $evidence.feedbackExclusions) -ceq (ConvertTo-SfroCanonicalJson $Action.payload.feedbackExclusions)) "Publication feedback exclusions mismatch."
            Assert-SfroIdentitySnapshot -Identity $evidence.preIdentity -Expected $Action.payload -Label "Review publication pre"
            Assert-SfroIdentitySnapshot -Identity $evidence.postIdentity -Expected $Action.payload -Label "Review publication post"
            Assert-Sfro ($evidence.recoveryExclusions -is [Collections.IDictionary]) "Review publication recovery exclusions are missing."
        }
        "publish-clean" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may publish clean-panel evidence."
            Assert-Sfro (Test-SfroCleanPanel $entry) "The panel is not eligible for agent-reviewed."
            Assert-Sfro ($evidence.liveHead -ceq $entry.epoch.head) "Clean publication live head mismatch."
            Assert-Sfro ([bool]$evidence.panelComplete -and [bool]$evidence.allOriginalsNoFindings) "The clean panel must be complete and all NO_FINDINGS."
            Assert-Sfro ([int]$evidence.assessmentSurvivorCount -eq 0) "The clean panel assessment must have zero survivors."
            Assert-Sfro ([bool]$evidence.readbackVerified) "Clean publication readback is required."
            Assert-Sfro ([int]$evidence.labelCountAfter -eq 1 -and [int]$evidence.markerCountAfter -eq 1) "Clean publication postconditions must exist exactly once."
            Assert-Sfro ($evidence.baseRef -ceq $Action.payload.baseRef -and $evidence.baseTip -ceq $Action.payload.baseTip) "Clean publication base identity mismatch."
            Assert-Sfro ($evidence.diffSha256 -ceq $Action.payload.diffSha256 -and $evidence.feedbackSha256 -ceq $Action.payload.feedbackSha256) "Clean publication identities mismatch."
            Assert-Sfro ((ConvertTo-SfroCanonicalJson $evidence.feedbackExclusions) -ceq (ConvertTo-SfroCanonicalJson $Action.payload.feedbackExclusions)) "Clean publication feedback exclusions mismatch."
            Assert-SfroIdentitySnapshot -Identity $evidence.preIdentity -Expected $Action.payload -Label "Clean publication pre"
            Assert-SfroIdentitySnapshot -Identity $evidence.postIdentity -Expected $Action.payload -Label "Clean publication post"
            Assert-Sfro ($evidence.recoveryExclusions -is [Collections.IDictionary]) "Clean publication recovery exclusions are missing."
        }
        "cleanup-children" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "Only the coordinator may clean up children."
            $required = @($Action.payload.requiredTerminalSessionIds | Sort-Object)
            $archived = @($evidence.archivedSessionIds | Sort-Object)
            Assert-Sfro ((ConvertTo-SfroCanonicalJson $required) -ceq (ConvertTo-SfroCanonicalJson $archived)) "Cleanup must archive every recorded child exactly once."
            Assert-Sfro ([bool]$evidence.allTerminal) "Only terminal children may be archived."
            Assert-Sfro (Test-SfroSha256 $evidence.receiptSha256) "Cleanup receipt hash is required."
        }
        "coordinator-handoff" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $entry.coordinatorSessionId) "The handoff must come from the coordinator."
            Assert-Sfro ([bool]$evidence.terminal) "Coordinator handoff must be terminal."
            $expectedHead = if ($null -ne $entry.epoch) { $entry.epoch.head } else { $null }
            Assert-Sfro ($evidence.head -ceq $expectedHead) "Coordinator handoff head mismatch."
            Assert-Sfro ($Action.payload.childCleanupHash -ceq $entry.cleanup.controllerReceiptSha256) "Coordinator handoff is not bound to the cleanup receipt."
            Assert-Sfro (Test-SfroSha256 $evidence.receiptSha256) "Coordinator handoff hash is required."
        }
        "archive-coordinator" {
            Assert-Sfro ($Receipt.actorSessionId -ceq $State.rootSessionId) "Only the campaign root may archive the coordinator."
            Assert-Sfro ($evidence.sessionId -ceq $entry.coordinatorSessionId) "Coordinator archive session mismatch."
            Assert-Sfro ([bool]$evidence.archived -and [bool]$evidence.noActiveChildren) "Coordinator archive must be verified."
            Assert-Sfro ($Action.payload.handoffSha256 -ceq $entry.handoff.controllerReceiptSha256) "Coordinator archive is not bound to the handoff receipt."
        }
        default {
            throw "Unsupported receipt kind '$($Action.kind)'."
        }
    }
}

function Add-SfroReceipt {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LedgerPath,
        [Parameter(Mandatory)][Collections.IDictionary]$Receipt
    )

    $state = Get-SfroState -LedgerPath $LedgerPath
    Assert-Sfro ($state.intents.Contains([string]$Receipt.intentId)) "Receipt references an unknown intent."
    $intent = $state.intents[[string]$Receipt.intentId]
    Assert-Sfro ($intent.status -in @("reserved", "unknown")) "Intent '$($Receipt.intentId)' is already terminal."
    Assert-SfroReceiptBase -Receipt $Receipt -Intent $intent
    $entry = $state.entries[[string][int]$intent.action.pr]
    $expectedActor = if ($intent.action.actor -eq "campaign-root") {
        $state.rootSessionId
    }
    else {
        $entry.coordinatorSessionId
    }
    Assert-Sfro ($Receipt.actorSessionId -ceq $expectedActor) "Receipt actor does not own the reserved intent."
    Assert-SfroProofArtifacts -Receipt $Receipt -Action $intent.action

    if ($Receipt.status -eq "accepted") {
        Assert-SfroAcceptedReceipt -State $state -Action $intent.action -Receipt $Receipt
    }
    elseif ($Receipt.status -eq "absent") {
        Assert-Sfro ($Receipt.evidence.code -ceq "effect-proven-absent") "Absent receipts require effect-proven-absent."
        Assert-Sfro (Test-SfroSha256 $Receipt.evidence.artifactSha256) "Absent receipts require a reconciliation artifact hash."
    }
    elseif ($Receipt.status -in @("failed", "timed-out")) {
        Assert-Sfro ($Receipt.evidence.code -is [string] -and $Receipt.evidence.code.Length -gt 0) "Failed receipts require an evidence code."
        Assert-Sfro (Test-SfroSha256 $Receipt.evidence.artifactSha256) "Failed receipts require a retained artifact hash."
        if ($intent.action.kind -eq "create-arm") {
            Assert-Sfro ($Receipt.evidence.Contains("sessionCreated")) "A failed arm creation must state whether a session was created."
            if ([bool]$Receipt.evidence.sessionCreated) {
                Assert-Sfro ($Receipt.evidence.Contains("sessionId") -and $Receipt.evidence.Contains("quiescent")) "A created failed arm requires session and quiescence evidence."
                Assert-Sfro ($Receipt.evidence.sessionId -is [string] -and $Receipt.evidence.sessionId.Length -gt 0) "A created failed arm requires its session id."
                Assert-Sfro ([bool]$Receipt.evidence.quiescent) "A created failed arm must be quiescent."
            }
        }
        elseif ($intent.action.kind -in @("verify-arm", "assign-arm", "observe-arm")) {
            Assert-Sfro ($Receipt.evidence.Contains("sessionId") -and $Receipt.evidence.Contains("quiescent")) "Failed arm receipts require session and quiescence evidence."
            Assert-Sfro ($Receipt.evidence.sessionId -ceq $intent.action.payload.sessionId) "Failed arm receipt session mismatch."
            Assert-Sfro ([bool]$Receipt.evidence.quiescent) "A failed or timed-out arm must be quiescent before the next arm starts."
        }
    }
    else {
        Assert-Sfro ($Receipt.evidence.reconcileBy -is [string] -and $Receipt.evidence.reconcileBy.Length -gt 0) "Unknown receipts require a reconciliation method."
    }

    $receiptCopy = Copy-SfroValue $Receipt
    $receiptCopy.receiptSha256 = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $Receipt)
    Add-SfroLedgerEvent -LedgerPath $LedgerPath -Type "intent.receipted" -Payload ([ordered]@{
        receipt = $receiptCopy
    }) -ExpectedRevision $state.revision | Out-Null
    return Get-SfroState -LedgerPath $LedgerPath
}

function Invoke-SfroNext {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$LedgerPath,
        [switch]$DryRun
    )

    $state = Get-SfroState -LedgerPath $LedgerPath
    $action = Get-SfroNextAction $state
    $isReconcile = $null -ne $action -and $action.Contains("reconcile") -and [bool]$action.reconcile
    if ($null -eq $action -or $isReconcile -or $DryRun) {
        return $action
    }

    return Reserve-SfroAction -LedgerPath $LedgerPath -Action $action
}

function Invoke-SfroSimulation {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ConfigPath)

    $configuration = ConvertFrom-SfroJson (Get-Content -LiteralPath $ConfigPath -Raw)
    Assert-SfroCampaignConfiguration -Configuration $configuration
    $queueHash = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $configuration.queue)
    return [ordered]@{
        schemaVersion = $script:SchemaVersion
        mode = "dry-run"
        githubMutations = 0
        appMutations = 0
        campaignId = $configuration.campaignId
        repository = $configuration.repository
        entryCount = @($configuration.queue).Count
        queueHash = $queueHash
        firstAction = [ordered]@{
            kind = "launch-consultation"
            actor = "campaign-root"
            model = $script:ConsultationModels[0]
            pr = [int]$configuration.queue[0].pr
        }
        architecture = "repository-state-machine + GitHub REST adapter + Copilot app adapter"
    }
}

function Invoke-SfroCanary {
    [CmdletBinding()]
    param()

    $testScript = Join-Path (Split-Path -Parent $PSScriptRoot) "tests/Test-SyncfusionReviewOrchestrator.ps1"
    $output = @(& pwsh -NoProfile -File $testScript)
    if ($LASTEXITCODE -ne 0) {
        throw "The fixture canary failed."
    }
    $summary = $output | Select-Object -Last 1
    Assert-Sfro ($summary -match "^Validated (?<count>\d+) Syncfusion review orchestrator assertions\.$") "The fixture canary did not return its validation summary."
    return [ordered]@{
        status = "PASS"
        mode = "fixture-only"
        assertions = [int]$Matches.count
        githubMutations = 0
        appMutations = 0
    }
}

Export-ModuleMember -Function @(
    "Add-SfroLedgerEvent",
    "Add-SfroReceipt",
    "ConvertFrom-SfroJson",
    "ConvertTo-SfroCanonicalJson",
    "Get-SfroFileSha256",
    "Get-SfroNativeSkillIdentity",
    "Get-SfroNextAction",
    "Get-SfroSha256",
    "Get-SfroState",
    "Invoke-SfroCanary",
    "Invoke-SfroNext",
    "Invoke-SfroSimulation",
    "New-SfroCampaign",
    "Read-SfroLedger",
    "Resolve-SfroReviewTriggerDecision",
    "Reserve-SfroAction"
)
