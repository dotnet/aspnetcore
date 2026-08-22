# Empirical candidate protocol

Read this reference only in `empirical` mode, together with the sibling
reviewer's `references/proof-calibration.md`.

## Assertion plan

Before editing, write:

```text
Setup:
Control:
Trigger:
Expected assertion:
Independent authority:
Allowed perturbations:
Impacted existing tests:
Path-execution witness:
Final observable:
Opposite-side control:
Adjacent preserved behavior:
Suppressed interval:
Resume trigger:
Pre/post value generation:
Runtime variants:
Repetitions:
Regression assertion disposition:
Diagnostic mutation disposition:
Authority-handoff mapping:
Preflight status:
Setup corrections:
Harness corrections:
Oracle corrections:
Candidate implementation iterations: <0-3>/3
```

Preserve the caller's assertion contract. A broader, easier stimulus is not
equivalent. Candidate-shaped thresholds remain diagnostic-only unless accepted
criteria require that exact result. Keep diagnostic assertion,
implementation-only, and combined diffs separate.

When the impact map requires authority handoffs, preserve its canonical rows in
the final artifact and use the output contract's expanded fields to name the
declared/input authority, effective authority, transformation/loss/reconstruction,
downstream consumer, and final observable. The assertion plan must include a
disagreement case and an aligned control before empirical evidence can prefer a
candidate. Do not promote any metadata or state source to universal authority.

## Preflight and correction accounting

Before frozen behavioral execution, preflight:

1. local SDK activation;
2. generated imports and restore state;
3. area-wrapper support for every requested argument or filter;
4. required generated or static assets, including the relevance proof for any
   proposed build-property bypass and an explicit record of its reduced build
   fidelity; and
5. a candidate-independent semantic oracle, including the expected
   per-version representation when versions encode the same behavior
   differently.

Record corrections in three separate categories:

- `setup`: SDK activation, restore, generated imports, and required assets;
- `harness`: wrapper/runner invocation, filtering, or result-adapter corrections
  that do not change the approved behavior;
- `oracle`: a version-specific representation correction derived from the same
  independent semantic contract.

These corrections are not behavioral red and do not consume the three candidate
implementation iterations. Start implementation iteration counting only after
untouched frozen product code reaches the approved assertion through the required
trigger and final observable. Freeze the corrected harness and semantic assertion
for identical head and candidate execution. Until then report preflight
incomplete or `Blocked` and keep the candidate implementation counter at `0/3`.

## Execution

After preflight, run mapped unchanged tests and the approved assertion on
untouched frozen head first. Do not create a mutation to manufacture red when
head passes. Build, harness, setup, oracle-representation, stale-element, or
infrastructure failures are `Blocked`, not a behavioral red.

If head fails at the predicted assertion, apply one candidate and run the
identical assertion. Allow at most three candidate implementation iterations for
the same hypothesis, counted from the first candidate edit after the qualified
frozen-head execution. Verify each execution matched setup, control, trigger,
assertion, runtime variants, and repetitions. Retain evidence that the trigger
reached the changed producer or handoff. Define and inspect the final
consumer-visible value, state, artifact, UI, or payload. A failure before that
path executes is not behavioral red for the candidate.

When the caller supplies a solution-comparison contract, preserve the same
candidate-independent defect assertion and common controls used for the other
mechanism. Classify a literal candidate failure as `fundamental`,
`bounded-refinement`, or `unresolved`. One bounded refinement may be applied and
rerun without pretending the literal candidate passed; retain both results.

Before treating an observation as red or green, record its predicted value under
both the claim and its negation. If they are identical, return `Blocked` until a
discriminating witness is available. A counted, trace-bearing, or deliberately
non-idempotent test probe may supply that witness when independent authority
requires unique execution or duplicate side effects are plausibly material.
This is test instrumentation, not a production mutation; do not require it when
repetition is permitted and immaterial.

| Evidence | Result |
|---|---|
| Frozen head passes approved assertion | `Pass` with no defect; no correction |
| Behavioral red/green plus required producer and falsification cases pass | `Pass` |
| Targeted green but required producer/stress evidence incomplete | `Blocked` |
| Test or compile fails because of candidate | `Fail` |
| Required environment or faithful scenario unavailable | `Blocked` |

The first green proves only scoped causality. Vary dimensions that can falsify
the mechanism, not a generic matrix. Repeated identical passes are repeatability.
Run the defect case, one opposite-side positive control, and the nearest adjacent
producer or consumer behavior the mechanism can affect. Use a source-backed
not-applicable disposition rather than inventing an unrelated control.
For recovery, exercise the first real producer event and opposite boundary. For
geometry/provenance, use a fixed control and bounded realistic variable
perturbation. For shared filters, cover mapped branches/consumers. For timeouts,
inspect and deterministically release inner work.

For serialization and compatibility claims, vary the bounded set of
representation and accessor/constructor paths that can change the external
contract, then run directly impacted unchanged tests. Do not promote one
targeted green while an affected producer/consumer variant remains untested.

A build-property bypass must be proven irrelevant and caps the result at
targeted-proven until standard build or exact CI passes. Preflight accounting does
not relax that cap. Disagreement among timing-sensitive repetitions is `Fail`
until explained. Never select only passing runs.

Report net production surface relative to the pre-change base and map callers
before claiming compatibility advantage. A proof candidate can be
`targeted-proven` while production preference remains unadjudicated.

`production-proven` requires every mapped unchanged test, a behavioral frozen-head
red, identical candidate green, real producer path, authoritative-enough oracle,
required regression, and relevant falsification cases. Otherwise preserve the
lower truthful label.
