#!/usr/bin/env pwsh
#Requires -Version 7.0

$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Throws {
    param([scriptblock]$Action, [string]$Pattern, [string]$Message)

    try {
        & $Action
    }
    catch {
        if ($_.Exception.Message -match $Pattern) {
            return
        }

        throw "$Message Unexpected error: $($_.Exception.Message)"
    }

    throw $Message
}

$skillRoot = Split-Path -Parent $PSScriptRoot
$modulePath = Join-Path $skillRoot "scripts/SyncfusionReviewOrchestrator.psm1"
$importScript = Join-Path $skillRoot "scripts/Import-SyncfusionReviewCampaign.ps1"
$githubAdapter = Join-Path $skillRoot "scripts/Invoke-SyncfusionGitHubAdapter.ps1"
$fixturePath = Join-Path $PSScriptRoot "fixtures/campaign.json"
$legacyControlPath = Join-Path $PSScriptRoot "fixtures/legacy-control.json"
$triggerRequestPath = Join-Path $PSScriptRoot "fixtures/review-trigger-request.json"
$triggerReconcileRequestPath = Join-Path $PSScriptRoot "fixtures/review-trigger-reconcile-request.json"
$publishRequestPath = Join-Path $PSScriptRoot "fixtures/publish-review-request.json"
$cleanRequestPath = Join-Path $PSScriptRoot "fixtures/publish-clean-request.json"
$githubFixturePath = Join-Path $PSScriptRoot "fixtures/github-api.json"
$existingReviewFixturePath = Join-Path $PSScriptRoot "fixtures/github-api-existing-review.json"
$partialCleanFixturePath = Join-Path $PSScriptRoot "fixtures/github-api-clean-partial.json"
$triggerReconcileFixturePath = Join-Path $PSScriptRoot "fixtures/github-api-trigger-reconcile.json"
Import-Module -Scope Local -Force -DisableNameChecking $modulePath

$shaA = "a" * 40
$shaB = "b" * 40
$shaC = "c" * 40
$hashA = "a" * 64
$hashB = "b" * 64
$hashC = "c" * 64
$assertions = 0
$script:TempRoots = [Collections.Generic.List[string]]::new()
$script:ProofRoot = Join-Path ([IO.Path]::GetTempPath()) ("sfro-proofs-" + [guid]::NewGuid().ToString("N"))
[IO.Directory]::CreateDirectory($script:ProofRoot) | Out-Null
$script:TempRoots.Add($script:ProofRoot)

function New-TestLedger {
    param([switch]$ImportRequired)

    $root = Join-Path ([IO.Path]::GetTempPath()) ("sfro-" + [guid]::NewGuid().ToString("N"))
    [IO.Directory]::CreateDirectory($root) | Out-Null
    $script:TempRoots.Add($root)
    $ledger = Join-Path $root "ledger.jsonl"
    New-SfroCampaign `
        -LedgerPath $ledger `
        -ConfigPath $fixturePath `
        -ImportRequired:$ImportRequired | Out-Null
    return $ledger
}

function New-ValidEvidence {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Action,
        [Parameter(Mandatory)][Collections.IDictionary]$State,
        [hashtable]$Options = @{}
    )

    $entry = $State.entries[[string][int]$Action.pr]
    switch ($Action.kind) {
        "accept-import" {
            return [ordered]@{
                queueHash = $Action.payload.queueHash
                configurationHash = $Action.payload.configurationHash
                sourceHashes = $Action.payload.sourceHashes
                accepted = $true
            }
        }
        "reconcile-imported-active" {
            return [ordered]@{
                disposition = "terminal"
                artifactSha256 = $hashA
            }
        }
        "launch-consultation" {
            return [ordered]@{
                model = $Action.payload.model
                readOnly = $true
                terminal = $true
                verdict = "ACCEPT"
                resultSha256 = $hashA
            }
        }
        "create-coordinator" {
            return [ordered]@{
                sessionId = "coordinator-$($Action.pr)"
                parentSessionId = $State.rootSessionId
                nonce = $Action.payload.nonce
                deterministicName = $Action.payload.deterministicName
                detached = $false
                coordinateWithCreator = $true
                observationArtifactSha256 = $hashA
            }
        }
        "freeze-epoch" {
            return [ordered]@{
                reviewerCommit = $shaA
                skillBlob = $shaB
                loadedBodySha256 = $hashA
                head = $shaC
                mergeBase = $shaA
                baseTip = $shaB
                readyV2BundleSha256 = $hashB
                feedbackSha256 = $hashC
                diffSha256 = $hashA
                guidanceMode = "target-base"
                cleanWorktree = $true
                crlfNormalizedIdentity = $true
                hasBom = $false
                hasLoneCr = $false
                verificationArtifactSha256 = $hashA
            }
        }
        "ensure-review-trigger" {
            return [ordered]@{
                head = $entry.epoch.head
                command = "/review"
                decision = "posted"
                commentId = 101
                readbackVerified = $true
                triggerCountForHead = 1
            }
        }
        "create-arm" {
            return [ordered]@{
                callerSessionId = $entry.coordinatorSessionId
                parentSessionId = $entry.coordinatorSessionId
                sessionId = "arm-$($Action.payload.arm)-$($Action.pr)"
                model = $Action.payload.model
                nonce = $Action.payload.nonce
                detached = $false
                coordinateWithCreator = $true
                observationArtifactSha256 = $hashB
            }
        }
        "verify-arm" {
            return [ordered]@{
                sessionId = $Action.payload.sessionId
                actualModel = $Action.payload.model
                zeroToolInitialization = $true
                idle = $true
                eventEvidenceVerified = $true
                readyToken = $Action.payload.expectedToken
                eventArtifactSha256 = $hashC
                eventIds = @("event-model", "event-idle")
            }
        }
        "assign-arm" {
            return [ordered]@{
                sessionId = $Action.payload.sessionId
                assignmentMessageId = "message-$($Action.payload.arm)"
                assignmentSha256 = $hashA
            }
        }
        "observe-arm" {
            $outcome = if ($Options.ContainsKey("Outcome")) { $Options.Outcome } else { "NO_FINDINGS" }
            return [ordered]@{
                sessionId = $Action.payload.sessionId
                outcome = $outcome
                terminal = $true
                idle = $true
                resultSha256 = $hashB
                firstToolName = "skill"
                firstToolSkill = "review-pull-request"
                eventArtifactSha256 = $hashC
                guideWorkers = @(
                    [ordered]@{
                        agentType = "general-purpose"
                        mode = "background"
                        model = $Action.payload.model
                        terminal = $true
                        idle = $true
                    }
                )
            }
        }
        "run-assessment" {
            [object[]]$survivors = @()
            if ($Options.ContainsKey("Survivors")) {
                $survivors = @($Options.Survivors)
            }
            return [ordered]@{
                head = $entry.epoch.head
                originalsUnchanged = $true
                survivors = $survivors
                resultSha256 = $hashC
            }
        }
        "run-falsifier" {
            return [ordered]@{
                model = $Action.payload.model
                readOnly = $true
                terminal = $true
                verdicts = @($Action.payload.findings | ForEach-Object {
                    [ordered]@{ id = $_.id; verdict = "upheld" }
                })
                head = $entry.epoch.head
                feedbackSha256 = $entry.epoch.feedbackSha256
                diffSha256 = $entry.epoch.diffSha256
                resultSha256 = $hashA
            }
        }
        "finalize-findings" {
            [object[]]$survivors = @($entry.assessment.survivors)
            if ($Options.ContainsKey("Survivors")) {
                $survivors = @($Options.Survivors)
            }
            return [ordered]@{
                head = $entry.epoch.head
                survivors = $survivors
                evidenceBasedNotMajority = $true
                resultSha256 = $hashB
            }
        }
        "publish-review" {
            return [ordered]@{
                liveHead = $entry.epoch.head
                event = "COMMENT"
                atomic = $true
                readbackVerified = $true
                feedbackComplete = $true
                deduplicated = $true
                changedLinesValid = $true
                agentAttributed = $true
                reviewId = 202
                publishedFindingsSha256 = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $Action.payload.findings)
                diffSha256 = $Action.payload.diffSha256
                feedbackSha256 = $Action.payload.feedbackSha256
            }
        }
        "publish-clean" {
            return [ordered]@{
                liveHead = $entry.epoch.head
                panelComplete = $true
                allOriginalsNoFindings = $true
                assessmentSurvivorCount = 0
                labelWasAbsent = $true
                markerWasAbsent = $true
                labelCountAfter = 1
                markerCountAfter = 1
                readbackVerified = $true
            }
        }
        "cleanup-children" {
            return [ordered]@{
                archivedSessionIds = @($Action.payload.requiredTerminalSessionIds)
                allTerminal = $true
                receiptSha256 = $hashC
            }
        }
        "coordinator-handoff" {
            return [ordered]@{
                terminal = $true
                head = $entry.epoch.head
                receiptSha256 = $hashA
            }
        }
        "archive-coordinator" {
            return [ordered]@{
                sessionId = $entry.coordinatorSessionId
                archived = $true
                noActiveChildren = $true
            }
        }
        default {
            throw "No fixture evidence for '$($Action.kind)'."
        }
    }
}

function Complete-Action {
    param(
        [Parameter(Mandatory)][string]$Ledger,
        [Parameter(Mandatory)][Collections.IDictionary]$Action,
        [ValidateSet("accepted", "absent", "failed", "timed-out", "unknown")]
        [string]$Status = "accepted",
        [hashtable]$Options = @{},
        [Collections.IDictionary]$OverrideEvidence
    )

    $state = Get-SfroState $Ledger
    $actor = if ($Action.actor -eq "campaign-root") {
        $state.rootSessionId
    }
    else {
        $state.entries[[string][int]$Action.pr].coordinatorSessionId
    }
    $evidence = if ($null -ne $OverrideEvidence) {
        $OverrideEvidence
    }
    elseif ($Status -eq "accepted") {
        New-ValidEvidence -Action $Action -State $state -Options $Options
    }
    elseif ($Status -eq "absent") {
        [ordered]@{ code = "effect-proven-absent"; artifactSha256 = $hashA }
    }
    elseif ($Status -in @("failed", "timed-out")) {
        $failure = [ordered]@{ code = $Options.Code; artifactSha256 = $hashA }
        if ($Action.kind -eq "create-arm") {
            $failure.sessionCreated = $false
        }
        elseif ($Action.kind -in @("verify-arm", "assign-arm", "observe-arm")) {
            $armState = $state.entries[[string][int]$Action.pr].arms[$Action.payload.arm]
            $failure.sessionId = $armState.sessionId
            $failure.quiescent = $true
        }
        $failure
    }
    else {
        [ordered]@{ reconcileBy = "deterministic session name and nonce" }
    }
    $claimsSha256 = Get-SfroSha256 (ConvertTo-SfroCanonicalJson $evidence)
    $proofPath = Join-Path $script:ProofRoot ("proof-" + [guid]::NewGuid().ToString("N") + ".json")
    $proofSource = if ($Action.kind -in @("ensure-review-trigger", "publish-review", "publish-clean")) {
        "github-adapter"
    }
    elseif ($Action.kind -in @("accept-import", "reconcile-imported-active")) {
        "operator-import"
    }
    else {
        "app-session-events"
    }
    $proofEnvelope = [ordered]@{
        schemaVersion = 1
        intentId = $Action.intentId
        actionId = $Action.actionId
        kind = $Action.kind
        source = $proofSource
        objectIds = @($actor, $Action.pr)
        claimsSha256 = $claimsSha256
    }
    $resultArtifactPath = Join-Path $script:ProofRoot ("result-" + [guid]::NewGuid().ToString("N") + ".json")
    [IO.File]::WriteAllText(
        $resultArtifactPath,
        (([ordered]@{ schemaVersion = 1; result = "generic non-envelope artifact" } | ConvertTo-Json -Compress) + "`n"),
        [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText(
        $proofPath,
        (ConvertTo-SfroCanonicalJson $proofEnvelope) + "`n",
        [Text.UTF8Encoding]::new($false))
    $evidence.proofs = @(
        [ordered]@{
            path = $resultArtifactPath
            sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $resultArtifactPath).Hash.ToLowerInvariant()
        },
        [ordered]@{
            path = $proofPath
            sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $proofPath).Hash.ToLowerInvariant()
        }
    )

    $receipt = [ordered]@{
        schemaVersion = 1
        intentId = $Action.intentId
        actionId = $Action.actionId
        kind = $Action.kind
        status = $Status
        actorSessionId = $actor
        observedAtUtc = [datetimeoffset]::UtcNow.ToString("O")
        evidence = $evidence
    }
    Add-SfroReceipt -LedgerPath $Ledger -Receipt $receipt | Out-Null
}

function Complete-Until {
    param(
        [Parameter(Mandatory)][string]$Ledger,
        [Parameter(Mandatory)][string]$Kind,
        [hashtable]$ArmOutcomes = @{},
        [object[]]$AssessmentSurvivors = @()
    )

    for ($guard = 0; $guard -lt 100; $guard++) {
        $action = Invoke-SfroNext $Ledger
        if ($action.kind -eq $Kind) {
            return $action
        }

        $options = @{}
        if ($action.kind -eq "observe-arm" -and $ArmOutcomes.ContainsKey($action.payload.arm)) {
            $options.Outcome = $ArmOutcomes[$action.payload.arm]
        }
        if ($action.kind -eq "run-assessment") {
            $options.Survivors = $AssessmentSurvivors
        }
        Complete-Action -Ledger $Ledger -Action $action -Options $options
    }

    throw "Did not reach action '$Kind'."
}

# Import acceptance and exact 33-entry authority gate.
$ledger = New-TestLedger -ImportRequired
$action = Invoke-SfroNext $ledger
Assert-True ($action.kind -eq "accept-import") "Imported state must require explicit acceptance."
$assertions++
Complete-Action -Ledger $ledger -Action $action
Assert-True ((Invoke-SfroNext $ledger).kind -eq "launch-consultation") "Accepted import must begin launch consultations."
$assertions++
$ledger = New-TestLedger -ImportRequired
$failedImport = Invoke-SfroNext $ledger
Complete-Action -Ledger $ledger -Action $failedImport -Status failed -Options @{ Code = "operator-rejected-import" }
Assert-True ($null -eq (Invoke-SfroNext $ledger)) "A rejected import must remain blocked without corrupting the ledger."
$assertions++
Get-SfroState $ledger | Out-Null
$assertions++

# Two consultations precede coordinator creation.
$ledger = New-TestLedger
$first = Invoke-SfroNext $ledger
Assert-True ($first.kind -eq "launch-consultation" -and $first.payload.model -eq "gemini-4-argon") "Gemini must be the first launch-plan consultation."
$assertions++
Complete-Action $ledger $first
$second = Invoke-SfroNext $ledger
Assert-True ($second.kind -eq "launch-consultation" -and $second.payload.model -eq "claude-haiku-5.5") "Haiku must be the second launch-plan consultation."
$assertions++
Complete-Action $ledger $second
Assert-True ((Invoke-SfroNext $ledger).kind -eq "create-coordinator") "Coordinator creation must wait for both consultations."
$assertions++

# Reserved effects reconcile after interruption and are never duplicated.
$ledger = New-TestLedger
$reserved = Invoke-SfroNext $ledger
$resumed = Invoke-SfroNext $ledger
Assert-True ([bool]$resumed.reconcile -and $resumed.kind -eq $reserved.kind) "An unresolved intent must reconcile the original action after resume."
$assertions++
Assert-True ($resumed.intentId -eq $reserved.intentId) "Resume must preserve the exact intent id."
$assertions++
Complete-Action -Ledger $ledger -Action $reserved -Status absent
$retried = Invoke-SfroNext $ledger
Assert-True ($retried.actionId -eq $reserved.actionId -and $retried.intentId.EndsWith("-attempt-2")) "A proven-absent effect must retry with a new attempt id."
$assertions++

# A rejected consultation cannot authorize coordinator creation.
$ledger = New-TestLedger
$consultation = Invoke-SfroNext $ledger
$state = Get-SfroState $ledger
$rejected = New-ValidEvidence -Action $consultation -State $state
$rejected.verdict = "REJECT"
Assert-Throws {
    Complete-Action -Ledger $ledger -Action $consultation -OverrideEvidence $rejected
} "must accept" "A rejected launch consultation must fail closed."
$assertions++
$ledger = New-TestLedger
$failedConsultation = Invoke-SfroNext $ledger
Complete-Action -Ledger $ledger -Action $failedConsultation -Status failed -Options @{ Code = "consultation-rejected" }
$afterFailedConsultation = Invoke-SfroNext $ledger
Assert-True ($afterFailedConsultation.pr -eq 69766 -and $afterFailedConsultation.kind -eq "launch-consultation") "A failed consultation must terminalize one PR without deadlocking the queue."
$assertions++

# Per-head hosted-review dedupe and new-head retrigger.
$known = @([ordered]@{
    campaignId = "campaign"
    pr = 42
    head = $shaA
    command = "/review"
    readbackVerified = $true
    commentId = 1
})
$sameHead = Resolve-SfroReviewTriggerDecision -CampaignId "campaign" -Pr 42 -Head $shaA -KnownTriggers $known
$newHead = Resolve-SfroReviewTriggerDecision -CampaignId "campaign" -Pr 42 -Head $shaB -KnownTriggers $known
Assert-True ($sameHead.decision -eq "reused") "The same head must reuse its verified /review trigger."
$assertions++
Assert-True ($newHead.decision -eq "post") "A new head must permit a new /review trigger."
$assertions++

# Only the coordinator can create an arm, and the child parent must match.
$ledger = New-TestLedger
$createArm = Complete-Until -Ledger $ledger -Kind "create-arm"
$state = Get-SfroState $ledger
$badEvidence = New-ValidEvidence -Action $createArm -State $state
$badEvidence.parentSessionId = "wrong-parent"
Assert-Throws {
    Complete-Action -Ledger $ledger -Action $createArm -OverrideEvidence $badEvidence
} "Arm parent mismatch" "A wrong arm parent must be rejected."
$assertions++
$ledger = New-TestLedger
$failedCreateArm = Complete-Until -Ledger $ledger -Kind "create-arm"
Complete-Action -Ledger $ledger -Action $failedCreateArm -Status failed -Options @{ Code = "session-creation-failed" }
$assessmentAfterCreateFailure = Complete-Until -Ledger $ledger -Kind "run-assessment"
Assert-True ($assessmentAfterCreateFailure.kind -eq "run-assessment") "A failed create-arm must preserve a complete-shaped INCOMPLETE original."
$assertions++

# Strict G/M/S/O sequence and preserved failures without replacement.
$ledger = New-TestLedger
$action = Complete-Until -Ledger $ledger -Kind "verify-arm"
Assert-True ($action.payload.arm -eq "G" -and $action.payload.model -eq "gpt-5.6-sol") "G must use GPT-5.6 Sol."
$assertions++
Complete-Action -Ledger $ledger -Action $action -Status failed -Options @{ Code = "model-mismatch" }
$action = Invoke-SfroNext $ledger
Assert-True ($action.kind -eq "create-arm" -and $action.payload.arm -eq "M" -and $action.payload.model -eq "gemini-4-argon") "A failed G must be preserved before M starts."
$assertions++
Complete-Action $ledger $action
$action = Invoke-SfroNext $ledger
Complete-Action $ledger $action
$action = Invoke-SfroNext $ledger
Assert-True ($action.kind -eq "assign-arm" -and $action.payload.arm -eq "M") "M must reach its assignment gate."
$assertions++
Complete-Action -Ledger $ledger -Action $action -Status failed -Options @{ Code = "skill-not-found" }
$action = Invoke-SfroNext $ledger
Assert-True ($action.kind -eq "create-arm" -and $action.payload.arm -eq "S" -and $action.payload.model -eq "gpt-6.1-sol") "A skill-not-found M must not be replaced."
$assertions++
Complete-Action $ledger $action
Complete-Action $ledger (Invoke-SfroNext $ledger)
Complete-Action $ledger (Invoke-SfroNext $ledger)
$action = Invoke-SfroNext $ledger
Assert-True ($action.kind -eq "observe-arm" -and $action.payload.arm -eq "S") "S must be observed before O starts."
$assertions++
Complete-Action -Ledger $ledger -Action $action -Status timed-out -Options @{ Code = "bounded-timeout" }
$action = Invoke-SfroNext $ledger
Assert-True ($action.kind -eq "create-arm" -and $action.payload.arm -eq "O" -and $action.payload.model -eq "claude-opus-5.5") "A timed-out S must be preserved before Opus starts."
$assertions++

# A timed-out arm cannot release the next arm without quiescence proof.
$ledger = New-TestLedger
$observe = Complete-Until -Ledger $ledger -Kind "observe-arm"
Assert-Throws {
    Complete-Action `
        -Ledger $ledger `
        -Action $observe `
        -Status timed-out `
        -OverrideEvidence ([ordered]@{
            code = "bounded-timeout"
            artifactSha256 = $hashA
            sessionId = $observe.payload.sessionId
            quiescent = $false
        })
} "must be quiescent" "A timed-out arm must not overlap the next arm."
$assertions++

# A failed coordinator-owned non-arm action aborts and cleans up one PR instead of deadlocking the queue.
$ledger = New-TestLedger
$freeze = Complete-Until -Ledger $ledger -Kind "freeze-epoch"
Complete-Action -Ledger $ledger -Action $freeze -Status failed -Options @{ Code = "head-moved" }
$abortCleanup = Invoke-SfroNext $ledger
Assert-True ($abortCleanup.kind -eq "cleanup-children") "A failed frozen epoch must enter cleanup instead of globally deadlocking."
$assertions++

# An incomplete panel can never receive the clean label.
$armOutcomes = @{ G = "NO_FINDINGS"; M = "INCOMPLETE"; S = "NO_FINDINGS"; O = "NO_FINDINGS" }
$ledger = New-TestLedger
$cleanup = Complete-Until -Ledger $ledger -Kind "cleanup-children" -ArmOutcomes $armOutcomes
Assert-True ($cleanup.kind -eq "cleanup-children") "An incomplete panel must skip publish-clean."
$assertions++

# Finding panels require all three falsifiers, finalization, and atomic COMMENT publication.
$finding = [ordered]@{
    id = "finding-1"
    path = "src/File.cs"
    line = 12
    side = "RIGHT"
    fingerprint = "<!-- finding:test-f1 -->"
}
$ledger = New-TestLedger
$publish = Complete-Until `
    -Ledger $ledger `
    -Kind "publish-review" `
    -ArmOutcomes @{ G = "FINDINGS"; M = "NO_FINDINGS"; S = "NO_FINDINGS"; O = "NO_FINDINGS" } `
    -AssessmentSurvivors @($finding)
$state = Get-SfroState $ledger
$entry = $state.entries[[string]$publish.pr]
Assert-True ($entry.falsifiers.Count -eq 3) "GPT, Gemini, and Haiku falsifiers must all complete."
$assertions++
Assert-True ($publish.payload.event -eq "COMMENT" -and [bool]$publish.payload.atomic) "Finding publication must be one atomic COMMENT review."
$assertions++
$badPublication = New-ValidEvidence -Action $publish -State $state
$badPublication.changedLinesValid = $false
Assert-Throws {
    Complete-Action -Ledger $ledger -Action $publish -OverrideEvidence $badPublication
} "changed RIGHT-side line" "Publication with an invalid changed-line anchor must be rejected."
$assertions++

# Cleanup requires every recorded child exactly once.
$ledger = New-TestLedger
$cleanup = Complete-Until -Ledger $ledger -Kind "cleanup-children"
$state = Get-SfroState $ledger
$badCleanup = New-ValidEvidence -Action $cleanup -State $state
$badCleanup.archivedSessionIds = @($badCleanup.archivedSessionIds | Select-Object -First 3)
Assert-Throws {
    Complete-Action -Ledger $ledger -Action $cleanup -OverrideEvidence $badCleanup
} "archive every recorded child" "Partial child cleanup must be rejected."
$assertions++
Complete-Action $ledger $cleanup
$handoff = Invoke-SfroNext $ledger
Complete-Action $ledger $handoff
$archive = Invoke-SfroNext $ledger
Complete-Action $ledger $archive
$nextPr = Invoke-SfroNext $ledger
Assert-True ($nextPr.pr -eq 69766 -and $nextPr.kind -eq "launch-consultation") "The next PR must release only after child and coordinator archival."
$assertions++

# Torn ledgers and stale revisions fail closed.
$ledger = New-TestLedger
[IO.File]::AppendAllText($ledger, "{", [Text.UTF8Encoding]::new($false))
Assert-Throws {
    Read-SfroLedger $ledger | Out-Null
} "partial trailing line" "A torn final ledger line must be rejected."
$assertions++

# Fixture-only dry-run/canary performs zero mutations.
$simulation = Invoke-SfroSimulation $fixturePath
Assert-True ($simulation.githubMutations -eq 0 -and $simulation.appMutations -eq 0) "Simulation must perform zero mutations."
$assertions++

# Migration reads snapshots without changing them and binds explicit acceptance metadata.
$migrationRoot = Join-Path ([IO.Path]::GetTempPath()) ("sfro-import-" + [guid]::NewGuid().ToString("N"))
[IO.Directory]::CreateDirectory($migrationRoot) | Out-Null
$script:TempRoots.Add($migrationRoot)
$candidatePath = Join-Path $migrationRoot "candidate.json"
$legacyHashBefore = (Get-FileHash -Algorithm SHA256 -LiteralPath $legacyControlPath).Hash
$migration = (& $importScript `
    -ControlJsonPath $legacyControlPath `
    -OutputPath $candidatePath `
    -RootSessionId "campaign-root-session") | ConvertFrom-Json
$legacyHashAfter = (Get-FileHash -Algorithm SHA256 -LiteralPath $legacyControlPath).Hash
Assert-True ($migration.queueCount -eq 33 -and $migration.requiresExplicitAcceptance) "Migration must produce a 33-entry candidate requiring acceptance."
$assertions++
Assert-True ($legacyHashBefore -eq $legacyHashAfter -and $migration.sourceFilesUnchanged) "Migration must not modify its source snapshot."
$assertions++
Import-Module -Scope Local -Force -DisableNameChecking $modulePath
$importLedger = Join-Path $migrationRoot "import-ledger.jsonl"
New-SfroCampaign -LedgerPath $importLedger -ConfigPath $candidatePath | Out-Null
$acceptImport = Invoke-SfroNext $importLedger
Assert-True ($acceptImport.kind -eq "accept-import") "Imported candidates must require acceptance without an extra CLI switch."
$assertions++
Complete-Action $importLedger $acceptImport
$importState = Get-SfroState $importLedger
Assert-True ($importState.entries["69945"].status -eq "terminal") "Imported completed PRs must not be reviewed again."
$assertions++
Assert-True ((Invoke-SfroNext $importLedger).kind -eq "reconcile-imported-active") "Imported active PRs must be reconciled before replacement work."
$assertions++

# GitHub adapter dry-runs render operations without invoking GitHub.
$triggerPlan = (& $githubAdapter -Operation EnsureReviewTrigger -RequestPath $triggerRequestPath) | ConvertFrom-Json
$reviewPlan = (& $githubAdapter -Operation PublishReview -RequestPath $publishRequestPath) | ConvertFrom-Json
Assert-True ($triggerPlan.githubCalls -eq 0 -and $triggerPlan.mutationPlan.decision -eq "reused") "Same-head trigger dry-run must reuse without a GitHub call."
$assertions++
Assert-True ($reviewPlan.githubCalls -eq 0 -and $reviewPlan.mutationPlan.body.event -eq "COMMENT") "Review dry-run must render one COMMENT review without a GitHub call."
$assertions++
$triggerExecution = (& $githubAdapter -Operation EnsureReviewTrigger -RequestPath $triggerRequestPath -FixturePath $githubFixturePath -Execute) | ConvertFrom-Json
$reviewExecution = (& $githubAdapter -Operation PublishReview -RequestPath $publishRequestPath -FixturePath $githubFixturePath -Execute) | ConvertFrom-Json
$cleanExecution = (& $githubAdapter -Operation PublishClean -RequestPath $cleanRequestPath -FixturePath $githubFixturePath -Execute) | ConvertFrom-Json
$reviewRecovery = (& $githubAdapter -Operation PublishReview -RequestPath $publishRequestPath -FixturePath $existingReviewFixturePath -Execute) | ConvertFrom-Json
$cleanRecovery = (& $githubAdapter -Operation PublishClean -RequestPath $cleanRequestPath -FixturePath $partialCleanFixturePath -Execute) | ConvertFrom-Json
$triggerRecovery = (& $githubAdapter -Operation EnsureReviewTrigger -RequestPath $triggerReconcileRequestPath -FixturePath $triggerReconcileFixturePath -Execute) | ConvertFrom-Json
Assert-True ($triggerExecution.evidence.readbackVerified -and $triggerExecution.evidence.decision -eq "reused") "Fixture execution must live-read the retained /review comment."
$assertions++
Assert-True ($reviewExecution.evidence.readbackVerified -and $reviewExecution.evidence.reviewId -eq 202) "Fixture execution must validate atomic review readback."
$assertions++
Assert-True ($cleanExecution.evidence.readbackVerified -and $cleanExecution.evidence.labelCountAfter -eq 1 -and $cleanExecution.evidence.markerCountAfter -eq 1) "Fixture execution must recover the clean marker and label exactly once."
$assertions++
Assert-True ($reviewRecovery.evidence.reviewId -eq 202 -and $reviewRecovery.githubCalls -eq 6) "Review reconciliation must reuse the existing marked review without another POST."
$assertions++
Assert-True (-not $cleanRecovery.evidence.markerWasAbsent -and $cleanRecovery.evidence.labelCountAfter -eq 1) "Clean reconciliation must finish a partial marker-before-label publication."
$assertions++
Assert-True ($triggerRecovery.evidence.decision -eq "reused" -and $triggerRecovery.evidence.commentId -eq 124) "Trigger reconciliation must discover a prior post instead of duplicating /review."
$assertions++

# Canonical JSON preserves top-level array identity.
Import-Module -Scope Local -Force -DisableNameChecking $modulePath
Assert-True ((ConvertTo-SfroCanonicalJson @()) -ceq "[]") "Canonical JSON must preserve an empty top-level array."
$assertions++
Assert-True ((ConvertTo-SfroCanonicalJson @("one")) -ceq '["one"]') "Canonical JSON must preserve a one-element top-level array."
$assertions++

foreach ($tempRoot in $script:TempRoots) {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

Write-Host "Validated $assertions Syncfusion review orchestrator assertions."
