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
itself. Automatic runs never invoke a model or judge.

Maintainers can also dispatch `Validate`, `Test`, or `Lint` manually. The
model-bearing `Run` action requires selecting one standard skill and defaults to
the one-run-per-stimulus smoke experiment. Full runs retain the standard spec's
trial count. Both modes run through the `copilot-pat-pool` environment with one
worker, serialize model-bearing runs, and retain the raw Vally output as a
workflow artifact for seven days. The environment must provide `COPILOT_PAT_0`
and allow the selected workflow ref. The hosted quality gate evaluates the
`skilled` variant independently: a weak baseline is expected and does not fail
the workflow, while incomplete trials or a skilled score below its threshold do.

After the workflow is present on the repository's default branch, maintainers
with `write`, `maintain`, or `admin` permission can request a smoke evaluation
for an open, same-repository pull request:

- Recommended: submit a PR review whose body starts with `/evaluate`. GitHub
  supplies the exact reviewed commit.
- In the PR conversation, comment `/evaluate <sha>`. A bare `/evaluate` posts
  guidance because an `issue_comment` event does not identify a commit.

The gate resolves the full commit, verifies it belongs to the PR, rejects fork
content, and discovers standard evals affected by the change. A central runner,
experiment, or workflow change selects every standard eval. The gate posts one
pending `skill-evaluations` commit status so duplicate requests for the same
commit normally stop at the gate. After acquiring the global model lane, the
worker verifies that its run still owns the pending status before exposing PR
content to the PAT-backed step, then rechecks after a short stabilization window
that lets the preceding run's separate reporter replace any racing claim. A
racing request therefore cannot cause a second model run. The model job has
read-only repository permissions, checks out the validated commit, runs affected
evals serially, and publishes a final commit status and PR comment linking to the
retained artifacts. Smoke results validate execution and the skilled threshold,
but Full runs remain the quality-evidence path.

`workflow_dispatch` remains the first control surface. Supplying both
`pr_number` and `head_sha` exercises the same exact-SHA PR gate while the
selected `eval` acts as a bounded override; omitting them preserves the original
one-skill manual run. Comment and review events always load workflow YAML from
the default branch. The secret-bearing job still executes eval and skill content
from the validated PR commit, so same-repository write access is an explicit
trust boundary. Use required reviewers on `copilot-pat-pool` if repository write
access alone should not authorize that execution.

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
