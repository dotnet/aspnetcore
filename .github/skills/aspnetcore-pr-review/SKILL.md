---
name: aspnetcore-pr-review
description: >-
  Multi-model adversarial review specifically for a dotnet/aspnetcore PR, issue
  fix, or local diff. Use whenever work in the ASP.NET Core repository needs a
  deep review, competing fixes, multi-model validation, adversarial consensus,
  or a decision about whether a local fix is the best approach. Runs four
  independent aspnetcore-try-fix candidates, cross-examines them, empirically
  adjudicates the strongest surviving finding with a lifecycle-aware proof
  ladder, then falsifies any proposed production fix in an isolated child
  worktree before recommending it. Produces one local-only recommendation.
  Do not use in dotnet/maui or any repository other than dotnet/aspnetcore.
  Never posts or pushes.
compatibility: Requires a dotnet/aspnetcore checkout and the sibling aspnetcore-try-fix skill
---

# ASP.NET Core Multi-Model Review

Review a PR or local issue fix using independent model diversity followed by an
adversarial consensus round. Keep the workflow local: no comments, approvals,
commits, pushes, PR creation, or branch changes.

## Repository scope

This skill is intentionally repository-specific. Before using it:

1. Verify the current Git checkout is `dotnet/aspnetcore` from its configured
   remote URL or trusted session metadata.
2. If the repository cannot be verified, stop and report that this skill only
   supports `dotnet/aspnetcore`.
3. Do not reinterpret these instructions for .NET MAUI or another repository.

## Inputs

- Issue/PR number or problem description.
- Current diff/fix summary, target files, validation evidence, and blockers.
- An artifact root outside the repository. Prefer the current session's artifact
  directory. If none is available, create a temporary directory and report it.

Resolve `<skill-root>` from the active `SKILL.md` and the candidate only from
`<skill-root>/../aspnetcore-try-fix/SKILL.md`. Record both paths and hashes;
stop rather than mix installed and project copies.

## Model panel

Run orchestration and final synthesis in a GPT-family session. Prefer
`gpt-5.6-sol` or the strongest newer GPT model available. Before Phase 1,
verify the current session model. If it is an Anthropic or other non-GPT model,
stop and report that the review must be restarted with a GPT orchestrator
instead of silently continuing.

Keep the orchestrator separate so candidate models do not also control evidence
selection and final synthesis.

Use four different model families/configurations:

| Candidate | Model | Primary challenge |
|---|---|---|
| A | `claude-opus-4.6` | Minimal root-cause and API-contract repair |
| B | `claude-opus-4.7` | Compatibility skeptic and failure modes |
| C | `gpt-5.3-codex` | Repository-pattern alternative |
| D | `gpt-5.5` | Test falsification and unnecessary-surface removal |

If a model is unavailable, record its substitute and preserve four distinct
models when possible. Record candidate timeouts/tool failures instead of
silently replacing their results.

## Phase 1: Freeze shared evidence

Create one provenance-backed evidence bundle before launching candidates. `git
diff` is not a complete dirty-tree inventory because it omits untracked files.
The authoritative local change set must combine tracked changes and in-scope
untracked files.

Save the bundle outside the repository using these names:

```text
<artifact-root>/aspnetcore-pr-review/
  evidence/manifest.md
  evidence/product-oracle.md
  evidence/head-drift.md
  evidence/impact-map.md
  evidence/tracked.diff
  evidence/files/
  candidates/candidate-a.md
  candidates/candidate-b.md
  candidates/candidate-c.md
  candidates/candidate-d.md
  cross-examination/candidate-a.md
  cross-examination/candidate-b.md
  cross-examination/candidate-c.md
  cross-examination/candidate-d.md
  empirical/manifest.md
  empirical/before.diff
  empirical/diagnostic.diff
  empirical/implementation.diff
  empirical/head.log
  empirical/red.log
  empirical/candidate.diff
  empirical/green.log
  empirical/claim-matrix.md
  empirical/stress-matrix.md
  empirical/result.md
  final/repository-oracle.md
  final/review.md
```

The manifest must record:

1. Repository remote, HEAD SHA, branch, and working directory.
2. `git status --porcelain=v1 -uall`, including every untracked path.
3. The complete tracked diff.
4. The content and SHA-256 hash of every relevant untracked or full source file.
5. Applicable `AGENTS.md` and instruction files with their hashes.
6. Issue/PR text and comments, with source URL.
7. Exact validation commands and complete logs when available.
8. Known environment failures, explicitly separated from product failures.
9. Which changed paths are in scope and why unrelated dirty paths were excluded.
10. An impact map from every changed producer, dispatcher, callback filter, or
    state transition to the consumers and unchanged tests that exercise each
    affected branch. Record the exact command selected for each test, or a
    source-backed reason why no existing test is impacted.

Give every model the same manifest, tracked diff, and captured files. Permit a
narrow source lookup outside the bundle only when the candidate records the
path and the claim it is verifying.

Changed paths are not a sufficient impact analysis. A narrow producer edit can
break an unchanged consumer test in another directory. Build
`evidence/impact-map.md` from symbols, callbacks, event branches, and callers
before selecting new assertions. Prefer running directly impacted existing
tests over inventing candidate-specific tests.

Do not include the parent's conclusion that the fix is correct. The goal is to
avoid anchoring.

## Phase 2: Establish the product oracle

Read `references/proof-calibration.md` before writing
`evidence/product-oracle.md`. Separate four things that are easy to conflate:
the observed symptom, the intended behavior, the patch author's objective, and
the proposed historical cause.

Classify each expected-behavior claim using the authority ladder in that
reference. A PR description can establish what its author is trying to change,
but cannot by itself establish accepted product intent or prove why an earlier
failure occurred. Implementation, existing tests, and model agreement remain
evidence of current behavior rather than automatic product intent.

Freeze the expected assertion and its independent authority before selecting a
candidate correction. If the scenario is justified only by a candidate or
patch-author hypothesis, it may be explored, but it starts as diagnostic-only
and cannot by itself justify a high-confidence implementation blocker.

## Phase 3: Independent candidates

Launch all four models with the resolved sibling
`<skill-root>/../aspnetcore-try-fix/SKILL.md` in `candidate-review` mode.

These invocations are read-only, so run them in parallel. Each prompt must:

- Require one root-cause hypothesis and one candidate only.
- For stateful, concurrent, lifecycle, interop, or browser-driven behavior,
  require a transition table containing: state/invariant, entry path, ordinary
  successful exit, cancellation/interruption exit, owner, and the observable
  consequence if the state is stranded or consumed twice.
- When callbacks, observers, measurements, or notifications are suppressed,
  disabled, discarded, or deferred, extend the table with: what stops
  updating, the first event after recovery, any ownership transfer, the
  generation/provenance of every value consumed after recovery, stale values
  that survive, and the opposite edge or boundary.
- Require an approach materially different from the current fix when viable.
- Allow `NO VIABLE ALTERNATIVE` only after the candidate names and rejects at
  least one mechanism-level alternative.
- Require a direct assessment of whether the current test can false-pass.
- Require citations for compatibility, browser-support, API-breaking,
  test-execution, and repository-pattern claims. A citation must be an exact
  source path and line, observed output, or primary-source URL.
- Require every expected-behavior claim to cite the shared product oracle and
  preserve its confidence. Candidates may challenge the oracle with evidence,
  but cannot silently replace it with implementation-derived intent.
- Require every proposed discriminating assertion to state why the expected
  result is required independently of that candidate.
- Pass `evidence/impact-map.md` explicitly and require the candidate to assess
  its mapped unchanged tests and uncovered producer branches.
- Label unverifiable claims `UNSUPPORTED`; unsupported claims cannot become
  required follow-ups or consensus findings.
- Prohibit edits, commits, pushes, comments, and external posting.
- Withhold all other models' outputs.

Save each raw response unchanged at its configured candidate path. Do not
replace raw output with an orchestrator summary.

## Phase 4: Adversarial cross-examination

After all independent candidates complete, send every model an anonymized
summary of all candidates and the current fix. Use IDs `P1` through `P4`, not
model names, and preserve this schema for each proposal:

```text
ID:
Root-cause hypothesis:
Mechanism-level change:
Files/surfaces:
Evidence and citations:
Known risks:
Recommendation:
```

Before cross-examination, validate each response against the sibling
`aspnetcore-try-fix` output schema. Allow one correction turn for missing
required fields or an oversized full-file restatement. Record how many distinct
root-cause mechanisms survived; four models selecting a helper shown in their
shared evidence is correlated convergence, not four independent runtime proofs.

Each model must:

1. Identify the strongest candidate and why.
2. Attack every candidate with one concrete failure scenario.
3. Mark each candidate `support`, `dispute`, or `discard`.
4. State whether the current fix is complete.
5. Offer a genuinely new idea or `NO NEW IDEA`.
6. Mark factual claims `VERIFIED`, `CONTRADICTED`, or `UNSUPPORTED` with evidence.
7. Rank surviving behavioral claims by falsifiability: concrete trigger,
   observable failure, and faithful test boundary. Prefer a bounded claim over a
   broad concern that cannot be distinguished.
8. Challenge whether the current tests cover the first event after a suppressed
   interval, every changed producer branch, and the opposite boundary. Propose
   one bounded state-derived adjacent scenario when they do not; do not expand
   into an unrelated Cartesian test matrix.

One cross-examination round is sufficient for this minimal reviewer. If two or
more models produce the same new idea, the orchestrator must verify it against
source or observed output before including it in synthesis. Save each raw
cross-examination response at its configured path.

## Phase 5: Empirical adjudication

Multi-model agreement is corroboration, not runtime proof. After
cross-examination, empirically adjudicate the highest-severity surviving
finding whose consequence can be tested. Do this before assigning the final
merge-readiness verdict.

1. Select one finding that:
   - has support from at least two independent candidates, or is a
     high-severity minority finding;
   - has no surviving source-level contradiction;
   - predicts a concrete observable failure;
   - can be distinguished at the smallest test surface that still exercises the
     information producer relevant to the claim. Do not choose an easier
     consumer-only unit test when the finding depends on browser, transport,
     process, or scheduler behavior.
     - is informed by the impact map and by directly impacted unchanged tests,
       not only the tests added or edited by the pull request.
2. Create an isolated child session or disposable detached worktree at the
   frozen PR head. Never edit the parent review worktree. Record its exact path,
   SHA, and clean status in `empirical/manifest.md`.
3. Invoke `aspnetcore-try-fix` in `empirical` mode sequentially, using the
   strongest consensus hypothesis, the exact claim to prove or reject, the
   relevant product-oracle entries, the already-approved candidate-independent
   assertion contract, its allowed perturbations, `evidence/impact-map.md`, and
   the smallest targeted validation command. The empirical agent may edit only
   its isolated worktree.
4. Build a proof ladder in `empirical/claim-matrix.md` and record the highest
   completed rung for each blocker-caliber claim:
   - source invariant or contradictory contract;
   - direct consumer behavior;
   - producer classification or dispatch;
   - real integration/runtime interaction;
   - production-candidate regression coverage.
   A lower rung does not prove a higher-rung scenario. For example, directly
   invoking a callback does not prove which callback a browser interaction
   produces.
5. Run directly impacted existing tests from `evidence/impact-map.md` against
   untouched frozen head before adding a new assertion. Preserve their commands
   and complete output. If an unchanged test already distinguishes the defect,
   use it as the primary assertion rather than replacing it with a narrower
   candidate-specific test.
6. Run the approved assertion against untouched frozen head first and save the
   complete result in `empirical/head.log`:
   - add or tighten one assertion that distinguishes the predicted defect;
   - if head fails behaviorally, copy that result to `empirical/red.log`, apply
     the smallest correction, and save the identical passing assertion in
     `empirical/green.log`;
   - if head passes, reject or narrow the blocker. Mark red/green not applicable
     rather than manufacturing a mutation; any useful mutation is diagnostic
     only and cannot represent a frozen-head defect;
   - save assertion changes as `before.diff` and `diagnostic.diff`, production
     changes as `implementation.diff`, the combined state as `candidate.diff`,
     and the structured result as `result.md` under `empirical/`;
   - classify the regression assertion as required, optional, or rejected, and
     any mutation separately as diagnostic-only, rejected, or not applicable.
     Required means accepted criteria or a proven defect needs exact coverage.
7. A valid red must fail at the predicted behavioral assertion. A stale browser
   element, harness timeout before the trigger, build failure, missing asset,
   infrastructure error, unrelated assertion, or different assertion is not
   behavioral red evidence. Fix the harness or classify the run as `Blocked`.
8. A pre-existing failing test is acceptable only when the same assertion
   passes after the candidate correction. A build-only failure, unrelated test
   failure, different assertion, or source-only prediction is not green
   evidence.
9. Treat the first green as causal evidence for the finding, not proof that the
   candidate is production-ready. Preserve these as separate conclusions:
   - **Finding proof:** does frozen head exhibit the predicted defect?
   - **Scenario proof:** did the real producer/runtime path exhibit it?
   - **Candidate proof:** did the proposed fix survive relevant counterexamples?
10. Run at most three iterations for the same hypothesis. If the environment,
   browser harness, or target test cannot run, preserve the failure and classify
   adjudication as `Blocked`; do not substitute aggregate CI or model agreement.
   If a generated asset or unrelated build target blocks the focused command,
   record the baseline failure before applying a documented property override.
   Verify the bypassed target cannot affect the exercised behavior and cap the
   candidate at `targeted-proven` unless the standard build or exact CI path
   later validates it.
11. For every other blocker-caliber behavioral claim, execute a narrow
    differentiating test when practical. If frozen head passes, explicitly
    discard or narrow the claim. If frozen head fails at the predicted
    assertion, promote it to a required follow-up using the highest proof rung
    reached. If it is not run, downgrade it rather than carrying it as an
    implementation blocker.
12. Do not require empirical validation for a claim fully established without
   runtime execution, such as a compiler error or a directly contradictory API
   contract. Record why execution adds no information.
13. Preserve all empirical artifacts before removing a disposable worktree. If
   safe cleanup is unavailable, leave the isolated worktree in place and report
   it rather than using destructive cleanup.

If no surviving finding can be empirically adjudicated, the final verdict must
say `blocked on evidence` for behavioral claims. It must not say `blocked on
implementation` with high confidence solely from multi-model source reasoning.

## Phase 6: Production-candidate falsification

Apply the production-proof rules in `references/proof-calibration.md`. Continue
in the same isolated worktree and save a lifecycle-derived matrix in
`empirical/stress-matrix.md`.

- Vary the dimensions that could falsify the mechanism. Repeating one identical
  deterministic scenario demonstrates repeatability, not a complete matrix.
- Scale the matrix to claim severity and statefulness. Do not add lifecycle
  scaffolding solely to earn `production-proven`; a lower proof cap is valid.
- Exercise the real producer/runtime boundary and the narrow neighboring suite.
- For a suppressed callback, observer, or measurement interval, exercise the
  first resumed event and the opposite edge or boundary. When geometry or
  measurement provenance is on the changed path, include one fixed-size control
  and one realistic variable-size or layout perturbation. Expand further only
  after a behavioral divergence or when the claim is platform-specific.
- For asymmetric before/after handling or batch filtering, cover before-only,
  after-only, and both-in-one-batch cases so suppressing a duplicate does not
  discard the callback required by another consumer.
- When recommending an observer-only timeout, inspect the inner task states
  after timeout, release or cancel them deterministically, observe exceptions,
  and verify cleanup cannot leak into later tests.
- Preserve configuration and platform limits. A targeted run using a build
  bypass cannot become cross-platform `production-proven`.
- In `empirical/stress-matrix.md`, mark the real producer/runtime boundary,
  varied falsification dimensions, applicable configurations/platforms,
  neighboring suite, and cleanup/interruption paths as `passed` or
  `not applicable - <specific reason>`; list distinct variants under
  `## Executed cases`.
- Classify the candidate as `production-proven`, `targeted-proven`,
  `diagnostic-only`, `rejected`, or `blocked`.

If no candidate is production-proven, the review may still request changes for
a proven defect. Describe the required invariant and evidence, but do not
prescribe an exact implementation.

## Phase 7: Recheck live head and synthesize

Before final synthesis for a pull request, fetch the live PR head again and
compare it with the frozen evidence SHA. Save the comparison in
`evidence/head-drift.md`.

- If the head is unchanged, proceed.
- If only unrelated paths changed, record why the evidence remains applicable.
- If a relevant source, test, contract, or instruction changed, refresh the
  evidence bundle, regenerate `evidence/impact-map.md`, and rerun both the
  affected proof and mapped unchanged tests before presenting the finding as
  current.
- Never silently describe frozen-head evidence as current-head validation.

The orchestrator, not any candidate model, synthesizes the result.

Before writing the final report, resolve `<skill-root>` to the directory that
contains this `SKILL.md` and run:

```bash
python3 <skill-root>/scripts/validate_artifacts.py \
  <artifact-root>/aspnetcore-pr-review
```

Fix missing files or sections before synthesis. If an artifact is legitimately
not applicable, create it with the reason instead of omitting it. Do not
override proof-calibration failures merely because the final narrative explains
them elsewhere.

For each claim:

- **Agree:** at least two models independently support it and no concrete
  counterexample survives, with factual support verified by the orchestrator.
- **Dispute:** models disagree or evidence is incomplete.
- **Discard:** the claim conflicts with source code, repository conventions, or
  observed test evidence.
- **Unsupported:** the claim lacks repository evidence, observed output, or a
  primary source. Exclude it from required follow-ups and verdict severity.
- **Oracle-blocked:** the implementation concern is testable, but the intended
  behavior still depends on unresolved product context.

For the empirically adjudicated finding:

- Promote a finding to a required implementation follow-up when frozen head
  fails at an independently justified behavioral assertion, the relevant oracle
  is authoritative enough for the requested severity, and the causal mechanism
  is supported. A candidate-shaped diagnostic turning green proves only its
  scoped experiment.
- If adjudication is blocked, preserve the structural concern under `Dispute`
  or as a required evidence follow-up, calibrated to the strongest evidence
  actually obtained.
- If the test contradicts the prediction, discard or narrow the finding before
  synthesis.

Select the recommended fix in this order:

1. Must satisfy strict behavioral red/green evidence.
2. Must survive the lifecycle-derived stress matrix and real producer boundary.
3. Must cover all producer/consumer paths involved in the bug.
4. Must preserve compatibility and public API requirements.
5. Prefer fewer concepts and files when correctness is equal.
6. Prefer established ASP.NET Core patterns over novel machinery.

## Phase 8: Preserve repository knowledge and draft readable comments

Write `final/repository-oracle.md` for product knowledge that was missing,
corrected, or difficult to discover during the review. Recommend only durable
surfaces appropriate to the information:

- Public observable behavior belongs in API documentation.
- Internal lifecycle and ownership invariants belong next to the state machine.
- Retention and takeover behavior belongs in paired behavioral tests.
- Cross-cutting review guidance belongs in repository agent instructions.

Do not leak review-session mechanics, model identities, local paths, or private
conversation into repository guidance.

When drafting a review comment, translate the internal proof into maintainer
language:

1. Start with the concrete action and visible failure.
2. Explain the causal code path using only the minimum necessary terminology.
3. State the requested change.
4. Include one concrete example when it makes the behavior easier to see.
5. State what the experiment does not prove whenever scenario, mechanism,
   configuration, or oracle fidelity is weaker than exact.

Avoid compressed phrases such as "pair the takeover check with a focused
retention assertion" when plain language can say "change one item above the
initial target, verify the target stays in place, then press End and verify the
last item loads." Internal terms such as product oracle, ownership, proof
ladder, and producer boundary may remain in artifacts, but define or translate
them before using them in a GitHub comment.

Save the synthesized output below to `final/review.md`.

## Output

```markdown
# Multi-Model Review

**Orchestrator:** <GPT model>

## Current fix
<summary>

## Independent candidates
| ID | Model | Root cause | Approach | Assessment |
|---|---|---|---|---|

## Adversarial consensus
### Agree
- <claim and evidence>

### Dispute
- <claim and unresolved evidence>

### Discard
- <claim and why>

## Test assessment
<frozen-head result and relevant paths; include strict red/green only for a
proven defect, otherwise state that head passed and no blocker was manufactured>

## Proof status
**Frozen-head result:** behavioral-fail / structural-defect / pass / blocked / not-applicable
**Finding proof:** empirical / structural / missing
**Scenario proof:** empirical / structural / missing
**Candidate proof:** production-proven / targeted-proven / diagnostic-only / rejected / blocked / none
**Product oracle:** documented / author-confirmed / test-encoded / inferred / unknown
**Oracle fidelity:** authoritative / corroborated / hypothesis / unknown
**Mechanism fidelity:** reproduced / structural / inferred / unknown
**Scenario fidelity:** exact / proxy / synthetic / missing
**Regression assertion disposition:** required-regression / optional-regression / rejected
**Diagnostic mutation disposition:** diagnostic-only / rejected / not-applicable

## Final recommendation
**Implementation verdict:** KEEP CURRENT FIX / REVISE / REPLACE
**Behavioral evidence:** empirical / structural / missing
**Merge readiness:** ready / recommendation only / blocked on evidence / blocked on product oracle / blocked on implementation
**Implementation confidence:** high / medium / low
**Reason:** <concise evidence>

## Required follow-ups
- <only concrete remaining work, or "None">

## Repository oracle gaps
- <durable documentation, invariant, test, or agent-guidance follow-up, or "None">

## Suggested review comments
- <plain-language comment with the concrete behavior and ask, or "None">
```

## Guardrails

- Never post review comments or submit GitHub review events.
- Never approve or request changes on GitHub.
- Never push, commit, create a PR, change branches, stash, reset, or clean.
- Never allow candidate agents to edit the shared worktree.
- Never allow the empirical agent to edit the parent review worktree; it must
  use an isolated child session or disposable detached worktree.
- Do not let test/build output substitute for behavioral evidence.
- Do not let implementation state, model consensus, or a green test substitute
  for an authoritative product oracle.
- Do not treat infrastructure or harness failure as a behavioral red.
- Do not manufacture a red after frozen head passes the approved assertion.
- Do not treat one green run as proof that a production implementation is safe.
- Do not infer producer behavior from a consumer-only test.
- Do not collapse disagreement into consensus; preserve unresolved disputes.
- Never infer the complete change set from `git diff` alone.
- Never promote an unsupported factual claim into a required change.
- Translate internal proof jargon into concrete maintainer language.
