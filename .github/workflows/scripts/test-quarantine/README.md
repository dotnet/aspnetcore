# Test quarantine KBE validation

The test-quarantine workflow uses a repository-owned `create_quarantine_issue`
safe-output script for new Case A quarantines. The agent selects a matcher, but
the script validates deterministic evidence and renders the final issue.

## Data flow

1. `Aggregate Part 1 failures` writes its exact serialized JSON to
   `$RUNNER_TEMP/test-quarantine-part1.json`.
2. The pre-activation job uploads that file as the one-day
   `test-quarantine-evidence-<run-id>` artifact.
3. The safe-output job downloads the artifact before processing tool calls.
4. `create_quarantine_issue` validates the exact test and matcher against the
   downloaded snapshot, including the newest retrievable build, test-run, and
   result identity.
5. The handler creates or reuses the quarantine issue and returns the
   temporary-ID mapping used by `add_comment` and `create_pull_request`.

Agent-provided log excerpts and URLs are for human display only. They are not
accepted as validation evidence.

## Build Insights behavior

A verified issue ends with exactly one `## Error Message` JSON block containing
`ErrorMessage`, `ErrorPattern`, `BuildRetry`, and `ExcludeConsoleLog`. Exactly
one matcher field is populated. `BuildRetry` is always `false` and
`ExcludeConsoleLog` is always `true`.

The `test-failure` label is always applied to a newly created quarantine issue.
The `Known Build Error` label is applied only when the matcher and duplicate
search validate and the repository variable
`TEST_QUARANTINE_ENABLE_KBE` is exactly `true`. The variable is intentionally
disabled by default until ownership of the shared KBE queue is approved.

Incomplete, broad, colliding, or unverifiable matchers produce the ordinary
quarantine issue without a KBE JSON block or KBE label.
An individual test found only in deterministic Source C crash blocks can also
receive an ordinary issue, but cannot activate a KBE because no exact VSTMR
test-run/result identity is available.

## Safety properties

- One exact fully qualified test per Case A issue and PR.
- At most ten issue-tool calls per workflow activation.
- Current repository only; fixed title and label policy.
- Exact-title open issues are reused without editing or relabeling them.
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

Validate the source with the repository's gh-aw v0.88.2 toolchain:

```bash
gh aw compile test-quarantine --no-emit --strict
```

After source changes, regenerate `.github/workflows/test-quarantine.lock.yml`
with `gh aw compile test-quarantine`. Never edit the generated lock file
manually.
