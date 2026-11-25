# Code review regression corpus

This public corpus is a **regression test and an upgrade gate** for `/review`.
It is separate from the `eval.vally.yaml` smoke test. Losing catches or adding
control findings warrants investigation. A higher score on known cases is
weak evidence of improvement. Watch production `/review` for a sharp drop.

Only a quarterly fresh batch measures quality. Harvest 10 to 15 new bug cases
from merged PRs where `/review` had not run when the maintainer commented.
Do not look at `/review` output while selecting cases. Add about five fresh
clean PRs; a person reads any findings on those controls. Stage the batch in
a local copy of `cases.json` and score it once before anyone reads the misses.
Then add it to the public corpus. Label each case `fresh batch` or
`added for a rule`; the runner reports them separately.

One batch is a rough reading. With 10 to 15 bug cases run once, the catch
rate is uncertain by 20 percentage points or more. Look at the trend over
three or four batches.

The corpus never runs automatically. A person starts every paid run. CI runs
only model-free harness checks.

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
workflow. There are three deliberate differences: the bundle path is local,
the hosted publication adapter and tools are omitted, and the bundles are
code-only with guidance files stripped. Hosted bundles keep guidance files
and include existing PR comments. This corpus PR does not edit the shared
eval lockfile. A separate PR bumps the shared tooling to the `/review` CLI.

Select explicit case IDs to check the setup or score a fresh batch. Each
selected case gets one trial:

```bash
node "$runner" prepare "$scratch/sample" case-01 case-04
node "$runner" review "$scratch/sample"
node "$runner" collect "$scratch/sample"
node "$runner" judge "$scratch/sample"
node "$runner" score "$scratch/sample"
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

`review` is the paid Vally stage. It runs only on the local backend. The reviewer
sees the skill, prompt and neutral, frozen source bundle, not this README, PR
mappings or rubrics. The executor checks the effective tool allowlist before each turn,
and the runner checks the native trace for successful skill loading and no
subagents. This is a tool restriction, not an OS sandbox, and does not
exercise hosted publication or live GitHub access. Treat the archived code
as evidence only; never execute it.

`collect` reads the saved Vally results and native traces without spending
credits. It writes one row per planned trial to `trials.json`, including
blocked, incomplete, malformed, missing, or invalid results. These are misses
for must-find cases and not clean results for controls. A failed trial does
not erase the rest of the run. Collect even if `review` exits nonzero.

`judge` is paid only when findings need semantic matching. The no-tools
matcher sees one review's findings and the corresponding rubrics, not the
source or case ID. Its matches are a triage signal, not a source verdict.
`score` is free. It uses `trials.json` and saved judge receipts to write
`summary.json` and print new control finding text. A missing judge receipt
leaves the trial unscored and prevents a gate pass. You can collect or score
again without rerunning the review. Use a new directory only for a new paid
review.

## When to run each tier

At about 26 credits per review, budget about 470 credits for the quick tier
and 2,100 for the full tier. Actual cost varies. The 38-credit validation
included a failed ingress attempt; it is not a per-review rate.

**Quick:** Use for a rule edit or CLI bump. Run the seven always-caught cases
from [`baseline.json`](baseline.json) and all 11 controls, one trial each.
Pass with at least six catches, no forbidden claim, and no more than one
ordinary finding above Run A on controls that completed in both runs. With
all comparable controls complete, that limit is three. An unfinished
candidate review for a historically completed control prevents a pass;
it does not count as clean. Run A was blocked on `case-05`, so the runner
reports it separately and excludes it from both compared totals. The rule
shipping bar is stricter: no new control findings. Read any unpaired control
findings before shipping; compare them to the frozen source.

```bash
node "$runner" prepare "$scratch/quick" --quick
node "$runner" review "$scratch/quick"
node "$runner" collect "$scratch/quick"
node "$runner" judge "$scratch/quick"
node "$runner" score "$scratch/quick"
```

**Full:** Use for a model or review-method change and once a quarter with the
fresh batch. Skip the quarterly full run if the skill, model, CLI and review
method are unchanged since the last full run; still run the fresh batch.
Run the 23 scored cases three times each and the 11 controls once each.
The two cases marked `excluded` stay in the corpus for the record; neither
tier nor a direct case selection runs them. Allow tens of gigabytes for
bundles and workspaces, not a RAM-backed `/tmp`.

```bash
node "$runner" prepare "$scratch/full" --full
node "$runner" review "$scratch/full"
node "$runner" collect "$scratch/full"
node "$runner" judge "$scratch/full"
node "$runner" score "$scratch/full"
```

A previously always-caught case is lost if it is caught in one or zero of
three trials. One or two cases lower is noise on its own. Replay only the
dropped case (`prepare <new-run-dir> --replay <case-id>`) before calling it a
regression. Run A is a historical reference, not a same-configuration
comparison. Record the skill hash, reviewer settings, and CLI pin for
upgrade comparisons.

## Tune from production misses

Each case pins one commit. Each trial reviews that commit in isolation,
even if the PR had multiple review rounds. A follow-up will pair must-find
cases with cases at their fix commits, where the fixed issue must not be
raised again.

1. Start with a bug a maintainer flagged that `/review` missed, or a comment a
   maintainer explicitly called wrong. Declined or resolved is not enough.
   The maintainer's statement is the ruling. An agent collects the quote
   and a person spot-checks it. Add one rule after two misses of the same
   kind on different PRs, or one explicitly wrong claim.
2. Admit a case only after verifying the maintainer's claim on the exact
   reviewed head and checking that it concerns changed code. Pin that head,
   the base tip, merge base, and diff digest. For a missed bug, pin the fix
   commit in `fixCommit` and verify that it addresses the same
   mechanism and material effect. Record the effect in the catch rubric.
   For a wrong claim, record it in `mustNotRaise` and verify from frozen
   source why it is wrong. A match in any trial fails the case. Other
   findings on that case count as control findings.
3. Ship a rule only if it fixes its triggering cases, passes the quick check,
   and adds no new adjudicated control findings.
4. Cap the skill at 15 rules. To test removing a rule, delete it and replay
   its own cases. If they still pass, keep it out.
