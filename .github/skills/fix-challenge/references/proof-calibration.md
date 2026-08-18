# Proof calibration

Use these rules before turning a plausible finding into a merge-readiness
verdict. Strict red/green establishes causality only for the assertion that was
actually run. It cannot make an unsupported assertion premise authoritative.

## Authority ladder

Classify every expected-behavior claim separately. Prefer the strongest
applicable source:

1. Accepted issue criteria or explicit maintainer clarification.
2. Public documentation, specification, or established compatibility contract.
3. An existing test whose intent is stated by one of the sources above.
4. Reporter observations and retained logs. These establish symptoms, not
   product intent unless they include accepted criteria.
5. A patch author's rationale. This can establish the patch objective, but is a
   hypothesis for product intent and historical cause until corroborated.
6. Implementation state, naming, repository pattern, or model inference.

Do not collapse patch intent, accepted product behavior, and historical cause
into one oracle entry. Record the claim, source, authority level, confidence,
and scope. A weaker source can motivate investigation, but final severity is
limited by the authority supporting the expected result.

`Product oracle` records the source category
(`documented`/`author-confirmed`/`test-encoded`/`inferred`/`unknown`).
`Oracle fidelity` records the authority result after scope is considered. For
example, a patch author can confirm patch intent while the corresponding
product-contract fidelity remains `hypothesis`.

## Candidate-independent assertion approval

Freeze the assertion contract before choosing the correction:

```text
Setup:
Control:
Trigger:
Input producer and provenance:
Production classification path:
Expected assertion:
Independent authority for the expected result:
Allowed perturbations:
Path-execution witness:
Final observable:
Pre-canceled side-effect oracle (when applicable):
Opposite-side control:
Adjacent preserved behavior:
Runtime variants:
Repetitions:
```

Ask whether the same assertion would still be required if the proposed
candidate were unknown. A probe selected because its input falls between the
old and proposed thresholds proves a policy difference, but remains
diagnostic-only unless an independent authority says that input must succeed.

Keep diagnostic and implementation changes separate:

- `diagnostic.diff`: instrumentation or assertions used to understand the
  finding.
- `implementation.diff`: the smallest change intended for the reviewed patch.
- `candidate.diff`: the combined state used during validation.
- `regression_assertion_disposition`: `required-regression`,
  `optional-regression`, or `rejected`.
- `diagnostic_mutation_disposition`: `diagnostic-only`, `rejected`, or
  `not-applicable`.

Classify assertions and mutations in separate fields. A merge-suitable
hardening assertion may be `optional-regression` while a historical mutation
used to challenge it remains `diagnostic-only`.
Use `required-regression` only when authoritative acceptance criteria or a
proven defect makes that exact coverage necessary.

A direct helper call, property assignment, or synthetic downstream notification
can be a useful diagnostic, but it does not satisfy a provenance-sensitive
behavioral assertion unless production uses the same entry and classification
path. Conversely, do not reject synthetic input categorically: when source and
contract establish that provenance is irrelevant and the same dispatcher is the
production boundary, classify the achieved rung normally.

## Multiplicity and change relevance

Evaluate multiplicity against an explicit oracle before interpreting a count:

- accepted criteria, documentation, or a source invariant may require unique
  authority or execution;
- a documented at-least-once, per-layer, per-handler, or fan-out contract may
  permit repetition;
- without either, multiplicity remains unresolved rather than implicitly unique.

Before calling a probe green, write the predicted observation for the claim being
true and false. Identical predictions make the probe non-discriminating. A
masked/idempotent value cannot prove unique execution, but source does not
automatically override a differing final observable either; use both at their
actual proof boundaries.

Treat behavior as pre-existing only when base and head share the same causal path
and final behavior and the change does not make that path reachable for a new
input/configuration or alter multiplicity. If the governing rule requires
uniqueness and a changed path newly exposes unexplained duplicates, preserve
`blocked on evidence` until counted, trace-bearing, or otherwise discriminating
execution evidence resolves the claim.

Run the approved assertion on untouched frozen head before applying a candidate.
If it passes, the blocker is contradicted. Do not mutate working code merely to
obtain red. A historical regression mutation may be useful diagnostically, but
it cannot substitute for a frozen-head failure or justify implementation
severity.

## Fidelity dimensions

Report each dimension independently:

| Dimension | Values | Meaning |
|---|---|---|
| Oracle | authoritative / corroborated / hypothesis / unknown | Why the expected result is required |
| Mechanism | reproduced / structural / inferred / unknown | Whether the causal path was observed |
| Scenario | exact / proxy / synthetic / missing | How closely execution matches the reported situation |
| Candidate | production-proven / targeted-proven / diagnostic-only / rejected / blocked | How broadly the proposed correction was validated |

The final confidence cannot exceed the weakest fidelity relevant to the
verdict. Describe mixed evidence explicitly, for example: "synthetic timeout
policy empirical; historical scheduler mechanism missing."

## Verdict gates

Use `blocked on implementation` only when all are true:

1. The expected behavior has sufficient authority for blocker severity.
2. Frozen head exhibits the predicted failure at a faithful assertion.
3. The supported mechanism connects that failure to the reviewed change.

Use `recommendation only` when evidence supports a repository-pattern,
diagnostic, resilience, or simplification improvement but not a correctness
blocker. Use `blocked on evidence` when the relevance or mechanism of a
behavioral concern cannot be established. Use `blocked on product oracle` when
competing intended behaviors require human clarification.

Missing coverage for a new branch, provider, or input modality is not itself a
behavioral failure. Require a discriminating regression when the branch fixes a
proven defect; otherwise report the gap separately as optional coverage or a
required evidence follow-up. Keep verified adjacent defects in explicit
follow-up scope unless the current change makes them reachable, changes their
behavior, or must correct them to satisfy the accepted contract.

## Implementation-selection gates

Calibrate implementation selection separately from defect severity:

- `not-requested`: the task only needs a correctness verdict; a proof candidate
  may remain illustrative.
- `unadjudicated`: at least one correction is proven, but a materially distinct
  viable alternative has not received equivalent comparison.
- `compared`: the strongest distinct mechanisms were tested or structurally
  closed, but evidence does not justify a unique preference.
- `preferred`: the strongest alternative was structurally impossible,
  empirically rejected after any bounded refinement, or equally tested and
  outperformed on compatibility, repository pattern, and net implementation
  surface.

Do not infer `preferred` from one green, model vote count, implementation order,
or a proof candidate's stronger test history. Trace affected callers before
crediting compatibility. Compare the net fix against the pre-change base so
removing patch-added types, constructors, adapters, or build exclusions counts
in the selection.

When new maintainer or empirical evidence changes these inputs, the prior
selection is stale. Reopen comparison and rewrite the canonical final review.

## Production-proof requirements

One green establishes a causal relationship for the scoped assertion. A
candidate becomes `production-proven` only after:

- the real producer/runtime boundary passes;
- retained evidence shows the trigger reached the changed producer or handoff;
- the final consumer-visible value, state, artifact, UI, or payload is inspected;
- the defect case, an opposite-side control, and the nearest affected adjacent
  behavior pass or have source-backed not-applicable dispositions;
- a matrix varies the dimensions that could falsify the mechanism;
- applicable configurations and platforms are covered;
- the neighboring suite passes; and
- cleanup and interruption paths are exercised.

Record each requirement in `empirical/stress-matrix.md` with these exact labels:
`Real producer/runtime boundary`, `Varied falsification dimensions`,
`Applicable configurations/platforms`, `Neighboring suite`, and
`Cleanup/interruption paths`. Mark each `passed` or
`not applicable - <specific reason>` before claiming `production-proven`.
List the distinct varied rows under an `## Executed cases` heading; duplicate
rows and unrelated tables do not satisfy the matrix.

Repeated runs of one deterministic scenario are repetition evidence, not a
stress matrix. A supported build-property bypass can produce
`targeted-proven`, but cannot imply the bypassed targets or other platforms were
validated.

Scale falsification to the claim. Stateful lifecycle, concurrency, interop, and
observer-timeout claims need the dimensions that can strand ownership or leak
work. A bounded stateless change may need only the real path and its nearest
counterexamples. Never add unrelated scaffolding solely to upgrade a proof
label; retain a lower candidate classification instead.

When an observer timeout does not cancel its inner work, the matrix must inspect
the inner task states after timeout, release or cancel them deterministically,
observe their exceptions, and verify cleanup cannot leak into later tests.

## Correlated convergence

Model diversity is not mechanism diversity. If every candidate receives the
same suggested helper, oracle framing, or diagnostic design, agreement on that
surface is correlated. Record distinct root-cause mechanisms and use consensus
as corroboration only after source or runtime evidence independently supports
the claim.

## Public comment calibration

Before drafting a comment, answer:

1. What did the experiment prove?
2. What did it not reproduce or establish?
3. Is the requested change required for correctness, or recommended for
   consistency, diagnostics, resilience, or simplicity?
4. What maintainer context could change the conclusion?

Translate those answers into ordinary maintainer language. Do not expose the
internal fidelity labels unless they make the request clearer.
