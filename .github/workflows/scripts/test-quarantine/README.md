# Test quarantine KBE validation

The test-quarantine workflow uses a repository-owned `create_quarantine_issue`
safe-output script for new Case A quarantines. The agent selects a matcher, but
the script validates deterministic evidence and renders the final issue.

## Data flow

1. `Aggregate Part 1 failures` writes its exact serialized JSON to
   `$RUNNER_TEMP/test-quarantine-part1.json`.
2. A full-history trusted checkout and
   `collect_case_a_eligibility.py` produce
   `test-quarantine-case-a-eligibility.json`. Each test receipt records exact
   source resolution, method/class/assembly quarantine state, quarantine
   history category, regression status, raw/excluded/post-cutoff build sets,
   the conservative freshness cutoff, exact evidence identity, and the
   `origin/main` history commit used for the decision. Assembly history is
   reconstructed across the resolved test project, including deleted
   quarantine files. Inherited tests retain both the declaring-method file and
   every runner-type file so a change or unquarantine on either side
   invalidates stale evidence.
3. The pre-activation job uploads both files as the one-day
   `test-quarantine-evidence-<run-id>` artifact.
4. The agent may choose a Case A test only from the deterministic eligible-test
   list injected into its prompt.
5. `create_quarantine_issue` verifies the receipt's Part 1 SHA-256,
   repository, ref, commit, minimum Case A predicates, exact test, matcher, and
   build/run/result identity.
6. The handler creates or reuses the quarantine issue and returns the
   temporary-ID mapping used by `add_comment` and `create_pull_request`. Reuse
   is resolved by paginating `GET /repos/{owner}/{repo}/issues` with
   `state=open`, `labels=test-failure`, and `per_page=100`, then comparing
   titles exactly and discarding pull requests. The strongly consistent list
   endpoint is used instead of the issue search API, whose index is eventually
   consistent and can miss an issue created by a recent run.

Agent-provided log excerpts and URLs are for human display only. They are not
accepted as validation evidence.

## Build Insights behavior

A verified issue ends with exactly one `## Error Message` JSON block containing
`ErrorMessage`, `ErrorPattern`, `BuildRetry`, and `ExcludeConsoleLog`. Exactly
one matcher field is populated. `BuildRetry` is always `false` and
`ExcludeConsoleLog` is always `true`.

The `test-failure` label is always applied to a newly created quarantine issue.
The `Known Build Error` label is applied only when the collector proves Case A
eligibility, the matcher and duplicate search validate, and the repository variable
`TEST_QUARANTINE_ENABLE_KBE` is exactly `true`. The variable is intentionally
disabled by default until a post-merge canary is explicitly approved.

Missing, contradictory, ineligible, or unproven receipts and incomplete, broad,
colliding, or unverifiable matchers produce the ordinary quarantine issue
without a KBE JSON block or KBE label. An individual test found only in
deterministic Source C crash blocks can also receive an ordinary issue, but
cannot activate a KBE because it has no collector-authored Case A receipt or
exact VSTMR test-run/result identity.

This is intentionally stricter than runtime's current `ci-failure-scan`.
Runtime is prior art for the Build Insights JSON and automatic-label behavior;
ASP.NET Core's combined quarantine/unquarantine workflow additionally binds KBE
activation to a deterministic Case A receipt so agent selection cannot turn a
regression, stale failure, existing quarantine, or Case B record into a KBE.

## Safety properties

- One exact fully qualified test per Case A issue and PR.
- The agent cannot author or override Case A eligibility facts.
- At least two distinct post-cutoff failures, exact current quarantine state,
  regression exclusion, and originating Case A category are enforced before
  KBE rendering.
- An assembly quarantine removal is treated as Case B when the exact test and
  runner existed at that transition. Ambiguous project or historical source
  association fails closed as unproven.
- The repository handler independently rejects a second `create_quarantine_issue`
  call in the same run, bounding new Case A output to one issue/PR/comment
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

Run the executable handler tests:

```bash
node .github/workflows/scripts/test-quarantine/test_kbe_issue_handler.js
```

Run the deterministic collector fixtures:

```bash
python3 .github/workflows/scripts/test-quarantine/test_collect_case_a_eligibility.py
```

Validate the source with the repository's gh-aw v0.88.2 toolchain:

```bash
gh aw compile test-quarantine --no-emit --strict
```

After source changes, regenerate `.github/workflows/test-quarantine.lock.yml`
with `gh aw compile test-quarantine`. Never edit the generated lock file
manually.
