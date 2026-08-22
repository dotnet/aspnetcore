# Evaluation anti-overfit policy

This policy applies to `fix-challenge`, `fix-issue`, and `fix-candidate`. It
protects their evaluation sets from optimizing for a small, recognizable
collection of prompts.

## Retention and scoring

Retain a regression once it is discovered. A lower score weight is not a reason
to delete, weaken, or stop running a regression. Score changes only affect
aggregation; they do not change the required behavioral evidence.

Aggregate scores by taking the mean within each `(tier, score_family)` and then
macro-average families in that tier. Consequently, every eval has normalized
family weight `1 / (number of families in its tier * number of evals in its
tier and family)`; adding near-duplicates cannot increase that family's
influence.

Designate held-out cases before changing the skill, and do not tune prompts,
examples, instructions, or scoring against them. A held-out failure may motivate
a new, separately provenanced train regression, but the original held-out case
remains unchanged.

## Instruction promotion

A regression does not automatically justify another global instruction. Promote
a rule into the always-loaded skill only when:

1. A retained before-change result fails for the reason the rule addresses.
2. The same mechanism transfers to an independently provenanced case outside
   the source PR or subsystem.
3. Held-out no-defect and bounded-stateless canaries do not acquire extra
   blockers or unnecessary lifecycle work.
4. The rule can be stated without source-PR nouns. Otherwise keep it in a
   conditional domain reference.
5. The addition consolidates or replaces narrower guidance when possible,
   rather than growing the skill indefinitely.

Passing only the regression that motivated a rule shows memorization, not
generalization.

## Provisional model selection

The model policy in
[`model-policy.v1.json`](../../../.github/skills/fix-challenge/references/model-policy.v1.json)
and this selection summary were first published at public commit
`cc3e5c604d82b1d6edbce59474200f686abea155`. The policy was selected from 30
valid outputs: six models each reviewed five frozen cases. The cases exercise
corrected-head abstention, compatibility, lifecycle/provenance, test
falsification, and input provenance. Six earlier compatibility attempts were
excluded because their frozen-input hashes did not match.

The one-trial-per-model evidence supports a provisional, not permanent, matrix.
It selected `gpt-5.6-luna` and `claude-opus-5` for bounded review; Luna, Opus,
`gpt-5.6-terra`, and `claude-sonnet-5` for full review; and
`mai-code-1.1-flash` as a non-voting full-path shadow. `gpt-5.6-sol` remains the
provisional orchestrator incumbent; it was reserved for judging and was not
compared as a candidate. Authoritative runtime-model and cost telemetry were
unavailable, and inconsistent self-reported latency was not used for selection.

## Metadata and controls

Every Vally stimulus has governance tags. `mechanism` and `score_family` are
lower kebab-case labels; `provenance_kind` and `provenance_source` identify a PR,
historical case, or synthetic source. `controls_positive` and
`controls_negative` are disjoint, nonempty, comma-separated zero-based indexes
into the rubric entries after the overall expected-outcome entry. Positive
controls identify evidence that must be present; negative controls identify an
overclaim, unrelated scaffold, mutation, or side effect the evaluator must
reject or avoid. These are expectation-level grading controls, not substitutes
for matched scenario controls.

Every new defect regression also needs a matched no-defect, alternate-cause, or
scope-control scenario in the same score family before its lesson becomes a
global instruction. The held-out no-defect and bounded-stateless cases are
permanent complexity-inflation canaries.

Discovery prompts must list a nonempty JSON array in the
`forbidden_prompt_terms` tag. Those terms must not occur in the prompt,
case-insensitively. Verification prompts may use an empty array, but every term
listed is still forbidden. Keep issue numbers,
implementation names, answer phrases, and other answer-revealing vocabulary out
of discovery prompts. Discovery evals receive frozen evidence through
stimulus-level `environment.files`;
removing facts from a prompt without supplying a fixture makes the eval
ungradeable rather than de-leaked.

Held-out stimuli carry `fixture_hashes` and `frozen_hash` tags. The validator
checks every fixture SHA-256 and recomputes the semantic stimulus hash from the
parsed prompt, rubric, fixture references, and governance tags, excluding the
hash field itself. Train and held-out provenance must remain disjoint within a
skill.

## Maintenance

Use ablations before accepting a new mechanism or scoring rule: remove the
claimed signal and confirm that the score changes for the intended reason. Prune
only a duplicate or disproven eval, recording the replacement or rationale;
never prune a regression merely because it is inconvenient.

Each expectation must reject a crafted bad result and accept a correct
paraphrase. An expectation that rejects neither is non-discriminating; one that
rejects the paraphrase is a wording matcher. Keep discovery prompts limited to
the evidence a reviewer would receive. Put the mechanism to discover in the
expected result, not in the prompt.

The validator reports family, tier, provenance, and prompt/expectation-overlap
concentration as warnings. These are investigation signals, not arbitrary
acceptance quotas: unusual distributions can be legitimate and must be judged
with provenance and transfer evidence.

Report family-macro and provenance-macro results separately. A source PR can
teach several real mechanisms; one scalar must not let its spread across
families hide poor transfer to other provenance.

Before accepting eval changes, run:

```powershell
pwsh eng/skill-evals/reviewer-suites/scripts/Validate-Evals.ps1 `
  -Path 'eng/skill-evals/fix-challenge/regression.vally.yaml,eng/skill-evals/fix-challenge/model-guardrail.vally.yaml,eng/skill-evals/fix-candidate/regression.vally.yaml,eng/skill-evals/fix-issue/regression.vally.yaml'
pwsh eng/skill-evals/reviewer-suites/scripts/Stage-ReviewerSkills.ps1 `
  /tmp/aspnetcore-review-skills
```

The ASP.NET Core repository carries the portable reviewer runtime, canonical
eval specifications, fixtures, and deterministic local validation. Publishing
or maintaining these skills does not require rerunning model or judge calls.

The four specs under `eng/skill-evals/` are the only source of truth for
prompts, rubrics, fixtures, models, and governance metadata. There is no
generated manifest or synchronization step. `Validate-Evals.ps1` performs the
cross-stimulus anti-overfit checks that Vally's schema lint does not cover.

These named reviewer specs are specialized capability and regression suites.
The repository runner auto-discovers only `eval.vally.yaml`; invoke these suites
explicitly with `-Eval` through the repository runner or with Vally's
`--eval-spec`/`-e` option. They may use the reviewer-specific staging helper
below because the discoverable workflows consume the shared candidate contract.

Official and comparison runs use `@microsoft/vally-cli@0.13.0`. Invoke that
exact package rather than an unversioned global `vally`; otherwise local results
can silently depend on an older schema or grading implementation. Record the
resolved version with the retained results. The repository-wide eval directory
does not currently pin a Vally package version, so update this pin deliberately
only after strict-linting all four canonical specs. ASP.NET
Core's `.npmrc` points at an authenticated Azure DevOps feed, while Vally 0.13
is not available from public npm. Authenticate that feed or select an approved
Microsoft mirror before running `npx`; the following mirror was used for the
retained local results:

```bash
export npm_config_registry=https://packagefeedproxy.microsoft.io/npm/
npx --yes --package @microsoft/vally-cli@0.13.0 vally --version
npx --yes --package @microsoft/vally-cli@0.13.0 vally lint \
  --eval-spec eng/skill-evals/fix-challenge/regression.vally.yaml \
  --strict
npx --yes --package @microsoft/vally-cli@0.13.0 vally lint \
  --eval-spec eng/skill-evals/fix-challenge/model-guardrail.vally.yaml \
  --strict
npx --yes --package @microsoft/vally-cli@0.13.0 vally lint \
  --eval-spec eng/skill-evals/fix-candidate/regression.vally.yaml \
  --strict
npx --yes --package @microsoft/vally-cli@0.13.0 vally lint \
  --eval-spec eng/skill-evals/fix-issue/regression.vally.yaml \
  --strict
```

Vally 0.13 emits `--output jsonl` records on standard output. Official runs
must retain that stream as `results.jsonl` and retain diagnostics separately;
`--output-dir` stores the Markdown report and telemetry, not the JSONL consumed
by `Aggregate-EvalScores.ps1`.

Use Vally for both repository and local execution. For example, this runs the
documentation-placement case locally with the reviewer skill and Vally's prompt
grader:

```bash
npx --yes --package @microsoft/vally-cli@0.13.0 vally eval \
  -e eng/skill-evals/fix-challenge/regression.vally.yaml \
  --skill-dir /tmp/aspnetcore-review-skills \
  --tag eval_id=17 \
  --runs 1 \
  --workers 1 \
  --timeout 1200s \
  --model gpt-5.6-sol \
  --judge-model claude-opus-5 \
  --workspace /tmp/fix-challenge-diagnostic/workspaces \
  --output jsonl
```

The non-GPT orchestrator guardrail is intentionally in
`model-guardrail.vally.yaml` so it can run under `claude-sonnet-5` without
invalidating the GPT-orchestrated cases in the main suite. These deep-review
specs are standalone Vally capability suites rather than inputs to the generic
`skills-vs-baseline` experiment. They need a sibling skill and repository
identity, so treating a live checkout as the baseline would auto-discover the
skills under test and invalidate the A/B comparison. Direct local runs can
select a case by its `eval_id` tag. Their declared environments copy repository
instructions, root build metadata, neutral fixture aliases, and only explicit
stimulus-level source overlays into a new independent Git repository.
Fixture-driven discovery cases do not receive an unrelated production source
tree. Source-backed cases must declare the narrow paths they need rather than
inheriting a whole product area. The reviewer skill directories are never
copied, canonical eval specs are deleted before the initial commit, and ignored
outputs are removed using the copied root `.gitignore`, and the origin has a
disabled push URL. This keeps snapshots small, prevents answer-key discovery,
and avoids sharing host Git metadata.
`Stage-ReviewerSkills.ps1` copies only the runtime files required by the two
discoverable workflow skills into a directory outside the checkout. The shared
candidate contract is copied into each eval workspace at its repository path.

Run official suites from a committed revision with no unrelated changes in the
declared source paths. The snapshot copies working-tree files, so an uncommitted
production change would otherwise alter the eval environment. This isolation is
not a security sandbox: the executor still has the host process environment,
network, and model credentials. Injection cases measure instruction adherence,
not containment. Run them in a least-privileged environment and never treat a
passing score as proof that a hostile model process could not exfiltrate data.

Scoped source makes repository inspection possible, but it does not recreate a
historical PR patch, guarantee every project dependency needed by a build, or
invent an empirical assertion contract. A case that supplies only a mechanism
fixture must stay in `candidate-review` or another explicitly bounded phase,
and its rubric must grade the validation plan rather than claim commands ran.
Require empirical execution only when the stimulus supplies a concrete
candidate state, independently justified assertion, all source dependency areas
needed by the command, and a safe restoration boundary.

Vally 0.13 removed the `pairwise` grader type from eval specs. These capability
suites use prompt grading only. Run the pinned CLI's `compare` command over an
experiment output directory when a comparative judgment is needed.

A one-trial local run is diagnostic feedback only. Official score aggregation
requires the five trials and executor model pinned in each canonical stimulus.
Use one worker and a dedicated retained workspace root. The source snapshot is
large enough that concurrent local environment setup can collide during Git
initialization; five sequential trials preserve isolation and reproducibility.
Run the GPT suites and the Claude guardrail separately when using direct Vally:

```bash
set -o pipefail
mkdir -p /tmp/fix-challenge-main /tmp/fix-challenge-guardrail /tmp/fix-candidate /tmp/fix-issue

npx --yes --package @microsoft/vally-cli@0.13.0 vally eval \
  -e eng/skill-evals/fix-challenge/regression.vally.yaml \
  --skill-dir /tmp/aspnetcore-review-skills \
  --runs 5 --workers 1 --timeout 1200s \
  --model gpt-5.6-sol --judge-model claude-opus-5 \
  --workspace /tmp/fix-challenge-main/workspaces \
  --output jsonl --output-dir /tmp/fix-challenge-main/artifacts \
  2>/tmp/fix-challenge-main/run.log |
  tee /tmp/fix-challenge-main/results.jsonl
npx --yes --package @microsoft/vally-cli@0.13.0 vally eval \
  -e eng/skill-evals/fix-challenge/model-guardrail.vally.yaml \
  --skill-dir /tmp/aspnetcore-review-skills \
  --runs 5 --workers 1 --timeout 1200s \
  --model claude-sonnet-5 --judge-model claude-opus-5 \
  --workspace /tmp/fix-challenge-guardrail/workspaces \
  --output jsonl --output-dir /tmp/fix-challenge-guardrail/artifacts \
  2>/tmp/fix-challenge-guardrail/run.log |
  tee /tmp/fix-challenge-guardrail/results.jsonl
npx --yes --package @microsoft/vally-cli@0.13.0 vally eval \
  -e eng/skill-evals/fix-candidate/regression.vally.yaml \
  --skill-dir /tmp/aspnetcore-review-skills \
  --runs 5 --workers 1 --timeout 1200s \
  --model gpt-5.6-sol --judge-model claude-opus-5 \
  --workspace /tmp/fix-candidate/workspaces \
  --output jsonl --output-dir /tmp/fix-candidate/artifacts \
  2>/tmp/fix-candidate/run.log |
  tee /tmp/fix-candidate/results.jsonl
npx --yes --package @microsoft/vally-cli@0.13.0 vally eval \
  -e eng/skill-evals/fix-issue/regression.vally.yaml \
  --skill-dir /tmp/aspnetcore-review-skills \
  --runs 5 --workers 1 --timeout 1200s \
  --model gpt-5.6-sol --judge-model claude-opus-5 \
  --workspace /tmp/fix-issue/workspaces \
  --output jsonl --output-dir /tmp/fix-issue/artifacts \
  2>/tmp/fix-issue/run.log |
  tee /tmp/fix-issue/results.jsonl
```

Vally supplies the score-producing prompt grader, repeated trials, and
pass@k/pass^k reporting. Run
`eng/skill-evals/reviewer-suites/scripts/Aggregate-EvalScores.ps1` with the four
canonical Vally specs and one or more
`-VallyResults <skill-name>=<results.jsonl>` arguments to additionally report
raw, family-macro, provenance-macro, and train-to-held-out transfer results.
The reviewer aggregation needs both its GPT and Claude result files:

```powershell
pwsh eng/skill-evals/reviewer-suites/scripts/Aggregate-EvalScores.ps1 `
  -EvalPath 'eng/skill-evals/fix-challenge/regression.vally.yaml,eng/skill-evals/fix-challenge/model-guardrail.vally.yaml,eng/skill-evals/fix-candidate/regression.vally.yaml,eng/skill-evals/fix-issue/regression.vally.yaml' `
  -VallyResults 'fix-challenge=/tmp/fix-challenge-main/results.jsonl,fix-challenge=/tmp/fix-challenge-guardrail/results.jsonl,fix-candidate=/tmp/fix-candidate/results.jsonl,fix-issue=/tmp/fix-issue/results.jsonl'
```

The `-Scores <path>` input remains available for importing results from
another evaluator.

### Grader infrastructure failures

A malformed or timed-out judge response is infrastructure failure, not a zero
quality score. Preserve the original JSONL, regrade its failed trajectory, and
preserve the repaired JSONL separately:

```bash
jq -c \
  'select(.type != "run-summary" and any(.gradeResult.details[]?; .metadata.error? != null))' \
  <original-results.jsonl> |
  npx --yes --package @microsoft/vally-cli@0.13.0 vally grade \
    -e <specialized.vally.yaml> \
    --judge-model claude-opus-5 \
    --output jsonl >regraded.jsonl
```

Pass the original result before the regraded result to
`Aggregate-EvalScores.ps1`. A later successful grade may supersede only an
earlier grader-error record with the same trajectory ID. Duplicate successful
records, unresolved grader errors, agent failures, and missing trials remain
fatal. Retain both files so the repair is auditable.

Retained JSONL, reports, timing, and model-authored logs are provenance-bearing
artifacts, not authenticated records. Preserve their command line, resolved CLI
version, source commit, skill hashes, model IDs, and timestamps. Do not describe
agent-authored transcripts or logs as tamper-proof or independently attested.
