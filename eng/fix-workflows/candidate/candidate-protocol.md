# Candidate protocol

Read this reference in `candidate-review` or `candidate-propose` mode.

Start from the frozen neutral packet. Inspect target code, surrounding files,
callers, mapped tests, and relevant instructions before reading the current fix
in detail. In `candidate-propose`, there is no current fix; do not inspect later
commits, a known solution PR, answer keys, peer outputs, or selection results.
Narrow lookups must record the path and claim they verify.

State:

- the observable failure, product oracle, and its authority;
- one mechanism-level root-cause hypothesis;
- the producer path and smallest distinguishing assertion;
- mapped unchanged tests and uncovered producer branches;
- why the candidate differs from current and prior approaches, or
  `not-applicable - no existing fix` in `candidate-propose`;
- net implementation surface relative to the pre-change base;
- mapped public/internal callers relevant to compatibility;
- whether the strongest concrete attack is fundamental or admits a bounded
  refinement that preserves the mechanism.

When the supplied impact map requires authority handoffs, carry its canonical
rows into the output contract rather than replacing them with a single
"authoritative source." For each row make the declared/input authority, effective
authority, transformation/loss/reconstruction, downstream consumer, and final
observable explicit. Identify the exact stage where information is discarded or
reconstructed and which governing contract decides the final output. Keep one
row per actual handoff through intermediate generators and consumers; do not
collapse stages merely because they currently agree. Predict the path-execution
witness for each mapped stage and the final-observable witness for the shared
comparison assertion.

For stateful behavior, write the transition table requested by the orchestrator.
For suppressed/deferred callbacks or measurements, trace the first recovery
producer event, ownership transfer, value generation/provenance, stale state,
and opposite boundary. Keep the adjacent matrix proportional.

Choose exactly one candidate. Prefer restoring information at the
producer/consumer contract, established repository patterns, minimal compatibility
surface, and real runtime dispatch. Reject symptom suppression, duplicate
hypotheses, and unrelated refactoring. `NO VIABLE ALTERNATIVE` is valid only
after naming and rejecting one real mechanism-level alternative.

Attack the candidate with a concrete scenario:

- Which call path, target framework, producer branch, or consumer bypasses it?
- Are existing handlers, public API, and serialization peers preserved?
- Can the proposed test pass without the reported bug?
- Is its expected result independently required?
- What happens for default/repeated/opposite transitions, cancellation,
  disposal, delayed/out-of-order delivery, partial batches, and no-op work when
  those dimensions apply?

Do not reject an otherwise viable mechanism merely because its literal first
draft mishandles one bounded case. Label the failure `bounded-refinement` when a
local correction follows from an already identified contract and state the
smallest differentiating case the orchestrator should run. Use `fundamental`
only when correcting the failure would abandon the mechanism or violate the
product oracle.

If the refined form has not run, set comparison readiness to
`needs-refinement`. `ready` means the candidate as written can enter the common
comparison matrix; it is not a synonym for "the refinement looks likely to
work."

For a production contender whose advantage depends on an authority handoff,
predict both a disagreement case and an aligned control for the common comparison
matrix. Set the recommendation to `keep preference open for equal comparison`
until the same final-observable assertion runs for both; an intermediate
descriptor alone cannot prefer a candidate.

Return `Proposed`, never `Pass`, because candidate analysis does not execute the
behavior.
