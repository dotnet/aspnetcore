#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('sdk-recovery', 'same-worker-lifecycle', 'ready-v2-transport', 'required-bytes-blocked')]
    [string]$Contract,

    [string]$ContractPath,

    [int]$ExpectedPr,

    [string]$ExpectedHead
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$fixtureContractLibrary = @(
    (Join-Path $PSScriptRoot 'fixture-contract.ps1'),
    (Join-Path $PSScriptRoot 'review-pull-request/fixture-contract.ps1')
) | Where-Object { Test-Path $_ -PathType Leaf } | Select-Object -First 1
if ($null -ne $fixtureContractLibrary) {
    . $fixtureContractLibrary
}

function Convert-ToText {
    param($Value)

    if ($null -eq $Value) {
        return ''
    }
    if ($Value -is [string]) {
        return $Value
    }
    $content = $Value.PSObject.Properties['content']?.Value
    if ($null -ne $content) {
        return [string]$content
    }
    return $Value | ConvertTo-Json -Compress -Depth 100
}

function Get-Argument {
    param(
        [object]$Call,
        [string]$Name
    )

    if ($null -eq $Call.Arguments) {
        return $null
    }
    return $Call.Arguments.PSObject.Properties[$Name]?.Value
}

function Complete-Grading {
    param(
        [bool]$Passed,
        [string]$Evidence,
        [string]$Status = 'success'
    )

    $result = [ordered]@{
        name = "trace-contract:$Contract"
        status = $Status
        kind = 'code'
        passed = $Passed
        score = if ($Passed) { 1 } else { 0 }
        evidence = $Evidence
    }
    [Console]::Out.WriteLine(($result | ConvertTo-Json -Compress -Depth 20))
    exit 0
}

function Fail {
    param([string]$Evidence)

    Complete-Grading -Passed $false -Evidence $Evidence
}

function Test-ShellTool {
    param([string]$Name)

    return $Name -match '^(bash|shell|powershell)$'
}

function Get-WorkerCoordinatorBehavior {
    param([object]$Call)

    $name = ([string]$Call.Name).ToLowerInvariant()
    if ($name -in @(
        'task',
        'read_agent',
        'write_agent',
        'list_agents',
        'ask_user',
        'create_session',
        'open_pr_session',
        'open_issue_session',
        'fork_session',
        'send_session_message',
        'get_session',
        'get_sessions_status',
        'list_sessions_and_chats',
        'answer_session_input',
        'relay_session_input',
        'respond_to_session_plan_v2',
        'run_dynamic_workflow',
        'dynamic_workflows_manage',
        'run_workflow',
        'get_session_automation',
        'list_workflows',
        'navigate_to',
        'navigate_to_my_work',
        'list_repositories',
        'add_repository',
        'read_workspace_file_draft_conflict'
    )) {
        return 'agent or session orchestration'
    }
    if ($name -in @(
        'web_fetch',
        'web_search',
        'semantic_code_search',
        'read_github_image',
        'navigate_to_github_item',
        'fetch',
        'fetch_blob'
    ) -or $name -match '(?i)(^|[._-])github([._-]|$)') {
        return 'live GitHub or fetch access'
    }
    if ($name -in @(
        'apply_patch',
        'sql',
        'image_generation',
        'create',
        'edit',
        'write',
        'edit_file',
        'write_file',
        'replace_string_in_file',
        'multi_replace_string_in_file',
        'insert_edit_into_file',
        'annotate_diff_line',
        'complete_workspace_file_draft_resolution',
        'task_complete'
    ) -or
        $name -match '^(?:create|edit|write|update|delete|reply|publish|manage|save|archive|install|share|add|remove|rename|annotate|complete|replace|insert)(?:$|[._-])') {
        return 'mutation or publication'
    }
    return ''
}

function Assert-WorkerToolBoundary {
    param([object[]]$WorkerCalls)

    foreach ($group in @($WorkerCalls | Group-Object AgentId)) {
        $ordered = @($group.Group | Sort-Object CallIndex)
        $skillCalls = @($ordered | Where-Object { $_.Name -ieq 'skill' })
        if (@($skillCalls | Where-Object {
            $_.Name -cne 'skill' -or
            [string](Get-Argument $_ 'skill') -cne 'review-pull-request'
        }).Count -ne 0) {
            Fail "Guide worker '$($group.Name)' invoked an unsupported skill."
        }
        if ($skillCalls.Count -gt 1) {
            Fail "Guide worker '$($group.Name)' invoked review-pull-request more than once."
        }
        if ($skillCalls.Count -eq 1 -and
            $skillCalls[0].CallIndex -ne $ordered[0].CallIndex) {
            Fail "Guide worker '$($group.Name)' did not use its optional review-pull-request load as its first tool call."
        }
        foreach ($call in $ordered | Where-Object { $_.Name -ine 'skill' }) {
            $behavior = Get-WorkerCoordinatorBehavior $call
            if (-not [string]::IsNullOrEmpty($behavior)) {
                Fail "Guide worker '$($group.Name)' used coordinator-only $behavior through '$($call.Name)'."
            }
        }
    }
}

function Test-CoordinatorOutputText {
    param([string]$Text)

    $reportPrefix = '(?:^|\bTurn\s+\d+\b[^:\r\n]*:\s*)\s*(?:[-+*]\s+)?(?:\*\*|__)?'
    $fieldSuffix = '(?:\*\*|__)?\s*:(?:\*\*|__)?\s*'
    return $Text -match "(?im)$reportPrefix" + "PATH$fieldSuffix" + '(?:per-guide(?!-worker)|single-reviewer)\b' -or
        $Text -match "(?im)$reportPrefix" + "(?:GUIDES|EXISTING_FEEDBACK_COVERAGE)$fieldSuffix"
}

function Assert-CompletedWorkerReportBoundary {
    param(
        [string]$Text,
        [string]$WorkerId,
        [string]$ExpectedGuide,
        [string[]]$AllGuides
    )

    if (Test-CoordinatorOutputText $Text) {
        Fail "Guide worker '$WorkerId' emitted coordinator aggregation output."
    }
    $reportedGuides = @($AllGuides | Where-Object {
        $Text.IndexOf($_, [StringComparison]::Ordinal) -ge 0
    })
    if ($reportedGuides.Count -eq 0) {
        return $false
    }
    if ($reportedGuides.Count -ne 1) {
        Fail "Guide worker '$WorkerId' named $($reportedGuides.Count) distinct guides instead of exactly one."
    }
    if ($reportedGuides[0] -cne $ExpectedGuide) {
        Fail "Guide worker '$WorkerId' named guide '$($reportedGuides[0])' instead of assigned guide '$ExpectedGuide'."
    }
    return $true
}

function Test-UsableResult {
    param([object]$Call)

    return $Call.ResultSuccess -eq $true -and
        -not [string]::IsNullOrWhiteSpace($Call.ResultText) -and
        $Call.ResultText -notmatch '(?i)file too large|output too large|truncat(?:ed|ion)|size warning'
}

function Get-ReportedTurns {
    param([string]$Text)

    $turns = [Collections.Generic.List[object]]::new()
    foreach ($match in [regex]::Matches(
        $Text,
        '(?is)\bTurn\s+(?<index>\d+)\b(?:(?!\bTurn\s+\d+\b).)*?(?<content>PATH:\s*per-guide-worker\b.*?)(?=\bTurn\s+\d+\b|\z)'
    )) {
        $content = $match.Groups['content'].Value.Trim()
        $payload = [regex]::Replace(
            $content,
            '(?is)^PATH:\s*per-guide-worker\b[\s.:-]*',
            ''
        ).Trim()
        $turns.Add([pscustomobject]@{
            Index = [int]$match.Groups['index'].Value
            Content = $content
            Payload = $payload
        })
    }
    return $turns.ToArray()
}

function Test-SuccessfulShellCall {
    param([object]$Call)

    if ($Call.ResultSuccess -ne $true) {
        return $false
    }
    $exitCode = $Call.Result?.PSObject.Properties['exitCode']?.Value
    return $null -eq $exitCode -or [int]$exitCode -eq 0
}

function Test-RunningRead {
    param([object]$Call)

    return (Get-ReadStatus $Call.ResultText) -eq 'running'
}

function Test-CompletedRead {
    param([object]$Call)

    return (Get-ReadStatus $Call.ResultText) -eq 'completed'
}

function Get-ReadStatus {
    param([string]$Text)

    $header = [regex]::Split($Text, '(?i)\bTurn\s+\d+\b', 2)[0]
    $match = [regex]::Match(
        $header,
        '(?i)\bstatus\s*["'']?\s*[:=]\s*["'']?(?<status>running|in[_ -]?progress|idle|completed|failed|cancelled)\b'
    )
    if (-not $match.Success) {
        $match = [regex]::Match(
            $header,
            '(?i)\bagent\s+is\s+(?:still\s+)?(?<status>running|in[_ -]?progress|idle|completed|failed|cancelled)\b'
        )
    }
    if (-not $match.Success) {
        return 'unknown'
    }
    $status = $match.Groups['status'].Value
    if ($status -match '^(?i:running|in[_ -]?progress)$') {
        return 'running'
    }
    if ($status -match '^(?i:idle|completed)$') {
        return 'completed'
    }
    return 'failed'
}

function Test-SameShellActivation {
    param([string]$Command)

    $prepare = [regex]::Match($Command, '(?is)dotnet\s+run\b.*?prepare-review\.cs')
    if (-not $prepare.Success) {
        return $false
    }
    $prefix = $Command.Substring(0, $prepare.Index)
    $powershell = $prefix -match '(?is)(?:^|[;&|]\s*)\.\s+["'']?[^;&|\r\n]*activate\.ps1["'']?\s*(?:;|&&)?\s*$'
    $posix = $prefix -match '(?is)(?:^|[;&|]\s*)(?:source|\.)\s+["'']?[^;&|\r\n]*activate\.sh["'']?\s*(?:;|&&)?\s*$'
    return $powershell -or $posix
}

function Test-PrepareCommand {
    param(
        [string]$Command,
        [int]$Pr,
        [string]$Head
    )

    $invocations = [regex]::Matches(
        $Command,
        '(?is)dotnet\s+run\b.*?prepare-review\.cs(?<arguments>.*?)(?=(?:\s*(?:;|&&|\|\|)\s*)|$)'
    )
    if ($invocations.Count -ne 1) {
        return $false
    }
    $arguments = $invocations[0].Groups['arguments'].Value
    $prMatches = [regex]::Matches(
        $arguments,
        "(?i)(?:^|\s)--pr(?:\s+|=)[`"']?(?<value>\d+)[`"']?(?=\s|$)"
    )
    $headMatches = [regex]::Matches(
        $arguments,
        "(?i)(?:^|\s)--head(?:\s+|=)[`"']?(?<value>[0-9a-f]{40})[`"']?(?=\s|$)"
    )
    return $prMatches.Count -eq 1 -and
        [int]$prMatches[0].Groups['value'].Value -eq $Pr -and
        $headMatches.Count -eq 1 -and
        $headMatches[0].Groups['value'].Value -eq $Head
}

function Get-Calls {
    param([object[]]$Events)

    $calls = [Collections.Generic.List[object]]::new()
    $byId = @{}
    for ($index = 0; $index -lt $Events.Count; $index++) {
        $event = $Events[$index]
        if ($event.type -eq 'tool_call') {
            $agentId = [string](
                $event.PSObject.Properties['agentId']?.Value ??
                $event.data.PSObject.Properties['agentId']?.Value
            )
            $call = [pscustomobject]@{
                CallIndex = $index
                ResultIndex = -1
                Name = [string]$event.data.toolName
                Id = [string]$event.data.toolCallId
                AgentId = $agentId
                Arguments = $event.data.arguments
                ResultSuccess = $null
                Result = $null
                ResultText = ''
            }
            $calls.Add($call)
            $byId["$agentId`:$($call.Id)"] = $call
            continue
        }
        if ($event.type -eq 'tool_result') {
            $id = [string]$event.data.toolCallId
            $agentId = [string](
                $event.PSObject.Properties['agentId']?.Value ??
                $event.data.PSObject.Properties['agentId']?.Value
            )
            $key = "$agentId`:$id"
            if ($byId.ContainsKey($key)) {
                $call = $byId[$key]
                $call.ResultIndex = $index
                $call.ResultSuccess = $event.data.success
                $call.Result = $event.data.result
                $call.ResultText = Convert-ToText $event.data.result
            }
        }
    }
    return $calls.ToArray()
}

function Test-SdkRecovery {
    param([object[]]$Calls)

    if ($ExpectedPr -le 0 -or
        $ExpectedHead -notmatch '^[0-9a-f]{40}$') {
        Fail 'The SDK recovery grader requires an expected PR number and immutable head.'
    }
    $rootCalls = @($Calls | Where-Object { [string]::IsNullOrEmpty($_.AgentId) })
    $shellCalls = @($rootCalls | Where-Object { Test-ShellTool $_.Name })
    $initial = $shellCalls |
        Where-Object {
            $command = [string](Get-Argument $_ 'command')
            $command -match '(?i)activate\.(ps1|sh)' -and
            (Test-SameShellActivation $command) -and
            (Test-PrepareCommand $command $ExpectedPr $ExpectedHead) -and
            $command -notmatch '(?i)eng[\\/]+common[\\/]+dotnet\.(ps1|sh)'
        } |
        Select-Object -First 1
    if ($null -eq $initial) {
        Fail 'No initial activate-and-prepare attempt was observed.'
    }
    if ($initial.ResultText -notmatch '(?i)SDK could not be found|has not been installed|not installed yet' -or
        $initial.ResultText -match '(?i)"?ready"?\s*[:=]\s*true' -or
        (Test-SuccessfulShellCall $initial)) {
        Fail 'The initial preparation attempt did not establish the naturally missing pinned SDK precondition.'
    }

    $recovery = $shellCalls |
        Where-Object {
            if ($_.CallIndex -le $initial.ResultIndex) {
                return $false
            }
            $command = [string](Get-Argument $_ 'command')
            return (Test-SameShellActivation $command) -and
                (Test-PrepareCommand $command $ExpectedPr $ExpectedHead) -and
                (Test-SuccessfulShellCall $_) -and
                $_.ResultText -match '(?i)"?ready"?\s*[:=]\s*true'
        } |
        Select-Object -First 1
    if ($null -eq $recovery) {
        Fail 'No same-invocation reactivation and pinned-head preparation was observed.'
    }

    $recoveryCommand = [string](Get-Argument $recovery 'command')
    $windowsRecovery = $recoveryCommand -match '(?i)activate\.ps1'
    $installPattern = if ($windowsRecovery) {
        '(?i)eng[\\/]common[\\/]dotnet\.ps1'
    } else {
        '(?i)eng/common/dotnet\.sh'
    }
    $combinedInstallPattern = if ($windowsRecovery) {
        '(?is)eng[\\/]common[\\/]dotnet\.ps1(?<between>.*?)activate\.ps1.*dotnet\s+run\b.*prepare-review\.cs'
    } else {
        '(?is)eng/common/dotnet\.sh(?<between>.*?)activate\.sh.*dotnet\s+run\b.*prepare-review\.cs'
    }
    $combinedMatch = [regex]::Match($recoveryCommand, $combinedInstallPattern)
    $combinedInstall = $combinedMatch.Success -and (
        $combinedMatch.Groups['between'].Value -match '&&' -or
        $combinedMatch.Groups['between'].Value -match
            '(?is)if\s*\(\s*\$LASTEXITCODE\s*-ne\s*0\s*\)\s*\{\s*exit\s+\$LASTEXITCODE\s*\}'
    )
    $priorInstall = $shellCalls |
        Where-Object {
            $_.CallIndex -gt $initial.ResultIndex -and
            $_.ResultIndex -lt $recovery.CallIndex -and
            [string](Get-Argument $_ 'command') -match $installPattern -and
            (Test-SuccessfulShellCall $_)
        } |
        Select-Object -Last 1
    if (-not $combinedInstall -and $null -eq $priorInstall) {
        Fail 'No successful supported SDK install preceded the pinned-head preparation.'
    }

    if ($recovery.ResultText -notmatch '"manifest"\s*:\s*"(?<manifest>(?:\\.|[^"])*)"') {
        Fail 'The ready preparation result did not identify its manifest.'
    }
    $preparedManifestPath = ('"' + $Matches.manifest + '"') | ConvertFrom-Json

    $manifestRead = $rootCalls |
        Where-Object {
            $targetMatch = [regex]::Match(
                $_.ResultText,
                '(?is)"target"\s*:\s*\{(?<target>.*?)\}'
            )
            $targetText = $targetMatch.Groups['target'].Value
            $_.CallIndex -gt $recovery.ResultIndex -and
            $_.Name -eq 'view' -and
            [string](Get-Argument $_ 'path') -eq $preparedManifestPath -and
            (Test-UsableResult $_) -and
            $_.ResultText -match '"version"\s*:\s*2' -and
            $_.ResultText -match '"ready"\s*:\s*true' -and
            $targetMatch.Success -and
            $targetText -match "(?is)`"pr`"\s*:\s*$ExpectedPr\b" -and
            $targetText -match "(?is)`"head`"\s*:\s*`"$([regex]::Escape($ExpectedHead))`""
        } |
        Select-Object -First 1
    if ($null -eq $manifestRead) {
        Fail 'The recovered preparation was not verified through its content-bearing ready version-2 manifest at the pinned PR head.'
    }

    Complete-Grading -Passed $true -Evidence 'Observed missing SDK, supported install, same-invocation reactivation, pinned-head preparation, and its ready version-2 manifest.'
}

function Test-SameWorkerLifecycle {
    param(
        [object]$GraderInput,
        [object[]]$Calls,
        [object[]]$Events
    )

    if ([string]::IsNullOrWhiteSpace($ContractPath)) {
        Fail 'The same-worker lifecycle grader requires an inventory path.'
    }
    if ($null -eq (Get-Command Assert-ReviewFixture -ErrorAction SilentlyContinue)) {
        Fail 'The trusted fixture-contract library is unavailable.'
    }
    $workDir = [IO.Path]::GetFullPath([string]$GraderInput.trajectory.workDir)
    try {
        $null = Assert-ReviewFixture `
            -BundleRoot (Join-Path $workDir '.review-bundle') `
            -ContractPath ([IO.Path]::GetFullPath($ContractPath, $workDir)) `
            -BundleName valid
    } catch {
        Fail "Trusted same-worker fixture validation failed: $($_.Exception.Message)"
    }

    $rootCalls = @($Calls | Where-Object { [string]::IsNullOrEmpty($_.AgentId) })
    $shellCalls = @($rootCalls | Where-Object { Test-ShellTool $_.Name })
    if ($shellCalls.Count -ne 0) {
        Fail 'A supplied-bundle lifecycle run used a shell tool.'
    }

    $tasks = @($rootCalls | Where-Object { $_.Name -eq 'task' })
    if ($tasks.Count -ne 1) {
        Fail "Expected exactly one worker dispatch; observed $($tasks.Count)."
    }
    $task = $tasks[0]
    if ((Get-Argument $task 'agent_type') -ne 'general-purpose' -or
        (Get-Argument $task 'model') -ne 'gpt-5.6-sol' -or
        (Get-Argument $task 'mode') -ne 'background') {
        Fail 'The sole worker was not dispatched as general-purpose / gpt-5.6-sol / background.'
    }
    if ($task.ResultSuccess -ne $true -or
        $task.ResultText -notmatch '(?i)agent[_ -]?id\s*[:=]\s*(?<id>[0-9a-f]{8}-[0-9a-f-]{27,})') {
        Fail 'The background dispatch did not return a concrete worker ID.'
    }
    $workerId = $Matches.id

    $writes = @($rootCalls | Where-Object { $_.Name -eq 'write_agent' })
    if ($writes.Count -ne 1) {
        Fail "Expected exactly one same-worker clarification; observed $($writes.Count)."
    }
    $clarificationMessage = [string](Get-Argument $writes[0] 'message')
    if ($clarificationMessage -notmatch '(?i)\b(candidate|finding|issue)\b' -or
        $clarificationMessage -notmatch '(?i)\b(base|target|old|previous)\b' -or
        $clarificationMessage -notmatch '(?i)\b(contract|behavior|implementation|code)\b') {
        Fail 'The same-worker message did not ask for the requested candidate clarification against the frozen base contract.'
    }
    $allReads = @($rootCalls | Where-Object { $_.Name -eq 'read_agent' })
    foreach ($call in @($allReads) + @($writes)) {
        if ((Get-Argument $call 'agent_id') -ne $workerId) {
            Fail 'A lifecycle call targeted a worker ID other than the runtime-returned dispatch ID.'
        }
        if ($call.ResultSuccess -ne $true) {
            Fail "The $($call.Name) call did not complete successfully."
        }
    }
    if (@($allReads | Where-Object { (Get-Argument $_ 'wait') -ne $true }).Count -ne 0) {
        Fail 'Every report collection attempt must use read_agent with wait:true.'
    }
    $reads = @(
        $rootCalls |
            Where-Object {
                $_.Name -eq 'read_agent' -and
                (Get-Argument $_ 'wait') -eq $true
            }
    )
    if ($reads.Count -lt 2) {
        Fail "Expected at least two report collection attempts with wait:true; observed $($reads.Count)."
    }
    $write = $writes[0]
    $preWriteReads = @(
        $reads |
            Where-Object {
                $_.CallIndex -gt $task.ResultIndex -and
                $_.ResultIndex -lt $write.CallIndex
            }
    )
    $postWriteReads = @(
        $reads |
            Where-Object { $_.CallIndex -gt $write.ResultIndex }
    )
    if ($preWriteReads.Count -eq 0 -or $postWriteReads.Count -eq 0 -or
        $preWriteReads.Count + $postWriteReads.Count -ne $reads.Count) {
        Fail 'The report collection attempts were not partitioned around the clarification.'
    }

    for ($index = 0; $index -lt $preWriteReads.Count - 1; $index++) {
        if (-not (Test-RunningRead $preWriteReads[$index])) {
            Fail 'A pre-clarification retry was not a nonterminal running snapshot.'
        }
    }
    $initialRead = $preWriteReads[-1]
    if (-not (Test-CompletedRead $initialRead)) {
        Fail 'The initial report collection did not reach a completed worker state.'
    }
    $initialTurns = @(Get-ReportedTurns $initialRead.ResultText)
    $initialSubstantiveTurns = @(
        $initialTurns |
            Where-Object { $_.Payload.Length -ge 20 }
    )
    if ($initialSubstantiveTurns.Count -eq 0) {
        Fail 'The initial read_agent result did not contain an actual completed worker report.'
    }
    $initialReportText = ($initialSubstantiveTurns.Payload -join "`n")
    $multipleCandidateReport =
        $initialReportText -match '(?i)\b(?:another|second|third|two|multiple|several)\s+(?:candidate|finding|issue)s?\b' -or
        $initialReportText -match '(?i)\bmore\s+than\s+one\s+(?:candidate|finding|issue)\b'
    $singleCandidateReport =
        -not $multipleCandidateReport -and
        $initialReportText -match '(?i)\b(?:one|single|only one)\s+(?:strongest\s+)?(?:candidate|finding|issue)\b'
    $selectedStrongestCandidate =
        $clarificationMessage -match '(?i)\b(strongest|top|highest|most[- ]serious)\s+(candidate|finding|issue)\b'
    if (-not $singleCandidateReport -and -not $selectedStrongestCandidate) {
        Fail 'The same-worker clarification did not unambiguously select the strongest candidate.'
    }
    $initialMaxTurn = (
        $initialSubstantiveTurns |
            Measure-Object Index -Maximum
    ).Maximum
    for ($index = 0; $index -lt $postWriteReads.Count - 1; $index++) {
        if (-not (Test-RunningRead $postWriteReads[$index])) {
            Fail 'A post-clarification retry was not a nonterminal running snapshot.'
        }
    }
    $followUpRead = $postWriteReads[-1]
    if (-not (Test-CompletedRead $followUpRead)) {
        Fail 'The follow-up report collection did not reach a completed worker state.'
    }
    $followUpSubstantiveTurns = @(
        Get-ReportedTurns $followUpRead.ResultText |
            Where-Object {
                $_.Payload.Length -ge 20 -and
                $_.Index -gt $initialMaxTurn
            }
    )
    if ($followUpSubstantiveTurns.Count -eq 0) {
        Fail 'The follow-up read_agent result did not contain an actual completed worker follow-up.'
    }
    $synthesisObserved = $false
    for ($index = $followUpRead.ResultIndex + 1; $index -lt $Events.Count; $index++) {
        $event = $Events[$index]
        $agentId = [string](
            $event.PSObject.Properties['agentId']?.Value ??
            $event.data.PSObject.Properties['agentId']?.Value
        )
        if ($event.type -eq 'assistant_message' -and
            [string]::IsNullOrEmpty($agentId)) {
            $synthesisObserved = $true
            break
        }
    }
    if (-not $synthesisObserved) {
        Fail 'Root synthesis occurred before the completed same-worker follow-up.'
    }

    Complete-Grading -Passed $true -Evidence 'Observed one background worker, an actual initial report, one clarification to the returned worker ID, and an actual follow-up report from that same ID.'
}

function Get-CanonicalCallPath {
    param(
        [object]$Call,
        [string]$WorkDir
    )

    $path = [string](
        (Get-Argument $Call 'path') ??
        (Get-Argument $Call 'filePath')
    )
    if ([string]::IsNullOrWhiteSpace($path)) {
        return ''
    }
    try {
        $separator = [IO.Path]::DirectorySeparatorChar
        $portable = $path.Replace('\', $separator).Replace('/', $separator)
        return [IO.Path]::GetFullPath($portable, $WorkDir)
    } catch {
        return ''
    }
}

function Test-ExplicitToolTruncation {
    param([string]$Text)

    return $Text -match '(?im)^\s*(?:\[\s*)?(?:output|result|content)\s+(?:was\s+)?truncated\b'
}

function Test-PromptContainsFixtureInput {
    param(
        [string]$Prompt,
        [string]$BundleRoot,
        [string[]]$RelativePaths
    )

    foreach ($relativePath in $RelativePaths) {
        $normalizedPath = $relativePath.Replace('\', '/')
        $fullPath = (Join-Path $BundleRoot (
            $normalizedPath.Replace('/', [IO.Path]::DirectorySeparatorChar)
        )).Replace('\', '/')
        $aliases = @($normalizedPath, ".review-bundle/$normalizedPath", $fullPath)
        foreach ($clause in Get-PromptClauses $Prompt) {
            if (-not (Test-ProhibitionClause $clause) -and
                @($aliases | Where-Object {
                    $clause.IndexOf(
                        $_,
                        [StringComparison]::OrdinalIgnoreCase
                    ) -ge 0
                }).Count -gt 0) {
                return $true
            }
        }

        $fullPath = Join-Path $BundleRoot (
            $normalizedPath.Replace('/', [IO.Path]::DirectorySeparatorChar)
        )
        if (Test-Path $fullPath -PathType Leaf) {
            $content = (Get-Content $fullPath -Raw -Encoding utf8).
                Replace("`r`n", "`n").
                Replace('\', '/').
                TrimEnd("`n")
            if (-not [string]::IsNullOrEmpty($content) -and
                $Prompt.IndexOf($content, [StringComparison]::Ordinal) -ge 0) {
                return $true
            }
        }
    }

    return $false
}

function Test-PromptContainsFixtureContent {
    param(
        [string]$Prompt,
        [string]$BundleRoot,
        [string]$RelativePath
    )

    $path = Join-Path $BundleRoot (
        $RelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar)
    )
    if (-not (Test-Path $path -PathType Leaf)) {
        return $false
    }

    $normalizedPrompt = $Prompt.Replace("`r`n", "`n").Replace('\', '/')
    $content = (Get-Content $path -Raw -Encoding utf8).
        Replace("`r`n", "`n").
        Replace('\', '/').
        TrimEnd("`n")
    if ([string]::IsNullOrEmpty($content)) {
        return $false
    }

    $lines = @($content -split "`n")
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = [string]$lines[$index]
        if ($line.Length -ge 24 -and
            $normalizedPrompt.IndexOf($line, [StringComparison]::Ordinal) -ge 0) {
            return $true
        }
        if ($index + 1 -lt $lines.Count) {
            $pair = "$line`n$($lines[$index + 1])"
            if ($pair.Length -ge 24 -and
                $normalizedPrompt.IndexOf($pair, [StringComparison]::Ordinal) -ge 0) {
                return $true
            }
        }
    }

    return $false
}

function Get-PromptClauses {
    param([string]$Prompt)

    return @(
        [regex]::Split(
            $Prompt.Replace("`r`n", "`n").Replace('\', '/'),
            '(?im)(?:\n|[;,]|\b(?:and|but|however|then)\b)'
        ) |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
}

function Test-ProhibitionClause {
    param([string]$Clause)

    return $Clause -match '(?i)\b(?:do not|don''t|must not|never|not assigned|coordinator[- ]only|coordinator owns|coordinator[- ]owned|exclude|prohibit|forbid|forbidden|skip|ignore|unavailable)\b'
}

function Test-PromptRequestsFixtureInput {
    param(
        [string]$Prompt,
        [string]$BundleRoot,
        [string]$RelativePath
    )

    $normalizedPath = $RelativePath.Replace('\', '/')
    $fullPath = (Join-Path $BundleRoot (
        $normalizedPath.Replace('/', [IO.Path]::DirectorySeparatorChar)
    )).Replace('\', '/')
    $aliases = @(
        $normalizedPath,
        ".review-bundle/$normalizedPath",
        $fullPath
    )
    foreach ($clause in Get-PromptClauses $Prompt) {
        if (@($aliases | Where-Object {
            $clause.IndexOf($_, [StringComparison]::OrdinalIgnoreCase) -ge 0
        }).Count -eq 0) {
            continue
        }
        if (Test-ProhibitionClause $clause) {
            continue
        }
        return $true
    }

    return $false
}

function Get-ApplicablePoliciesForGuide {
    param(
        [object]$Guide,
        [object[]]$Policies,
        [string]$BundleRoot
    )

    $guidePath = [string]$Guide.path
    $guideFile = Join-Path $BundleRoot (
        "guidance/$guidePath.source".Replace(
            '/',
            [IO.Path]::DirectorySeparatorChar
        )
    )
    $guideContent = Get-Content $guideFile -Raw -Encoding utf8
    return @($Policies | Where-Object {
        $link = "$([IO.Path]::GetFileName([string]$_.path))#$([string]$_.anchor)"
        $guideContent.IndexOf($link, [StringComparison]::Ordinal) -ge 0
    })
}

function Get-ExactViewCoverage {
    param(
        [object[]]$Calls,
        [object]$FileContract,
        [string]$BundleRoot,
        [string]$WorkDir
    )

    $fixturePath = [IO.Path]::GetFullPath(
        (Join-Path $BundleRoot ([string]$FileContract.path).Replace(
            '/',
            [IO.Path]::DirectorySeparatorChar
        ))
    )
    $matching = @(
        $Calls |
            Where-Object {
                $_.Name -eq 'view' -and
                (Get-CanonicalCallPath $_ $WorkDir) -ieq $fixturePath
            }
    )
    if ($matching.Count -eq 0) {
        return [pscustomobject]@{
            Passed = $false
            Evidence = "No coordinator native view call read $($FileContract.path)."
            LastResultIndex = -1
        }
    }

    $expectedLines = @(
        Get-Content $fixturePath -Encoding utf8
    )
    $observed = @{}
    $lastResultIndex = -1
    foreach ($call in $matching) {
        if ($call.ResultSuccess -ne $true -or
            [string]::IsNullOrWhiteSpace($call.ResultText)) {
            continue
        }
        $lineMatches = @(
            [regex]::Matches(
                $call.ResultText,
                '(?m)^(?:L(?<line>\d+):|(?<line>\d+)\.\s?)(?<content>.*)$'
            )
        )
        if ($lineMatches.Count -eq 0) {
            continue
        }
        if (Test-ExplicitToolTruncation $call.ResultText) {
            $lastLine = (
                $lineMatches |
                    ForEach-Object { [int]$_.Groups['line'].Value } |
                    Measure-Object -Maximum
            ).Maximum
            $lineMatches = @(
                $lineMatches |
                    Where-Object {
                        [int]$_.Groups['line'].Value -ne $lastLine
                    }
            )
        }
        foreach ($lineMatch in $lineMatches) {
            $line = [int]$lineMatch.Groups['line'].Value
            $content = $lineMatch.Groups['content'].Value
            if ($line -lt 1 -or $line -gt $expectedLines.Count) {
                return [pscustomobject]@{
                    Passed = $false
                    Evidence = "A native read returned out-of-range line $line for $($FileContract.path)."
                    LastResultIndex = $lastResultIndex
                }
            }
            $expected = [string]$expectedLines[$line - 1]
            if ($content -cne $expected) {
                return [pscustomobject]@{
                    Passed = $false
                    Evidence = "A native read returned content different from trusted fixture line $line of $($FileContract.path)."
                    LastResultIndex = $lastResultIndex
                }
            }
            $observed[$line] = $content
        }
        $lastResultIndex = [Math]::Max($lastResultIndex, $call.ResultIndex)
    }

    for ($line = 1; $line -le $expectedLines.Count; $line++) {
        if (-not $observed.ContainsKey($line)) {
            return [pscustomobject]@{
                Passed = $false
                Evidence = "Native reads left an actual returned-line gap in $($FileContract.path) at line $line."
                LastResultIndex = $lastResultIndex
            }
        }
    }
    return [pscustomobject]@{
        Passed = $true
        Evidence = ''
        LastResultIndex = $lastResultIndex
    }
}

function Test-ReadyV2Transport {
    param(
        [object]$GraderInput,
        [object[]]$Calls,
        [object[]]$Events
    )

    if ([string]::IsNullOrWhiteSpace($ContractPath)) {
        Fail 'The ready-v2 transport grader requires an inventory path.'
    }
    if ($null -eq (Get-Command Assert-ReviewFixture -ErrorAction SilentlyContinue)) {
        Fail 'The trusted fixture-contract library is unavailable.'
    }
    if (@($Calls | Where-Object { Test-ShellTool $_.Name }).Count -ne 0) {
        Fail 'A supplied-bundle transport run used a shell tool.'
    }

    $workDir = [IO.Path]::GetFullPath([string]$GraderInput.trajectory.workDir)
    $contractFile = [IO.Path]::GetFullPath($ContractPath, $workDir)
    $bundleRoot = [IO.Path]::GetFullPath(
        (Join-Path $workDir '.review-bundle')
    )
    try {
        $null = Assert-ReviewFixture `
            -BundleRoot $bundleRoot `
            -ContractPath $contractFile `
            -BundleName valid
    } catch {
        Fail "Trusted ready-v2 fixture validation failed: $($_.Exception.Message)"
    }
    $inventory = Get-Content $contractFile -Raw -Encoding utf8 |
        ConvertFrom-Json -Depth 100
    $rootCalls = @(
        $Calls |
            Where-Object { [string]::IsNullOrEmpty($_.AgentId) }
    )
    $workerCalls = @(
        $Calls |
            Where-Object { -not [string]::IsNullOrEmpty($_.AgentId) }
    )
    Assert-WorkerToolBoundary $workerCalls
    $coordinatorOnlyEntries = @(
        $inventory.files |
            Where-Object { 'worker' -notin @($_.readRoles) }
    )
    foreach ($call in $workerCalls) {
        if ($call.Name -in @('view', 'read_file')) {
            $path = Get-CanonicalCallPath $call $workDir
            if ([string]::IsNullOrEmpty($path)) {
                continue
            }
            foreach ($entry in $coordinatorOnlyEntries) {
                $fixturePath = [IO.Path]::GetFullPath(
                    (Join-Path $bundleRoot ([string]$entry.path).Replace(
                        '/',
                        [IO.Path]::DirectorySeparatorChar
                    ))
                )
                $pathPrefix = $path.TrimEnd(
                    [IO.Path]::DirectorySeparatorChar,
                    [IO.Path]::AltDirectorySeparatorChar
                ) + [IO.Path]::DirectorySeparatorChar
                if ($path -ieq $fixturePath -or
                    $fixturePath.StartsWith(
                        $pathPrefix,
                        [StringComparison]::OrdinalIgnoreCase
                    )) {
                    Fail "A worker read coordinator-only fixture input '$($entry.path)'."
                }
            }
        } elseif ($call.Name -in @('rg', 'glob')) {
            $rawPaths = Get-Argument $call 'paths'
            $searchPaths = if ($null -eq $rawPaths) {
                @($workDir)
            } elseif ($rawPaths -is [string]) {
                @([string]$rawPaths)
            } else {
                $resolvedPaths = @($rawPaths | ForEach-Object { [string]$_ })
                if ($resolvedPaths.Count -eq 0) {
                    @($workDir)
                } else {
                    $resolvedPaths
                }
            }
            foreach ($searchPath in $searchPaths) {
                try {
                    $separator = [IO.Path]::DirectorySeparatorChar
                    $portable = $searchPath.Replace('\', $separator).Replace('/', $separator)
                    $canonicalSearchPath = [IO.Path]::GetFullPath($portable, $workDir)
                } catch {
                    Fail "A worker rg call used an invalid search path '$searchPath'."
                }
                $searchPrefix = $canonicalSearchPath.TrimEnd(
                    [IO.Path]::DirectorySeparatorChar,
                    [IO.Path]::AltDirectorySeparatorChar
                ) + [IO.Path]::DirectorySeparatorChar
                foreach ($entry in $coordinatorOnlyEntries) {
                    $fixturePath = [IO.Path]::GetFullPath(
                        (Join-Path $bundleRoot ([string]$entry.path).Replace(
                            '/',
                            [IO.Path]::DirectorySeparatorChar
                        ))
                    )
                    if ($canonicalSearchPath -ieq $fixturePath -or
                        $fixturePath.StartsWith(
                            $searchPrefix,
                            [StringComparison]::OrdinalIgnoreCase
                        )) {
                        Fail "A worker $($call.Name) scope could read coordinator-only fixture input '$($entry.path)'."
                    }
                }
            }
        }
    }

    $tasks = @($rootCalls | Where-Object { $_.Name -eq 'task' })
    $manifest = Get-Content (Join-Path $bundleRoot 'manifest.json') -Raw -Encoding utf8 |
        ConvertFrom-Json -Depth 100
    $files = @(Get-Content (Join-Path $bundleRoot 'files.json') -Raw -Encoding utf8 |
        ConvertFrom-Json -Depth 100)
    $expectedWorkerCount = @($manifest.guides).Count
    if ($tasks.Count -ne $expectedWorkerCount) {
        Fail "Expected exactly $expectedWorkerCount prescribed guide worker dispatch, but observed $($tasks.Count)."
    }
    $assignedGuides = @{}
    $workerGuideById = @{}
    foreach ($task in $tasks) {
        if ([string](Get-Argument $task 'agent_type') -cne 'general-purpose' -or
            [string](Get-Argument $task 'model') -cne 'gpt-5.6-sol' -or
            [string](Get-Argument $task 'mode') -cne 'background') {
            Fail 'A prescribed guide worker was not configured as background general-purpose gpt-5.6-sol.'
        }
        $workerPrompt = ([string](Get-Argument $task 'prompt')).Replace('\', '/')
        if ([string]::IsNullOrWhiteSpace($workerPrompt)) {
            Fail 'A prescribed guide worker was dispatched without its required inputs.'
        }
        foreach ($entry in $coordinatorOnlyEntries) {
            $relativePath = [string]$entry.path
            if ((Test-PromptContainsFixtureContent `
                -Prompt $workerPrompt `
                -BundleRoot $bundleRoot `
                -RelativePath $relativePath) -or
                (Test-PromptRequestsFixtureInput `
                -Prompt $workerPrompt `
                -BundleRoot $bundleRoot `
                -RelativePath $relativePath)) {
                Fail "A worker prompt embedded or requested coordinator-only fixture content '$relativePath'."
            }
        }

        $matchingGuides = @($manifest.guides | Where-Object {
            $guidePath = [string]$_.path
            Test-PromptContainsFixtureInput `
                -Prompt $workerPrompt `
                -BundleRoot $bundleRoot `
                -RelativePaths @($guidePath, "guidance/$guidePath.source")
        })
        if ($matchingGuides.Count -ne 1) {
            Fail "A worker prompt matched $($matchingGuides.Count) assigned guides instead of exactly one."
        }
        $guide = $matchingGuides[0]
        $guidePath = [string]$guide.path
        if ($assignedGuides.ContainsKey($guidePath)) {
            Fail "Guide '$guidePath' was assigned to more than one worker task."
        }
        $assignedGuides[$guidePath] = $task
        if ($task.ResultSuccess -ne $true -or
            $task.ResultText -notmatch '(?i)agent[_ -]?id\s*[:=]\s*(?<id>[0-9a-f]{8}-[0-9a-f-]{27,})') {
            Fail "The '$guidePath' background dispatch did not return a concrete worker ID."
        }
        $workerId = $Matches.id
        if ($workerGuideById.ContainsKey($workerId)) {
            Fail "Worker '$workerId' was assigned more than one guide."
        }
        $workerGuideById[$workerId] = $guidePath

        $applicablePolicies = @(
            Get-ApplicablePoliciesForGuide `
                -Guide $guide `
                -Policies @($manifest.policies) `
                -BundleRoot $bundleRoot
        )
        foreach ($policy in $applicablePolicies) {
            $policyPath = [string]$policy.path
            $policyBodyPath = [string]$policy.body.path
            if (-not (Test-PromptContainsFixtureInput `
                -Prompt $workerPrompt `
                -BundleRoot $bundleRoot `
                -RelativePaths @(
                    $policyPath,
                    "guidance/$policyPath.source",
                    $policyBodyPath
                ))) {
                Fail "The '$guidePath' worker prompt omitted applicable policy '$policyPath'."
            }
        }
        foreach ($policy in @($manifest.policies | Where-Object {
            [string]$_.path -notin @($applicablePolicies | ForEach-Object {
                [string]$_.path
            })
        })) {
            $policyPath = [string]$policy.path
            if (Test-PromptContainsFixtureInput `
                -Prompt $workerPrompt `
                -BundleRoot $bundleRoot `
                -RelativePaths @(
                    $policyPath,
                    "guidance/$policyPath.source",
                    [string]$policy.body.path
                )) {
                Fail "The '$guidePath' worker prompt included non-applicable policy '$policyPath'."
            }
        }
        foreach ($inputPath in @('files.json', 'diff.patch')) {
            if (-not (Test-PromptContainsFixtureInput `
                -Prompt $workerPrompt `
                -BundleRoot $bundleRoot `
                -RelativePaths @($inputPath))) {
                Fail "The '$guidePath' worker prompt omitted required worker input '$inputPath'."
            }
        }
        foreach ($source in @(
            $manifest.sources.head,
            $manifest.sources.mergeBase,
            $manifest.sources.baseTip
        )) {
            $sourceRoot = [string]$source.root
            if (-not (Test-PromptContainsFixtureInput `
                -Prompt $workerPrompt `
                -BundleRoot $bundleRoot `
                -RelativePaths @($sourceRoot))) {
                Fail "The '$guidePath' worker prompt omitted frozen source root '$sourceRoot'."
            }
        }
        foreach ($file in $files) {
            $filename = [string]$file.filename
            if (-not (Test-PromptContainsFixtureInput `
                -Prompt $workerPrompt `
                -BundleRoot $bundleRoot `
                -RelativePaths @($filename))) {
                Fail "The '$guidePath' worker prompt omitted relevant changed source '$filename'."
            }
        }
    }
    foreach ($guide in @($manifest.guides)) {
        if (-not $assignedGuides.ContainsKey([string]$guide.path)) {
            Fail "Guide '$([string]$guide.path)' was not assigned to a worker task."
        }
    }
    foreach ($call in $workerCalls) {
        if (-not $workerGuideById.ContainsKey($call.AgentId)) {
            Fail "Observed worker tool activity for unassigned worker '$($call.AgentId)'."
        }
    }
    foreach ($event in $Events) {
        $agentId = [string](
            $event.PSObject.Properties['agentId']?.Value ??
            $event.data.PSObject.Properties['agentId']?.Value
        )
        if ($event.type -eq 'assistant_message' -and
            -not [string]::IsNullOrEmpty($agentId)) {
            if (-not $workerGuideById.ContainsKey($agentId)) {
                Fail "Observed worker output for unassigned worker '$agentId'."
            }
            if (Test-CoordinatorOutputText ([string]$event.data.content)) {
                Fail "Guide worker '$agentId' emitted coordinator aggregation output."
            }
        }
    }
    foreach ($call in $rootCalls | Where-Object { $_.Name -in @('read_agent', 'write_agent') }) {
        $workerId = [string](Get-Argument $call 'agent_id')
        if (-not $workerGuideById.ContainsKey($workerId)) {
            Fail "Coordinator agent lifecycle call targeted unassigned worker '$workerId'."
        }
    }
    $completedWorkerReports = @{}
    foreach ($call in $rootCalls | Where-Object { $_.Name -eq 'read_agent' }) {
        $workerId = [string](Get-Argument $call 'agent_id')
        if ((Get-Argument $call 'wait') -ne $true) {
            Fail "Guide worker '$workerId' report collection did not use read_agent with wait:true."
        }
        if ($call.ResultSuccess -ne $true) {
            Fail "Guide worker '$workerId' report collection did not complete successfully."
        }
        if (Test-CoordinatorOutputText $call.ResultText) {
            Fail "Guide worker '$workerId' emitted coordinator aggregation output."
        }
        $readStatus = Get-ReadStatus $call.ResultText
        if ($readStatus -eq 'failed' -or $readStatus -eq 'unknown') {
            Fail "Guide worker '$workerId' report collection returned status '$readStatus'."
        }
        if ($readStatus -eq 'completed') {
            $namedAssignedGuide = Assert-CompletedWorkerReportBoundary `
                -Text $call.ResultText `
                -WorkerId $workerId `
                -ExpectedGuide ([string]$workerGuideById[$workerId]) `
                -AllGuides @($manifest.guides | ForEach-Object { [string]$_.path })
            if ($namedAssignedGuide) {
                $completedWorkerReports[$workerId] = $true
            }
        }
    }
    foreach ($workerId in $workerGuideById.Keys) {
        if (-not $completedWorkerReports.ContainsKey($workerId)) {
            Fail "Guide worker '$workerId' had no completed collected report."
        }
    }
    foreach ($call in $workerCalls | Where-Object {
        $_.Name -in @('view', 'read_file') -and
        $workerGuideById.ContainsKey($_.AgentId)
    }) {
        $path = Get-CanonicalCallPath $call $workDir
        if ([string]::IsNullOrEmpty($path)) {
            continue
        }
        $assignedGuide = [string]$workerGuideById[$call.AgentId]
        foreach ($guide in @($manifest.guides | Where-Object {
            [string]$_.path -cne $assignedGuide
        })) {
            $otherGuidePath = [IO.Path]::GetFullPath(
                (Join-Path $bundleRoot "guidance/$([string]$guide.path).source").Replace(
                    '/',
                    [IO.Path]::DirectorySeparatorChar
                )
            )
            $pathPrefix = $path.TrimEnd(
                [IO.Path]::DirectorySeparatorChar,
                [IO.Path]::AltDirectorySeparatorChar
            ) + [IO.Path]::DirectorySeparatorChar
            if ($path -ieq $otherGuidePath -or
                $otherGuidePath.StartsWith(
                    $pathPrefix,
                    [StringComparison]::OrdinalIgnoreCase
                )) {
                Fail "Guide worker '$($call.AgentId)' read unassigned guide '$([string]$guide.path)'."
            }
        }
    }
    foreach ($call in $workerCalls | Where-Object {
        $_.Name -in @('rg', 'glob') -and
        $workerGuideById.ContainsKey($_.AgentId)
    }) {
        $rawPaths = Get-Argument $call 'paths'
        $searchPaths = if ($null -eq $rawPaths) {
            @($workDir)
        } elseif ($rawPaths -is [string]) {
            @([string]$rawPaths)
        } else {
            $resolvedPaths = @($rawPaths | ForEach-Object { [string]$_ })
            if ($resolvedPaths.Count -eq 0) {
                @($workDir)
            } else {
                $resolvedPaths
            }
        }
        $assignedGuide = [string]$workerGuideById[$call.AgentId]
        foreach ($searchPath in $searchPaths) {
            try {
                $separator = [IO.Path]::DirectorySeparatorChar
                $portable = $searchPath.Replace('\', $separator).Replace('/', $separator)
                $canonicalSearchPath = [IO.Path]::GetFullPath($portable, $workDir)
            } catch {
                Fail "A worker rg call used an invalid search path '$searchPath'."
            }
            $searchPrefix = $canonicalSearchPath.TrimEnd(
                [IO.Path]::DirectorySeparatorChar,
                [IO.Path]::AltDirectorySeparatorChar
            ) + [IO.Path]::DirectorySeparatorChar
            foreach ($guide in @($manifest.guides | Where-Object {
                [string]$_.path -cne $assignedGuide
            })) {
                $otherGuidePath = [IO.Path]::GetFullPath(
                    (Join-Path $bundleRoot "guidance/$([string]$guide.path).source").Replace(
                        '/',
                        [IO.Path]::DirectorySeparatorChar
                    )
                )
                if ($canonicalSearchPath -ieq $otherGuidePath -or
                    $otherGuidePath.StartsWith(
                        $searchPrefix,
                        [StringComparison]::OrdinalIgnoreCase
                    )) {
                    Fail "Guide worker '$($call.AgentId)' $($call.Name) scope included unassigned guide '$([string]$guide.path)'."
                }
            }
        }
    }
    $firstTaskIndex = ($tasks | Measure-Object CallIndex -Minimum).Minimum
    $synthesisIndex = $Events.Count
    for ($index = 0; $index -lt $Events.Count; $index++) {
        $event = $Events[$index]
        $agentId = [string](
            $event.PSObject.Properties['agentId']?.Value ??
            $event.data.PSObject.Properties['agentId']?.Value
        )
        if ($event.type -eq 'assistant_message' -and
            [string]::IsNullOrEmpty($agentId)) {
            $synthesisIndex = $index
        }
    }
    $coverageByPath = @{}
    foreach ($entry in $inventory.files | Where-Object coordinatorRequired) {
        $coverage = Get-ExactViewCoverage `
            -Calls $rootCalls `
            -FileContract $entry `
            -BundleRoot $bundleRoot `
            -WorkDir $workDir
        if (-not $coverage.Passed) {
            Fail $coverage.Evidence
        }
        if ($coverage.LastResultIndex -ge $synthesisIndex) {
            Fail "The coordinator read $($entry.path) after root synthesis."
        }
        $coverageByPath[[string]$entry.path] = $coverage
    }

    foreach ($preDispatchPath in @(
        'manifest.json',
        'guidance/.github/skills/review-pull-request/routing.md.source'
    )) {
        if ($coverageByPath[$preDispatchPath].LastResultIndex -ge $firstTaskIndex) {
            Fail "The coordinator did not verify $preDispatchPath before routing the guide worker."
        }
    }

    Complete-Grading -Passed $true -Evidence (
        'Observed a trusted ready-v2 fixture, exact coordinator reads through EOF, ' +
        'valid role separation, routed dispatch, and no shell fallback.'
    )
}

function Test-RequiredBytesBlocked {
    param(
        [object]$GraderInput,
        [object[]]$Calls,
        [object[]]$Events
    )

    if ([string]::IsNullOrWhiteSpace($ContractPath)) {
        Fail 'The unavailable-byte grader requires an inventory path.'
    }
    if ($null -eq (Get-Command Assert-ReviewFixture -ErrorAction SilentlyContinue)) {
        Fail 'The trusted fixture-contract library is unavailable.'
    }
    $rootCalls = @($Calls | Where-Object { [string]::IsNullOrEmpty($_.AgentId) })
    if (@($rootCalls | Where-Object { Test-ShellTool $_.Name }).Count -ne 0) {
        Fail 'The unavailable-byte path attempted a shell fallback.'
    }
    if (@($rootCalls | Where-Object { $_.Name -eq 'task' }).Count -ne 0) {
        Fail 'A worker was dispatched despite unavailable required bundle bytes.'
    }
    $unsupportedCalls = @(
        $rootCalls |
            Where-Object { $_.Name -notin @('view', 'rg', 'glob') }
    )
    if ($unsupportedCalls.Count -ne 0) {
        Fail "The unavailable-byte path used unsupported fallback tool '$($unsupportedCalls[0].Name)'."
    }

    $workDir = [IO.Path]::GetFullPath([string]$GraderInput.trajectory.workDir)
    $contractFile = [IO.Path]::GetFullPath($ContractPath, $workDir)
    try {
        $null = Assert-ReviewFixture `
            -BundleRoot (Join-Path $workDir '.review-bundle') `
            -ContractPath $contractFile `
            -BundleName missingFeedback
    } catch {
        Fail "Trusted missing-feedback fixture validation failed: $($_.Exception.Message)"
    }
    $manifestPath = [IO.Path]::GetFullPath(
        (Join-Path $workDir '.review-bundle/manifest.json')
    )
    $feedbackPath = [IO.Path]::GetFullPath(
        (Join-Path $workDir '.review-bundle/feedback.json')
    )
    $manifestRead = $rootCalls |
        Where-Object {
            $_.Name -eq 'view' -and
            -not [string]::IsNullOrWhiteSpace([string](Get-Argument $_ 'path')) -and
            [IO.Path]::GetFullPath(
                [string](Get-Argument $_ 'path'),
                $workDir
            ) -eq $manifestPath -and
            (Test-UsableResult $_) -and
            $_.ResultText -match '"version"\s*:\s*2' -and
            $_.ResultText -match '"ready"\s*:\s*true'
        } |
        Select-Object -First 1
    if ($null -eq $manifestRead) {
        Fail 'The unavailable-byte path did not first read the exact ready version-2 manifest.'
    }
    $inventory = Get-Content $contractFile -Raw -Encoding utf8 |
        ConvertFrom-Json -Depth 100
    $manifestCoverage = Get-ExactViewCoverage `
        -Calls $rootCalls `
        -FileContract ($inventory.files | Where-Object path -CEQ 'manifest.json') `
        -BundleRoot (Join-Path $workDir '.review-bundle') `
        -WorkDir $workDir
    if (-not $manifestCoverage.Passed) {
        Fail "The unavailable-byte path did not completely read the trusted manifest: $($manifestCoverage.Evidence)"
    }

    $feedbackReads = @(
        $rootCalls |
            Where-Object {
                $_.Name -eq 'view' -and
                -not [string]::IsNullOrWhiteSpace([string](Get-Argument $_ 'path')) -and
                [IO.Path]::GetFullPath(
                    [string](Get-Argument $_ 'path'),
                    $workDir
                ) -eq $feedbackPath
            }
    )
    if ($feedbackReads.Count -eq 0) {
        Fail 'The unavailable required feedback input was never read.'
    }
    if (@(
        $feedbackReads |
            Where-Object { $_.CallIndex -le $manifestCoverage.LastResultIndex }
    ).Count -ne 0) {
        Fail 'The exact required feedback input was attempted before the ready manifest read completed.'
    }
    $failedRead = $feedbackReads |
        Where-Object {
            $_.CallIndex -gt $manifestCoverage.LastResultIndex -and
            $_.ResultSuccess -eq $false
        } |
        Select-Object -First 1
    if ($null -eq $failedRead) {
        Fail 'The feedback read did not establish that the required bytes were unavailable.'
    }
    if (@($feedbackReads | Where-Object { $_.ResultSuccess -eq $true }).Count -ne 0) {
        Fail 'The exact required feedback input was successfully read after an earlier failure.'
    }
    if (Test-Path $feedbackPath) {
        Fail 'The blocked fixture unexpectedly contains the required feedback input.'
    }
    if ([string]$GraderInput.trajectory.output -notmatch '(?im)^\s*STATUS:\s*BLOCKED\b') {
        Fail 'Unavailable required bytes did not produce STATUS: BLOCKED.'
    }
    $blockedMessageObserved = $false
    for ($index = $failedRead.ResultIndex + 1; $index -lt $Events.Count; $index++) {
        $event = $Events[$index]
        $agentId = [string](
            $event.PSObject.Properties['agentId']?.Value ??
            $event.data.PSObject.Properties['agentId']?.Value
        )
        if ($event.type -eq 'assistant_message' -and
            [string]::IsNullOrEmpty($agentId) -and
            [string]$event.data.content -match '(?im)^\s*STATUS:\s*BLOCKED\b') {
            $blockedMessageObserved = $true
            break
        }
    }
    if (-not $blockedMessageObserved) {
        Fail 'No root assistant message reported STATUS: BLOCKED after the failed required read.'
    }

    Complete-Grading -Passed $true -Evidence 'Observed a failed native required-input read, no fallback or worker dispatch, and an explicit BLOCKED result.'
}

try {
    if ([string]::IsNullOrWhiteSpace($env:EVALUATE_GRADER_INPUT)) {
        throw 'EVALUATE_GRADER_INPUT is not set.'
    }
    $graderInput = Get-Content $env:EVALUATE_GRADER_INPUT -Raw -Encoding utf8 |
        ConvertFrom-Json -Depth 100
    $events = @($graderInput.trajectory.events)
    $calls = @(Get-Calls $events)
    switch ($Contract) {
        'sdk-recovery' { Test-SdkRecovery $calls }
        'same-worker-lifecycle' { Test-SameWorkerLifecycle $graderInput $calls $events }
        'ready-v2-transport' { Test-ReadyV2Transport $graderInput $calls $events }
        'required-bytes-blocked' { Test-RequiredBytesBlocked $graderInput $calls $events }
    }
} catch {
    Complete-Grading -Passed $false -Evidence "Trace grader error: $($_.Exception.Message)" -Status 'error'
}
