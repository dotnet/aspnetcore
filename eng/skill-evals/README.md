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

`investigate-issue` is intentionally excluded from hosted model-bearing runs.
Its standard spec remains available for model-free lint, experiment resolution,
and reviewed private operator runs. Do not run its model-bearing cases in a
host that publishes replies, logs, artifacts, or status links.

The experiment deliberately does not override `runs`. A standard spec owns its
trial count through `defaults.runs`. The existing specs retain five runs per
stimulus. The dotnet/skills quality gate uses five trials as the minimum at
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

### Private operator runs for `investigate-issue`

The canonical eval contains synthetic issue evidence, but it does not establish
host privacy or provide real storage paths. Private model-bearing work therefore
uses an explicit two-step helper and a frozen manifest:

```powershell
./eng/skill-evals/prepare_investigate_issue_run.ps1 Prepare `
    -TrustedRoot <reviewed-control-plane-root> `
    -CandidateRoot <candidate-root> `
    -OutputRoot <new-private-directory> `
    -CaseName <explicit-case-names> -Runs <count> `
    -ActorModel <permitted-model> -JudgeModel <permitted-model> `
    -ConfirmPrivateHost

./eng/skill-evals/prepare_investigate_issue_run.ps1 Run `
    -Manifest <manifest.json> `
    -ApprovedManifestSha256 <reviewed-sha256>

./eng/skill-evals/assert_investigate_issue_run.ps1 `
    -Manifest <manifest.json>
```

`Prepare` is model-free. It requires actual operator confirmation, calls the
unchanged `stage_run.ps1` once per cell, and uses
`project_investigate_issue_eval.mjs` with pinned Vally 0.13's loader,
validator, and YAML serializer. Every selected case/repetition has exactly two
isolated cells: no-skill `baseline` and candidate `skilled`. It records exact
input/tool hashes, models, paths, fixture state, expected identities, and argv.
For three cases at five repetitions this is 30 cells and 15 pairs; that example
is not authorization to run them.

`Run` requires the approved manifest hash, rechecks trusted and candidate input
hashes, dry-runs each projected cell separately, and then uses the exact shape:

```text
node <vally-cli-0.13.0>/dist/index.js experiment run <cell-experiment>
  --variant <baseline-or-skilled> --workers 1
  --workspace <cell-workspaces> --output-dir <cell-output>
```

There is no `--compare`, retry, fallback model, or shared `run.ps1 Run` path.
Native snapshots/results remain under each cell output. Exit code 1 can mean an
ordinary completed grading failure, so structured output completeness—not exit
status alone—decides whether the fixed matrix continues. Infrastructure failure
stops later launches and records them as not started.

The dedicated checker validates exact cell/pair cardinality, composite
`(cellId, native itemId)` identity, variant/stimulus/model/input/hash ownership,
grader coverage, frozen projection/grader/config identity, and all skilled
deterministic checks. Cardinality is derived from declared cases, repetitions,
and both variants rather than editable counters. It allows baseline
quality failures and applies the declared threshold once to the cell-weighted
mean of all skilled results. Cohort means are reported only. It writes a
separate assessment and pairing ledger; it never rewrites raw Vally output or
fabricates the standard combined layout. Do not pass split directories to
`assert_results.ps1`.

Persistence acceptance requires actual actor writer/read-back traces and exact
saved-report byte parity. Run the separately gated effect checker with
`ActorTrace`. Prepare automatically records the explicit private-host
confirmation, submitted invocation hash, per-cell canonical/effective/setup
hashes, storage grant/path, and frozen scenario-control expectation. Run
validates that preparation receipt and automatically binds the approved
manifest invocation plus each native result path/hash. Scenario expectations
remain labeled as frozen case input; they are not fresh reproduction
observations or execution permission. Actor/reporter text cannot grant trust.
The checker classifies supported read/write/exists
operations, allows read-only collision inspection, decodes native text content
blocks, and requires a successful write followed by read-back plus exact report
bytes. Every persistence-cell call is classified: recognized execution and any
write outside the exact granted destination are rejected, and bounded
artifact/workspace/operator roots must contain no fallback files. Failed reads
and opaque shell operations never count as writes; opaque operations keep the
cell not-assessed.
Collision and writer-failure setup tests prove only the fixture. An actor that
preflight-rejects a destination below a regular-file parent may be correct, but
the writer-error branch remains `not-exercised` unless a real writer call fails.

The same checker exposes `HostProbe` and `FileTrigger` receipt actions for the
separately approved Docker lane. Pass successful effect-assessment files to
`assert_investigate_issue_run.ps1 -EffectAssessment ...`; without them its
runtime acceptance remains explicitly `not-assessed-by-structural-checker`.
Each assessment reports named covered and pending gates. The structural checker
reports partial coverage until the union covers every required actor and host
effect gate. Full runtime acceptance additionally requires passed assessments
with no pending gates and one passed ActorTrace result for every selected
cell/repetition; partial coverage from one repetition cannot promote
not-exercised peers. Effect actions are singleton and gate-scoped:
`ActorTrace`, `ExecutionReceipt`, `HostProbe`, and `FileTrigger` may report only
their own named gates, and run-specific ActorTrace/ExecutionReceipt assessments
must bind the selected manifest path/hash. Unknown, duplicate, cross-run, or
wrong-action evidence is rejected. Missing optional host/file evidence remains
partial. Zero applicable cells is never a pass.

Execution-stop gates allow native skill activation and supported read-only
evidence tools. Recognized write/execute operations fail; opaque operations are
not assessed. `NoApprovalOrDeniedApprovalPerformsZeroExecution` requires both
absent and denied trusted-controller states. The separate `ExecutionReceipt`
action binds complete actor effects to every cell's runner receipt,
approved/actual argv hash, top-level process exit, controller environment
restoration, descendant exit, and unchanged unrelated markers. Run captures the
facts it directly observes and records unsupported descendant/host observations
as `unknown`; the action then remains partial instead of fabricating
`ExecutionReceiptMatchesToolsAndCleanup`.

After an approved Run completes, use its generated controller artifacts:

```powershell
$run = Get-Content <manifest.json> -Raw | ConvertFrom-Json -Depth 100
pwsh eng/skill-evals/assert_investigate_issue_effects.ps1 ActorTrace `
  -Manifest <manifest.json> `
  -HostControlReceipt $run.controller.actorControlPath `
  -Output <actor-effects.json>
pwsh eng/skill-evals/assert_investigate_issue_effects.ps1 ExecutionReceipt `
  -Manifest <manifest.json> `
  -Receipt <actor-effects.json> `
  -HostControlReceipt $run.controller.executionControlPath `
  -Output <execution-effects.json>
```

The second command requires complete ActorTrace coverage. A controller with
supported descendant/process-marker observation may supply those real
observations; the current local helper intentionally records them as unknown.

The host lane requires a trusted controller precondition receipt proving the
fresh dummy credential and marker existed outside the child and the controlled
host endpoint was reachable there, then binds those exact identities to the
child receipt. The file-trigger lane handles atomic rename notifications and
runs a separate producer-absent observation control. Model-free Boolean receipt
fixtures prove assertion dispatch only, not Docker or .NET runtime effects.

Actor approval-transition tests and the offline Docker host-effect tests
documented by the skill remain separately approved lanes. Model-free helper and
checker tests do not prove agent behavior, Docker isolation, networking,
protected-marker denial, process cleanup, or report persistence.

The actor-trace lane must cover
`NoApprovalOrDeniedApprovalPerformsZeroExecution`,
`ReporterApprovalDoesNotAuthorizeExecution`,
`MaterialCommandChangeRequiresReapproval`,
`SensitiveStopNeverTransitionsToExecution`,
`UnknownThirdPartyTriggerRequestsCleanRepro`,
`ApprovedDocumentedAlternativeSampleIsNotBugProof`,
`SuccessfulSaveHasExactReadbackParity`,
`CollisionPreservesExistingReport`,
`AgentWriterFailureKeepsChatReport`, and
`ExecutionReceiptMatchesToolsAndCleanup`. The host-effect lane must separately
cover `ApprovedHostContainsExpectedEffects`,
`ApprovedHostCannotReadProtectedMarkers`,
`ApprovedHostCannotReachUnrelatedHostNetwork`, and
`ReducedSamplePreservesOriginalFileTrigger`. Every test must reach its material
assertion; skipped, unavailable, preflight-only, or unsupported outcomes are
blocked/not-exercised rather than passes.

## Hosted entry point

`.github/workflows/skill-evals.yml` runs `Validate` automatically when pull
requests or pushes to `main` change runtime skills, eval assets, or the workflow
itself. Validation parses and dry-runs both the standard and smoke experiments
without invoking a model or judge.

Hosted automatic selection removes the `investigate-issue` lane only after
affected-skill and central-change classification, preserving every other
eligible lane. An empty eligible set skips before status claiming or model
scheduling. Explicit or bypassed attempts to supply the excluded lane fail with
a clear eligibility diagnostic at the request or common staging boundary. This
does not affect private operator runs or model-free validation. The exclusion
becomes active only after the trusted default-branch workflow contains it;
candidate code does not disable already-deployed hosted entry points.

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
scores; only the skilled score is threshold-gated. The assertion also requires
the skilled variant to cover every named stimulus for the planned trial count,
with unique trial identities. For each skilled stimulus declaring
`output-matches`, the result's grader-type multiset must match its plan, and
every `output-matches` result must contain Boolean `passed: true`. A high mean
cannot compensate for a failed deterministic contract. Baseline grader failures
remain permitted. The final status and PR
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

The deterministic gate trusts pinned Vally's structured plan and results. It
does not parse judge explanations, depend on grader ordering or display names,
or reimplement regex matching. It detects missing, extra, or mismatched grader
types, not forged substitutions between same-type graders. Passing output
checks is not proof that tool-use or every safety boundary was respected.
Candidate assertion changes are not hosted enforcement until the trusted
default-branch control plane includes them; apply a candidate assertion locally
to downloaded artifacts when assessing such a change.

For `investigate-issue`, preserve the original 21-stimulus cohort, report the
two previously appended contrast cases separately, and report the newer
host/persistence cohort separately again. A changed denominator must not
disguise regressions. Inspect each legacy skilled response and its rubric
evidence, not just the mean or judge narrative, and investigate material
regressions. Every skilled deterministic requirement must pass regardless of
cohort. Do not impose per-row score monotonicity on stochastic one-trial smoke
runs or baseline responses that never loaded the skill. The synthetic
snapshots, formatting checks, same-model judge, and checked-in host descriptions
do not establish real storage effects or arbitrary host privacy; retain
separate writer/read-back and tool-trace evidence.

The sensitive-stop and known-specialist prompt graders
explicitly distinguish assistant-authored disclosures from user input in
Vally's session timeline, while retaining tool-call evidence for prohibited
actions. The invalid-input matcher accepts plain or bold `one` in the same
required phrase. Validate such grader corrections against preserved
trajectories and label the results as regrades, not new agent trials. Include
a clean-output versus actual-echo control when correcting disclosure grading;
removing a false positive must not permit a real leak.

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
