# Test quarantine KBE shadow evaluation

This directory implements a **read-only, non-authoritative shadow evaluation** of whether a
single, already-open dotnet/aspnetcore test-quarantine issue has enough evidence for a
Runtime-style Known Build Error (KBE) signature. It is deliberately narrow in scope: it never
authorizes an automated fix, never mutates repository state, and never promotes itself to
production without an explicit, separate decision by a maintainer.

## Components

| File | Role |
|---|---|
| `Collect-TestQuarantineKbeEvidence.ps1` | Deterministic collector. Given one issue number (and, optionally, a manual signature override), gathers public evidence and emits a **dossier**: either a `candidate` ready for the evaluator, or a structured `incomplete` outcome. |
| `Evaluate-TestQuarantineKbeCandidate.ps1` | Deterministic evaluator. Validates a candidate's signature, build/environment provenance, and authoritative Passed evidence, then emits a **receipt**. |
| `New-TestQuarantineKbeSummary.ps1` | Renders a short Markdown summary of a dossier (+ receipt, when present) for the workflow step summary and as an artifact. |
| `test-quarantine-kbe-shadow-candidate.schema.json` | Versioned schema for the evaluator's input. |
| `test-quarantine-kbe-shadow-receipt.schema.json` | Versioned schema for the evaluator's output. |
| `test-quarantine-kbe-shadow-dossier.schema.json` | Schema for the collector's output envelope (provenance + either `candidate` or `incomplete`). Independently versioned; it wraps and reuses the candidate schema rather than replacing or competing with it. |
| `fixtures/<issue>/` | Compact, sanitized, offline fixtures for three real pilot issues, each with a golden `expected-dossier.json`. |
| `Test-Evaluate-TestQuarantineKbeCandidate.ps1`, `Test-Collect-TestQuarantineKbeEvidence.ps1` | Deterministic, offline test suites (no network access). |
| `Test-WorkflowScriptInjectionSafety.ps1` | Static regression test asserting that no `run:` script body in the two workflows below interpolates a `${{ ... }}` GitHub Actions expression (see "Script-injection safety" below). |

The companion `.github/workflows/test-quarantine-kbe-shadow.yml` (maintainer dispatch) and
`.github/workflows/test-quarantine-kbe-shadow-tests.yml` (CI) workflows are described below.

## Trust boundary

* **Build Analysis is corroborating, never authoritative.** The collector records a snapshot of
  the GitHub "Build Analysis" check-run for every resolved build's commit: check id, conclusion, a
  SHA-256 of its full text, a capped/redacted excerpt, and two *conservative* substring checks --
  `exact_test_referenced` (the quarantined test's **full fully-qualified name** appears verbatim;
  never set from a bare method name, which commonly collides with unrelated tests -- a
  `short_name_referenced` field records that weaker match separately, for transparency only) and
  `known_issue_referenced` (a **concrete** dotnet/aspnetcore issue number/URL appears near a
  "Known Issue" style label; the bare phrase alone, e.g. a heading or table column reading "Known
  Issues" with no associated reference, does not set this true -- `known_issue_numbers` records
  exactly which issue(s) were found). A missing or generic snapshot is recorded and surfaced, but
  it never overrides raw evidence and never by itself makes a candidate valid or invalid. Direct
  queries against the Build Analysis abstraction for three real pilot builds (1563420, 1551326,
  1569737) returned only generic, task-level, unmatched failures and no known issues -- this is
  exactly the "generic" case the collector's pilot fixtures encode.
* **Authoritative VSTMR test-result detail, not Build Analysis and not a raw Helix console-log
  fetch, is what proves an exact test failure and its recurrence.** Azure DevOps' `resultsbyBuild`
  *summary* rows carry only identity and outcome (`id`, `runId`, `automatedTestName`, `outcome`) --
  confirmed live against aspnetcore#68947's own cited build: no `comment`/`errorMessage`/
  `stackTrace` field is present on an ordinary xUnit test's summary row. The collector instead
  calls the *detailed* per-result endpoint (`GET .../test/Runs/{runId}/results/{resultId}`) to
  retrieve the authoritative `errorMessage`/`stackTrace`, and materializes evidence from that text
  directly. Helix job/work-item coordinates are recorded as metadata only when a `comment` field
  happens to be present (in practice, only a Helix work item's own crash/`.WorkItemExecution`
  pseudo-test row carries one); otherwise `helix_unavailable: true` is recorded explicitly and the
  VSTMR detail text remains authoritative on its own -- never silently degraded. Recurrence
  requires evidence from **at least two distinct builds** (a single build producing two separate
  artifacts is not recurrence), and at least one authoritative **Passed**
  occurrence. Signature matching against that raw text uses ordinal, case-sensitive substring
  containment (`[string]::Contains(..., Ordinal)`) throughout -- never PowerShell's
  `-like`/`-notlike` operators, whose `*`, `?`, and `[...]` wildcard semantics would otherwise
  silently misinterpret a literal ErrorMessage containing any of those characters.
* **Azure DevOps build recurrence spans both `failed` and `partiallySucceeded` results.** A
  `resultFilter=failed`-only recurrence query misses real evidence: aspnetcore#68947's own cited
  build 1551326 is itself `partiallySucceeded`, not `failed`, confirmed live. Azure DevOps'
  `resultFilter` does not support a comma-separated multi-value combination (verified live:
  `resultFilter=failed,partiallySucceeded` does not behave as their union), so the collector issues
  one request per result value and merges/dedupes by build id (`Merge-AzdoBuildLists`, covered by a
  direct, network-free unit test).
* **A `## Failing Test(s)` section naming more than one concrete test identity fails closed.**
  aspnetcore#68724 names both a base test and its server-execution subclass override in one
  section; live data shows only the override actually failed while the base identity passed.
  Silently picking the first backtick-quoted name risks binding evidence to the wrong test. This
  collector requires exactly one unambiguous identity per run and fails closed
  (`multiple-test-identities-unresolved`) otherwise, rather than guessing. Evaluating every listed
  identity independently is a reasonable follow-up left for later, since this PR targets one
  issue/one root cause at a time -- documented here as an accepted simplification, not an
  oversight.
* **The `test-failure` label alone is not proof an issue was generated by quarantine automation**
  (any contributor can apply it to an ordinary bug report). The collector additionally requires the
  issue body to contain the trusted `<!-- gh-aw-workflow-id: test-quarantine -->` or
  `<!-- gh-aw-workflow-call-id: dotnet/aspnetcore/test-quarantine -->` HTML-comment marker the
  production quarantine workflow stamps into every issue it creates.
* **A duplicate-search hit is discovery only, not a validated duplicate.** Every numeric result
  returned by the four categorized GitHub searches is fetched. An existing KBE requires the exact
  FQN plus a documented `ErrorMessage`/`ErrorPattern` that matches every authoritative failure log
  with evaluator-compatible semantics. A fix PR requires the exact FQN plus a compatible signature,
  linked KBE/quarantine issue, or root-cause association. Incompatible fetched items remain
  `unvalidated_candidate` entries; a failed candidate-detail fetch makes that query and the overall
  duplicate coverage incomplete. The
  "recently" closed/merged categories carry an explicit 90-day `closed:>=`/`merged:>=` time-window
  qualifier so the label matches what the query actually searches, and a query is only marked
  `complete` when GitHub reports `incomplete_results: false` **and** every matching item (per
  `total_count`, across up to 3 paginated pages of 100) was actually retrieved -- a `total_count`
  larger than a single page previously went unnoticed.
* **The immutable workflow-dispatch ref is verified, not assumed.** Trusted `github.ref` and
  `github.sha` values are passed through `env:` bindings. The ref must be exactly `refs/heads/main`,
  the checkout must equal the dispatch SHA, and the dispatch SHA must be identical to or an
  ancestor/member of current main. The dossier records event ref/SHA, checkout SHA, and current
  main SHA separately, so main advancing after dispatch is valid while non-main dispatches fail
  closed.
* **Every countable build is validated before use.** Pipeline definition must be 83 or 87,
  `sourceBranch` must be exactly `refs/heads/main`, status must be `completed`, and failure builds
  must be `failed` or `partiallySucceeded` (`succeeded` is required for the Passed scan). These
  dimensions are recorded in build provenance and candidate evidence.
* **Platform/configuration are derived from authoritative metadata, never fabricated.** Azure
  DevOps' `buildConfiguration.platform`/`.flavor` fields are empty strings on every real run
  observed live; the only authoritative, cheaply-available signal is the VSTMR TestRun's own
  `name` (e.g. `Quarantine-Mono-Linux-Release-xunit`). The collector parses recognized
  platform/configuration tokens out of that name. A counted failure or pass with either dimension
  `"unknown"` emits explicit missing-evidence codes and prevents a candidate/validated receipt.
* **Never infer a pass, a recurrence, a signature, a platform/configuration, or a validated
  duplicate from missing or unverifiable evidence.** Every gap -- a build whose Azure DevOps
  metadata has aged out of retention, historical VSTMR test-result data no longer queryable for a
  build, an ambiguous or absent signature, an incomplete duplicate search, an unconfirmed
  repository ref -- is recorded as an explicit reason code and fails the run closed
  (`outcome: "incomplete"`) rather than silently degrading. Real Azure DevOps retention data
  confirms this is not hypothetical: build 1537561 (cited by aspnetcore#68947) has already aged out
  of the public `dnceng-public` project.
* **No repository-state mutation.** The workflow's permissions are `contents: read`,
  `issues: read`, `pull-requests: read`, `checks: read` -- read-only across the board. It uses only
  the ambient `GITHUB_TOKEN`, sent as a real `Authorization: Bearer <token>` header (never a
  literal placeholder), never a PAT or other secret. Authenticated calls are load-bearing for
  practical use, not cosmetic: GitHub's unauthenticated search-API rate limit (10 requests/minute)
  is exhausted by a single run's four duplicate searches plus one retry, and the collector logs the
  authenticated `X-RateLimit-Remaining`/`X-RateLimit-Limit` headers (non-blocking, informational
  only) after each GitHub call. The workflow uploads artifacts; it never calls any write API (no
  labels, comments, commits, branches, or files).
* **Evidence provenance remains unverified in this PR.** Even when the collector's candidate is
  independently valid, the evaluator still emits `evidence_provenance_verified: false`,
  `eligible_for_kbe_enrichment: false`, and `human_review_required: true`. Flipping
  `evidence_provenance_verified` to `true` is a deliberate, separate promotion decision (see
  "Promotion gates" below) -- not something this collector claims for itself.

## Workflow: `test-quarantine-kbe-shadow.yml` (maintainer dispatch)

* **Trigger**: `workflow_dispatch` only, with a required `issue_number` input and an optional
  `signature` input (used only when the issue body has no fenced `## Error Message` block that the
  collector can extract deterministically).
* **Fork guard**: the job is gated on `github.repository == 'dotnet/aspnetcore'`, so a forked copy
  of this file is inert.
* **Concurrency**: one run per issue number (`test-quarantine-kbe-shadow-<issue_number>`),
  cancelling any still-running dispatch for the same issue.
* **Permissions**: `contents: read`, `issues: read`, `pull-requests: read`, `checks: read` -- the
  last two are the "only demonstrated needs" beyond the baseline: `checks: read` for the Build
  Analysis check-run snapshot, `pull-requests: read` for the duplicate fix-PR search.
* **Actions are pinned by commit SHA** (`actions/checkout`, `actions/upload-artifact`), matching
  repository convention.
* **Steps**: validate the input, checkout, run the collector, run the evaluator only when the
  collector's outcome is `candidate`, render the human-readable summary (to the job's step summary
  and as a file), then upload artifacts.
* **Artifacts** (uploaded with `retention-days: 7`, i.e. short-lived by design): `dossier.json`,
  `candidate.json` (candidate outcome only), `receipt.json` (candidate outcome only),
  `summary.md`, and the capped/redacted evidence text files under `evidence/`.

## CI: `test-quarantine-kbe-shadow-tests.yml`

Runs both deterministic, offline PowerShell test suites (`Test-Evaluate-TestQuarantineKbeCandidate.ps1`
and `Test-Collect-TestQuarantineKbeEvidence.ps1`), plus the script-injection safety guard described
below, on every pull request that touches this directory or either workflow file, plus on manual
dispatch. Every fixture is local and offline; no network access or secrets are required, so this is
a low-risk, standard `pull_request`-triggered check -- no self-test mode inside the shadow workflow
itself was needed to close this CI gap.

## Script-injection safety

`test-quarantine-kbe-shadow.yml` accepts two `workflow_dispatch` inputs, `issue_number` and
`signature`, both of which are attacker-influenceable text (anyone who can dispatch the workflow
controls their exact content). Neither is ever interpolated directly into a `run:` script body via
`${{ inputs.issue_number }}` / `${{ inputs.signature }}`: a signature value containing a quote,
backtick, or newline embedded directly into script text could otherwise execute arbitrary commands
on the runner. Instead, every input flows through a step (or job) `env:` binding -- for example
`env: { SIGNATURE_INPUT: ${{ inputs.signature }} }` -- and is read back inside the script as an
opaque environment variable (`$env:SIGNATURE_INPUT`), which the PowerShell parser never re-parses
as script text regardless of its content. `issue_number` is additionally validated against
`^[1-9][0-9]*$` before being persisted to `$GITHUB_ENV`, and only that already-validated
`env.ISSUE_NUMBER` value (not the raw input) is used in the uploaded artifact's name -- a
non-`run:` context where Actions itself handles the substitution, not a shell.

`Test-WorkflowScriptInjectionSafety.ps1` is a static, offline regression test for exactly this
invariant: it parses every `run:` block (block-scalar and single-line forms) in both workflow files
using a minimal, indentation-based reader and fails if any of them contains a `${{` token. It
deliberately does not inspect `if:`, `env:`, `with:`, or `concurrency:` values, since a `${{ }}`
expression there is evaluated by the Actions engine itself and is not a script-injection vector.

## Fixture mode (`-FixtureRoot`)

Both `Collect-TestQuarantineKbeEvidence.ps1` and its tests support a `-FixtureRoot <dir>`
parameter. When set, the collector reads one consolidated `fixture.json` document from that
directory instead of making any live GitHub/Azure DevOps call. `fixture.json` mirrors the shape of
the real endpoints captured live during development:

Fixture tests always pass an explicit `EventRef = refs/heads/main` and `EventSha` equal to the
checked-out repository SHA. The collector does not default these parameters from ambient
`GITHUB_REF`/`GITHUB_SHA`, so a pull-request test job cannot change fixture output. Live workflow
callers pass trusted `github.ref`/`github.sha` through step environment bindings.

| Key | Mirrors |
|---|---|
| `issue` | `GET /repos/{repo}/issues/{number}` (number, state, labels, body) |
| `main_branch` *(optional)* | Current main `.sha`, plus optional `contains_event_sha` to model ancestry/membership after main advances; omit for legacy pilot fixtures that do not exercise this guard |
| `azdo_builds` | `GET .../build/builds/{id}` keyed by build id |
| `recurrence_scan`, `negative_scan` | `GET .../build/builds?resultFilter=...` keyed by pipeline definition id |
| `vstmr_summary` | `GET .../testresults/resultsbyBuild?buildId=...` keyed by build id (array of summary rows) |
| `vstmr_detail` | `GET .../test/Runs/{runId}/results/{resultId}` keyed by `"{runId}:{resultId}"` |
| `vstmr_runs` | `GET .../test/runs/{runId}` keyed by run id (only `.name` is used) |
| `check_runs` | `GET /repos/{repo}/commits/{sha}/check-runs` keyed by commit SHA |
| `duplicate_search` | `GET /search/issues?q=...` keyed by category, pre-paginated (`complete`, `result_numbers`, `total_count`) |
| `duplicate_candidate_text` | `GET /repos/{repo}/issues/{number}` (title + body) keyed by issue/PR number, used to validate a duplicate-search hit |

## Pilot fixtures

| Issue | Fixture invocation | Outcome |
|---|---|---|
| [#68724](https://github.com/dotnet/aspnetcore/issues/68724) | no `-Signature` | `incomplete`: `## Failing Test(s)` names two distinct concrete identities (a base test and its server-execution subclass override); live data shows only the override actually failed, so the collector fails closed (`multiple-test-identities-unresolved`) rather than guess |
| [#68947](https://github.com/dotnet/aspnetcore/issues/68947) | `-Signature "OpenQA.Selenium.WebDriverException : The HTTP request to the remote WebDriver server"` (the real issue body has no fenced `## Error Message`, so extraction is deterministically ambiguous without the override -- verified by a second, signature-less invocation of the same fixture in the test suite) | `candidate`, `timeout-needs-classification` (generic Selenium/WebDriver timeout, not a test-specific KBE); recurrence is established via the supplementary scan since the issue's own second cited build has aged out of retention |
| [#68945](https://github.com/dotnet/aspnetcore/issues/68945) | `-Signature "System.Threading.Tasks.TaskCanceledException: The operation was canceled."` | `incomplete`: the second cited build's Azure DevOps build record still resolves, but its historical VSTMR test-result data is no longer queryable, leaving only one usable failure log below the two-build recurrence floor (`raw-evidence-insufficient`) |

Each fixture directory also has an `expected-dossier.json` golden file used for deep-equality
comparison in the test suite. Golden comparisons exclude the collector's `generated_utc` /
`retrieved_utc` / `captured_utc` / `checked_utc` timestamps and the running checkout's
`commit_sha` / `event_sha` / `checkout_sha` / `current_main_sha` -- all of which are expected to differ
run-to-run and commit-to-commit -- replacing them with the literal sentinel `<GENERATED>` on both
sides before comparing.

The test suite additionally covers, via small synthetic (non-pilot) fixtures: a closed issue, an
issue missing the `test-failure` label, an issue carrying the label but missing the trusted
workflow marker, immutable dispatch ancestry and non-main rejection, strict build definition/ref/
status/result gates, skip-only negative evidence, unknown environment dimensions, Build Analysis
flag precision, compatible and incompatible same-FQN duplicate signatures, failed duplicate-detail
fetches, fix-PR associations, wildcard-shaped literal signatures, and build-list merge/dedupe.

## Reconciling with the existing evaluator contract

The collector does **not** introduce a third, competing candidate contract. Its `candidate` output,
when present, is validated against the same versioned
`test-quarantine-kbe-shadow-candidate.schema.json` used by the evaluator and is fed to
`Evaluate-TestQuarantineKbeCandidate.ps1` exactly as-is. `test-quarantine-kbe-shadow-dossier.schema.json` is a
new, independently versioned (`schema_version: 1`) envelope that carries collector-specific
provenance (repository-ref verification, Azure DevOps build resolution, Build Analysis check-run
snapshots, raw-evidence retrieval, unvalidated duplicate-search candidates) alongside that same
`candidate` object, or a structured `incomplete` outcome when any evidence gate fails. Because the
candidate schema's `duplicate_check.queries[]` items are `additionalProperties: false` (and
correctly so -- it must stay byte-for-byte compatible with the evaluator), the
dossier-only `total_count` field is carried on `provenance.duplicate_search.queries[]` only, never
on `candidate.duplicate_check.queries[]`.

## Eventual extraction of the production deterministic collector

`.github/workflows/test-quarantine.md` already embeds a much larger deterministic, pre-activation
collector (Azure DevOps `resultsbyBuild`/build-timeline aggregation across ~200 builds, Helix
console-log `[FAIL]` block extraction, secret redaction) that gathers failure evidence for the
*entire* quarantine-management workflow, not a single issue. **This PR does not modify that
workflow or its production quarantine behavior.** The long-term path is to factor the shared
pieces -- the Azure DevOps/VSTMR fetch helpers, the secret-redaction patterns -- out of
`test-quarantine.md`'s Python step and this directory's PowerShell collector into one common,
tested module that both consume, rather than maintaining two independent implementations of the
same evidence-gathering logic indefinitely. Until that extraction happens, this collector
intentionally duplicates only the narrow, single-issue subset of that logic it needs (build
resolution, VSTMR summary/detail lookup, the same secret-redaction pattern family) and
cross-references the production workflow's real, observed issue-body formats (both the
`50_test_failure.md` template and the freeform `## Details` variant) so it does not invent a third,
incompatible issue-body convention.

## Promotion gates

None of the following are implemented by this PR. They are the measurable conditions a future,
separate change would need to satisfy before this collector's output could ever be trusted enough
to flip `evidence_provenance_verified` to `true` or to feed a maintainer-triggered fix workflow:

1. **Signed/attested provenance for every raw evidence file** -- e.g. a hash chain from the Azure
   DevOps API response actually observed at collection time, not merely a locally computed SHA-256
   of whatever bytes were written to disk.
2. **A second, independent collector run reaching the same `candidate` (or `incomplete`) result**
   for the same issue, to bound single-run collection errors (rate limiting, partial API
   responses, transient Azure DevOps outages).
3. **A maintainer explicitly reviewing and approving** the specific candidate/receipt pair -- this
   PR's receipt already always sets `human_review_required: true` for exactly this reason.
4. **The shared extraction described above landing** so the single-issue collector and the
   production quarantine workflow's evidence gathering can no longer silently diverge.
5. **A dedicated, maintainer-triggered fix workflow being designed and reviewed separately** -- this
   PR intentionally stops at read-only evaluation and does not implement or wire up any automated
   fix path.
6. **Evaluating every identity named in a multi-identity `## Failing Test(s)` section
   independently**, rather than failing the whole run closed the moment more than one concrete
   test is named (the current, deliberately conservative behavior for this PR).
