# Reviewer concurrency capability development suite

This specialized, opt-in suite measures source-grounded discovery and restraint
in one Copilot context over curated ASP.NET Core pull-request projections. It
does not replay the hosted review service, prove coordinator/per-guide-worker
fidelity, estimate population accuracy, compare model families, or demonstrate
a reviewer repair.

The reduced suite retains the representative #70106 restraint case and the
#57972 supported-parallel positive needed to prevent blanket silence from
passing. An earlier supplied-candidate pilot and unused development cases remain
preserved in prior branch history and retained session artifacts, but are not
part of the current suite. Label revision 3 records the pruned two-case
development set; the exact revision-2 label artifact used for the observed
#70106 measurement remains identified by commit, Git blob, SHA-256, and run ID.

## Cases

| Case | Frozen source and cutoff | Source-backed outcome |
|---|---|---|
| #70106 restraint | Head `2edf3761692afe15475075eb2fc0088566162045`; merge base `10bc886bfa321718276619d1077cc14eaf48456f`; feedback before `2026-10-31T12:50:58Z` | `NO_FINDINGS` is valid. Head prevents an event during the awaited token request from repopulating the shared cache, although that request may use its local header. The residual post-check/no-await race requires genuinely concurrent provider notification, whose supported product contract is not established, and the materially equivalent stale-cache effect predates the pull request. A P2 lock demand, blanket parallel-impossibility claim, or invented provider prohibition fails. |
| #57972 supported-parallel positive | Head `0a8373f036c151ae2773f4e5a1cb4aeccade6bce`; merge base `542f2b9877da67e90cca6ce1b65aa9557fbb2aa8`; feedback before `2024-09-19T20:14:43Z` | The advertised concurrent-document fix converts several caches but leaves `_operationTransformerContextCache` as `Dictionary` on a keyed-singleton service whose concurrent HTTP request path calls `TryAdd`. The field predates the pull request, but converting every shared mutable cache is a binding obligation for this concurrency fix. Follow-up `cb01ee07234372356dbd64cffd575e0603c91a38` converts it and changes the test to `Parallel.ForAsync(0, 150, ...)`; the author reported that the stronger test exposed the omission. |

## Blinding and projections

Public case and stimulus identifiers are neutral PR numbers. Each bundle
contains the complete frozen diff and changed-file list, ordinary Git blobs for
every changed file plus named producer, consumer, and contract paths, and
feedback strictly before the label-source comment. Expected outcomes, later
fixes, later maintainer feedback or opinions, and the rubric remain outside
subject staging. Legitimate feedback that predates each cutoff is intentionally
staged.

The PR descriptions are not current mutable bodies. Each is the latest exact
GitHub `userContentEdits.diff` body version before its cutoff. When a PR has no
body edit history, the current body is retained only with the recorded empty
history and pre-cutoff creation identity. Historical labels are unavailable
from that API, so labels and post-cutoff decision metadata are omitted rather
than projected from present state.

`grading_environment.files` adds labels and the semantic rubric only to
Vally's disposable grader workspace. This is a configured staging boundary,
not proof of host-level secrecy on a shared machine. The accepted claim is
limited to no configured subject exposure and no observed trajectory access.
The grade result records configured evidence inputs and source-consistent
rationale, but exact grader file-read events were not retained.

The focused #70106 projection includes runtime-aware dispatcher and host-builder
source, but not the broader #66729 and #54365 support history used in the
separate supplied-candidate calibration. Absence of that history cannot prove
either supported arbitrary off-thread notifications or a binding provider
prohibition. This suite is a curated natural review, not a full historical
support replay.

## Execution and scoring

`reviewer-capability.vally.yaml` uses the pinned Vally 0.19.0 and Copilot
1.0.80/SDK 1.0.15 path. Every trusted caller prompt explicitly configures the
review skill's documented offline exception: apply the same routed guide texts
and acceptance gates in one context, spawn no workers, and report
`PATH: single-reviewer`. The suite therefore measures one-pass review behavior,
not independent guide-worker synthesis.

The prompt grader uses Vally's supported `gradeTrial` path with
`evidence: [trajectory, repo]`. Native skill activation, complete assigned
guide reads, exact model, case, and run provenance, prohibited tools, answer
access, and the single-reviewer receipt are independent integrity gates and
receive no semantic credit. Automated acceptance is necessary but not
sufficient; every subject output still requires source adjudication, especially
a plausible finding outside the label key.

The canonical `prepare-review.cs --check` path is live-target and preparation
version bound, so it cannot attest these historical suite-local projections.
The suite does not manufacture current heads or synthetic Git identities.
`check_reviewer_capability.ps1` instead validates every projected source as an
ordinary Git blob, complete diffs and changed-file metadata, exact historical
body and feedback cutoffs, common/case overlay equality, artifact hashes,
answer separation, and frozen reviewer criteria.

Five derivative trajectories exercise the same semantic scoring path:

1. revision-qualified #70106 restraint;
2. unsupported #70106 P2 lock demand;
3. invented provider-prohibition justification;
4. correct #57972 supported-parallel finding;
5. structurally complete blanket silence on the #57972 positive.

Controls retain the same structured review format and `PATH: single-reviewer`
receipt. They are judge-development controls, not subject executions,
historical replays, or independent votes. Vally `grade` can exit zero for a
valid negative score, so control validation must assert each named semantic
score against its expected relation.

## Observed bounded pilot

The first #70106 command failed before model execution because Vally resolves
`environment.skills` relative to the eval specification. That error is retained
as reviewer CLI attempt 1. After correcting only the relative path, the
authorized compatibility recovery ran once as reviewer CLI attempt 2, run
`3cf0bd32-d93e-41ca-8650-547caafe9379`.

That unhinted GPT-5.6 review reproduced the original failure class. It emitted
an actionable P2 lock-and-generation demand for the post-check/cache-store
window and assumed multithreaded WebAssembly reachability without establishing
a supported producer or a binding requirement to make the pre-existing
mechanism atomic in this pull request. The output did not explicitly say that
the field or mechanism was newly introduced. The associated Gemini grade was
binary 0 for the intended source-backed reason.

The first associated-grade command and the first three control-grade commands
failed before judge execution because `--stimulus` is invalid for saved
`EvalOutcome` input. Corrected grade commands then produced:

- revision-qualified #70106 restraint: 1;
- unsupported #70106 P2 lock demand: 0;
- invented provider prohibition: 1, unexpectedly.

The third control was ambiguous because the false guarantee appeared under
`DISCARDED`; the judge interpreted the text as a proposition the reviewer
rejected rather than a justification it relied upon. Its retained run input and
result remain the measurement record. The current trajectory states the
reliance explicitly, but that revision is unmeasured. Both #57972 controls and
the #57972 subject case are also unrun.

Ledger:

- reviewer CLI attempts `2/6`, actual reviewer model calls `1`;
- associated-grade CLI attempts `2/6`, actual associated judge calls `1`;
- judge-only control CLI attempts `6/6`, actual control judge calls `3`.

The retained subject run also reported that `projection-map.json` was not staged
beside the bundle manifest. The measured run is preserved unchanged; the
current spec stages that referenced metadata for future use.

These results establish one exploratory, non-gating recurrence in the
configured one-context path. They do not establish a recurrence rate, hosted
coordinator/worker reliability, comparative priming effects, or a demonstrated
repair. The saved grade used the #70106 fixture bundle as repository evidence
rather than the original complete subject overlay workspace; exact judge
file-read events were not retained. Its binary 0 is supported by the frozen
label and manual source adjudication, not independent ground truth supplied by
the judge.

## Claim-triggered validation experiment

A later bounded comparison tested one process candidate: after initial
candidate generation, expose only candidate ID, claim type, and changed
`file:line`; then read an instruction-owned validation reference from the
native installed skill and adjudicate producer/support, exact schedule,
base-to-head causality, material effect, and any applicable incomplete-fix or
new-feature obligation. The reference distinguishes `UNRESOLVED`,
`DISCARDED`, and `N/A`, and explicitly rejects blanket concurrency and
pre-existing-state vetoes.

The comparison froze the current two-case projections, ordinary caller prompt,
labels, rubric, GPT-5.6 subject, Gemini judge, Vally 0.19.0, Copilot CLI 1.0.80,
and Copilot SDK 1.0.15 before all four attempts. The baseline used reviewer
skill blob `b11fe9fe79b5d848687ba1c93362f4aaf22bfe53`; the treatment used
`a8c9856cd260ad22938a2e1bfe638294abe635f3` plus reference blob
`f9fa346b2001a5cb551748a369c7fd78d8ca22a7`. Each arm ran each case once with
one worker and no retries, followed by exactly one associated grade.

| Arm | Case | Run | Integrity | Score | Observed outcome |
|---|---|---|---|---|---|
| baseline | #70106 | `80264656-7637-4e1d-b354-d7fbf48e22e4` | pass | 0 | A different unsupported concurrency claim targeted line 69 CTS replacement between concurrent requests and the displaced token remaining uncanceled; it was not the original line-77 event-after-check schedule. |
| baseline | #57972 | `88337c83-1a44-4be3-ac7b-51eb2869b3ff` | pass | 0 | The required `_operationTransformerContextCache` finding was missed; novel `OpenApiSchemaStore` claims require manual adjudication. |
| treatment | #70106 | `2431eec9-973a-4f8f-ade6-fe399ec11a2d` | fail | 0 | The installed reference was read with exact bytes after the ledger, but the provisional ledger included reasoning and the original line-77 check-to-commit demand persisted even though the output recognized old/head equivalence. |
| treatment | #57972 | `909c0edd-0f97-490d-8ee0-4f86cbae1cb9` | pass | 0 | The phase executed, but the required positive was still missed; a novel `OpenApiSchemaStore` cache-factory claim requires manual adjudication. |

The treatment reference read was delivered from the activated skill installation
with exact path, byte hash, and event ordering in both treatment runs. That
receipt proves phase delivery only, not semantic validation. Both arms scored 0
on both cases, so this experiment shows no attributable repair. The treatment
also failed its own minimal-ledger contract on #70106. Because both #57972 arms
missed the required finding, the two score-0 results do not demonstrate a
treatment-caused recall regression. No attempt was retried, no controls or
other model families were run, and no guide or label was retuned after
observing the results. Accounting closed at four reviewer CLI attempts and four
actual reviewer calls, plus four associated-grade CLI attempts and four actual
judge calls, with zero execution errors. That authorized comparison budget is
exhausted; no additional paid run is part of this draft.

This is exposed-two-case, one-trial-per-cell development evidence. It does not
establish a treatment effect, general reviewer improvement, or hosted
coordinator/worker arbitration behavior. The failed process candidate remains
in this draft only as the opt-in eval-local
`treatments/claim-validation/review-pull-request/SKILL.md` and reference,
exercised by
`reviewer-capability-treatment.vally.yaml`. The live default reviewer skill is
the unchanged baseline. This preserves the exact measured proposal and negative
result without silently promoting it as a fix.

## Claim-neutral evidence-scout comparison

A second opt-in process candidate tested a different mechanism from the failed
same-context validation phase:

1. a fresh evidence-scout execution reads every file in the frozen projected
   head, merge-base, and base-tip source sets plus the routed guidance and
   review inputs;
2. it returns only a strict JSON inventory of symmetrically enumerated source
   observations and exact source citations; and
3. a fresh ordinary reviewer receives only the validated inventory, verifies
   cited source itself, and produces the sole semantically graded review.

The scout schema has no defect, severity, actionability, repair, disposition,
support, schedule, or obligation fields. Model-authored fields are closed
categories, exact source tokens, paths, line ranges, and citation IDs. Exact
source and pull excerpts are validated byte-for-byte and are not rejected when
the original text itself contains judgment words. Coverage is enforced against
the trusted finite set of every file in the frozen projected head source set,
not a list declared by the scout. This proves projected-file coverage only; it
does not prove that unprojected repository source was exhaustively retrieved or
that the inventory is semantically neutral or useful.

The generated finalizer spec stages the exact validated inventory outside
`review/bundle`. Its native `review-pull-request` skill is byte-identical to the
frozen default except for one allowlisted section authorizing an
instruction-owned evidence-scout reference. The ordinary caller prompt, frozen
source and guidance, labels, rubric, GPT-5.6 subject identity, and Gemini judge
identity remain unchanged. The checker binds the discovery output bytes and
hash to the generated finalizer spec, verifies native skill activation and
input reads, rejects configured or observed answer-key access, and requires the
finalizer to re-read cited source after consuming the scout. These receipts
prove contract execution and artifact delivery, not truth, secrecy,
independence, or review quality. The treatment also requires one forced
complete `view` per identity-receipted input so raw-byte receipts are
deterministic; the unchanged baseline has no added tool-use constraint.

The preregistered comparison froze plan SHA-256
`8d21136fdac1953c5b09b1290a63f09ff9a188616781361b3bd7c3e0ec527dc0`,
candidate-patch SHA-256
`1c4b25b6005342ca76c9152e2e12330609d132ea3af66cdc6581551d5bc57088`,
and candidate tree `783d4564e9e27b95647d0b4ddf5b3f614a828b1e`.
Commit `897fa4efabf3ed9388233d19b320cf4dc876d990` published that exact
tree before execution. Baselines used one reviewer execution each. Treatments
used one scout and, only after a valid scout, one fresh ordinary reviewer. Only
ordinary reviews were graded. The cap was six planned GPT-5.6 reviewer
executions within eight authorized slots and four Gemini grades, with one
worker, zero retries, and no recovery use of the two reserved slots.

The pinned npm and platform packages were `1.0.80`, while the exact common
Copilot binary reported `1.0.105`; its SHA-256 was
`4f590f1f60f3f2bd4cf731cddf50360672d3c97d93150bd05ffa496d65e326f8`.
Both arms used that same binary. This is a fresh paired comparison, not a
controlled comparison with earlier runs identified only by package labels.

| Stage | Case | Run | Integrity | Score | Observed outcome |
|---|---|---|---|---:|---|
| baseline | #70106 | `d7baf99f-23e1-4526-bee6-caccb66630bf` | pass | 0 | Reported an actionable P2 race in concurrent line-69 CTS replacement, not the original line-77 schedule. The grade classified this distinct candidate `MANUAL_ADJUDICATION_REQUIRED`; it remains pending rather than a source-proven false positive. |
| baseline | #57972 | `bd427ddc-78ed-4df7-810c-31d8b52f26a3` | pass | 0 | Missed the required `_operationTransformerContextCache` finding. Two `OpenApiSchemaStore` findings remain pending manual source adjudication. |
| scout | #70106 | `5500d779-3b38-419d-9efb-b5050179d670` | pass | N/A | Produced a valid 27-item, four-file projected evidence inventory with exact source-bound citations. |
| finalizer | #70106 | `52d41550-8ee2-493e-a780-8a17f6f1b204` | fail | 1 | Returned semantically correct restraint with `NO_FINDINGS`, but read `review/bundle/required/pull/body.source` only after the installed reference and staged inventory. This violates the frozen core-input-before-inventory protocol, so the treatment cell remains red despite its semantic score. |
| scout | #57972 | `bde60a8f-e65d-4949-a3b4-80e4ec0008e0` | fail | N/A | Returned six coverage rows with no evidence links and only two duplicate `pull_scope_statement` entries for the same body span, so the intended resource inventory was not demonstrated. Its excerpt matched lines 1-15, but `E001-C01` used raw-file SHA-256 `dc669f96760d58998daaf886f01ab472abb06c4738b99f2f8266e1dde2f892c9` instead of required LF-joined excerpt SHA-256 `f53fd9a2ed9d6e60c2387d0ec202bbe5efbe3d8c539afdc384f01c7554d0db74`. |
| finalizer | #57972 | `64632659-10ab-4e83-9c9d-bc453be8be4e` | skipped | N/A | Not launched because its scout failed the preregistered exact-byte gate; no grade was launched. |

The #70106 finalizer returned `NO_FINDINGS` and received judge score 1; that is
the saved semantic outcome, not proof that it understood the unsupported
residual schedule. The candidate still failed because the preregistered
mechanism required both semantic and protocol Integrity. Conversely, the late
read does not show that evidence scouting can never help. The #57972 finalizer
never ran, so this comparison provides no treatment result for required-positive
recall and cannot show a treatment-caused recall regression.

Accounting closed at five reviewer executions and three grades, with zero
retries, controls, variants, or platform execution errors. The malformed
#57972 scout consumed its execution and skipped only its dependent finalizer
and grade. Three authorized reviewer slots were unused: the skipped dependent
finalizer and the two slots reserved as unavailable rather than retry capacity.
Because neither treatment case passed both unchanged semantic and Integrity
gates, this candidate is not a demonstrated repair and remains disabled.

Implementation paths:

- `reviewer-capability-evidence-scout.vally.yaml`
- `treatments/evidence-scout/`
- `check_evidence_scout.ps1`
- `test_check_evidence_scout.ps1`

A post-freeze verifier repair changed only
`check_reviewer_capability.ps1`. In the #57972 single-overlay path, PowerShell
unrolled one expected launch-file entry to a scalar, so appending the required
projection-map entry concatenated strings and falsely rejected the saved
bundle. Commit `545c4dc4d00a87533c608f42a18e7a988ff19794` typed that value as
`string[]` and made the count comparison scalar-safe. The frozen checker was
blob `0c81ed499b5b18e2bd8a4b5e5e73276904822775`; repaired validation used
blob `77b47397ac7d4bdd9bf066a7cbf5d10aefca422b`. The original freeze plan,
candidate patch, subject outputs, grades, prompts, skills, fixtures, labels,
rubric, discovery extraction, generated finalizer input, and acceptance rules
remain unchanged. The repair occurred after both baseline executions and
before the treatment executions; it only reads saved artifacts and was used to
revalidate both baselines without rerunning either model. The existing checker
self-test now exercises the #57972 one-overlay source bundle plus projection
map: the frozen checker fails that focused case on scalar `Count`, while the
repaired checker passes it.

The live default reviewer remains unchanged at blob
`b11fe9fe79b5d848687ba1c93362f4aaf22bfe53`.

## Revision rules

- Revise a source label only when new primary evidence contradicts it; retain
  the old revision and reason.
- Revise the grader only when same-path controls expose substantive semantic
  discrimination failure, never to require hidden wording or formatting.
- A harness, judge, loading, or role-fidelity failure is not reviewer evidence.
- Report case outcomes and limitations directly; these related development
  cases do not support a population rate.
