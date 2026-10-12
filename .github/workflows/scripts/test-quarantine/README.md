# Test quarantine KBE validation

The test-quarantine workflow uses a repository-owned `create_quarantine_issue`
safe-output script for new quarantines. The agent selects a matcher, but
the script validates deterministic evidence and renders the final issue.

A **new quarantine** is for a test that is not currently quarantined and is not
being re-quarantined after a prior unquarantine. A **re-quarantine** restores
quarantine after it was previously removed and reuses the original tracking
issue. These correspond to Case A and Case B in
[`test-quarantine.md`](../../test-quarantine.md), respectively, and to the
`case-a` and `case-b` values in eligibility receipts.

## Data flow

1. `Aggregate Part 1 failures` writes its exact serialized JSON to
   `$RUNNER_TEMP/test-quarantine-part1.json`.
2. A full-history trusted checkout and
   `collect_case_a_eligibility.py` produce
   `test-quarantine-case-a-eligibility.json`. Each test receipt records exact
   source resolution, data-row/method/class/assembly quarantine state, quarantine
   history category, regression status, raw/excluded/post-cutoff build sets,
   the conservative freshness cutoff, build-source ancestry against the
   history cutoff commit, exact evidence identity, and the `origin/main`
   history commit used for the decision. The receipt also identifies Case B
   tests with at least one post-cutoff failure whose source snapshot contains
   the unquarantine commit. Assembly history is
   reconstructed across the resolved test project, including deleted
   quarantine files. Same-project partial declarations are also evaluated as a
   logical type for type-level quarantine history, so sibling declaration
   renames, deletions, and attribute removals can still reveal a prior unquarantine.
   Type and assembly transitions apply only when the test resolved through the
   runner's actual inheritance chain at that commit. Merely having both types
   present is insufficient; later-added tests or later inheritance do not
   inherit old removals. Historical-only intermediate types are considered,
   rather than assuming the current chain existed unchanged.
   Partial inherited runners are resolved through an unambiguous same-project
   base chain. A full runner name found in multiple projects is rejected before
   either direct or inherited method matching. That logical type-history check
   remains separate from the exact
   method freshness cutoff and Source B pull-request file checks, which still
   key off the resolved declaring method and inherited runner files rather than
   every partial sibling declaration.
3. `collect_requarantine_history.py` enumerates every current data-row-,
   method-, type-, and assembly-level quarantine target from trusted source. It
   classifies the exact first-parent history from project-wide commit/parent
   source snapshots as `first-quarantine`, `re-quarantined`, or `ambiguous`;
   method and partial
   type moves between files preserve their logical history, and issue-URL-only
   replacements are not remove/add transitions. Automated unquarantine requires an exact
   `first-quarantine` match and fails closed otherwise.
4. The pre-activation job uploads all three files as the one-day
   `test-quarantine-evidence-<run-id>` artifact.
5. The agent may choose a new-quarantine or re-quarantine candidate only from
   the corresponding deterministic eligible-test list injected into its
   prompt.
6. `create_quarantine_issue` verifies the receipt's Part 1 SHA-256,
   repository, ref, commit, minimum new-quarantine predicates, exact test,
   matcher, and build/run/result identity. It rejects any test whose trusted
   receipt is not Case A, so a Case B re-quarantine cannot create a duplicate
   ordinary issue.
7. The handler creates or reuses the quarantine issue and returns the
   temporary-ID mapping used by `add_comment` and `create_pull_request`. Reuse
   is resolved by paginating `GET /repos/{owner}/{repo}/issues` with
   `state=open`, `labels=test-failure`, and `per_page=100`, then comparing
   titles exactly and discarding pull requests. The strongly consistent list
   endpoint is used instead of the issue search API, whose index is eventually
   consistent and can miss an issue created by a recent run.
8. Before the built-in `create_pull_request` handler runs,
   `validate_pull_request_outputs.py` applies each authoritative format-patch
   to the receipt-bound `origin/main` snapshot and derives its exact quarantine
   target changes. It rejects unrelated edits, mixed additions/removals,
   duplicate targets, stale project source, additions that do not match an
   eligible Case A or Case B receipt, and removals whose exact current history
   is not `first-quarantine`.

Agent-provided log excerpts and URLs are for human display only. They are not
accepted as validation evidence.

Part 1 aggregates by exact test-case name, preserving theory argument lists,
but not by assembly-qualified identity. The collector correlates those arguments
to an unambiguous `InlineData` row on a `ConditionalTheory`, or an existing
`QuarantinedTestData` row, when possible and
otherwise retains method-level behavior. It fails closed on ambiguous runner or
data-row identities rather than using representative metadata to guess.
Boolean and integer inline constants are normalized to their rendered argument
values while retaining the original source arguments for patch validation.
A `ConditionalTheory` with inline rows that cannot be matched exactly (including
missing argument lists or unsupported constant expressions) is unproven, not a
method-level quarantine candidate. Trailing attribute comments do not hide rows.
Row mapping also fails closed when other method attributes could provide data:
`MemberData`, `ClassData`, and unrecognized attributes can produce the same
rendered arguments as an inline row. Only the recognized theory, inline/data
quarantine, method quarantine, xUnit trait, and known repository non-data condition
attributes (including `MsQuicSupported`, `OSSkipCondition`, and
`FrameworkSkipCondition`) are accepted for automatic row mapping. The known
conditions derive directly from `Attribute`, not `DataAttribute`; implementing
`ITestCondition` alone is not sufficient. Unknown attributes are not assumed to
be non-data metadata.
Matching inline and quarantined rows with identical arguments are ambiguous too.
Renamed row owners receive the same conservative history checks as method targets.
Unresolved historical inheritance is unproven, not evidence that a test was
never inherited.

## Build Insights behavior

A verified issue ends with exactly one `## Error Message` JSON block containing
`ErrorMessage`, `ErrorPattern`, `BuildRetry`, and `ExcludeConsoleLog`. Exactly
one matcher field is populated. `BuildRetry` is always `false` and
`ExcludeConsoleLog` is always `true`.

The `test-failure` label is always applied to a newly created quarantine issue.
The `Known Build Error` label is applied only when the collector proves
new-quarantine eligibility, the matcher and duplicate search validate, and the
repository variable `TEST_QUARANTINE_ENABLE_KBE` is exactly `true`. The variable
is intentionally disabled by default until a post-merge canary is explicitly approved.

A trusted Case A receipt that is ineligible for KBE activation, or an
incomplete, broad, colliding, or unverifiable matcher, produces the ordinary
quarantine issue without a KBE JSON block or KBE label. Missing,
identity-mismatched, non-Case-A, or absent per-test receipts reject issue
creation. An individual test found only in deterministic Source C crash blocks
can receive an ordinary issue when its trusted receipt identifies it as Case A,
but cannot activate a KBE without exact VSTMR test-run/result identity.

This is intentionally stricter than runtime's current `ci-failure-scan`.
Runtime is prior art for the Build Insights JSON and automatic-label behavior;
ASP.NET Core's combined quarantine/unquarantine workflow additionally binds KBE
activation to a deterministic new-quarantine receipt so agent selection cannot
turn a regression, stale failure, existing quarantine, or re-quarantine record
into a KBE.

## Safety properties

- One exact fully qualified test per new-quarantine issue and PR.
- Row-level quarantine changes preserve the original inline data arguments and
  are accepted only when the deterministic receipt resolves that exact row.
- An exact `InlineData` candidate on a `ConditionalTheory` is always row-scoped;
  the validator never permits broadening it to a method quarantine. Ordinary
  xUnit theories remain method-scoped because they cannot consume quarantine
  row metadata.
- The collector recognizes multiline quarantine/data attributes, but automated
  row rewrites are deliberately limited to one-line attributes so patch
  validation never has to infer unchanged argument lines from diff context.
  One-line row replacements may retain trailing line or block comments. The
  validator ignores comment brackets when parsing, but requires the exact
  comment to remain attached to the same data arguments in the same diff hunk.
- The agent cannot author or override new-quarantine eligibility facts.
- Every quarantine or unquarantine PR is mechanically bound to deterministic
  receipts before the privileged PR handler runs. An unquarantine PR may
  remove multiple targets only when they share one issue and every target is a
  verified first quarantine.
- A post-cutoff failure is rejected when its source commit does not contain
  the history-derived cutoff commit, including stale PR merge snapshots that
  started after an unquarantine landed.
- At least two distinct post-cutoff failures, exact current quarantine state,
  regression exclusion, and the new-quarantine category are enforced before
  KBE rendering.
- Quarantine additions are bound to a deterministic operating-system set.
  A subset is emitted only when every retained incident has an unambiguous
  platform identity; otherwise the receipt requires all supported platforms.
  Source A/B retain per-build `queues` for every distinct result, including
  multiple platforms in one build. These come from the Helix job API's `QueueId`,
  not the OS-neutral work-item name. Source C records carry the selected job's
  `queue`. Job lookups (including failures) are cached across all three sources.
  Additional result-detail calls are bounded per source; missing identities,
  failed lookups, and budget exhaustion leave explicit unknown entries rather
  than borrowing the representative result's OS. Older payloads without queue
  metadata also require all supported platforms.
  Existing partially scoped targets are not automatically widened or narrowed.
- An assembly quarantine removal is treated as a prior unquarantine only if the
  runner actually inherited or declared the test at that transition. Ambiguous
  project or historical source association fails closed as unproven.
- The repository handler independently rejects a second `create_quarantine_issue`
  call in the same run, bounding new-quarantine output to one issue/PR/comment
  chain. This does not depend on the gh-aw v0.88.2 per-tool call allowance.
- Current repository only; fixed title and label policy.
- Exact-title open `test-failure` issues are reused without editing or
  relabeling them. Pull requests are excluded from the reuse scan, and the
  title comparison is exact, so a near-miss title never suppresses a real
  quarantine issue.
- Threat detection must succeed before writes.
- `GH_AW_SAFE_OUTPUTS_STAGED=true` produces an Actions summary preview and no
  GitHub write.
- Human log content is secret-scrubbed and HTML-escaped, so it cannot publish
  token-shaped credentials or introduce a competing fenced JSON block.
- Optional log links require an approved HTTPS host and contain no credentials,
  query string, or fragment.

## Validation

The [`Workflow tests`](../../workflow-tests.yml) pull-request workflow runs these suites when workflow or skill files change; the commands below also support local validation.

These fixtures use synthetic evidence, mock GitHub requests, and temporary Git
repositories. They do not create issues or pull requests, run an agent, or prove
live Build Insights enrollment. The workflow source and generated lock still
need the separate compilation step below.

Run the executable handler tests:

```bash
node .github/workflows/scripts/test-quarantine/test_kbe_issue_handler.js
```

Run the deterministic collector fixtures:

```bash
python3 -B .github/workflows/scripts/test-quarantine/test_collect_case_a_eligibility.py
```

Run the pull-request action-boundary fixtures:

```bash
python3 -B .github/workflows/scripts/test-quarantine/test_validate_pull_request_outputs.py
```

Validate the source with the repository's gh-aw toolchain:

```bash
gh aw compile test-quarantine --no-emit --strict
```

After source changes, regenerate `.github/workflows/test-quarantine.lock.yml`
with `gh aw compile test-quarantine`. Never edit the generated lock file
manually.
