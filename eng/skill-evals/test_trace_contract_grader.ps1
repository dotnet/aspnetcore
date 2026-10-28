#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$grader = Join-Path $PSScriptRoot 'trace-contract-grader.ps1'
$controlPath = Join-Path $PSScriptRoot 'review-pull-request/fixtures/controls/retained-sdk-lifecycle.json'
$nativeControlRoot = Join-Path $PSScriptRoot 'review-pull-request/fixtures/controls'
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
        [object]$Guide,
        [string]$BundleRoot
    )

    $prompt = @(
        'Review the assigned projected transport inputs.'
        $workerRoleBoundary
        "Trusted bundle root: $BundleRoot"
        "Frozen PR: $($Manifest.target.pr)"
        "Frozen head: $($Manifest.target.head)"
        "Frozen merge base: $($Manifest.target.mergeBase)"
        "Frozen base tip: $($Manifest.target.baseTip)"
        'Verbatim assignment:'
        ($Guide.assignment | ConvertTo-Json -Compress -Depth 20)
        @($Guide.assignment.sourceRoles | ForEach-Object {
            "Source role $_ root: $($Manifest.sources.$_.root)"
        })
        'Do not read pull.json; the coordinator owns that input.'
    )
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
            -Guide $manifest.guides[1] `
            -BundleRoot (Join-Path $testRoot '.review-bundle')
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
            -Guide $manifest.guides[0] `
            -BundleRoot (Join-Path $testRoot '.review-bundle')
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
    $events.Add([pscustomobject]@{
        type = 'assistant_message'
        data = [pscustomobject]@{ content = 'STATUS: NO_FINDINGS' }
    })
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

    foreach ($nativeControl in @(
        [pscustomobject]@{
            Arm = 'g'
            EventsSha256 = 'f9360ba3885704439ff3061f5f29938b59f4617865f4fc5cd3a83b888b4423a6'
            EventsBytes = 7175367
            ControlSha256 = '5e8e7eb7ddd97fd2a1d676b1ac35090173fd5d9b9e785bc4fc1590c81d09f943'
            ExpectedPassed = $true
            ExpectedEvidence = 'frozen candidate'
        },
        [pscustomobject]@{
            Arm = 's'
            EventsSha256 = 'e3e188372ab4e28beda75cfe269ad0e0ede70a07499e1c78bcd738670105b955'
            EventsBytes = 9380832
            ControlSha256 = '00dbbb486f4f54df351451a3c7c34f74b8115853c3c7075ef0bd0718a36a3491'
            ExpectedPassed = $true
            ExpectedEvidence = 'frozen candidate'
        },
        [pscustomobject]@{
            Arm = 'o'
            EventsSha256 = '18a392fa1ab285632453cf5b4061fc5111a7cd546caa02c4e2f090f9f6deea1a'
            EventsBytes = 2961856
            ControlSha256 = '721f06df41e891b4af75dfc1227a4d3c8b186ec8122bdcb6d1e81592c5b1ea0b'
            ExpectedPassed = $false
            ExpectedEvidence = 'intermediate output derived from'
        }
    )) {
        $path = Join-Path $nativeControlRoot (
            "retained-native-$($nativeControl.Arm)-scope.json"
        )
        $control = Get-Content $path -Raw -Encoding utf8 |
            ConvertFrom-Json -Depth 100
        if ([string]$control.sourceEvidence.retainedEventsSha256 -cne
            $nativeControl.EventsSha256 -or
            [int64]$control.sourceEvidence.retainedEventsBytes -ne
            $nativeControl.EventsBytes -or
            [string]$control.sourceEvidence.arm -cne
            $nativeControl.Arm.ToUpperInvariant() -or
            [string]$control.sourceEvidence.candidateRevision -cne
            '374fde1927b759db52996bb1074aa7473a543b12' -or
            [string]$control.sourceEvidence.trustedRevision -cne
            'a888f052c880760ddd6df5de8d98d4051622d546' -or
            (Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne
            $nativeControl.ControlSha256 -or
            (@($control.sourceEvidence.eventIds) -join "`n") -cne
            (@($control.trajectory.events.id) -join "`n") -or
            @($control.sourceEvidence.eventIds | Sort-Object -Unique).Count -ne
            @($control.sourceEvidence.eventIds).Count) {
            throw "Retained native $($nativeControl.Arm.ToUpperInvariant()) evidence binding changed."
        }
        Invoke-Control `
            "native-retained-$($nativeControl.Arm)" `
            native-worker-scope `
            $control `
            $nativeControl.ExpectedPassed `
            $nativeControl.ExpectedEvidence `
            ''
    }

    $nativeG = Get-Content (
        Join-Path $nativeControlRoot 'retained-native-g-scope.json'
    ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    $nativeUnknownEvent = Clone-Object $nativeG
    $nativeUnknownEvent.trajectory.events[0].type = 'tool.execution_changed'
    Invoke-Control native-unknown-event native-worker-scope $nativeUnknownEvent $false 'unsupported native event type' ''

    $nativeMalformedTool = Clone-Object $nativeG
    $nativeMalformedTool.trajectory.events[0].data.PSObject.Properties.Remove(
        'toolName'
    )
    Invoke-Control native-malformed-tool native-worker-scope $nativeMalformedTool $false 'missing its tool call ID or tool name' ''

    $nativeMissingWorker = Clone-Object $nativeG
    $nativeWorkerId = [string]$nativeMissingWorker.nativeScope.workers[0].agentId
    $nativeMissingWorker.trajectory.events = @(
        $nativeMissingWorker.trajectory.events |
            Where-Object { [string]$_.agentId -cne $nativeWorkerId }
    )
    Invoke-Control native-missing-worker native-worker-scope $nativeMissingWorker $false 'did not appear in the retained tool events' ''

    $nativeMissingAgentId = Clone-Object $nativeG
    foreach ($event in $nativeMissingAgentId.trajectory.events) {
        if ([string]$event.agentId -ceq $nativeWorkerId) {
            $event.agentId = $null
        }
    }
    Invoke-Control native-missing-agent-id native-worker-scope $nativeMissingAgentId $false 'missing worker attribution' ''

    $nativeWorkerSkillMissingAgentId = Get-Content (
        Join-Path $nativeControlRoot 'retained-native-s-scope.json'
    ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    foreach ($event in $nativeWorkerSkillMissingAgentId.trajectory.events |
        Where-Object { [string]$_.data.toolCallId -ceq 'call_o8gPTcoTOUqIvlHS5tjy2YVy' }) {
        $event.agentId = $null
    }
    Invoke-Control native-worker-skill-missing-agent-id native-worker-scope $nativeWorkerSkillMissingAgentId $false 'missing worker attribution' ''

    $nativePartialAttribution = Get-Content (
        Join-Path $nativeControlRoot 'retained-native-o-scope.json'
    ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    foreach ($event in $nativePartialAttribution.trajectory.events) {
        if ([string]$event.data.toolCallId -cne 'toolu_012UXeE4Un8Fe7UdbrFwsgbX') {
            $event.agentId = $null
        }
    }
    Invoke-Control native-partial-attribution native-worker-scope $nativePartialAttribution $false 'missing worker attribution' ''

    $nativeCompletionBeforeStart = Get-Content (
        Join-Path $nativeControlRoot 'retained-native-o-scope.json'
    ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    $outOfOrder = $nativeCompletionBeforeStart.trajectory.events[2]
    $nativeCompletionBeforeStart.trajectory.events[2] =
        $nativeCompletionBeforeStart.trajectory.events[3]
    $nativeCompletionBeforeStart.trajectory.events[3] = $outOfOrder
    Invoke-Control native-completion-before-start native-worker-scope $nativeCompletionBeforeStart $false 'occurred before its start event' ''

    $nativeFailedCompletion = Clone-Object $nativeG
    $failedCompletion = $nativeFailedCompletion.trajectory.events |
        Where-Object {
            $_.type -ceq 'tool.execution_complete' -and
            -not [string]::IsNullOrWhiteSpace([string]$_.agentId)
        } |
        Select-Object -First 1
    $failedCompletion.data.success = $false
    $failedCompletion.data.PSObject.Properties.Remove('result')
    Invoke-Control native-failed-completion-without-result native-worker-scope $nativeFailedCompletion $true 'frozen candidate' ''

    $nativeEmptyWorkers = Clone-Object $nativeG
    $nativeEmptyWorkers.nativeScope.workers = @()
    Invoke-Control native-empty-workers native-worker-scope $nativeEmptyWorkers $false 'no declared workers' ''

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

    $mismatchedLifecycleSynthesis = Clone-Object $retainedLifecycle
    $mismatchedLifecycleSynthesis.trajectory.events[-1].data.content =
        'Clarification complete.'
    Invoke-Control lifecycle-mismatched-synthesis same-worker-lifecycle $mismatchedLifecycleSynthesis $false 'before the completed same-worker follow-up' '.eval-grader/inventory.json'

    $fullLifecycleSynthesis = Clone-Object $retainedLifecycle
    $fullLifecycleSynthesis.trajectory.events[-1].data.content =
        $fullLifecycleSynthesis.trajectory.output
    Invoke-Control lifecycle-full-output-synthesis same-worker-lifecycle $fullLifecycleSynthesis $true 'same ID' '.eval-grader/inventory.json'

    $transport = New-TransportInput
    Invoke-Control transport-positive ready-v2-transport $transport $true 'exact coordinator reads' '.eval-grader/inventory.json'

    $semanticPrompt = Clone-Object $transport
    $semanticManifest = Get-Content (
        Join-Path $testRoot '.review-bundle/manifest.json'
    ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    foreach ($worker in @(
        [pscustomobject]@{ CallId = 'worker'; GuideIndex = 0 },
        [pscustomobject]@{ CallId = 'worker-blazor'; GuideIndex = 1 }
    )) {
        $indexes = Get-ControlEventIndexes `
            -ControlInput $semanticPrompt `
            -ToolCallId $worker.CallId
        $guide = $semanticManifest.guides[$worker.GuideIndex]
        $compactAssignment = $guide.assignment |
            ConvertTo-Json -Compress -Depth 20
        $prettyAssignment = $guide.assignment |
            ConvertTo-Json -Depth 20
        $prompt = $semanticPrompt.trajectory.events[$indexes[0]].data.arguments.prompt.
            Replace($compactAssignment, $prettyAssignment).
            Replace('Frozen PR:', 'PR #').
            Replace('Frozen head:', 'head =').
            Replace('Frozen merge base:', 'mergeBase =').
            Replace('Frozen base tip:', 'baseTip =').
            Replace('Source role head root:', 'head root =').
            Replace('Source role mergeBase root:', 'mergeBase root =').
            Replace('Source role baseTip root:', 'baseTip root =')
        $semanticPrompt.trajectory.events[$indexes[0]].data.arguments.prompt = $prompt
    }
    Invoke-Control transport-semantic-prompt-format ready-v2-transport $semanticPrompt $true 'valid role separation' '.eval-grader/inventory.json'

    $structuredPrompt = Clone-Object $transport
    foreach ($worker in @(
        [pscustomobject]@{ CallId = 'worker'; Format = 'json' },
        [pscustomobject]@{ CallId = 'worker-blazor'; Format = 'markdown' }
    )) {
        $indexes = Get-ControlEventIndexes `
            -ControlInput $structuredPrompt `
            -ToolCallId $worker.CallId
        $prompt = $structuredPrompt.trajectory.events[$indexes[0]].data.arguments.prompt
        $identityLines = @(
            "Frozen PR: $($semanticManifest.target.pr)"
            "Frozen head: $($semanticManifest.target.head)"
            "Frozen merge base: $($semanticManifest.target.mergeBase)"
            "Frozen base tip: $($semanticManifest.target.baseTip)"
        ) -join "`n"
        $sourceLines = @($semanticManifest.guides[0].assignment.sourceRoles | ForEach-Object {
            "Source role $_ root: $($semanticManifest.sources.$_.root)"
        }) -join "`n"
        if ($worker.Format -eq 'json') {
            $identityReplacement = (
                [ordered]@{
                    pr = $semanticManifest.target.pr
                    head = $semanticManifest.target.head
                    mergeBase = $semanticManifest.target.mergeBase
                    baseTip = $semanticManifest.target.baseTip
                } | ConvertTo-Json -Compress
            )
            $sourceReplacement = (
                [ordered]@{
                    head = $semanticManifest.sources.head.root
                    mergeBase = $semanticManifest.sources.mergeBase.root
                    baseTip = $semanticManifest.sources.baseTip.root
                } | ConvertTo-Json -Compress
            )
        } else {
            $identityReplacement = @(
                "- **PR:** $($semanticManifest.target.pr)"
                "- **Head:** ``$($semanticManifest.target.head)``"
                "- **Merge base:** ``$($semanticManifest.target.mergeBase)``"
                "- **Base tip:** ``$($semanticManifest.target.baseTip)``"
            ) -join "`n"
            $sourceReplacement = @(
                "- head: $($semanticManifest.sources.head.root)"
                "- mergeBase: $($semanticManifest.sources.mergeBase.root)"
                "- baseTip: $($semanticManifest.sources.baseTip.root)"
            ) -join "`n"
        }
        $structuredPrompt.trajectory.events[$indexes[0]].data.arguments.prompt =
            $prompt.Replace($identityLines, $identityReplacement).
                Replace($sourceLines, $sourceReplacement)
    }
    Invoke-Control transport-structured-prompt-format ready-v2-transport $structuredPrompt $true 'valid role separation' '.eval-grader/inventory.json'

    $unrelatedProhibitionPrompt = Clone-Object $transport
    foreach ($worker in @('worker', 'worker-blazor')) {
        $indexes = Get-ControlEventIndexes `
            -ControlInput $unrelatedProhibitionPrompt `
            -ToolCallId $worker
        $unrelatedProhibitionPrompt.trajectory.events[$indexes[0]].data.arguments.prompt =
            $unrelatedProhibitionPrompt.trajectory.events[$indexes[0]].data.arguments.prompt.
                Replace(
                    "Frozen PR: $($semanticManifest.target.pr)",
                    "PR: #$($semanticManifest.target.pr); do not publish feedback."
                ).
                Replace(
                    "Frozen head: $($semanticManifest.target.head)",
                    "Frozen head: $($semanticManifest.target.head). Do not execute target code."
                )
    }
    Invoke-Control transport-unrelated-prohibition-format ready-v2-transport $unrelatedProhibitionPrompt $true 'valid role separation' '.eval-grader/inventory.json'

    $nestedAssignmentPrompt = Clone-Object $transport
    foreach ($worker in @(
        [pscustomobject]@{ CallId = 'worker'; GuideIndex = 0 },
        [pscustomobject]@{ CallId = 'worker-blazor'; GuideIndex = 1 }
    )) {
        $indexes = Get-ControlEventIndexes `
            -ControlInput $nestedAssignmentPrompt `
            -ToolCallId $worker.CallId
        $assignment = $semanticManifest.guides[$worker.GuideIndex].assignment |
            ConvertTo-Json -Compress -Depth 20
        $nestedAssignmentPrompt.trajectory.events[$indexes[0]].data.arguments.prompt =
            $nestedAssignmentPrompt.trajectory.events[$indexes[0]].data.arguments.prompt.Replace(
                $assignment,
                "{`"assignment`":$assignment}"
            )
    }
    Invoke-Control transport-nested-assignment-format ready-v2-transport $nestedAssignmentPrompt $true 'valid role separation' '.eval-grader/inventory.json'

    $missingRootSynthesis = Clone-Object $transport
    $missingRootSynthesis.trajectory.events = @(
        $missingRootSynthesis.trajectory.events[0..(
            $missingRootSynthesis.trajectory.events.Count - 2
        )]
    )
    Invoke-Control transport-missing-root-synthesis ready-v2-transport $missingRootSynthesis $false 'root synthesis matching trajectory output' '.eval-grader/inventory.json'

    $mismatchedRootSynthesis = Clone-Object $transport
    $mismatchedRootSynthesis.trajectory.events[-1].data.content = 'STATUS: FINDINGS'
    Invoke-Control transport-mismatched-root-synthesis ready-v2-transport $mismatchedRootSynthesis $false 'root synthesis matching trajectory output' '.eval-grader/inventory.json'

    $rootSkill = Clone-Object $transport
    $rootSkill.trajectory.events = @(
        New-Call skill root-skill @{
            skill = 'review-pull-request'
        } $true 'Skill loaded.'
    ) + @($rootSkill.trajectory.events)
    Invoke-Control transport-root-skill ready-v2-transport $rootSkill $true 'valid role separation' '.eval-grader/inventory.json'

    $wrongRootSkill = Clone-Object $rootSkill
    $wrongRootSkillIndexes = Get-ControlEventIndexes `
        -ControlInput $wrongRootSkill `
        -ToolCallId 'root-skill'
    $wrongRootSkill.trajectory.events[$wrongRootSkillIndexes[0]].data.arguments.skill =
        'other-skill'
    Invoke-Control transport-root-wrong-skill ready-v2-transport $wrongRootSkill $false 'root skill load' '.eval-grader/inventory.json'

    $repeatedRootSkill = Clone-Object $rootSkill
    $rootSynthesis = $repeatedRootSkill.trajectory.events[-1]
    $repeatedRootSkill.trajectory.events = @(
        $repeatedRootSkill.trajectory.events[0..(
            $repeatedRootSkill.trajectory.events.Count - 2
        )]
        New-Call skill repeated-root-skill @{
            skill = 'review-pull-request'
        } $true 'Skill loaded again.'
        $rootSynthesis
    )
    Invoke-Control transport-root-repeated-skill ready-v2-transport $repeatedRootSkill $false 'root skill load' '.eval-grader/inventory.json'

    $rootLiveFetch = Clone-Object $transport
    $rootSynthesis = $rootLiveFetch.trajectory.events[-1]
    $rootLiveFetch.trajectory.events = @(
        $rootLiveFetch.trajectory.events[0..(
            $rootLiveFetch.trajectory.events.Count - 2
        )]
        New-Call web_fetch root-live-fetch @{
            url = 'https://example.invalid'
        } $true 'Fetched.'
        $rootSynthesis
    )
    Invoke-Control transport-root-live-fetch ready-v2-transport $rootLiveFetch $false "unsupported root tool 'web_fetch'" '.eval-grader/inventory.json'

    $rootMutation = Clone-Object $transport
    $rootSynthesis = $rootMutation.trajectory.events[-1]
    $rootMutation.trajectory.events = @(
        $rootMutation.trajectory.events[0..(
            $rootMutation.trajectory.events.Count - 2
        )]
        New-Call apply_patch root-mutation @{
            patch = 'synthetic'
        } $true 'Applied.'
        $rootSynthesis
    )
    Invoke-Control transport-root-mutation ready-v2-transport $rootMutation $false "unsupported root tool 'apply_patch'" '.eval-grader/inventory.json'

    $rootOutsideRead = Clone-Object $transport
    $rootSynthesis = $rootOutsideRead.trajectory.events[-1]
    $rootOutsideRead.trajectory.events = @(
        $rootOutsideRead.trajectory.events[0..(
            $rootOutsideRead.trajectory.events.Count - 2
        )]
        New-Call view root-outside-read @{
            path = 'src/Untrusted.cs'
            view_range = @(1, -1)
        } $true 'L1:untrusted'
        $rootSynthesis
    )
    Invoke-Control transport-root-outside-read ready-v2-transport $rootOutsideRead $false 'outside its exact bundle inputs' '.eval-grader/inventory.json'

    $rootOptionalRead = Clone-Object $transport
    $transportInventory = Get-Content (
        Join-Path $testRoot '.eval-grader/inventory.json'
    ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    $optionalCoordinatorInput = $transportInventory.files |
        Where-Object {
            -not $_.coordinatorRequired -and
            'coordinator' -in @($_.readRoles)
        } |
        Select-Object -First 1
    $rootSynthesis = $rootOptionalRead.trajectory.events[-1]
    $rootOptionalRead.trajectory.events = @(
        $rootOptionalRead.trajectory.events[0..(
            $rootOptionalRead.trajectory.events.Count - 2
        )]
        New-Call view root-optional-read @{
            path = ".review-bundle/$([string]$optionalCoordinatorInput.path)"
            view_range = @(1, 1)
        } $true 'L1:optional frozen input'
        $rootSynthesis
    )
    Invoke-Control transport-root-optional-read ready-v2-transport $rootOptionalRead $true 'valid role separation' '.eval-grader/inventory.json'

    $rootBroadSearch = Clone-Object $transport
    $rootSynthesis = $rootBroadSearch.trajectory.events[-1]
    $rootBroadSearch.trajectory.events = @(
        $rootBroadSearch.trajectory.events[0..(
            $rootBroadSearch.trajectory.events.Count - 2
        )]
        New-Call rg root-broad-search @{
            pattern = 'Value'
            paths = '.review-bundle'
            output_mode = 'content'
        } $true '.review-bundle/diff.patch'
        $rootSynthesis
    )
    Invoke-Control transport-root-broad-search ready-v2-transport $rootBroadSearch $false 'broad root search' '.eval-grader/inventory.json'

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
    Invoke-Control transport-worker-skill-text-only ready-v2-transport $skillTextOnly $false 'did not invoke review-pull-request exactly once' '.eval-grader/inventory.json'

    $workerWithoutCalls = Clone-Object $transport
    $workerWithoutCallIndexes = @(
        Get-ControlEventIndexes -ControlInput $workerWithoutCalls -ToolCallId 'worker-blazor-skill'
        Get-ControlEventIndexes -ControlInput $workerWithoutCalls -ToolCallId 'worker-blazor-guide'
    )
    $workerWithoutCalls.trajectory.events = @(
        for ($index = 0; $index -lt $workerWithoutCalls.trajectory.events.Count; $index++) {
            if ($index -notin $workerWithoutCallIndexes) {
                $workerWithoutCalls.trajectory.events[$index]
            }
        }
    )
    Invoke-Control transport-worker-without-tool-calls ready-v2-transport $workerWithoutCalls $false 'did not invoke review-pull-request exactly once' '.eval-grader/inventory.json'

    $failedWorkerSkill = Clone-Object $transport
    $failedWorkerSkillIndexes = Get-ControlEventIndexes `
        -ControlInput $failedWorkerSkill `
        -ToolCallId 'worker-blazor-skill'
    $failedWorkerSkill.trajectory.events[$failedWorkerSkillIndexes[1]].data.success = $false
    Invoke-Control transport-worker-failed-skill ready-v2-transport $failedWorkerSkill $false 'did not successfully invoke review-pull-request' '.eval-grader/inventory.json'

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

    $workerUnsupportedTool = Clone-Object $transport
    $rootSynthesis = $workerUnsupportedTool.trajectory.events[-1]
    $workerUnsupportedTool.trajectory.events = @(
        $workerUnsupportedTool.trajectory.events[0..(
            $workerUnsupportedTool.trajectory.events.Count - 2
        )]
        New-Call lsp worker-lsp @{
            operation = 'workspaceSymbol'
            query = 'Value'
        } $true 'symbol result' '22222222-2222-2222-2222-222222222222'
        $rootSynthesis
    )
    Invoke-Control transport-worker-unsupported-tool ready-v2-transport $workerUnsupportedTool $false 'unsupported worker tool' '.eval-grader/inventory.json'

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
    $rootSynthesis = $workerRunningReport.trajectory.events[-1]
    $workerRunningReport.trajectory.events = @(
        $workerRunningReport.trajectory.events[0..(
            $workerRunningReport.trajectory.events.Count - 2
        )]
        New-Call read_agent worker-cross-running-report @{
            agent_id = '22222222-2222-2222-2222-222222222222'
            wait = $true
        } $true 'Agent is still running, status: running, total_turns: 0.'
        $rootSynthesis
    )
    Invoke-Control transport-worker-running-report ready-v2-transport $workerRunningReport $true 'valid role separation' '.eval-grader/inventory.json'

    $workerFollowUpWithoutGuide = Clone-Object $transport
    $followUpEvents = @(
        New-Call write_agent worker-cross-follow-up @{
        agent_id = '22222222-2222-2222-2222-222222222222'
        message = 'Clarify the candidate against the frozen base contract.'
        } $true 'Message delivered.'
        New-Call read_agent worker-cross-follow-up-report @{
        agent_id = '22222222-2222-2222-2222-222222222222'
        wait = $true
        since_turn = 1
        } $true 'Agent is idle, status: idle, total_turns: 2. Turn 1 follow-up: PATH: per-guide-worker. Clarification complete; the candidate remains unresolved.'
    )
    $rootSynthesis = $workerFollowUpWithoutGuide.trajectory.events[-1]
    $workerFollowUpWithoutGuide.trajectory.events = @(
        $workerFollowUpWithoutGuide.trajectory.events[0..(
            $workerFollowUpWithoutGuide.trajectory.events.Count - 2
        )]
        $followUpEvents
        $rootSynthesis
    )
    Invoke-Control transport-worker-follow-up-without-guide ready-v2-transport $workerFollowUpWithoutGuide $true 'valid role separation' '.eval-grader/inventory.json'

    $workerMarkdownFollowUp = Clone-Object $workerFollowUpWithoutGuide
    $markdownFollowUpReport = Get-ControlEventIndexes `
        -ControlInput $workerMarkdownFollowUp `
        -ToolCallId 'worker-cross-follow-up-report'
    $workerMarkdownFollowUp.trajectory.events[$markdownFollowUpReport[1]].data.result =
        $workerMarkdownFollowUp.trajectory.events[$markdownFollowUpReport[1]].data.result.Replace(
            'PATH: per-guide-worker',
            '**PATH:** per-guide-worker'
        )
    Invoke-Control transport-worker-markdown-follow-up ready-v2-transport $workerMarkdownFollowUp $true 'valid role separation' '.eval-grader/inventory.json'

    $workerUnchangedFollowUp = Clone-Object $workerFollowUpWithoutGuide
    $initialReport = Get-ControlEventIndexes `
        -ControlInput $workerUnchangedFollowUp `
        -ToolCallId 'worker-cross-report'
    $followUpReport = Get-ControlEventIndexes `
        -ControlInput $workerUnchangedFollowUp `
        -ToolCallId 'worker-cross-follow-up-report'
    $workerUnchangedFollowUp.trajectory.events[$followUpReport[1]].data.result =
        $workerUnchangedFollowUp.trajectory.events[$initialReport[1]].data.result
    Invoke-Control transport-worker-unchanged-follow-up ready-v2-transport $workerUnchangedFollowUp $false 'no new completed follow-up turn' '.eval-grader/inventory.json'

    $workerSharedFollowUp = Clone-Object $transport
    $rootSynthesis = $workerSharedFollowUp.trajectory.events[-1]
    $workerSharedFollowUp.trajectory.events = @(
        $workerSharedFollowUp.trajectory.events[0..(
            $workerSharedFollowUp.trajectory.events.Count - 2
        )]
        New-Call write_agent worker-cross-first-clarification @{
            agent_id = '22222222-2222-2222-2222-222222222222'
            message = 'Clarify the candidate against the frozen base contract.'
        } $true 'Message delivered.'
        New-Call write_agent worker-cross-second-clarification @{
            agent_id = '22222222-2222-2222-2222-222222222222'
            message = 'Clarify the issue against the frozen base contract.'
        } $true 'Message delivered.'
        New-Call read_agent worker-cross-shared-follow-up @{
            agent_id = '22222222-2222-2222-2222-222222222222'
            wait = $true
            since_turn = 1
        } $true 'Agent is idle, status: idle, total_turns: 2. Turn 1 follow-up: PATH: per-guide-worker. Clarification complete; the candidate remains unresolved.'
        $rootSynthesis
    )
    Invoke-Control transport-worker-shared-follow-up ready-v2-transport $workerSharedFollowUp $false 'no new completed follow-up turn' '.eval-grader/inventory.json'

    $crossWorkerClarifications = Clone-Object $transport
    $rootSynthesis = $crossWorkerClarifications.trajectory.events[-1]
    $crossWorkerClarifications.trajectory.events = @(
        $crossWorkerClarifications.trajectory.events[0..(
            $crossWorkerClarifications.trajectory.events.Count - 2
        )]
        New-Call write_agent worker-cross-clarification @{
            agent_id = '22222222-2222-2222-2222-222222222222'
            message = 'Clarify the candidate against the frozen base contract.'
        } $true 'Message delivered.'
        New-Call write_agent worker-blazor-clarification @{
            agent_id = '11111111-1111-1111-1111-111111111111'
            message = 'Clarify the issue against the frozen base contract.'
        } $true 'Message delivered.'
        New-Call read_agent worker-cross-clarification-report @{
            agent_id = '22222222-2222-2222-2222-222222222222'
            wait = $true
            since_turn = 1
        } $true 'Agent is idle, status: idle, total_turns: 2. Turn 1 follow-up: PATH: per-guide-worker. Clarification complete for the assigned cross-cutting guide.'
        New-Call read_agent worker-blazor-clarification-report @{
            agent_id = '11111111-1111-1111-1111-111111111111'
            wait = $true
            since_turn = 1
        } $true 'Agent is idle, status: idle, total_turns: 2. Turn 1 follow-up: PATH: per-guide-worker. Clarification complete for the assigned Blazor guide.'
        $rootSynthesis
    )
    Invoke-Control transport-cross-worker-clarifications ready-v2-transport $crossWorkerClarifications $true 'valid role separation' '.eval-grader/inventory.json'

    $workerClarificationWithoutFollowUp = Clone-Object $transport
    $rootSynthesis = $workerClarificationWithoutFollowUp.trajectory.events[-1]
    $workerClarificationWithoutFollowUp.trajectory.events = @(
        $workerClarificationWithoutFollowUp.trajectory.events[0..(
            $workerClarificationWithoutFollowUp.trajectory.events.Count - 2
        )]
        New-Call write_agent worker-cross-uncollected-follow-up @{
            agent_id = '22222222-2222-2222-2222-222222222222'
            message = 'Clarify the candidate against the frozen base contract.'
        } $true 'Message delivered.'
        $rootSynthesis
    )
    Invoke-Control transport-worker-clarification-without-follow-up ready-v2-transport $workerClarificationWithoutFollowUp $false 'no new completed follow-up turn' '.eval-grader/inventory.json'

    $workerLateFollowUpWithoutGuide = Clone-Object $transport
    $workerLateFollowUpWithoutGuide.trajectory.events += $followUpEvents
    Invoke-Control transport-worker-late-follow-up-without-guide ready-v2-transport $workerLateFollowUpWithoutGuide $false 'report collection completed after root synthesis' '.eval-grader/inventory.json'

    $workerWriteAfterSynthesis = Clone-Object $transport
    $workerWriteAfterSynthesis.trajectory.events += New-Call write_agent worker-cross-write-after-synthesis @{
        agent_id = '22222222-2222-2222-2222-222222222222'
        message = 'Clarify the candidate against the frozen base contract.'
    } $true 'Message delivered.'
    Invoke-Control transport-worker-write-after-synthesis ready-v2-transport $workerWriteAfterSynthesis $false 'lifecycle call occurred after root synthesis' '.eval-grader/inventory.json'

    $workerFollowUpCoordinatorOutput = Clone-Object $workerFollowUpWithoutGuide
    $followUpReport = Get-ControlEventIndexes -ControlInput $workerFollowUpCoordinatorOutput -ToolCallId 'worker-cross-follow-up-report'
    $workerFollowUpCoordinatorOutput.trajectory.events[$followUpReport[1]].data.result =
        'Agent is idle, status: idle, total_turns: 2. Turn 1 follow-up: PATH: single-reviewer. Clarification complete.'
    Invoke-Control transport-worker-follow-up-coordinator-output ready-v2-transport $workerFollowUpCoordinatorOutput $false 'coordinator aggregation output' '.eval-grader/inventory.json'

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

    $workerMissingPath = Clone-Object $transport
    $workerReport = Get-ControlEventIndexes -ControlInput $workerMissingPath -ToolCallId 'worker-cross-report'
    $workerMissingPath.trajectory.events[$workerReport[1]].data.result =
        $workerMissingPath.trajectory.events[$workerReport[1]].data.result.Replace(
            'PATH: per-guide-worker',
            'Worker analysis complete'
        )
    Invoke-Control transport-worker-missing-path ready-v2-transport $workerMissingPath $false 'did not report PATH: per-guide-worker' '.eval-grader/inventory.json'

    $workerReadAgentCombinedOutput = Clone-Object $transport
    $workerReport = Get-ControlEventIndexes -ControlInput $workerReadAgentCombinedOutput -ToolCallId 'worker-cross-report'
    $workerReadAgentCombinedOutput.trajectory.events[$workerReport[1]].data.result =
        'Agent is idle, status: idle, total_turns: 1. Turn 0: PATH: per-guide. GUIDES: docs/CrossCuttingGuidance.md, docs/BlazorComponentsGuidance.md'
    Invoke-Control transport-worker-read-agent-combined-output ready-v2-transport $workerReadAgentCombinedOutput $false 'coordinator aggregation output' '.eval-grader/inventory.json'

    $workerMarkdownCombinedOutput = Clone-Object $transport
    $workerReport = Get-ControlEventIndexes -ControlInput $workerMarkdownCombinedOutput -ToolCallId 'worker-cross-report'
    $workerMarkdownCombinedOutput.trajectory.events[$workerReport[1]].data.result =
        "Agent is idle, status: idle, total_turns: 1. Turn 0: **PATH:** single-reviewer`n**GUIDES:** docs/CrossCuttingGuidance.md`n**Guide:** docs/CrossCuttingGuidance.md"
    Invoke-Control transport-worker-markdown-combined-output ready-v2-transport $workerMarkdownCombinedOutput $false 'coordinator aggregation output' '.eval-grader/inventory.json'

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
    Invoke-Control transport-worker-cross-guide-read ready-v2-transport $workerCrossGuideRead $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerCrossGuideDirectoryRead = Clone-Object $transport
    $workerCrossGuideDirectoryRead.trajectory.events += New-Call view worker-cross-guide-directory @{
        path = '.review-bundle/guidance/docs'
    } $true 'docs/CrossCuttingGuidance.md.source docs/BlazorComponentsGuidance.md.source' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-cross-guide-directory ready-v2-transport $workerCrossGuideDirectoryRead $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerCrossGuideSearch = Clone-Object $transport
    $workerCrossGuideSearch.trajectory.events += New-Call rg worker-cross-guide-search @{
        pattern = 'guidance'
        paths = '.review-bundle/guidance'
        output_mode = 'content'
    } $true 'match' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-cross-guide-search ready-v2-transport $workerCrossGuideSearch $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerCrossGuideGlob = Clone-Object $transport
    $workerCrossGuideGlob.trajectory.events += New-Call glob worker-cross-guide-glob @{
        pattern = '*.source'
        paths = '.review-bundle/guidance/docs'
    } $true 'docs/CrossCuttingGuidance.md.source docs/BlazorComponentsGuidance.md.source' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-cross-guide-glob ready-v2-transport $workerCrossGuideGlob $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerDefaultWildcardGlob = Clone-Object $transport
    $workerDefaultWildcardGlob.trajectory.events += New-Call glob worker-default-wildcard-glob @{
        pattern = '**/required/**/*.source'
    } $true 'required/coordinator/exclusions/0000-scope.source' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-default-wildcard-glob ready-v2-transport $workerDefaultWildcardGlob $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerSiblingPatternGlob = Clone-Object $transport
    $workerSiblingPatternGlob.trajectory.events += New-Call glob worker-sibling-pattern-glob @{
        pattern = '0000/policies/0000.source*'
        paths = '.review-bundle/required/guides'
    } $true '0000/policies/0000.source' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-sibling-pattern-glob ready-v2-transport $workerSiblingPatternGlob $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerSingularSearchPath = Clone-Object $transport
    $workerSingularSearchPath.trajectory.events += New-Call rg worker-singular-search-path @{
        pattern = 'Components'
        path = '.review-bundle/required/guides/0001/policies/0000.source'
        output_mode = 'content'
    } $true 'Components policy' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-singular-search-path ready-v2-transport $workerSingularSearchPath $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerOutsideBundle = Clone-Object $transport
    $workerOutsideBundle.trajectory.events += New-Call view worker-outside-bundle @{
        path = '.eval-grader/inventory.json'
        view_range = @(1, -1)
    } $true 'trusted inventory' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-outside-bundle ready-v2-transport $workerOutsideBundle $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerAssignedPolicy = Clone-Object $transport
    $workerAssignedPolicy.trajectory.events += New-Call view worker-assigned-policy @{
        path = '.review-bundle/required/guides/0000/policies/0000.source'
        view_range = @(1, -1)
    } $true 'L1:### Required clause' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-assigned-policy ready-v2-transport $workerAssignedPolicy $true 'valid role separation' '.eval-grader/inventory.json'

    $workerAssignedExclusion = Clone-Object $transport
    $workerAssignedExclusion.trajectory.events += New-Call view worker-assigned-exclusion @{
        path = '.review-bundle/required/guides/0000/exclusions/0000-scope.source'
        view_range = @(1, -1)
    } $true 'L1:Running target code is excluded.' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-assigned-exclusion ready-v2-transport $workerAssignedExclusion $true 'valid role separation' '.eval-grader/inventory.json'

    $workerAssignedSourceSearch = Clone-Object $transport
    $workerAssignedSourceSearch.trajectory.events += New-Call rg worker-assigned-source-search @{
        pattern = 'Value'
        paths = '.review-bundle/source/5789cbca8244dcff42ca0722f977b8a0e981b8b0'
        output_mode = 'content'
    } $true 'src/Value.cs.source:1:class Value' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-assigned-source-search ready-v2-transport $workerAssignedSourceSearch $true 'valid role separation' '.eval-grader/inventory.json'

    foreach ($enumeration in @(
        [pscustomobject]@{
            Name = 'assigned-guide'
            Path = '.review-bundle/required/guides/0000'
        },
        [pscustomobject]@{
            Name = 'assigned-policy-parent'
            Path = '.review-bundle/required/guides/0000/policies'
        },
        [pscustomobject]@{
            Name = 'all-guides'
            Path = '.review-bundle/required/guides'
        }
    )) {
        $workerParentEnumeration = Clone-Object $transport
        $workerParentEnumeration.trajectory.events += New-Call view worker-parent-enumeration @{
            path = $enumeration.Path
        } $true 'directory entries' '22222222-2222-2222-2222-222222222222'
        Invoke-Control `
            "transport-worker-parent-enumeration-$($enumeration.Name)" `
            ready-v2-transport `
            $workerParentEnumeration `
            $false `
            'unassigned bundle path' `
            '.eval-grader/inventory.json'
    }

    $workerSiblingPolicy = Clone-Object $transport
    $workerSiblingPolicy.trajectory.events += New-Call view worker-sibling-policy @{
        path = '.review-bundle/required/guides/0001/policies/0000.source'
        view_range = @(1, -1)
    } $true 'L1:### Components clause' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-sibling-policy ready-v2-transport $workerSiblingPolicy $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerCoordinatorExclusion = Clone-Object $transport
    $workerCoordinatorExclusion.trajectory.events += New-Call view worker-coordinator-exclusion @{
        path = '.review-bundle/required/coordinator/exclusions/0000-scope.source'
        view_range = @(1, -1)
    } $true 'L1:Running target code is excluded.' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-coordinator-exclusion ready-v2-transport $workerCoordinatorExclusion $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerTraversal = Clone-Object $transport
    $workerTraversal.trajectory.events += New-Call view worker-traversal @{
        path = '.review-bundle/required/guides/0000/policies/../../../coordinator/exclusions/0000-scope.source'
        view_range = @(1, -1)
    } $true 'L1:Running target code is excluded.' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-traversal ready-v2-transport $workerTraversal $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerVariableShell = Clone-Object $transport
    $bundlePath = Join-Path $testRoot '.review-bundle'
    $workerVariableShell.trajectory.events += New-Call powershell worker-variable-shell @{
        command = "`$b='$bundlePath'; Get-ChildItem `"`$b\\required\\guides`""
    } $true ([pscustomobject]@{ content = 'directory entries'; exitCode = 0 }) '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-variable-shell ready-v2-transport $workerVariableShell $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerBracedVariableShell = Clone-Object $transport
    $workerBracedVariableShell.trajectory.events += New-Call powershell worker-braced-variable-shell @{
        command = '$sub="../../required/guides/0001/policies/0000.source"; Get-Content ".review-bundle/source/5789cbca8244dcff42ca0722f977b8a0e981b8b0/${sub}"'
    } $true ([pscustomobject]@{ content = 'sibling policy'; exitCode = 0 }) '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-braced-variable-shell ready-v2-transport $workerBracedVariableShell $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerPrefixVariableShell = Clone-Object $transport
    $workerPrefixVariableShell.trajectory.events += New-Call powershell worker-prefix-variable-shell @{
        command = '$s="src"; $sub="../../required/guides/0001/policies/0000.source"; Get-Content ".review-bundle/source/5789cbca8244dcff42ca0722f977b8a0e981b8b0/$sub"'
    } $true ([pscustomobject]@{ content = 'sibling policy'; exitCode = 0 }) '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-prefix-variable-shell ready-v2-transport $workerPrefixVariableShell $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerMultiPathShell = Clone-Object $transport
    $workerMultiPathShell.trajectory.events += New-Call powershell worker-multi-path-shell @{
        command = 'Get-Content .\.review-bundle\source\5789cbca8244dcff42ca0722f977b8a0e981b8b0\src\Value.cs.source .\.review-bundle\required\guides\0001\policies\0000.source'
    } $true ([pscustomobject]@{ content = 'combined content'; exitCode = 0 }) '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-multi-path-shell ready-v2-transport $workerMultiPathShell $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerRelativeShell = Clone-Object $transport
    $workerRelativeShell.trajectory.events += New-Call powershell worker-relative-shell @{
        command = 'Get-Content .\.review-bundle\required\guides\0001\policies\0000.source'
    } $true ([pscustomobject]@{ content = 'sibling policy'; exitCode = 0 }) '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-relative-shell ready-v2-transport $workerRelativeShell $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerRedirectedOutput = Clone-Object $transport
    $redirectedPath = Join-Path $testRoot 'worker-broad-output.txt'
    $workerRedirectedOutput.trajectory.events += New-Call powershell worker-redirected-shell @{
        command = "`$b='$bundlePath'; Get-ChildItem `"`$b\\required\\guides`" | Get-Content > '$redirectedPath'"
    } $true ([pscustomobject]@{
        content = "Output too large to read at once. Saved to: $redirectedPath"
        exitCode = 0
    }) '22222222-2222-2222-2222-222222222222'
    $workerRedirectedOutput.trajectory.events += New-Call view worker-redirected-read @{
        path = $redirectedPath
        forceReadLargeFiles = $true
    } $true 'contaminated output' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-redirected-output ready-v2-transport $workerRedirectedOutput $false 'intermediate output derived from' '.eval-grader/inventory.json'

    $workerDirectRedirect = Clone-Object $transport
    $directRedirectPath = Join-Path $testRoot 'worker-direct-output.txt'
    $workerDirectRedirect.trajectory.events += New-Call powershell worker-direct-redirect-shell @{
        command = "Get-ChildItem '$bundlePath\\required\\guides' | Get-Content > '$directRedirectPath'"
    } $true ([pscustomobject]@{
        content = 'Command completed.'
        exitCode = 0
    }) '22222222-2222-2222-2222-222222222222'
    $workerDirectRedirect.trajectory.events += New-Call view worker-direct-redirect-read @{
        path = $directRedirectPath
        forceReadLargeFiles = $true
    } $true 'contaminated output' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-direct-redirect ready-v2-transport $workerDirectRedirect $false 'intermediate output derived from' '.eval-grader/inventory.json'

    $workerSearchContaminatedOutput = Clone-Object $transport
    $searchOutputPath = Join-Path $testRoot 'worker-search-output.txt'
    $workerSearchContaminatedOutput.trajectory.events += New-Call powershell worker-search-output-shell @{
        command = "Get-ChildItem '$bundlePath\\required\\guides'"
    } $true ([pscustomobject]@{
        content = "Output too large to read at once. Saved to: $searchOutputPath"
        exitCode = 0
    }) '22222222-2222-2222-2222-222222222222'
    $workerSearchContaminatedOutput.trajectory.events += New-Call rg worker-search-output-read @{
        pattern = 'policy'
        paths = $searchOutputPath
        output_mode = 'content'
    } $true 'policy' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-search-contaminated-output ready-v2-transport $workerSearchContaminatedOutput $false 'intermediate output derived from' '.eval-grader/inventory.json'

    $workerExactShell = Clone-Object $transport
    $workerExactShell.trajectory.events += New-Call powershell worker-exact-shell @{
        command = 'Get-Content .\.review-bundle\required\guides\0000\policies\0000.source'
    } $true ([pscustomobject]@{ content = 'assigned policy'; exitCode = 0 }) '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-exact-shell ready-v2-transport $workerExactShell $false 'used a shell tool' '.eval-grader/inventory.json'

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

    $unboundReadAgent = Clone-Object $transport
    $unboundReadAgent.trajectory.events += New-Call read_agent unbound-read-agent @{
        agent_id = '33333333-3333-3333-3333-333333333333'
        wait = $true
    } $true 'Agent is idle, status: idle, total_turns: 1.'
    Invoke-Control transport-root-unbound-read-agent ready-v2-transport $unboundReadAgent $false 'targeted unassigned worker' '.eval-grader/inventory.json'

    $unboundWriteAgent = Clone-Object $transport
    $unboundWriteAgent.trajectory.events += New-Call write_agent unbound-write-agent @{
        agent_id = '33333333-3333-3333-3333-333333333333'
        message = 'Continue.'
    } $true 'Message delivered.'
    Invoke-Control transport-root-unbound-write-agent ready-v2-transport $unboundWriteAgent $false 'targeted unassigned worker' '.eval-grader/inventory.json'

    $missingWorkerPrompt = Clone-Object $transport
    $workerCall = Get-ControlEventIndexes -ControlInput $missingWorkerPrompt -ToolCallId 'worker'
    $missingWorkerPrompt.trajectory.events[$workerCall[0]].data.arguments.prompt = ''
    Invoke-Control transport-missing-worker-prompt ready-v2-transport $missingWorkerPrompt $false 'without its required inputs' '.eval-grader/inventory.json'

    $missingWorkerInput = Clone-Object $transport
    $workerCall = Get-ControlEventIndexes -ControlInput $missingWorkerInput -ToolCallId 'worker'
    $missingWorkerInput.trajectory.events[$workerCall[0]].data.arguments.prompt =
        $missingWorkerInput.trajectory.events[$workerCall[0]].data.arguments.prompt.Replace(
            'required/guides/0000/policies/0000.source',
            'required/guides/0000/policies/missing.source'
        )
    Invoke-Control transport-missing-worker-input ready-v2-transport $missingWorkerInput $false 'did not preserve the verbatim assignment' '.eval-grader/inventory.json'

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
            'required/guides/0001/policies/0000.source',
            'required/guides/0001/policies/missing.source'
        )
    Invoke-Control transport-missing-second-worker-policy ready-v2-transport $missingSecondPolicy $false 'did not preserve the verbatim assignment' '.eval-grader/inventory.json'

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
        'diff.patch',
        'missing.patch'
        )
    Invoke-Control transport-missing-second-shared-input ready-v2-transport $missingSecondSharedInput $false 'did not preserve the verbatim assignment' '.eval-grader/inventory.json'

        $transportManifest = Get-Content (
            Join-Path $testRoot '.review-bundle/manifest.json'
        ) -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
        $prohibitedSourceRoot = Clone-Object $transport
        $firstWorker = Get-ControlEventIndexes -ControlInput $prohibitedSourceRoot -ToolCallId 'worker'
        $headSourceRoot = [string]$transportManifest.sources.head.root
        $prohibitedSourceRoot.trajectory.events[$firstWorker[0]].data.arguments.prompt =
            $prohibitedSourceRoot.trajectory.events[$firstWorker[0]].data.arguments.prompt.Replace(
                "Source role head root: $headSourceRoot",
                "Do not use source root: $headSourceRoot"
            )
            Invoke-Control transport-prohibited-source-root ready-v2-transport $prohibitedSourceRoot $false 'omitted the exact frozen source role binding' '.eval-grader/inventory.json'

        $missingTargetIdentity = Clone-Object $transport
        $secondWorker = Get-ControlEventIndexes -ControlInput $missingTargetIdentity -ToolCallId 'worker-blazor'
        $missingTargetIdentity.trajectory.events[$secondWorker[0]].data.arguments.prompt =
            $missingTargetIdentity.trajectory.events[$secondWorker[0]].data.arguments.prompt.Replace(
                    "Frozen head: $($transportManifest.target.head)",
                    'Frozen head: missing-head'
            )
            Invoke-Control transport-missing-target-identity ready-v2-transport $missingTargetIdentity $false 'omitted the exact frozen head binding' '.eval-grader/inventory.json'

            $negatedTargetIdentity = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $negatedTargetIdentity -ToolCallId 'worker'
            $negatedTargetIdentity.trajectory.events[$firstWorker[0]].data.arguments.prompt =
                $negatedTargetIdentity.trajectory.events[$firstWorker[0]].data.arguments.prompt.Replace(
                    "Frozen head: $($transportManifest.target.head)",
                    "Frozen head: do not use $($transportManifest.target.head)"
                )
            Invoke-Control transport-negated-target-identity ready-v2-transport $negatedTargetIdentity $false 'omitted the exact frozen head binding' '.eval-grader/inventory.json'

            $simpleNegatedTargetIdentity = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $simpleNegatedTargetIdentity -ToolCallId 'worker'
            $simpleNegatedTargetIdentity.trajectory.events[$firstWorker[0]].data.arguments.prompt =
                $simpleNegatedTargetIdentity.trajectory.events[$firstWorker[0]].data.arguments.prompt.Replace(
                    "Frozen head: $($transportManifest.target.head)",
                    "Frozen head is not $($transportManifest.target.head)"
                )
            Invoke-Control transport-simple-negated-target-identity ready-v2-transport $simpleNegatedTargetIdentity $false 'omitted the exact frozen head binding' '.eval-grader/inventory.json'

            $sourceRootOnlyTargetIdentity = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $sourceRootOnlyTargetIdentity -ToolCallId 'worker'
            $headSourceRoot = [string]$transportManifest.sources.head.root
            $sourceRootOnlyTargetIdentity.trajectory.events[$firstWorker[0]].data.arguments.prompt =
                $sourceRootOnlyTargetIdentity.trajectory.events[$firstWorker[0]].data.arguments.prompt.
                    Replace(
                        "Frozen head: $($transportManifest.target.head)",
                        'Frozen head identity omitted.'
                    )
            $sourceRootOnlyTargetIdentity.trajectory.events[$firstWorker[0]].data.arguments.prompt +=
                "`nhead: $headSourceRoot"
            Invoke-Control transport-source-root-not-target-identity ready-v2-transport $sourceRootOnlyTargetIdentity $false 'omitted the exact frozen head binding' '.eval-grader/inventory.json'

            $swappedAssignmentMetadata = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $swappedAssignmentMetadata -ToolCallId 'worker'
            $firstAssignment = $transportManifest.guides[0].assignment
            $firstHash = [string]$firstAssignment.files[0].sha256
            $secondHash = [string]$firstAssignment.files[1].sha256
            $swapMarker = '__assignment_hash_swap__'
            $swappedAssignmentMetadata.trajectory.events[$firstWorker[0]].data.arguments.prompt =
                $swappedAssignmentMetadata.trajectory.events[$firstWorker[0]].data.arguments.prompt.
                    Replace($firstHash, $swapMarker).
                    Replace($secondHash, $firstHash).
                    Replace($swapMarker, $secondHash)
            Invoke-Control transport-swapped-assignment-metadata ready-v2-transport $swappedAssignmentMetadata $false 'did not preserve the verbatim assignment' '.eval-grader/inventory.json'

            $conflictingAssignmentMetadata = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $conflictingAssignmentMetadata -ToolCallId 'worker'
            $conflictingAssignment = Clone-Object $transportManifest.guides[0].assignment
            $conflictingAssignment.files[0].sha256 = '0' * 64
            $conflictingAssignmentMetadata.trajectory.events[$firstWorker[0]].data.arguments.prompt +=
                "`nConflicting assignment: " +
                ($conflictingAssignment | ConvertTo-Json -Compress -Depth 20)
            Invoke-Control transport-conflicting-assignment-metadata ready-v2-transport $conflictingAssignmentMetadata $false 'did not preserve the verbatim assignment' '.eval-grader/inventory.json'

            $swappedSourceBindings = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $swappedSourceBindings -ToolCallId 'worker'
            $headSourceRoot = [string]$transportManifest.sources.head.root
            $mergeBaseSourceRoot = [string]$transportManifest.sources.mergeBase.root
            $swapMarker = '__source_binding_swap__'
            $swappedSourceBindings.trajectory.events[$firstWorker[0]].data.arguments.prompt =
                $swappedSourceBindings.trajectory.events[$firstWorker[0]].data.arguments.prompt.
                    Replace("Source role head root: $headSourceRoot", $swapMarker).
                    Replace(
                        "Source role mergeBase root: $mergeBaseSourceRoot",
                        "Source role head root: $mergeBaseSourceRoot"
                    ).
                    Replace(
                        $swapMarker,
                        "Source role mergeBase root: $headSourceRoot"
                    )
            Invoke-Control transport-swapped-source-bindings ready-v2-transport $swappedSourceBindings $false 'exact frozen source role binding for' '.eval-grader/inventory.json'

            $swappedTargetBindings = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $swappedTargetBindings -ToolCallId 'worker'
            $head = [string]$transportManifest.target.head
            $mergeBase = [string]$transportManifest.target.mergeBase
            $swapMarker = '__target_binding_swap__'
            $swappedTargetBindings.trajectory.events[$firstWorker[0]].data.arguments.prompt =
                $swappedTargetBindings.trajectory.events[$firstWorker[0]].data.arguments.prompt.
                    Replace("Frozen head: $head", $swapMarker).
                    Replace(
                        "Frozen merge base: $mergeBase",
                        "Frozen head: $mergeBase"
                    ).
                    Replace($swapMarker, "Frozen merge base: $head")
            Invoke-Control transport-swapped-target-bindings ready-v2-transport $swappedTargetBindings $false 'exact frozen head binding' '.eval-grader/inventory.json'

            $sameLineSwappedBindings = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $sameLineSwappedBindings -ToolCallId 'worker'
            $headSourceRoot = [string]$transportManifest.sources.head.root
            $mergeBaseSourceRoot = [string]$transportManifest.sources.mergeBase.root
            $baseTipSourceRoot = [string]$transportManifest.sources.baseTip.root
            $head = [string]$transportManifest.target.head
            $mergeBase = [string]$transportManifest.target.mergeBase
            $baseTip = [string]$transportManifest.target.baseTip
            $sameLineSwappedBindings.trajectory.events[$firstWorker[0]].data.arguments.prompt =
                $sameLineSwappedBindings.trajectory.events[$firstWorker[0]].data.arguments.prompt.
                    Replace(
                        "Frozen head: $head`nFrozen merge base: $mergeBase`nFrozen base tip: $baseTip",
                        "Frozen head: $mergeBase, merge base: $baseTip, base tip: $head"
                    ).
                    Replace(
                        "Source role head root: $headSourceRoot`nSource role mergeBase root: $mergeBaseSourceRoot`nSource role baseTip root: $baseTipSourceRoot",
                        "Source roots: head root=$mergeBaseSourceRoot, mergeBase root=$baseTipSourceRoot, baseTip root=$headSourceRoot"
                    )
            Invoke-Control transport-same-line-swapped-bindings ready-v2-transport $sameLineSwappedBindings $false 'exact frozen source role binding for' '.eval-grader/inventory.json'

            $prNumberSubstring = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $prNumberSubstring -ToolCallId 'worker'
            $prNumberSubstring.trajectory.events[$firstWorker[0]].data.arguments.prompt =
                $prNumberSubstring.trajectory.events[$firstWorker[0]].data.arguments.prompt.Replace(
                    "Frozen PR: $($transportManifest.target.pr)",
                    "PR #$($transportManifest.target.pr)13"
                )
            Invoke-Control transport-pr-number-substring ready-v2-transport $prNumberSubstring $false 'omitted the exact frozen PR binding' '.eval-grader/inventory.json'

            $conflictingTargetBinding = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $conflictingTargetBinding -ToolCallId 'worker'
            $conflictingTargetBinding.trajectory.events[$firstWorker[0]].data.arguments.prompt +=
                "`nReview the code at head: $head or head: $mergeBase"
            Invoke-Control transport-conflicting-target-binding ready-v2-transport $conflictingTargetBinding $false 'omitted the exact frozen head binding' '.eval-grader/inventory.json'

            $conflictingSourceBinding = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $conflictingSourceBinding -ToolCallId 'worker'
            $conflictingSourceBinding.trajectory.events[$firstWorker[0]].data.arguments.prompt +=
                "`nSource role head root: $mergeBaseSourceRoot"
            Invoke-Control transport-conflicting-source-binding ready-v2-transport $conflictingSourceBinding $false 'omitted the exact frozen source role binding for' '.eval-grader/inventory.json'

            $trailingNegatedTargetBinding = Clone-Object $transport
            $firstWorker = Get-ControlEventIndexes -ControlInput $trailingNegatedTargetBinding -ToolCallId 'worker'
            $trailingNegatedTargetBinding.trajectory.events[$firstWorker[0]].data.arguments.prompt =
                $trailingNegatedTargetBinding.trajectory.events[$firstWorker[0]].data.arguments.prompt.Replace(
                    "Frozen head: $head",
                    "Frozen head: ``$head``, which isn't the revision to review"
                )
            Invoke-Control transport-trailing-negated-target-binding ready-v2-transport $trailingNegatedTargetBinding $false 'omitted the exact frozen head binding' '.eval-grader/inventory.json'

        $nonApplicableSecondPolicy = Clone-Object $transport
        $secondWorker = Get-ControlEventIndexes -ControlInput $nonApplicableSecondPolicy -ToolCallId 'worker-blazor'
        $nonApplicableSecondPolicy.trajectory.events[$secondWorker[0]].data.arguments.prompt +=
            "`nAdditional policy: required/guides/0000/policies/0000.source"
    Invoke-Control transport-non-applicable-second-policy ready-v2-transport $nonApplicableSecondPolicy $false 'included unassigned input' '.eval-grader/inventory.json'

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

    $coordinatorOnlyLabelRequest = Clone-Object $transport
    $secondWorker = Get-ControlEventIndexes -ControlInput $coordinatorOnlyLabelRequest -ToolCallId 'worker-blazor'
    $coordinatorOnlyLabelRequest.trajectory.events[$secondWorker[0]].data.arguments.prompt =
        $coordinatorOnlyLabelRequest.trajectory.events[$secondWorker[0]].data.arguments.prompt.Replace(
            'Do not read pull.json; the coordinator owns that input.',
            'Coordinator-only input pull.json must still be read by the worker.'
        )
    Invoke-Control transport-coordinator-only-label-request ready-v2-transport $coordinatorOnlyLabelRequest $false 'embedded or requested coordinator-only' '.eval-grader/inventory.json'

    $doubleNegativeCoordinatorRequest = Clone-Object $transport
    $secondWorker = Get-ControlEventIndexes -ControlInput $doubleNegativeCoordinatorRequest -ToolCallId 'worker-blazor'
    $doubleNegativeCoordinatorRequest.trajectory.events[$secondWorker[0]].data.arguments.prompt =
        $doubleNegativeCoordinatorRequest.trajectory.events[$secondWorker[0]].data.arguments.prompt.Replace(
            'Do not read pull.json; the coordinator owns that input.',
            'Do not skip reading pull.json.'
        )
    Invoke-Control transport-double-negative-coordinator-request ready-v2-transport $doubleNegativeCoordinatorRequest $false 'embedded or requested coordinator-only' '.eval-grader/inventory.json'

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
            '22222222-2222-2222-2222-222222222222'
    }
    Invoke-Control transport-worker-coordinator-only ready-v2-transport $wrongRole $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $wrongRoleReadFile = Clone-Object $transport
    $wrongRoleReadFile.trajectory.events += New-Call read_file worker-read-manifest @{
        filePath = '.review-bundle/manifest.json'
        startLine = 1
        endLine = 20
    } $true 'manifest bytes' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-read-file-coordinator-only ready-v2-transport $wrongRoleReadFile $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerRgManifest = Clone-Object $transport
    $workerRgManifest.trajectory.events += New-Call rg worker-rg-manifest @{
        pattern = 'ready'
        paths = '.review-bundle/manifest.json'
        output_mode = 'content'
    } $true '3:  "ready": true' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-rg-coordinator-only ready-v2-transport $workerRgManifest $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerRgBundle = Clone-Object $transport
    $workerRgBundle.trajectory.events += New-Call rg worker-rg-bundle @{
        pattern = 'ready'
        paths = '.review-bundle'
        output_mode = 'content'
    } $true 'manifest.json:3:  "ready": true' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-rg-broad-scope ready-v2-transport $workerRgBundle $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerGlobBundle = Clone-Object $transport
    $workerGlobBundle.trajectory.events += New-Call glob worker-glob-bundle @{
        pattern = '*.json'
        paths = '.review-bundle'
    } $true 'manifest.json pull.json feedback.json' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-glob-broad-scope ready-v2-transport $workerGlobBundle $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerRgDefault = Clone-Object $transport
    $workerRgDefault.trajectory.events += New-Call rg worker-rg-default @{
        pattern = 'ready'
        output_mode = 'content'
    } $true '.review-bundle/manifest.json:3:  "ready": true' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-rg-default-scope ready-v2-transport $workerRgDefault $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerRgEmpty = Clone-Object $transport
    $workerRgEmpty.trajectory.events += New-Call rg worker-rg-empty @{
        pattern = 'ready'
        paths = @()
        output_mode = 'content'
    } $true '.review-bundle/manifest.json:3:  "ready": true' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-rg-empty-scope ready-v2-transport $workerRgEmpty $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerRgParent = Clone-Object $transport
    $workerRgParent.trajectory.events += New-Call rg worker-rg-parent @{
        pattern = 'ready'
        paths = $testRoot
        output_mode = 'content'
    } $true '.review-bundle/manifest.json:3:  "ready": true' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-rg-parent-scope ready-v2-transport $workerRgParent $false 'unassigned bundle path' '.eval-grader/inventory.json'

    $workerGrepSibling = Clone-Object $transport
    $workerGrepSibling.trajectory.events += New-Call grep worker-grep-sibling @{
        pattern = 'Components'
        paths = '.review-bundle/required/guides/0001/policies/0000.source'
        output_mode = 'content'
    } $true '1:Components policy' '22222222-2222-2222-2222-222222222222'
    Invoke-Control transport-worker-grep-sibling ready-v2-transport $workerGrepSibling $false 'unassigned bundle path' '.eval-grader/inventory.json'

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
    $rootSynthesis = $lateManifest.trajectory.events[-1]
    $lateManifest.trajectory.events = @(
        for ($index = 0; $index -lt $lateManifest.trajectory.events.Count - 1; $index++) {
            if ($index -notin $manifestRead) {
                $lateManifest.trajectory.events[$index]
            }
        }
    ) + $manifestEvents + $rootSynthesis
    Invoke-Control transport-late-manifest ready-v2-transport $lateManifest $false 'before routing the guide worker' '.eval-grader/inventory.json'

    $readAfterSynthesis = Clone-Object $transport
    $latePullRead = Get-ControlEventIndexes -ControlInput $readAfterSynthesis -ToolCallId 'read-pull-json'
    $latePullEvents = @($readAfterSynthesis.trajectory.events[$latePullRead])
    $rootSynthesis = $readAfterSynthesis.trajectory.events[-1]
    $readAfterSynthesis.trajectory.events = @(
        for ($index = 0; $index -lt $readAfterSynthesis.trajectory.events.Count - 1; $index++) {
            if ($index -notin $latePullRead) {
                $readAfterSynthesis.trajectory.events[$index]
            }
        }
    ) + $rootSynthesis + $latePullEvents
    Invoke-Control transport-read-after-synthesis ready-v2-transport $readAfterSynthesis $false 'after root synthesis' '.eval-grader/inventory.json'

    $workerReportAfterSynthesis = Clone-Object $transport
    $lateWorkerReport = Get-ControlEventIndexes -ControlInput $workerReportAfterSynthesis -ToolCallId 'worker-cross-report'
    $lateWorkerReportEvents = @($workerReportAfterSynthesis.trajectory.events[$lateWorkerReport])
    $rootSynthesis = $workerReportAfterSynthesis.trajectory.events[-1]
    $workerReportAfterSynthesis.trajectory.events = @(
        for ($index = 0; $index -lt $workerReportAfterSynthesis.trajectory.events.Count - 1; $index++) {
            if ($index -notin $lateWorkerReport) {
                $workerReportAfterSynthesis.trajectory.events[$index]
            }
        }
    ) + $rootSynthesis + $lateWorkerReportEvents
    Invoke-Control transport-worker-report-after-synthesis ready-v2-transport $workerReportAfterSynthesis $false 'report collection completed after root synthesis' '.eval-grader/inventory.json'

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
                New-Call skill blocked-root-skill @{
                    skill = 'review-pull-request'
                } $true 'Skill loaded.'
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
    $errorBlocked.trajectory.events[5].data.result =
        'Native view error: access to the required feedback input was denied.'
    Invoke-Control missing-bytes-error-blocked required-bytes-blocked $errorBlocked $true 'BLOCKED result' '.eval-grader/inventory.json'

    $wrongSkillBlocked = Clone-Object $blocked
    $wrongSkillBlocked.trajectory.events[0].data.arguments.skill = 'other-skill'
    Invoke-Control missing-bytes-wrong-skill required-bytes-blocked $wrongSkillBlocked $false 'one successful review-pull-request skill load' '.eval-grader/inventory.json'

    $lateSkillBlocked = Clone-Object $blocked
    $lateSkillBlocked.trajectory.events = @(
        $lateSkillBlocked.trajectory.events[2..3]
        $lateSkillBlocked.trajectory.events[0..1]
        $lateSkillBlocked.trajectory.events[4..6]
    )
    Invoke-Control missing-bytes-late-skill required-bytes-blocked $lateSkillBlocked $false 'begin with one successful review-pull-request skill load' '.eval-grader/inventory.json'

    $workerSkillBlocked = Clone-Object $blocked
    $workerSkillBlocked.trajectory.events += New-Call skill blocked-worker-skill @{
        skill = 'other-skill'
    } $true 'Skill loaded.' '33333333-3333-3333-3333-333333333333'
    Invoke-Control missing-bytes-worker-skill required-bytes-blocked $workerSkillBlocked $false 'worker-attributed tool call' '.eval-grader/inventory.json'

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
        $partialManifest.trajectory.events[0..1]
        New-Call view blocked-manifest-first @{
            path = '.review-bundle/manifest.json'
            view_range = @(1, 20)
        } $true $firstManifestResult
        $partialManifest.trajectory.events[4..5]
        New-Call view blocked-manifest-rest @{
            path = '.review-bundle/manifest.json'
            view_range = @(21, -1)
        } $true $remainingManifestResult
        $partialManifest.trajectory.events[6]
    )
    Invoke-Control missing-bytes-partial-manifest required-bytes-blocked $partialManifest $false 'before the ready manifest' '.eval-grader/inventory.json'

    $feedbackBeforeManifest = Clone-Object $blocked
    $feedbackBeforeManifest.trajectory.events = @(
        $feedbackBeforeManifest.trajectory.events[0..1] +
        $feedbackBeforeManifest.trajectory.events[4..5] +
        $feedbackBeforeManifest.trajectory.events[2..3] +
        $feedbackBeforeManifest.trajectory.events[6]
    )
    Invoke-Control missing-bytes-before-manifest required-bytes-blocked $feedbackBeforeManifest $false 'before the ready manifest' '.eval-grader/inventory.json'

    $retainedFeedbackBeforeFailure = Clone-Object $blocked
    $retainedFeedbackPath = $transportInventory.files |
        Where-Object {
            [string]$_.path -match 'feedback' -and
            [string]$_.path -cne 'feedback.json' -and
            'coordinator' -in @($_.readRoles)
        } |
        Select-Object -First 1 -ExpandProperty path
    $retainedFeedbackBeforeFailure.trajectory.events = @(
        $retainedFeedbackBeforeFailure.trajectory.events[0..3]
        New-Call view blocked-retained-feedback @{
            path = ".review-bundle/$retainedFeedbackPath"
            view_range = @(1, -1)
        } $true 'L1:retained feedback'
        $retainedFeedbackBeforeFailure.trajectory.events[4..6]
    )
    Invoke-Control missing-bytes-retained-feedback-fallback required-bytes-blocked $retainedFeedbackBeforeFailure $false 'substitute evidence outside the manifest' '.eval-grader/inventory.json'

    $successFallback = Clone-Object $blocked
    $successFallback.trajectory.output = 'STATUS: NO_FINDINGS'
    Invoke-Control missing-bytes-success required-bytes-blocked $successFallback $false 'STATUS: BLOCKED' '.eval-grader/inventory.json'

    $mismatchedBlockedSynthesis = Clone-Object $blocked
    $mismatchedBlockedSynthesis.trajectory.events[-1].data.content =
        'Required feedback remained unavailable.'
    Invoke-Control missing-bytes-mismatched-synthesis required-bytes-blocked $mismatchedBlockedSynthesis $false 'No root assistant message reported STATUS: BLOCKED' '.eval-grader/inventory.json'

    $searchFallback = Clone-Object $blocked
    $rootSynthesis = $searchFallback.trajectory.events[-1]
    $searchFallback.trajectory.events = @(
        $searchFallback.trajectory.events[0..(
            $searchFallback.trajectory.events.Count - 2
        )]
        New-Call rg blocked-search-fallback @{
            pattern = 'feedback'
            paths = '.review-bundle'
            output_mode = 'files_with_matches'
        } $true '.review-bundle/manifest.json'
        $rootSynthesis
    )
    Invoke-Control missing-bytes-search-fallback required-bytes-blocked $searchFallback $false "unsupported fallback tool 'rg'" '.eval-grader/inventory.json'

    $substituteView = Clone-Object $blocked
    $rootSynthesis = $substituteView.trajectory.events[-1]
    $substituteView.trajectory.events = @(
        $substituteView.trajectory.events[0..(
            $substituteView.trajectory.events.Count - 2
        )]
        New-Call view blocked-substitute-view @{
            path = '.review-bundle/pull.json'
            view_range = @(1, -1)
        } $true 'L1:{"number":69782}'
        $rootSynthesis
    )
    Invoke-Control missing-bytes-substitute-view required-bytes-blocked $substituteView $false 'attempted substitute evidence' '.eval-grader/inventory.json'

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
