# Skill evaluations

This directory contains evaluation-only assets for skills shipped from
`.github/skills`. Runtime skill instructions and references stay under
`.github/skills`; eval specifications, their fixtures, runners, and validators
stay here.

## Layout and discovery

`eng/skill-evals/<skill>/eval.vally.yaml` is the standard one-skill lane,
auto-discovered by this repository's experiment and runner.
`skills-vs-baseline.experiment.yaml` runs the generic skill specs twice with the
same stimuli: once without a skill and once with exactly
`.github/skills/<skill>` loaded. The experiment owns `environment.skills`;
standard specs must not set it themselves. Reviewer comparison uses
`review-vs-baseline.experiment.yaml` and `review-smoke.experiment.yaml` instead.
The verifier component suite has no skill arm and runs directly.

Any other `*.vally.yaml` file is a specialized suite. Specialized suites are not
auto-discovered and must be passed explicitly to the runner. Keep fixture files
beside the spec that consumes them, normally under a `fixtures` directory. Do
not place eval specs, `evals` directories, or eval runners in runtime skill
directories. A runtime skill may use a `fixtures` directory for non-eval assets.

The standard `review-pull-request` suite uses frozen source and diff excerpts from
ASP.NET Core PRs #70106, #69782, #70304, #69721, and #70299. These cover exact-input
old/head comparison, constructor mapping versus a lower-layer head test, renderer
disposal and issue intent, a browser-test wrapper ordering that contradicts a
claimed token-exchange dependency, and existing feedback on render-mode-dependent
navigation. Each case carries its source roles,
Git blob identities, original line ranges, and fixture SHA-256 hashes in
`provenance.json`. Issue and feedback excerpts identify their captured public
revision metadata. Neutral stimulus names and destination paths do not encode
expected dispositions. A sixth case integrates the synthetic public catch-policy
example from #70312. It omits the explicit missing-policy hints and the redundant
existing-question case. It does not represent production framework source.
It replaces the retired standalone catch-policy suite and its duplicate fixtures.
Fixture attributes retain LF and upstream whitespace so Git checkout preserves
the frozen hashes on Windows and Unix.

These are offline single-context candidate exercises, not full bundle preparation,
guide-worker dispatch, verifier integration, live PR review, or browser tests.
Both arms share frozen source and both guidance documents. Only the skilled arm
receives the skill and verifier/worker instructions. Baseline does not receive
`SKILL.md` or either agent profile. `tool-calls.required` matches both read arguments and
returned bytes, including actual skill text from native acquisition or an installed
file read. A success acknowledgement or a filename citation does not satisfy those
checks. Content signatures prove the specified bytes appeared in a tool result,
not that every possible review path ran.

Only the deterministic program grader contributes to disposition scores. It checks
the status line, C1's section placement, inline confidence, and existing-thread
deduplication. Skill acquisition, read receipts, and LLM rubrics have zero weight.
The result gate checks evidence/guidance receipts for both arms and skill/profile
receipts for skilled only. A missing baseline skill cannot lower its disposition
score. Each skilled stimulus must independently meet its threshold. Baseline
disposition scores are reported, not threshold-gated.

The zero-weight LLM rubrics remain secondary diagnostics, not semantic quality
gates. They still incur judge calls during a model-bearing run. The subject and
judge both use `gpt-5.6-sol`; the judge is not independent-model corroboration.
These public development cases are not held-out evidence. Five repeats do not
create five independent PR lineages.

The `review-candidate-verifier` component suite checks normalized `decision` and
applicable `downgradeKind`, `dropBasis`, and `contradictedLink` fields. It tolerates
irrelevant JSON and citation-format differences. Every case must pass at least
four of five trials independently. Read receipts remain a separate integrity
gate. Case a uses public C# source. Case b adds an assigned synthetic propagation
contract, not an actual public API guarantee. Case c uses a synthetic JavaScript
component and an immutable observation artifact. Component attributes preserve the exact supplied
bytes, including existing line endings. Provenance records hashes and case kinds.

Cases a, b, and c scored 5/5, 5/5, and 5/5 (15/15 disposition and 15/15
`evidence-reads`) on the verifier text in this PR, including the merged #70319
guard clauses and the tightened secondary-failure `contract` rule. A fourth
policy-only head-test scenario (close to @maraf's 3/10 `DROP(unestablished)` /
7/10 `DOWNGRADE(intent)` synthetic fixture) was omitted from the component gate;
`SKILL.md` retains the coordinator backstop mapping `DOWNGRADE(intent)` with
`contract: "n/a"` to `UNRESOLVED`, which has not been measured end to end, and
the 30-trial-per-arm L1 `review-pull-request` model suite was not rerun on the
final patch.

Model-free validation checks layout, schema, source hashes, read-guard coverage,
and experiment selection. It does not establish adjudication quality. A
model-bearing `Run` and inspection of every trial's actual read receipts and
semantic result remain necessary before making a behavioral claim.

The experiment deliberately does not override `runs`. A standard spec owns its
trial count through `defaults.runs`. The reviewer suite has 30 trials per arm.
The component suite has 15 trials. The dotnet/skills quality gate uses five trials as the minimum at
which a clean sweep can support a one-sided sign test at 5%; enforcing a
trial-count policy here remains a documented follow-up.

## Local entry point

Run these commands from any directory:

```powershell
# Safe default: deterministic checks with no model or judge calls
./eng/skill-evals/run.ps1

# Prove every validator rule and runner isolation with self-tests
./eng/skill-evals/run.ps1 Test

# Select this suite for layout, lint, and both experiment dry-runs.
./eng/skill-evals/run.ps1 Validate -Suite review-pull-request
./eng/skill-evals/run.ps1 Validate -Suite review-candidate-verifier
# Run infrastructure self-tests plus the selected suite's fixture checks.
./eng/skill-evals/run.ps1 Test -Suite review-pull-request
# The original staging self-test also has this forwarding entry point.
./eng/skill-evals/tests/test_stage_run.ps1

# Strict Vally parsing/schema lint; does not call a model
./eng/skill-evals/run.ps1 Lint

# Model-bearing operations are explicit and are not part of validation
./eng/skill-evals/run.ps1 Run
./eng/skill-evals/run.ps1 Run -Eval eng/skill-evals/review-public-api/eval.vally.yaml
./eng/skill-evals/run.ps1 Run -Eval eng/skill-evals/review-public-api/eval.vally.yaml -Experiment eng/skill-evals/skills-smoke.experiment.yaml
./eng/skill-evals/run.ps1 Run -Suite review-candidate-verifier
./eng/skill-evals/run.ps1 Run -Eval eng/skill-evals/<skill>/<specialized>.vally.yaml
```

Without a suite or explicit experiment, `Run` dispatches each standard suite into
its own output subdirectory. This includes reviewer comparison and the direct
verifier component suite. `assert_results.ps1` checks that combined output
recursively. A direct component run writes a plan beside Vally's timestamped
`results.jsonl`; it does not invent an experiment variant directory. For the
reviewer and verifier suites, smoke selects one repeat per case and checks trial
completion, grader scores, and read-integrity receipts without enforcing the
multi-trial per-stimulus disposition threshold.

## Hosted entry point

`.github/workflows/skill-evals.yml` runs `Validate` automatically when pull
requests or pushes to `main` change runtime skills, eval assets, or the workflow
itself. Validation parses and dry-runs both the standard and smoke experiments
without invoking a model or judge.

Maintainers can also dispatch `Validate`, `Test`, or `Lint` manually. The
model-bearing `Run` action requires selecting one standard skill and defaults to
the one-run-per-stimulus smoke experiment. Full runs retain the standard spec's
trial count. Both modes use the repository-scoped `copilot-pat-pool`
environment with one worker, serialize model-bearing runs, and retain the raw
Vally output as a workflow artifact for seven days. A dedicated trusted job
finds the configured `COPILOT_PAT_0` through `COPILOT_PAT_9` pool entries,
randomly selects one, and exposes only its numeric slot to the worker. The Vally
step resolves that slot to the selected secret, or uses the repository
`COPILOT_GITHUB_TOKEN` when the pool is empty. Same-repository `write`,
`maintain`, or `admin` access is the authorization boundary for selecting and
running a host workflow revision. The in-file default-ref checks prevent
accidental non-default dispatches of the unmodified workflow, but a trusted
writer could deliberately change those checks in a branch-selected workflow
revision.
Pool entries are fine-grained PATs that grant only `Copilot Requests (Read)` for
public repositories and expire after eight days. The selected credential
materializes only in the Vally execution step as `COPILOT_GITHUB_TOKEN`;
checkout, target resolution, staging, artifact upload, and reporting never
receive it.

After the workflow is present on the repository's default branch, maintainers
with `write`, `maintain`, or `admin` permission can request a smoke evaluation
for an open, same-repository pull request:

- In the PR conversation, comment `/evaluate <sha>`. A bare `/evaluate` posts
  guidance because an `issue_comment` event does not identify a commit. This
  event loads the workflow from the default branch rather than the PR merge
  context.

Requests from actors whose repository permission cannot be verified or is below
`write` are logged as notices and ignored before any PR or model work begins.
Repository `write`, `maintain`, or `admin` permission is the authorization
boundary for model-bearing requests. The gate resolves the full commit, verifies
it belongs to the PR, refuses fork content, and discovers standard evals affected
by the change. A central runner, experiment, or workflow change selects every
standard eval, but candidate control-plane files are never executed. The gate
posts one pending `skill-evaluations` commit status so duplicate requests for the
same commit normally stop at the gate.

After acquiring the global model lane, the worker verifies that its run still
owns the pending status, then rechecks after a short stabilization window that
lets the preceding run's separate reporter replace any racing claim. A racing
request therefore cannot cause a second model run. The worker checks out
`github.workflow_sha` as the trusted control plane and the validated PR commit
as a separate exact-SHA candidate. For each selected eval, it creates a clean
temporary tree containing only the trusted runner, assertion, and central
experiments plus the candidate's selected skill, `eval.vally.yaml`, and fixture
tree. Symlinks, reparse points, path traversal names, source-nested staging
destinations, and missing inputs fail closed. Staging also rejects a candidate
`eval.vally.yaml` that declares a `commands`, `mcpServers`, or
`executor.config.provider` field, since those let an eval run arbitrary
commands, start MCP servers, or redirect model calls; staged fixture files have
their executable bit set automatically instead. The trusted runner and assertion
execute from that staged tree, so candidate changes to `run.ps1`,
`assert_results.ps1`, central experiments, or the workflow cannot gain code
execution in the PAT-backed step. Both baseline and skilled variants must
produce the exact planned result count, successful trial statuses, and grader
scores; only the skilled score is threshold-gated. The final status and PR
comment link to the retained artifacts. Smoke results validate execution,
read-integrity receipts, and generic aggregate thresholds, while Full runs
enforce per-stimulus review and verifier thresholds and remain the
quality-evidence path.

Vally and Copilot still interpret the staged candidate skill and eval stimuli as
agent instructions inside the token-bearing execution step. The staging boundary
also does not defend against a trusted writer deliberately modifying the host
workflow revision itself. Authorizing repository writers is therefore an
explicit trust decision: a malicious writer could attempt to disclose the
Copilot token through either surface. The repository-scoped token's single read
permission, public-repository restriction, and eight-day expiry bound that
accepted risk.

`workflow_dispatch` remains the first control surface. PAT-backed dispatches
must select the repository default branch; selecting a feature branch fails
before candidate checkout or token materialization when the unmodified workflow
is running. Supplying both `pr_number` and `head_sha` exercises the same
exact-SHA PR gate while the selected `eval` acts as a bounded override; omitting
them preserves the original one-skill manual run. Comment events load workflow
YAML from the default branch. Candidate skill instructions and eval data come
from the validated exact SHA, while every executable control-plane file comes
from the selected host workflow revision.

Environment deployment-branch policies or required reviewers are optional,
broader hardening if repository owners decide write access alone should not
authorize PAT use. Because `copilot-pat-pool` is shared, apply that decision
consistently across all workflows that consume the environment rather than
uniquely to this workflow.

Hosted `Validate`, `Lint`, and `Run` operations use the toolchain pinned by
`evaluation-tools/package-lock.json`, including `@microsoft/vally-cli@0.19.0`
and `@github/copilot@1.0.80`. The manifest also constrains transitive Copilot
packages to that compatible version so the SDK and native platform package
retain the same `./sdk` entry point. The workflow installs that lockfile with
`npm ci` before any Copilot credential enters scope and verifies the Vally,
Copilot, and Linux x64 platform entry points.

The local runner defaults to the installed pinned toolchain when present.
Local model-bearing `Run` operations require
`npm ci --prefix eng/skill-evals/evaluation-tools` first. Model-free validation
can still download the exact Vally version through `npx` and the Microsoft
package feed proxy without modifying the checked-in manifests. Set
`SKILL_EVAL_VALLY` to another installed Vally executable, or pass
`-Vally <command> -VallyPrefix <arguments>`, to intentionally override that
invocation. The runner prints the resolved command and reported version for
provenance. Additional Vally arguments can be appended to the command. Run
output defaults to `artifacts/skill-evals`.

Skill-comparison runs use Vally's experiment `--compare` mode. Vally 0.13 and later do
not use the old per-stimulus `pairwise` grader, so comparison is owned by the
experiment rather than repeated in each eval spec.

## Result interpretation and provenance

An incomplete run, an unavailable model or judge, a timeout, an authentication
failure, or too few completed trials is an infrastructure/inconclusive result,
not evidence that the skill failed. A quality conclusion requires completed
baseline and skilled trials under the same inputs and identities.

Retain the raw Vally output and enough provenance to reproduce a conclusion:

- repository commit and whether the worktree was dirty;
- eval and experiment file paths and revisions;
- Vally version and full invocation;
- executor/tool identity and version;
- model and judge model identities;
- timestamps, run counts, retries, and incomplete trials.

Defaults in each standard spec identify its model and judge. CLI overrides are
allowed for an intentional run, but the override and resulting identities must
remain in the saved provenance. Do not compare runs whose relevant identities
or inputs differ without calling out that difference.

## Validation boundaries

Pinned Vally owns YAML parsing, duplicate-key rejection, and eval/grader schema
validation. Default `Validate` also resolves the experiment with Vally
`--dry-run`, so experiment YAML, variants, and eval discovery are checked
without model or judge calls. The parser-free repository checks cover standard
eval-to-skill mapping, git-tracked eval specs and fixture trees, symlink-free
fixtures, and separation of eval specs and `evals` directories from runtime
skills. The self-tests inject every repository-layout failure class and
exercise runner dispatch without model calls.

Checks that require interpreting eval YAML remain deferred until Vally exposes a
stable machine-readable validation contract or this repository has enough
demonstrated failures to justify a repo-native parser. These include
reference-specific fixture/path validation, trial-count policy, standard eval
skill-selection ownership, model/judge policy, and answer-material staging
rules. Do not approximate those checks with prose matching.

Specialized suites own case promotion, consolidation, retirement, and held-out
refresh to keep coverage representative and bounded.

Validation does not judge prompt or rubric quality, run a model, validate
runtime skill behavior, or decide whether a specialized suite is statistically
persuasive. Those concerns belong in skill-specific review and runtime
validation. Scheduled model evals and cross-repository comparison adapters are
deliberate follow-ups.
