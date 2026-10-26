#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$grader = Join-Path $PSScriptRoot 'trace-contract-grader.ps1'
$controlPath = Join-Path $PSScriptRoot 'review-pull-request/fixtures/controls/retained-sdk-lifecycle.json'
$bundleRoot = Join-Path $PSScriptRoot 'review-pull-request/fixtures/bundle'
$contractSource = Join-Path $PSScriptRoot 'review-pull-request/fixtures/contracts/pagination.json'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-review-trace-grader-test-' + [guid]::NewGuid().ToString('N')
)
$utf8 = [Text.UTF8Encoding]::new($false)

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

function New-PaginationInput {
    $events = [Collections.Generic.List[object]]::new()
    foreach ($event in New-Call view manifest @{ path = '.review-bundle/manifest.json' } $true '{"version":2,"ready":true}') {
        $events.Add($event)
    }
    foreach ($event in New-Call view routing @{ path = '.review-bundle/guidance/.github/skills/review-pull-request/routing.md.source' } $true '| * | docs/CrossCuttingGuidance.md |') {
        $events.Add($event)
    }

    $contract = Get-Content $contractSource -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    foreach ($file in $contract.files) {
        $firstEnd = [Math]::Min(600, [int]$file.lineCount)
        $secondEnd = [Math]::Min(1200, [int]$file.lineCount)
        $ranges = @(
            @(1, $firstEnd),
            @(($firstEnd + 1), $secondEnd),
            @(($secondEnd + 1), -1)
        )
        for ($index = 0; $index -lt $ranges.Count; $index++) {
            $range = $ranges[$index]
            if ($range[0] -gt [int]$file.lineCount) {
                continue
            }
            $lines = Get-Content (Join-Path $testRoot ".review-bundle/$($file.path)") -Encoding utf8
            $lastLine = if ($range[1] -eq -1) { $lines.Count } else { $range[1] }
            $result = for ($line = $range[0]; $line -le $lastLine; $line++) {
                "L${line}:$($lines[$line - 1])"
            }
            $result = $result -join "`n"
            foreach ($event in New-Call view "$($file.path)-$index" @{
                path = ".review-bundle/$($file.path)"
                view_range = $range
            } $true $result) {
                $events.Add($event)
            }
        }
    }
    foreach ($event in New-Call task worker @{
        agent_type = 'general-purpose'
        model = 'gpt-5.6-sol'
        mode = 'background'
    } $true 'Agent started in background with agent_id: 11111111-1111-1111-1111-111111111111.') {
        $events.Add($event)
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
    Copy-Item $contractSource (Join-Path $testRoot '.eval-grader/pagination.json')

    $retained = Get-Content $controlPath -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    $retainedLifecycle = Clone-Object $retained.input
    $retainedLifecycle.trajectory.events = @($retainedLifecycle.trajectory.events[6..14])
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

    Invoke-Control retained-lifecycle same-worker-lifecycle $retainedLifecycle $true 'same ID' ''

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
    Invoke-Control lifecycle-sync same-worker-lifecycle $syncWorker $false 'not dispatched as' ''

    $wrongWorker = Clone-Object $retainedLifecycle
    $wrongWorker.trajectory.events[4].data.arguments.agent_id = '22222222-2222-2222-2222-222222222222'
    Invoke-Control lifecycle-wrong-id same-worker-lifecycle $wrongWorker $false 'other than' ''

    $missingInitialReport = Clone-Object $retainedLifecycle
    $missingInitialReport.trajectory.events[3].data.result = 'Agent is running.'
    Invoke-Control lifecycle-missing-initial same-worker-lifecycle $missingInitialReport $false 'initial report collection' ''

    $missingFollowUp = Clone-Object $retainedLifecycle
    $missingFollowUp.trajectory.events[7].data.success = $false
    Invoke-Control lifecycle-missing-follow-up same-worker-lifecycle $missingFollowUp $false 'did not complete successfully' ''

    $identicalFollowUp = Clone-Object $retainedLifecycle
    $identicalFollowUp.trajectory.events[7].data.result =
        $identicalFollowUp.trajectory.events[3].data.result
    Invoke-Control lifecycle-identical-follow-up same-worker-lifecycle $identicalFollowUp $false 'actual completed worker follow-up' ''

    $idleOnly = Clone-Object $retainedLifecycle
    $idleOnly.trajectory.events[3].data.result =
        'Agent is idle, status: idle, total_turns: 1. No report content returned.'
    Invoke-Control lifecycle-idle-only same-worker-lifecycle $idleOnly $false 'actual completed worker report' ''

    $pathOnly = Clone-Object $retainedLifecycle
    $pathOnly.trajectory.events[3].data.result =
        'Agent is idle, status: idle, total_turns: 1. Turn 0: PATH: per-guide-worker.'
    $pathOnly.trajectory.events[7].data.result =
        'Agent is idle, status: idle, total_turns: 2. Turn 1: PATH: per-guide-worker.'
    Invoke-Control lifecycle-path-only same-worker-lifecycle $pathOnly $false 'actual completed worker report' ''

    $completedStatusWords = Clone-Object $retainedLifecycle
    $completedStatusWords.trajectory.events[3].data.result +=
        ' Running target code remained excluded; no required check failed or was cancelled.'
    $completedStatusWords.trajectory.events[7].data.result +=
        ' The in-progress candidate remained unresolved.'
    Invoke-Control lifecycle-completed-status-words same-worker-lifecycle $completedStatusWords $true 'same ID' ''

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
    Invoke-Control lifecycle-running-retries same-worker-lifecycle $retryLifecycle $true 'same ID' ''

    $runningFinal = Clone-Object $retainedLifecycle
    $runningFinal.trajectory.events[7].data.result =
        'Agent is still running, status: running, total_turns: 2. Turn 1: PATH: per-guide-worker. Clarification complete enough to report a candidate.'
    Invoke-Control lifecycle-running-final same-worker-lifecycle $runningFinal $false 'did not reach a completed worker state' ''

    $nonWaitRead = Clone-Object $retainedLifecycle
    $nonWaitRead.trajectory.events[2].data.arguments.wait = $false
    Invoke-Control lifecycle-nonwait-read same-worker-lifecycle $nonWaitRead $false 'wait:true' ''

    $pagination = New-PaginationInput
    Invoke-Control pagination-positive hosted-pagination $pagination $true 'gap-free' '.eval-grader/pagination.json'

    $sizeWarning = Clone-Object $pagination
    $sizeWarning.trajectory.events[5].data.result = 'File too large to read at once.'
    Invoke-Control pagination-size-warning hosted-pagination $sizeWarning $false 'actual returned-line gap' '.eval-grader/pagination.json'

    $rangeGap = Clone-Object $pagination
    $rangeGap.trajectory.events[6].data.arguments.view_range[0] = 602
    Invoke-Control pagination-range-gap hosted-pagination $rangeGap $true 'gap-free' '.eval-grader/pagination.json'

    $returnedGap = Clone-Object $pagination
    $returnedGap.trajectory.events[5].data.result =
        (($returnedGap.trajectory.events[5].data.result -split "`n")[0..598] -join "`n")
    Invoke-Control pagination-returned-gap hosted-pagination $returnedGap $false 'actual returned-line gap' '.eval-grader/pagination.json'

    $truncatedLine = Clone-Object $pagination
    $truncatedLine.trajectory.events[5].data.result =
        $truncatedLine.trajectory.events[5].data.result +
        "`n[Output truncated. Use view_range=[601, ...] to continue reading.]"
    Invoke-Control pagination-truncated-line hosted-pagination $truncatedLine $false 'actual returned-line gap' '.eval-grader/pagination.json'

    $missingSentinel = Clone-Object $pagination
    $missingSentinel.trajectory.events[5].data.result =
        $missingSentinel.trajectory.events[5].data.result.Replace(
            'FILES_SENTINEL_0000',
            'REMOVED_SENTINEL'
        )
    Invoke-Control pagination-missing-sentinel hosted-pagination $missingSentinel $false 'omitted sentinel' '.eval-grader/pagination.json'

    $shellFallback = Clone-Object $pagination
    $shellFallback.trajectory.events = @(
        New-Call powershell shell @{ command = 'Get-Content .review-bundle/files.json' } $true 'content'
    ) + @($shellFallback.trajectory.events)
    Invoke-Control pagination-shell hosted-pagination $shellFallback $false 'shell tool' '.eval-grader/pagination.json'

    $forcedRead = Clone-Object $pagination
    $forcedRead.trajectory.events[4].data.arguments |
        Add-Member -NotePropertyName forceReadLargeFiles -NotePropertyValue $true
    Invoke-Control pagination-force-read hosted-pagination $forcedRead $false 'forced whole-file read' '.eval-grader/pagination.json'

    $prematureWorker = Clone-Object $pagination
    $taskEvents = @($prematureWorker.trajectory.events[-2], $prematureWorker.trajectory.events[-1])
    $prematureWorker.trajectory.events =
        $taskEvents + @($prematureWorker.trajectory.events[0..($prematureWorker.trajectory.events.Count - 3)])
    Invoke-Control pagination-premature-worker hosted-pagination $prematureWorker $false 'No native view call' '.eval-grader/pagination.json'

    $workerRecovery = Clone-Object $pagination
    $workerPair = @(
        $workerRecovery.trajectory.events[6],
        $workerRecovery.trajectory.events[7]
    )
    $workerPair[0].agentId = '44444444-4444-4444-4444-444444444444'
    $workerPair[1].agentId = '44444444-4444-4444-4444-444444444444'
    $workerRecovery.trajectory.events = @(
        $workerRecovery.trajectory.events[0..5] +
        $workerRecovery.trajectory.events[8..($workerRecovery.trajectory.events.Count - 1)] +
        $workerPair
    )
    Invoke-Control pagination-worker-recovery hosted-pagination $workerRecovery $false 'actual returned-line gap' '.eval-grader/pagination.json'

    $postDispatchRead = Clone-Object $pagination
    $postDispatchRead.trajectory.events += New-Call view post-dispatch @{
        path = '.review-bundle/feedback.json'
        view_range = @(1, 20)
    } $true 'L1:{'
    Invoke-Control pagination-post-dispatch-read hosted-pagination $postDispatchRead $true 'gap-free' '.eval-grader/pagination.json'

    Remove-Item (Join-Path $testRoot '.review-bundle/feedback.json') -Force
    $blocked = [pscustomobject]@{
        trajectory = [pscustomobject]@{
            workDir = $testRoot
            output = 'STATUS: BLOCKED'
            events = @(
                New-Call view blocked-manifest @{
                    path = '.review-bundle/manifest.json'
                    view_range = @(1, 120)
                } $true "L1:{`nL2:  `"version`": 2,`nL3:  `"ready`": true"
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
    Invoke-Control missing-bytes-blocked required-bytes-blocked $blocked $true 'BLOCKED result' ''

    $feedbackBeforeManifest = Clone-Object $blocked
    $feedbackBeforeManifest.trajectory.events = @(
        $feedbackBeforeManifest.trajectory.events[2..3] +
        $feedbackBeforeManifest.trajectory.events[0..1] +
        $feedbackBeforeManifest.trajectory.events[4]
    )
    Invoke-Control missing-bytes-before-manifest required-bytes-blocked $feedbackBeforeManifest $false 'before the ready manifest' ''

    $successFallback = Clone-Object $blocked
    $successFallback.trajectory.output = 'STATUS: NO_FINDINGS'
    Invoke-Control missing-bytes-success required-bytes-blocked $successFallback $false 'STATUS: BLOCKED' ''

    $blockedWorker = Clone-Object $blocked
    $blockedWorker.trajectory.events += New-Call task worker @{
        agent_type = 'general-purpose'
        model = 'gpt-5.6-sol'
        mode = 'background'
    } $true 'Agent started in background with agent_id: 33333333-3333-3333-3333-333333333333.'
    Invoke-Control missing-bytes-worker required-bytes-blocked $blockedWorker $false 'worker was dispatched' ''

    $blockedThenSuccess = Clone-Object $blocked
    $blockedThenSuccess.trajectory.events += New-Call view feedback-success @{
        path = '.review-bundle/feedback.json'
        view_range = @(1, 20)
    } $true 'L1:{}'
    Invoke-Control missing-bytes-later-success required-bytes-blocked $blockedThenSuccess $false 'successfully read' ''

    Write-Host 'Trace-contract grader controls passed.'
} finally {
    Remove-Item $testRoot -Recurse -Force
}
