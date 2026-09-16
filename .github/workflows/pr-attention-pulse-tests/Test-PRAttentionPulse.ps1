#!/usr/bin/env pwsh
#Requires -Version 7.0

$ErrorActionPreference = "Stop"
$script:DashboardIssueNumber = 69328

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

function Assert-Throws
{
    param(
        [Parameter(Mandatory)][scriptblock]$Action,
        [Parameter(Mandatory)][string]$Message,
        [string]$ExpectedMessage
    )

    try
    {
        & $Action
    }
    catch
    {
        if (-not [string]::IsNullOrEmpty($ExpectedMessage))
        {
            Assert-True ($_.Exception.Message.Contains($ExpectedMessage, [StringComparison]::Ordinal)) "Expected rejection '$ExpectedMessage'; actual: $($_.Exception.Message)"
        }

        return
    }

    throw $Message
}

function Write-JsonFile
{
    param(
        [Parameter(Mandatory)][object]$Value,
        [Parameter(Mandatory)][string]$Path
    )

    $json = $Value | ConvertTo-Json -Depth 100
    [IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}

function Invoke-Sanitizer
{
    param(
        [string]$FixtureName,
        [string]$SourcePath,
        [ValidateSet("blazor", "repository-wide")]
        [string]$Scope = "blazor",
        [int]$CollectionExitCode = 0,
        [int]$MaxOutputBytes = 65536
    )

    $inputPath = Join-Path $tempRoot "input-$([guid]::NewGuid().ToString('N')).json"
    $outputPath = Join-Path $tempRoot "output-$([guid]::NewGuid().ToString('N')).json"
    if ($FixtureName)
    {
        Copy-Item (Join-Path $fixtureRoot $FixtureName) $inputPath
    }
    elseif ($SourcePath)
    {
        Copy-Item $SourcePath $inputPath
    }

    try
    {
        & $sanitizerPath `
            -InputPath $inputPath `
            -OutputPath $outputPath `
            -AttemptTimestamp $attemptTimestamp `
            -CollectionExitCode $CollectionExitCode `
            -Scope $Scope `
            -MaxOutputBytes $MaxOutputBytes

        Assert-True (-not (Test-Path $inputPath)) "The raw input must always be deleted."
        return Get-Content -Raw $outputPath | ConvertFrom-Json -Depth 100
    }
    finally
    {
        Remove-Item $inputPath, $outputPath -Force -ErrorAction SilentlyContinue
    }
}

function New-CombinedPulse
{
    param(
        [Parameter(Mandatory)][object]$Blazor,
        [Parameter(Mandatory)][object]$RepositoryWide
    )

    $blazorPath = Join-Path $tempRoot "blazor-$([guid]::NewGuid().ToString('N')).json"
    $repositoryWidePath = Join-Path $tempRoot "repository-wide-$([guid]::NewGuid().ToString('N')).json"
    $outputPath = Join-Path $tempRoot "combined-$([guid]::NewGuid().ToString('N')).json"
    try
    {
        Write-JsonFile -Value $Blazor -Path $blazorPath
        Write-JsonFile -Value $RepositoryWide -Path $repositoryWidePath
        & $combinerPath `
            -BlazorInputPath $blazorPath `
            -RepositoryWideInputPath $repositoryWidePath `
            -OutputPath $outputPath
        Assert-True (-not (Test-Path $blazorPath) -and -not (Test-Path $repositoryWidePath)) "Intermediate area envelopes must be deleted after combination."

        return Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json -Depth 100
    }
    finally
    {
        Remove-Item $blazorPath, $repositoryWidePath, $outputPath -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-Renderer
{
    param([Parameter(Mandatory)][object]$Pulse)

    $inputPath = Join-Path $tempRoot "pulse-$([guid]::NewGuid().ToString('N')).json"
    $outputPath = Join-Path $tempRoot "body-$([guid]::NewGuid().ToString('N')).md"
    try
    {
        Write-JsonFile -Value $Pulse -Path $inputPath
        & $rendererPath -InputPath $inputPath -OutputPath $outputPath
        return Get-Content -Raw $outputPath
    }
    finally
    {
        Remove-Item $inputPath, $outputPath -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-PublicationValidator
{
    param(
        [Parameter(Mandatory)][object]$Pulse,
        [object]$AgentOutput,
        [string]$AgentOutputJson,
        [string]$ExpectedBody
    )

    $pulsePath = Join-Path $tempRoot "pulse-$([guid]::NewGuid().ToString('N')).json"
    $bodyPath = Join-Path $tempRoot "body-$([guid]::NewGuid().ToString('N')).md"
    $outputPath = Join-Path $tempRoot "agent-$([guid]::NewGuid().ToString('N')).json"
    $safeOutputsPath = Join-Path (Split-Path -Parent $outputPath) "safeoutputs.jsonl"
    try
    {
        if ($null -eq $ExpectedBody)
        {
            $ExpectedBody = Invoke-PinnedOutputSanitizer -Content (Invoke-Renderer -Pulse $Pulse)
        }

        Write-JsonFile -Value $Pulse -Path $pulsePath
        [IO.File]::WriteAllText($bodyPath, $ExpectedBody, [Text.UTF8Encoding]::new($false))
        if ($PSBoundParameters.ContainsKey("AgentOutputJson"))
        {
            [IO.File]::WriteAllText($outputPath, $AgentOutputJson, [Text.UTF8Encoding]::new($false))
        }
        else
        {
            Write-JsonFile -Value $AgentOutput -Path $outputPath
        }
        [IO.File]::WriteAllText($safeOutputsPath, '{"type":"update_issue"}' + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
        try
        {
            $validatorMessages = @(& pwsh -NoProfile -File $validatorPath `
                -AgentOutputPath $outputPath `
                -PulseInputPath $pulsePath `
                -ExpectedBodyPath $bodyPath `
                -SanitizerModulePath $collectorSanitizerPath `
                -ExpectedIssueNumber $script:DashboardIssueNumber 2>&1)
            $validatorExitCode = $LASTEXITCODE
            if ($validatorExitCode -ne 0)
            {
                throw "The native publication validator exited with code $validatorExitCode`: $($validatorMessages -join ' ')"
            }

            $retainedOutput = Get-Content -Raw $outputPath | ConvertFrom-Json -Depth 100
            Assert-True (@($retainedOutput.items).Count -eq 1) "A valid publication payload must remain intact."
            Assert-True ([string]::Equals(@($retainedOutput.items)[0].body, $ExpectedBody, [StringComparison]::Ordinal)) "A valid publication body must remain byte-identical."
        }
        catch
        {
            $failedClosed = Get-Content -Raw $outputPath | ConvertFrom-Json -Depth 10
            Assert-True (@($failedClosed.PSObject.Properties).Count -eq 2) "Rejected output must be replaced by the exact empty collector envelope."
            Assert-True ($failedClosed.items -is [array] -and $failedClosed.items.Count -eq 0) "Rejected output must leave no publishable item."
            Assert-True ($failedClosed.errors -is [array] -and $failedClosed.errors.Count -eq 0) "Rejected output must retain an empty collector errors array."
            Assert-True (-not (Test-Path $safeOutputsPath)) "Rejected output must remove the copied raw safe-output stream."
            Assert-True (-not (Test-Path "$outputPath.rejected")) "Rejected output quarantine must not be published as an artifact."
            throw
        }
    }
    finally
    {
        Remove-Item $pulsePath, $bodyPath, $outputPath, $safeOutputsPath -Force -ErrorAction SilentlyContinue
    }
}

function New-ValidAgentOutput
{
    param([Parameter(Mandatory)][string]$Body)

    return [pscustomobject]@{
        items = @(
            [pscustomobject]@{
                type = "update_issue"
                issue_number = $script:DashboardIssueNumber
                operation = "replace"
                body = $Body
            }
        )
        errors = @()
    }
}

function Get-GhAwExtensionRoot
{
    $candidateRoots = @(
        $(if ($env:GH_CONFIG_DIR) { Join-Path $env:GH_CONFIG_DIR "extensions/gh-aw" }),
        $(if ($env:LOCALAPPDATA) { Join-Path $env:LOCALAPPDATA "GitHub CLI/extensions/gh-aw" }),
        $(if ($env:XDG_DATA_HOME) { Join-Path $env:XDG_DATA_HOME "gh/extensions/gh-aw" }),
        $(if ($HOME) { Join-Path $HOME ".local/share/gh/extensions/gh-aw" }),
        $(if ($HOME) { Join-Path $HOME ".config/gh/extensions/gh-aw" })
    ) | Where-Object { $_ }
    foreach ($candidateRoot in $candidateRoots)
    {
        if (Test-Path -LiteralPath (Join-Path $candidateRoot "actions/setup/js/sanitize_content.cjs"))
        {
            return $candidateRoot
        }
    }

    $previousDebug = $env:GH_DEBUG
    try
    {
        $env:GH_DEBUG = "api"
        $debugOutput = @(& gh aw --version 2>&1)
        if ($LASTEXITCODE -ne 0)
        {
            throw "The installed gh-aw extension could not be inspected."
        }
    }
    finally
    {
        if ($null -eq $previousDebug)
        {
            Remove-Item Env:GH_DEBUG -ErrorAction SilentlyContinue
        }
        else
        {
            $env:GH_DEBUG = $previousDebug
        }
    }

    foreach ($line in $debugOutput)
    {
        $match = [regex]::Match([string]$line, "^\[git(?:\.exe)? -C (.+) config remote\.origin\.url\]$")
        if ($match.Success)
        {
            return $match.Groups[1].Value.Trim('"')
        }
    }

    throw "Could not locate the installed gh-aw extension."
}

function Invoke-PinnedOutputSanitizer
{
    param([Parameter(Mandatory)][string]$Content)

    $inputPath = Join-Path $tempRoot "collector-input-$([guid]::NewGuid().ToString('N')).txt"
    $outputPath = Join-Path $tempRoot "collector-output-$([guid]::NewGuid().ToString('N')).txt"
    try
    {
        [IO.File]::WriteAllText($inputPath, $Content, [Text.UTF8Encoding]::new($false))
        $nodeScript = @'
const fs = require("fs");
global.core = { info() {}, warning() {}, error() {} };
const { sanitizeContent } = require(process.argv[2]);
const input = fs.readFileSync(process.argv[3], "utf8");
const output = sanitizeContent(input, {
  allowedAliases: [],
});
fs.writeFileSync(process.argv[4], output, "utf8");
'@
        $previousUrls = $env:GH_AW_SAFE_OUTPUTS_URLS
        $previousReferences = $env:GH_AW_ALLOWED_GITHUB_REFS
        try
        {
            $env:GH_AW_SAFE_OUTPUTS_URLS = "allowed-or-code-region"
            $env:GH_AW_ALLOWED_GITHUB_REFS = "dotnet/aspnetcore"
            $nodeScript | node - $collectorSanitizerPath $inputPath $outputPath
            if ($LASTEXITCODE -ne 0)
            {
                throw "The pinned gh-aw output sanitizer failed."
            }
        }
        finally
        {
            if ($null -eq $previousUrls)
            {
                Remove-Item Env:GH_AW_SAFE_OUTPUTS_URLS -ErrorAction SilentlyContinue
            }
            else
            {
                $env:GH_AW_SAFE_OUTPUTS_URLS = $previousUrls
            }
            if ($null -eq $previousReferences)
            {
                Remove-Item Env:GH_AW_ALLOWED_GITHUB_REFS -ErrorAction SilentlyContinue
            }
            else
            {
                $env:GH_AW_ALLOWED_GITHUB_REFS = $previousReferences
            }
        }

        return Get-Content -LiteralPath $outputPath -Raw
    }
    finally
    {
        Remove-Item $inputPath, $outputPath -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-PinnedCollector
{
    param([Parameter(Mandatory)][string]$Body)

    $collectorRoot = Join-Path $tempRoot "collector-$([guid]::NewGuid().ToString('N'))"
    $safeOutputPath = Join-Path $collectorRoot "outputs.jsonl"
    $configPath = Join-Path $collectorRoot "config.json"
    $validationRoot = Join-Path $collectorRoot "gh-aw/safeoutputs"
    $validationPath = Join-Path $validationRoot "validation.json"
    $collectorOutputRoot = Join-Path $collectorRoot "collector-output"
    try
    {
        New-Item -ItemType Directory -Force $validationRoot, $collectorOutputRoot | Out-Null
        $rawItem = [ordered]@{
            type = "update_issue"
            issue_number = $script:DashboardIssueNumber
            operation = "replace"
            body = $Body
        }
        [IO.File]::WriteAllText(
            $safeOutputPath,
            ($rawItem | ConvertTo-Json -Depth 10 -Compress),
            [Text.UTF8Encoding]::new($false))
        [IO.File]::WriteAllText(
            $configPath,
            "{`"max_bot_mentions`":`"0`",`"mentions`":{`"enabled`":false},`"update_issue`":{`"allow_body`":true,`"footer`":false,`"max`":1,`"required_title_prefix`":`"[pr-attention-pulse]`",`"target`":`"$script:DashboardIssueNumber`"}}",
            [Text.UTF8Encoding]::new($false))
        [IO.File]::WriteAllText(
            $validationPath,
            '{"mentions":{"enabled":false},"update_issue":{"defaultMax":1,"fields":{"assignees":{"type":"array","itemType":"string","itemSanitize":true,"itemMaxLength":39},"body":{"type":"string","sanitize":true,"maxLength":65000},"issue_number":{"issueOrPRNumber":true},"labels":{"type":"array"},"milestone":{"optionalPositiveInteger":true},"operation":{"type":"string","enum":["replace","append","prepend","replace-island"]},"repo":{"type":"string","maxLength":256},"status":{"type":"string","enum":["open","closed"]},"title":{"type":"string","sanitize":true,"maxLength":128}},"customValidation":"requiresOneOf:status,title,body,labels,assignees,milestone"}}',
            [Text.UTF8Encoding]::new($false))

        $collectorScript = @'
global.core = {
  info() {},
  warning() {},
  error() {},
  setOutput() {},
  exportVariable() {},
  setFailed(message) { throw new Error(message); },
};
global.context = {
  repo: { owner: "PureWeen", repo: "aspnetcore" },
  eventName: "workflow_dispatch",
  payload: {},
};
global.github = {};
const constants = require(process.argv[3]);
constants.TMP_GH_AW_PATH = process.argv[6];
process.env.GH_AW_SAFE_OUTPUTS = process.argv[4];
process.env.GH_AW_SAFE_OUTPUTS_CONFIG_PATH = process.argv[5];
process.env.RUNNER_TEMP = process.argv[7];
process.env.GH_AW_SAFE_OUTPUTS_URLS = "allowed-or-code-region";
process.env.GH_AW_ALLOWED_GITHUB_REFS = "dotnet/aspnetcore";
require(process.argv[2]).main().catch(error => {
  console.error(error);
  process.exit(1);
});
'@
        $collectorScript |
            node - `
                (Join-Path $collectorJsRoot "collect_ndjson_output.cjs") `
                (Join-Path $collectorJsRoot "constants.cjs") `
                $safeOutputPath `
                $configPath `
                $collectorOutputRoot `
                $collectorRoot
        if ($LASTEXITCODE -ne 0)
        {
            throw "The pinned gh-aw output collector failed."
        }

        $collectorOutputPath = Join-Path $collectorOutputRoot "agent_output.json"
        if (-not (Test-Path -LiteralPath $collectorOutputPath))
        {
            throw "The pinned gh-aw output collector did not produce agent_output.json."
        }

        return Get-Content -LiteralPath $collectorOutputPath -Raw | ConvertFrom-Json -Depth 100
    }
    finally
    {
        Remove-Item -LiteralPath $collectorRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-RealQueueFixture
{
    param(
        [Parameter(Mandatory)][string]$FixtureName,
        [ValidateSet("blazor", "repository-wide")]
        [string]$Scope = "blazor"
    )

    $queueOutput = Join-Path $tempRoot "queue-$([guid]::NewGuid().ToString('N')).json"
    try
    {
        if ($Scope -ceq "blazor")
        {
            & $queueScript `
                -Repository dotnet/aspnetcore `
                -Preset blazor `
                -DisablePersonalInbox `
                -InputPath (Join-Path $queueFixtureRoot $FixtureName) `
                -Now $queueSnapshot `
                -OutputFormat Json > $queueOutput
        }
        else
        {
            & $queueScript `
                -Repository dotnet/aspnetcore `
                -AllRepo `
                -DisablePersonalInbox `
                -InputPath (Join-Path $queueFixtureRoot $FixtureName) `
                -Now $queueSnapshot `
                -OutputFormat Json > $queueOutput
        }
        Assert-True $? "The real queue script failed for $FixtureName."

        return Invoke-Sanitizer -SourcePath $queueOutput -Scope $Scope
    }
    finally
    {
        Remove-Item $queueOutput -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-PresentationCase
{
    param([string]$CaseName, [scriptblock]$Action)

    try
    {
        & $Action
        Write-Output "PASS $CaseName"
    }
    catch
    {
        $failure = "$CaseName`: $($_.Exception.Message)"
        $presentationFailures.Add($failure)
        Write-Output "FAIL $failure"
    }
}

function Get-PulseAreaBlock
{
    param(
        [Parameter(Mandatory)][string]$Body,
        [Parameter(Mandatory)][string]$Label
    )

    $summary = "<summary><strong>$Label</strong>"
    $summaryStart = $Body.IndexOf($summary, [StringComparison]::Ordinal)
    Assert-True ($summaryStart -ge 0) "The '$Label' area summary is missing."
    $detailsStart = $Body.LastIndexOf("<details>", $summaryStart, [StringComparison]::Ordinal)
    $detailsEnd = $Body.IndexOf("</details>", $summaryStart, [StringComparison]::Ordinal)
    Assert-True ($detailsStart -ge 0 -and $detailsEnd -gt $summaryStart) "The '$Label' area details block is invalid."

    return $Body.Substring($detailsStart, ($detailsEnd + "</details>".Length) - $detailsStart)
}

function New-PresentationAreaFromRawFixture
{
    param(
        [Parameter(Mandatory)][ValidateSet("blazor", "repository-wide")][string]$Scope,
        [int]$CandidateNumber,
        [string]$CommunityReasonCode
    )

    $raw = Get-Content -LiteralPath (Join-Path $fixtureRoot "normal-legacy.json") -Raw | ConvertFrom-Json -Depth 100
    if ($Scope -ceq "repository-wide")
    {
        $raw.filter.name = "adhoc"
        $raw.filter.description = "Ad hoc pull-request scope"
        $raw.filter.coverage = "all-repo"
        $raw.filter.selection = "(all open pull requests)"
        $raw.filter.allRepositoryPullRequests = $true
        foreach ($item in $raw.items)
        {
            $item.scopeMatch = "all-repo"
        }
    }

    if ($CandidateNumber -gt 0)
    {
        $candidate = @($raw.items | Where-Object { $_.number -eq $CandidateNumber })
        Assert-True ($candidate.Count -eq 1) "The focused candidate '$CandidateNumber' is missing."
        $candidate = $candidate[0]
        $candidate.reasonCodes = @($candidate.reasonCodes | Where-Object { $_ -cne "community-contribution" -and $_ -cne "community-contribution-extra" })
        if (-not [string]::IsNullOrEmpty($CommunityReasonCode))
        {
            $candidate.reasonCodes = @($candidate.reasonCodes) + $CommunityReasonCode
        }
        if ($CommunityReasonCode -cne "community-contribution")
        {
            $candidate.author = "community-author"
            $candidate.title = "community-contribution text"
        }
    }

    $rawPath = Join-Path $tempRoot "presentation-raw-$([guid]::NewGuid().ToString('N')).json"
    try
    {
        Write-JsonFile -Value $raw -Path $rawPath
        return Invoke-Sanitizer -SourcePath $rawPath -Scope $Scope
    }
    finally
    {
        Remove-Item -LiteralPath $rawPath -Force -ErrorAction SilentlyContinue
    }
}

function Assert-CommunityMarkerPresentation
{
    param(
        [Parameter(Mandatory)][ValidateSet("blazor", "repository-wide")][string]$Scope,
        [Parameter(Mandatory)][ValidateSet("reviewNow", "verifyDiscussionBeforeReview", "needsRescue", "readyToMerge")][string]$ViewName,
        [Parameter(Mandatory)][bool]$ExpectedMarker,
        [Parameter(Mandatory)][object]$BlazorBaseline,
        [Parameter(Mandatory)][object]$RepositoryWideBaseline
    )

    $candidateNumber = switch ($ViewName)
    {
        "reviewNow" { 101 }
        "verifyDiscussionBeforeReview" { 103 }
        "needsRescue" { 104 }
        "readyToMerge" { 105 }
    }
    $reasonCode = if ($ExpectedMarker) { "community-contribution" } else { "community-contribution-extra" }
    $focusedArea = New-PresentationAreaFromRawFixture -Scope $Scope -CandidateNumber $candidateNumber -CommunityReasonCode $reasonCode
    $combined = if ($Scope -ceq "blazor")
    {
        New-CombinedPulse -Blazor $focusedArea -RepositoryWide $RepositoryWideBaseline
    }
    else
    {
        New-CombinedPulse -Blazor $BlazorBaseline -RepositoryWide $focusedArea
    }

    $body = Invoke-Renderer -Pulse $combined
    $label = if ($Scope -ceq "blazor") { "Blazor" } else { "Repository-wide" }
    $block = Get-PulseAreaBlock -Body $body -Label $label
    $reference = "[dotnet/aspnetcore#$candidateNumber](https://github.com/dotnet/aspnetcore/pull/$candidateNumber)"
    $rows = @($block -split "`r?`n" | Where-Object { $_.Contains($reference, [StringComparison]::Ordinal) })
    Assert-True ($rows.Count -eq 1) "The focused '$Scope/$ViewName' candidate must render exactly once."
    $row = $rows[0]
    $candidate = @($focusedArea.views.$ViewName | Where-Object { $_.number -eq $candidateNumber })
    Assert-True ($candidate.Count -eq 1) "The sanitized '$Scope/$ViewName' candidate is missing."
    $candidate = $candidate[0]
    $authorWithMarker = "$($candidate.author) **Community**"
    $expectedAuthorText = if ($ViewName -ceq "verifyDiscussionBeforeReview")
    {
        "**Author:** $authorWithMarker"
    }
    else
    {
        $authorWithMarker
    }
    Assert-True ($row.Contains("``$reasonCode``", [StringComparison]::Ordinal)) "The focused reason code must remain visible in Reasons."
    Assert-True ($row.Contains("$($candidate.ageDays)d open", [StringComparison]::Ordinal)) "The focused candidate must retain its exact open age."
    Assert-True (-not ($row -match "\b[0-9]+d idle\b")) "The focused candidate row must not claim inactivity."
    Assert-True ($block.Contains("| PR | Title | Author | Next actor | Open age | Reasons / Blockers |")) "Ordinary candidate tables must retain six columns with the Open age heading."
    if ($ExpectedMarker)
    {
        Assert-True ($row.Contains($expectedAuthorText, [StringComparison]::Ordinal)) "The exact community reason code must add the fixed author marker."
    }
    else
    {
        Assert-True (-not $row.Contains("**Community**", [StringComparison]::Ordinal)) "A partial reason code or author/title text must not add the community marker."
    }

    Invoke-PublicationValidator -Pulse $combined -AgentOutput (New-ValidAgentOutput -Body $body) -ExpectedBody $body
    $forgedRow = if ($ExpectedMarker)
    {
        $row.Replace(" **Community**", "")
    }
    elseif ($ViewName -ceq "verifyDiscussionBeforeReview")
    {
        $row.Replace("**Author:** $($candidate.author)", "**Author:** $($candidate.author) **Community**")
    }
    else
    {
        $row.Replace("| $($candidate.author) |", "| $($candidate.author) **Community** |")
    }
    Assert-True (-not [string]::Equals($row, $forgedRow, [StringComparison]::Ordinal)) "The forged marker case must change the focused row."
    $forgedBody = $body.Replace($row, $forgedRow)
    Assert-Throws {
        Invoke-PublicationValidator -Pulse $combined -AgentOutput (New-ValidAgentOutput -Body $forgedBody) -ExpectedBody $body
    } "The private validator must reject a forged community marker change."
}

function Get-PresentationSection
{
    param([string]$Body, [string]$Name)

    $match = [regex]::Match($Body, "(?ms)^## $([regex]::Escape($Name))`n(?<content>.*?)(?=^## |\z)")
    Assert-True $match.Success "The H2 section '$Name' is missing."

    return $match.Groups["content"].Value
}

function Assert-PresentationLayout
{
    param([object]$Pulse, [string]$Body)

    $headings = @([regex]::Matches($Body, "(?m)^#{1,6} .+$") | ForEach-Object Value)
    $expected = @("## Summary counts", "## Review now", "## Verify discussion before review", "## Needs rescue", "## Ready to merge", "## Coverage and data quality")
    Assert-True (($headings -join "`n") -ceq ($expected -join "`n")) "Expected exactly six ordered H2 sections; actual: $($headings -join ', ')."
    $disclaimer = "> [!IMPORTANT]`n> These views identify pull requests worth inspecting. They do not certify readiness, prove that feedback was addressed, authorize merge or review, or reliably establish completion."
    Assert-True ($Body.StartsWith($disclaimer, [StringComparison]::Ordinal)) "The exact disclaimer must be prominent."
    Assert-True ($Body.Contains("> Auto-generated by PR Attention Pulse. Manual edits are replaced on the next manual run.")) "The compact auto-generated header is missing."
    $label = if ($Pulse.scope -ceq "blazor") { "Blazor" } else { "Repository-wide" }
    $opening = "<details>"
    Assert-True ([regex]::Matches($Body, "(?m)^<details(?: open)?>$").Count -eq 1) "A single-area report must contain exactly one details block."
    Assert-True ([regex]::Matches($Body, "(?m)^</details>$").Count -eq 1) "A single-area report must close exactly one details block."
    $detailsStart = $Body.IndexOf($opening, [StringComparison]::Ordinal)
    $summaryStart = $Body.IndexOf("<summary><strong>$label</strong>", $detailsStart, [StringComparison]::Ordinal)
    $detailsEnd = $Body.IndexOf("</details>", $summaryStart, [StringComparison]::Ordinal)
    Assert-True ($detailsStart -ge 0 -and $summaryStart -gt $detailsStart -and $detailsEnd -gt $summaryStart) "The '$label' details structure is invalid."
    foreach ($heading in $expected)
    {
        $headingIndex = $Body.IndexOf($heading, [StringComparison]::Ordinal)
        Assert-True ($headingIndex -gt $summaryStart -and $headingIndex -lt $detailsEnd) "Section '$heading' must be inside the '$label' details block."
    }
    $outsideDetails = $Body.Remove($detailsStart, ($detailsEnd + "</details>".Length) - $detailsStart)
    Assert-True (-not ($outsideDetails -match "(?m)^## ")) "No report section may appear outside its area details block."
    $withoutAllowedLinks = [regex]::Replace(
        $Body,
        "\[dotnet/aspnetcore#[1-9][0-9]*\]\(https://github\.com/dotnet/aspnetcore/pull/[1-9][0-9]*\)",
        "")
    $withoutAreaMarkup = $withoutAllowedLinks.Replace($opening, "").Replace("</details>", "")
    $withoutAreaMarkup = [regex]::Replace($withoutAreaMarkup, "(?m)^<summary><strong>(?:Blazor|Repository-wide)</strong>[^`r`n]*</summary>$", "")
    Assert-True (-not ($withoutAreaMarkup -match "(?i)<(?:details|summary|strong|table|a|br)\b|\]\(|https?://|(?<![\w])@[A-Za-z0-9]")) "Presentation must not add unexpected HTML, mentions, or links."
    $header = $Body.Substring(0, $Body.IndexOf("## Summary counts", [StringComparison]::Ordinal))
    Assert-True (-not ($header -match "(?i)\.lock\.yml|pr-attention-pulse\.md|\b[0-9a-f]{40}\b")) "The header must not expose a workflow filename or commit hash."
}

function Assert-CombinedPresentationLayout
{
    param([Parameter(Mandatory)][object]$Pulse, [Parameter(Mandatory)][string]$Body)

    Assert-True ($Pulse.schemaVersion -ceq "2.0.0" -and @($Pulse.areas).Count -eq 2) "The combined presentation requires exactly two ordered areas."
    Assert-True ([regex]::Matches($Body, "(?m)^<details open>$").Count -eq 0) "No area may be expanded by default."
    Assert-True ([regex]::Matches($Body, "(?m)^<details>$").Count -eq 2) "Both areas must be collapsed by default."
    Assert-True ([regex]::Matches($Body, "(?m)^</details>$").Count -eq 2) "Both area blocks must be closed."
    Assert-True ($Body.Contains("This initial area composition includes the maintained **Blazor** view and a **Repository-wide** baseline.")) "The initial area composition note is missing."
    Assert-True ($Body.Contains("Additional product areas will be added only after maintainers define their exact label/path queries and decide whether this report shape is useful.")) "The future-area design note is missing."

    $expectedHeadings = @("## Summary counts", "## Review now", "## Verify discussion before review", "## Needs rescue", "## Ready to merge", "## Coverage and data quality")
    $cursor = 0
    foreach ($area in @($Pulse.areas))
    {
        $opening = if ($area.openByDefault) { "<details open>" } else { "<details>" }
        $start = $Body.IndexOf($opening, $cursor, [StringComparison]::Ordinal)
        $summaryEnd = $Body.IndexOf("</summary>", $start, [StringComparison]::Ordinal)
        $end = $Body.IndexOf("</details>", $summaryEnd, [StringComparison]::Ordinal)
        Assert-True ($start -ge $cursor -and $summaryEnd -gt $start -and $end -gt $summaryEnd) "The '$($area.id)' area block is missing or out of order."
        $block = $Body.Substring($start, ($end + "</details>".Length) - $start)
        Assert-True ($block.Contains("<summary><strong>$($area.label)</strong> -")) "The '$($area.id)' summary must include its trusted label and compact status."
        $headings = @([regex]::Matches($block, "(?m)^#{1,6} .+$") | ForEach-Object Value)
        Assert-True ([string]::Equals(($headings -join "`n"), ($expectedHeadings -join "`n"), [StringComparison]::Ordinal)) "The '$($area.id)' area must contain exactly the six ordered report sections."

        $expectedNumbers = if ($area.status -ceq "complete")
        {
            @(
                foreach ($viewName in @("reviewNow", "verifyDiscussionBeforeReview", "needsRescue", "readyToMerge"))
                {
                    foreach ($candidate in @($area.views.$viewName))
                    {
                        [string]$candidate.number
                    }
                }
            )
        }
        else
        {
            @()
        }
        $actualNumbers = @(
            [regex]::Matches($block, "\[dotnet/aspnetcore#(?<number>[1-9][0-9]*)\]\(https://github\.com/dotnet/aspnetcore/pull/\k<number>\)") |
                ForEach-Object { $_.Groups["number"].Value }
        )
        Assert-True ([string]::Equals(($actualNumbers -join ","), ($expectedNumbers -join ","), [StringComparison]::Ordinal)) "The '$($area.id)' links must preserve exact per-area membership and order."
        Assert-True (@($actualNumbers | Select-Object -Unique).Count -eq @($actualNumbers).Count) "The '$($area.id)' area must not repeat a pull request."
        $cursor = $end + "</details>".Length
    }

    $outsideBlocks = [regex]::Replace($Body, "(?ms)^<details(?: open)?>$.*?^</details>$", "")
    Assert-True (-not ($outsideBlocks -match "(?m)^## ")) "No report section may appear outside the two area blocks."
}

function Assert-PresentationTablesAndFields
{
    param([object]$Pulse, [string]$Body)

    Assert-PresentationLayout -Pulse $Pulse -Body $Body
    Import-Module -Scope Local -Force (Join-Path $supportRoot "PRAttentionPulseContract.psm1")
    $before = $Pulse | ConvertTo-Json -Depth 100 -Compress
    $directBody = ConvertTo-PRAttentionPulseBody -Pulse $Pulse
    Assert-True ([string]::Equals($before, ($Pulse | ConvertTo-Json -Depth 100 -Compress), [StringComparison]::Ordinal)) "Rendering must not mutate the supplied envelope."
    Assert-True ([string]::Equals($directBody, $Body, [StringComparison]::Ordinal)) "The file entry point and the single production renderer must agree."
    if ($Pulse.status -ceq "unavailable")
    {
        Assert-True (-not ($Body -match "(?m)^\|")) "Unavailable data must not manufacture a table."
        return
    }

    $source = $Pulse.source
    $summary = Get-PresentationSection -Body $Body -Name "Summary counts"
    $audit = Get-PresentationSection -Body $Body -Name "Coverage and data quality"
    $isoFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'"
    $generated = ([datetimeoffset]$source.generatedAt).ToUniversalTime().ToString($isoFormat, [Globalization.CultureInfo]::InvariantCulture)
    $attempted = ([datetimeoffset]$Pulse.attemptedAt).ToUniversalTime().ToString($isoFormat, [Globalization.CultureInfo]::InvariantCulture)
    Assert-True ($Body.Contains("> Source generated: ``$generated``; attempted: ``$attempted``.")) "Freshness must retain both exact supplied timestamps."
    Assert-True ($Body.Contains("> Source: ``$($source.repository)``. $($source.filter.description); coverage ``$($source.filter.coverage)``; selection $($source.filter.selection).")) "Every source scope field must survive."
    Assert-True ($summary.Contains("Open: $($source.census.openPullRequests). Matched: $($source.census.matched). Returned: $($source.query.returnedPullRequestCount) of $($source.query.openPullRequestCount); query complete: $($source.query.complete.ToString().ToLowerInvariant()).")) "Summary census and query values must remain distinct."
    foreach ($line in @(
        "- Query coverage: $($source.query.returnedPullRequestCount) of $($source.query.openPullRequestCount) open pull requests returned; complete $($source.query.complete.ToString().ToLowerInvariant()).",
        "- Discussion coverage: $($source.discussion.assessedCandidateCount) of limit $($source.discussion.candidateLimit) assessed; $($source.discussion.verificationNeededCount) need verification; $($source.discussion.unassessedReviewNowCount) Review now candidates unassessed.",
        "- Queue census: Review now $($source.census.byBucket.ReviewNow); Needs rescue $($source.census.byBucket.NeedsRescue); Ready to merge $($source.census.byBucket.ReadyToMerge); Waiting on author $($source.census.byBucket.WaitingOnAuthor); Waiting on CI $($source.census.byBucket.WaitingOnCI); Design decision $($source.census.byBucket.DesignDecision); Draft $($source.census.byBucket.Draft); Excluded $($source.census.byBucket.Excluded).",
        "- Scope census: $($source.census.labelOnly) label-only; $($source.census.pathOnly) path-only; $($source.census.labelAndPath) label-and-path; $($source.census.incidentalPathExcluded) incidental paths excluded; $($source.census.unresolvedMergeable) unresolved mergeability.",
        "- Overflow: Review now $($source.overflow.reviewNow); Needs rescue $($source.overflow.needsRescue); Ready to merge $($source.overflow.readyToMerge).",
        "- Caps: Review now $($source.caps.reviewNow); Review now per author $($source.caps.reviewNowPerAuthor); Needs rescue $($source.caps.needsRescue); Ready to merge $($source.caps.readyToMerge)."))
    {
        Assert-True ($audit.Contains($line)) "The visible audit lost a mapped value: $line"
    }
    $warnings = @($source.warnings)
    if ($warnings.Count -eq 0)
    {
        Assert-True ($audit.Contains("- Warnings: None.") -and -not $Body.Contains("> [!WARNING]")) "An empty warning array must remain explicitly empty."
    }
    else
    {
        $banner = "> [!WARNING]`n" + (($warnings | ForEach-Object { "> $_" }) -join "`n")
        Assert-True ($Body.IndexOf($banner, [StringComparison]::Ordinal) -ge 0 -and $Body.IndexOf($banner, [StringComparison]::Ordinal) -lt $Body.IndexOf("## Summary counts", [StringComparison]::Ordinal)) "Every ordered warning must appear near the top."
        $actualWarnings = @([regex]::Matches($audit, "(?m)^- Warning: (.+)$") | ForEach-Object { $_.Groups[1].Value })
        Assert-True (($actualWarnings -join "`n") -ceq ($warnings -join "`n")) "The full audit must preserve warning order and duplicates."
    }

    $expectedReferences = [Collections.Generic.List[string]]::new()
    foreach ($viewName in $presentationViews.Keys)
    {
        $name, $bucket = $presentationViews[$viewName]
        $discussion = $viewName -ceq "verifyDiscussionBeforeReview"
        $items = @($Pulse.views.$viewName)
        $total = if ($discussion)
        {
            $source.discussion.verificationNeededCount
        }
        else
        {
            $source.census.byBucket.$bucket
        }
        $section = Get-PresentationSection -Body $Body -Name $name
        $rows = @([regex]::Matches($section, '(?m)^\| [0-9]+: .+ \|$') | ForEach-Object Value)
        Assert-True ($rows.Count -eq $items.Count) "$name must have exactly one physical row per candidate."
        $tableLines = @([regex]::Matches($section, "(?m)^\|.*$") | ForEach-Object Value)
        $expectedLineCount = if ($items.Count -eq 0)
        {
            0
        }
        else
        {
            $items.Count + 2
        }
        Assert-True ($tableLines.Count -eq $expectedLineCount) "$name must have only one self-contained candidate table, with no evidence grid."
        if ($items.Count -gt 0)
        {
            $tableHeader = if ($discussion)
            {
                "| PR / Author | Title | Next actor / Age | Reasons / Blockers | Discussion / Comments | Threads |"
            }
            else
            {
                "| PR | Title | Author | Next actor | Open age | Reasons / Blockers |"
            }
            Assert-True ($tableLines[0] -ceq $tableHeader) "$name has the wrong six-column grouping."
            Assert-True ($tableLines[1] -ceq "| --- | --- | --- | --- | --- | --- |") "$name has an invalid table delimiter."
        }
        if ($discussion)
        {
            Assert-True ($summary.Contains("| $name | $($items.Count) | $total assessed candidates need verification |")) "Verification counts must not use the assessment budget as their denominator."
            Assert-True ($section.Contains("Displaying $($items.Count) of $total assessed candidates needing verification; $($total - $items.Count) are not displayed.")) "Verification display and undisplayed counts must remain explicit."
            Assert-True ($section.Contains("Assessment budget: $($source.discussion.candidateLimit) candidates, not a claim that $($source.discussion.candidateLimit) verification rows can be shown.")) "The assessment budget is not a display cap."
        }
        elseif ($viewName -ceq "reviewNow")
        {
            Assert-True ($summary.Contains("| $name | $($items.Count) | $total in the ReviewNow inventory bucket, not $total cleared for review |")) "ReviewNow inventory is not a cleared-for-review count."
            Assert-True ($section.Contains("Displaying $($items.Count) candidates from a ReviewNow inventory of $total. Legacy overflow: $($source.overflow.reviewNow).")) "Review now must retain displayed, inventory and legacy overflow counts."
            Assert-True ($section.Contains("The inventory includes discussion-verification and unassessed candidates; see coverage below.")) "ReviewNow inventory semantics must remain visible."
        }
        else
        {
            Assert-True ($summary.Contains("| $name | $($items.Count) | $total in the $bucket inventory bucket |")) "$name summary totals must remain visible."
            Assert-True ($section.Contains("Displaying $($items.Count) of $total inventory candidates. Legacy overflow: $($source.overflow.$viewName).")) "$name must retain displayed, inventory and legacy overflow counts."
        }

        for ($index = 0; $index -lt $items.Count; $index++)
        {
            $item = $items[$index]
            $row = $rows[$index]
            $cells = @($row.Split("|") | ForEach-Object Trim)
            Assert-True ($cells.Count -eq 8) "Candidate $($item.number) must occupy exactly six cells."
            $reference = "[dotnet/aspnetcore#$($item.number)](https://github.com/dotnet/aspnetcore/pull/$($item.number))"
            $expectedReferences.Add($reference)
            $identity = "$($item.rank): $reference"
            $author = [string]$item.author
            if (@($item.reasonCodes) -ccontains "community-contribution")
            {
                $author += " **Community**"
            }
            if ($discussion)
            {
                $identity += "; **Author:** $author"
            }
            Assert-True ($cells[1] -ceq $identity -and $item.bucket -ceq $bucket) "Candidate identity, original rank, author or classification changed."
            Assert-True ($cells[2] -ceq $item.title) "Candidate $($item.number) lost its full sanitized title."
            $openAge = "$($item.ageDays)d open"
            $reasons = if (@($item.reasonCodes).Count -eq 0)
            {
                "None."
            }
            else
            {
                ((@($item.reasonCodes) | ForEach-Object { "``$_``" }) -join ", ") + "."
            }
            $blockers = if (@($item.blockers).Count -eq 0)
            {
                "None."
            }
            else
            {
                @($item.blockers) -join "; "
            }
            $reasonCell = if ($discussion)
            {
                $cells[4]
            }
            else
            {
                $cells[6]
            }
            $withoutScope = ($reasonCell -split ' \*\*Scope match:\*\* ', 2)[0]
            Assert-True ($withoutScope -ceq "**Reasons:** $reasons **Blockers:** $blockers") "Every ordered reason and blocker must stay in candidate $($item.number)'s row."
            if ($discussion)
            {
                Assert-True ($cells[3] -ceq "**Next:** $($item.nextActor); $openAge") "Discussion actor and open age must remain in the same row."
                $assessment = $item.discussionAssessment
                $signals = if (@($assessment.signals).Count -eq 0)
                {
                    "None."
                }
                else
                {
                    ((@($assessment.signals) | ForEach-Object { "``$_``" }) -join ", ") + "."
                }
                $expectedDiscussion = "**State:** ``$($assessment.state)``; assessment complete: $($assessment.complete.ToString().ToLowerInvariant()). **Signals:** $signals **Comments:** $($assessment.commentTotalCount) total; evidence truncated: $($assessment.commentEvidenceTruncated.ToString().ToLowerInvariant())."
                Assert-True ($cells[5] -ceq $expectedDiscussion) "Every assessment, signal and comment field must stay in candidate $($item.number)'s row."
                $threads = $assessment.threads
                Assert-True ($cells[6] -ceq "$($threads.returnedCount) of $($threads.totalCount) returned; complete: $($threads.complete.ToString().ToLowerInvariant()); $($threads.unresolvedCount) unresolved; $($threads.outdatedUnresolvedCount) outdated unresolved") "Every thread count and completeness flag must stay in candidate $($item.number)'s row."
            }
            else
            {
                Assert-True ($cells[3] -ceq $author -and $cells[4] -ceq $item.nextActor -and $cells[5] -ceq $openAge) "Author, next actor, and open age must remain exact."
            }
        }
    }
    $references = @([regex]::Matches($Body, '\[dotnet/aspnetcore#[1-9][0-9]*\]\(https://github\.com/dotnet/aspnetcore/pull/[1-9][0-9]*\)') | ForEach-Object Value)
    Assert-True (($references -join ",") -ceq ($expectedReferences -join ",")) "Each deterministic upstream pull request link must occur once, in original view order."
}

function Assert-PresentationScopePlacement
{
    param([object]$Pulse, [string]$Body)

    $items = @($presentationViews.Keys | ForEach-Object { $Pulse.views.$_ })
    $scopes = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($item in $items)
    {
        $null = $scopes.Add($item.scopeMatch)
    }
    $common = @([regex]::Matches($Body, "(?m)^> Scope match for all displayed candidates: (.+)\.$"))
    if ($items.Count -gt 0 -and $scopes.Count -eq 1)
    {
        Assert-True ($common.Count -eq 1 -and $common[0].Groups[1].Value -ceq $items[0].scopeMatch) "A nonempty ordinally uniform scope must appear once in the header, not use query coverage."
        Assert-True (-not $Body.Contains("**Scope match:**")) "Uniform scope must not be repeated in candidate rows."
    }
    else
    {
        Assert-True ($common.Count -eq 0) "Mixed or empty candidate sets must not claim a common scope."
        Assert-True ([regex]::Matches($Body, "\*\*Scope match:\*\*").Count -eq $items.Count) "Every mixed-scope candidate must retain its scope in its own row."
        foreach ($item in $items)
        {
            $link = "[dotnet/aspnetcore#$($item.number)](https://github.com/dotnet/aspnetcore/pull/$($item.number))"
            $row = @($Body.Split("`n") | Where-Object { $_.Contains($link) })
            Assert-True ($row.Count -eq 1 -and $row[0].Contains("**Scope match:** $($item.scopeMatch).")) "Candidate $($item.number) lost its actual scope."
        }
    }
}

$testRoot = $PSScriptRoot
$workflowRoot = Split-Path -Parent $testRoot
$supportRoot = Join-Path $workflowRoot "pr-attention-pulse"
$fixtureRoot = Join-Path $testRoot "fixtures"
$presentationRoot = Join-Path $fixtureRoot "presentation"
$presentationViews = [ordered]@{
    reviewNow = @("Review now", "ReviewNow")
    verifyDiscussionBeforeReview = @("Verify discussion before review", "ReviewNow")
    needsRescue = @("Needs rescue", "NeedsRescue")
    readyToMerge = @("Ready to merge", "ReadyToMerge")
}
$queueRoot = Join-Path (Split-Path -Parent $workflowRoot) "skills/pr-attention-queue"
$queueFixtureRoot = Join-Path $queueRoot "tests/fixtures"
$queueScript = Join-Path $queueRoot "scripts/Get-PRAttentionQueue.ps1"
$sanitizerPath = Join-Path $supportRoot "Sanitize-PRAttentionPulse.ps1"
$combinerPath = Join-Path $supportRoot "Combine-PRAttentionPulse.ps1"
$rendererPath = Join-Path $supportRoot "Render-PRAttentionPulse.ps1"
$validatorPath = Join-Path $supportRoot "Validate-PRAttentionPulseOutput.ps1"
$compilePath = Join-Path $supportRoot "Compile-PRAttentionPulse.ps1"
$workflowPath = Join-Path $workflowRoot "pr-attention-pulse.md"
$lockPath = Join-Path $workflowRoot "pr-attention-pulse.lock.yml"
$ghAwVersion = (& gh aw --version 2>&1) -join "`n"
Assert-True ($LASTEXITCODE -eq 0 -and $ghAwVersion.Contains("v0.88.7")) "Focused tests require the reviewed gh-aw v0.88.7 installation."
& pwsh -NoProfile -File (Join-Path $testRoot "Test-PulseReviewRequirements.ps1")
Assert-True ($LASTEXITCODE -eq 0) "The effective generated security and presentation controls must pass."
$collectorJsRoot = Join-Path (Get-GhAwExtensionRoot) "actions/setup/js"
$collectorSanitizerPath = Join-Path $collectorJsRoot "sanitize_content.cjs"
$attemptTimestamp = [datetime]"2026-09-10T20:04:56Z"
$queueSnapshot = [datetime]"2026-09-03T18:00:00Z"
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) "pr-attention-pulse-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force $tempRoot | Out-Null

try
{
    & node (Join-Path $testRoot "Test-PulsePublicationCommand.cjs") $collectorJsRoot $workflowPath $tempRoot
    Assert-True ($LASTEXITCODE -eq 0) "The pinned CLI publication serialization must preserve the canonical body."

    $normal = Invoke-Sanitizer -FixtureName "normal-legacy.json"
    Assert-True ($normal.schemaVersion -eq "1.0.0") "The pulse envelope must be versioned."
    Assert-True ($normal.status -eq "complete") "A complete inventory must remain complete."
    Assert-True ($normal.candidateCountsAvailable -is [bool] -and $normal.candidateCountsAvailable) "Complete candidate counts must be available."
    Assert-True (@($normal.views.reviewNow).Count -eq 2) "Both Review now candidates must survive."
    Assert-True ($normal.views.reviewNow[0].number -eq 101) "Review now ordering must preserve digest rank."
    Assert-True ($normal.views.reviewNow[1].number -eq 102) "Review now ordering must preserve the second digest rank."
    Assert-True ($normal.views.reviewNow[1].rank -eq 2) "The engine-provided digest rank must be preserved."
    Assert-True (@($normal.views.verifyDiscussionBeforeReview).Count -eq 1) "Discussion verification must remain a separate legacy view."
    Assert-True (-not $normal.views.verifyDiscussionBeforeReview[0].discussionAssessment.complete) "Partial discussion evidence must remain incomplete."
    Assert-True ($normal.views.verifyDiscussionBeforeReview[0].discussionAssessment.signals -contains "discussion-incomplete") "Discussion completeness signals must be preserved."
    Assert-True ($normal.views.verifyDiscussionBeforeReview[0].discussionAssessment.threads.unresolvedCount -eq 2) "Thread assessment counts must be preserved."
    Assert-True ($normal.views.needsRescue[0].bucket -eq "NeedsRescue") "Needs rescue membership must not be reclassified."
    Assert-True ($normal.views.readyToMerge[0].bucket -eq "ReadyToMerge") "Ready to merge membership must not be reclassified."
    Assert-True ($normal.views.reviewNow[1].author -eq "second-author") "Authors must render without mentions."
    foreach ($viewName in @("reviewNow", "needsRescue", "readyToMerge"))
    {
        foreach ($candidate in @($normal.views.$viewName))
        {
            Assert-True ($null -eq $candidate.PSObject.Properties["discussionAssessment"]) "Discussion assessment data must cross only for the discussion verification view."
        }
    }

    $repositoryWideNormalPath = Join-Path $tempRoot "repository-wide-normal.json"
    $repositoryWideNormalRaw = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $repositoryWideNormalRaw.filter.name = "adhoc"
    $repositoryWideNormalRaw.filter.description = "Ad hoc pull-request scope"
    $repositoryWideNormalRaw.filter.coverage = "all-repo"
    $repositoryWideNormalRaw.filter.selection = "(all open pull requests)"
    $repositoryWideNormalRaw.filter.allRepositoryPullRequests = $true
    foreach ($candidate in $repositoryWideNormalRaw.items)
    {
        $candidate.scopeMatch = "all-repo"
    }
    Write-JsonFile -Value $repositoryWideNormalRaw -Path $repositoryWideNormalPath
    $repositoryWideNormal = Invoke-Sanitizer -SourcePath $repositoryWideNormalPath -Scope repository-wide
    Assert-True ($repositoryWideNormal.status -ceq "complete") "The repository-wide baseline fixture must satisfy its independent sanitizer contract."
    $combinedNormal = New-CombinedPulse -Blazor $normal -RepositoryWide $repositoryWideNormal
    Assert-True ($combinedNormal.schemaVersion -ceq "2.0.0" -and $combinedNormal.status -ceq "complete") "Two complete areas must produce a complete versioned combined envelope."
    Assert-True ($combinedNormal.areas[0].id -ceq "blazor" -and -not $combinedNormal.areas[0].openByDefault) "Blazor must be the first, collapsed area."
    Assert-True ($combinedNormal.areas[1].id -ceq "repository-wide" -and -not $combinedNormal.areas[1].openByDefault) "Repository-wide must be the second, collapsed baseline."
    $combinedNormalBody = Invoke-Renderer -Pulse $combinedNormal
    Assert-CombinedPresentationLayout -Pulse $combinedNormal -Body $combinedNormalBody
    Assert-True ($combinedNormalBody.Length -le 65000) "The combined two-area report must remain within the publication body limit."
    Assert-True ([regex]::Matches($combinedNormalBody, "\[dotnet/aspnetcore#101\]\(https://github\.com/dotnet/aspnetcore/pull/101\)").Count -eq 2) "The same pull request may appear once in each independently generated area."
    $combinedPostIngestionBody = Invoke-PinnedOutputSanitizer -Content $combinedNormalBody
    Assert-True ([string]::Equals($combinedPostIngestionBody, $combinedNormalBody, [StringComparison]::Ordinal)) "Pinned gh-aw output sanitization must preserve the complete combined report byte-for-byte."
    $combinedOutput = Invoke-PinnedCollector -Body $combinedPostIngestionBody
    Assert-True ($combinedOutput.errors -is [array] -and $combinedOutput.errors.Count -eq 0) "The pinned collector must accept the combined report without errors."
    Assert-True (@($combinedOutput.items).Count -eq 1) "The pinned collector must emit exactly one update item for the combined report."
    Invoke-PublicationValidator -Pulse $combinedNormal -AgentOutput $combinedOutput -ExpectedBody $combinedPostIngestionBody

    $reorderedCombined = $combinedNormal | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
    $reorderedCombined.areas = @($reorderedCombined.areas[1], $reorderedCombined.areas[0])
    Assert-Throws { Invoke-Renderer -Pulse $reorderedCombined } "The renderer must reject reordered combined areas." "The combined Pulse area order or metadata is invalid."
    $mislabeledCombined = $combinedNormal | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
    $mislabeledCombined.areas[0].label = "Repository-wide"
    Assert-Throws { Invoke-Renderer -Pulse $mislabeledCombined } "The renderer must reject mismatched combined-area metadata." "The combined Pulse area order or metadata is invalid."
    $wrongCombinedStatus = $combinedNormal | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
    $wrongCombinedStatus.status = "partial"
    Assert-Throws { Invoke-Renderer -Pulse $wrongCombinedStatus } "The renderer must reject a combined status inconsistent with its area results." "The combined Pulse status does not match its area results."

    $normalJson = $normal | ConvertTo-Json -Depth 100
    foreach ($prohibitedText in @(
        "RAW BODY",
        "RAW REVIEW",
        "RAW COMMENT",
        "RAW EVIDENCE",
        "RAW DISCUSSION",
        "src/Secret.cs",
        "https://",
        "@",
        '`',
        "|",
        "<b>",
        "#88",
        "example.com",
        "GH-77",
        "deadbee5"))
    {
        Assert-True (-not $normalJson.Contains($prohibitedText)) "Sanitized model input leaked prohibited text: $prohibitedText"
    }
    foreach ($prohibitedProperty in @('"body"', '"reviews"', '"comments"', '"files"', '"url"', '"excerpt"'))
    {
        Assert-True (-not $normalJson.Contains($prohibitedProperty)) "Sanitized model input leaked prohibited property: $prohibitedProperty"
    }

    $zero = Invoke-Sanitizer -FixtureName "complete-zero.json"
    Assert-True ($zero.status -eq "complete") "A complete zero inventory must be a normal successful result."
    Assert-True ($zero.source.census.openPullRequests -eq 0) "A complete zero inventory must preserve zero census counts."
    foreach ($viewName in @("reviewNow", "verifyDiscussionBeforeReview", "needsRescue", "readyToMerge"))
    {
        Assert-True (@($zero.views.$viewName).Count -eq 0) "A complete zero inventory must keep '$viewName' empty."
    }
    $zeroBody = Invoke-Renderer -Pulse $zero
    Assert-True ([regex]::Matches($zeroBody, "None in this complete inventory\.").Count -eq 4) "A complete zero inventory must render all four candidate sections as empty."

    $collectionFailure = Invoke-Sanitizer -CollectionExitCode 9
    Assert-True ($collectionFailure.status -eq "unavailable") "Collection failure must produce a failure envelope."
    Assert-True ($collectionFailure.errorCategory -eq "collection-failed") "Collection failure must preserve its bounded category."
    Assert-True (-not $collectionFailure.candidateCountsAvailable) "Failure candidate counts must be unavailable, not zero."
    Assert-True ($null -eq $collectionFailure.source.census) "A failure envelope must not manufacture zero census counts."
    $missingOutput = Invoke-Sanitizer
    Assert-True ($missingOutput.errorCategory -eq "collection-output-missing") "Missing collection output must produce a deterministic failure envelope."

    $malformed = Invoke-Sanitizer -FixtureName "malformed.json"
    Assert-True ($malformed.errorCategory -eq "malformed-json") "Malformed JSON must produce a deterministic failure envelope."
    $incompatible = Invoke-Sanitizer -FixtureName "incompatible.json"
    Assert-True ($incompatible.errorCategory -eq "incompatible-schema") "An incompatible schema must fail closed."
    $incomplete = Invoke-Sanitizer -FixtureName "incomplete.json"
    Assert-True ($incomplete.errorCategory -eq "incomplete-query") "A partial query must fail closed."
    $wrongRepository = Invoke-Sanitizer -FixtureName "wrong-repository.json"
    Assert-True ($wrongRepository.errorCategory -eq "unexpected-repository") "The sanitizer must reject the wrong source repository."

    $repositoryCasePath = Join-Path $tempRoot "repository-case.json"
    $repositoryCase = Get-Content -Raw (Join-Path $fixtureRoot "complete-zero.json") | ConvertFrom-Json -Depth 100
    $repositoryCase.repository = "DOTNET/ASPNETCORE"
    Write-JsonFile -Value $repositoryCase -Path $repositoryCasePath
    $repositoryCaseResult = Invoke-Sanitizer -SourcePath $repositoryCasePath
    Assert-True ($repositoryCaseResult.errorCategory -eq "unexpected-repository") "Repository identity must use an ordinal comparison."

    foreach ($scopeMutation in @(
        @{ Name = "wrong-filter-name"; Apply = { param($value) $value.filter.name = "all-repo" } },
        @{ Name = "wrong-filter-description"; Apply = { param($value) $value.filter.description = "Every open pull request in the repository" } },
        @{ Name = "wrong-filter-coverage"; Apply = { param($value) $value.filter.coverage = "all-repo" } },
        @{ Name = "wrong-filter-selection"; Apply = { param($value) $value.filter.selection = "(all open pull requests)" } },
        @{ Name = "wrong-filter-all-repository"; Apply = { param($value) $value.filter.allRepositoryPullRequests = $true } },
        @{ Name = "wrong-scope-match"; Apply = { param($value) $value.items[0].scopeMatch = "all-repo" } }))
    {
        $scopePath = Join-Path $tempRoot "$($scopeMutation.Name).json"
        $scopeInput = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
        & $scopeMutation.Apply $scopeInput
        Write-JsonFile -Value $scopeInput -Path $scopePath
        $scopeResult = Invoke-Sanitizer -SourcePath $scopePath -Scope blazor
        Assert-True ($scopeResult.errorCategory -eq "invalid-contract") "The Blazor sanitizer must reject $($scopeMutation.Name)."
    }

    $strictBooleanPath = Join-Path $tempRoot "strict-boolean.json"
    $strictBoolean = Get-Content -Raw (Join-Path $fixtureRoot "complete-zero.json") | ConvertFrom-Json -Depth 100
    $strictBoolean.query.complete = "false"
    Write-JsonFile -Value $strictBoolean -Path $strictBooleanPath
    $strictBooleanResult = Invoke-Sanitizer -SourcePath $strictBooleanPath
    Assert-True ($strictBooleanResult.errorCategory -eq "invalid-contract") "String false must not be treated as boolean true."

    $missingFieldPath = Join-Path $tempRoot "missing-field.json"
    $missingField = Get-Content -Raw (Join-Path $fixtureRoot "complete-zero.json") | ConvertFrom-Json -Depth 100
    $missingField.census.PSObject.Properties.Remove("matched")
    Write-JsonFile -Value $missingField -Path $missingFieldPath
    $missingFieldResult = Invoke-Sanitizer -SourcePath $missingFieldPath
    Assert-True ($missingFieldResult.errorCategory -eq "invalid-contract") "Missing counts must not silently become zero."

    $mismatchedCensusPath = Join-Path $tempRoot "mismatched-census.json"
    $mismatchedCensus = Get-Content -Raw (Join-Path $fixtureRoot "complete-zero.json") | ConvertFrom-Json -Depth 100
    $mismatchedCensus.census.openPullRequests = 1
    Write-JsonFile -Value $mismatchedCensus -Path $mismatchedCensusPath
    $mismatchedCensusResult = Invoke-Sanitizer -SourcePath $mismatchedCensusPath
    Assert-True ($mismatchedCensusResult.errorCategory -eq "invalid-contract") "Query and census inventory counts must agree."

    $duplicateNumberPath = Join-Path $tempRoot "duplicate-number.json"
    $duplicateNumber = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $duplicateNumber.items[1].number = $duplicateNumber.items[0].number
    Write-JsonFile -Value $duplicateNumber -Path $duplicateNumberPath
    $duplicateNumberResult = Invoke-Sanitizer -SourcePath $duplicateNumberPath
    Assert-True ($duplicateNumberResult.errorCategory -eq "invalid-contract") "Duplicate pull request numbers must fail closed."

    $duplicateRankPath = Join-Path $tempRoot "duplicate-rank.json"
    $duplicateRank = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $duplicateRank.items[1].digestRank = $duplicateRank.items[0].digestRank
    Write-JsonFile -Value $duplicateRank -Path $duplicateRankPath
    $duplicateRankResult = Invoke-Sanitizer -SourcePath $duplicateRankPath
    Assert-True ($duplicateRankResult.errorCategory -eq "invalid-contract") "Duplicate view ranks must fail closed."

    $bucketCasePath = Join-Path $tempRoot "bucket-case.json"
    $bucketCase = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $bucketCase.items[0].bucket = $bucketCase.items[0].bucket.ToLowerInvariant()
    Write-JsonFile -Value $bucketCase -Path $bucketCasePath
    $bucketCaseResult = Invoke-Sanitizer -SourcePath $bucketCasePath
    Assert-True ($bucketCaseResult.errorCategory -eq "invalid-contract") "Legacy bucket names must use ordinal comparisons."

    $reasonCasePath = Join-Path $tempRoot "reason-case.json"
    $reasonCase = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $reasonCase.items[0].reasonCodes[0] = $reasonCase.items[0].reasonCodes[0].ToUpperInvariant()
    Write-JsonFile -Value $reasonCase -Path $reasonCasePath
    $reasonCaseResult = Invoke-Sanitizer -SourcePath $reasonCasePath
    Assert-True ($reasonCaseResult.errorCategory -eq "invalid-contract") "Stable reason codes must remain lowercase exact contract values."

    $discussionCountPath = Join-Path $tempRoot "discussion-count.json"
    $discussionCount = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $discussionCount.discussion.verificationNeededCount = 2
    Write-JsonFile -Value $discussionCount -Path $discussionCountPath
    $discussionCountResult = Invoke-Sanitizer -SourcePath $discussionCountPath
    Assert-True ($discussionCountResult.errorCategory -eq "invalid-contract") "Discussion summary counts must match assessment states."

    $boundedDiscussionPath = Join-Path $tempRoot "bounded-discussion.json"
    $boundedDiscussion = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $boundedDiscussion.items[2].shownInDiscussionVerification = $false
    $boundedDiscussion.items[2].discussionVerificationRank = $null
    Write-JsonFile -Value $boundedDiscussion -Path $boundedDiscussionPath
    $boundedDiscussionResult = Invoke-Sanitizer -SourcePath $boundedDiscussionPath
    Assert-True ($boundedDiscussionResult.status -eq "complete") "A bounded discussion view may show fewer candidates than the full verification-needed count."
    Assert-True (@($boundedDiscussionResult.views.verifyDiscussionBeforeReview).Count -eq 0) "The sanitizer must preserve bounded discussion-view membership without reclassification."
    Assert-True ($boundedDiscussionResult.source.discussion.verificationNeededCount -eq 1) "The full verification-needed count must remain distinct from shown membership."

    $unassessedDiscussionPath = Join-Path $tempRoot "unassessed-discussion.json"
    $unassessedDiscussion = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $unassessedDiscussion.discussion.candidateLimit = 2
    $unassessedDiscussion.discussion.assessedCandidateCount = 2
    $unassessedDiscussion.discussion.verificationNeededCount = 0
    $unassessedDiscussion.discussion.unassessedReviewNowCount = 1
    $unassessedDiscussion.items[2].shownInDiscussionVerification = $false
    $unassessedDiscussion.items[2].discussionVerificationRank = $null
    $unassessedDiscussion.items[2].discussionAssessment.state = "not-assessed"
    $unassessedDiscussion.items[2].discussionAssessment.complete = $false
    $unassessedDiscussion.items[2].discussionAssessment.signals = @("discussion-not-assessed")
    $unassessedDiscussion.items[2].discussionAssessment.commentTotalCount = 0
    $unassessedDiscussion.items[2].discussionAssessment.commentEvidenceTruncated = $false
    $unassessedDiscussion.items[2].discussionAssessment.threads.totalCount = 0
    $unassessedDiscussion.items[2].discussionAssessment.threads.returnedCount = 0
    $unassessedDiscussion.items[2].discussionAssessment.threads.complete = $false
    $unassessedDiscussion.items[2].discussionAssessment.threads.unresolvedCount = 0
    $unassessedDiscussion.items[2].discussionAssessment.threads.outdatedUnresolvedCount = 0
    Write-JsonFile -Value $unassessedDiscussion -Path $unassessedDiscussionPath
    $unassessedDiscussionResult = Invoke-Sanitizer -SourcePath $unassessedDiscussionPath
    Assert-True ($unassessedDiscussionResult.status -eq "complete") "A bounded legacy queue with unassessed Review now candidates must remain valid."
    Assert-True ($unassessedDiscussionResult.source.discussion.unassessedReviewNowCount -eq 1) "The unassessed Review now count must be preserved."

    $unexpectedDiscussionPath = Join-Path $tempRoot "unexpected-discussion.json"
    $unexpectedDiscussion = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $unexpectedDiscussion.items[3].discussionAssessment = $unexpectedDiscussion.items[0].discussionAssessment
    Write-JsonFile -Value $unexpectedDiscussion -Path $unexpectedDiscussionPath
    $unexpectedDiscussionResult = Invoke-Sanitizer -SourcePath $unexpectedDiscussionPath
    Assert-True ($unexpectedDiscussionResult.errorCategory -eq "invalid-contract") "Non-ReviewNow items must not carry unnecessary discussion data."

    $invalidAuthorPath = Join-Path $tempRoot "invalid-author.json"
    $invalidAuthor = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $invalidAuthor.items[0].author = "invalid author"
    Write-JsonFile -Value $invalidAuthor -Path $invalidAuthorPath
    $invalidAuthorResult = Invoke-Sanitizer -SourcePath $invalidAuthorPath
    Assert-True ($invalidAuthorResult.errorCategory -eq "invalid-contract") "Invalid author identifiers must fail closed."

    $oversizedPath = Join-Path $tempRoot "oversized.json"
    $oversized = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
    $oversized.warnings = @(1..20 | ForEach-Object { "warning-$_ " + ("x" * 500) })
    Write-JsonFile -Value $oversized -Path $oversizedPath
    $oversizedResult = Invoke-Sanitizer -SourcePath $oversizedPath -MaxOutputBytes 4096
    Assert-True ($oversizedResult.errorCategory -eq "sanitized-output-too-large") "Oversized sanitized output must become a bounded failure envelope."

    $realFixtureExpectations = [ordered]@{
        "blazor/pull-requests.json" = @(3, 0, 3, 1)
        "blazor/correctness-pull-requests.json" = @(0, 1, 2, 1)
        "blazor/discussion-pull-requests.json" = @(0, 0, 0, 0)
        "repository-wide/pull-requests.json" = @(5, 0, 3, 1)
        "repository-wide/correctness-pull-requests.json" = @(4, 1, 2, 1)
        "repository-wide/discussion-pull-requests.json" = @(2, 5, 0, 0)
    }
    $realPulses = [ordered]@{}
    foreach ($fixtureKey in $realFixtureExpectations.Keys)
    {
        $scope, $fixtureName = $fixtureKey.Split("/", 2)
        $realPulse = Invoke-RealQueueFixture -FixtureName $fixtureName -Scope $scope
        Assert-True ($realPulse.status -eq "complete") "The sanitizer must accept real $scope output from $fixtureName."
        Assert-True ($realPulse.source.query.returnedPullRequestCount -eq $realPulse.source.query.openPullRequestCount) "The real $scope query must be independently complete."
        Assert-True ($realPulse.source.census.matched -le $realPulse.source.query.returnedPullRequestCount) "The real $scope matched inventory cannot exceed the complete query."
        $expectedCounts = $realFixtureExpectations[$fixtureKey]
        Assert-True (@($realPulse.views.reviewNow).Count -eq $expectedCounts[0]) "The real $fixtureKey Review now membership changed."
        Assert-True (@($realPulse.views.verifyDiscussionBeforeReview).Count -eq $expectedCounts[1]) "The real $fixtureKey discussion verification membership changed."
        Assert-True (@($realPulse.views.needsRescue).Count -eq $expectedCounts[2]) "The real $fixtureKey Needs rescue membership changed."
        Assert-True (@($realPulse.views.readyToMerge).Count -eq $expectedCounts[3]) "The real $fixtureKey Ready to merge membership changed."
        $realBody = Invoke-Renderer -Pulse $realPulse
        Invoke-PublicationValidator -Pulse $realPulse -AgentOutput (New-ValidAgentOutput -Body $realBody) -ExpectedBody $realBody
        $realPulses[$fixtureKey] = $realPulse
    }

    $normalBody = Invoke-Renderer -Pulse $normal
    Assert-True ([string]::Equals($normalBody, $normalBody.TrimEnd(), [StringComparison]::Ordinal)) "Canonical rendering must not contain collector-trimmed trailing whitespace."
    $postIngestionBody = Invoke-PinnedOutputSanitizer -Content $normalBody
    Assert-True ([string]::Equals($postIngestionBody, $normalBody, [StringComparison]::Ordinal)) "Pinned gh-aw output sanitization must preserve the complete canonical report byte-for-byte."
    Assert-True ([string]::Equals((Invoke-PinnedOutputSanitizer -Content $postIngestionBody), $postIngestionBody, [StringComparison]::Ordinal)) "Pinned output sanitization must be idempotent for the canonical report."
    Assert-True ($normalBody.Contains('[dotnet/aspnetcore#101](https://github.com/dotnet/aspnetcore/pull/101)')) "Rendered identifiers must be deterministic upstream pull request links."
    Assert-True ($normalBody.Contains("2026-09-10T20:04:56.0000000Z")) "Rendered timestamps must be deterministic invariant ISO values."
    Assert-True ($normalBody.Contains("Queue census: Review now 3; Needs rescue 1; Ready to merge 1")) "Rendered output must preserve the engine-provided bucket census."
    Assert-True (-not ($normalBody -match "(?<![\w])@[A-Za-z0-9]")) "Rendered output must not mention authors."
    $normalBodyWithoutAllowedReferences = [regex]::Replace($normalBody, "\[dotnet/aspnetcore#[1-9][0-9]*\]\(https://github\.com/dotnet/aspnetcore/pull/[1-9][0-9]*\)", "")
    Assert-True (-not ($normalBodyWithoutAllowedReferences -match "(?i)\bhttps?://|\]\(")) "Rendered output must contain only deterministic upstream pull request links."
    Assert-True (-not ($normalBodyWithoutAllowedReferences -match "(?<!#)#[0-9]+")) "Rendered output must not contain bare issue references."

    $failureBody = Invoke-Renderer -Pulse $collectionFailure
    Assert-True ($failureBody.Contains("> [!WARNING]`n> Blazor attention data unavailable")) "Failure rendering must prominently report the unavailable area."
    Assert-True ($failureBody.Contains("Candidate counts unavailable.")) "Failure rendering must mark counts unavailable."
    Assert-True (-not $failureBody.Contains("Open pull requests: 0")) "Failure rendering must not present unavailable counts as zero."
    $repositoryWideFailure = Invoke-Sanitizer -CollectionExitCode 8 -Scope repository-wide
    $blazorFailureCombined = New-CombinedPulse -Blazor $collectionFailure -RepositoryWide $repositoryWideNormal
    Assert-True ($blazorFailureCombined.status -ceq "partial") "A Blazor-only collection failure must preserve the complete repository-wide area."
    $blazorFailureBody = Invoke-Renderer -Pulse $blazorFailureCombined
    Assert-CombinedPresentationLayout -Pulse $blazorFailureCombined -Body $blazorFailureBody
    Assert-True ($blazorFailureBody.Contains("> Blazor attention data unavailable") -and
        $blazorFailureBody.Contains("<summary><strong>Repository-wide</strong> - 5 matched;")) "A Blazor failure must not erase or zero the repository-wide baseline."
    $repositoryWideFailureCombined = New-CombinedPulse -Blazor $normal -RepositoryWide $repositoryWideFailure
    Assert-True ($repositoryWideFailureCombined.status -ceq "partial") "A repository-wide collection failure must preserve the complete Blazor area."
    $repositoryWideFailureBody = Invoke-Renderer -Pulse $repositoryWideFailureCombined
    Assert-CombinedPresentationLayout -Pulse $repositoryWideFailureCombined -Body $repositoryWideFailureBody
    Assert-True ($repositoryWideFailureBody.Contains("> Repository-wide attention data unavailable") -and
        $repositoryWideFailureBody.Contains("<summary><strong>Blazor</strong> - 5 matched;")) "A repository-wide failure must not erase or zero the Blazor area."

    $validOutput = Invoke-PinnedCollector -Body $postIngestionBody
    Assert-True ($validOutput.errors -is [array] -and $validOutput.errors.Count -eq 0) "The pinned collector must accept the canonical report without errors."
    Assert-True (@($validOutput.items).Count -eq 1) "The pinned collector must emit exactly one update item."
    Assert-True ([string]::Equals($validOutput.items[0].body, $postIngestionBody, [StringComparison]::Ordinal)) "The pinned collector must preserve the trusted-normalized body byte-for-byte."
    Invoke-PublicationValidator -Pulse $normal -AgentOutput $validOutput -ExpectedBody $postIngestionBody
    $publicationBodyPath = Join-Path $tempRoot "publication-body.md"
    [IO.File]::WriteAllText($publicationBodyPath, $validOutput.items[0].body, [Text.UTF8Encoding]::new($false))
    & node (Join-Path $testRoot "Test-PulseIssueUpdate.cjs") $collectorJsRoot $lockPath $publicationBodyPath
    Assert-True ($LASTEXITCODE -eq 0) "The pinned issue handler must publish the validated body without mutation."
    Invoke-PublicationValidator -Pulse $zero -AgentOutput (New-ValidAgentOutput -Body $zeroBody) -ExpectedBody $zeroBody

    $failureEnvelopes = [ordered]@{
        "collection-failed" = $collectionFailure
        "collection-output-missing" = $missingOutput
        "malformed-json" = $malformed
        "incompatible-schema" = $incompatible
        "incomplete-query" = $incomplete
        "unexpected-repository" = $wrongRepository
        "invalid-contract" = $strictBooleanResult
        "sanitized-output-too-large" = $oversizedResult
    }
    foreach ($expectedCategory in $failureEnvelopes.Keys)
    {
        $failureEnvelope = $failureEnvelopes[$expectedCategory]
        Assert-True ($failureEnvelope.errorCategory -eq $expectedCategory) "The '$expectedCategory' failure fixture produced the wrong category."
        $canonicalFailureBody = Invoke-Renderer -Pulse $failureEnvelope
        Invoke-PublicationValidator `
            -Pulse $failureEnvelope `
            -AgentOutput (New-ValidAgentOutput -Body $canonicalFailureBody) `
            -ExpectedBody $canonicalFailureBody
    }

    foreach ($legitimateTitle in @(
        "Fix placeholder text in InputText",
        "Preserve TODO text in diagnostics",
        "Avoid test body regression",
        "Handle report_incomplete status text"))
    {
        $legitimateTitlePath = Join-Path $tempRoot "legitimate-title-$([guid]::NewGuid().ToString('N')).json"
        $legitimateTitleInput = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
        $legitimateTitleInput.items[0].title = $legitimateTitle
        Write-JsonFile -Value $legitimateTitleInput -Path $legitimateTitlePath
        $legitimateTitlePulse = Invoke-Sanitizer -SourcePath $legitimateTitlePath
        Assert-True ($legitimateTitlePulse.status -eq "complete") "A legitimate pull request title containing '$legitimateTitle' must remain publishable."
        $legitimateTitleBody = Invoke-PinnedOutputSanitizer -Content (Invoke-Renderer -Pulse $legitimateTitlePulse)
        Invoke-PublicationValidator `
            -Pulse $legitimateTitlePulse `
            -AgentOutput (New-ValidAgentOutput -Body $legitimateTitleBody) `
            -ExpectedBody $legitimateTitleBody
    }

    $safeCellPulses = [ordered]@{ "normal-legacy" = $normal }
    $unicodeIndex = 0
    foreach ($unicodeTitle in @(
        "Handle soft$([char]0x00AD)hyphen input",
        "Support woman$([char]0x200D)technologist input",
        "Normalize Greek $([char]0x0391) input"))
    {
        $unicodeTitlePath = Join-Path $tempRoot "unicode-title-$([guid]::NewGuid().ToString('N')).json"
        $unicodeTitleInput = Get-Content -Raw (Join-Path $fixtureRoot "normal-legacy.json") | ConvertFrom-Json -Depth 100
        $unicodeTitleInput.items[0].title = $unicodeTitle
        Write-JsonFile -Value $unicodeTitleInput -Path $unicodeTitlePath
        $unicodeTitlePulse = Invoke-Sanitizer -SourcePath $unicodeTitlePath
        $preIngestionBody = Invoke-Renderer -Pulse $unicodeTitlePulse
        $unicodeTitleBody = Invoke-PinnedOutputSanitizer -Content $preIngestionBody
        Assert-True (-not [string]::Equals($unicodeTitleBody, $preIngestionBody, [StringComparison]::Ordinal)) "The pinned normalizer must exercise the Unicode mutation case '$unicodeTitle'."
        Assert-True ([string]::Equals((Invoke-PinnedOutputSanitizer -Content $unicodeTitleBody), $unicodeTitleBody, [StringComparison]::Ordinal)) "Pinned output sanitization must be idempotent after normalizing '$unicodeTitle'."
        Invoke-PublicationValidator `
            -Pulse $unicodeTitlePulse `
            -AgentOutput (Invoke-PinnedCollector -Body $unicodeTitleBody) `
            -ExpectedBody $unicodeTitleBody
        $safeCellPulses[@("soft-hyphen", "zero-width-joiner", "greek-alpha")[$unicodeIndex++]] = $unicodeTitlePulse
    }

    $wrongIssue = New-ValidAgentOutput -Body $normalBody
    $wrongIssue.items[0].issue_number = $script:DashboardIssueNumber + 1
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $wrongIssue -ExpectedBody $normalBody } "The publication boundary must reject the wrong fixed issue."

    $append = New-ValidAgentOutput -Body $normalBody
    $append.items[0].operation = "append"
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $append -ExpectedBody $normalBody } "The publication boundary must reject append operations."

    $multiple = New-ValidAgentOutput -Body $normalBody
    $multiple.items += $multiple.items[0]
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $multiple -ExpectedBody $normalBody } "The publication boundary must reject multiple outputs."

    $missingErrors = New-ValidAgentOutput -Body $normalBody
    $missingErrors.PSObject.Properties.Remove("errors")
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $missingErrors -ExpectedBody $normalBody } "The publication boundary must require the collector errors array."

    $collectorErrors = New-ValidAgentOutput -Body $normalBody
    $collectorErrors.errors = @("Line 1: rejected")
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $collectorErrors -ExpectedBody $normalBody } "The publication boundary must reject collector validation errors."

    $wrongType = New-ValidAgentOutput -Body $normalBody
    $wrongType.items[0].type = "report_incomplete"
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $wrongType -ExpectedBody $normalBody } "The publication boundary must reject other output types."

    $wrongTypeCase = New-ValidAgentOutput -Body $normalBody
    $wrongTypeCase.items[0].type = "UPDATE_ISSUE"
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $wrongTypeCase -ExpectedBody $normalBody } "The publication boundary must reject alternate output type casing."

    $wrongOperationCase = New-ValidAgentOutput -Body $normalBody
    $wrongOperationCase.items[0].operation = "REPLACE"
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $wrongOperationCase -ExpectedBody $normalBody } "The publication boundary must reject alternate operation casing."

    $forbiddenMutation = New-ValidAgentOutput -Body $normalBody
    $forbiddenMutation.items[0] | Add-Member -NotePropertyName labels -NotePropertyValue @("bug")
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $forbiddenMutation -ExpectedBody $normalBody } "The publication boundary must reject fields that could mutate anything except the body."

    $placeholder = New-ValidAgentOutput -Body "test body"
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $placeholder -ExpectedBody $normalBody } "The publication boundary must reject placeholder reports."

    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutputJson "{not-json" -ExpectedBody $normalBody } "Malformed agent output must fail closed."

    $alteredBody = New-ValidAgentOutput -Body ($normalBody.Replace("Review now", "Review later"))
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $alteredBody -ExpectedBody $normalBody } "The publication boundary must reject altered reports."

    $invisibleMutation = New-ValidAgentOutput -Body ($normalBody.Replace("These views", "These$([char]0x00AD) views"))
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $invisibleMutation -ExpectedBody $normalBody } "The publication boundary must use ordinal equality and reject invisible mutations."

    $wrongExpectedBody = $normalBody + "extra"
    Assert-Throws { Invoke-PublicationValidator -Pulse $normal -AgentOutput $validOutput -ExpectedBody $wrongExpectedBody } "The publication boundary must reject a body not derived from sanitized input."

    Assert-True (Test-Path $workflowPath) "The Pulse workflow source must exist."
    $workflow = Get-Content -Raw $workflowPath
    Assert-True (-not $workflow.Contains("github.repository == 'PureWeen/aspnetcore'")) "The workflow must be deployable upstream instead of remaining fork-only."
    Assert-True ($workflow -match "(?m)^\s*workflow_dispatch:\s*$") "The workflow must remain manually dispatched."
    Assert-True ($workflow.Contains("checkout: false")) "The compiler-managed checkout must be disabled."
    $preStepsIndex = [regex]::Match($workflow, "(?m)^pre-steps:").Index
    $stepsIndex = $workflow.IndexOf("steps:", $preStepsIndex + "pre-steps:".Length, [StringComparison]::Ordinal)
    $checkoutIndex = $workflow.IndexOf("uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1", [StringComparison]::Ordinal)
    Assert-True ($preStepsIndex -ge 0 -and $checkoutIndex -gt $preStepsIndex -and $checkoutIndex -lt $stepsIndex) "The explicit trusted checkout must run only in top-level pre-steps."
    Assert-True ($workflow.Contains("persist-credentials: false")) "The trusted checkout must not persist credentials."
    Assert-True ($workflow.Contains(".github/skills/pr-attention-queue")) "The trusted checkout must include the legacy queue implementation."
    Assert-True ($workflow.Contains(".github/workflows/pr-attention-pulse")) "The trusted checkout must include the Pulse support scripts."
    Assert-True ([regex]::Matches($workflow, "Get-PRAttentionQueue\.ps1 -Repository dotnet/aspnetcore").Count -eq 2) "Production must execute two independent queue collections."
    Assert-True ($workflow.Contains("Get-PRAttentionQueue.ps1 -Repository dotnet/aspnetcore -Preset blazor -DisablePersonalInbox -OutputFormat Json")) "Production must explicitly use the maintained Blazor preset."
    Assert-True ($workflow.Contains("Get-PRAttentionQueue.ps1 -Repository dotnet/aspnetcore -AllRepo -DisablePersonalInbox -OutputFormat Json")) "Production must preserve the repository-wide baseline."
    Assert-True ($workflow.IndexOf("-Preset blazor", [StringComparison]::Ordinal) -lt $workflow.IndexOf("-AllRepo", [StringComparison]::Ordinal)) "The Blazor producer must run before the repository-wide producer."
    Assert-True ($workflow.Contains("Combine-PRAttentionPulse.ps1")) "Production must create one versioned combined sanitized envelope."
    foreach ($path in @("queue-blazor-raw.json", "queue-repository-wide-raw.json", "pulse-blazor.json", "pulse-repository-wide.json"))
    {
        Assert-True ($workflow.Contains($path)) "Production must explicitly account for trusted-only intermediate '$path'."
    }
    Assert-True (-not $workflow.Contains("contents/`$path")) "Trusted source acquisition must not use an additional token-backed Contents API path."
    Assert-True ([regex]::Matches($workflow, "Assert-PrivateValidatorRoot -Path").Count -eq 2) "Private validator storage must be checked before canonical copy and before validation."
    Assert-True ($workflow.Contains('Get-CanonicalPath -Path $env:GITHUB_WORKSPACE')) "The private validator root must be outside the symlink-resolved workspace."
    Assert-True ($workflow.Contains('Get-CanonicalPath -Path "/tmp"')) "The private validator root must be outside symlink-resolved /tmp."
    Assert-True ($workflow.Contains('Join-Path $env:RUNNER_TEMP "gh-aw"')) "The private validator root must be outside the symlink-resolved AWF runtime directory."
    Assert-True ($workflow.Contains("Normalize the trusted Pulse body for ingestion")) "The trusted renderer output must pass through the pinned runtime output sanitizer."
    Assert-True ($workflow.Contains('require(path.join(actionsDir, "sanitize_content.cjs"))')) "Trusted normalization must reuse the pinned gh-aw sanitizer."
    Assert-True ($workflow.Contains('GH_AW_SANITIZER_MODULE_PATH: ${{ runner.temp }}/gh-aw/actions/sanitize_content.cjs')) "Post-agent canonical verification must receive the same pinned sanitizer path."
    Assert-True ($workflow.Contains("-SanitizerModulePath `$env:GH_AW_SANITIZER_MODULE_PATH")) "Post-agent canonical verification must use the pinned sanitizer path."
    Assert-True ($workflow.Contains('bash: ["cat"]')) "The requested shell surface must remain minimal even though v0.88.7 adds baseline utilities."
    Assert-True ($workflow.Contains("gh-aw v0.88.7 retains compiler-required runtime files and a")) "The prompt must accurately distinguish bounded task data from compiler-required runtime files."
    Assert-True ($workflow.Contains("Although the compiler exposes baseline shell utilities")) "The prompt must not claim that the effective shell is cat-only."
    Assert-True ($workflow.Contains("two independently collected dashboards")) "The prompt must preserve both independently generated area reports."
    Assert-True ($workflow.Contains('Remove-Item .pr-attention-pulse/pulse-request.json')) "The serialized request must be removed after inference."
    Assert-True (-not ($workflow -match '(?m)^\s*issue_number:\s*58\s*$')) "The emitted payload must not use the fork's issue 58."
    Assert-True (-not ($workflow -match '(?m)^\s*target:\s*["'']?58["'']?\s*$')) "The safe-output handler must not target the fork's issue 58."
    Assert-True ($workflow.Contains('target: "69328"')) "The safe-output handler must use the permanent dashboard issue."
    Assert-True ($workflow.Contains("operation: replace")) "The payload contract must replace the issue body."
    Assert-True (($workflow | Select-String -Pattern "type: update_issue" -AllMatches).Matches.Count -eq 1) "The prompt must define exactly one update_issue payload."
    Assert-True ($workflow.Contains("github: false")) "The inference sandbox must not mount GitHub tools."
    Assert-True ($workflow -match "network:\s+allowed:\s+\[\]") "The inference sandbox must have no network."
    Assert-True ($workflow.Contains("max-ai-credits: -1")) "Detector token steering must be disabled by removing its credit budget."
    Assert-True ($workflow.Contains("if: always()")) "The trusted publication validator must run even after an earlier agent-job failure."
    Assert-True ($workflow.Contains('pr-attention-pulse-validator/pulse-input.json')) "Post-validation must use the pre-agent trusted input copy."
    Assert-True ($workflow.Contains('pr-attention-pulse-validator/pulse-body.md')) "Post-validation must use the pre-agent trusted body copy."
    Assert-True (-not $workflow.Contains("reviewLifecycle")) "The workflow must not use post-legacy lifecycle fields."
    Assert-True (-not $workflow.Contains("groupOrder")) "The workflow must not use post-legacy group ordering."
    Assert-True (-not $workflow.Contains("merge-candidates")) "The workflow must not use merge-candidate semantics."
    Assert-True ($workflow.Contains("mentions: false")) "Safe output mentions must be disabled."
    Assert-True ($workflow.Contains("- dotnet/aspnetcore")) "Safe-output normalization must allow only the trusted upstream repository references."
    Assert-True (Test-Path $compilePath) "The scoped compiler helper must be present."
    $compileHelper = Get-Content -Raw $compilePath
    Assert-True ($compileHelper.Contains('SetEnvironmentVariable($policyVariable, "gpt-5.6-sol", "Process")')) "The compile helper must scope the singleton model policy to its process."
    Assert-True ($compileHelper.Contains("finally")) "The compile helper must restore the caller's process environment."

    Assert-True (Test-Path $lockPath) "The generated Pulse lock must exist."
    $lock = Get-Content -Raw $lockPath
    $normalizedLock = $lock.Replace("\", "")
    $metadataLine = (Get-Content $lockPath | Select-Object -First 1)
    Assert-True ($metadataLine.Contains('"agent_model":"gpt-5.6-sol"')) "The main agent metadata must pin gpt-5.6-sol."
    Assert-True ($metadataLine.Contains('"detection_agent_model":"gpt-5.6-sol"')) "Threat detection metadata must pin gpt-5.6-sol."
    Assert-True (($lock | Select-String -Pattern "COPILOT_MODEL: gpt-5\.6-sol" -AllMatches).Matches.Count -eq 2) "Both inference stages must receive exactly gpt-5.6-sol."

    $awfConfigLines = @(
        Get-Content $lockPath |
            Where-Object {
                $_.Contains("printf '%s\n'") -and
                $_.Contains("awf-config.schema.json") -and
                $_.Contains('> "${RUNNER_TEMP}/gh-aw/awf-config.json"')
            }
    )
    Assert-True ($awfConfigLines.Count -eq 2) "Exactly two inference AWF configurations are expected."
    foreach ($line in $awfConfigLines)
    {
        $normalizedLine = $line.Replace("\", "")
        Assert-True ($normalizedLine.Contains('"allowedModels":["gpt-5.6-sol"]')) "Every inference stage must enforce the singleton model allowlist."
        Assert-True ($normalizedLine.Contains("v0.28.14")) "Every inference stage must use the reviewed AWF v0.28.14 runtime."
    }
    $mainConfigLine = $awfConfigLines[0].Replace("\", "")
    $detectorConfigLine = $awfConfigLines[1].Replace("\", "")
    Assert-True ($mainConfigLine.Contains('"enableTokenSteering":false')) "Main-agent token steering must be explicitly disabled."
    Assert-True ($mainConfigLine.Contains('"modelFallback":{"enabled":false}')) "Main-agent model fallback must be explicitly disabled."
    Assert-True (-not $detectorConfigLine.Contains("enableTokenSteering")) "Detector token steering must be absent, which is false in AWF."
    Assert-True (-not $detectorConfigLine.Contains("maxAiCredits")) "Detector credit budgeting must be absent to keep token steering disabled."
    Assert-True (-not $detectorConfigLine.Contains("modelFallback")) "Detector fallback is enforced behaviorally by the singleton post-rewrite model policy, not a literal field."
    Assert-True (-not $lock.Contains("GH_AW_EVALS_MODEL")) "No evaluation inference stage may be introduced."
    $publicationPreparationIndex = $lock.IndexOf("name: Preserve canonical Pulse body on publication", [StringComparison]::Ordinal)
    $publicationJobIndex = $lock.IndexOf("`n  safe_outputs:", [StringComparison]::Ordinal)
    $publicationHandlerIndex = $lock.IndexOf("name: Process Safe Outputs", [StringComparison]::Ordinal)
    Assert-True ($publicationPreparationIndex -gt $publicationJobIndex -and $publicationPreparationIndex -lt $publicationHandlerIndex) "Workflow-id decoration must be disabled only in the publication job before the real handler."
    Assert-True ([regex]::Matches($lock, 'core\.exportVariable\("GH_AW_WORKFLOW_ID", ""\)').Count -eq 1) "Only one publication-scoped workflow-id override is allowed."
    Assert-True (-not $lock.Contains("Configure Git credentials")) "No generated git credential step may run without a checkout."
    Assert-True ([regex]::Matches($lock, "(?m)^  safe-outputs:").Count -eq 0) "The hyphenated job alias must not create a custom mutation-capable job."
    Assert-True ([regex]::Matches($lock, "(?m)^  safe_outputs:").Count -eq 1) "Exactly one built-in safe-output job must be generated."
    $checkoutCount = [regex]::Matches($lock, "uses: actions/checkout@").Count
    Assert-True ($checkoutCount -gt 0) "Generated framework checkouts must remain inspectable."
    Assert-True ([regex]::Matches($lock, "persist-credentials: false").Count -eq $checkoutCount) "Every generated checkout must discard its credential."
    Assert-True (-not $lock.Contains('"name":"github","tools"')) "The inference sandbox must not mount the GitHub MCP server."
    Assert-True ($normalizedLock.Contains('"mcp_servers":[{"name":"safeoutputs","tools":["update_issue"]}]')) "Only the fixed update_issue safe-output tool may be mounted."
    Assert-True (($lock | Select-String -Pattern "--exclude-env COPILOT_GITHUB_TOKEN" -AllMatches).Matches.Count -eq 2) "Provider authentication must be excluded from both inference containers."
    $agentStepStart = $lock.IndexOf("- name: Execute GitHub Copilot CLI", [StringComparison]::Ordinal)
    $agentStepEnd = $lock.IndexOf("- name: Detect agent errors", $agentStepStart, [StringComparison]::Ordinal)
    Assert-True ($agentStepStart -ge 0 -and $agentStepEnd -gt $agentStepStart) "The generated main inference step could not be isolated."
    $agentStep = $lock.Substring($agentStepStart, $agentStepEnd - $agentStepStart)
    foreach ($name in @("GH_TOKEN", "GH_AW_GITHUB_TOKEN", "GITHUB_MCP_SERVER_TOKEN", "GITHUB_TOKEN", "OTEL_EXPORTER_OTLP_HEADERS", "GH_AW_OTLP_ENDPOINTS"))
    {
        Assert-True ([regex]::Matches($agentStep, "(?<!\S)--exclude-env $([regex]::Escape($name))(?=\s|\\\\)").Count -eq 1) "The main inference command must exclude '$name' exactly once."
        Assert-True ([regex]::Matches($agentStep, "(?m)^\s+$([regex]::Escape($name)): \$\{\{ needs\.pat_pool\.outputs\.pat_number \}\}\r?$").Count -eq 1) "The v0.88.7 compatibility adapter must bind '$name' only to the non-secret PAT slot number."
        Assert-True (-not ($agentStep -match "(?m)^\s+$([regex]::Escape($name)):.*secrets\.")) "The main inference step must not bind '$name' to a secret-bearing workflow value."
        Assert-True (-not ($agentStep -match "(?m)\bexport\s+$([regex]::Escape($name))=")) "The main inference command must not export '$name' into the sandbox."
    }
    Assert-True (-not $normalizedLock.Contains('"target":"58"')) "The generated safe-output policy must not pin the fork's issue 58."
    Assert-True ($normalizedLock.Contains('"target":"69328"')) "The generated safe-output policy must preserve the permanent dashboard target."
    Assert-True ($normalizedLock.Contains('"required_title_prefix":"[pr-attention-pulse]"')) "The generated safe-output policy must require the dashboard title prefix."
    Assert-True (-not $normalizedLock.Contains('"create_issue"')) "The generated workflow must not expose issue creation."
    Assert-True ($lock.IndexOf("Remove-Item -Recurse -Force .github", [StringComparison]::Ordinal) -lt $agentStepStart) "Repository workflow sources and raw data must be removed before inference."
    $activationCleanupIndex = $lock.IndexOf("name: Remove repository data from activation artifact", [StringComparison]::Ordinal)
    $activationUploadIndex = $lock.IndexOf("name: Upload activation artifact", [StringComparison]::Ordinal)
    Assert-True ($activationCleanupIndex -ge 0 -and $activationCleanupIndex -lt $activationUploadIndex) "Activation repository data must be removed before artifact staging."
    $artifactDownloadIndex = $lock.IndexOf("name: Download activation artifact", [StringComparison]::Ordinal)
    $preAgentBoundaryIndex = $lock.IndexOf("name: Enforce model-visible Pulse boundary", [StringComparison]::Ordinal)
    Assert-True ($preAgentBoundaryIndex -gt $artifactDownloadIndex -and $preAgentBoundaryIndex -lt $agentStepStart) "The model-visible boundary must be re-established after artifact download and before inference."
    Assert-True ($agentStep.Contains("--add-dir /tmp/gh-aw/") -and $agentStep.Contains('--add-dir "${GITHUB_WORKSPACE}"')) "The effective model-visible mounts must remain explicit in regression coverage."
    foreach ($path in @("/tmp/gh-aw/base", "/tmp/gh-aw/.github/agents", "/tmp/gh-aw/.github/skills"))
    {
        Assert-True (-not $agentStep.Contains($path)) "The generated inference command must not directly depend on deleted repository-derived path '$path'."
    }
    $validatorStepIndex = $lock.IndexOf("name: Validate the sole publication payload", [StringComparison]::Ordinal)
    $evidenceStepIndex = $lock.IndexOf("name: Upload validated Pulse publication evidence", [StringComparison]::Ordinal)
    $cleanupStepIndex = $lock.IndexOf("name: Remove sanitized Pulse data", [StringComparison]::Ordinal)
    Assert-True ($evidenceStepIndex -gt $validatorStepIndex -and $evidenceStepIndex -lt $cleanupStepIndex) "Canonical audit evidence must be retained only after validation and before cleanup."
    $evidenceStep = $lock.Substring($evidenceStepIndex, $cleanupStepIndex - $evidenceStepIndex)
    Assert-True ($evidenceStep.Contains('pr-attention-pulse-validator/pulse-input.json') -and $evidenceStep.Contains('pr-attention-pulse-validator/pulse-body.md')) "Publication evidence must use the private canonical copies, not model-visible files."
    Assert-True (-not $lock.Contains("--allow-tool shell(jq)")) "Publication must not grant jq permission."
    $fallbackArtifactIndex = $lock.IndexOf("name: Upload agent output fallback artifact", [StringComparison]::Ordinal)
    Assert-True ($validatorStepIndex -gt $agentStepStart -and $validatorStepIndex -lt $fallbackArtifactIndex) "Trusted validation must run before either publication artifact is uploaded."
    Assert-True ($lock.Substring([Math]::Max(0, $validatorStepIndex - 40), [Math]::Min(120, $lock.Length - [Math]::Max(0, $validatorStepIndex - 40))).Contains("if: always()")) "Trusted validation must run after any earlier agent-job failure."
    $canonicalCopyIndex = $lock.IndexOf("Copy-Item .pr-attention-pulse/pulse-input.json", [StringComparison]::Ordinal)
    $normalizationStepIndex = $lock.IndexOf("name: Normalize the trusted Pulse body for ingestion", [StringComparison]::Ordinal)
    $firstPrivateRootCheckIndex = $lock.IndexOf("Assert-PrivateValidatorRoot -Path `$validatorRoot", [StringComparison]::Ordinal)
    $secondPrivateRootCheckIndex = $lock.LastIndexOf("Assert-PrivateValidatorRoot -Path", [StringComparison]::Ordinal)
    $validatorInvocationIndex = $lock.IndexOf("Validate-PRAttentionPulseOutput.ps1", $secondPrivateRootCheckIndex, [StringComparison]::Ordinal)
    Assert-True ($normalizationStepIndex -ge 0 -and $normalizationStepIndex -lt $firstPrivateRootCheckIndex) "Pinned output normalization must run before the canonical body is protected."
    Assert-True ($firstPrivateRootCheckIndex -ge 0 -and $firstPrivateRootCheckIndex -lt $canonicalCopyIndex) "The private validator root must be checked before canonical data is copied."
    Assert-True ($secondPrivateRootCheckIndex -gt $validatorStepIndex -and $secondPrivateRootCheckIndex -lt $validatorInvocationIndex) "The private validator root must be checked again immediately before validation."
    Assert-True (-not ($lock -match "--mount[^\r\n]*pr-attention-pulse-validator")) "The private validator root must not be mounted into either inference sandbox."
    Assert-True ($lock.Contains('(always() && needs.agent.result != ''skipped'') && (needs.agent.result == ''success'')')) "Threat detection must require successful trusted validation."
    Assert-True ($lock.Contains('(needs.agent.result == ''success'')')) "Safe-output publication must require successful trusted validation."
    Assert-True ($lock.Contains('daily_ai_credits_exceeded == ''true'')) && (false)')) "The conclusion job must be unreachable so detector/failure tracking cannot mutate GitHub."
    Assert-True ($lock.Contains("GH_AW_VALIDATION_JSON")) "The generated lock must expose the exact collector validation contract."
    Assert-True ($lock -match '"body":\s*\{\s*"type": "string",\s*"sanitize": true,\s*"maxLength": 65000') "The generated collector must sanitize and bound the issue body."
    Assert-True ($lock -match '"operation":\s*\{\s*"type": "string",\s*"enum":\s*\[\s*"replace"') "The generated collector must preserve the replacement operation contract."
    Assert-True ($lock -match "permissions:\s+contents: read") "The generated agent job must keep minimum read-only repository permissions."
    Assert-True ($lock -match "permissions:\s+issues: write") "Only the safe-output publication job may receive issue write permission."

    Write-Output "PASS ExistingPreservationAndPublication"
    $presentationFailures = [Collections.Generic.List[string]]::new()
    $published = Get-Content -LiteralPath (Join-Path $presentationRoot "published-34643961191.pulse.json") -Raw | ConvertFrom-Json -Depth 100
    $published | Add-Member -NotePropertyName scope -NotePropertyValue "repository-wide"
    $presentationFixtures = [ordered]@{
        "normal-legacy" = $normal
        "complete-zero" = $zero
        "incomplete-query" = $incomplete
        "bounded-empty-verification" = $boundedDiscussionResult
        "published-34643961191" = $published
    }

    foreach ($scope in @("blazor", "repository-wide"))
    {
        foreach ($viewName in @("reviewNow", "verifyDiscussionBeforeReview", "needsRescue", "readyToMerge"))
        {
            foreach ($expectedMarker in @($true, $false))
            {
                $expectation = if ($expectedMarker) { "exact-code" } else { "partial-code" }
                Invoke-PresentationCase "CommunityMarker/$scope/$viewName/$expectation" {
                    Assert-CommunityMarkerPresentation `
                        -Scope $scope `
                        -ViewName $viewName `
                        -ExpectedMarker $expectedMarker `
                        -BlazorBaseline $normal `
                        -RepositoryWideBaseline $repositoryWideNormal
                }
            }
        }
    }

    Invoke-PresentationCase "CollapsedSummary/complete" {
        $body = Invoke-Renderer -Pulse $normal
        Assert-True ($body.Contains("<summary><strong>Blazor</strong> - 5 matched; shown: 2 review now, 1 verify discussion, 1 rescue, 1 ready; generated 2026-09-10T20:00:00.0000000Z</summary>")) "A complete summary must use displayed array lengths."
    }
    Invoke-PresentationCase "CollapsedSummary/complete-zero" {
        $body = Invoke-Renderer -Pulse $zero
        Assert-True ($body.Contains("<summary><strong>Blazor</strong> - 0 matched; shown: 0 review now, 0 verify discussion, 0 rescue, 0 ready; generated 2026-09-10T20:00:00.0000000Z</summary>")) "A complete-zero summary must explicitly show four zero displayed counts."
    }
    Invoke-PresentationCase "CollapsedSummary/capped" {
        $capped = $realPulses["repository-wide/discussion-pull-requests.json"]
        $body = Invoke-Renderer -Pulse $capped
        $expected = "<summary><strong>Repository-wide</strong> - $($capped.source.census.matched) matched; shown: $(@($capped.views.reviewNow).Count) review now, $(@($capped.views.verifyDiscussionBeforeReview).Count) verify discussion, $(@($capped.views.needsRescue).Count) rescue, $(@($capped.views.readyToMerge).Count) ready; generated "
        Assert-True ($capped.source.overflow.reviewNow -gt 0) "The capped summary control requires real producer overflow."
        Assert-True ($body.Contains($expected)) "A capped summary must report displayed arrays instead of inventory counts."
        Invoke-PublicationValidator -Pulse $capped -AgentOutput (New-ValidAgentOutput -Body $body) -ExpectedBody $body
    }
    Invoke-PresentationCase "CollapsedSummary/unassessed" {
        $body = Invoke-Renderer -Pulse $unassessedDiscussionResult
        $expected = "<summary><strong>Blazor</strong> - $($unassessedDiscussionResult.source.census.matched) matched; shown: $(@($unassessedDiscussionResult.views.reviewNow).Count) review now, $(@($unassessedDiscussionResult.views.verifyDiscussionBeforeReview).Count) verify discussion, $(@($unassessedDiscussionResult.views.needsRescue).Count) rescue, $(@($unassessedDiscussionResult.views.readyToMerge).Count) ready; generated "
        Assert-True ($unassessedDiscussionResult.source.discussion.unassessedReviewNowCount -gt 0) "The unassessed summary control requires a real sanitized unassessed count."
        Assert-True ($body.Contains($expected)) "An unassessed summary must report displayed arrays instead of deriving ordinary review count."
        Assert-True ($body.Contains("$($unassessedDiscussionResult.source.discussion.unassessedReviewNowCount) Review now candidates unassessed")) "The unassessed count must remain in the area audit."
        Invoke-PublicationValidator -Pulse $unassessedDiscussionResult -AgentOutput (New-ValidAgentOutput -Body $body) -ExpectedBody $body
    }
    Invoke-PresentationCase "CollapsedSummary/unavailable" {
        $body = Invoke-Renderer -Pulse $collectionFailure
        Assert-True ($body.Contains("<summary><strong>Blazor</strong> - data unavailable; attempted 2026-09-10T20:04:56.0000000Z</summary>")) "An unavailable summary must remain distinct from complete zero."
        Assert-True (-not $body.Contains("shown:")) "An unavailable summary must not manufacture displayed counts."
    }

    $tableFixtures = [ordered]@{}
    foreach ($name in $presentationFixtures.Keys)
    {
        $tableFixtures[$name] = $presentationFixtures[$name]
    }
    foreach ($name in $realPulses.Keys)
    {
        $tableFixtures[$name] = $realPulses[$name]
    }

    $variedPath = Join-Path $tempRoot "varied-presentation.json"
    $varied = Get-Content -LiteralPath (Join-Path $fixtureRoot "normal-legacy.json") -Raw | ConvertFrom-Json -Depth 100
    $varied.items[0].reasonCodes = @("last-code", "first-code", "last-code")
    $varied.items[1].reasonCodes = @()
    $varied.items[2].discussionAssessment.signals = @()
    $varied.items[3].blockers = @("First blocking condition.", "Second blocking condition.")
    $varied.census.labelOnly = 0
    $varied.census.pathOnly = 1
    $varied.census.labelAndPath = 4
    $varied.census.incidentalPathExcluded = 2
    $varied.census.unresolvedMergeable = 3
    $varied.warnings = @("First warning.", "Repeated warning.", "Repeated warning.", "Last warning.")
    Write-JsonFile -Value $varied -Path $variedPath
    $tableFixtures["varied-evidence"] = Invoke-Sanitizer -SourcePath $variedPath
    Assert-True ($tableFixtures["varied-evidence"].status -ceq "complete") "The synthetic varied-evidence fixture must reach rendering."
    $boundedDigestPath = Join-Path $tempRoot "bounded-empty-digest.json"
    $boundedDigest = Get-Content -LiteralPath (Join-Path $fixtureRoot "normal-legacy.json") -Raw | ConvertFrom-Json -Depth 100
    foreach ($item in $boundedDigest.items)
    {
        $item.shownInDigest = $false
        $item.digestRank = $null
    }
    Write-JsonFile -Value $boundedDigest -Path $boundedDigestPath
    $boundedDigestResult = Invoke-Sanitizer -SourcePath $boundedDigestPath
    Assert-True ($boundedDigestResult.status -ceq "complete") "The synthetic empty digest with positive inventory totals must reach rendering."
    $tableFixtures["bounded-empty-digest"] = $boundedDigestResult
    foreach ($name in $tableFixtures.Keys)
    {
        Invoke-PresentationCase "PresentationTablesAndFields/$name" {
            $pulse = $tableFixtures[$name]
            Assert-PresentationTablesAndFields -Pulse $pulse -Body (Invoke-Renderer -Pulse $pulse)
        }
    }

    $scopeFixtures = [ordered]@{ "uniform-published" = $published; "mixed" = $normal; "empty" = $zero; "displayed-only" = $boundedDigestResult }
    foreach ($name in @("uniform-not-query-coverage"))
    {
        $raw = Get-Content -LiteralPath (Join-Path $fixtureRoot "normal-legacy.json") -Raw | ConvertFrom-Json -Depth 100
        foreach ($item in $raw.items)
        {
            $item.scopeMatch = "label-only"
        }
        $path = Join-Path $tempRoot "$name.json"
        Write-JsonFile -Value $raw -Path $path
        $scopeFixtures[$name] = Invoke-Sanitizer -SourcePath $path
        Assert-True ($scopeFixtures[$name].status -ceq "complete") "The synthetic $name fixture must reach rendering."
    }
    foreach ($name in $scopeFixtures.Keys)
    {
        Invoke-PresentationCase "PresentationScopePlacement/$name" {
            $pulse = $scopeFixtures[$name]
            Assert-PresentationScopePlacement -Pulse $pulse -Body (Invoke-Renderer -Pulse $pulse)
        }
    }

    foreach ($length in @(239, 240, 241))
    {
        $raw = Get-Content -LiteralPath (Join-Path $fixtureRoot "normal-legacy.json") -Raw | ConvertFrom-Json -Depth 100
        $raw.items[1].title = "x" * $length
        $path = Join-Path $tempRoot "title-$length.json"
        Write-JsonFile -Value $raw -Path $path
        $pulse = Invoke-Sanitizer -SourcePath $path
        $expectedTitle = if ($length -le 240)
        {
            "x" * $length
        }
        else
        {
            ("x" * 237) + "..."
        }
        Assert-True ($pulse.status -ceq "complete" -and $pulse.views.reviewNow[0].title -ceq $expectedTitle) "The existing 240-character sanitizer boundary must remain exact for length $length."
        $safeCellPulses["title-$length"] = $pulse
    }
    foreach ($name in $safeCellPulses.Keys)
    {
        Invoke-PresentationCase "PresentationSafeCells/$name" {
            $pulse = $safeCellPulses[$name]
            $body = Invoke-Renderer -Pulse $pulse
            Assert-PresentationTablesAndFields -Pulse $pulse -Body $body
            $normalized = Invoke-PinnedOutputSanitizer -Content $body
            $rawRows = @([regex]::Matches($body, '(?m)^\| [0-9]+: .+ \|$') | ForEach-Object Value)
            $normalizedRows = @([regex]::Matches($normalized, '(?m)^\| [0-9]+: .+ \|$') | ForEach-Object Value)
            Assert-True ($rawRows.Count -eq $normalizedRows.Count) "Real ingestion must not introduce or remove candidate rows."
            for ($index = 0; $index -lt $rawRows.Count; $index++)
            {
                Assert-True ($normalizedRows[$index].Split("|").Count -eq 8) "Real ingestion must retain exactly six cells."
                $expectedRow = Invoke-PinnedOutputSanitizer -Content $rawRows[$index]
                Assert-True ([string]::Equals($normalizedRows[$index], $expectedRow, [StringComparison]::Ordinal)) "Only the existing pinned Unicode transformations may change a complete candidate row."
            }
            Assert-True ($normalized.Length -ge 200 -and $normalized.Length -le 65000) "The original publication body size limits must remain sufficient."
        }
    }

    $emptyFixtures = [ordered]@{ "complete-zero" = $zero; "bounded-empty-verification" = $boundedDiscussionResult; "bounded-empty-digest" = $boundedDigestResult; "missing-count" = $missingFieldResult }
    foreach ($name in $failureEnvelopes.Keys)
    {
        $emptyFixtures[$name] = $failureEnvelopes[$name]
    }
    foreach ($name in @("null-census-count", "null-candidate-age", "null-thread-count"))
    {
        $raw = Get-Content -LiteralPath (Join-Path $fixtureRoot "normal-legacy.json") -Raw | ConvertFrom-Json -Depth 100
        switch ($name)
        {
            "null-census-count"
            {
                $raw.census.matched = $null
            }
            "null-candidate-age"
            {
                $raw.items[1].ageDays = $null
            }
            "null-thread-count"
            {
                $raw.items[2].discussionAssessment.threads.returnedCount = $null
            }
        }
        $path = Join-Path $tempRoot "$name.json"
        Write-JsonFile -Value $raw -Path $path
        $emptyFixtures[$name] = Invoke-Sanitizer -SourcePath $path
        Assert-True ($emptyFixtures[$name].errorCategory -ceq "invalid-contract") "The $name fixture must fail the real sanitizer instead of manufacturing zero."
    }
    foreach ($name in $emptyFixtures.Keys)
    {
        Invoke-PresentationCase "PresentationEmptyAndUnavailable/$name" {
            $pulse = $emptyFixtures[$name]
            $body = Invoke-Renderer -Pulse $pulse
            Assert-PresentationLayout -Pulse $pulse -Body $body
            if ($pulse.status -ceq "unavailable")
            {
                Assert-True (-not $pulse.candidateCountsAvailable -and $null -eq $pulse.source.census) "Unavailable must not manufacture a census."
                Assert-True ($body.Contains("Error category: ``$($pulse.errorCategory)``.") -and $body.Contains("Attempted: ``2026-09-10T20:04:56.0000000Z``.")) "Unavailable must preserve category and exact attempted time."
                Assert-True ($body.Contains("Candidate counts unavailable.") -and $body.Contains("No complete source coverage was available for this area.") -and -not ($body -match "(?m)^\||\bOpen: 0")) "Unavailable is not an empty successful inventory."
                Assert-True ([regex]::Matches($body, "Unavailable because this area's collection did not produce a complete compatible inventory\.").Count -eq 4) "Every unavailable candidate section must explain its state."
            }
            else
            {
                foreach ($viewName in $presentationViews.Keys)
                {
                    if (@($pulse.views.$viewName).Count -gt 0)
                    {
                        continue
                    }
                    $sectionName, $bucket = $presentationViews[$viewName]
                    $total = if ($viewName -ceq "verifyDiscussionBeforeReview")
                    {
                        $pulse.source.discussion.verificationNeededCount
                    }
                    else
                    {
                        $pulse.source.census.byBucket.$bucket
                    }
                    $section = Get-PresentationSection -Body $body -Name $sectionName
                    $message = if ($total -eq 0)
                    {
                        "None in this complete inventory."
                    }
                    else
                    {
                        "No candidates shown in this view."
                    }
                    Assert-True ($section.Contains($message)) "An empty $sectionName view with source total $total must say '$message'."
                    Assert-True (-not ($section -match "(?m)^\|")) "An empty view must not invent rows."
                    if ($total -gt 0)
                    {
                        Assert-True (-not $section.Contains("None in this complete inventory.")) "Zero shown must not imply zero source candidates."
                    }
                }
            }
        }
    }

    foreach ($name in $presentationFixtures.Keys)
    {
        Invoke-PresentationCase "PresentationGoldenDeterminism/$name" {
            $pulse = $presentationFixtures[$name]
            # Only fixture checkout transport is normalized, never a rendered or submitted body.
            $expected = [IO.File]::ReadAllText((Join-Path $presentationRoot "$name.expected.txt")).Replace("`r`n", "`n")
            Assert-True (-not $expected.EndsWith("`n") -and -not $expected.Contains("`r")) "Golden files must have canonical LF with no final newline."
            $culture = [Globalization.CultureInfo]::CurrentCulture
            $uiCulture = [Globalization.CultureInfo]::CurrentUICulture
            try
            {
                foreach ($cultureName in @("en-US", "fr-FR", "tr-TR"))
                {
                    [Globalization.CultureInfo]::CurrentCulture = [Globalization.CultureInfo]::GetCultureInfo($cultureName)
                    [Globalization.CultureInfo]::CurrentUICulture = [Globalization.CultureInfo]::GetCultureInfo($cultureName)
                    $actual = Invoke-PinnedOutputSanitizer -Content (Invoke-Renderer -Pulse $pulse)
                    Assert-True ([string]::Equals($actual, $expected, [StringComparison]::Ordinal)) "The reviewed $name golden differs under $cultureName (actual $($actual.Length), expected $($expected.Length) characters)."
                }
            }
            finally
            {
                [Globalization.CultureInfo]::CurrentCulture = $culture
                [Globalization.CultureInfo]::CurrentUICulture = $uiCulture
            }
        }
    }

    $roundTripFixtures = [ordered]@{}
    foreach ($name in $presentationFixtures.Keys)
    {
        $roundTripFixtures[$name] = $presentationFixtures[$name]
    }
    $roundTripFixtures["discussion-pull-requests"] = $realPulses["repository-wide/discussion-pull-requests.json"]
    $roundTripFixtures["varied-evidence"] = $tableFixtures["varied-evidence"]
    $roundTripFixtures["bounded-empty-digest"] = $boundedDigestResult
    foreach ($name in $roundTripFixtures.Keys)
    {
        Invoke-PresentationCase "PresentationCanonicalRoundTrip/$name" {
            $pulse = $roundTripFixtures[$name]
            $rendered = Invoke-Renderer -Pulse $pulse
            $canonical = Invoke-PinnedOutputSanitizer -Content $rendered
            Assert-True ([string]::Equals($rendered, $canonical, [StringComparison]::Ordinal)) "These non-Unicode-mutation fixtures must retain every rendered field through normalization."
            Assert-True ([string]::Equals((Invoke-PinnedOutputSanitizer -Content $canonical), $canonical, [StringComparison]::Ordinal)) "Trusted normalization must remain idempotent."
            $collected = Invoke-PinnedCollector -Body $canonical
            Assert-True ([string]::Equals($collected.items[0].body, $canonical, [StringComparison]::Ordinal)) "The actual collector must preserve the exact normalized body."
            Invoke-PublicationValidator -Pulse $pulse -AgentOutput $collected -ExpectedBody $canonical
            $path = Join-Path $tempRoot "$name.publication.md"
            [IO.File]::WriteAllText($path, $collected.items[0].body, [Text.UTF8Encoding]::new($false))
            & node (Join-Path $testRoot "Test-PulseIssueUpdate.cjs") $collectorJsRoot $lockPath $path
            Assert-True ($LASTEXITCODE -eq 0) "The real pinned issue handler must send the exact validated $name body."
        }
    }
    $canonicalLink = "[dotnet/aspnetcore#101](https://github.com/dotnet/aspnetcore/pull/101)"
    $linkTamperingCases = [ordered]@{
        "mismatched-label-target" = { param($body) $body.Replace($canonicalLink, "[dotnet/aspnetcore#101](https://github.com/dotnet/aspnetcore/pull/102)") }
        "wrong-owner-repo" = { param($body) $body.Replace($canonicalLink, "[dotnet/runtime#101](https://github.com/dotnet/runtime/pull/101)") }
        "issue-path" = { param($body) $body.Replace($canonicalLink, "[dotnet/aspnetcore#101](https://github.com/dotnet/aspnetcore/issues/101)") }
        "query-suffix" = { param($body) $body.Replace($canonicalLink, "[dotnet/aspnetcore#101](https://github.com/dotnet/aspnetcore/pull/101?notification_referrer_id=1)") }
        "fragment-suffix" = { param($body) $body.Replace($canonicalLink, "[dotnet/aspnetcore#101](https://github.com/dotnet/aspnetcore/pull/101#discussion)") }
        "duplicate-reference" = { param($body) $body.Replace("| First review candidate |", "| First review candidate $canonicalLink |") }
        "extra-arbitrary-link" = { param($body) $body.Replace("| First review candidate |", "| First review candidate [outside](https://example.com/) |") }
        "malformed-number" = { param($body) $body.Replace($canonicalLink, "[dotnet/aspnetcore#001](https://github.com/dotnet/aspnetcore/pull/001)") }
        "candidate-url-text" = { param($body) $body.Replace("| First review candidate |", "| First review candidate https://attacker.example/path |") }
    }
    foreach ($name in $linkTamperingCases.Keys)
    {
        Invoke-PresentationCase "PresentationCanonicalRoundTrip/$name" {
            $canonical = Invoke-PinnedOutputSanitizer -Content (Invoke-Renderer -Pulse $normal)
            Assert-True ($canonical.Contains("| First review candidate |") -and $canonical.Contains($canonicalLink)) "The canonical link and candidate cell must exist before exercising tampering."
            $tampered = & $linkTamperingCases[$name] $canonical
            Assert-True (-not [string]::Equals($tampered, $canonical, [StringComparison]::Ordinal)) "Table tampering must actually change the body."
            Import-Module -Scope Local -Force (Join-Path $supportRoot "PRAttentionPulseContract.psm1")
            Assert-Throws `
                -Action { Assert-PRAttentionPulseOutput -AgentOutput (New-ValidAgentOutput -Body $tampered) -Pulse $normal -ExpectedBody $tampered -ExpectedIssueNumber $script:DashboardIssueNumber } `
                -Message "The direct publication contract must reject $name."
            Assert-Throws `
                -Action { Invoke-PublicationValidator -Pulse $normal -AgentOutput (New-ValidAgentOutput -Body $tampered) -ExpectedBody $canonical } `
                -Message "The publication boundary must reject $name and remove publishable output." `
                -ExpectedMessage "The emitted body does not exactly match"
        }
    }
    Invoke-PresentationCase "PresentationCanonicalRoundTrip/changed-cell" {
        $canonical = Invoke-PinnedOutputSanitizer -Content (Invoke-Renderer -Pulse $normal)
        $tampered = $canonical.Replace("| First review candidate |", "| Altered title |")
        Assert-True (-not [string]::Equals($tampered, $canonical, [StringComparison]::Ordinal)) "Table tampering must actually change the body."
        Assert-Throws `
            -Action { Invoke-PublicationValidator -Pulse $normal -AgentOutput (New-ValidAgentOutput -Body $tampered) -ExpectedBody $canonical } `
            -Message "The publication boundary must reject changed-cell and remove publishable output." `
            -ExpectedMessage "The emitted body does not exactly match"
    }
    Assert-True ($presentationFailures.Count -eq 0) "$($presentationFailures.Count) presentation case(s) failed; see every named outcome above."
    Write-Output "PR Attention Pulse tests passed."
}
finally
{
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
