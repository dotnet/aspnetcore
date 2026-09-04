# Skill evaluations

This directory contains evaluation-only assets for skills shipped from
`.github/skills`. Runtime skill instructions and references stay under
`.github/skills`; eval specifications, their fixtures, runners, and validators
stay here.

## Layout and discovery

`eng/skill-evals/<skill>/eval.vally.yaml` is the standard one-skill lane,
auto-discovered by this repository's experiment and runner.
`skills-vs-baseline.experiment.yaml` runs each of those specs twice with the
same stimuli: once without a skill and once with exactly
`.github/skills/<skill>` loaded. The experiment owns `environment.skills`;
standard specs must not set it themselves.

Any other `*.vally.yaml` file is a specialized suite. Specialized suites are not
auto-discovered and must be passed explicitly to the runner. Keep fixture files
beside the spec that consumes them, normally under a `fixtures` directory. Do
not place eval specs, `evals` directories, or eval runners in runtime skill
directories. A runtime skill may use a `fixtures` directory for non-eval assets.

The experiment deliberately does not override `runs`. A standard spec owns its
trial count through `defaults.runs`. The existing specs retain five runs and 25
trials each. The dotnet/skills quality gate uses five trials as the minimum at
which a clean sweep can support a one-sided sign test at 5%; enforcing a
trial-count policy here remains a documented follow-up.

## Local entry point

Run these commands from any directory:

```powershell
# Safe default: deterministic checks with no model or judge calls
./eng/skill-evals/run.ps1

# Prove every validator rule and runner isolation with self-tests
./eng/skill-evals/run.ps1 Test

# Strict Vally parsing/schema lint; does not call a model
./eng/skill-evals/run.ps1 Lint

# Model-bearing operations are explicit and are not part of validation
./eng/skill-evals/run.ps1 Run
./eng/skill-evals/run.ps1 Run -Eval eng/skill-evals/review-public-api/eval.vally.yaml
./eng/skill-evals/run.ps1 Run -Eval eng/skill-evals/review-public-api/eval.vally.yaml -Experiment eng/skill-evals/skills-smoke.experiment.yaml
./eng/skill-evals/run.ps1 Run -Eval eng/skill-evals/<skill>/<specialized>.vally.yaml
```

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
Vally output as a workflow artifact for seven days. The shared environment
provides `COPILOT_PAT_0`; same-repository `write`, `maintain`, or `admin` access
is the authorization boundary for selecting and running a host workflow
revision. The in-file default-ref checks prevent accidental non-default
dispatches of the unmodified workflow, but a trusted writer could deliberately
change those checks in a branch-selected workflow revision.
The fine-grained PAT grants only `Copilot Requests (Read)` for public
repositories and expires after eight days. It materializes only in the Vally
execution step as `COPILOT_GITHUB_TOKEN`; checkout, target resolution, staging,
artifact upload, and reporting never receive it.

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
destinations, and missing inputs fail closed. The trusted runner and assertion
execute from that staged tree, so candidate changes to `run.ps1`,
`assert_results.ps1`, central experiments, or the workflow cannot gain code
execution in the PAT-backed step. Both baseline and skilled variants must
produce the exact planned result count, successful trial statuses, and grader
scores; only the skilled score is threshold-gated. The final status and PR
comment link to the retained artifacts. Smoke results validate execution and
the skilled threshold, but Full runs remain the quality-evidence path.

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

`Validate`, `Lint`, and `Run` use the exact
`@microsoft/vally-cli@0.13.0` package through `npx` and the Microsoft package
feed proxy. Pass `-Vally <command> -VallyPrefix <arguments>` only to
intentionally override that invocation. The runner prints the resolved command
and reported version for provenance. Additional Vally arguments can be appended
to the command. If the package is not already cached, `npx` downloads that
exact version from the proxy; validation is model-free, not offline. It does not
install a package into the repository or modify dependency manifests. Run
output defaults to `artifacts/skill-evals`.

Standard runs use Vally's experiment `--compare` mode. Vally 0.13 removed the
old per-stimulus `pairwise` grader, so comparison is owned by the experiment
rather than repeated in each eval spec.

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
