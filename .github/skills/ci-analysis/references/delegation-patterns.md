# Subagent Delegation Patterns

CI investigations involve repetitive, mechanical work that burns main conversation context. Delegate data gathering to subagents; keep interpretation in the main agent.

## Pattern 1: Scanning Multiple Console Logs

**When:** Multiple failing work items across several jobs.

**Delegate:**
```
Extract all unique test failures from these Helix work items:

Job: {JOB_ID_1}, Work items: {ITEM_1}, {ITEM_2}
Job: {JOB_ID_2}, Work items: {ITEM_3}

For each, search console logs for lines ending with [FAIL] (xUnit format).
If hlx MCP is not available, fall back to:
  ./scripts/Get-CIStatus.ps1 -HelixJob "{JOB}" -WorkItem "{ITEM}"

Extract lines ending with [FAIL] (xUnit format). Ignore [OUTPUT] and [PASS] lines.

Return JSON: { "failures": [{ "test": "Namespace.Class.Method", "workItems": ["item1", "item2"] }] }
```

## Pattern 2: Finding a Baseline Build

**When:** A test fails on a PR — need to confirm it passes on the target branch.

**Delegate:**
```
Find a recent passing build on {TARGET_BRANCH} of dotnet/{REPO} that ran the same test leg.

Failing build: {BUILD_ID}, job: {JOB_NAME}, work item: {WORK_ITEM}

Steps:
1. Search for recently merged PRs:
   Search for recently merged PRs on {TARGET_BRANCH}
2. Run: ./scripts/Get-CIStatus.ps1 -PRNumber {MERGED_PR} -Repository "dotnet/{REPO}"
3. Find the build with same job name that passed
4. Locate the Helix job ID (may need artifact download — see [azure-cli.md](azure-cli.md))

Return JSON: { "found": true, "buildId": N, "helixJob": "...", "workItem": "...", "result": "Pass" }
Or: { "found": false, "reason": "no passing build in last 5 merged PRs" }

If authentication fails or API returns errors, STOP and return the error — don't troubleshoot.
```

## Pattern 3: Extracting Merge PR Changed Files

**When:** A large merge PR (hundreds of files) has test failures — need the file list for the main agent to analyze.

**Delegate:**
```
List all changed files on merge PR #{PR_NUMBER} in dotnet/{REPO}.

Get the list of changed files for PR #{PR_NUMBER} in dotnet/{REPO}

For each file, note: path, change type (added/modified/deleted), lines changed.

Return JSON: { "totalFiles": N, "files": [{ "path": "...", "changeType": "modified", "linesChanged": N }] }
```

> The main agent decides which files are relevant to the specific failures — don't filter in the subagent.

## Pattern 4: Parallel Artifact Extraction

**When:** Multiple builds or artifacts need independent analysis — binlog comparison, canceled job recovery, multi-build progression.

**Key insight:** Launch one subagent per build/artifact in parallel. Each does its mechanical extraction independently. The main agent synthesizes results across all of them.

**Delegate (per build, for binlog analysis):**
```
Download and analyze binlog from AzDO build {BUILD_ID}, artifact {ARTIFACT_NAME}.

Steps:
1. Download the artifact (see [azure-cli.md](azure-cli.md))
2. Load the binlog, find the {TASK_NAME} task invocations, get full task details including CommandLineArguments.

Return JSON: { "buildId": N, "project": "...", "args": ["..."] }
```

**Delegate (per build, for canceled job recovery):**
```
Check if canceled job "{JOB_NAME}" from build {BUILD_ID} has recoverable Helix results.

Steps:
1. Check if TRX test results are available for the work item. Parse them for pass/fail counts.
2. If no structured results, check for testResults.xml
3. Parse the XML for pass/fail counts on the <assembly> element

Return JSON: { "jobName": "...", "hasResults": true, "passed": N, "failed": N }
Or: { "jobName": "...", "hasResults": false, "reason": "no testResults.xml uploaded" }
```

This pattern scales to any number of builds — launch N subagents for N builds, collect results, compare.

## Pattern 5: Build Progression with Target HEAD Extraction

**When:** PR has multiple builds and you need the full progression table with target branch HEADs.

**Delegate (one subagent per build):**
```
Extract the target branch HEAD from AzDO build {BUILD_ID}.

Fetch the checkout task log (typically LOG ID 5, starting around LINE 500+ to skip git-fetch output)

Search for: "HEAD is now at {mergeCommit} Merge {prSourceSha} into {targetBranchHead}"

Return JSON: { "buildId": N, "targetHead": "abc1234", "mergeCommit": "def5678" }
Or: { "buildId": N, "targetHead": null, "error": "merge line not found in log 5" }
```

Launch one per build in parallel. The main agent combines with the build list to build the full progression table.

## General Guidelines

- **Use `general-purpose` agent type** — it has shell + MCP access for Helix, AzDO, binlog, and GitHub queries
- **Run independent tasks in parallel** — the whole point of delegation
- **Include script paths** — subagents don't inherit skill context
- **Require structured JSON output** — enables comparison across subagents
- **Don't delegate interpretation** — subagents return facts, main agent reasons
- **STOP on errors** — subagents should return error details immediately, not troubleshoot auth/environment issues
- **Use SQL for many results** — when launching 5+ subagents or doing multi-phase delegation, store results in a SQL table (`CREATE TABLE results (agent_id TEXT, build_id INT, data TEXT, status TEXT)`) so you can query across all results instead of holding them in context
- **Specify `model: "claude-sonnet-4"` for MCP-heavy tasks** — default model may time out on multi-step MCP tool chains
