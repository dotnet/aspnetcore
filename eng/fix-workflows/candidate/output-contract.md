# Candidate output contract

Read this reference only when writing the candidate artifact.

```markdown
## Fix Workflow Candidate

**Mode:** candidate-review / candidate-propose / empirical
**Candidate role:** proof-vehicle / production-contender
**Approach:** <short name>
**Root-cause hypothesis:** <mechanism>
**Different from current fix:** <mechanism-level difference or not-applicable - no existing fix>
**Files:** <paths>
**Result:** Pass / Fail / Blocked / Proposed
**Product oracle:** documented / author-confirmed / test-encoded / inferred / unknown
**Oracle fidelity:** authoritative / corroborated / hypothesis / unknown
**Mechanism fidelity:** reproduced / structural / inferred / unknown
**Scenario fidelity:** exact / proxy / synthetic / missing
**Regression assertion disposition:** required-regression / optional-regression / rejected
**Diagnostic mutation disposition:** diagnostic-only / rejected / not-applicable
**Refinement disposition:** not-applicable / bounded-refinement / fundamental / unresolved
**Comparison readiness:** ready / needs-refinement / rejected / not-requested

### Proposed change
<specific implementation>

### Evidence
<exact citations and observed output>

### Authority handoffs
<Preserve the supplied canonical mapping when required, and augment each row
with downstream consumer and final observable. Keep each real intermediate
generator/consumer handoff as a separate row even when authorities align, and
represent the declared source and final observable as distinct endpoint stages.
Otherwise record the supplied not-applicable disposition.>

| Stage/handoff | Declared/input authority | Effective authority | Transformation/loss/reconstruction | Downstream consumer | Final observable | Governing contract | Disagreement risk |
|---|---|---|---|---|---|---|---|

### Handoff witnesses
<One row for every mapped intermediate stage when authority handoffs are
required. In candidate-review, predict the witness rather than claiming it ran.>

| Stage/handoff | Path-execution witness | Final-observable witness |
|---|---|---|

### Preflight and iteration accounting
<SDK activation, generated imports/restore state, wrapper argument support,
required assets/bypasses, and semantic/per-version oracle; then record setup
corrections, harness corrections, oracle corrections, and candidate implementation
iterations as separate entries, including the current `<0-3>/3` count. In
candidate-review use Not run.>

### Execution matrix
<one row per requested variant and repetition, or Not run in candidate-review>

### Impacted existing tests
<mapped unchanged tests, results, and justified exclusions>

### Recovery and provenance
<state/value generations and opposite boundary, or Not applicable>

### Proof status
- Finding: empirical / structural / missing
- Scenario: empirical / structural / missing
- Candidate: production-proven / targeted-proven / diagnostic-only / rejected / blocked
- Assertion fidelity: exact / scenario mismatch / incomplete

### Claim verification
- VERIFIED: <claim and source/output>
- CONTRADICTED: <claim and source/output>
- UNSUPPORTED: <claim or None>

### Adversarial findings
- <concrete issue or None>

### Tradeoffs
<net surface versus pre-change base, mapped caller compatibility, coverage>

### Mechanism closure
<why the strongest attack is fundamental, bounded-refinement, or unresolved>

### Recommendation
<In candidate-review: keep current fix / prefer this candidate / combine
specific parts / keep preference open for equal comparison. In
candidate-propose: prefer this candidate / keep preference open for equal
comparison / no viable candidate.>
```

Use `needs-refinement` when the literal production contender failed and the
proposed bounded refinement has not run. In candidate-review mode, do not write
`prefer this candidate` for that state; write
`keep preference open for equal comparison` instead.

Return the complete response to the orchestrator. The orchestrator saves it
unchanged to the invocation envelope's unique `response_path`; a correction uses
a new path and never overwrites the initial response.
