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
| `Evaluate-TestQuarantineKbeCandidate.ps1` | Pre-existing, unmodified deterministic evaluator. Validates a candidate's signature against its evidence and emits a **receipt**. |
| `New-TestQuarantineKbeSummary.ps1` | Renders a short Markdown summary of a dossier (+ receipt, when present) for the workflow step summary and as an artifact. |
| `test-quarantine-kbe-shadow-candidate.schema.json` | Pre-existing, unmodified schema for the evaluator's input. |
| `test-quarantine-kbe-shadow-receipt.schema.json` | Pre-existing, unmodified schema for the evaluator's output. |
| `test-quarantine-kbe-shadow-dossier.schema.json` | Schema for the collector's output envelope (provenance + either `candidate` or `incomplete`). Independently versioned; it wraps and reuses the candidate schema rather than replacing or competing with it. |
| `fixtures/<issue>/` | Compact, sanitized, offline fixtures for three real pilot issues, each with a golden `expected-dossier.json`. |
| `Test-Evaluate-TestQuarantineKbeCandidate.ps1`, `Test-Collect-TestQuarantineKbeEvidence.ps1` | Deterministic, offline test suites (no network access). |

The companion `.github/workflows/test-quarantine-kbe-shadow.yml` (maintainer dispatch) and
`.github/workflows/test-quarantine-kbe-shadow-tests.yml` (CI) workflows are described below.

## Trust boundary

* **Build Analysis is corroborating, never authoritative.** The collector records a snapshot of
  the GitHub "Build Analysis" check-run for every resolved build's commit (check id, conclusion,
  a SHA-256 of its full text, a capped/redacted excerpt, and a conservative substring check for
  whether the text names this exact test and/or a "Known Issue"). A missing or generic snapshot
  is recorded and surfaced, but it never overrides raw evidence and never by itself makes a
  candidate valid or invalid. Direct queries against the Build Analysis abstraction for three real
  pilot builds (1563420, 1551326, 1569737) returned only generic, task-level, unmatched failures
  and no known issues — this is exactly the "generic" case the collector's fixtures for #68724 and
  #68945 encode.
* **Raw AzDO/Helix/VSTMR evidence is what proves an exact test failure and its recurrence.** The
  collector resolves Azure DevOps build metadata, VSTMR test results, and Helix console-log
  content directly. Recurrence requires evidence from **at least two distinct builds** (a single
  build producing two separate artifacts is not recurrence), and at least one authoritative
  negative (passed/skipped) occurrence.
* **Never infer a pass, a recurrence, or a signature from missing or expired evidence.** Every gap
  — a build whose Azure DevOps metadata has aged out of retention, a Helix console log that has
  expired, an ambiguous or absent signature, an incomplete duplicate search — is recorded as an
  explicit reason code and fails the run closed (`outcome: "incomplete"`) rather than silently
  degrading. Real Azure DevOps retention data confirms this is not hypothetical: build 1537561
  (cited by aspnetcore#68947) has already aged out of the public `dnceng-public` project.
* **No repository-state mutation.** The workflow's permissions are `contents: read`,
  `issues: read`, `pull-requests: read`, `checks: read` — read-only across the board. It uses only
  the ambient `GITHUB_TOKEN`, never a PAT or other secret. It uploads artifacts; it never calls any
  write API (no labels, comments, commits, branches, or files).
* **Evidence provenance remains unverified in this PR.** Even when the collector's candidate is
  independently valid, the unmodified evaluator still emits `evidence_provenance_verified: false`,
  `eligible_for_kbe_enrichment: false`, and `human_review_required: true`. Flipping
  `evidence_provenance_verified` to `true` is a deliberate, separate promotion decision (see
  "Promotion gates" below) — not something this collector claims for itself.

## Workflow: `test-quarantine-kbe-shadow.yml` (maintainer dispatch)

* **Trigger**: `workflow_dispatch` only, with a required `issue_number` input and an optional
  `signature` input (used only when the issue body has no fenced `## Error Message` block that the
  collector can extract deterministically).
* **Fork guard**: the job is gated on `github.repository == 'dotnet/aspnetcore'`, so a forked copy
  of this file is inert.
* **Concurrency**: one run per issue number (`test-quarantine-kbe-shadow-<issue_number>`),
  cancelling any still-running dispatch for the same issue.
* **Permissions**: `contents: read`, `issues: read`, `pull-requests: read`, `checks: read` — the
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
and `Test-Collect-TestQuarantineKbeEvidence.ps1`) on every pull request that touches this directory
or either workflow file, plus on manual dispatch. Every fixture is local and offline; no network
access or secrets are required, so this is a low-risk, standard `pull_request`-triggered check —
no self-test mode inside the shadow workflow itself was needed to close this CI gap.

## Fixture mode (`-FixtureRoot`)

Both `Collect-TestQuarantineKbeEvidence.ps1` and its tests support a `-FixtureRoot <dir>`
parameter. When set, the collector reads one consolidated `fixture.json` document from that
directory instead of making any live GitHub/Azure DevOps/Helix call — this is how the test suite
and the three pilot fixtures below achieve fully offline, deterministic coverage. `fixture.json`
mirrors the shape of the real endpoints documented in `test-quarantine.md`'s "API Reference"
section: the GitHub issue body/labels/state, Azure DevOps build metadata keyed by build id, a
capped recurrence/negative-build candidate list per pipeline definition, VSTMR test outcomes keyed
by build id, capped Helix console excerpts keyed by build id, GitHub check-run snapshots keyed by
commit SHA, and categorized duplicate-search results.

## Pilot fixtures

| Issue | Fixture invocation | Outcome |
|---|---|---|
| [#68724](https://github.com/dotnet/aspnetcore/issues/68724) | no `-Signature` (deterministic `## Error Message` extraction) | `candidate`, `reuse-existing-kbe` (recommends reusing #68708) |
| [#68947](https://github.com/dotnet/aspnetcore/issues/68947) | `-Signature "OpenQA.Selenium.WebDriverException: TaskCanceledException"` (the real issue body has no fenced `## Error Message`, so extraction is deterministically ambiguous without the override — verified by a second, signature-less invocation of the same fixture in the test suite) | `candidate`, `timeout-needs-classification` (generic Selenium/WebDriver timeout, not a test-specific KBE) |
| [#68945](https://github.com/dotnet/aspnetcore/issues/68945) | `-Signature "System.Threading.Tasks.TaskCanceledException: The operation was canceled."` | `incomplete`: the second cited build's Helix console-log artifact has expired (`raw-evidence-expired`), leaving only one usable failure log below the two-build recurrence floor (`raw-evidence-insufficient`) |

Each fixture directory also has an `expected-dossier.json` golden file used for deep-equality
comparison in the test suite. Golden comparisons exclude the collector's `generated_utc` /
`retrieved_utc` / `captured_utc` / `checked_utc` timestamps and the running checkout's HEAD
`commit_sha` — all of which are expected to differ run-to-run and commit-to-commit — replacing
them with the literal sentinel `<GENERATED>` on both sides before comparing.

## Reconciling with the existing evaluator contract

The collector does **not** introduce a third, competing dossier schema. Its `candidate` output,
when present, is validated against the same unmodified
`test-quarantine-kbe-shadow-candidate.schema.json` used by the evaluator and is fed to the
unmodified `Evaluate-TestQuarantineKbeCandidate.ps1` exactly as-is — this PR does not change that
script, its tests, or either of its schemas. `test-quarantine-kbe-shadow-dossier.schema.json` is a
new, independently versioned (`schema_version: 1`) envelope that carries collector-specific
provenance (Azure DevOps build resolution, Build Analysis check-run snapshots, raw-evidence
retrieval/expiry) alongside that same `candidate` object, or a structured `incomplete` outcome when
any evidence gate fails.

## Eventual extraction of the production deterministic collector

`.github/workflows/test-quarantine.md` already embeds a much larger deterministic, pre-activation
collector (Azure DevOps `resultsbyBuild`/build-timeline aggregation across ~200 builds, Helix
console-log `[FAIL]` block extraction, secret redaction) that gathers failure evidence for the
*entire* quarantine-management workflow, not a single issue. **This PR does not modify that
workflow or its production quarantine behavior.** The long-term path is to factor the shared
pieces — the Azure DevOps/Helix/VSTMR fetch helpers, the secret-redaction patterns, the Helix
`[FAIL]`-block extraction — out of `test-quarantine.md`'s Python step and this directory's
PowerShell collector into one common, tested module that both consume, rather than maintaining two
independent implementations of the same evidence-gathering logic indefinitely. Until that
extraction happens, this collector intentionally duplicates only the narrow, single-issue subset
of that logic it needs (build resolution, VSTMR outcome lookup, Helix console retrieval, the same
secret-redaction pattern family) and cross-references the production workflow's real, observed
issue-body formats (both the `50_test_failure.md` template and the freeform `## Details` variant)
so it does not invent a third, incompatible issue-body convention.

## Promotion gates

None of the following are implemented by this PR. They are the measurable conditions a future,
separate change would need to satisfy before this collector's output could ever be trusted enough
to flip `evidence_provenance_verified` to `true` or to feed a maintainer-triggered fix workflow:

1. **Signed/attested provenance for every raw evidence file** — e.g. a hash chain from the Azure
   DevOps/Helix API response actually observed at collection time, not merely a locally computed
   SHA-256 of whatever bytes were written to disk.
2. **A second, independent collector run reaching the same `candidate` (or `incomplete`) result**
   for the same issue, to bound single-run collection errors (rate limiting, partial API
   responses, transient Azure DevOps/Helix outages).
3. **A maintainer explicitly reviewing and approving** the specific candidate/receipt pair — this
   PR's receipt already always sets `human_review_required: true` for exactly this reason.
4. **The shared extraction described above landing** so the single-issue collector and the
   production quarantine workflow's evidence gathering can no longer silently diverge.
5. **A dedicated, maintainer-triggered fix workflow being designed and reviewed separately** — this
   PR intentionally stops at read-only evaluation and does not implement or wire up any automated
   fix path.
