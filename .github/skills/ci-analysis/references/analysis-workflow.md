# Analysis Workflow (Steps 1–3)

After completing Step 0 (Gather Context — see SKILL.md), follow these steps.

## Step 1: Run the Script

Run with `-ShowLogs` for detailed failure info. See [script-modes.md](script-modes.md) for parameter details.

## Step 1b: Investigate with AzDO Tools

When the script output is insufficient (e.g., build timeline fetch fails), use AzDO MCP tools to query builds directly. **Match the org from the build URL to the correct AzDO tools** — see [azdo-helix-reference.md](azdo-helix-reference.md#azure-devops-organizations). PR builds are in `dnceng-public`; internal builds are in `dnceng`.

## Step 2: Analyze Results

1. **Check Build Analysis** — If the Build Analysis GitHub check is **green**, all failures matched known issues and it's safe to retry. If it's **red**, some failures are unaccounted for — you must identify which failing jobs are covered by known issues and which are not. For 3+ failures, use SQL tracking to avoid missed matches (see [sql-tracking.md](sql-tracking.md)).
2. **Correlate with PR changes** — Same files failing = likely PR-related
3. **Compare with baseline** — If a test passes on the target branch but fails on the PR, compare Helix binlogs. See [binlog-comparison.md](binlog-comparison.md) — **delegate binlog download/extraction to subagents** to avoid burning context on mechanical work.
4. **Check build progression** — If the PR has multiple builds (multiple pushes), check whether earlier builds passed. A failure that appeared after a specific push narrows the investigation to those commits. See [build-progression-analysis.md](build-progression-analysis.md). Present findings as facts, not fix recommendations.
5. **Interpret patterns** (but don't jump to conclusions):
   - Same error across many jobs → Real code issue
   - Build Analysis flags a known issue → That *specific failure* is safe to retry (but others may not be)
   - Failure is **not** in Build Analysis → Investigate further before assuming transient
   - Device failures, Docker pulls, network timeouts → *Could* be infrastructure, but verify against the target branch first
   - Test timeout but tests passed → Executor issue, not test failure
6. **Check for mismatch with user's question** — The script only reports builds for the current head SHA. If the user asks about a job, error, or cancellation that doesn't appear in the results, **ask** if they're referring to a prior build. Common triggers:
   - User mentions a canceled job but `canceledJobNames` is empty
   - User says "CI is failing" but the latest build is green
   - User references a specific job name not in the current results
   Offer to re-run with `-BuildId` if the user can provide the earlier build ID from AzDO.

## Step 3: Verify Before Claiming

Before stating a failure's cause, verify your claim:

- **"Infrastructure failure"** → Did Build Analysis flag it? Does the same test pass on the target branch? If neither, don't call it infrastructure.
- **"Transient/flaky"** → Has it failed before? Is there a known issue? A single non-reproducing failure isn't enough to call it flaky.
- **"PR-related"** → Do the changed files actually relate to the failing test? Correlation in the script output is heuristic, not proof.
- **"Safe to retry"** → Are ALL failures accounted for (known issues or infrastructure), or are you ignoring some? Check the Build Analysis check status — if it's red, not all failures are matched. Map each failing job to a specific known issue before concluding "safe to retry."
- **"Not related to this PR"** → Have you checked if the test passes on the target branch? Don't assume — verify.
