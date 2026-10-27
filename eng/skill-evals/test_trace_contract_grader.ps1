#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$grader = Join-Path $PSScriptRoot 'trace-contract-grader.ps1'
$controlPath = Join-Path $PSScriptRoot 'review-pull-request/fixtures/controls/retained-sdk-lifecycle.json'
$bundleRoot = Join-Path $PSScriptRoot 'review-pull-request/fixtures/bundle'
$contractSource = Join-Path $PSScriptRoot 'review-pull-request/fixtures/contracts/inventory.json'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-review-trace-grader-test-' + [guid]::NewGuid().ToString('N')
)
$utf8 = [Text.UTF8Encoding]::new($false)
$workerRoleBoundary = 'Worker role boundary: you are not the coordinator. The platform-required skill(skill="review-pull-request") load may occur at most once and only as your first tool call; it is instruction acquisition, not behavioral recursion. Do not invoke skill again, bootstrap or prepare review, spawn tasks/subagents/sessions, read coordinator-only manifest/pull/feedback, access live GitHub or fetch, mutate or publish, execute target code, aggregate guides, or emit PATH: per-guide. Return PATH: per-guide-worker and name only the assigned guide.'

function Clone-Object {
    param($Value)

    return ($Value | ConvertTo-Json -Depth 100) | ConvertFrom-Json -Depth 100
}

function Invoke-Control {
    param(
        [string]$Name,
        [string]$Contract,
        [object]$GraderInput,
        [bool]$ExpectedPassed,
        [string]$ExpectedEvidence,
        [string]$ContractPath,
        [int]$ExpectedPr = 0,
        [string]$ExpectedHead = ''
    )

    $inputPath = Join-Path $testRoot "$Name.json"
    [IO.File]::WriteAllText(
        $inputPath,
        ($GraderInput | ConvertTo-Json -Depth 100) + "`n",
        $utf8
    )
    $previous = $env:EVALUATE_GRADER_INPUT
    try {
        $env:EVALUATE_GRADER_INPUT = $inputPath
        $arguments = @('-NoProfile', '-File', $grader, $Contract)
        if ($ContractPath) {
            $arguments += @('-ContractPath', $ContractPath)
        }
        if ($ExpectedPr -gt 0) {
            $arguments += @('-ExpectedPr', $ExpectedPr)
        }
        if ($ExpectedHead) {
            $arguments += @('-ExpectedHead', $ExpectedHead)
        }
        $output = & pwsh @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "$Name exited with $LASTEXITCODE."
        }
        if (@($output).Count -ne 1) {
            throw "$Name emitted $(@($output).Count) stdout records instead of one GraderResult."
        }
        $result = ($output | Out-String).Trim() | ConvertFrom-Json -Depth 20
    } finally {
        $env:EVALUATE_GRADER_INPUT = $previous
    }

    if ($result.passed -ne $ExpectedPassed) {
        throw "$Name expected passed=$ExpectedPassed but got passed=$($result.passed): $($result.evidence)"
    }
    $expectedScore = if ($ExpectedPassed) { 1 } else { 0 }
    if ([int]$result.score -ne $expectedScore) {
        throw "$Name expected binary score $expectedScore but got $($result.score)."
    }
    if ($ExpectedEvidence -and $result.evidence -notlike "*$ExpectedEvidence*") {
        throw "$Name expected evidence containing '$ExpectedEvidence' but got '$($result.evidence)'."
    }
    Write-Host "  [OK] $Name"
}

function New-Call {
    param(
        [string]$Name,
        [string]$Id,
        [hashtable]$Arguments,
        [bool]$Success,
        $Result,
        [string]$AgentId = ''
    )

    return @(
        [pscustomobject]@{
            type = 'tool_call'
            agentId = $AgentId
            data = [pscustomobject]@{
                toolName = $Name
                toolCallId = $Id
                arguments = [pscustomobject]$Arguments
            }
        },
        [pscustomobject]@{
            type = 'tool_result'
            agentId = $AgentId
            data = [pscustomobject]@{
                toolName = $Name
                toolCallId = $Id
                success = $Success
                result = $Result
            }
        }
    )
}

function Get-ControlEventIndexes {
    param([object]$ControlInput, [string]$ToolCallId)

    return @(
        for ($index = 0; $index -lt $ControlInput.trajectory.events.Count; $index++) {
            $toolCallIdProperty =
                $ControlInput.trajectory.events[$index].data.PSObject.Properties['toolCallId']
            if ($null -ne $toolCallIdProperty -and
                [string]$toolCallIdProperty.Value -ceq $ToolCallId) {
                $index
            }
        }
    )
}

function Add-NumberedRead {
    param(
        [Collections.Generic.List[object]]$Events,
        [string]$RelativePath,
        [int]$Start,
        [int]$End,
        [string]$Id,
        [string]$AgentId = ''
    )

    $lines = @(Get-Content (Join-Path $testRoot ".review-bundle/$RelativePath") -Encoding utf8)
    $last = if ($End -lt 0) { $lines.Count } else { $End }
    $result = for ($line = $Start; $line -le $last; $line++) {
        "L${line}:$($lines[$line - 1])"
    }
    $path = if ($RelativePath -eq 'files.json') {
        '.\.review-bundle\files.json'
    } else {
        ".review-bundle/$RelativePath"
    }
    foreach ($event in New-Call view $Id @{
        path = $path
        view_range = @($Start, $(if ($End -lt 0) { -1 } else { $End }))
    } $true ($result -join "`n") $AgentId) {
        $Events.Add($event)
    }
}

function New-WorkerPrompt {
    param(
        [object]$Manifest,
        [object[]]$Files,
        [object]$Guide,
        [object]$Policy
    )

    $prompt = @(
        'Review the assigned projected transport inputs.'
        $workerRoleBoundary
        "Assigned guide: guidance/$($Guide.path).source"
        "Applicable policy: $($Policy.body.path)"
        'Projected changed-file metadata: files.json'
        'Full diff: diff.patch'
        "Head source root: $($Manifest.sources.head.root)"
        "Merge-base source root: $($Manifest.sources.mergeBase.root)"
        "Base-tip source root: $($Manifest.sources.baseTip.root)"
        @($Files | ForEach-Object { "Relevant source: $($_.filename)" })
        'Do not read pull.json; the coordinator owns that input.'
    )
    if ([string]$Guide.path -ceq 'docs/BlazorComponentsGuidance.md') {
        $prompt += 'Do not read guidance/docs/CrossCuttingGuidance.md.source or required/policies/0000.source; those inputs are not assigned.'
    }
    return $prompt -join "`n"
}

function New-TransportInput {
    $events = [Collections.Generic.List[object]]::new()
    $contract = Get-Content $contractSource -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    $required = @($contract.files | Where-Object coordinatorRequired)
    $manifest = Get-Content (
        Join-Path $testRoot '.review-bundle/manifest.json'
    ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    $files = @(Get-Content (
        Join-Path $testRoot '.review-bundle/files.json'
    ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100)
    foreach ($path in @(
        'manifest.json',
        'guidance/.github/skills/review-pull-request/routing.md.source'
    )) {
        $file = $required | Where-Object path -CEQ $path
        Add-NumberedRead $events $path 1 -1 ("read-" + $path.Replace('/', '-').Replace('.', '-'))
    }
    foreach ($event in New-Call task worker-blazor @{
        agent_type = 'general-purpose'
        model = 'gpt-5.6-sol'
        mode = 'background'
        prompt = New-WorkerPrompt `
            -Manifest $manifest `
            -Files $files `
            -Guide $manifest.guides[1] `
            -Policy $manifest.policies[1]
    } $true 'Agent started in background with agent_id: 11111111-1111-1111-1111-111111111111.') {
        $events.Add($event)
    }
    foreach ($event in New-Call skill worker-blazor-skill @{
        skill = 'review-pull-request'
    } $true 'Skill loaded.' '11111111-1111-1111-1111-111111111111') {
        $events.Add($event)
    }
    foreach ($event in New-Call view worker-blazor-guide @{
        path = '.review-bundle/guidance/docs/BlazorComponentsGuidance.md.source'
        view_range = @(1, -1)
    } $true 'L1:# Blazor components guidance' '11111111-1111-1111-1111-111111111111') {
        $events.Add($event)
    }
    foreach ($event in New-Call task worker @{
        agent_type = 'general-purpose'
        model = 'gpt-5.6-sol'
        mode = 'background'
        prompt = New-WorkerPrompt `
            -Manifest $manifest `
            -Files $files `
            -Guide $manifest.guides[0] `
            -Policy $manifest.policies[0]
    } $true 'Agent started in background with agent_id: 22222222-2222-2222-2222-222222222222.') {
        $events.Add($event)
    }
    foreach ($event in New-Call skill worker-cross-skill @{
        skill = 'review-pull-request'
    } $true 'Skill loaded.' '22222222-2222-2222-2222-222222222222') {
        $events.Add($event)
    }
    foreach ($event in New-Call rg worker-cross-diff @{
        pattern = 'Value'
        paths = '.review-bundle/diff.patch'
        output_mode = 'content'
    } $true 'diff --git a/src/Value.cs b/src/Value.cs' '22222222-2222-2222-2222-222222222222') {
        $events.Add($event)
    }
    $events.Add([pscustomobject]@{
        type = 'assistant_message'
        agentId = '11111111-1111-1111-1111-111111111111'
        data = [pscustomobject]@{
            content = "PATH: per-guide-worker`nGUIDE: docs/BlazorComponentsGuidance.md`nSTATUS: complete"
        }
    })
    $events.Add([pscustomobject]@{
        type = 'assistant_message'
        agentId = '22222222-2222-2222-2222-222222222222'
        data = [pscustomobject]@{
            content = "PATH: per-guide-worker`nGUIDE: docs/CrossCuttingGuidance.md`nSTATUS: complete"
        }
    })
    foreach ($event in New-Call read_agent worker-blazor-report @{
        agent_id = '11111111-1111-1111-1111-111111111111'
        wait = $true
    } $true "Agent is idle, status: idle, total_turns: 1. Turn 0: PATH: per-guide-worker`nGUIDE: docs/BlazorComponentsGuidance.md`nSTATUS: complete") {
        $events.Add($event)
    }
    foreach ($event in New-Call read_agent worker-cross-report @{
        agent_id = '22222222-2222-2222-2222-222222222222'
        wait = $true
    } $true "Agent is idle, status: idle, total_turns: 1. Turn 0: PATH: per-guide-worker`nGUIDE: docs/CrossCuttingGuidance.md`nSTATUS: complete") {
        $events.Add($event)
    }
    foreach ($file in $required | Where-Object {
        $_.path -notin @(
            'manifest.json',
            'guidance/.github/skills/review-pull-request/routing.md.source'
        )
    }) {
        $id = "read-" + ([string]$file.path).Replace('/', '-').Replace('.', '-')
        if ($file.path -eq 'required/pull/body.source') {
            Add-NumberedRead $events $file.path 1 90 "$id-a"
            Add-NumberedRead $events $file.path 91 -1 "$id-b"
        } else {
            Add-NumberedRead $events $file.path 1 -1 $id
        }
    }
    return [pscustomobject]@{
        trajectory = [pscustomobject]@{
            workDir = $testRoot
            output = 'STATUS: NO_FINDINGS'
            events = $events.ToArray()
        }
    }
}

New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    Copy-Item $bundleRoot (Join-Path $testRoot '.review-bundle') -Recurse
    New-Item -ItemType Directory -Path (Join-Path $testRoot '.eval-grader') | Out-Null
    Copy-Item $contractSource (Join-Path $testRoot '.eval-grader/inventory.json')

    $retained = Get-Content $controlPath -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    $retainedLifecycle = Clone-Object $retained.input
    $retainedLifecycle.trajectory.events = @($retainedLifecycle.trajectory.events[6..14])
    $retainedLifecycle.trajectory.workDir = $testRoot
    $expectedPr = 70024
    $expectedHead = 'd3068c868d2fafeb5f3a37f94c4d8637ce063ed0'
    Invoke-Control sdk-retained-unbound sdk-recovery $retained.input $false 'initial activate-and-prepare' '' $expectedPr $expectedHead

    $sdkBound = Clone-Object $retained.input
    $sdkBound.trajectory.events[0].data.arguments.command += " --head $expectedHead"
    $sdkBound.trajectory.events[2].data.arguments.command += " --head $expectedHead"
    $sdkBound.trajectory.events[5].data.result =
        "L1:{`nL2:  `"version`": 2,`nL3:  `"ready`": true,`nL4:  `"target`": {`nL5:    `"pr`": $expectedPr,`nL6:    `"head`": `"$expectedHead`"`nL7:  }`nL8:}"
    Invoke-Control sdk-bound-combined sdk-recovery $sdkBound $true 'pinned-head preparation' '' $expectedPr $expectedHead

    $sdkSplit = Clone-Object $sdkBound
    $splitInstall = New-Call powershell sdk-install @{
        command = '& .\eng\common\dotnet.ps1'
    } $true ([pscustomobject]@{ content = 'Installed the pinned SDK.'; exitCode = 0 })
    $splitPrepare = New-Call powershell sdk-recovery @{
        command = ". .\activate.ps1; dotnet run .\.github\skills\review-pull-request\scripts\prepare-review.cs -- --pr $expectedPr --head $expectedHead"
    } $true $sdkSplit.trajectory.events[3].data.result
    $sdkSplit.trajectory.events = @(
        $sdkSplit.trajectory.events[0..1] +
        $splitInstall +
        $splitPrepare +
        $sdkSplit.trajectory.events[4..($sdkSplit.trajectory.events.Count - 1)]
    )
    Invoke-Control sdk-split-install sdk-recovery $sdkSplit $true 'pinned-head preparation' '' $expectedPr $expectedHead

    $sdkSplitFailed = Clone-Object $sdkSplit
    $sdkSplitFailed.trajectory.events[3].data.success = $false
    $sdkSplitFailed.trajectory.events[3].data.result = [pscustomobject]@{
        content = 'SDK installation failed.'
        exitCode = 1
    }
    Invoke-Control sdk-split-install-failed sdk-recovery $sdkSplitFailed $false 'No successful supported SDK install' '' $expectedPr $expectedHead

    $sdkLateInstall = Clone-Object $sdkBound
    $sdkLateInstall.trajectory.events[2].data.arguments.command =
        ". .\activate.ps1; dotnet run .\.github\skills\review-pull-request\scripts\prepare-review.cs -- --pr $expectedPr --head $expectedHead; & .\eng\common\dotnet.ps1"
    Invoke-Control sdk-late-install sdk-recovery $sdkLateInstall $false 'No successful supported SDK install' '' $expectedPr $expectedHead

    $sdkUnguardedCombined = Clone-Object $sdkBound
    $sdkUnguardedCombined.trajectory.events[2].data.arguments.command =
        "& .\eng\common\dotnet.ps1; . .\activate.ps1; dotnet run .\.github\skills\review-pull-request\scripts\prepare-review.cs -- --pr $expectedPr --head $expectedHead"
    Invoke-Control sdk-unguarded-combined sdk-recovery $sdkUnguardedCombined $false 'No successful supported SDK install' '' $expectedPr $expectedHead

    Invoke-Control retained-lifecycle same-worker-lifecycle $retainedLifecycle $true 'same ID' '.eval-grader/inventory.json'

    $sdkNoReactivate = Clone-Object $sdkBound
    $sdkNoReactivate.trajectory.events[2].data.arguments.command =
        "& .\eng\common\dotnet.ps1; dotnet run .\.github\skills\review-pull-request\scripts\prepare-review.cs -- --pr $expectedPr --head $expectedHead"
    Invoke-Control sdk-missing-reactivation sdk-recovery $sdkNoReactivate $false 'same-invocation reactivation' '' $expectedPr $expectedHead

    $sdkNotV2 = Clone-Object $sdkBound
    $sdkNotV2.trajectory.events[5].data.result = '{"version":1,"ready":true}'
    Invoke-Control sdk-not-v2 sdk-recovery $sdkNotV2 $false 'ready version-2 manifest' '' $expectedPr $expectedHead

    $sdkWrongCommandHead = Clone-Object $sdkBound
    $sdkWrongCommandHead.trajectory.events[2].data.arguments.command =
        $sdkWrongCommandHead.trajectory.events[2].data.arguments.command.Replace(
            $expectedHead,
            '0000000000000000000000000000000000000000'
        )
    Invoke-Control sdk-wrong-command-head sdk-recovery $sdkWrongCommandHead $false 'pinned-head preparation' '' $expectedPr $expectedHead

    $sdkExpectedHeadOutsidePrepare = Clone-Object $sdkBound
    $sdkExpectedHeadOutsidePrepare.trajectory.events[2].data.arguments.command =
        "Write-Output '--pr $expectedPr --head $expectedHead'; . .\activate.ps1; dotnet run .\.github\skills\review-pull-request\scripts\prepare-review.cs -- --pr 1 --head 0000000000000000000000000000000000000000"
    Invoke-Control sdk-expected-head-outside-prepare sdk-recovery $sdkExpectedHeadOutsidePrepare $false 'pinned-head preparation' '' $expectedPr $expectedHead

    $sdkChildActivation = Clone-Object $sdkSplit
    $sdkChildActivation.trajectory.events[4].data.arguments.command =
        "& .\activate.ps1; dotnet run .\.github\skills\review-pull-request\scripts\prepare-review.cs -- --pr $expectedPr --head $expectedHead"
    Invoke-Control sdk-child-activation sdk-recovery $sdkChildActivation $false 'same-invocation reactivation' '' $expectedPr $expectedHead

    $sdkWrongManifestHead = Clone-Object $sdkBound
    $sdkWrongManifestHead.trajectory.events[5].data.result =
        $sdkWrongManifestHead.trajectory.events[5].data.result.Replace(
            $expectedHead,
            '0000000000000000000000000000000000000000'
        )
    Invoke-Control sdk-wrong-manifest-head sdk-recovery $sdkWrongManifestHead $false 'pinned PR head' '' $expectedPr $expectedHead

    $sdkUnlinkedManifest = Clone-Object $sdkBound
    $sdkUnlinkedManifest.trajectory.events[4].data.arguments.path =
        'C:\synthetic\other-bundle\manifest.json'
    Invoke-Control sdk-unlinked-manifest sdk-recovery $sdkUnlinkedManifest $false 'its content-bearing' '' $expectedPr $expectedHead

    $syncWorker = Clone-Object $retainedLifecycle
    $syncWorker.trajectory.events[0].data.arguments.mode = 'sync'
    Invoke-Control lifecycle-sync same-worker-lifecycle $syncWorker $false 'not dispatched as' '.eval-grader/inventory.json'

    $wrongWorker = Clone-Object $retainedLifecycle
    $wrongWorker.trajectory.events[4].data.arguments.agent_id = '22222222-2222-2222-2222-222222222222'
    Invoke-Control lifecycle-wrong-id same-worker-lifecycle $wrongWorker $false 'other than' '.eval-grader/inventory.json'

    $unrelatedClarification = Clone-Object $retainedLifecycle
    $unrelatedClarification.trajectory.events[4].data.arguments.message =
        'Summarize your report.'
    Invoke-Control lifecycle-unrelated-clarification same-worker-lifecycle $unrelatedClarification $false 'requested candidate clarification' '.eval-grader/inventory.json'

    $ambiguousMultipleCandidates = Clone-Object $retainedLifecycle
    $ambiguousMultipleCandidates.trajectory.events[3].data.result =
        $ambiguousMultipleCandidates.trajectory.events[3].data.result.Replace(
            'one candidate',
            'two candidates'
        )
    Invoke-Control lifecycle-ambiguous-multiple-candidates same-worker-lifecycle $ambiguousMultipleCandidates $false 'strongest candidate' '.eval-grader/inventory.json'

    $strongestMultipleCandidates = Clone-Object $ambiguousMultipleCandidates
    $strongestMultipleCandidates.trajectory.events[4].data.arguments.message =
        'Clarify the strongest candidate against the frozen base contract.'
    Invoke-Control lifecycle-strongest-multiple-candidates same-worker-lifecycle $strongestMultipleCandidates $true 'same ID' '.eval-grader/inventory.json'

    $oneThenAnotherCandidate = Clone-Object $retainedLifecycle
    $oneThenAnotherCandidate.trajectory.events[3].data.result =
        $oneThenAnotherCandidate.trajectory.events[3].data.result.Replace(
            'one candidate requiring frozen base-contract clarification',
            'one candidate is high severity, and another candidate is medium severity'
        )
    Invoke-Control lifecycle-one-then-another-candidate same-worker-lifecycle $oneThenAnotherCandidate $false 'strongest candidate' '.eval-grader/inventory.json'

    $missingInitialReport = Clone-Object $retainedLifecycle
    $missingInitialReport.trajectory.events[3].data.result = 'Agent is running.'
    Invoke-Control lifecycle-missing-initial same-worker-lifecycle $missingInitialReport $false 'initial report collection' '.eval-grader/inventory.json'

    $missingFollowUp = Clone-Object $retainedLifecycle
    $missingFollowUp.trajectory.events[7].data.success = $false
    Invoke-Control lifecycle-missing-follow-up same-worker-lifecycle $missingFollowUp $false 'did not complete successfully' '.eval-grader/inventory.json'

    $identicalFollowUp = Clone-Object $retainedLifecycle
    $identicalFollowUp.trajectory.events[7].data.result =
        $identicalFollowUp.trajectory.events[3].data.result
    Invoke-Control lifecycle-identical-follow-up same-worker-lifecycle $identicalFollowUp $false 'actual completed worker follow-up' '.eval-grader/inventory.json'

    $idleOnly = Clone-Object $retainedLifecycle
    $idleOnly.trajectory.events[3].data.result =
        'Agent is idle, status: idle, total_turns: 1. No report content returned.'
    Invoke-Control lifecycle-idle-only same-worker-lifecycle $idleOnly $false 'actual completed worker report' '.eval-grader/inventory.json'

    $pathOnly = Clone-Object $retainedLifecycle
    $pathOnly.trajectory.events[3].data.result =
        'Agent is idle, status: idle, total_turns: 1. Turn 0: PATH: per-guide-worker.'
    $pathOnly.trajectory.events[7].data.result =
        'Agent is idle, status: idle, total_turns: 2. Turn 1: PATH: per-guide-worker.'
    Invoke-Control lifecycle-path-only same-worker-lifecycle $pathOnly $false 'actual completed worker report' '.eval-grader/inventory.json'

    $completedStatusWords = Clone-Object $retainedLifecycle
    $completedStatusWords.trajectory.events[3].data.result +=
        ' Running target code remained excluded; no required check failed or was cancelled.'
    $completedStatusWords.trajectory.events[7].data.result +=
        ' The in-progress candidate remained unresolved.'
    Invoke-Control lifecycle-completed-status-words same-worker-lifecycle $completedStatusWords $true 'same ID' '.eval-grader/inventory.json'

    $retryLifecycle = Clone-Object $retainedLifecycle
    $preRetry = New-Call read_agent initial-running @{
        agent_id = '3808c3c7-a0d6-4a0d-8175-26ead12116bc'
        wait = $true
        since_turn = 0
    } $true 'Agent is still running, status: running, total_turns: 1. Turn 0: PATH: per-guide-worker. Initial report is partially available but not complete.'
    $postRetry = New-Call read_agent follow-up-running @{
        agent_id = '3808c3c7-a0d6-4a0d-8175-26ead12116bc'
        wait = $true
        since_turn = 0
    } $true 'Agent is still running, status: running, total_turns: 2. Turn 0: PATH: per-guide-worker. Initial report complete with one candidate requiring frozen base-contract clarification. Turn 1: PATH: per-guide-worker. Follow-up reasoning is partially available but not complete.'
    $retryLifecycle.trajectory.events = @(
        $retryLifecycle.trajectory.events[0..1] +
        $preRetry +
        $retryLifecycle.trajectory.events[2..5] +
        $postRetry +
        $retryLifecycle.trajectory.events[6..8]
    )
    Invoke-Control lifecycle-running-retries same-worker-lifecycle $retryLifecycle $true 'same ID' '.eval-grader/inventory.json'

    $runningFinal = Clone-Object $retainedLifecycle
    $runningFinal.trajectory.events[7].data.result =
        'Agent is still running, status: running, total_turns: 2. Turn 1: PATH: per-guide-worker. Clarification complete enough to report a candidate.'
    Invoke-Control lifecycle-running-final same-worker-lifecycle $runningFinal $false 'did not reach a completed worker state' '.eval-grader/inventory.json'

    $nonWaitRead = Clone-Object $retainedLifecycle
    $nonWaitRead.trajectory.events[2].data.arguments.wait = $false
    Invoke-Control lifecycle-nonwait-read same-worker-lifecycle $nonWaitRead $false 'wait:true' '.eval-grader/inventory.json'

    $prematureSynthesis = Clone-Object $retainedLifecycle
    $prematureSynthesis.trajectory.events = @(
        $prematureSynthesis.trajectory.events[0..5] +
        $prematureSynthesis.trajectory.events[8] +
        $prematureSynthesis.trajectory.events[6..7]
    )
    Invoke-Control lifecycle-premature-synthesis same-worker-lifecycle $prematureSynthesis $false 'before the completed same-worker follow-up' '.eval-grader/inventory.json'

    $transport = New-TransportInput
    Invoke-Control transport-positive ready-v2-transport $transport $true 'exact coordinator reads' '.eval-grader/inventory.json'

    $rootSkill = Clone-Object $transport
    $rootSkill.trajectory.events = @(
        New-Call skill root-skill @{
            skill = 'review-pull-request'
        } $true 'Skill loaded.'
    ) + @($rootSkill.trajectory.events)
    Invoke-Control transport-root-skill ready-v2-transport $rootSkill $true 'valid role separation' '.eval-grader/inventory.json'

    $skillTextOnly = Clone-Object $transport
    $initialSkillIndexes = @(
        Get-ControlEventIndexes -ControlInput $skillTextOnly -ToolCallId 'worker-blazor-skill'
        Get-ControlEventIndexes -ControlInput $skillTextOnly -ToolCallId 'worker-cross-skill'
    )
    $skillTextOnly.trajectory.events = @(
        for ($index = 0; $index -lt $skillTextOnly.trajectory.events.Count; $index++) {
            if ($index -notin $initialSkillIndexes) {
                $skillTextOnly.trajectory.events[$index]
            }
        }
    )
    foreach ($toolCallId in @('worker', 'worker-blazor')) {
        $workerCall = Get-ControlEventIndexes -ControlInput $skillTextOnly -ToolCallId $toolCallId
        $skillTextOnly.trajectory.events[$workerCall[0]].data.arguments.prompt =
            $skillTextOnly.trajectory.events[$workerCall[0]].data.arguments.prompt.Replace(
                $workerRoleBoundary,
                'Do not re-invoke review-pull-request. Return PATH: per-guide-worker.'
            )
    }
    Invoke-Control transport-worker-skill-text-only ready-v2-transport $skillTextOnly $true 'valid role separation' '.eval-grader/inventory.json'

    $repeatedSkill = Clone-Object $transport
    $repeatedSkill.trajectory.events += New-Call skill worker-cross-skill-repeat @{
        skill = 'review-pull-request'
    } $true 'Skill loaded again.' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-repeated-skill ready-v2-transport $repeatedSkill $false 'more than once' '.eval-grader/inventory.json'

    $lateSkill = Clone-Object $transport
    $lateSkillIndexes = Get-ControlEventIndexes -ControlInput $lateSkill -ToolCallId 'worker-cross-skill'
    $lateSkillEvents = @($lateSkill.trajectory.events[$lateSkillIndexes])
    $lateSkill.trajectory.events = @(
        for ($index = 0; $index -lt $lateSkill.trajectory.events.Count; $index++) {
            if ($index -notin $lateSkillIndexes) {
                $lateSkill.trajectory.events[$index]
            }
        }
    ) + $lateSkillEvents
    Invoke-Control transport-worker-late-skill ready-v2-transport $lateSkill $false 'first tool call' '.eval-grader/inventory.json'

    $wrongSkill = Clone-Object $transport
    $wrongSkillIndexes = Get-ControlEventIndexes -ControlInput $wrongSkill -ToolCallId 'worker-cross-skill'
    $wrongSkill.trajectory.events[$wrongSkillIndexes[0]].data.arguments.skill = 'other-skill'
    Invoke-Control transport-worker-wrong-skill ready-v2-transport $wrongSkill $false 'unsupported skill' '.eval-grader/inventory.json'

    $wrongSkillCase = Clone-Object $transport
    $wrongSkillCaseIndexes = Get-ControlEventIndexes -ControlInput $wrongSkillCase -ToolCallId 'worker-cross-skill'
    $wrongSkillCase.trajectory.events[$wrongSkillCaseIndexes[0]].data.toolName = 'Skill'
    Invoke-Control transport-worker-wrong-skill-case ready-v2-transport $wrongSkillCase $false 'unsupported skill' '.eval-grader/inventory.json'

    foreach ($coordinatorBehavior in @(
        @{
            Name = 'transport-worker-task-spawn'
            Tool = 'task'
            Arguments = @{ agent_type = 'general-purpose'; prompt = 'Nested review.' }
        },
        @{
            Name = 'transport-worker-session-spawn'
            Tool = 'create_session'
            Arguments = @{ name = 'nested'; kickoff = @{ prompt = 'Nested review.' } }
        },
        @{
            Name = 'transport-worker-agent-orchestration'
            Tool = 'read_agent'
            Arguments = @{ agent_id = '55555555-5555-5555-5555-555555555555'; wait = $true }
        },
        @{
            Name = 'transport-worker-live-fetch'
            Tool = 'web_fetch'
            Arguments = @{ url = 'https://example.invalid/' }
        },
        @{
            Name = 'transport-worker-live-github'
            Tool = 'semantic_code_search'
            Arguments = @{ repo = 'dotnet/aspnetcore'; query = 'live source' }
        },
        @{
            Name = 'transport-worker-mutation'
            Tool = 'apply_patch'
            Arguments = @{ patch = 'mutation' }
        },
        @{
            Name = 'transport-worker-publication'
            Tool = 'create_pull_request'
            Arguments = @{ title = 'publication' }
        },
        @{
            Name = 'transport-worker-create'
            Tool = 'create'
            Arguments = @{ path = 'new.txt'; content = 'mutation' }
        },
        @{
            Name = 'transport-worker-edit'
            Tool = 'edit'
            Arguments = @{ path = 'existing.txt'; content = 'mutation' }
        },
        @{
            Name = 'transport-worker-run-workflow'
            Tool = 'run_workflow'
            Arguments = @{ workflow_id = 'workflow' }
        },
        @{
            Name = 'transport-worker-complete-draft-resolution'
            Tool = 'complete_workspace_file_draft_resolution'
            Arguments = @{ request_id = 'request'; files = @() }
        },
        @{
            Name = 'transport-worker-read-draft-conflict'
            Tool = 'read_workspace_file_draft_conflict'
            Arguments = @{ path = 'existing.txt' }
        }
    )) {
        $workerCoordinatorBehavior = Clone-Object $transport
        $workerCoordinatorBehavior.trajectory.events += New-Call `
            $coordinatorBehavior.Tool `
            $coordinatorBehavior.Name `
            $coordinatorBehavior.Arguments `
            $true `
            'completed' `
            '22222222-2222-2222-2222-222222222222'
        Invoke-Control $coordinatorBehavior.Name ready-v2-transport $workerCoordinatorBehavior $false 'used coordinator-only' '.eval-grader/inventory.json'
    }

    $workerBootstrap = Clone-Object $transport
    $workerBootstrap.trajectory.events += New-Call powershell worker-bootstrap @{
        command = '. .\activate.ps1; dotnet run .\.github\skills\review-pull-request\scripts\prepare-review.cs -- --pr 1'
    } $true 'prepared' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-bootstrap ready-v2-transport $workerBootstrap $false 'shell tool' '.eval-grader/inventory.json'

    $workerTargetExecution = Clone-Object $transport
    $workerTargetExecution.trajectory.events += New-Call powershell worker-target-execution @{
        command = 'dotnet test'
    } $true 'passed' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-target-execution ready-v2-transport $workerTargetExecution $false 'shell tool' '.eval-grader/inventory.json'

    $workerCombinedOutput = Clone-Object $transport
    $workerCombinedOutput.trajectory.events += [pscustomobject]@{
        type = 'assistant_message'
        agentId = '22222222-2222-2222-2222-222222222222'
        data = [pscustomobject]@{
            content = "PATH: per-guide`nGUIDES: docs/CrossCuttingGuidance.md, docs/BlazorComponentsGuidance.md"
        }
    }
    Invoke-Control transport-worker-combined-output ready-v2-transport $workerCombinedOutput $false 'coordinator aggregation output' '.eval-grader/inventory.json'

    $workerIntermediateMessage = Clone-Object $transport
    $workerIntermediateMessage.trajectory.events += [pscustomobject]@{
        type = 'assistant_message'
        agentId = '22222222-2222-2222-2222-222222222222'
        data = [pscustomobject]@{
            content = 'Reading the assigned guide now.'
        }
    }
    Invoke-Control transport-worker-intermediate-message ready-v2-transport $workerIntermediateMessage $true 'valid role separation' '.eval-grader/inventory.json'

    $workerRunningReport = Clone-Object $transport
    $workerRunningReport.trajectory.events += New-Call read_agent worker-cross-running-report @{
        agent_id = '22222222-2222-2222-2222-222222222222'
        wait = $true
    } $true 'Agent is still running, status: running, total_turns: 0.'
    Invoke-Control transport-worker-running-report ready-v2-transport $workerRunningReport $true 'valid role separation' '.eval-grader/inventory.json'

    $workerProseGuide = Clone-Object $transport
    foreach ($toolCallId in @('worker-blazor-report', 'worker-cross-report')) {
        $workerReport = Get-ControlEventIndexes -ControlInput $workerProseGuide -ToolCallId $toolCallId
        $workerProseGuide.trajectory.events[$workerReport[1]].data.result =
            $workerProseGuide.trajectory.events[$workerReport[1]].data.result.Replace(
                'GUIDE: ',
                '**Guide:** '
            )
    }
    Invoke-Control transport-worker-prose-guide ready-v2-transport $workerProseGuide $true 'valid role separation' '.eval-grader/inventory.json'

    $workerReadAgentCombinedOutput = Clone-Object $transport
    $workerReport = Get-ControlEventIndexes -ControlInput $workerReadAgentCombinedOutput -ToolCallId 'worker-cross-report'
    $workerReadAgentCombinedOutput.trajectory.events[$workerReport[1]].data.result =
        'Agent is idle, status: idle, total_turns: 1. Turn 0: PATH: per-guide. GUIDES: docs/CrossCuttingGuidance.md, docs/BlazorComponentsGuidance.md'
    Invoke-Control transport-worker-read-agent-combined-output ready-v2-transport $workerReadAgentCombinedOutput $false 'coordinator aggregation output' '.eval-grader/inventory.json'

    $workerMultipleGuides = Clone-Object $transport
    $workerReport = Get-ControlEventIndexes -ControlInput $workerMultipleGuides -ToolCallId 'worker-cross-report'
    $workerMultipleGuides.trajectory.events[$workerReport[1]].data.result =
        "Agent is idle, status: idle, total_turns: 1. Turn 0: PATH: per-guide-worker`nGUIDE: docs/CrossCuttingGuidance.md`nGUIDE: docs/BlazorComponentsGuidance.md"
    Invoke-Control transport-worker-multiple-guides ready-v2-transport $workerMultipleGuides $false 'named 2 distinct guides' '.eval-grader/inventory.json'

    $workerWrongGuide = Clone-Object $transport
    $workerReport = Get-ControlEventIndexes -ControlInput $workerWrongGuide -ToolCallId 'worker-cross-report'
    $workerWrongGuide.trajectory.events[$workerReport[1]].data.result =
        "Agent is idle, status: idle, total_turns: 1. Turn 0: PATH: per-guide-worker`nGUIDE: docs/BlazorComponentsGuidance.md"
    Invoke-Control transport-worker-wrong-guide ready-v2-transport $workerWrongGuide $false 'instead of assigned guide' '.eval-grader/inventory.json'

    $workerCrossGuideRead = Clone-Object $transport
    $workerCrossGuideRead.trajectory.events += New-Call view worker-cross-guide-read @{
        path = '.review-bundle/guidance/docs/BlazorComponentsGuidance.md.source'
        view_range = @(1, -1)
    } $true 'L1:# Blazor components guidance' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-cross-guide-read ready-v2-transport $workerCrossGuideRead $false 'read unassigned guide' '.eval-grader/inventory.json'

    $workerCrossGuideSearch = Clone-Object $transport
    $workerCrossGuideSearch.trajectory.events += New-Call rg worker-cross-guide-search @{
        pattern = 'guidance'
        paths = '.review-bundle/guidance'
        output_mode = 'content'
    } $true 'match' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-cross-guide-search ready-v2-transport $workerCrossGuideSearch $false 'rg scope included unassigned guide' '.eval-grader/inventory.json'

    $missingCompletedWorkerReport = Clone-Object $transport
    $missingReportIndexes = Get-ControlEventIndexes -ControlInput $missingCompletedWorkerReport -ToolCallId 'worker-cross-report'
    $missingCompletedWorkerReport.trajectory.events = @(
        for ($index = 0; $index -lt $missingCompletedWorkerReport.trajectory.events.Count; $index++) {
            if ($index -notin $missingReportIndexes) {
                $missingCompletedWorkerReport.trajectory.events[$index]
            }
        }
    )
    Invoke-Control transport-worker-missing-completed-report ready-v2-transport $missingCompletedWorkerReport $false 'no completed collected report' '.eval-grader/inventory.json'

    $unboundWorker = Clone-Object $transport
    $unboundWorker.trajectory.events += New-Call view worker-unbound-read @{
        path = '.review-bundle/guidance/docs/BlazorComponentsGuidance.md.source'
        view_range = @(1, -1)
    } $true 'L1:# Blazor components guidance' '33333333-3333-3333-3333-333333333333'
    Invoke-Control transport-worker-unbound-id ready-v2-transport $unboundWorker $false 'unassigned worker' '.eval-grader/inventory.json'

    $missingWorkerPrompt = Clone-Object $transport
    $workerCall = Get-ControlEventIndexes -ControlInput $missingWorkerPrompt -ToolCallId 'worker'
    $missingWorkerPrompt.trajectory.events[$workerCall[0]].data.arguments.prompt = ''
    Invoke-Control transport-missing-worker-prompt ready-v2-transport $missingWorkerPrompt $false 'without its required inputs' '.eval-grader/inventory.json'

    $missingWorkerInput = Clone-Object $transport
    $workerCall = Get-ControlEventIndexes -ControlInput $missingWorkerInput -ToolCallId 'worker'
    $missingWorkerInput.trajectory.events[$workerCall[0]].data.arguments.prompt =
        $missingWorkerInput.trajectory.events[$workerCall[0]].data.arguments.prompt.Replace(
            'required/policies/0000.source',
            'required/policies/missing.source'
        )
    Invoke-Control transport-missing-worker-input ready-v2-transport $missingWorkerInput $false 'omitted applicable policy' '.eval-grader/inventory.json'

    foreach ($argument in @('agent_type', 'model', 'mode')) {
        $wrongWorkerConfig = Clone-Object $transport
        $workerCall = Get-ControlEventIndexes -ControlInput $wrongWorkerConfig -ToolCallId 'worker'
        $wrongWorkerConfig.trajectory.events[$workerCall[0]].data.arguments.$argument = 'wrong'
        Invoke-Control "transport-wrong-worker-$argument" ready-v2-transport $wrongWorkerConfig $false 'not configured' '.eval-grader/inventory.json'
    }

    $missingSecondWorker = Clone-Object $transport
    $secondWorker = Get-ControlEventIndexes -ControlInput $missingSecondWorker -ToolCallId 'worker-blazor'
    $missingSecondWorker.trajectory.events = @(
        for ($index = 0; $index -lt $missingSecondWorker.trajectory.events.Count; $index++) {
            if ($index -notin $secondWorker) {
                $missingSecondWorker.trajectory.events[$index]
            }
        }
    )
    Invoke-Control transport-missing-second-worker ready-v2-transport $missingSecondWorker $false 'Expected exactly 2' '.eval-grader/inventory.json'

    $overbroadSecondWorker = Clone-Object $transport
    $secondWorker = Get-ControlEventIndexes -ControlInput $overbroadSecondWorker -ToolCallId 'worker-blazor'
    $overbroadSecondWorker.trajectory.events[$secondWorker[0]].data.arguments.prompt +=
        "`nAlso assigned guide: guidance/docs/CrossCuttingGuidance.md.source"
    Invoke-Control transport-overbroad-second-worker ready-v2-transport $overbroadSecondWorker $false 'assigned guides instead of exactly one' '.eval-grader/inventory.json'

    $duplicateSecondWorker = Clone-Object $transport
    $firstWorker = Get-ControlEventIndexes -ControlInput $duplicateSecondWorker -ToolCallId 'worker'
    $secondWorker = Get-ControlEventIndexes -ControlInput $duplicateSecondWorker -ToolCallId 'worker-blazor'
    $duplicateSecondWorker.trajectory.events[$secondWorker[0]].data.arguments.prompt =
        $duplicateSecondWorker.trajectory.events[$firstWorker[0]].data.arguments.prompt
    Invoke-Control transport-duplicate-second-worker ready-v2-transport $duplicateSecondWorker $false 'assigned to more than one worker' '.eval-grader/inventory.json'

    $missingSecondPolicy = Clone-Object $transport
    $secondWorker = Get-ControlEventIndexes -ControlInput $missingSecondPolicy -ToolCallId 'worker-blazor'
    $missingSecondPolicy.trajectory.events[$secondWorker[0]].data.arguments.prompt =
        $missingSecondPolicy.trajectory.events[$secondWorker[0]].data.arguments.prompt.Replace(
            'required/policies/0001.source',
            'required/policies/missing.source'
        )
    Invoke-Control transport-missing-second-worker-policy ready-v2-transport $missingSecondPolicy $false 'omitted applicable policy' '.eval-grader/inventory.json'

    foreach ($argument in @('agent_type', 'model', 'mode')) {
        $wrongSecondWorker = Clone-Object $transport
        $secondWorker = Get-ControlEventIndexes -ControlInput $wrongSecondWorker -ToolCallId 'worker-blazor'
        $wrongSecondWorker.trajectory.events[$secondWorker[0]].data.arguments.$argument = 'wrong'
        Invoke-Control "transport-wrong-second-worker-$argument" ready-v2-transport $wrongSecondWorker $false 'not configured' '.eval-grader/inventory.json'
    }

    $missingSecondSharedInput = Clone-Object $transport
    $secondWorker = Get-ControlEventIndexes -ControlInput $missingSecondSharedInput -ToolCallId 'worker-blazor'
    $missingSecondSharedInput.trajectory.events[$secondWorker[0]].data.arguments.prompt =
        $missingSecondSharedInput.trajectory.events[$secondWorker[0]].data.arguments.prompt.Replace(
            'Full diff: diff.patch',
            'Full diff omitted.'
        )
    Invoke-Control transport-missing-second-shared-input ready-v2-transport $missingSecondSharedInput $false 'omitted required worker input' '.eval-grader/inventory.json'

        $transportManifest = Get-Content (
            Join-Path $testRoot '.review-bundle/manifest.json'
        ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
        $prohibitedSourceRoot = Clone-Object $transport
        $firstWorker = Get-ControlEventIndexes -ControlInput $prohibitedSourceRoot -ToolCallId 'worker'
        $headSourceRoot = [string]$transportManifest.sources.head.root
        $prohibitedSourceRoot.trajectory.events[$firstWorker[0]].data.arguments.prompt =
            $prohibitedSourceRoot.trajectory.events[$firstWorker[0]].data.arguments.prompt.Replace(
                "Head source root: $headSourceRoot",
                "Do not use source root: $headSourceRoot"
            )
        Invoke-Control transport-prohibited-source-root ready-v2-transport $prohibitedSourceRoot $false 'omitted frozen source root' '.eval-grader/inventory.json'

        $transportFiles = @(Get-Content (
            Join-Path $testRoot '.review-bundle/files.json'
        ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100)
        $prohibitedChangedSource = Clone-Object $transport
        $secondWorker = Get-ControlEventIndexes -ControlInput $prohibitedChangedSource -ToolCallId 'worker-blazor'
        $changedFilename = [string]$transportFiles[0].filename
        $prohibitedChangedSource.trajectory.events[$secondWorker[0]].data.arguments.prompt =
            $prohibitedChangedSource.trajectory.events[$secondWorker[0]].data.arguments.prompt.Replace(
                "Relevant source: $changedFilename",
                "Ignore relevant source: $changedFilename"
            )
        Invoke-Control transport-prohibited-changed-source ready-v2-transport $prohibitedChangedSource $false 'omitted relevant changed source' '.eval-grader/inventory.json'

        $nonApplicableSecondPolicy = Clone-Object $transport
        $secondWorker = Get-ControlEventIndexes -ControlInput $nonApplicableSecondPolicy -ToolCallId 'worker-blazor'
        $nonApplicableSecondPolicy.trajectory.events[$secondWorker[0]].data.arguments.prompt +=
            "`nAdditional policy: required/policies/0000.source"
    Invoke-Control transport-non-applicable-second-policy ready-v2-transport $nonApplicableSecondPolicy $false 'included non-applicable policy' '.eval-grader/inventory.json'

    $partialCoordinatorContentLeak = Clone-Object $transport
    $secondWorker = Get-ControlEventIndexes -ControlInput $partialCoordinatorContentLeak -ToolCallId 'worker-blazor'
    $pullBodyLines = @(Get-Content (
        Join-Path $testRoot '.review-bundle/required/pull/body.source'
    ) -Encoding utf8)
    $partialCoordinatorContentLeak.trajectory.events[$secondWorker[0]].data.arguments.prompt +=
        "`nEmbedded coordinator excerpt:`n$($pullBodyLines[0])"
    Invoke-Control transport-partial-coordinator-content-leak ready-v2-transport $partialCoordinatorContentLeak $false 'embedded or requested coordinator-only' '.eval-grader/inventory.json'

    $coordinatorPathRequest = Clone-Object $transport
    $secondWorker = Get-ControlEventIndexes -ControlInput $coordinatorPathRequest -ToolCallId 'worker-blazor'
    $coordinatorPathRequest.trajectory.events[$secondWorker[0]].data.arguments.prompt =
        $coordinatorPathRequest.trajectory.events[$secondWorker[0]].data.arguments.prompt.Replace(
            'Do not read pull.json; the coordinator owns that input.',
            'Pull metadata input: pull.json'
        )
    Invoke-Control transport-coordinator-path-request ready-v2-transport $coordinatorPathRequest $false 'embedded or requested coordinator-only' '.eval-grader/inventory.json'

    $mixedCoordinatorPathRequest = Clone-Object $transport
    $secondWorker = Get-ControlEventIndexes -ControlInput $mixedCoordinatorPathRequest -ToolCallId 'worker-blazor'
    $mixedCoordinatorPathRequest.trajectory.events[$secondWorker[0]].data.arguments.prompt +=
        "`nDo not publish feedback; Pull body input: required/pull/body.source"
    Invoke-Control transport-mixed-coordinator-path-request ready-v2-transport $mixedCoordinatorPathRequest $false 'embedded or requested coordinator-only' '.eval-grader/inventory.json'

    $extraWorker = Clone-Object $transport
    $workerCall = Get-ControlEventIndexes -ControlInput $extraWorker -ToolCallId 'worker'
    $workerArguments = $extraWorker.trajectory.events[$workerCall[0]].data.arguments
    $extraWorker.trajectory.events += New-Call task extra-worker @{
        agent_type = [string]$workerArguments.agent_type
        model = [string]$workerArguments.model
        mode = [string]$workerArguments.mode
        prompt = [string]$workerArguments.prompt
    } $true 'Agent started in background with agent_id: 22222222-2222-2222-2222-222222222222.'
    Invoke-Control transport-extra-worker ready-v2-transport $extraWorker $false 'Expected exactly 2' '.eval-grader/inventory.json'

    $oneCompleteRead = Clone-Object $transport
    $bodyA = Get-ControlEventIndexes -ControlInput $oneCompleteRead -ToolCallId 'read-required-pull-body-source-a'
    $bodyB = Get-ControlEventIndexes -ControlInput $oneCompleteRead -ToolCallId 'read-required-pull-body-source-b'
    $bodyLines = Get-Content (Join-Path $testRoot '.review-bundle/required/pull/body.source') -Encoding utf8
    $oneCompleteRead.trajectory.events[$bodyA[0]].data.arguments.view_range = @(1, -1)
    $oneCompleteRead.trajectory.events[$bodyA[1]].data.result = @(
        for ($line = 1; $line -le $bodyLines.Count; $line++) {
            "L${line}:$($bodyLines[$line - 1])"
        }
    ) -join "`n"
    $oneCompleteRead.trajectory.events = @(
        for ($index = 0; $index -lt $oneCompleteRead.trajectory.events.Count; $index++) {
            if ($index -notin $bodyB) {
                $oneCompleteRead.trajectory.events[$index]
            }
        }
    )
    Invoke-Control transport-one-complete-read ready-v2-transport $oneCompleteRead $true 'exact coordinator reads' '.eval-grader/inventory.json'

    $returnedGap = Clone-Object $transport
    $filesRead = Get-ControlEventIndexes -ControlInput $returnedGap -ToolCallId 'read-files-json'
    $returned = @($returnedGap.trajectory.events[$filesRead[1]].data.result -split "`n")
    $returnedGap.trajectory.events[$filesRead[1]].data.result = @(
        $returned[0..4] + $returned[6..($returned.Count - 1)]
    ) -join "`n"
    Invoke-Control transport-returned-line-gap ready-v2-transport $returnedGap $false 'actual returned-line gap' '.eval-grader/inventory.json'

    $wrongLine = Clone-Object $transport
    $wrongRead = Get-ControlEventIndexes -ControlInput $wrongLine -ToolCallId 'read-files-json'
    $wrongLine.trajectory.events[$wrongRead[1]].data.result =
        $wrongLine.trajectory.events[$wrongRead[1]].data.result.Replace(
            'L1:[',
            'L1:{'
        )
    Invoke-Control transport-wrong-returned-line ready-v2-transport $wrongLine $false 'different from trusted fixture line' '.eval-grader/inventory.json'

    $ordinaryWord = Clone-Object $transport
    $ordinaryRead = Get-ControlEventIndexes -ControlInput $ordinaryWord -ToolCallId 'read-files-json'
    $ordinaryWord.trajectory.events[$ordinaryRead[1]].data.result +=
        "`nA review note mentioned truncation without a tool notice."
    Invoke-Control transport-ordinary-truncation-word ready-v2-transport $ordinaryWord $true 'exact coordinator reads' '.eval-grader/inventory.json'

    $explicitTruncation = Clone-Object $transport
    $truncatedRead = Get-ControlEventIndexes -ControlInput $explicitTruncation -ToolCallId 'read-files-json'
    $explicitTruncation.trajectory.events[$truncatedRead[1]].data.result +=
        "`n[Output truncated. Use view_range to continue.]"
    Invoke-Control transport-explicit-truncation ready-v2-transport $explicitTruncation $false 'actual returned-line gap' '.eval-grader/inventory.json'

    $wrongRole = Clone-Object $transport
    $pullRead = Get-ControlEventIndexes -ControlInput $wrongRole -ToolCallId 'read-pull-json'
    foreach ($index in $pullRead) {
        $wrongRole.trajectory.events[$index].agentId =
            '44444444-4444-4444-4444-444444444444'
    }
    Invoke-Control transport-worker-coordinator-only ready-v2-transport $wrongRole $false 'worker read coordinator-only' '.eval-grader/inventory.json'

    $wrongRoleReadFile = Clone-Object $transport
    $wrongRoleReadFile.trajectory.events += New-Call read_file worker-read-manifest @{
        filePath = '.review-bundle/manifest.json'
        startLine = 1
        endLine = 20
    } $true 'manifest bytes' '44444444-4444-4444-4444-444444444444'
    Invoke-Control transport-worker-read-file-coordinator-only ready-v2-transport $wrongRoleReadFile $false 'worker read coordinator-only' '.eval-grader/inventory.json'

    $workerRgManifest = Clone-Object $transport
    $workerRgManifest.trajectory.events += New-Call rg worker-rg-manifest @{
        pattern = 'ready'
        paths = '.review-bundle/manifest.json'
        output_mode = 'content'
    } $true '3:  "ready": true' '44444444-4444-4444-4444-444444444444'
    Invoke-Control transport-worker-rg-coordinator-only ready-v2-transport $workerRgManifest $false 'worker rg scope' '.eval-grader/inventory.json'

    $workerRgBundle = Clone-Object $transport
    $workerRgBundle.trajectory.events += New-Call rg worker-rg-bundle @{
        pattern = 'ready'
        paths = '.review-bundle'
        output_mode = 'content'
    } $true 'manifest.json:3:  "ready": true' '44444444-4444-4444-4444-444444444444'
    Invoke-Control transport-worker-rg-broad-scope ready-v2-transport $workerRgBundle $false 'worker rg scope' '.eval-grader/inventory.json'

    $workerRgDefault = Clone-Object $transport
    $workerRgDefault.trajectory.events += New-Call rg worker-rg-default @{
        pattern = 'ready'
        output_mode = 'content'
    } $true '.review-bundle/manifest.json:3:  "ready": true' '44444444-4444-4444-4444-444444444444'
    Invoke-Control transport-worker-rg-default-scope ready-v2-transport $workerRgDefault $false 'worker rg scope' '.eval-grader/inventory.json'

    $workerRgEmpty = Clone-Object $transport
    $workerRgEmpty.trajectory.events += New-Call rg worker-rg-empty @{
        pattern = 'ready'
        paths = @()
        output_mode = 'content'
    } $true '.review-bundle/manifest.json:3:  "ready": true' '44444444-4444-4444-4444-444444444444'
    Invoke-Control transport-worker-rg-empty-scope ready-v2-transport $workerRgEmpty $false 'worker rg scope' '.eval-grader/inventory.json'

    $workerRgShared = Clone-Object $transport
    $workerRgShared.trajectory.events += New-Call rg worker-rg-shared @{
        pattern = 'Value'
        paths = '.review-bundle/diff.patch'
        output_mode = 'content'
    } $true 'diff --git a/src/Value.cs b/src/Value.cs' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-rg-shared-scope ready-v2-transport $workerRgShared $true 'valid role separation' '.eval-grader/inventory.json'

    $workerDoesNotCredit = Clone-Object $transport
    $diffRead = Get-ControlEventIndexes -ControlInput $workerDoesNotCredit -ToolCallId 'read-diff-patch'
    foreach ($index in $diffRead) {
        $workerDoesNotCredit.trajectory.events[$index].agentId =
            '22222222-2222-2222-2222-222222222222'
    }
    Invoke-Control transport-worker-shared-no-credit ready-v2-transport $workerDoesNotCredit $false 'No coordinator native view call' '.eval-grader/inventory.json'

    $sharedWorkerDuplicate = Clone-Object $transport
    $diffLines = Get-Content (Join-Path $testRoot '.review-bundle/diff.patch') -Encoding utf8
    $sharedWorkerDuplicate.trajectory.events += New-Call view worker-shared-diff @{
        path = '.review-bundle/diff.patch'
        view_range = @(1, -1)
    } $true (@(
        for ($line = 1; $line -le $diffLines.Count; $line++) {
            "L${line}:$($diffLines[$line - 1])"
        }
    ) -join "`n") '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-shared-worker-input ready-v2-transport $sharedWorkerDuplicate $true 'valid role separation' '.eval-grader/inventory.json'

    $lateManifest = Clone-Object $transport
    $manifestRead = Get-ControlEventIndexes -ControlInput $lateManifest -ToolCallId 'read-manifest-json'
    $manifestEvents = @($lateManifest.trajectory.events[$manifestRead])
    $lateManifest.trajectory.events = @(
        for ($index = 0; $index -lt $lateManifest.trajectory.events.Count; $index++) {
            if ($index -notin $manifestRead) {
                $lateManifest.trajectory.events[$index]
            }
        }
    ) + $manifestEvents
    Invoke-Control transport-late-manifest ready-v2-transport $lateManifest $false 'before routing the guide worker' '.eval-grader/inventory.json'

    $readAfterSynthesis = Clone-Object $transport
    $latePullRead = Get-ControlEventIndexes -ControlInput $readAfterSynthesis -ToolCallId 'read-pull-json'
    $latePullEvents = @($readAfterSynthesis.trajectory.events[$latePullRead])
    $readAfterSynthesis.trajectory.events = @(
        for ($index = 0; $index -lt $readAfterSynthesis.trajectory.events.Count; $index++) {
            if ($index -notin $latePullRead) {
                $readAfterSynthesis.trajectory.events[$index]
            }
        }
    ) + [pscustomobject]@{
        type = 'assistant_message'
        data = [pscustomobject]@{ content = 'STATUS: NO_FINDINGS' }
    } + $latePullEvents
    Invoke-Control transport-read-after-synthesis ready-v2-transport $readAfterSynthesis $false 'after root synthesis' '.eval-grader/inventory.json'

    $shellFallback = Clone-Object $transport
    $shellFallback.trajectory.events = @(
        New-Call powershell shell @{ command = 'Get-Content .review-bundle/files.json' } $true 'content'
    ) + @($shellFallback.trajectory.events)
    Invoke-Control transport-shell ready-v2-transport $shellFallback $false 'shell tool' '.eval-grader/inventory.json'

    [IO.File]::WriteAllText(
        (Join-Path $testRoot '.review-bundle/untrusted-extra.source'),
        "extra`n",
        $utf8
    )
    Invoke-Control transport-required-scope-failure ready-v2-transport $transport $false 'fixture validation failed' '.eval-grader/inventory.json'
    Invoke-Control lifecycle-required-scope-failure same-worker-lifecycle $retainedLifecycle $false 'fixture validation failed' '.eval-grader/inventory.json'
    Remove-Item (Join-Path $testRoot '.review-bundle/untrusted-extra.source') -Force

    Remove-Item (Join-Path $testRoot '.review-bundle/feedback.json') -Force
    $blockedManifestLines = Get-Content (
        Join-Path $testRoot '.review-bundle/manifest.json'
    ) -Encoding utf8
    $blockedManifestResult = @(
        for ($line = 1; $line -le $blockedManifestLines.Count; $line++) {
            "L${line}:$($blockedManifestLines[$line - 1])"
        }
    ) -join "`n"
    $blocked = [pscustomobject]@{
        trajectory = [pscustomobject]@{
            workDir = $testRoot
            output = 'STATUS: BLOCKED'
            events = @(
                New-Call view blocked-manifest @{
                    path = '.review-bundle/manifest.json'
                    view_range = @(1, -1)
                } $true $blockedManifestResult
                New-Call view missing-feedback @{
                    path = '.review-bundle/feedback.json'
                    view_range = @(1, 20)
                } $false 'File not found.'
                [pscustomobject]@{
                    type = 'assistant_message'
                    data = [pscustomobject]@{ content = 'STATUS: BLOCKED' }
                }
            )
        }
    }
    $blocked.trajectory.events = @($blocked.trajectory.events | ForEach-Object { $_ })
    Invoke-Control missing-bytes-blocked required-bytes-blocked $blocked $true 'BLOCKED result' '.eval-grader/inventory.json'

    $errorBlocked = Clone-Object $blocked
    $errorBlocked.trajectory.events[3].data.result =
        'Native view error: access to the required feedback input was denied.'
    Invoke-Control missing-bytes-error-blocked required-bytes-blocked $errorBlocked $true 'BLOCKED result' '.eval-grader/inventory.json'

    $partialManifest = Clone-Object $blocked
    $firstManifestResult = @(
        for ($line = 1; $line -le 20; $line++) {
            "L${line}:$($blockedManifestLines[$line - 1])"
        }
    ) -join "`n"
    $remainingManifestResult = @(
        for ($line = 21; $line -le $blockedManifestLines.Count; $line++) {
            "L${line}:$($blockedManifestLines[$line - 1])"
        }
    ) -join "`n"
    $partialManifest.trajectory.events = @(
        New-Call view blocked-manifest-first @{
            path = '.review-bundle/manifest.json'
            view_range = @(1, 20)
        } $true $firstManifestResult
        $partialManifest.trajectory.events[2..3]
        New-Call view blocked-manifest-rest @{
            path = '.review-bundle/manifest.json'
            view_range = @(21, -1)
        } $true $remainingManifestResult
        $partialManifest.trajectory.events[4]
    )
    Invoke-Control missing-bytes-partial-manifest required-bytes-blocked $partialManifest $false 'before the ready manifest' '.eval-grader/inventory.json'

    $feedbackBeforeManifest = Clone-Object $blocked
    $feedbackBeforeManifest.trajectory.events = @(
        $feedbackBeforeManifest.trajectory.events[2..3] +
        $feedbackBeforeManifest.trajectory.events[0..1] +
        $feedbackBeforeManifest.trajectory.events[4]
    )
    Invoke-Control missing-bytes-before-manifest required-bytes-blocked $feedbackBeforeManifest $false 'before the ready manifest' '.eval-grader/inventory.json'

    $successFallback = Clone-Object $blocked
    $successFallback.trajectory.output = 'STATUS: NO_FINDINGS'
    Invoke-Control missing-bytes-success required-bytes-blocked $successFallback $false 'STATUS: BLOCKED' '.eval-grader/inventory.json'

    $blockedWorker = Clone-Object $blocked
    $blockedWorker.trajectory.events += New-Call task worker @{
        agent_type = 'general-purpose'
        model = 'gpt-5.6-sol'
        mode = 'background'
    } $true 'Agent started in background with agent_id: 33333333-3333-3333-3333-333333333333.'
    Invoke-Control missing-bytes-worker required-bytes-blocked $blockedWorker $false 'worker was dispatched' '.eval-grader/inventory.json'

    $blockedThenSuccess = Clone-Object $blocked
    $blockedThenSuccess.trajectory.events += New-Call view feedback-success @{
        path = '.review-bundle/feedback.json'
        view_range = @(1, 20)
    } $true 'L1:{}'
    Invoke-Control missing-bytes-later-success required-bytes-blocked $blockedThenSuccess $false 'successfully read' '.eval-grader/inventory.json'

    [IO.File]::WriteAllText(
        (Join-Path $testRoot '.review-bundle/untrusted-extra.source'),
        "extra`n",
        $utf8
    )
    Invoke-Control missing-bytes-required-scope-failure required-bytes-blocked $blocked $false 'fixture validation failed' '.eval-grader/inventory.json'
    Remove-Item (Join-Path $testRoot '.review-bundle/untrusted-extra.source') -Force

    Write-Host 'Trace-contract grader controls passed.'
} finally {
    Remove-Item $testRoot -Recurse -Force
}
