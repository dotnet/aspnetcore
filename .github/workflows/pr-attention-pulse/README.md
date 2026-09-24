# PR Attention Pulse snapshot consumer contract

The canonical [Pulse issue](https://github.com/dotnet/aspnetcore/issues/69328)
publishes a report and the identity of the exact JSON file used to render it.
Consumers can reuse those results without collecting or classifying PRs again.
The snapshot is **capped report results, not a full-queue export**. A filter over
its rows can find only displayed candidates; no matching row does not establish
that no matching PR exists.

The existing `pulse-publication-evidence` Actions artifact contains exactly:

- `pulse-input.json`: the combined, sanitized JSON, including its original bytes.
- `pulse-body.md`: the canonical published Markdown body.

There is no additional exported context file or new JSON schema. Private
preparation/validation context is an implementation detail, not a consumer
dependency. The uploader does not explicitly overwrite earlier evidence
(`overwrite: false`, including the action's default). This does **not** guarantee
that GitHub retains earlier-attempt artifacts after an all-job rerun.

## JSON envelope and scope

The exported root has `schemaVersion: "2.0.0"`, `status`, and an ordered `areas`
array with exactly these two entries:

| Index | `id` | `label` | `openByDefault` | Complete area's resolved filter |
| --- | --- | --- | --- | --- |
| 0 | `blazor` | `Blazor` | `false` | `name: "blazor"`, `coverage: "labels-and-paths"`, `allRepositoryPullRequests: false` |
| 1 | `repository-wide` | `Repository-wide` | `false` | `name: "adhoc"`, `coverage: "all-repo"`, `allRepositoryPullRequests: true` |

Blazor selects the union of the maintained Blazor labels and Components paths.
A path-only match must meet the producer's minimum changed-file share; incidental
path matches are counted separately. The repository-wide query covers all open
PRs. These are **overlapping, independently collected scopes**, not disjoint
partitions or simultaneous queries. A PR can appear in both areas with different
rank or evidence. Do not sum their census counts, deduplicate away one area's
assessment, or infer a cross-area ranking.

Each area has `id`, `label`, `openByDefault`, `status`, `attemptedAt`,
`candidateCountsAvailable`, and `source`:

| Area status | Meaning and fields |
| --- | --- |
| `complete` | `candidateCountsAvailable: true`; `source.schemaVersion: "1.0.0"`; all five `views` arrays exist, possibly empty. The open-PR inventory query was complete, not necessarily the discussion evidence. |
| `unavailable` | `candidateCountsAvailable: false`; `errorCategory` explains the failure; `source` contains `repository: "dotnet/aspnetcore"`. **No `views`, census, caps, or candidate counts are supplied.** Do not manufacture them as zeros. |

Root `status` is `complete` when both areas are complete, `partial` when exactly
one is complete, and `unavailable` when neither is complete. In a partial result,
either Blazor or repository-wide can be unavailable; preserve the other area's
data and report the missing area explicitly.

`errorCategory` currently includes `collection-failed`,
`collection-output-missing`, `malformed-json`, `incompatible-schema`,
`unexpected-repository`, `incomplete-query`, `invalid-contract`, and
`sanitized-output-too-large`. These describe unavailable collection, not an
empty queue. Conversely, a complete-zero area has real zero counts and empty
view arrays with `candidateCountsAvailable: true`. Empty displayed views alone
need not mean a zero census: caps, exclusions, and unassessed evidence can keep
nonzero inventory out of the display.

### Complete-area source metadata

| Field in `source` | Meaning |
| --- | --- |
| `repository`, `schemaVersion` | Fixed upstream repository and the underlying queue contract (`1.0.0`), not the combined envelope's version. |
| `generatedAt` | That area's producer snapshot time, normalized to UTC. It is not the completion time of both queries or publication time. |
| `query` | `openPullRequestCount`, `returnedPullRequestCount`, and `complete: true`. The two counts agree for accepted inventory. |
| `filter` | `name`, `description`, `coverage`, `selection`, `allRepositoryPullRequests`; the resolved scope, not a request to run another query. |
| `census` | `openPullRequests`, `matched`, `labelOnly`, `pathOnly`, `labelAndPath`, `incidentalPathExcluded`, `unresolvedMergeable`, and `byBucket`. |
| `caps` | `reviewNow`, `reviewNowPerAuthor`, `needsRescue`, `readyToMerge`: configured display limits, not promised row counts. |
| `overflow` | `reviewNow`, `needsRescue`, `readyToMerge`: legacy inventory-minus-displayed counts for those buckets. They can include verification-needed, unassessed, or excluded candidates; they are not another list of PRs. |
| `discussion` | `candidateLimit`, `assessedCandidateCount`, `verificationNeededCount`, `unassessedReviewNowCount`: bounded review-candidate assessment coverage. |
| `mergeDiscussion` | Independent merge assessment `candidateLimit`, `assessedCandidateCount`, `eligibleCount`, `verificationNeededCount`, `unassessedCandidateCount`, `excludedCandidateCount`, and display `verificationLimit`. |
| `warnings` | Ordered display strings describing limitations; keep them visible rather than replacing them with a success indicator. |

`census.byBucket` contains `ReviewNow`, `NeedsRescue`, `ReadyToMerge`,
`WaitingOnAuthor`, `WaitingOnCI`, `DesignDecision`, `Draft`, and `Excluded`.
These are inventory classifications. In particular, `ReviewNow` census is not
the number cleared for unattended review, and `ReadyToMerge` census is not the
number with established merge eligibility. Merge assessed count equals eligible
plus verification-needed counts; assessed, unassessed, and excluded counts
together account for the prospective merge inventory.

The snapshot omits the producer's full `items` universe, raw discussion text,
community/personal inboxes, and other nonprojected data. Census, overflow, and
caps do not make the omitted PR records recoverable.

### Views, ranks, and items

Render the five views in this order, retaining their array order and one-based,
contiguous `rank`. Rank is **local to each view in each area**, not a score.
The sanitizer maps the producer's `digestRank`, `discussionVerificationRank`,
or `mergeVerificationRank` to this common field. Do not re-rank by PR number,
age, title, or array order in some other source.

| View | Bucket | Interpretation and bound |
| --- | --- | --- |
| `reviewNow` | `ReviewNow` | Selected ordinary-review candidates, subject to `caps.reviewNow`, `caps.reviewNowPerAuthor`, exclusions, and assessed discussion. |
| `verifyDiscussionBeforeReview` | `ReviewNow` | Assessed candidates needing interpretation before ordinary review. The producer has a separate display cap; `discussion.candidateLimit` is an assessment budget, **not** a promise of that many displayed rows. |
| `needsRescue` | `NeedsRescue` | Ownership/triage/close-or-revive work, capped by `caps.needsRescue`; do not promote these to ordinary review. |
| `readyToMerge` | `ReadyToMerge` | Only `mergeEligibility: "eligible"` candidates, capped by `caps.readyToMerge`. |
| `verifyDiscussionBeforeMerge` | `ReadyToMerge` | `verification-needed` or `not-assessed` candidates, capped independently by `mergeDiscussion.verificationLimit`. Not a merge recommendation or proof of an author blocker. |

Every displayed item contains:

- `number`: positive PR number in `dotnet/aspnetcore`; construct its link as
  `https://github.com/dotnet/aspnetcore/pull/<number>`.
- `title`, `author`, `bucket`, `rank`, and `nextActor`.
- `reasonCodes`: ordered stable lowercase codes, such as `needs-first-review`,
  `review-requested`, `roundtrip-waiting`, `never-reviewed`, `approved`, or
  `ci-green`. Preserve codes; do not infer readiness from a single code.
- `blockers`: ordered sanitized display strings, possibly empty. Uncertainty in
  discussion is not automatically an author blocker.
- `ageDays`, `idleDays`: nonnegative integer ages at collection.
- `scopeMatch`: `label-only`, `path-only`, or `label-and-path` in Blazor;
  `all-repo` in repository-wide.

Titles are **sanitized display data, not lossless originals**. Normalization,
character replacement, removal of links/references, whitespace collapse, and
length limits can change them. Do not use titles as PR identity or executable
content. PR number and repository identify a PR. Other display strings also
have bounded sanitization; the snapshot is not a raw GitHub response.

The verification-before-review view and both merge views include
`discussionAssessment`:

- `state`, `complete`, and ordered stable `signals`.
- `commentTotalCount` and `commentEvidenceTruncated`.
- `threads.totalCount`, `returnedCount`, `complete`, `unresolvedCount`, and
  `outdatedUnresolvedCount`.

Current emitted states are `clear`, `verification-needed`, and `not-assessed`.
The review sanitizer also accepts the legacy `actionable` state; do not invent
it or treat a state without its other evidence as a fresh assessment. Signals
include `discussion-incomplete`, `discussion-not-assessed`,
`current-inline-discussion-unassessed`, `review-evidence-incomplete`, and
non-author/disposition feedback codes.

Inventory completeness does not establish discussion completeness. The producer
uses bounded candidate budgets and recent comments/threads, not a full crawl.
Current unresolved inline threads require interpretation; their text/authors
are not exported. Unassessed zero comment/thread counts are placeholders, not
proof that no discussion exists. Preserve incomplete, truncated, unassessed,
and warning states even when root `status` is `complete`.

`commentEvidenceTruncated` also covers the producer's ten-excerpt display cap.
It can be `true` while `complete` is `true` and merge eligibility is `eligible`
when the collected bounded evidence was complete (for example, eleven
comments). Do not equate that flag alone with a failed inventory query or erase
it because the assessment is clear. There are no comment excerpts in Pulse.

Both merge views include `mergeEligibility`. An eligible row requires complete,
clear bounded evidence and no current unresolved thread; outdated-only
unresolved threads can remain. Eligibility is a selection gate, not permission
to merge or a guarantee about later changes. Older producer data lacking the
entire merge extension is conservatively projected into verification as
`not-assessed`, never promoted merely because its bucket is `ReadyToMerge`.
Consumers should not infer eligibility from a missing or partial extension.

### Times and compatible changes

The issue's **Snapshot generated** time is frozen by trusted preparation after
the combined file is finished. It differs from each area's `source.generatedAt`
and from publication time. It does not claim simultaneous source queries.

Legacy combined `attemptedAt` can be a culture-formatted string without a UTC
offset, even though the intermediate area sanitizer emitted UTC. Preserve it
as legacy display metadata. Do not repair the export or use locale-dependent
parsing of `attemptedAt` for snapshot identity or freshness. Use the exact
published snapshot tuple for identity and the explicit UTC source/snapshot
times for their respective timing meanings.

Validate the combined schema `2.0.0` and each complete area's source schema
`1.0.0`. Tolerate compatible additive fields within supported schemas, retaining
known fields and their meanings. Unsupported versions, malformed JSON, missing
required fields, or inconsistent statuses are explicit errors, **never empty
results**. A consumer rendering items must also honor the item/evidence contracts
above; a version check alone is not full validation.

## Retrieve the published snapshot

**Authority is the captured published issue body, not the newest run, newest
artifact, or an overall workflow conclusion.** A producing artifact can exist
even when later publication is blocked. A retry of downstream publication jobs
can publish bytes from an earlier producing attempt. Always use the producing
run/attempt tuple printed in the report.

The report ends with this form (the checksum below is a placeholder):

```markdown
## Snapshot

Snapshot generated: `2026-09-23T19:30:00.0000000Z`. [Producing workflow run](https://github.com/dotnet/aspnetcore/actions/runs/34643961191/attempts/1).
Artifact: `pulse-publication-evidence`; files: `pulse-input.json`, `pulse-body.md`.
Capped report results, not the full queue. Authenticated ZIP artifact; retained for seven days.

<details>
<summary>Snapshot identity</summary>

Repository: `dotnet/aspnetcore`; run ID: `34643961191`; attempt: `1`.
JSON SHA-256: `<64 lower-case hex hash of exact file bytes>`.

</details>
```

Repository/server are fixed to `dotnet/aspnetcore` and `https://github.com`.
Run ID and attempt are positive ASCII decimal **strings**, not 32-bit integers.
The generation timestamp is strict UTC with seven fractional digits and `Z`.
The exact names and URL are part of the identity, not configurable alternatives.

1. Read and capture the issue's whole `body` string once. Decode the API's JSON
   string, but do not trim, reformat, normalize newlines, or reconstruct Markdown.
2. Validate its snapshot suffix and tuple. Optionally cross-check the specific
   attempt through
   `GET /repos/dotnet/aspnetcore/actions/runs/<run-id>/attempts/<attempt>`.
   Do not demand overall success or compare against the latest attempt instead.
3. Enumerate **all pages** of
   `GET /repos/dotnet/aspnetcore/actions/runs/<run-id>/artifacts`, selecting the
   exact name `pulse-publication-evidence`. An artifact record has **no attempt
   property**. Name, creation time, or list position cannot select the attempt.
4. Download each available candidate by artifact ID with
   `GET /repos/dotnet/aspnetcore/actions/artifacts/<artifact-id>/zip`. Read the
   two expected files. Require `pulse-body.md` to equal the captured body using
   ordinal string comparison. This also matches repository, run, attempt,
   generation time, names, and producing-run URL in the body.
5. Hash the **exact `pulse-input.json` bytes before parsing**, including encoding,
   whitespace, and trailing newline. Compare lowercase SHA-256 to the published
   checksum. Do not hash the ZIP or a reserialized object. A wrong-attempt body
   can accompany identical JSON bytes and therefore the same checksum.
6. Require exactly one matching candidate. Reject zero matches and ambiguous
   multiple exact matches. An unreadable candidate or incomplete artifact listing
   prevents establishing uniqueness; report that failure rather than guessing.
   Then parse and validate the supported JSON envelope. Keep unavailable/partial
   data states separate from retrieval failures.

Downloads require **GitHub authentication and appropriate repository/Actions
read access** (for example, Actions read access for a fine-grained token, plus
access to read the issue). These are consumer credentials; there is no need to
broaden workflow permissions. All API operations below are GETs.

The artifact is a ZIP with seven-day retention, not a permanent public JSON URL.
The download endpoint redirects to a short-lived download URL; do not store that
URL as the snapshot identity. Authentication/authorization failures, deletion,
expiration, or reruns can make the referenced snapshot unavailable, potentially
**before seven days**. Non-overwriting upload behavior does not prevent GitHub's
own rerun lifecycle from removing prior artifacts. Never silently select newer
data or report an empty queue when the published artifact is unavailable.

### PowerShell retrieval example

This named example uses only PowerShell 7/.NET, not the repository's producer
module. Its two injectable transports perform read-only JSON GETs and ZIP GETs.
The example checks identity, exact body/bytes, uniqueness, schema versions,
ordered area/status metadata, query completeness, and the five view arrays.
It deliberately does not reimplement the producer's classifier or every
item-level validation rule.

<!-- pulse-snapshot-retrieval -->
```powershell
function Get-PublishedPulseSnapshot
{
    param(
        [Parameter(Mandatory)][string]$PublishedBody,
        [Parameter(Mandatory)][scriptblock]$ReadJson,
        [Parameter(Mandatory)][scriptblock]$ReadZip
    )

    $ErrorActionPreference = "Stop"
    $pattern = '(?m)^## Snapshot\n\n' +
        'Snapshot generated: `(?<time>[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{7}Z)`\. ' +
        '\[Producing workflow run\]\(https://github\.com/dotnet/aspnetcore/actions/runs/(?<run>[1-9][0-9]*)/attempts/(?<attempt>[1-9][0-9]*)\)\.\n' +
        'Artifact: `pulse-publication-evidence`; files: `pulse-input\.json`, `pulse-body\.md`\.\n' +
        'Capped report results, not the full queue\. Authenticated ZIP artifact; retained for seven days\.\n\n' +
        '<details>\n<summary>Snapshot identity</summary>\n\n' +
        'Repository: `dotnet/aspnetcore`; run ID: `\k<run>`; attempt: `\k<attempt>`\.\n' +
        'JSON SHA-256: `(?<hash>[0-9a-f]{64})`\.\n\n</details>\n?\z'
    $identity = [regex]::Match($PublishedBody, $pattern)
    $time = [datetimeoffset]::MinValue
    if (-not $identity.Success -or
        [regex]::Matches($PublishedBody, '(?m)^## Snapshot$').Count -ne 1 -or
        -not [datetimeoffset]::TryParseExact(
            $identity.Groups["time"].Value, "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AssumeUniversal, [ref]$time))
    {
        throw "Invalid published snapshot identity."
    }

    $runId = $identity.Groups["run"].Value
    $attempt = $identity.Groups["attempt"].Value
    $repositoryPath = "repos/dotnet/aspnetcore"
    try
    {
        $run = & $ReadJson "$repositoryPath/actions/runs/$runId/attempts/$attempt"
    }
    catch
    {
        throw "Producing attempt read failed: $($_.Exception.Message)"
    }
    if ([string]$run.id -cne $runId -or [string]$run.run_attempt -cne $attempt -or
        $run.repository.full_name -cne "dotnet/aspnetcore")
    {
        throw "Producing attempt identity mismatch."
    }

    $artifacts = [Collections.Generic.List[object]]::new()
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $total = $null
    $page = 1
    do
    {
        try
        {
            $response = & $ReadJson "$repositoryPath/actions/runs/$runId/artifacts?per_page=100&page=$page"
        }
        catch
        {
            throw "Artifact listing read failed: $($_.Exception.Message)"
        }
        if ($response.artifacts -isnot [array] -or
            ($response.total_count -isnot [long] -and $response.total_count -isnot [int]) -or
            $response.total_count -lt 0)
        {
            throw "Invalid artifact listing."
        }
        if ($null -eq $total)
        {
            $total = $response.total_count
        }
        if ($response.total_count -ne $total -or
            ($response.artifacts.Count -eq 0 -and $artifacts.Count -lt $total))
        {
            throw "Artifact listing changed or is incomplete; retry the captured snapshot."
        }
        foreach ($artifact in $response.artifacts)
        {
            $id = [string]$artifact.id
            if ($id -cnotmatch '^[1-9][0-9]*$' -or -not $ids.Add($id))
            {
                throw "Invalid or repeated artifact ID in listing."
            }
            $artifacts.Add($artifact)
        }
        if ($artifacts.Count -gt $total)
        {
            throw "Artifact listing changed or is incomplete; retry the captured snapshot."
        }
        $page++
    } while ($artifacts.Count -lt $total)

    $candidates = @($artifacts | Where-Object {
        [string]::Equals($_.name, "pulse-publication-evidence", [StringComparison]::Ordinal)
    })
    if ($candidates.Count -eq 0)
    {
        throw "Snapshot unavailable: publication artifact is missing."
    }
    $matching = [Collections.Generic.List[object]]::new()
    $rejections = [Collections.Generic.List[string]]::new()
    $utf8 = [Text.UTF8Encoding]::new($false, $true)
    foreach ($artifact in $candidates)
    {
        if ($artifact.expired -isnot [bool])
        {
            throw "Invalid artifact expiry metadata."
        }
        if ($artifact.expired)
        {
            $rejections.Add("$($artifact.id): expired")
            continue
        }
        if ([string]$artifact.workflow_run.id -cne $runId)
        {
            $rejections.Add("$($artifact.id): artifact run identity mismatch")
            continue
        }
        try
        {
            [byte[]]$zipBytes = & $ReadZip "$repositoryPath/actions/artifacts/$($artifact.id)/zip"
        }
        catch
        {
            throw "Artifact $($artifact.id) download/read failed; cannot establish uniqueness: $($_.Exception.Message)"
        }

        $buffer = $null
        $archive = $null
        try
        {
            $buffer = [IO.MemoryStream]::new($zipBytes, $false)
            $archive = [IO.Compression.ZipArchive]::new($buffer, [IO.Compression.ZipArchiveMode]::Read)
            $files = @{}
            foreach ($name in @("pulse-input.json", "pulse-body.md"))
            {
                $entries = @($archive.Entries | Where-Object {
                    [string]::Equals($_.FullName, $name, [StringComparison]::Ordinal)
                })
                if ($archive.Entries.Count -ne 2 -or $entries.Count -ne 1)
                {
                    throw "ZIP must contain the two exact expected files, once each."
                }
                $stream = $entries[0].Open()
                $content = [IO.MemoryStream]::new()
                try
                {
                    $stream.CopyTo($content)
                    $files[$name] = $content.ToArray()
                }
                finally
                {
                    $stream.Dispose()
                    $content.Dispose()
                }
            }
            $body = $utf8.GetString($files["pulse-body.md"])
            if (-not [string]::Equals($body, $PublishedBody, [StringComparison]::Ordinal))
            {
                throw "Published body/identity mismatch."
            }
            $sha256 = [Security.Cryptography.SHA256]::Create()
            try
            {
                $hash = [BitConverter]::ToString($sha256.ComputeHash($files["pulse-input.json"])).Replace("-", "").ToLowerInvariant()
            }
            finally
            {
                $sha256.Dispose()
            }
            if ($hash -cne $identity.Groups["hash"].Value)
            {
                throw "Exact-byte JSON checksum mismatch."
            }
            $matching.Add($files["pulse-input.json"])
        }
        catch
        {
            $rejections.Add("$($artifact.id): $($_.Exception.Message)")
        }
        finally
        {
            if ($null -ne $archive)
            {
                $archive.Dispose()
            }
            if ($null -ne $buffer)
            {
                $buffer.Dispose()
            }
        }
    }
    if ($matching.Count -ne 1)
    {
        if ($matching.Count -gt 1)
        {
            throw "Snapshot ambiguous: multiple exact matching artifacts."
        }
        throw "Snapshot unavailable: no exact matching artifact. $($rejections -join '; ')"
    }

    try
    {
        $pulse = $utf8.GetString($matching[0]) | ConvertFrom-Json -Depth 100 -NoEnumerate
    }
    catch
    {
        throw "Malformed snapshot JSON: $($_.Exception.Message)"
    }
    if ($null -eq $pulse -or $pulse.GetType() -ne [System.Management.Automation.PSCustomObject])
    {
        throw "Invalid snapshot envelope."
    }
    if ($pulse.schemaVersion -cne "2.0.0")
    {
        throw "Unsupported combined snapshot schema."
    }
    if ($pulse.areas -isnot [array] -or $pulse.areas.Count -ne 2)
    {
        throw "Invalid snapshot areas."
    }
    $completeCount = 0
    $areaIds = @("blazor", "repository-wide")
    $labels = @("Blazor", "Repository-wide")
    for ($index = 0; $index -lt 2; $index++)
    {
        $area = $pulse.areas[$index]
        if ($area.id -cne $areaIds[$index] -or $area.label -cne $labels[$index] -or
            $area.openByDefault -isnot [bool] -or $area.openByDefault -or
            $area.source.repository -cne "dotnet/aspnetcore" -or
            $area.candidateCountsAvailable -isnot [bool] -or
            $area.status -cnotin @("complete", "unavailable"))
        {
            throw "Invalid snapshot area metadata."
        }
        if ($area.status -ceq "unavailable")
        {
            if ($area.candidateCountsAvailable -or $area.PSObject.Properties["views"] -or
                [string]::IsNullOrWhiteSpace($area.errorCategory))
            {
                throw "Invalid unavailable-area contract."
            }
            continue
        }
        $completeCount++
        if ($area.source.schemaVersion -cne "1.0.0")
        {
            throw "Unsupported complete-area source schema."
        }
        if (-not $area.candidateCountsAvailable -or
            $area.source.query.complete -isnot [bool] -or -not $area.source.query.complete -or
            $area.source.query.openPullRequestCount -ne $area.source.query.returnedPullRequestCount)
        {
            throw "Invalid complete-area inventory contract."
        }
        foreach ($view in @("reviewNow", "verifyDiscussionBeforeReview", "needsRescue", "readyToMerge", "verifyDiscussionBeforeMerge"))
        {
            if ($area.views.$view -isnot [array])
            {
                throw "Missing or invalid snapshot view '$view'."
            }
        }
    }
    $expectedStatus = @("unavailable", "partial", "complete")[$completeCount]
    if ($pulse.status -cne $expectedStatus)
    {
        throw "Snapshot status does not match its areas."
    }

    return $pulse
}
```

For an authenticated `gh` session, the following adapters supply those GETs.
The ZIP adapter copies native stdout bytes rather than passing binary through
PowerShell's text pipeline. It follows the download endpoint's redirect through
`gh api`; it does not persist a redirect URL. Transport failures are errors.

```powershell
$readJson = {
    param([string]$Path)
    $json = & gh api --hostname github.com --method GET $Path
    if ($LASTEXITCODE -ne 0)
    {
        throw "GitHub JSON read failed: $Path"
    }
    return ($json -join "`n" | ConvertFrom-Json -Depth 100)
}
$readZip = {
    param([string]$Path)
    $start = [Diagnostics.ProcessStartInfo]::new("gh")
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in @("api", "--hostname", "github.com", "--method", "GET", $Path))
    {
        $start.ArgumentList.Add($argument)
    }
    $process = [Diagnostics.Process]::Start($start)
    $errorText = $process.StandardError.ReadToEndAsync()
    $bytes = [IO.MemoryStream]::new()
    try
    {
        $process.StandardOutput.BaseStream.CopyTo($bytes)
        $process.WaitForExit()
        if ($process.ExitCode -ne 0)
        {
            throw "GitHub ZIP read failed: $($errorText.GetAwaiter().GetResult())"
        }
        return ,$bytes.ToArray()
    }
    finally
    {
        $bytes.Dispose()
        $process.Dispose()
    }
}
$issue = & $readJson "repos/dotnet/aspnetcore/issues/69328"
$publishedBody = $issue.body
$pulse = Get-PublishedPulseSnapshot -PublishedBody $publishedBody -ReadJson $readJson -ReadZip $readZip
```

For manual inspection, these are also read-only (use the captured IDs, not the
illustrative values for another report):

```powershell
gh api --hostname github.com --method GET "repos/dotnet/aspnetcore/actions/runs/34643961191/attempts/1"
gh api --hostname github.com --method GET --paginate --slurp "repos/dotnet/aspnetcore/actions/runs/34643961191/artifacts?per_page=100"
```

The local `Test-PulseSnapshotRetrieval.ps1` suite extracts and executes the named
retrieval example with API-shaped pages and ZIP fixtures. It tests this consumer
algorithm, not hosted authentication, actual Actions artifact retention/rerun
behavior, or issue publication. Those boundaries are not established by mocks.
