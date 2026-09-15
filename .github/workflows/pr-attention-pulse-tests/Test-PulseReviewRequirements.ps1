#!/usr/bin/env pwsh
#Requires -Version 7.0

$ErrorActionPreference = "Stop"

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

$testRoot = $PSScriptRoot
$workflowRoot = Split-Path -Parent $testRoot
$workflowPath = Join-Path $workflowRoot "pr-attention-pulse.md"
$lockPath = Join-Path $workflowRoot "pr-attention-pulse.lock.yml"
$contractPath = Join-Path $workflowRoot "pr-attention-pulse/PRAttentionPulseContract.psm1"
$publishedFixturePath = Join-Path $testRoot "fixtures/presentation/published-34643961191.pulse.json"

$workflow = Get-Content -LiteralPath $workflowPath -Raw
$lock = Get-Content -LiteralPath $lockPath -Raw
$agentStepStart = $lock.IndexOf("- name: Execute GitHub Copilot CLI", [StringComparison]::Ordinal)
$agentStepEnd = $lock.IndexOf("- name: Detect agent errors", $agentStepStart, [StringComparison]::Ordinal)
Assert-True ($agentStepStart -ge 0 -and $agentStepEnd -gt $agentStepStart) "The generated main inference step could not be isolated."
$agentStep = $lock.Substring($agentStepStart, $agentStepEnd - $agentStepStart)

$failures = [Collections.Generic.List[string]]::new()

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
        [regex]::Matches($toolComment, "(?m)^        # --allow-tool shell\((?<command>[^)]+)\)$") |
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

Invoke-Control "DeterministicClickableReferences" {
    Import-Module -Scope Local -Force $contractPath
    $pulse = Get-Content -LiteralPath $publishedFixturePath -Raw | ConvertFrom-Json -Depth 100
    $body = ConvertTo-PRAttentionPulseBody -Pulse $pulse
    $expectedNumbers = @(
        foreach ($viewName in @("reviewNow", "verifyDiscussionBeforeReview", "needsRescue", "readyToMerge"))
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

if ($failures.Count -gt 0)
{
    throw "$($failures.Count) review requirement control(s) failed."
}

Write-Output "All Pulse review requirement controls passed."
