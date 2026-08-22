# Reviewer output contract

Read this reference only during live-head refresh and final synthesis.

## Live-head refresh

Fetch the live PR head and compare it with the frozen SHA. Save the comparison:

- unchanged: proceed;
- unrelated drift: cite why evidence remains applicable;
- relevant source, test, contract, producer, or instruction drift: refresh the
  evidence and impact map, then rerun affected proof and mapped unchanged tests.

Never describe frozen-head evidence as current-head validation.

## Artifact schema

Retain the exact `references/model-policy.v1.json` bytes as
`evidence/model-policy.v1.json`. Hosted orchestration also retains
`evidence/review-input.json`, whose panel must match the policy exactly. A
configured model mismatch fails validation. When authoritative runtime-model
telemetry is unavailable, set the panel runtime identity to `unverified` and the
run to non-comparable.

The `**Path:**` field selects the validator contract:

- `bounded` requires shared evidence, candidates A/B, live-head drift,
  `evidence/skipped-phases.md`, repository oracle, and final review. If the
  candidate is `targeted-proven`, also retain the actual frozen-head log,
  candidate-green log, and empirical result. The result records path execution,
  final observable inspection, the defect case, an opposite-side control, and
  adjacent preserved behavior through retained artifact references and
  `empirical/boundary-matrix.md`. Do not create unused full-path boilerplate.
- `full` requires all four candidates, all four cross-examinations, and the
  complete empirical proof tree defined in `evidence-and-orchestration.md`.

The final proof labels must agree with the path. In particular,
`production-proven` requires `full`; bounded `targeted-proven` requires
candidate-independent behavioral red, identical candidate green, empirical
finding/scenario evidence, a required regression assertion, demonstrated path
execution, final observable inspection, and the scoped boundary controls.

Every final review declares a review goal and implementation-selection status.
`solution-selection` with `preferred` requires
`final/implementation-selection.md`; `defect-adjudication` does not manufacture
that comparison. A later comparison that changes the recommendation requires a
fresh final review and validator run.

`issue-resolution` is used only by `fix-issue` and always declares a selection
status. `adopt candidate` requires preferred selection, a proven candidate, and
a nonempty `final/proposed-fix.diff`. `no change` means the approved assertion
passed on frozen head. `no viable candidate` means a defect was established but
no candidate reached the required proof. `blocked` means oracle, evidence,
environment, or orchestration prevented a decision.

When required, write `final/implementation-selection.md` with this shape:

```markdown
# Implementation Selection

**Shared comparison contract:** <candidate-independent assertion and controls>
**Pre-change base:** <base SHA or frozen base identity>

## Candidate comparison
| Candidate | Mechanism | Literal result | Refinement | Equal-matrix result | Net surface | Caller compatibility | Closure |
|---|---|---|---|---|---|---|---|
| <candidate> | <distinct mechanism> | <result> | <not-applicable/bounded-refinement/fundamental/unresolved> | <passed/failed/not-run/blocked/not-applicable> | <base-relative surface> | <mapped callers> | <open/structural/empirical> |
```

Include at least two materially distinct candidates unless every alternative is
structurally impossible. A structural closure still receives its own complete
row with `fundamental` refinement disposition and `not-applicable` equal-matrix
result. Empirical closure requires a `passed` or `failed` equal-matrix result.
A preferred candidate's equal-matrix result is always `passed`.

Multiplicity and pre-existing dispositions are structured so deterministic
validation can reject contradictions without pretending to interpret arbitrary
prose:

- `Pre-existing disposition` records whether base has the same causal path and
  final behavior.
- `Changed reachability` records whether head exposes a new input/configuration,
  alters multiplicity, remains unchanged, or is unresolved.
- `Multiplicity oracle`, `Multiplicity evidence`, and `Multiplicity disposition`
  record authority, observation, and adjudication separately.

A same-path/same-behavior disposition cannot coexist with newly reachable or
altered multiplicity. A duplicate observation under a uniqueness oracle cannot
be harmless unless the oracle records an accepted exception. A multiplicity
blocker requires relevant changed reachability, a verified duplicate, and the
declared frozen-head and boundary evidence. It also requires a `REVISE` or
`REPLACE` implementation verdict and `blocked on implementation` readiness. Any
behavioral `blocked on implementation` verdict requires retained result and
boundary-matrix artifacts. Semantic questions such as whether a source
invariant is accepted remain evaluator work, not PowerShell text matching.

For a proven candidate, `empirical/result.md` contains exactly one relative,
nonempty artifact reference for each of `Frozen path witness`,
`Candidate path witness`, `Frozen final observable`, and
`Candidate final observable`. `empirical/boundary-matrix.md` contains distinct
`defect`, `opposite`, and `adjacent` case IDs. Opposite or adjacent may be
not-applicable only with a reason and a nonempty evidence artifact containing
the source-backed disposition.

## Claim synthesis

The GPT orchestrator, not a candidate, assigns:

- **Agree:** independently supported and verified, with no surviving concrete
  counterexample.
- **Dispute:** models disagree or required evidence is incomplete.
- **Discard:** contradicted by source, contract, or observed behavior.
- **Unsupported:** no repository evidence, observed output, or primary source;
  exclude it from required follow-ups and severity.
- **Oracle-blocked:** implementation concern is testable but accepted behavior
  remains unresolved.

Promote a behavioral implementation blocker only when frozen head fails an
independently justified assertion at the required producer boundary and the
causal mechanism and oracle support that severity. If empirical work is blocked,
preserve a disputed concern or required evidence follow-up. If it contradicts
the prediction, discard or narrow the finding.

Distinguish a reproduced branch defect from missing discriminating coverage. A
new input branch, provider, or modality without a faithful test is coverage debt
unless source or runtime evidence establishes incorrect behavior. Keep verified
adjacent defects as named follow-ups when they do not arise from the current
change; do not expand the required fix merely because the same review discovered
them.

Choose among equally correct fixes only after the solution-selection protocol.
Compare compatibility backed by caller mapping, affected producer/consumer
coverage, established repository patterns, and net implementation surface
relative to the pre-change base. A first green proof candidate is not a
selection result.

## Repository knowledge

Write `final/repository-oracle.md` only for durable knowledge that was missing or
hard to find:

- express local mechanics through precise names, named methods or variables, and
  smaller responsibilities; name the concrete structural replacement instead of
  vaguely asking for clearer code;
- reserve concise comments for durable nonlocal reasons that structure cannot
  express, not narration of the call graph or implementation;
- keep public API documentation consumer-observable and exclude internal
  implementation details, including control flow or lifecycle state;
- keep lifecycle/ownership invariants near the state machine and executable
  retention/takeover behavior in paired tests;
- cross-cutting review rules belong in repository instructions.

Do not leak model identities, local paths, private conversation, or review-session
mechanics into repository guidance.

## Final report

Write `final/review.md`:

```markdown
# Multi-Model Review

**Orchestrator:** gpt-5.6-sol
**Path:** bounded / full
**Review goal:** defect-adjudication / solution-selection / issue-resolution
**Panel provenance:** policy-pinned
**Comparable run:** no
**Candidate runtime identity:** unverified

## Current fix
<summary>

## Independent candidates
| ID | Model | Root cause | Approach | Assessment |
|---|---|---|---|---|

## Adversarial consensus
<for bounded, synthesize the orchestrator comparison of A/B; for full, synthesize
the saved cross-examination round>
### Agree
- <verified claim>
### Dispute
- <unresolved claim>
### Discard
- <rejected claim>

## Test assessment
<frozen-head, candidate, stimulus-provenance, path-execution, final-observable,
boundary-control, mapped-test, and configuration evidence>

## Implementation selection
**Selection status:** not-requested / unadjudicated / compared / preferred
**Proof candidate:** <candidate ID/name or none>
**Preferred production candidate:** <candidate ID/name or none>
**Alternative closure:** not-required / open / structural / empirical
<comparison summary and `final/implementation-selection.md` reference when
selection status is compared or preferred>

## Proof status
**Frozen-head result:** behavioral-fail / structural-defect / pass / blocked / not-applicable
**Finding proof:** empirical / structural / missing
**Scenario proof:** empirical / structural / missing
**Candidate proof:** production-proven / targeted-proven / diagnostic-only / rejected / blocked / none
**Changed path execution:** demonstrated / structural / blocked / missing / not-applicable
**Final observable:** inspected / structural / blocked / missing / not-applicable
**Boundary controls:** passed / partial / blocked / missing / not-applicable
**Pre-existing disposition:** same-path-same-behavior / not-pre-existing / unresolved / not-applicable
**Changed reachability:** newly-reachable / multiplicity-altered / unchanged / unresolved / not-applicable
**Multiplicity oracle:** requires-unique / permits-multiple / accepted-exception / unresolved / not-applicable
**Multiplicity evidence:** duplicate-observed / single-observed / masked / missing / not-applicable
**Multiplicity disposition:** blocker / unresolved / harmless / not-applicable
**Product oracle:** documented / author-confirmed / test-encoded / inferred / unknown
**Oracle fidelity:** authoritative / corroborated / hypothesis / unknown
**Mechanism fidelity:** reproduced / structural / inferred / unknown
**Scenario fidelity:** exact / proxy / synthetic / missing
**Regression assertion disposition:** required-regression / optional-regression / rejected
**Diagnostic mutation disposition:** diagnostic-only / rejected / not-applicable

## Final recommendation
**Implementation verdict:** KEEP CURRENT FIX / REVISE / REPLACE / ADOPT CANDIDATE / NO CHANGE / NO VIABLE CANDIDATE / BLOCKED
**Behavioral evidence:** empirical / structural / missing
**Merge readiness:** ready / recommendation only / blocked on evidence / blocked on product oracle / blocked on implementation
**Implementation confidence:** high / medium / low
**Reason:** <calibrated evidence>

## Required follow-ups
- <concrete remaining work or None>

## Repository oracle gaps
- <durable follow-up or None>

## Suggested review comments
- <plain-language draft or None>
```

`Candidate proof` describes the named proof candidate. When the preferred
production candidate differs, its own row must contain completed equal-matrix
evidence; proof from another candidate cannot establish the preferred
candidate's behavior.

Draft comments as maintainer-facing text: visible failure, causal path, requested
change, and a concrete example when useful. Translate internal terms such as
oracle, ownership, and proof ladder. State what an experiment does not prove.
Never post the draft.

When selection is `unadjudicated`, describe required behavioral invariants but
do not prescribe the proof candidate or call it best/preferred. When selection
is `preferred`, identify the equally tested or structurally closed alternative
and summarize why the selected mechanism won.
