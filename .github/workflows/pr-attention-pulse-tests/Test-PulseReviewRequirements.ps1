#!/usr/bin/env pwsh
#Requires -Version 7.0

param(
    [string]$WorkflowPath = (Join-Path $PSScriptRoot "..\pr-attention-pulse.md"),
    [string]$LockPath = (Join-Path $PSScriptRoot "..\pr-attention-pulse.lock.yml")
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Test-PRAttentionPulse.ps1") -FunctionsOnly

function Assert-True
{
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

function Invoke-Control
{
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Action
    )

    try
    {
        & $Action
        Write-Output "PASS $Name"
    }
    catch
    {
        $script:failures.Add("$Name`: $($_.Exception.Message)")
        Write-Output "FAIL $Name`: $($_.Exception.Message)"
    }
}

function Get-CompiledJob
{
    param([string]$Name)

    $match = [regex]::Match($lock, "(?ms)^  $([regex]::Escape($Name)):\r?\n.*?(?=^  [A-Za-z_][A-Za-z0-9_-]*:\r?$|\z)")
    Assert-True $match.Success "The generated '$Name' job could not be isolated."

    return $match.Value
}

$testRoot = $PSScriptRoot
$workflowRoot = Split-Path -Parent $testRoot
$supportRoot = Join-Path $workflowRoot "pr-attention-pulse"
$combinerPath = Join-Path $workflowRoot "pr-attention-pulse/Combine-PRAttentionPulse.ps1"
$contractPath = Join-Path $workflowRoot "pr-attention-pulse/PRAttentionPulseContract.psm1"
$publishedFixturePath = Join-Path $testRoot "fixtures/presentation/published-34643961191.pulse.json"

$workflow = Get-Content -LiteralPath $workflowPath -Raw
$lock = Get-Content -LiteralPath $lockPath -Raw
$agentStepStart = $lock.IndexOf("- name: Execute GitHub Copilot CLI", [StringComparison]::Ordinal)
$agentStepEnd = $lock.IndexOf("- name: Detect agent errors", $agentStepStart, [StringComparison]::Ordinal)
Assert-True ($agentStepStart -ge 0 -and $agentStepEnd -gt $agentStepStart) "The generated main inference step could not be isolated."
$agentStep = $lock.Substring($agentStepStart, $agentStepEnd - $agentStepStart)

$failures = [Collections.Generic.List[string]]::new()

Invoke-Control "DetectorUsesSelectedPatPool" {
    $agentCredential = [regex]::Matches((Get-CompiledJob "agent"), "(?m)^          COPILOT_GITHUB_TOKEN: (.+)\r?$")
    $detectorCredential = [regex]::Matches((Get-CompiledJob "detection"), "(?m)^          COPILOT_GITHUB_TOKEN: (.+)\r?$")
    Assert-True ($agentCredential.Count -eq 1 -and $detectorCredential.Count -eq 1) "Each inference job must bind exactly one provider credential."
    $selectedPat = $agentCredential[0].Groups[1].Value.Trim()
    Assert-True ($selectedPat.Contains("needs.pat_pool.outputs.pat_number") -and $selectedPat.Contains("secrets.COPILOT_PAT_")) "The main agent must retain the existing PAT-pool selector."
    Assert-True ($detectorCredential[0].Groups[1].Value.Trim() -ceq $selectedPat) "The inline detector must use the main agent's selected PAT-pool expression, not the standalone COPILOT_GITHUB_TOKEN secret."
}

Invoke-Control "DetectorPoolDependencyAndEnvironment" {
    foreach ($name in @("agent", "detection"))
    {
        $job = Get-CompiledJob $name
        $needs = [regex]::Match($job, "(?m)^    needs:\r?\n(?:      - [^\r\n]+\r?\n)+").Value
        Assert-True ($needs -match "(?m)^      - pat_pool\r?$") "The '$name' job must depend directly on pat_pool."
        Assert-True ($job -match "(?m)^    environment: copilot-pat-pool\r?$") "The '$name' job must use the existing PAT-pool environment."
    }
}

Invoke-Control "InlineDetectorFailsClosed" {
    $detector = Get-CompiledJob "detection"
    Assert-True ($workflow -match "(?m)^  gh-aw-detection: false\r?$") "The inline-detector workaround must remain enabled."
    Assert-True ($detector.Contains("copilot_harness.cjs") -and $detector.Contains("parse_threat_detection_results.cjs")) "The generated detector must execute and parse inline detection."
    $continueOnError = [regex]::Matches($detector, '(?m)^          GH_AW_DETECTION_CONTINUE_ON_ERROR: "([^"]+)"\r?$')
    Assert-True ($continueOnError.Count -eq 2) "Both detection setup and result parsing must declare failure handling."
    foreach ($match in $continueOnError)
    {
        Assert-True ($match.Groups[1].Value -ceq "false") "Detection errors must not be accepted."
    }
    Assert-True ((Get-CompiledJob "safe_outputs").Contains("needs.detection.result == 'success'")) "Publication must require successful detection."
}

Invoke-Control "DetectorModelPolicy" {
    $detector = Get-CompiledJob "detection"
    Assert-True ($detector -match "(?m)^          COPILOT_MODEL: gpt-5\.6-sol\r?$") "Inline detection must retain the pinned model."
    $config = [regex]::Match($detector, "printf '%s\\n' '(?<config>\{.+\})' >")
    Assert-True $config.Success "The generated inline detector AWF configuration could not be read."
    $apiProxy = ($config.Groups["config"].Value | ConvertFrom-Json -Depth 50).apiProxy
    Assert-True (@($apiProxy.allowedModels).Count -eq 1 -and ($apiProxy.allowedModels -join ",") -ceq "gpt-5.6-sol") "The inline detector must enforce the singleton gpt-5.6-sol model policy."
    foreach ($name in @("enableTokenSteering", "maxAiCredits", "modelFallback"))
    {
        Assert-True ($null -eq $apiProxy.PSObject.Properties[$name]) "Inline detection must retain the existing absence of '$name'; steering stays disabled and the singleton policy prevents model fallback."
    }
}

Invoke-Control "ProviderCredentialExclusions" {
    foreach ($name in @("agent", "detection"))
    {
        Assert-True ([regex]::Matches((Get-CompiledJob $name), "(?<!\S)--exclude-env COPILOT_GITHUB_TOKEN(?=\s|\\\\)").Count -eq 1) "The '$name' inference command must exclude the provider credential exactly once."
    }
}

Invoke-Control "ActivationArtifactBoundary" {
    $activationCleanup = $lock.IndexOf("name: Remove repository data from activation artifact", [StringComparison]::Ordinal)
    $artifactUpload = $lock.IndexOf("name: Upload activation artifact", [StringComparison]::Ordinal)
    Assert-True ($activationCleanup -ge 0 -and $activationCleanup -lt $artifactUpload) "Activation repository data must be deleted before artifact staging."
    $cleanupStep = $lock.Substring($activationCleanup, $artifactUpload - $activationCleanup)
    foreach ($path in @("/tmp/gh-aw/base", "/tmp/gh-aw/.github/agents", "/tmp/gh-aw/.github/skills"))
    {
        Assert-True ($cleanupStep.Contains($path)) "Activation cleanup must remove and verify '$path'."
    }
}

Invoke-Control "EffectiveModelVisibleBoundary" {
    $artifactDownload = $lock.IndexOf("name: Download activation artifact", [StringComparison]::Ordinal)
    $preAgentCleanup = $lock.IndexOf("name: Enforce model-visible Pulse boundary", [StringComparison]::Ordinal)
    Assert-True ($preAgentCleanup -gt $artifactDownload -and $preAgentCleanup -lt $agentStepStart) "The model-visible boundary must be enforced after artifact download and before inference."
    $trustedCleanupStart = $lock.IndexOf("name: Protect canonical Pulse artifacts", [StringComparison]::Ordinal)
    Assert-True ($trustedCleanupStart -ge 0 -and $trustedCleanupStart -lt $preAgentCleanup) "Trusted workspace cleanup must run before the pre-agent boundary check."
    $trustedCleanupStep = $lock.Substring($trustedCleanupStart, $preAgentCleanup - $trustedCleanupStart)
    Assert-True ($trustedCleanupStep.Contains('Get-ChildItem -LiteralPath $env:GITHUB_WORKSPACE -Force')) "Trusted cleanup must enumerate every workspace root entry."
    Assert-True ($trustedCleanupStep.Contains('$_.Name, \".pr-attention-pulse\"')) "Trusted cleanup must preserve only the bounded Pulse directory."
    Assert-True ($trustedCleanupStep.Contains("Remove-Item -Recurse -Force")) "Trusted cleanup must delete every other workspace entry."
    Assert-True (-not $trustedCleanupStep.Contains("Remove-Item -Recurse -Force .github, .git")) "Trusted cleanup must not delete only repository metadata while leaving other checkout files."
    $cleanupStep = $lock.Substring($preAgentCleanup, $agentStepStart - $preAgentCleanup)
    foreach ($requiredText in @(
        "/tmp/gh-aw/base",
        "/tmp/gh-aw/.github/agents",
        "/tmp/gh-aw/.github/skills",
        ".github",
        ".git",
        ".pr-attention-pulse/pulse-input.json",
        ".pr-attention-pulse/pulse-body.md",
        ".pr-attention-pulse/pulse-request.json"))
    {
        Assert-True ($cleanupStep.Contains($requiredText)) "Pre-agent enforcement must check '$requiredText'."
    }
    Assert-True ($cleanupStep.Contains("find")) "Pre-agent enforcement must reject every workspace entry outside the bounded Pulse directory."

    $toolCommentStart = $agentStep.IndexOf("# Copilot CLI tool arguments (sorted):", [StringComparison]::Ordinal)
    $toolCommentEnd = $agentStep.IndexOf("timeout-minutes:", $toolCommentStart, [StringComparison]::Ordinal)
    Assert-True ($toolCommentStart -ge 0 -and $toolCommentEnd -gt $toolCommentStart) "The effective Copilot tool comment could not be isolated."
    $toolComment = $agentStep.Substring($toolCommentStart, $toolCommentEnd - $toolCommentStart)
    $actualShellTools = @(
        [regex]::Matches($toolComment, "(?m)^        # --allow-tool shell\((?<command>[^)]+)\)\r?$") |
            ForEach-Object { $_.Groups["command"].Value }
    )
    $expectedShellTools = @("cat", "date", "echo", "grep", "head", "ls", "printf", "pwd", "safeoutputs:*", "sort", "tail", "uniq", "wc", "yq")
    Assert-True ([string]::Equals(($actualShellTools -join ","), ($expectedShellTools -join ","), [StringComparison]::Ordinal)) "The effective v0.88.7 shell surface changed; actual: $($actualShellTools -join ',')."
    Assert-True (-not $toolComment.Contains("shell(jq)")) "The model must not receive jq."
    Assert-True ($agentStep.Contains("--add-dir /tmp/gh-aw/") -and $agentStep.Contains('--add-dir "${GITHUB_WORKSPACE}"')) "The regression control must account for both broad model-visible mounts."
    foreach ($path in @("/tmp/gh-aw/base", "/tmp/gh-aw/.github/agents", "/tmp/gh-aw/.github/skills"))
    {
        Assert-True (-not $agentStep.Contains($path)) "The generated inference command must not directly depend on deleted repository-derived path '$path'."
    }
    foreach ($requiredRuntime in @(
        "/tmp/gh-aw/aw-prompts/prompt.txt",
        '${RUNNER_TEMP}/gh-aw/actions/copilot_harness.cjs',
        '$HOME/.copilot/mcp-config.json',
        "safeoutputs:*"))
    {
        Assert-True ($agentStep.Contains($requiredRuntime)) "Compiler-required runtime reference '$requiredRuntime' must remain available after repository-data cleanup."
    }
}

Invoke-Control "SnapshotTrustedActionsBinding" {
    $agent = Get-CompiledJob "agent"
    foreach ($binding in @(
        @{ Name = "REPOSITORY"; Context = "repository"; Parameter = "Repository" },
        @{ Name = "SERVER_URL"; Context = "server_url"; Parameter = "ServerUrl" },
        @{ Name = "RUN_ID"; Context = "run_id"; Parameter = "RunId" },
        @{ Name = "RUN_ATTEMPT"; Context = "run_attempt"; Parameter = "RunAttempt" }))
    {
        $environment = "PULSE_SNAPSHOT_$($binding.Name)"
        $assignment = $environment + ': ${{ github.' + $binding.Context + ' }}'
        Assert-True ([regex]::Matches($agent, [regex]::Escape($assignment)).Count -eq 2) "Both preparation and validation must bind '$environment' from trusted Actions context."
        Assert-True ($agent.Contains("-$($binding.Parameter) `$env:$environment") -and
            $agent.Contains("-Expected$($binding.Parameter) `$env:$environment")) "The renderer and private validator must both consume '$environment'."
        Assert-True (-not $agentStep.Contains($environment)) "Snapshot preparation context must not be supplied to inference."
    }
    Assert-True ($agent.Contains('-SnapshotContextPath .pr-attention-pulse/pulse-snapshot-context.json') -and
        $agent.Contains('pr-attention-pulse-validator/pulse-snapshot-context.json')) "Frozen identity must be staged once and then read from the private canonical directory."
    $renderer = Get-Content -LiteralPath (Join-Path $supportRoot "Render-PRAttentionPulse.ps1") -Raw
    Assert-True ([regex]::Matches($renderer, '\[datetime\]::UtcNow').Count -eq 1) "Snapshot generation must sample the UTC clock only once."
    $validator = Get-Content -LiteralPath (Join-Path $supportRoot "Validate-PRAttentionPulseOutput.ps1") -Raw
    Assert-True (-not $validator.Contains("UtcNow")) "Post-agent validation must reuse the frozen timestamp, not resample the clock."
    Assert-True ($workflow.Contains("The Snapshot section is supplied by trusted Actions preparation, not by the queue JSON.")) "The model must copy trusted identity instead of inventing it from queue data."
}

Invoke-Control "SnapshotPrivateContextBoundary" {
    $protection = [regex]::Match($lock, "(?ms)^      - name: Protect canonical Pulse artifacts\r?\n.*?(?=^      - |\z)").Value
    $copy = $protection.IndexOf('Copy-Item .pr-attention-pulse/pulse-snapshot-context.json $validatorRoot', [StringComparison]::Ordinal)
    $remove = $protection.IndexOf('Remove-Item .pr-attention-pulse/pulse-snapshot-context.json -Force', [StringComparison]::Ordinal)
    Assert-True ($copy -gt $protection.IndexOf('Assert-PrivateValidatorRoot -Path $validatorRoot', [StringComparison]::Ordinal) -and
        $remove -gt $copy) "The checked private copy must exist before the workspace context is removed."
    $boundary = [regex]::Match($lock, "(?ms)^      - name: Enforce model-visible Pulse boundary\r?\n.*?(?=^      - |\z)").Value
    Assert-True ($boundary.Length -gt 0 -and -not $boundary.Contains("pulse-snapshot-context.json")) "The exact three-file model allowlist must not expand."
    foreach ($file in @("pulse-body.md", "pulse-input.json", "pulse-request.json"))
    {
        Assert-True ([regex]::Matches($boundary, [regex]::Escape(".pr-attention-pulse/$file")).Count -eq 2) "The boundary must still list and verify '$file'."
    }
    Assert-True ([regex]::Matches($lock, 'Remove-Item \.pr-attention-pulse/pulse-snapshot-context\.json -Force').Count -eq 2) "Both pre-inference staging and final cleanup must remove the workspace context."
    Assert-True ($lock.Contains('Remove-Item "${{ runner.temp }}/pr-attention-pulse-validator" -Recurse -Force')) "The existing private-directory cleanup must also remove frozen identity."
}

Invoke-Control "SnapshotNonOverwritingEvidenceAndGates" {
    $upload = [regex]::Match($lock, "(?ms)^      - name: Upload validated Pulse publication evidence\r?\n.*?(?=^      - |\z)").Value
    Assert-True ($upload.Contains("uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a")) "Evidence must retain the pinned uploader."
    Assert-True ($upload -match "(?m)^\s+name: pulse-publication-evidence\r?$" -and
        $upload -match "(?m)^\s+retention-days: 7\r?$" -and
        $upload -match "(?m)^\s+if-no-files-found: error\r?$") "The artifact name, retention and required-files policy must remain unchanged."
    Assert-True (-not ($upload -match "(?m)^\s*(?:overwrite|continue-on-error|if):")) "Upload must not overwrite evidence or run/succeed past an earlier failure."
    Assert-True ([regex]::Matches($upload, 'pr-attention-pulse-validator/pulse-(?:input\.json|body\.md)').Count -eq 2 -and
        -not $upload.Contains("pulse-snapshot-context.json")) "Only the exact private JSON/body pair may be exported."
    Assert-True ($lock.IndexOf("name: Validate the sole publication payload", [StringComparison]::Ordinal) -lt
        $lock.IndexOf("name: Upload validated Pulse publication evidence", [StringComparison]::Ordinal)) "Private validation must precede upload."
    Assert-True ((Get-CompiledJob "safe_outputs").Contains("needs.agent.result == 'success'") -and
        (Get-CompiledJob "safe_outputs").Contains("needs.detection.result == 'success'") -and
        (Get-CompiledJob "detection").Contains("needs.agent.result == 'success'")) "Publication and detection must require agent success, including evidence upload."
}

Invoke-Control "GitHubCredentialExclusions" {
    foreach ($name in @("GH_TOKEN", "GH_AW_GITHUB_TOKEN", "GITHUB_MCP_SERVER_TOKEN", "GITHUB_TOKEN"))
    {
        Assert-True ([regex]::Matches($agentStep, "(?<!\S)--exclude-env $([regex]::Escape($name))(?=\s|\\\\)").Count -eq 1) "The main inference command must exclude '$name' exactly once."
    }
}

Invoke-Control "TelemetryCredentialExclusions" {
    foreach ($name in @("OTEL_EXPORTER_OTLP_HEADERS", "GH_AW_OTLP_ENDPOINTS"))
    {
        Assert-True ([regex]::Matches($agentStep, "(?<!\S)--exclude-env $([regex]::Escape($name))(?=\s|\\\\)").Count -eq 1) "The main inference command must exclude '$name' exactly once."
    }
}

Invoke-Control "FixedDashboardTarget" {
    $issueNumber = 69328
    Assert-True (-not $workflow.Contains("PR_ATTENTION_PULSE_ISSUE_NUMBER")) "The workflow must not depend on the retired repository variable."
    Assert-True (-not $lock.Contains("PR_ATTENTION_PULSE_ISSUE_NUMBER")) "The generated workflow must not depend on the retired repository variable."
    Assert-True (-not $workflow.Contains("resolve_dashboard_target")) "The configurable target resolver must be removed."
    Assert-True (-not $lock.Contains("resolve_dashboard_target")) "The generated configurable target resolver must be removed."
    Assert-True ($workflow.Contains("target: `"$issueNumber`"")) "The source safe-output policy must hardcode the permanent dashboard issue."
    Assert-True ($lock.Replace("\", "").Contains('"target":"69328"')) "The generated handler policy must hardcode the permanent dashboard issue."
    Assert-True ($workflow.Contains("issue_number: $issueNumber")) "The prompt payload contract must name the permanent dashboard issue."
    Assert-True ($workflow.Contains("const issueNumber = $issueNumber;")) "Trusted request serialization must hardcode the permanent dashboard issue."
    Assert-True ($workflow.Contains("`$dashboardIssueNumber = $issueNumber")) "The private validator must hardcode the permanent dashboard issue."

    $validatorStart = $lock.IndexOf("validate_dashboard_target:", [StringComparison]::Ordinal)
    Assert-True ($validatorStart -ge 0) "The trusted fixed-target validation job must be generated."
    $agentJobStart = $lock.IndexOf("`n  agent:", [StringComparison]::Ordinal)
    $agentNeeds = $lock.Substring($agentJobStart, $agentStepStart - $agentJobStart)
    Assert-True ($agentNeeds.Contains("validate_dashboard_target")) "The agent job must depend on successful fixed-target validation."
    $safeOutputsStart = $lock.IndexOf("`n  safe_outputs:", [StringComparison]::Ordinal)
    $handlerStep = $lock.IndexOf("name: Process Safe Outputs", $safeOutputsStart, [StringComparison]::Ordinal)
    $safeOutputTargetCheck = $lock.IndexOf("name: Revalidate fixed Pulse dashboard target", $safeOutputsStart, [StringComparison]::Ordinal)
    Assert-True ($safeOutputTargetCheck -gt $safeOutputsStart -and $safeOutputTargetCheck -lt $handlerStep) "The fixed dashboard target must be revalidated immediately before safe-output handling."
    $safeOutputPrelude = $lock.Substring($safeOutputsStart, $handlerStep - $safeOutputsStart)
    Assert-True ($safeOutputPrelude.Contains("validate_dashboard_target")) "The safe-output job must depend on successful fixed-target validation."

    foreach ($requiredText in @(
        'dotnet/aspnetcore',
        'repos/dotnet/aspnetcore/issues/69328',
        'pull_request',
        'state',
        '[pr-attention-pulse]'))
    {
        Assert-True ($workflow.Contains($requiredText)) "The trusted target checks must enforce '$requiredText'."
    }
    Assert-True ([regex]::Matches($workflow, 'GITHUB_REPOSITORY.*dotnet/aspnetcore').Count -eq 2) "Execution and publication must each enforce the canonical upstream repository."
    Assert-True ([regex]::Matches($workflow, 'repos/dotnet/aspnetcore/issues/69328').Count -eq 2) "The fixed issue must be checked before inference and immediately before publication."
    Assert-True ([regex]::Matches($workflow, 'PSObject\.Properties\["pull_request"\]').Count -eq 2) "Both target checks must reject a pull request."
    Assert-True ([regex]::Matches($workflow, '\[string\]\$issue\.state, "open"').Count -eq 2) "Both target checks must require the issue to remain open."
    Assert-True ([regex]::Matches($workflow, '\(\[string\]\$issue\.title\)\.StartsWith\("\[pr-attention-pulse\]"').Count -eq 2) "Both target checks must require the dashboard title prefix."
    Assert-True ($workflow.Contains('-ExpectedIssueNumber $dashboardIssueNumber')) "The private validator must enforce the permanent issue number."
}

Invoke-Control "DeterministicClickableReferences" {
    Import-Module -Scope Local -Force $contractPath
    $pulse = Get-Content -LiteralPath $publishedFixturePath -Raw | ConvertFrom-Json -Depth 100
    $pulse | Add-Member -NotePropertyName scope -NotePropertyValue "repository-wide"
    $body = ConvertTo-PRAttentionPulseBody -Pulse $pulse -Json (Get-FixtureSnapshotJson -Pulse $pulse) -SnapshotContext (Get-FixtureSnapshotContext -Pulse $pulse)
    $pulse = Resolve-PulseMergeArea -Area $pulse
    $expectedNumbers = @(
        foreach ($viewName in @("reviewNow", "verifyDiscussionBeforeReview", "needsRescue", "readyToMerge", "verifyDiscussionBeforeMerge"))
        {
            foreach ($candidate in @($pulse.views.$viewName))
            {
                [string]$candidate.number
            }
        }
    )
    $matches = @([regex]::Matches($body, "\[dotnet/aspnetcore#(?<label>[1-9][0-9]*)\]\(https://github\.com/dotnet/aspnetcore/pull/(?<target>[1-9][0-9]*)\)"))
    Assert-True ($matches.Count -eq $expectedNumbers.Count) "Every displayed candidate must have exactly one deterministic upstream pull-request link."
    $actualNumbers = @(
        foreach ($match in $matches)
        {
            Assert-True ([string]::Equals($match.Groups["label"].Value, $match.Groups["target"].Value, [StringComparison]::Ordinal)) "A link label and target number differ."
            $match.Groups["label"].Value
        }
    )
    Assert-True ([string]::Equals(($actualNumbers -join ","), ($expectedNumbers -join ","), [StringComparison]::Ordinal)) "Clickable references must preserve exact candidate membership and ordering."
    Assert-True (@($actualNumbers | Select-Object -Unique).Count -eq $actualNumbers.Count) "Every displayed pull request number must appear exactly once."
}

Invoke-Control "BlazorScopeAndPresentation" {
    $productionInvocations = @([regex]::Matches(
        $workflow,
        "(?m)pwsh \.github/skills/pr-attention-queue/scripts/Get-PRAttentionQueue\.ps1(?<arguments>.*?) > \.pr-attention-pulse/queue-(?<scope>[^ ]+)-raw\.json"))
    Assert-True ($productionInvocations.Count -eq 2) "Exactly two independent production queue invocations are required."
    $blazorArguments = ($productionInvocations[0].Groups["arguments"].Value -replace "\s+", " ").Trim()
    $repositoryWideArguments = ($productionInvocations[1].Groups["arguments"].Value -replace "\s+", " ").Trim()
    Assert-True ([string]::Equals(
        $blazorArguments,
        "-Repository dotnet/aspnetcore -Preset blazor -DisablePersonalInbox -OutputFormat Json",
        [StringComparison]::Ordinal)) "The first producer must use the explicit Blazor preset; actual arguments: '$blazorArguments'."
    Assert-True ([string]::Equals(
        $repositoryWideArguments,
        "-Repository dotnet/aspnetcore -AllRepo -DisablePersonalInbox -OutputFormat Json",
        [StringComparison]::Ordinal)) "The second producer must use the repository-wide baseline; actual arguments: '$repositoryWideArguments'."
    Assert-True ($productionInvocations[0].Groups["scope"].Value -ceq "blazor" -and
        $productionInvocations[1].Groups["scope"].Value -ceq "repository-wide") "The two raw producer outputs must remain independently named and ordered."
    foreach ($path in @(
        "queue-blazor-raw.json",
        "queue-repository-wide-raw.json",
        "pulse-blazor.json",
        "pulse-repository-wide.json"))
    {
        Assert-True ($workflow.Contains($path)) "The trusted workflow must explicitly handle and delete '$path'."
    }

    $combiner = Get-Content -LiteralPath $combinerPath -Raw
    Assert-True (-not $combiner.Contains('-OpenByDefault $true')) "No Pulse area may be expanded by default."
    Assert-True ([regex]::Matches($combiner, '(?m)^\s*-OpenByDefault \$false').Count -eq 2) "Both Pulse area envelopes must be collapsed by default."
    $contract = Get-Content -LiteralPath $contractPath -Raw
    Assert-True ($contract.Contains("This initial area composition includes the maintained **Blazor** view and a **Repository-wide** baseline.")) "The trusted renderer must explain the two initial scopes."
    Assert-True ($contract.Contains("Additional product areas will be added only after maintainers define their exact label/path queries")) "The trusted renderer must defer future area taxonomy and presentation decisions."
}

if ($failures.Count -gt 0)
{
    throw "$($failures.Count) review requirement control(s) failed."
}

Write-Output "All Pulse review requirement controls passed."
