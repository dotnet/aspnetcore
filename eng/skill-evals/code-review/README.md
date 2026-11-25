# Code review regression corpus

The standard `eval.vally.yaml` is a smoke test for the skill. This separate,
manual corpus run checks whether changes to the reviewer regress its ability
to find material bugs on frozen ASP.NET Core diffs. The public cases contain
25 historically labeled must-find cases (23 primary and two exploratory)
and 11 screened controls. The exploratory cases remain available for
comparison but do not count toward the primary catch score: one label
conflicted with the merged behavior, and the other was a narrow test gap
below the materiality bar.

This is a **public regression set**, not an estimate of review quality.
Losing catches or producing new findings on controls warrants investigating.
Gains are weaker evidence because the cases are public. Watch production
`/review` outcomes for a sharp drop; they are not a gate for each rule change.

## Run locally

Use an unprivileged user on Linux or WSL with the checkout and run directory on
the Linux filesystem. Use a full clone (no `--depth` or `--filter`),
Node.js 22 or later, and the
repository's [build prerequisites](../../../docs/BuildFromSource.md).
The commands below start at the checkout root:

```bash
./eng/common/dotnet.sh
source ./activate.sh
export VALLY_TOOLS="$PWD/eng/skill-evals/evaluation-tools"
npm ci --prefix "$VALLY_TOOLS"

runner=eng/skill-evals/code-review/run.mjs
cli_version=$(node "$runner" cli-version)
test -n "$cli_version"
scratch=$(mktemp -d "$HOME/code-review-eval.XXXXXXXX")
cli_package="@github/copilot-linux-$(node -p 'process.arch')"
npm install --prefix "$scratch/cli" --no-save --no-package-lock "$cli_package@$cli_version"
export COPILOT_CLI_PATH="$scratch/cli/node_modules/$cli_package/copilot"
"$COPILOT_CLI_PATH" --version
```

Provide Copilot authentication through the environment (`COPILOT_GITHUB_TOKEN`
or `GH_TOKEN`). Reviewer and judge sessions use isolated CLI homes; do not
copy account credentials into a run directory. The runner takes its analysis
prompt, model, CLI pin, and read/skill tool list from the checked-out `/review`
workflow. It adapts the frozen bundle path and omits the hosted publication
instructions and tools. The shared tool lockfile is unchanged.

To check the setup or reproduce a specific miss without running a tier, select
one or two cases. Each selected case gets one trial:

```bash
node "$runner" prepare "$scratch/sample" case-01 case-04
node "$runner" review "$scratch/sample"
node "$runner" judge "$scratch/sample"
```

`prepare` does not use a model. It fetches missing pins from the public
repository, checks the recorded merge bases and diff hashes, and rebuilds
code-only bundles with the existing `prepare-review.cs` producer. A cached Git
directory or bare store can be supplied through `REVIEW_OBJECT_STORE`. For a
reduced cache without complete history, preparation uses the recorded merge
base and still checks the diff hash. Preparation fails if the pinned Git
objects or their trees cannot be recovered; do not substitute a newer PR head.
The pinned base tips are comparison inputs, not a claim that each was the
actual base tip at the time of the historical review.

`review` runs only on the local Vally backend. The reviewer sees the skill,
prompt and neutral, frozen source bundle, not this README, PR mappings or
rubrics. The executor checks the effective tool allowlist before each turn,
and the runner checks the native trace for successful skill loading and no
subagents. This is a tool restriction, not an OS sandbox, and does not
exercise hosted publication or live GitHub access. Treat the archived code
as evidence only; never execute it. `judge` sends only one review's findings
and its rubrics to a no-tools matcher; it does not re-review source or see the
case ID. Its matches are a triage signal, not independent source adjudication.

`summary.json` records per-case catches or control finding counts, and the
stages retain findings, native traces and judge receipts for investigation.
An unfinished review or an invalid trace fails the run rather than becoming
a miss or a clean control. Use a new run directory for a rerun; inspect the
saved evidence first because an incomplete run may already have incurred
charges. Control findings are not automatically false positives: compare
their substance to the baseline and adjudicate them against the frozen diff.

## When to run each tier

Both tiers are manual, **not per-PR CI**. Actual cost depends on the workflow's
model and reviewer output. Compare the candidate with a saved baseline that
uses the same settings except for the change under evaluation.

**Quick check (~270 AI credits):** Seven historically always-caught cases and
three controls, one trial each. Use this for a rule edit or CLI bump. Require
at least six catches and no new adjudicated control findings. The summary
checks the catch threshold; compare control findings with the saved baseline
manually.

```bash
node "$runner" prepare "$scratch/quick" --quick
node "$runner" review "$scratch/quick"
node "$runner" judge "$scratch/quick"
```

**Full run (~2,400 AI credits):** Use for a model or review-method change and
after every few rule changes. Allow tens of gigabytes for bundles and preserved
workspaces; do not put a full run on a RAM-backed `/tmp`.

```bash
node "$runner" prepare "$scratch/full" --full
node "$runner" review "$scratch/full"
node "$runner" judge "$scratch/full"
```

The full run repeats each of the 25 historically labeled cases three times
and each control once, matching the original corpus run. A previously
always-caught case is lost if the candidate catches it in only one or zero of
three trials. Rerun a dropped case before calling it a regression. Record the
skill hash, reviewer settings, and CLI pin when comparing runs.

## Tune from production misses

1. Start with a bug a maintainer flagged that `/review` missed, or a claim a
   maintainer explicitly called wrong. Declined or ignored feedback alone is
   not a label. Add one rule after two misses of the same kind on different
   PRs, or one explicitly wrong claim.
2. Admit a case only after verifying the maintainer's claim on the exact
   reviewed head and checking that it concerns changed code. Pin that head,
   the base tip, merge base, and diff digest. For a missed bug, pin the fix
   commit in `fixCommit` and verify that it addresses the same
   mechanism and material effect. Record the effect in the catch rubric.
   For a wrong claim, record the exact claim in `mustNotRaise` for manual
   adjudication and verify from frozen source why it is wrong.
3. Ship a rule only if it fixes its triggering cases, passes the quick check,
   and adds no new adjudicated control findings.
4. Compare full runs periodically. Remove rules that do not improve their
   triggering cases or prevent other regressions.
