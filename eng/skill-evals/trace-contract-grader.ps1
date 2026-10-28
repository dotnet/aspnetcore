#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(
        'sdk-recovery',
        'same-worker-lifecycle',
        'ready-v2-transport',
        'required-bytes-blocked',
        'native-worker-scope'
    )]
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
    param(
        [object[]]$WorkerCalls,
        [string[]]$ExpectedWorkerIds = @(),
        [switch]$RequireSkill
    )

    if ($RequireSkill) {
        foreach ($workerId in $ExpectedWorkerIds) {
            if (@($WorkerCalls | Where-Object {
                [string]$_.AgentId -ceq $workerId
            }).Count -eq 0) {
                Fail "Guide worker '$workerId' did not invoke review-pull-request exactly once."
            }
        }
    }

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
        if ($RequireSkill -and $skillCalls.Count -eq 0) {
            Fail "Guide worker '$($group.Name)' did not invoke review-pull-request exactly once."
        }
        if ($skillCalls.Count -eq 1 -and $skillCalls[0].ResultSuccess -ne $true) {
            Fail "Guide worker '$($group.Name)' did not successfully invoke review-pull-request."
        }
        if ($skillCalls.Count -eq 1 -and
            $skillCalls[0].CallIndex -ne $ordered[0].CallIndex) {
            Fail "Guide worker '$($group.Name)' did not use its review-pull-request load as its first tool call."
        }
        foreach ($call in $ordered | Where-Object { $_.Name -ine 'skill' }) {
            $behavior = Get-WorkerCoordinatorBehavior $call
            if (-not [string]::IsNullOrEmpty($behavior)) {
                Fail "Guide worker '$($group.Name)' used coordinator-only $behavior through '$($call.Name)'."
            }
            if ($call.Name -inotmatch '^(view|read_file|rg|grep|glob|bash|shell|powershell)$') {
                Fail "Guide worker '$($group.Name)' used unsupported worker tool '$($call.Name)'."
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
    if ($Text -notmatch '(?im)(?:^|\bTurn\s+\d+\b[^:\r\n]*:\s*)\s*(?:[-+*]\s+)?(?:\*\*|__)?PATH(?:\*\*|__)?\s*:(?:\*\*|__)?\s*per-guide-worker\b') {
        Fail "Guide worker '$WorkerId' did not report PATH: per-guide-worker."
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
        '(?is)\bTurn\s+(?<index>\d+)\b(?:(?!\bTurn\s+\d+\b).)*?(?<content>(?:\*\*|__)?PATH(?:\*\*|__)?\s*:(?:\*\*|__)?\s*per-guide-worker\b.*?)(?=\bTurn\s+\d+\b|\z)'
    )) {
        $content = $match.Groups['content'].Value.Trim()
        $payload = [regex]::Replace(
            $content,
            '(?is)^(?:\*\*|__)?PATH(?:\*\*|__)?\s*:(?:\*\*|__)?\s*per-guide-worker\b[\s.:-]*',
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

function Convert-NativeEvents {
    param([object[]]$Events)

    return @(
        foreach ($event in $Events) {
            switch ([string]$event.type) {
                'tool.execution_start' {
                    [pscustomobject]@{
                        type = 'tool_call'
                        agentId = [string](Get-PropertyValue $event 'agentId')
                        data = [pscustomobject]@{
                            toolName = [string](Get-PropertyValue $event.data 'toolName')
                            toolCallId = [string](Get-PropertyValue $event.data 'toolCallId')
                            arguments = Get-PropertyValue $event.data 'arguments'
                        }
                    }
                    continue
                }
                'tool.execution_complete' {
                    [pscustomobject]@{
                        type = 'tool_result'
                        agentId = [string](Get-PropertyValue $event 'agentId')
                        data = [pscustomobject]@{
                            toolName = [string](Get-PropertyValue $event.data 'toolName')
                            toolCallId = [string](Get-PropertyValue $event.data 'toolCallId')
                            success = Get-PropertyValue $event.data 'success'
                            result = Get-PropertyValue $event.data 'result'
                        }
                    }
                    continue
                }
                'assistant.message' {
                    [pscustomobject]@{
                        type = 'assistant_message'
                        agentId = [string](Get-PropertyValue $event 'agentId')
                        data = [pscustomobject]@{
                            content = [string](Get-PropertyValue $event.data 'content')
                        }
                    }
                    continue
                }
                default {
                    $event
                }
            }
        }
    )
}

function Get-PropertyValue {
    param(
        [object]$Object,
        [string]$Name
    )

    if ($null -eq $Object) {
        return $null
    }
    foreach ($property in $Object.PSObject.Properties) {
        if ($property.Name -ceq $Name) {
            return $property.Value
        }
    }
    return $null
}

function Test-Property {
    param(
        [object]$Object,
        [string]$Name
    )

    if ($null -eq $Object) {
        return $false
    }
    return @($Object.PSObject.Properties |
        Where-Object Name -CEQ $Name).Count -eq 1
}

function Assert-NativeEventContract {
    param([object[]]$Events)

    $starts = @{}
    $completions = @{}
    foreach ($event in $Events) {
        $type = [string]$event.type
        $agentId = [string](Get-PropertyValue $event 'agentId')
        switch ($type) {
            'tool.execution_start' {
                $toolCallId = [string](Get-PropertyValue $event.data 'toolCallId')
                $toolName = [string](Get-PropertyValue $event.data 'toolName')
                if ([string]::IsNullOrWhiteSpace($toolCallId) -or
                    [string]::IsNullOrWhiteSpace($toolName) -or
                    -not (Test-Property $event.data 'arguments')) {
                    Fail 'A native tool start event is missing its tool call ID or tool name.'
                }
                if ([string]::IsNullOrWhiteSpace($agentId) -and (
                    $toolName -cne 'skill' -or
                    [string](Get-PropertyValue (
                        Get-PropertyValue $event.data 'arguments'
                    ) 'skill') -cne 'review-pull-request' -or
                    -not [string]::IsNullOrWhiteSpace(
                        [string](Get-PropertyValue $event.data 'parentToolCallId')
                    )
                )) {
                    Fail "Native tool call '$toolCallId' is missing worker attribution."
                }
                $key = "$agentId`:$toolCallId"
                if ($starts.ContainsKey($key)) {
                    Fail "Native tool call '$toolCallId' has duplicate start events."
                }
                $starts[$key] = $event
                continue
            }
            'tool.execution_complete' {
                $toolCallId = [string](Get-PropertyValue $event.data 'toolCallId')
                $success = Get-PropertyValue $event.data 'success'
                if ([string]::IsNullOrWhiteSpace($toolCallId) -or
                    $success -isnot [bool]) {
                    Fail 'A native tool completion event is missing its tool call ID or Boolean success state.'
                }
                $key = "$agentId`:$toolCallId"
                if (-not $starts.ContainsKey($key)) {
                    Fail "Native tool completion '$key' occurred before its start event."
                }
                if ($completions.ContainsKey($key)) {
                    Fail "Native tool call '$toolCallId' has duplicate completion events."
                }
                $completions[$key] = $event
                continue
            }
            'assistant.message' {
                if ($null -eq $event.data.PSObject.Properties['content']) {
                    Fail 'A native assistant message is missing its content.'
                }
                continue
            }
            default {
                Fail "The native worker-scope control contains unsupported native event type '$type'."
            }
        }
    }
    foreach ($key in $starts.Keys) {
        if (-not $completions.ContainsKey($key)) {
            Fail "Native tool call '$key' has no completion event."
        }
    }
    foreach ($key in $completions.Keys) {
        if (-not $starts.ContainsKey($key)) {
            Fail "Native tool completion '$key' has no start event."
        }
    }
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
    $trajectoryOutput = [string]$GraderInput.trajectory.output
    $expectedStatus = [regex]::Match(
        $trajectoryOutput,
        '(?im)^\s*STATUS:\s*[A-Z_]+\s*$'
    ).Value.Trim()
    $rootSynthesisIndexes = @(
        for ($index = 0; $index -lt $Events.Count; $index++) {
            $event = $Events[$index]
            $agentId = [string](
                $event.PSObject.Properties['agentId']?.Value ??
                $event.data.PSObject.Properties['agentId']?.Value
            )
            if ($event.type -eq 'assistant_message' -and
                [string]::IsNullOrEmpty($agentId)) {
                $content = ([string]$event.data.content).Trim()
                if ($content -ceq $trajectoryOutput.Trim() -or
                    $content -ceq $expectedStatus) {
                    $index
                }
            }
        }
    )
    if ([string]::IsNullOrWhiteSpace($expectedStatus) -or
        $rootSynthesisIndexes.Count -ne 1 -or
        $rootSynthesisIndexes[0] -le $followUpRead.ResultIndex) {
        Fail 'Root synthesis occurred before the completed same-worker follow-up.'
    }
    $synthesisIndex = $rootSynthesisIndexes[0]
    if (@($rootCalls | Where-Object {
        $_.Name -in @('read_agent', 'write_agent') -and
        $_.CallIndex -ge $synthesisIndex
    }).Count -ne 0) {
        Fail 'A same-worker lifecycle call occurred after root synthesis.'
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

function Get-CanonicalPath {
    param(
        [string]$Path,
        [string]$BasePath
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }
    try {
        $separator = [IO.Path]::DirectorySeparatorChar
        $portable = $Path.Replace('\', $separator).Replace('/', $separator)
        return [IO.Path]::GetFullPath($portable, $BasePath)
    } catch {
        return ''
    }
}

function Test-PathAtOrWithin {
    param(
        [string]$Path,
        [string]$Root
    )

    if ([string]::IsNullOrEmpty($Path) -or [string]::IsNullOrEmpty($Root)) {
        return $false
    }
    $prefix = $Root.TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar
    ) + [IO.Path]::DirectorySeparatorChar
    return $Path -ieq $Root -or
        $Path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
}

function Get-ShellExpandedCommand {
    param([string]$Command)

    $expanded = $Command
    $variables = @(
        foreach ($match in [regex]::Matches(
        $Command,
        '(?im)\$(?:\{(?<braced>[A-Za-z_][A-Za-z0-9_]*)\}|(?<plain>[A-Za-z_][A-Za-z0-9_]*))\s*=\s*(?:''(?<single>[^'']*)''|"(?<double>[^"]*)"|(?<bare>[^\s;|]+))'
    )) {
            $name = if ($match.Groups['braced'].Success) {
                $match.Groups['braced'].Value
            } else {
                $match.Groups['plain'].Value
            }
            $value = @('single', 'double', 'bare') |
                Where-Object { $match.Groups[$_].Success } |
                ForEach-Object { $match.Groups[$_].Value } |
                Select-Object -First 1
            [pscustomobject]@{
                Name = $name
                Value = [string]$value
            }
        }
    ) | Sort-Object { $_.Name.Length } -Descending
    for ($pass = 0; $pass -lt 2; $pass++) {
        foreach ($variable in $variables) {
            foreach ($pattern in @(
                ('\$\{' + [regex]::Escape($variable.Name) + '\}')
                ('\$' + [regex]::Escape($variable.Name) + '(?![A-Za-z0-9_])')
            )) {
                $replacement = [string]$variable.Value
                $expanded = [regex]::Replace(
                    $expanded,
                    $pattern,
                    { param($match) $replacement },
                    [Text.RegularExpressions.RegexOptions]::IgnoreCase
                )
            }
        }
    }
    return $expanded
}

function Get-ShellPathCandidates {
    param(
        [string]$Command,
        [string]$WorkDir
    )

    $expanded = Get-ShellExpandedCommand $Command
    $candidates = [Collections.Generic.List[string]]::new()
    foreach ($match in [regex]::Matches(
        $expanded,
        '(?i)(?<path>(?:[A-Za-z]:[\\/]|\.{1,2}[\\/])[^''"\s;,|)<>]+|\.review-bundle(?:[\\/][^''"\s;,|)<>]+)?)'
    )) {
        $candidate = $match.Groups['path'].Value.Trim()
        $candidate = $candidate.TrimEnd('*', '?')
        $canonical = Get-CanonicalPath $candidate $WorkDir
        if (-not [string]::IsNullOrEmpty($canonical)) {
            $candidates.Add($canonical)
        }
    }
    foreach ($match in [regex]::Matches($expanded, '[''"](?<path>[^''"]+)[''"]')) {
        $candidate = $match.Groups['path'].Value.Trim().TrimEnd('*', '?')
        if ($candidate -notmatch '[\\/]' -and
            -not [IO.Path]::IsPathFullyQualified($candidate)) {
            continue
        }
        $canonical = Get-CanonicalPath $candidate $WorkDir
        if (-not [string]::IsNullOrEmpty($canonical)) {
            $candidates.Add($canonical)
        }
    }
    return @($candidates | Sort-Object -Unique)
}

function Get-ShellRedirectedOutputPaths {
    param(
        [string]$Command,
        [string]$WorkDir
    )

    $expanded = Get-ShellExpandedCommand $Command
    $paths = [Collections.Generic.List[string]]::new()
    foreach ($match in [regex]::Matches(
        $expanded,
        '(?i)(?:^|[\s;|])>{1,2}\s*(?:[''"](?<quoted>[^''"]+)[''"]|(?<bare>[^\s;|<>]+))'
    )) {
        $candidate = if ($match.Groups['quoted'].Success) {
            $match.Groups['quoted'].Value
        } else {
            $match.Groups['bare'].Value
        }
        $canonical = Get-CanonicalPath $candidate $WorkDir
        if (-not [string]::IsNullOrEmpty($canonical)) {
            $paths.Add($canonical)
        }
    }
    foreach ($match in [regex]::Matches(
        $expanded,
        '(?i)\b(?:Out-File|Set-Content|Tee-Object)\b[^\r\n;|]*?-(?:File)?Path\s+(?:[''"](?<quoted>[^''"]+)[''"]|(?<bare>[^\s;|<>]+))'
    )) {
        $candidate = if ($match.Groups['quoted'].Success) {
            $match.Groups['quoted'].Value
        } else {
            $match.Groups['bare'].Value
        }
        $canonical = Get-CanonicalPath $candidate $WorkDir
        if (-not [string]::IsNullOrEmpty($canonical)) {
            $paths.Add($canonical)
        }
    }
    return @($paths | Sort-Object -Unique)
}

function Get-SavedOutputPaths {
    param(
        [object]$Call,
        [string]$WorkDir
    )

    return @(
        foreach ($match in [regex]::Matches(
            $Call.ResultText,
            '(?im)^\s*(?:Output too large to read at once.*?Saved to:|Saved to:)\s*(?<path>[^\r\n]+)\s*$'
        )) {
            $path = Get-CanonicalPath $match.Groups['path'].Value.Trim() $WorkDir
            if (-not [string]::IsNullOrEmpty($path)) {
                $path
            }
        }
    )
}

function Get-WorkerCallPathInfo {
    param(
        [object]$Call,
        [string]$WorkDir
    )

    $paths = [Collections.Generic.List[string]]::new()
    if ($Call.Name -in @('view', 'read_file')) {
        $path = Get-CanonicalCallPath $Call $WorkDir
        if ([string]::IsNullOrEmpty($path)) {
            return [pscustomobject]@{
                Paths = @()
                Error = "used an invalid $($Call.Name) path"
            }
        }
        $paths.Add($path)
    } elseif ($Call.Name -in @('rg', 'grep', 'glob')) {
        $rawPaths = Get-Argument $Call 'paths'
        if ($null -eq $rawPaths) {
            $rawPaths = Get-Argument $Call 'path'
        }
        $searchPaths = if ($null -eq $rawPaths -or
            ($rawPaths -is [string] -and
                [string]::IsNullOrWhiteSpace([string]$rawPaths))) {
            @($WorkDir)
        } elseif ($rawPaths -is [string]) {
            @([string]$rawPaths)
        } else {
            $resolved = @(
                $rawPaths |
                    ForEach-Object { [string]$_ } |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
            )
            if ($resolved.Count -eq 0) { @($WorkDir) } else { $resolved }
        }
        foreach ($searchPath in $searchPaths) {
            $canonical = Get-CanonicalPath $searchPath $WorkDir
            if ([string]::IsNullOrEmpty($canonical)) {
                return [pscustomobject]@{
                    Paths = @()
                    Error = "used an invalid $($Call.Name) scope '$searchPath'"
                }
            }
            $paths.Add($canonical)
            if ($Call.Name -eq 'glob') {
                $pattern = [string](Get-Argument $Call 'pattern')
                if (-not [string]::IsNullOrWhiteSpace($pattern) -and
                    $pattern -match '[\\/]') {
                    $prefixParts = [Collections.Generic.List[string]]::new()
                    foreach ($part in $pattern.Replace('\', '/').Split('/')) {
                        if ($part -match '[*?\[]') {
                            break
                        }
                        if (-not [string]::IsNullOrWhiteSpace($part)) {
                            $prefixParts.Add($part)
                        }
                    }
                    if ($prefixParts.Count -gt 0) {
                        $combined = Get-CanonicalPath (
                            Join-Path $canonical ($prefixParts -join '/')
                        ) $WorkDir
                        if (-not [string]::IsNullOrEmpty($combined)) {
                            $paths.Add($combined)
                        }
                    }
                }
            }
        }
    } elseif (Test-ShellTool $Call.Name) {
        foreach ($path in Get-ShellPathCandidates (
            [string](Get-Argument $Call 'command')
        ) $WorkDir) {
            $paths.Add($path)
        }
    }
    return [pscustomobject]@{
        Paths = @($paths | Sort-Object -Unique)
        Error = ''
    }
}

function Get-WorkerScopeViolation {
    param(
        [object]$Call,
        [object]$Scope,
        [string]$WorkDir
    )

    $bundleRoot = [string]$Scope.BundleRoot
    $assignedFiles = @($Scope.AssignedFiles)
    $sourceRoots = @($Scope.SourceRoots)
    $pathInfo = Get-WorkerCallPathInfo $Call $WorkDir
    if (-not [string]::IsNullOrEmpty($pathInfo.Error)) {
        return $pathInfo.Error
    }

    foreach ($path in $pathInfo.Paths) {
        if ((Test-ShellTool $Call.Name) -and
            -not (Test-PathAtOrWithin $path $bundleRoot) -and
            -not (Test-PathAtOrWithin $bundleRoot $path)) {
            continue
        }
        if ($path -iin $assignedFiles) {
            continue
        }
        if (@($sourceRoots | Where-Object {
            Test-PathAtOrWithin $path ([string]$_)
        }).Count -gt 0) {
            continue
        }
        return "scope included an unassigned bundle path '$path'"
    }
    return ''
}

function Assert-WorkerInputScopes {
    param(
        [object[]]$WorkerCalls,
        [hashtable]$ScopesByWorker,
        [string]$WorkDir
    )

    $contaminatedOutputs = @{}
    $firstViolation = ''
    foreach ($call in $WorkerCalls | Sort-Object CallIndex) {
        if (-not $ScopesByWorker.ContainsKey([string]$call.AgentId)) {
            Fail "Observed worker tool activity for unassigned worker '$($call.AgentId)'."
        }
        $scope = $ScopesByWorker[[string]$call.AgentId]
        $pathInfo = Get-WorkerCallPathInfo $call $WorkDir
        foreach ($path in $pathInfo.Paths) {
            foreach ($contaminatedPath in $contaminatedOutputs.Keys) {
                if ($path -ieq $contaminatedPath -or
                    (Test-PathAtOrWithin $contaminatedPath $path)) {
                    Fail "Guide worker '$($call.AgentId)' consumed intermediate output derived from $($contaminatedOutputs[$contaminatedPath])."
                }
            }
        }
        $violation = Get-WorkerScopeViolation $call $scope $WorkDir
        if (-not [string]::IsNullOrEmpty($violation)) {
            $evidence = "Guide worker '$($call.AgentId)' $violation."
            $outputPaths = @(
                Get-SavedOutputPaths $call $WorkDir
                if (Test-ShellTool $call.Name) {
                    Get-ShellRedirectedOutputPaths (
                        [string](Get-Argument $call 'command')
                    ) $WorkDir
                }
            )
            foreach ($path in $outputPaths | Sort-Object -Unique) {
                $contaminatedOutputs[$path] = $evidence
            }
            if ([string]::IsNullOrEmpty($firstViolation)) {
                $firstViolation = $evidence
            }
        }
    }
    if (-not [string]::IsNullOrEmpty($firstViolation)) {
        Fail $firstViolation
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

    if ($Clause -match '(?i)\b(?:do not|don''t|never|must not)\s+(?:skip|ignore|avoid)\b') {
        return $false
    }
    return $Clause -match '(?i)\b(?:do not|don''t|must not|never|not assigned|exclude|prohibit|forbid|forbidden|skip|ignore|unavailable)\b'
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

function Get-JsonAssignmentValues {
    param(
        [object]$Value
    )

    if ($null -eq $Value) {
        return
    }
    if ($Value -is [string] -or $Value.GetType().IsPrimitive) {
        return
    }
    if ($Value -is [Management.Automation.PSCustomObject] -and
        $null -ne $Value.PSObject.Properties['files'] -and
        $null -ne $Value.PSObject.Properties['sourceRoles']) {
        $Value
        return
    }
    if ($Value -is [Collections.IEnumerable] -and
        $Value -isnot [Management.Automation.PSCustomObject]) {
        foreach ($item in $Value) {
            Get-JsonAssignmentValues $item
        }
        return
    }
    foreach ($property in $Value.PSObject.Properties) {
        Get-JsonAssignmentValues $property.Value
    }
}

function Test-PromptContainsAssignment {
    param(
        [string]$Prompt,
        [object]$Assignment
    )

    $expected = $Assignment | ConvertTo-Json -Compress -Depth 20
    $start = -1
    $depth = 0
    $inString = $false
    $escaped = $false
    $assignments = [Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $Prompt.Length; $index++) {
        $character = $Prompt[$index]
        if ($inString) {
            if ($escaped) {
                $escaped = $false
            } elseif ($character -eq '\') {
                $escaped = $true
            } elseif ($character -eq '"') {
                $inString = $false
            }
            continue
        }
        if ($character -eq '"') {
            $inString = $true
            continue
        }
        if ($character -eq '{') {
            if ($depth -eq 0) {
                $start = $index
            }
            $depth++
            continue
        }
        if ($character -ne '}' -or $depth -eq 0) {
            continue
        }
        $depth--
        if ($depth -ne 0) {
            continue
        }
        $candidate = $Prompt.Substring($start, $index - $start + 1)
        try {
            $parsed = $candidate | ConvertFrom-Json -Depth 20
            foreach ($assignmentValue in @(Get-JsonAssignmentValues $parsed)) {
                $assignments.Add($assignmentValue)
            }
        } catch {
        }
    }
    return $assignments.Count -eq 1 -and
        ($assignments[0] | ConvertTo-Json -Compress -Depth 20) -ceq $expected
}

function Test-PromptBinding {
    param(
        [string]$Prompt,
        [string]$LabelPattern,
        [string]$Value,
        [switch]$AllowHashSeparator
    )

    $normalizedValue = $Value.Replace('\', '/')
    $separators = if ($AllowHashSeparator) {
        '(?::|=|#|\bis\b)'
    } else {
        '(?::|=|\bis\b)'
    }
    $valuePrefix = if ($AllowHashSeparator) {
        '(?:#[ \t]*)?'
    } else {
        ''
    }
    $labelFormatting = '[ \t*_`"'']*'
    $valueFormatting = '[ \t*_`"''\[]*'
    $bindingPattern = (
        "(?:$LabelPattern)$labelFormatting$separators" +
        "$valueFormatting$valuePrefix" +
        '(?<value>[A-Za-z0-9][A-Za-z0-9_./\\-]*)'
    )
    $foundExpected = $false
    $normalizedPrompt = $Prompt.Replace('\', '/')
    foreach ($binding in [regex]::Matches($normalizedPrompt, $bindingPattern)) {
        $prefixStart = 0
        foreach ($delimiter in @("`n", '.', ';', ',')) {
            $position = $normalizedPrompt.LastIndexOf(
                $delimiter,
                [Math]::Max(0, $binding.Index - 1)
            )
            if ($position -ge $prefixStart) {
                $prefixStart = $position + 1
            }
        }
        $bindingPrefix = $normalizedPrompt.Substring(
            $prefixStart,
            $binding.Index - $prefixStart
        )
        if ($bindingPrefix -match (
            "(?i)(?:do not|don't|must not|never)\s+" +
            "(?:use|review|treat|select|trust|rely on)\s+(?:the\s+)?$"
        )) {
            continue
        }
        $rawValue = $binding.Groups['value'].Value
        $actualValue = $rawValue -replace '[.)\]}*_`]+$', ''
        $sameValueShape = if ($normalizedValue -match '^\d+$') {
            $actualValue -match '^\d+$'
        } elseif ($normalizedValue -match '^[0-9a-f]{40}$') {
            $actualValue -match '^[0-9a-f]{40}$'
        } else {
            $actualValue -match '[/\\]'
        }
        if (-not $sameValueShape) {
            continue
        }
        $tailStart = $binding.Index + $binding.Length -
            ($rawValue.Length - $actualValue.Length)
        $tailEnd = $normalizedPrompt.Length
        $tailDelimiter = ''
        foreach ($delimiter in @("`n", '.', ';', ',')) {
            $position = $normalizedPrompt.IndexOf($delimiter, $tailStart)
            if ($position -ge 0 -and $position -lt $tailEnd) {
                $tailEnd = $position
                $tailDelimiter = $delimiter
            }
        }
        $tail = $normalizedPrompt.Substring($tailStart, $tailEnd - $tailStart)
        $tail = $tail -replace '^[\s`*_"''\])}]+', ''
        $negatedBinding = $tail -match (
            "(?i)^\s*(?:(?:but|however)\s+)?" +
            "(?:(?:which|it|this|that)\s+)?" +
            "(?:(?:is|was|should|must|do)\s+not|" +
            "(?:isn't|wasn't|shouldn't|mustn't|don't))\b"
        )
        if (-not $negatedBinding -and $tailDelimiter -eq ',') {
            $continuationStart = $tailEnd + 1
            $continuationEnd = $normalizedPrompt.Length
            foreach ($delimiter in @("`n", '.', ';', ',')) {
                $position = $normalizedPrompt.IndexOf($delimiter, $continuationStart)
                if ($position -ge 0 -and $position -lt $continuationEnd) {
                    $continuationEnd = $position
                }
            }
            $continuation = $normalizedPrompt.Substring(
                $continuationStart,
                $continuationEnd - $continuationStart
            )
            $continuation = $continuation -replace '^[\s`*_"''\])}]+', ''
            $negatedBinding = $continuation -match (
                "(?i)^\s*(?:(?:but|however)\s+)?" +
                "(?:which|it|this|that)\s+" +
                "(?:(?:is|was|should|must|do)\s+not|" +
                "(?:isn't|wasn't|shouldn't|mustn't|don't))\b"
            )
        }
        if ($actualValue -cne $normalizedValue -or
            $negatedBinding) {
            return $false
        }
        $foundExpected = $true
    }
    return $foundExpected
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

function Test-NativeWorkerScope {
    param(
        [object]$GraderInput,
        [object[]]$Calls
    )

    $scopeContract = $GraderInput.PSObject.Properties['nativeScope']?.Value
    if ($null -eq $scopeContract -or
        [string]$scopeContract.candidateRevision -notmatch '^[0-9a-f]{40}$' -or
        [string]::IsNullOrWhiteSpace([string]$scopeContract.bundleRoot)) {
        Fail 'The native worker-scope control is missing frozen candidate or bundle metadata.'
    }
    $workDir = [IO.Path]::GetFullPath([string]$GraderInput.trajectory.workDir)
    $bundleRoot = Get-CanonicalPath ([string]$scopeContract.bundleRoot) $workDir
    $scopes = @{}
    $declaredWorkers = @($scopeContract.workers)
    if ($declaredWorkers.Count -eq 0) {
        Fail 'The native worker-scope control has no declared workers.'
    }
    foreach ($worker in $declaredWorkers) {
        $workerId = [string]$worker.agentId
        if ([string]::IsNullOrWhiteSpace($workerId) -or $scopes.ContainsKey($workerId)) {
            Fail 'The native worker-scope control has missing or duplicate worker identity.'
        }
        $scopes[$workerId] = [pscustomobject]@{
            BundleRoot = $bundleRoot
            AssignedFiles = @($worker.assignedFiles | ForEach-Object {
                Get-CanonicalPath ([string]$_) $bundleRoot
            })
            SourceRoots = @($worker.sourceRoots | ForEach-Object {
                Get-CanonicalPath ([string]$_) $bundleRoot
            })
        }
    }
    $workerCalls = @($Calls | Where-Object {
        -not [string]::IsNullOrEmpty($_.AgentId)
    })
    foreach ($workerId in $scopes.Keys) {
        if (@($workerCalls | Where-Object {
            [string]$_.AgentId -ceq [string]$workerId
        }).Count -eq 0) {
            Fail "Declared native worker '$workerId' did not appear in the retained tool events."
        }
    }
    Assert-WorkerToolBoundary $workerCalls
    Assert-WorkerInputScopes $workerCalls $scopes $workDir
    if (@($workerCalls | Where-Object { Test-ShellTool $_.Name }).Count -ne 0) {
        Fail 'A supplied-bundle worker used a shell tool.'
    }
    Complete-Grading -Passed $true -Evidence (
        "Observed native worker tool events within frozen candidate " +
        "$($scopeContract.candidateRevision) assignment scopes."
    )
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

    $tasks = @($rootCalls | Where-Object { $_.Name -eq 'task' })
    $manifest = Get-Content (Join-Path $bundleRoot 'manifest.json') -Raw -Encoding utf8 |
        ConvertFrom-Json -Depth 100
    $expectedWorkerCount = @($manifest.guides).Count
    if ($tasks.Count -ne $expectedWorkerCount) {
        Fail "Expected exactly $expectedWorkerCount prescribed guide worker dispatch, but observed $($tasks.Count)."
    }
    $assignedGuides = @{}
    $workerGuideById = @{}
    $workerScopes = @{}
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
        $assignment = $guide.assignment
        $assignedPaths = @($assignment.files | ForEach-Object { [string]$_.path })
        $bundleRootPrompt = $bundleRoot.Replace('\', '/')
        if ($workerPrompt.IndexOf(
            $bundleRootPrompt,
            [StringComparison]::OrdinalIgnoreCase
        ) -lt 0) {
            Fail "The '$guidePath' worker prompt omitted the trusted absolute bundle root."
        }
        if (-not (Test-PromptContainsAssignment $workerPrompt $assignment)) {
            Fail "The '$guidePath' worker prompt did not preserve the verbatim assignment."
        }
        foreach ($other in @(
            $manifest.guides.assignment.files |
                Where-Object { [string]$_.path -cnotin $assignedPaths }
        )) {
            if (Test-PromptContainsFixtureInput `
                -Prompt $workerPrompt `
                -BundleRoot $bundleRoot `
                -RelativePaths @([string]$other.path)) {
                Fail "The '$guidePath' worker prompt included unassigned input '$($other.path)'."
            }
        }
        $sourceRoots = @(
            foreach ($role in @($assignment.sourceRoles)) {
                $sourceRoot = [string]$manifest.sources.$role.root
                if (-not (Test-PromptBinding `
                    -Prompt $workerPrompt `
                    -LabelPattern (
                        "(?i)(?:\bsource\s+role\s+)?\b" +
                        "$([regex]::Escape([string]$role))\b(?:\s+root\b)?"
                    ) `
                    -Value $sourceRoot)) {
                    Fail "The '$guidePath' worker prompt omitted the exact frozen source role binding for '$role'."
                }
                [IO.Path]::GetFullPath(
                    (Join-Path $bundleRoot $sourceRoot.Replace(
                        '/',
                        [IO.Path]::DirectorySeparatorChar
                    ))
                )
            }
        )
        foreach ($identity in @(
            [pscustomobject]@{
                Label = 'PR'
                Pattern = '(?i)(?:\bfrozen\s+)?\bPR\b'
                Value = $manifest.target.pr
                AllowHashSeparator = $true
            },
            [pscustomobject]@{
                Label = 'head'
                Pattern = '(?i)(?:\bfrozen\s+)?\bhead\b(?!\s+root\b)'
                Value = $manifest.target.head
                AllowHashSeparator = $false
            },
            [pscustomobject]@{
                Label = 'merge base'
                Pattern = '(?i)(?:\bfrozen\s+)?\bmerge(?:\s+|[-_]?)(?:base)\b(?!\s+root\b)'
                Value = $manifest.target.mergeBase
                AllowHashSeparator = $false
            },
            [pscustomobject]@{
                Label = 'base tip'
                Pattern = '(?i)(?:\bfrozen\s+)?\bbase(?:\s+|[-_]?)(?:tip)\b(?!\s+root\b)'
                Value = $manifest.target.baseTip
                AllowHashSeparator = $false
            }
        )) {
            if (-not (Test-PromptBinding `
                -Prompt $workerPrompt `
                -LabelPattern $identity.Pattern `
                -Value ([string]$identity.Value) `
                -AllowHashSeparator:$identity.AllowHashSeparator)) {
                Fail "The '$guidePath' worker prompt omitted the exact frozen $($identity.Label) binding."
            }
        }
        $workerScopes[$workerId] = [pscustomobject]@{
            BundleRoot = $bundleRoot
            AssignedFiles = @($assignedPaths | ForEach-Object {
                [IO.Path]::GetFullPath(
                    (Join-Path $bundleRoot $_.Replace(
                        '/',
                        [IO.Path]::DirectorySeparatorChar
                    ))
                )
            })
            SourceRoots = $sourceRoots
        }
    }
    foreach ($guide in @($manifest.guides)) {
        if (-not $assignedGuides.ContainsKey([string]$guide.path)) {
            Fail "Guide '$([string]$guide.path)' was not assigned to a worker task."
        }
    }
    Assert-WorkerInputScopes $workerCalls $workerScopes $workDir
    Assert-WorkerToolBoundary @(
        $workerCalls |
            Where-Object { $workerGuideById.ContainsKey([string]$_.AgentId) }
    ) -ExpectedWorkerIds @($workerGuideById.Keys) -RequireSkill
    if (@($Calls | Where-Object { Test-ShellTool $_.Name }).Count -ne 0) {
        Fail 'A supplied-bundle transport run used a shell tool.'
    }
    $unsupportedRootCalls = @(
        $rootCalls |
            Where-Object {
                $_.Name -inotmatch '^(skill|view|read_file|rg|grep|glob|task|read_agent|write_agent)$'
            }
    )
    if ($unsupportedRootCalls.Count -ne 0) {
        Fail "The coordinator used unsupported root tool '$($unsupportedRootCalls[0].Name)'."
    }
    $rootSkillCalls = @($rootCalls | Where-Object { $_.Name -ieq 'skill' })
    if ($rootSkillCalls.Count -gt 1 -or
        @($rootSkillCalls | Where-Object {
            $_.Name -cne 'skill' -or
            [string](Get-Argument $_ 'skill') -cne 'review-pull-request' -or
            $_.ResultSuccess -ne $true
        }).Count -ne 0 -or
        ($rootSkillCalls.Count -eq 1 -and
        $rootSkillCalls[0].CallIndex -ne (
            $rootCalls | Measure-Object CallIndex -Minimum
        ).Minimum)) {
        Fail 'The coordinator root skill load was not one optional successful first review-pull-request invocation.'
    }
    if (@($rootCalls | Where-Object { $_.Name -in @('rg', 'grep', 'glob') }).Count -ne 0) {
        Fail 'The coordinator used a broad root search instead of exact trusted bundle reads.'
    }
    $coordinatorPaths = @(
        $inventory.files |
            Where-Object { 'coordinator' -in @($_.readRoles) } |
            ForEach-Object {
                [IO.Path]::GetFullPath(
                    (Join-Path $bundleRoot ([string]$_.path).Replace(
                        '/',
                        [IO.Path]::DirectorySeparatorChar
                    ))
                )
            }
    )
    foreach ($call in $rootCalls | Where-Object {
        $_.Name -in @('view', 'read_file')
    }) {
        $path = Get-CanonicalCallPath $call $workDir
        if ($null -eq $path -or $path -cnotin $coordinatorPaths) {
            Fail "The coordinator read untrusted path '$path' outside its exact bundle inputs."
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
    $completedWorkerReportIndexes = @{}
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
            $completedWorkerReportIndexes[$workerId] = [Math]::Max(
                [int]($completedWorkerReportIndexes[$workerId] ?? -1),
                $call.ResultIndex
            )
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
    $trajectoryOutput = [string]$GraderInput.trajectory.output
    $rootSynthesisIndexes = @(
        for ($index = 0; $index -lt $Events.Count; $index++) {
            $event = $Events[$index]
            $agentId = [string](
                $event.PSObject.Properties['agentId']?.Value ??
                $event.data.PSObject.Properties['agentId']?.Value
            )
            if ($event.type -eq 'assistant_message' -and
                [string]::IsNullOrEmpty($agentId) -and
                ([string]$event.data.content).Trim() -ceq $trajectoryOutput.Trim()) {
                $index
            }
        }
    )
    if ([string]::IsNullOrWhiteSpace($trajectoryOutput) -or
        $rootSynthesisIndexes.Count -ne 1) {
        Fail 'The coordinator did not emit exactly one root synthesis matching trajectory output.'
    }
    $synthesisIndex = $rootSynthesisIndexes[0]
    $lifecycleCalls = @(
        $rootCalls |
            Where-Object { $_.Name -in @('read_agent', 'write_agent') }
    )
    $writes = @(
        $lifecycleCalls |
            Where-Object { $_.Name -eq 'write_agent' } |
            Sort-Object CallIndex
    )
    for ($writeIndex = 0; $writeIndex -lt $writes.Count; $writeIndex++) {
        $write = $writes[$writeIndex]
        if ($write.CallIndex -ge $synthesisIndex) {
            continue
        }
        $nextSameWorkerWrite = @(
            $writes |
                Select-Object -Skip ($writeIndex + 1) |
                Where-Object {
                    [string](Get-Argument $_ 'agent_id') -ceq
                        [string](Get-Argument $write 'agent_id')
                } |
                Select-Object -First 1
        )
        $followUpBoundary = if ($nextSameWorkerWrite.Count -eq 1) {
            $nextSameWorkerWrite[0].CallIndex
        } else {
            $synthesisIndex
        }
        $workerId = [string](Get-Argument $write 'agent_id')
        if ($write.ResultSuccess -ne $true) {
            Fail "Guide worker '$workerId' clarification did not complete successfully."
        }
        $priorReads = @(
            $lifecycleCalls |
                Where-Object {
                    $_.Name -eq 'read_agent' -and
                    [string](Get-Argument $_ 'agent_id') -ceq $workerId -and
                    (Get-Argument $_ 'wait') -eq $true -and
                    $_.ResultSuccess -eq $true -and
                    $_.ResultIndex -lt $write.CallIndex -and
                    (Get-ReadStatus $_.ResultText) -eq 'completed'
                }
        )
        if ($priorReads.Count -eq 0) {
            Fail "Guide worker '$workerId' clarification occurred before an initial completed report."
        }
        $priorMaxTurn = @(
            $priorReads |
                ForEach-Object { Get-ReportedTurns $_.ResultText } |
                Measure-Object Index -Maximum
        )[0].Maximum
        $followUpReads = @(
            $lifecycleCalls |
                Where-Object {
                    $_.Name -eq 'read_agent' -and
                    [string](Get-Argument $_ 'agent_id') -ceq $workerId -and
                    (Get-Argument $_ 'wait') -eq $true -and
                    $_.ResultSuccess -eq $true -and
                    $_.CallIndex -gt $write.ResultIndex -and
                    $_.ResultIndex -lt $followUpBoundary -and
                    (Get-ReadStatus $_.ResultText) -eq 'completed' -and
                    @(
                        Get-ReportedTurns $_.ResultText |
                            Where-Object {
                                $_.Payload.Length -ge 20 -and
                                $_.Index -gt $priorMaxTurn
                            }
                    ).Count -ne 0
                }
        )
        if ($followUpReads.Count -eq 0) {
            Fail "Guide worker '$workerId' clarification had no new completed follow-up turn before root synthesis."
        }
    }
    foreach ($workerId in $completedWorkerReportIndexes.Keys) {
        if ($completedWorkerReportIndexes[$workerId] -ge $synthesisIndex) {
            Fail "Guide worker '$workerId' report collection completed after root synthesis."
        }
    }
    if (@($lifecycleCalls | Where-Object {
        $_.CallIndex -ge $synthesisIndex
    }).Count -ne 0) {
        Fail 'A coordinator agent lifecycle call occurred after root synthesis.'
    }
    $firstTaskIndex = ($tasks | Measure-Object CallIndex -Minimum).Minimum
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
    $workerCalls = @($Calls | Where-Object {
        -not [string]::IsNullOrEmpty($_.AgentId)
    })
    if ($workerCalls.Count -ne 0) {
        Fail 'The unavailable-byte path performed a worker-attributed tool call.'
    }
    $rootSkillCalls = @($rootCalls | Where-Object { $_.Name -eq 'skill' })
    if ($rootSkillCalls.Count -ne 1 -or
        [string](Get-Argument $rootSkillCalls[0] 'skill') -cne 'review-pull-request' -or
        $rootSkillCalls[0].ResultSuccess -ne $true -or
        $rootSkillCalls[0].CallIndex -ne (
            $rootCalls | Measure-Object CallIndex -Minimum
        ).Minimum) {
        Fail 'The unavailable-byte path did not begin with one successful review-pull-request skill load.'
    }
    $unsupportedCalls = @(
        $rootCalls |
            Where-Object { $_.Name -notin @('skill', 'view') }
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
    if (@($rootCalls | Where-Object {
        if ($_.Name -ne 'view') {
            return $false
        }
        $path = Get-CanonicalCallPath $_ $workDir
        return $path -ne $manifestPath -and $path -ne $feedbackPath
    }).Count -ne 0) {
        Fail 'The unavailable-byte path attempted substitute evidence outside the manifest and exact required feedback input.'
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
    if (@($rootCalls | Where-Object {
        $_.Name -eq 'view' -and
        $_.CallIndex -gt $failedRead.ResultIndex
    }).Count -ne 0) {
        Fail 'The unavailable-byte path attempted substitute evidence after the required read failed.'
    }
    if (Test-Path $feedbackPath) {
        Fail 'The blocked fixture unexpectedly contains the required feedback input.'
    }
    if ([string]$GraderInput.trajectory.output -notmatch '(?im)^\s*STATUS:\s*BLOCKED\b') {
        Fail 'Unavailable required bytes did not produce STATUS: BLOCKED.'
    }
    $trajectoryOutput = [string]$GraderInput.trajectory.output
    $rootSynthesisIndexes = @(
        for ($index = 0; $index -lt $Events.Count; $index++) {
            $event = $Events[$index]
            $agentId = [string](
                $event.PSObject.Properties['agentId']?.Value ??
                $event.data.PSObject.Properties['agentId']?.Value
            )
            if ($event.type -eq 'assistant_message' -and
                [string]::IsNullOrEmpty($agentId) -and
                ([string]$event.data.content).Trim() -ceq $trajectoryOutput.Trim()) {
                $index
            }
        }
    )
    if ($rootSynthesisIndexes.Count -ne 1 -or
        $rootSynthesisIndexes[0] -le $failedRead.ResultIndex) {
        Fail 'No root assistant message reported STATUS: BLOCKED after the failed required read.'
    }
    $synthesisIndex = $rootSynthesisIndexes[0]
    if (@($rootCalls | Where-Object { $_.CallIndex -ge $synthesisIndex }).Count -ne 0) {
        Fail 'The unavailable-byte path performed a tool call after root synthesis.'
    }

    Complete-Grading -Passed $true -Evidence 'Observed a failed native required-input read, no fallback or worker dispatch, and an explicit BLOCKED result.'
}

try {
    if ([string]::IsNullOrWhiteSpace($env:EVALUATE_GRADER_INPUT)) {
        throw 'EVALUATE_GRADER_INPUT is not set.'
    }
    $graderInput = Get-Content $env:EVALUATE_GRADER_INPUT -Raw -Encoding utf8 |
        ConvertFrom-Json -Depth 100
    if ($Contract -eq 'native-worker-scope') {
        Assert-NativeEventContract @($graderInput.trajectory.events)
    }
    $events = @(Convert-NativeEvents @($graderInput.trajectory.events))
    $calls = @(Get-Calls $events)
    switch ($Contract) {
        'sdk-recovery' { Test-SdkRecovery $calls }
        'same-worker-lifecycle' { Test-SameWorkerLifecycle $graderInput $calls $events }
        'ready-v2-transport' { Test-ReadyV2Transport $graderInput $calls $events }
        'required-bytes-blocked' { Test-RequiredBytesBlocked $graderInput $calls $events }
        'native-worker-scope' { Test-NativeWorkerScope $graderInput $calls }
    }
} catch {
    Complete-Grading -Passed $false -Evidence "Trace grader error: $($_.Exception.Message)" -Status 'error'
}
