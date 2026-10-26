#requires -Version 7.0
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('sdk-recovery', 'same-worker-lifecycle', 'hosted-pagination', 'required-bytes-blocked')]
    [string]$Contract,

    [string]$ContractPath,

    [int]$ExpectedPr,

    [string]$ExpectedHead
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

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
    param([object[]]$Calls)

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

    Complete-Grading -Passed $true -Evidence 'Observed one background worker, an actual initial report, one clarification to the returned worker ID, and an actual follow-up report from that same ID.'
}

function Get-ViewCoverage {
    param(
        [object[]]$Calls,
        [object]$FileContract
    )

    $matching = @(
        $Calls |
            Where-Object {
                $_.Name -eq 'view' -and
                [string](Get-Argument $_ 'path') -match ('(?i)(^|[\\/])' + [regex]::Escape([string]$FileContract.path) + '$')
            }
    )
    if ($matching.Count -eq 0) {
        return [pscustomobject]@{ Passed = $false; Evidence = "No native view call read $($FileContract.path)."; LastResultIndex = -1 }
    }

    $observedLines = @{}
    $lastResultIndex = -1
    $creditedCalls = 0
    foreach ($call in $matching) {
        if ((Get-Argument $call 'forceReadLargeFiles') -eq $true) {
            return [pscustomobject]@{ Passed = $false; Evidence = "A forced whole-file read bypassed pagination for $($FileContract.path)."; LastResultIndex = $lastResultIndex }
        }
        if ($call.ResultSuccess -ne $true -or
            [string]::IsNullOrWhiteSpace($call.ResultText) -or
            $call.ResultText -match '(?i)file too large|output too large|size warning') {
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
        if ($call.ResultText -match '(?i)\[Output truncated\.|output truncated|truncation') {
            $lastReturnedLine = (
                $lineMatches |
                    ForEach-Object { [int]$_.Groups['line'].Value } |
                    Measure-Object -Maximum
            ).Maximum
            $lineMatches = @(
                $lineMatches |
                    Where-Object {
                        [int]$_.Groups['line'].Value -ne $lastReturnedLine
                    }
            )
        }
        if ($lineMatches.Count -eq 0) {
            continue
        }
        foreach ($lineMatch in $lineMatches) {
            $line = [int]$lineMatch.Groups['line'].Value
            $content = $lineMatch.Groups['content'].Value
            if ($line -lt 1 -or $line -gt [int]$FileContract.lineCount) {
                return [pscustomobject]@{ Passed = $false; Evidence = "A native read returned out-of-range line $line for $($FileContract.path)."; LastResultIndex = $lastResultIndex }
            }
            if ($observedLines.ContainsKey($line) -and
                $observedLines[$line] -ne $content) {
                return [pscustomobject]@{ Passed = $false; Evidence = "Native reads returned conflicting content for line $line of $($FileContract.path)."; LastResultIndex = $lastResultIndex }
            }
            $observedLines[$line] = $content
        }
        $creditedCalls++
        $lastResultIndex = [Math]::Max($lastResultIndex, $call.ResultIndex)
    }
    if ($creditedCalls -lt 2) {
        return [pscustomobject]@{ Passed = $false; Evidence = "Fewer than two content-bearing paged reads covered $($FileContract.path)."; LastResultIndex = $lastResultIndex }
    }

    for ($line = 1; $line -le [int]$FileContract.lineCount; $line++) {
        if (-not $observedLines.ContainsKey($line)) {
            return [pscustomobject]@{ Passed = $false; Evidence = "Native reads left an actual returned-line gap in $($FileContract.path) at line $line."; LastResultIndex = $lastResultIndex }
        }
    }
    $content = ($observedLines.GetEnumerator() |
        Sort-Object Name |
        ForEach-Object { [string]$_.Value }) -join "`n"
    foreach ($sentinel in $FileContract.sentinels) {
        if ($content -notmatch [regex]::Escape([string]$sentinel)) {
            return [pscustomobject]@{ Passed = $false; Evidence = "The native read results omitted sentinel $sentinel from $($FileContract.path)."; LastResultIndex = $lastResultIndex }
        }
    }
    return [pscustomobject]@{ Passed = $true; Evidence = ''; LastResultIndex = $lastResultIndex }
}

function Test-HostedPagination {
    param(
        [object]$GraderInput,
        [object[]]$Calls
    )

    if ([string]::IsNullOrWhiteSpace($ContractPath)) {
        Fail 'The hosted pagination grader requires a contract path.'
    }
    if (@($Calls | Where-Object { Test-ShellTool $_.Name }).Count -ne 0) {
        Fail 'A hosted supplied-bundle run used a shell tool.'
    }
    $rootCalls = @($Calls | Where-Object { [string]::IsNullOrEmpty($_.AgentId) })
    $tasks = @($rootCalls | Where-Object { $_.Name -eq 'task' })
    if ($tasks.Count -eq 0) {
        Fail 'No prescribed guide worker was dispatched after the bundle reads.'
    }
    $firstTaskIndex = ($tasks | Measure-Object CallIndex -Minimum).Minimum
    $preDispatchCalls = @(
        $rootCalls |
            Where-Object {
                $_.CallIndex -lt $firstTaskIndex -and
                $_.ResultIndex -ge 0 -and
                $_.ResultIndex -lt $firstTaskIndex
            }
    )
    $contractFile = Join-Path ([string]$GraderInput.trajectory.workDir) $ContractPath
    $pagination = Get-Content $contractFile -Raw -Encoding utf8 | ConvertFrom-Json -Depth 100
    if (@($pagination.files).Count -ne 3) {
        Fail 'The pagination contract must describe exactly files.json, pull.json, and feedback.json.'
    }

    $lastEvidenceIndex = -1
    foreach ($file in $pagination.files) {
        if ($file.path -notin @('files.json', 'pull.json', 'feedback.json') -or
            [int]$file.lineCount -lt 200 -or
            [int]$file.minimumBytes -le 20480 -or
            [string]::IsNullOrWhiteSpace([string]$file.sha256) -or
            @($file.sentinels).Count -lt 5) {
            Fail "The pagination contract for $($file.path) is not discriminating."
        }
        $fixturePath = Join-Path ([string]$GraderInput.trajectory.workDir) ".review-bundle/$($file.path)"
        if (-not (Test-Path $fixturePath -PathType Leaf) -or
            (Get-Item $fixturePath).Length -lt [int]$file.minimumBytes -or
            @(Get-Content $fixturePath -Encoding utf8).Count -ne [int]$file.lineCount -or
            (Get-FileHash $fixturePath -Algorithm SHA256).Hash -ne [string]$file.sha256) {
            Fail "The staged oversized fixture for $($file.path) does not match its contract."
        }
        $coverage = Get-ViewCoverage -Calls $preDispatchCalls -FileContract $file
        if (-not $coverage.Passed) {
            Fail $coverage.Evidence
        }
        $lastEvidenceIndex = [Math]::Max($lastEvidenceIndex, $coverage.LastResultIndex)
    }

    $manifest = $preDispatchCalls |
        Where-Object {
            $_.Name -eq 'view' -and
            [string](Get-Argument $_ 'path') -match '(?i)manifest\.json$' -and
            (Test-UsableResult $_) -and
            $_.ResultText -match '"version"\s*:\s*2' -and
            $_.ResultText -match '"ready"\s*:\s*true'
        } |
        Select-Object -First 1
    $routing = $preDispatchCalls |
        Where-Object {
            $_.Name -eq 'view' -and
            [string](Get-Argument $_ 'path') -match '(?i)routing\.md\.source$' -and
            (Test-UsableResult $_)
        } |
        Select-Object -First 1
    if ($null -eq $manifest -or $null -eq $routing) {
        Fail 'The hosted run did not read the ready manifest and routed guidance table before dispatch.'
    }
    $lastEvidenceIndex = [Math]::Max($lastEvidenceIndex, [Math]::Max($manifest.ResultIndex, $routing.ResultIndex))

    if ($firstTaskIndex -le $lastEvidenceIndex) {
        Fail 'A guide worker was dispatched before all required hosted bundle bytes were read.'
    }

    Complete-Grading -Passed $true -Evidence 'Observed native, content-bearing, gap-free oversized bundle reads with sentinels before worker dispatch and no shell fallback.'
}

function Test-RequiredBytesBlocked {
    param(
        [object]$GraderInput,
        [object[]]$Calls,
        [object[]]$Events
    )

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
            Where-Object { $_.CallIndex -le $manifestRead.ResultIndex }
    ).Count -ne 0) {
        Fail 'The exact required feedback input was attempted before the ready manifest read completed.'
    }
    $failedRead = $feedbackReads |
        Where-Object {
            $_.CallIndex -gt $manifestRead.ResultIndex -and
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
        'same-worker-lifecycle' { Test-SameWorkerLifecycle $calls }
        'hosted-pagination' { Test-HostedPagination $graderInput $calls }
        'required-bytes-blocked' { Test-RequiredBytesBlocked $graderInput $calls $events }
    }
} catch {
    Complete-Grading -Passed $false -Evidence "Trace grader error: $($_.Exception.Message)" -Status 'error'
}
