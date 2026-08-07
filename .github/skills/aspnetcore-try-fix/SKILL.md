---
name: aspnetcore-try-fix
description: >-
  Produce and evaluate one independent fix candidate specifically for the
  dotnet/aspnetcore repository. Use whenever an ASP.NET Core issue, PR, or local
  patch needs an alternative root-cause hypothesis, a competing implementation,
  or empirical validation. Each invocation owns one candidate only and must
  differ materially from the current fix or prior attempts. Do not use this
  skill in dotnet/maui or any repository other than dotnet/aspnetcore.
compatibility: Requires a dotnet/aspnetcore checkout, git, and its local .NET/Node toolchain
---

# ASP.NET Core Try-Fix

Generate one independent fix candidate, test it when isolation permits, and
return evidence that another reviewer can compare with other candidates.
When the sibling reviewer skill is available, use
`../aspnetcore-pr-review/references/proof-calibration.md` as the canonical
calibration contract; the rules below are the standalone minimum.

## Repository scope

This skill is intentionally repository-specific. Before using it:

1. Verify the current Git checkout is `dotnet/aspnetcore` from its configured
   remote URL or trusted session metadata.
2. If the repository cannot be verified, stop and report that this skill only
   supports `dotnet/aspnetcore`.
3. Do not reinterpret these instructions for .NET MAUI or another repository.

## Activation guard

Use this skill only when all of these are available:

- A concrete bug or behavior to fix.
- The current fix or a description of prior attempts.
- Target files or an area to investigate.
- A relevant validation command or an explicit reason testing is blocked.

Do not use it for summaries, general architecture questions, CI-only triage, or
ordinary code review with no request for an alternative.

## Modes

### `candidate-review`

Use this mode when reviewing an existing PR or dirty local diff. It is strictly
read-only and safe to run concurrently across models.

- Form a root-cause hypothesis independently from the current fix.
- Propose one concrete alternative.
- Compare it with the current fix only after the proposal is formed.
- Inspect existing validation and identify gaps.
- Do not edit files, run destructive git commands, commit, push, or post.

### `empirical`

Use this mode only in an isolated child session/worktree or when the caller has
explicitly provided a safe restoration mechanism.

- Implement one candidate.
- Run the supplied targeted validation.
- Allow at most three implementation iterations for that same hypothesis.
- Capture the diff and test output before restoring.
- Run attempts sequentially if they share a workspace or server.
- Treat the first green as provisional. Before returning `Pass`, execute the
  caller's lifecycle-derived stress matrix and the real producer/runtime path
  when the claim depends on browser, interop, transport, process, or scheduler
  behavior.

If the parent worktree contains user changes and no isolation/restoration
mechanism is available, return `Blocked` instead of modifying it.

## Inputs

| Input | Required | Description |
|---|---|---|
| `problem` | Yes | Observable behavior and expected behavior |
| `current_fix` | Yes | Current local/PR approach or `none` |
| `target_files` | Yes | Relevant files or repository area |
| `validation` | Yes | Targeted command(s) or known blocker |
| `product_oracle` | Yes | Expected user-visible behavior, source, and confidence |
| `oracle_authority` | Yes | Why the expected result is required independently of a candidate |
| `proof_target` | `empirical` | The exact behavioral claim this candidate must prove or reject |
| `assertion_contract` | `empirical` | Required setup, control, trigger, and assertion |
| `allowed_perturbations` | `empirical` | What the empirical test may vary without changing the scenario |
| `mode` | Yes | `candidate-review` or `empirical` |
| `evidence_manifest` | Yes | Frozen evidence bundle from the orchestrator |
| `artifact_path` | Yes | Exact path for the raw candidate response |
| `prior_attempts` | No | Approaches already tried and why they failed |
| `hints` | No | Advisory review findings or constraints |

## Repository rules

1. Read applicable `AGENTS.md` and `.github/instructions/*.instructions.md`
   files before analysis or edits.
2. Activate the local SDK before any `dotnet` command:
   `source activate.sh` on macOS/Linux or `. ./activate.ps1` on Windows.
3. Select the smallest existing build/test command that covers the behavior.
   For Components issues, follow `src/Components/AGENTS.md`, including browser
   reproduction and a permanent E2E test when behavior is browser-observable.
4. When a test is added for a bug, require strict red/green evidence:
   the same assertion must fail for the missing behavior and pass with the fix.
5. Treat pre-existing or infrastructure failures separately. Demonstrate them
   with an unchanged baseline test before calling them unrelated.
6. A documented build-property bypass is allowed only after showing the
   bypassed target cannot affect the focused behavior. Record it and cap proof
   at `targeted-proven` until the standard build or exact CI path passes.
7. Never change `global.json`, package manifests, lock files, or NuGet
   configuration unless the caller explicitly requests it.
8. Never commit, push, create a PR, post comments, or change branches.
9. Cite exact repository paths and lines, observed output, or primary sources
   for compatibility, browser-support, API-breaking, test-execution, and
   repository-pattern claims. Label claims `UNSUPPORTED` when they cannot be
   verified; unsupported claims cannot justify a required change.

## Workflow

### 1. Inspect independently

Start from the frozen evidence manifest. Read the target code, full surrounding
files, call sites, tests, and relevant history before reading the current fix
in detail. Record any narrow lookup outside the bundle and the claim it verifies.
State:

- The product oracle and whether each expected behavior is documented,
  author-confirmed, test-encoded, inferred, or unknown.
- Whether the source establishes accepted behavior, patch intent, an observed
  symptom, or only a proposed historical cause. A PR-author rationale can state
  patch intent but is not automatically an accepted contract or causal proof.
- The observable failure.
- The likely root cause.
- The code path that produces the behavior.
- The smallest test that can distinguish broken from fixed.
- For stateful behavior, a transition table with the state/invariant, entry,
  ordinary successful exit, interruption exit, owner, and stranded-state
  consequence.

Implementation and tests show current behavior, but do not establish product
intent by themselves. If the expected behavior is only inferred, label it
`UNSUPPORTED` and do not recommend a lifecycle change as though it were
authoritative. If the caller provides an authoritative clarification, update
the hypothesis instead of defending a prior inference.

### 2. Review current and prior approaches

Read the current diff and prior attempts. Explain why the new candidate is
different at the root-cause level, not merely a different edit location.

### 3. Design one candidate

Choose exactly one approach. Prefer:

- Correcting the producer/consumer contract where information is lost.
- Reusing established repository patterns.
- Minimal API and compatibility surface.
- Tests that exercise the real dispatch/runtime path.

Reject candidates that only suppress symptoms, duplicate an existing failed
hypothesis, or require unrelated refactoring.

In `empirical` mode, write the exact assertion plan before editing:

```text
Setup:
Control:
Trigger:
Expected assertion:
Independent authority for the expected result:
Allowed perturbations:
Runtime variants:
Repetitions:
Assertion disposition: merge-candidate / diagnostic-only / rejected
```

Preserve the caller's assertion contract. Do not replace a focused stimulus
with a broader one merely because an existing control is convenient. For
example, changing one adjacent item's size is not equivalent to switching the
layout model for every item.

Before editing, ask whether this assertion would still be required if the
candidate were unknown. A synthetic input chosen because it falls between the
old and proposed thresholds demonstrates the policy difference, but remains
diagnostic-only unless independent authority says that input must succeed.

Keep diagnostic assertions and the proposed implementation separable. Capture
the diagnostic-only diff, implementation-only diff, and combined candidate
diff. Do not recommend committing a slow, configuration-specific, or synthetic
assertion without explicitly classifying it as merge-candidate.

If no alternative is viable, return `NO VIABLE ALTERNATIVE` only after naming
and rejecting at least one mechanism-level alternative with evidence.

### 4. Adversarial self-review

Before declaring the candidate viable, attack it:

- What call path or target framework bypasses it?
- Does it preserve existing handlers and compatibility behavior?
- Does a new public API require API baseline or documentation updates?
- Does serialization/deserialization stay in sync across JS and .NET?
- Can the test pass without exercising the reported bug?
- Is the assertion independently justified, or shaped to favor this candidate?
- What happens for null/default values, repeated events, and opposite state
  transitions?
- For timing-sensitive state, what happens with equal/changed inputs,
  delayed/out-of-order delivery, no-op operations, rapid generations,
  cancellation, disposal, and partial observer/event batches?
- If an observer times out without canceling inner work, what state are those
  tasks in, how are they released, and can they leak into later tests?

Record only concrete concerns with a failing scenario.

### 5. Validate

In `candidate-review`, evaluate whether the supplied validation is sufficient
and predict the differentiating result. Do not claim `Pass`.

In `empirical`, first require frozen-head failure at the predicted behavioral
assertion. Infrastructure, build, stale-element, setup, or unrelated assertion
failures are `Blocked`, not behavioral red. Then run the supplied command with
the candidate and classify:

| Evidence | Result |
|---|---|
| Behavioral red/green, stress matrix, and required producer path pass consistently | `Pass` |
| Targeted assertion turns green but stress/producer validation is incomplete | `Blocked` |
| Test ran and failed | `Fail` |
| Code did not compile | `Fail` |
| Required environment unavailable | `Blocked` |
| Only review/build succeeded, behavior test did not run | `Blocked` |

If the test fails before reaching the requested trigger, classify it as
`Scenario mismatch` under `Blocked` or preserve it as a separate finding. Do
not claim it proves or disproves the requested behavior, and do not stop the
requested proof when a narrower stimulus can faithfully exercise the supplied
assertion contract.

After each run, verify that the executed setup, control, trigger, assertion,
runtime variants, and repetition count match the assertion plan. Report a
per-execution matrix rather than only an aggregate pass/fail result.

Repeating one deterministic case establishes repeatability, not a complete
stress matrix. Vary the dimensions that could falsify the mechanism. Preserve
configuration, platform, and build-bypass limits when classifying proof.

If repeated timing-sensitive runs disagree, return `Fail` until the divergence
is explained and corrected. Never select only the passing run.

### 6. Return a structured candidate

Use this exact shape:

```markdown
## Try-Fix Candidate

**Mode:** candidate-review / empirical
**Approach:** <short name>
**Root-cause hypothesis:** <mechanism>
**Different from current fix:** <mechanism-level difference>
**Files:** <paths>
**Result:** Pass / Fail / Blocked / Proposed
**Product oracle:** documented / author-confirmed / test-encoded / inferred / unknown
**Oracle fidelity:** authoritative / corroborated / hypothesis / unknown
**Mechanism fidelity:** reproduced / structural / inferred / unknown
**Scenario fidelity:** exact / proxy / synthetic / missing
**Assertion disposition:** merge-candidate / diagnostic-only / rejected

### Proposed change
<specific implementation>

### Evidence
<tests run or evidence still required, with exact citations>

### Execution matrix
<one row per requested runtime variant and repetition>

### Proof status
- Finding: empirical / structural / missing
- Scenario: empirical / structural / missing
- Candidate: production-proven / targeted-proven / diagnostic-only / rejected / blocked
- Assertion fidelity: exact / scenario mismatch / incomplete

### Claim verification
- VERIFIED: <claim and source/output>
- CONTRADICTED: <claim and source/output>
- UNSUPPORTED: <claim lacking evidence, or "None">

### Adversarial findings
- <concrete issue, or "None">

### Tradeoffs
<complexity, compatibility, and coverage>

### Recommendation
Keep current fix / prefer this candidate / combine specific parts
```

Write the complete response to `artifact_path` and return that path to the
orchestrator. Do not overwrite another candidate's artifact.
