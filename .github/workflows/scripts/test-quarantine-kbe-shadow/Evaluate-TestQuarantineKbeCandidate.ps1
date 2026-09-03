#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CandidateFile,

    [Parameter(Mandatory = $true)]
    [string]$EvidenceRoot,

    [Parameter(Mandatory = $true)]
    [string]$OutputFile,

    [string]$RepositoryRoot = "$PSScriptRoot/../../../..",

    [string]$CandidateSchemaFile = "$PSScriptRoot/test-quarantine-kbe-shadow-candidate.schema.json",

    [string]$ReceiptSchemaFile = "$PSScriptRoot/test-quarantine-kbe-shadow-receipt.schema.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$minimumFailureEvidenceFloor = 2
$minimumNegativeEvidenceFloor = 1
$failureAssociationWindowLines = 50

function Get-Sha256String
{
    param([Parameter(Mandatory = $true)][string]$Value)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)

    return [System.Convert]::ToHexString($hash).ToLowerInvariant()
}

function Get-SafeExcerpt
{
    param([Parameter(Mandatory = $true)][string]$Value)

    $excerpt = [System.Text.RegularExpressions.Regex]::Replace($Value, "[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "?").Trim()
    if ($excerpt.Length -gt 300)
    {
        $excerpt = $excerpt.Substring(0, 300)
    }

    return $excerpt
}

function Resolve-EvidencePath
{
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    if ([System.IO.Path]::IsPathRooted($RelativePath))
    {
        throw "Evidence path must be relative: $RelativePath"
    }

    $rootInfo = [System.IO.DirectoryInfo]::new($Root)
    if ($null -ne $rootInfo.LinkTarget)
    {
        throw "Evidence root must not be a symbolic link."
    }

    $segments = @($RelativePath -split "[\\/]")
    if ($segments.Count -eq 0 -or
        @($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -in @(".", "..") }).Count -gt 0)
    {
        throw "Evidence path contains an invalid path segment: $RelativePath"
    }

    $normalizedRelativePath = [string]::Join([System.IO.Path]::DirectorySeparatorChar, $segments)
    $candidatePath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($Root, $normalizedRelativePath))
    $rootPrefix = $Root.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $candidatePath.StartsWith($rootPrefix, [System.StringComparison]::Ordinal))
    {
        throw "Evidence path escapes the evidence root: $RelativePath"
    }

    $currentPath = $Root
    foreach ($segment in $segments)
    {
        $currentPath = [System.IO.Path]::Combine($currentPath, $segment)
        if ([System.IO.Directory]::Exists($currentPath) -or [System.IO.File]::Exists($currentPath))
        {
            $itemInfo = Get-Item -LiteralPath $currentPath -Force
            if ($null -ne $itemInfo.LinkTarget)
            {
                throw "Evidence path must not traverse a symbolic link: $RelativePath"
            }
        }
    }

    if (-not [System.IO.File]::Exists($candidatePath))
    {
        throw "Evidence file does not exist: $RelativePath"
    }

    return $candidatePath
}

function New-MatchedLine
{
    param(
        [Parameter(Mandatory = $true)][int]$LineNumber,
        [Parameter(Mandatory = $true)][int]$PatternIndex,
        [Parameter(Mandatory = $true)][string]$Line
    )

    return [ordered]@{
        line_number = $LineNumber
        pattern_index = $PatternIndex
        line_sha256 = Get-Sha256String -Value $Line
        excerpt = Get-SafeExcerpt -Value $Line
    }
}

function Test-Log
{
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Kind,
        [Parameter(Mandatory = $true)][string[]]$Values,
        [Parameter(Mandatory = $true)][string]$TestName,
        [Parameter(Mandatory = $true)][int]$FailureAssociationWindowLines,
        [System.Text.RegularExpressions.Regex[]]$Regexes
    )

    $matchedLines = [System.Collections.Generic.List[object]]::new()
    $matchedLineNumbers = [System.Collections.Generic.List[int]]::new()
    $failedTestLineNumbers = [System.Collections.Generic.List[int]]::new()
    $lineNumber = 0
    $matchCount = 0
    $passOrSkipMatchCount = 0
    $regexTimeoutCount = 0
    $patternIndex = 0
    $matched = $false
    $escapedTestName = [System.Text.RegularExpressions.Regex]::Escape($TestName)
    $testNameMatcher = [System.Text.RegularExpressions.Regex]::new(
        "(^|[^A-Za-z0-9_.+])$escapedTestName($|[^A-Za-z0-9_.+])",
        [System.Text.RegularExpressions.RegexOptions]::CultureInvariant -bor
            [System.Text.RegularExpressions.RegexOptions]::NonBacktracking,
        [System.TimeSpan]::FromMilliseconds(50))

    foreach ($line in [System.IO.File]::ReadLines($Path))
    {
        $lineNumber++

        $normalizedLine = $line -replace "^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z\s+", ""
        $lineContainsTest = $testNameMatcher.IsMatch($normalizedLine)
        $lineIndicatesFailure =
            $normalizedLine -match "(?i)^\s*\[FAIL(?:ED)?\]\s+" -or
            $normalizedLine -match "(?i)^\s*Failed\s+" -or
            $normalizedLine -match "(?i)^\s*\[[^\]\r\n]+\]\s+.+\s+\[FAIL(?:ED)?\]\s*$"
        $isFailedTestMarker = $lineContainsTest -and $lineIndicatesFailure
        if ($isFailedTestMarker)
        {
            $failedTestLineNumbers.Add($lineNumber)
        }

        if ($normalizedLine -match "(?i)^\s*(?:\[(?:PASS|SKIP)\]\s+|Passed\s+|Skipped\s+)")
        {
            for ($index = 0; $index -lt $Values.Count; $index++)
            {
                try
                {
                    $passOrSkipMatched = if ($Kind -eq "ErrorPattern")
                    {
                        $Regexes[$index].IsMatch($line)
                    }
                    else
                    {
                        $line.IndexOf($Values[$index], [System.StringComparison]::Ordinal) -ge 0
                    }
                }
                catch [System.Text.RegularExpressions.RegexMatchTimeoutException]
                {
                    $regexTimeoutCount++
                    $passOrSkipMatched = $false
                }

                if ($passOrSkipMatched)
                {
                    $passOrSkipMatchCount++
                    break
                }
            }
        }

        if ($isFailedTestMarker)
        {
            continue
        }

        if ($Values.Count -eq 1)
        {
            try
            {
                $lineMatched = if ($Kind -eq "ErrorPattern")
                {
                    $Regexes[0].IsMatch($line)
                }
                else
                {
                    $line.IndexOf($Values[0], [System.StringComparison]::Ordinal) -ge 0
                }
            }
            catch [System.Text.RegularExpressions.RegexMatchTimeoutException]
            {
                $regexTimeoutCount++
                $lineMatched = $false
            }

            if ($lineMatched)
            {
                $matched = $true
                $matchCount++
                $matchedLineNumbers.Add($lineNumber)

                if ($matchedLines.Count -lt 20)
                {
                    $matchedLines.Add((New-MatchedLine -LineNumber $lineNumber -PatternIndex 0 -Line $line))
                }
            }

            continue
        }

        if ($matched)
        {
            continue
        }

        try
        {
            $lineMatched = if ($Kind -eq "ErrorPattern")
            {
                $Regexes[$patternIndex].IsMatch($line)
            }
            else
            {
                $line.IndexOf($Values[$patternIndex], [System.StringComparison]::Ordinal) -ge 0
            }
        }
        catch [System.Text.RegularExpressions.RegexMatchTimeoutException]
        {
            $regexTimeoutCount++
            $lineMatched = $false
        }

        if ($lineMatched)
        {
            $matchedLines.Add((New-MatchedLine -LineNumber $lineNumber -PatternIndex $patternIndex -Line $line))
            $matchedLineNumbers.Add($lineNumber)

            $patternIndex++
            if ($patternIndex -eq $Values.Count)
            {
                $matched = $true
                $matchCount = 1
            }
        }
    }

    $signatureAssociatedWithFailedTest =
        $matched -and
        $failedTestLineNumbers.Count -gt 0 -and
        @(
            $matchedLineNumbers |
                Where-Object {
                    $matchedLineNumber = $_
                    @(
                        $failedTestLineNumbers |
                            Where-Object { [System.Math]::Abs($_ - $matchedLineNumber) -le $FailureAssociationWindowLines }
                    ).Count -gt 0
                }
        ).Count -gt 0

    return [ordered]@{
        line_count = $lineNumber
        matched = $matched
        match_count = $matchCount
        pass_or_skip_match_count = $passOrSkipMatchCount
        regex_timeout_count = $regexTimeoutCount
        failed_test_detected = $failedTestLineNumbers.Count -gt 0
        failed_test_line_numbers = @($failedTestLineNumbers | Select-Object -First 20)
        signature_associated_with_failed_test = $signatureAssociatedWithFailedTest
        matched_lines = @($matchedLines)
    }
}

$candidatePath = (Resolve-Path -LiteralPath $CandidateFile).Path
$candidateSchemaPath = (Resolve-Path -LiteralPath $CandidateSchemaFile).Path
$receiptSchemaPath = (Resolve-Path -LiteralPath $ReceiptSchemaFile).Path
$evidenceRootPath = (Resolve-Path -LiteralPath $EvidenceRoot).Path
$repositoryRootPath = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$outputPath = [System.IO.Path]::GetFullPath($OutputFile)

if ($outputPath -in @($candidatePath, $candidateSchemaPath, $receiptSchemaPath))
{
    throw "OutputFile must not overwrite an evaluator input."
}

if ([System.IO.File]::Exists($outputPath))
{
    [System.IO.File]::Delete($outputPath)
}

$candidateJson = [System.IO.File]::ReadAllText($candidatePath)
if (-not ($candidateJson | Test-Json -SchemaFile $candidateSchemaPath))
{
    throw "Candidate JSON does not satisfy the versioned schema."
}

$candidateDocument = [System.Text.Json.JsonDocument]::Parse($candidateJson)
try
{
    $checkedUtcText = $candidateDocument.RootElement.GetProperty("duplicate_check").GetProperty("checked_utc").GetString()
}
finally
{
    $candidateDocument.Dispose()
}

$candidate = $candidateJson | ConvertFrom-Json -Depth 32
$repositoryHead = (& git -C $repositoryRootPath rev-parse HEAD 2>$null).Trim()
if ($LASTEXITCODE -ne 0 -or $repositoryHead -notmatch "^[0-9a-f]{40}$")
{
    throw "RepositoryRoot is not a readable Git repository."
}

if (-not $repositoryHead.Equals([string]$candidate.repository_ref.commit_sha, [System.StringComparison]::Ordinal))
{
    throw "Candidate repository commit does not match the checked-out repository."
}

$expectedIssueUrl = "https://github.com/dotnet/aspnetcore/issues/$($candidate.issue.number)"
if (-not $expectedIssueUrl.Equals([string]$candidate.issue.url, [System.StringComparison]::Ordinal))
{
    throw "Candidate issue URL does not match the issue number."
}

$candidateSha256 = (Get-FileHash -LiteralPath $candidatePath -Algorithm SHA256).Hash.ToLowerInvariant()
$candidateSchemaSha256 = (Get-FileHash -LiteralPath $candidateSchemaPath -Algorithm SHA256).Hash.ToLowerInvariant()
$receiptSchemaSha256 = (Get-FileHash -LiteralPath $receiptSchemaPath -Algorithm SHA256).Hash.ToLowerInvariant()
$kind = [string]$candidate.signature.kind
$values = @($candidate.signature.values | ForEach-Object { [string]$_ })
$qualityFailures = [System.Collections.Generic.List[string]]::new()
$incompleteReasons = [System.Collections.Generic.List[string]]::new()

$testName = [string]$candidate.test.fully_qualified_name
$testLeafName = ($testName -split "\.")[-1]
if ($kind -eq "ErrorMessage")
{
    foreach ($value in $values)
    {
        $literalWithoutResultPrefix = $value -replace "^\s*\[(FAIL|PASS|SKIP)\]\s*", ""
        if ($testName.IndexOf($literalWithoutResultPrefix, [System.StringComparison]::Ordinal) -ge 0 -or
            $testLeafName.IndexOf($literalWithoutResultPrefix, [System.StringComparison]::Ordinal) -ge 0)
        {
            $qualityFailures.Add("The literal signature contains only the test identifier or a fragment of it.")
        }
    }
}

$duplicateStatus = [string]$candidate.duplicate_check.status
$proposedClassification = [string]$candidate.proposed_classification
if ($duplicateStatus -eq "none" -and $proposedClassification -eq "reuse-existing-kbe")
{
    $qualityFailures.Add("The proposed classification requires an existing KBE, but the duplicate check reports none.")
}

$issueReferences = @($candidate.duplicate_check.references | Where-Object { $_ -match "^issue:[1-9][0-9]*$" })
$pullRequestReferences = @($candidate.duplicate_check.references | Where-Object { $_ -match "^pull-request:[1-9][0-9]*$" })
if ($duplicateStatus -eq "existing-kbe" -and $issueReferences.Count -eq 0)
{
    $qualityFailures.Add("The duplicate check reports an existing KBE without an issue reference.")
}

if ($duplicateStatus -eq "existing-fix-pr" -and $pullRequestReferences.Count -eq 0)
{
    $qualityFailures.Add("The duplicate check reports an existing fix PR without a pull request reference.")
}

$checkedUtc = [System.DateTimeOffset]::MinValue
if (-not [System.DateTimeOffset]::TryParse(
        $checkedUtcText,
        [System.Globalization.CultureInfo]::InvariantCulture,
        [System.Globalization.DateTimeStyles]::None,
        [ref]$checkedUtc))
{
    throw "Duplicate check timestamp is invalid."
}

$now = [System.DateTimeOffset]::UtcNow
$checkedUtc = $checkedUtc.ToUniversalTime()
if ($checkedUtc -gt $now.AddMinutes(5))
{
    $qualityFailures.Add("The duplicate check timestamp is in the future.")
}
elseif ($checkedUtc -lt $now.AddHours(-24))
{
    $incompleteReasons.Add("The duplicate check is older than 24 hours.")
}

$regexes = @()
if ($kind -eq "ErrorPattern")
{
    $regexOptions = [System.Text.RegularExpressions.RegexOptions]::Singleline -bor
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor
        [System.Text.RegularExpressions.RegexOptions]::NonBacktracking
    $regexTimeout = [System.TimeSpan]::FromMilliseconds(50)

    foreach ($value in $values)
    {
        if ($value -in @(".*", ".+", "^.*$", "^.+$"))
        {
            $qualityFailures.Add("The regex signature is unbounded and matches arbitrary text.")
        }

        try
        {
            $regexes += [System.Text.RegularExpressions.Regex]::new($value, $regexOptions, $regexTimeout)
        }
        catch
        {
            $qualityFailures.Add("The regex is not compatible with Build Analysis matching: $($_.Exception.Message)")
        }
    }

    foreach ($regex in $regexes)
    {
        $bareTestProbes = @(
            $testName,
            $testLeafName,
            "[FAIL] $testName",
            "[PASS] $testName",
            "[SKIP] $testName"
        )
        foreach ($probe in $bareTestProbes)
        {
            try
            {
                if ($regex.IsMatch($probe))
                {
                    $qualityFailures.Add("The regex signature matches the test identifier or a fragment of it.")
                    break
                }
            }
            catch [System.Text.RegularExpressions.RegexMatchTimeoutException]
            {
                $qualityFailures.Add("The regex exceeded the Build Analysis timeout while checking signature specificity.")
                break
            }
        }
    }
}

$logResults = [System.Collections.Generic.List[object]]::new()
$failureLogCount = 0
$negativeLogCount = 0
$failureLogsMatched = 0
$negativeCollisionCount = 0
$passOrSkipCollisionCount = 0
$failureHashes = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$negativeHashes = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$failureBuildIds = [System.Collections.Generic.HashSet[int]]::new()
$negativeBuildIds = [System.Collections.Generic.HashSet[int]]::new()
$seenLogIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$seenLogPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

foreach ($log in $candidate.evidence.raw_logs)
{
    if (-not $seenLogIds.Add([string]$log.id))
    {
        throw "Duplicate evidence id: $($log.id)."
    }

    if (-not $seenLogPaths.Add([string]$log.path))
    {
        throw "Duplicate evidence path: $($log.path)."
    }

    $resolvedLogPath = Resolve-EvidencePath -Root $evidenceRootPath -RelativePath ([string]$log.path)
    $actualHash = (Get-FileHash -LiteralPath $resolvedLogPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if (-not $actualHash.Equals([string]$log.sha256, [System.StringComparison]::Ordinal))
    {
        throw "Evidence hash mismatch for $($log.id)."
    }

    $match = if ($regexes.Count -eq $values.Count -or $kind -eq "ErrorMessage")
    {
        Test-Log `
            -Path $resolvedLogPath `
            -Kind $kind `
            -Values $values `
            -TestName ([string]$candidate.test.fully_qualified_name) `
            -FailureAssociationWindowLines $failureAssociationWindowLines `
            -Regexes $regexes
    }
    else
    {
        [ordered]@{
            line_count = @([System.IO.File]::ReadLines($resolvedLogPath)).Count
            matched = $false
            match_count = 0
            pass_or_skip_match_count = 0
            regex_timeout_count = 0
            failed_test_detected = $false
            failed_test_line_numbers = @()
            signature_associated_with_failed_test = $false
            matched_lines = @()
        }
    }

    if ($log.role -eq "failure")
    {
        $failureLogCount++
        $null = $failureHashes.Add($actualHash)
        $null = $failureBuildIds.Add([int]$log.build.id)
        if ($match.matched)
        {
            $failureLogsMatched++
        }

        if (-not $match.failed_test_detected)
        {
            $qualityFailures.Add("Failure log '$($log.id)' does not contain a supported failed-test marker for the declared test.")
        }
        elseif ($match.matched -and -not $match.signature_associated_with_failed_test)
        {
            $qualityFailures.Add("The signature match in failure log '$($log.id)' is not within $failureAssociationWindowLines lines of the declared test failure.")
        }
    }
    else
    {
        $negativeLogCount++
        if ([string]$log.outcome -eq "passed")
        {
            $null = $negativeHashes.Add($actualHash)
            $null = $negativeBuildIds.Add([int]$log.build.id)
        }
        if ($match.matched)
        {
            $negativeCollisionCount++
        }
    }

    if ([string]$log.build.platform -eq "unknown")
    {
        $incompleteReasons.Add("Evidence log '$($log.id)' has unknown platform; exact environment dimensions are required.")
    }
    if ([string]$log.build.configuration -eq "unknown")
    {
        $incompleteReasons.Add("Evidence log '$($log.id)' has unknown configuration; exact environment dimensions are required.")
    }

    $passOrSkipCollisionCount += [int]$match.pass_or_skip_match_count

    $logResults.Add([ordered]@{
        id = [string]$log.id
        role = [string]$log.role
        outcome = [string]$log.outcome
        path = [string]$log.path
        source_url = [string]$log.source_url
        sha256 = $actualHash
        build = $log.build
        line_count = [int]$match.line_count
        matched = [bool]$match.matched
        match_count = [int]$match.match_count
        pass_or_skip_match_count = [int]$match.pass_or_skip_match_count
        regex_timeout_count = [int]$match.regex_timeout_count
        failed_test_detected = [bool]$match.failed_test_detected
        failed_test_line_numbers = @($match.failed_test_line_numbers)
        signature_associated_with_failed_test = [bool]$match.signature_associated_with_failed_test
        matched_lines = @($match.matched_lines)
    })
}

$requiredFailureLogs = [System.Math]::Max(
    $minimumFailureEvidenceFloor,
    [int]$candidate.policy.minimum_failure_logs)
$requiredNegativeLogs = [System.Math]::Max(
    $minimumNegativeEvidenceFloor,
    [int]$candidate.policy.minimum_negative_logs)

if ($failureHashes.Count -lt $requiredFailureLogs)
{
    $incompleteReasons.Add("Only $($failureHashes.Count) distinct failure log(s) were supplied; $requiredFailureLogs are required.")
}

if ($failureBuildIds.Count -lt $requiredFailureLogs)
{
    $incompleteReasons.Add("Only $($failureBuildIds.Count) distinct failure build(s) were supplied; $requiredFailureLogs are required.")
}

if ($negativeHashes.Count -lt $requiredNegativeLogs)
{
    $incompleteReasons.Add("Only $($negativeHashes.Count) distinct authoritative Passed log(s) were supplied; $requiredNegativeLogs are required.")
}

if ($negativeBuildIds.Count -lt $requiredNegativeLogs)
{
    $incompleteReasons.Add("Only $($negativeBuildIds.Count) distinct authoritative Passed build(s) were supplied; $requiredNegativeLogs are required.")
}

if (@($failureHashes | Where-Object { $negativeHashes.Contains($_) }).Count -gt 0)
{
    $qualityFailures.Add("The same log content was supplied as both failure and negative evidence.")
}

if ($failureLogsMatched -ne $failureLogCount)
{
    $qualityFailures.Add("The signature did not match every supplied failure log.")
}

if ($negativeCollisionCount -gt 0)
{
    $qualityFailures.Add("The signature matched $negativeCollisionCount negative log(s).")
}

if ($passOrSkipCollisionCount -gt 0)
{
    $qualityFailures.Add("The signature matched $passOrSkipCollisionCount pass or skip line(s).")
}

$totalRegexTimeoutCount = 0
foreach ($logResult in $logResults)
{
    $totalRegexTimeoutCount += [int]$logResult.regex_timeout_count
}

if ($totalRegexTimeoutCount -gt 0)
{
    $qualityFailures.Add("The regex exceeded the Build Analysis timeout on at least one line.")
}

$coverage = $candidate.duplicate_check.coverage
$requiredDuplicateCategories = @(
    "open-kbe",
    "recently-closed-kbe",
    "open-fix-pr",
    "recently-merged-fix-pr"
)
$completeDuplicateCategories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($query in $candidate.duplicate_check.queries)
{
    if ([bool]$query.complete)
    {
        $null = $completeDuplicateCategories.Add([string]$query.category)
    }
}

$kbeQueryNumbers = @(
    $candidate.duplicate_check.queries |
        Where-Object { $_.category -in @("open-kbe", "recently-closed-kbe") } |
        ForEach-Object { $_.result_numbers }
)
$fixPrQueryNumbers = @(
    $candidate.duplicate_check.queries |
        Where-Object { $_.category -in @("open-fix-pr", "recently-merged-fix-pr") } |
        ForEach-Object { $_.result_numbers }
)

if ($duplicateStatus -eq "existing-kbe")
{
    $referencedKbeNumbers = @($issueReferences | ForEach-Object { [int]($_ -replace "^issue:", "") })
    if (@($referencedKbeNumbers | Where-Object { $_ -in $kbeQueryNumbers }).Count -eq 0)
    {
        $qualityFailures.Add("The existing KBE reference is absent from the recorded KBE query results.")
    }
}

if ($duplicateStatus -eq "existing-fix-pr")
{
    $referencedFixPrNumbers = @($pullRequestReferences | ForEach-Object { [int]($_ -replace "^pull-request:", "") })
    if (@($referencedFixPrNumbers | Where-Object { $_ -in $fixPrQueryNumbers }).Count -eq 0)
    {
        $qualityFailures.Add("The existing fix PR reference is absent from the recorded PR query results.")
    }
}

if ($duplicateStatus -eq "none" -and
    ($candidate.duplicate_check.references.Count -gt 0 -or
        $kbeQueryNumbers.Count -gt 0 -or
        $fixPrQueryNumbers.Count -gt 0))
{
    $qualityFailures.Add("The duplicate check reports no duplicate despite recorded issue or pull request results.")
}

$duplicateCoverageComplete =
    [bool]$coverage.open_kbes -and
    [bool]$coverage.recently_closed_kbes -and
    [bool]$coverage.open_fix_prs -and
    [bool]$coverage.recently_merged_fix_prs -and
    (@($candidate.duplicate_check.queries | Where-Object { -not $_.complete }).Count -eq 0) -and
    (@($requiredDuplicateCategories | Where-Object { -not $completeDuplicateCategories.Contains($_) }).Count -eq 0)

if (-not $duplicateCoverageComplete -or $candidate.duplicate_check.status -eq "not-evaluated")
{
    $incompleteReasons.Add("Duplicate KBE and fix PR coverage is incomplete.")
}

$deterministicStatus = if ($qualityFailures.Count -gt 0)
{
    "rejected"
}
elseif ($incompleteReasons.Count -gt 0)
{
    "incomplete"
}
else
{
    "validated"
}

$shadowRecommendation = "human-review"
if ($deterministicStatus -eq "incomplete")
{
    $shadowRecommendation = "insufficient-evidence"
}
elseif ($deterministicStatus -eq "validated")
{
    $shadowRecommendation = switch ($duplicateStatus)
    {
        "existing-kbe" { "reuse-existing-kbe"; break }
        "existing-fix-pr" { "existing-fix-pr"; break }
        "ambiguous" { "human-review"; break }
        "integrity-filtered" { "human-review"; break }
        default { $proposedClassification }
    }
}

$eligibleForKbeEnrichment = $false

$reasons = @($qualityFailures) + @($incompleteReasons)
if ($reasons.Count -eq 0)
{
    $reasons = @("The candidate passed deterministic signature, evidence, and duplicate-coverage gates.")
}

$receipt = [ordered]@{
    schema_version = 1
    repository = "dotnet/aspnetcore"
    repository_ref = $candidate.repository_ref
    generated_utc = [System.DateTimeOffset]::UtcNow.ToString("O")
    evaluator = [ordered]@{
        name = "Evaluate-TestQuarantineKbeCandidate.ps1"
        version = 1
        matcher = "Build Analysis compatible signature matcher with failed-test association"
        failure_association_window_lines = $failureAssociationWindowLines
        candidate_sha256 = $candidateSha256
        candidate_schema_sha256 = $candidateSchemaSha256
        receipt_schema_sha256 = $receiptSchemaSha256
    }
    issue = $candidate.issue
    test = $candidate.test
    signature = $candidate.signature
    policy = $candidate.policy
    evidence = [ordered]@{
        failure_log_count = $failureLogCount
        negative_log_count = $negativeLogCount
        distinct_failure_log_count = $failureHashes.Count
        distinct_negative_log_count = $negativeHashes.Count
        distinct_failure_build_count = $failureBuildIds.Count
        distinct_negative_build_count = $negativeBuildIds.Count
        all_failure_logs_matched = $failureLogCount -gt 0 -and $failureLogsMatched -eq $failureLogCount
        negative_collision_count = $negativeCollisionCount
        pass_or_skip_collision_count = $passOrSkipCollisionCount
        logs = @($logResults)
        corroborating_context = @($candidate.evidence.corroborating_context)
    }
    duplicate_check = $candidate.duplicate_check
    agent_proposed_classification = $proposedClassification
    deterministic_status = $deterministicStatus
    shadow_recommendation = $shadowRecommendation
    eligible_for_kbe_enrichment = $eligibleForKbeEnrichment
    evidence_provenance_verified = $false
    human_review_required = $true
    zero_remote_writes = $true
    reasons = @($reasons | Select-Object -Unique)
}

$receiptJson = $receipt | ConvertTo-Json -Depth 32
if (-not ($receiptJson | Test-Json -SchemaFile $receiptSchemaPath))
{
    throw "Generated receipt does not satisfy the versioned schema."
}

$outputDirectory = Split-Path -Parent $outputPath
if ($outputDirectory)
{
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}

[System.IO.File]::WriteAllText($outputPath, $receiptJson + [System.Environment]::NewLine)
Write-Host "Wrote $deterministicStatus shadow receipt to $outputPath"
