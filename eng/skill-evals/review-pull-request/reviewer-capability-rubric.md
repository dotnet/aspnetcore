# Reviewer capability semantic rubric

This rubric grades the review outcome, not whether the subject printed a
receipt, used a particular heading, repeated the hidden label, or called a
particular tool.

## Binary decision

Return a passing score only when the response satisfies the named case's
`acceptance` rules and none of its `rejection` rules. Apply these rules to the
claims the reviewer actually makes.

- Ignore a provisional `CANDIDATE_LEDGER` entry that contains only an ID, claim
  type, and changed `file:line`. It is process evidence, not a submitted finding
  or factual rationale. Continue to grade every asserted reason in the final
  `NEW_FINDINGS`, `DISCARDED`, and `UNRESOLVED` sections.
- A natural negative review is not required to volunteer a hidden historical
  diagnosis. Where the case permits `NO_FINDINGS`, accurate silence can pass.
- A positive case explicitly marked `actionable-finding-required` fails when
  the reviewer is silent or returns `NO_FINDINGS`.
- Do not require the exact hidden priority when the response identifies the
  correct actionable defect, supported trigger, and material effect at another
  defensible review priority.
- A known concern already present in the supplied feedback is not a new
  actionable finding. Reporting it as new fails the deduplication case.
- Conditional mechanisms are not actionable findings unless the response also
  establishes supported reachability, changed-line causality, a material
  effect, and any relevant repository obligation.
- A pre-existing effect is not a pull-request regression merely because the
  pull request edits nearby synchronization or cache logic.
- Distinguish the reviewed revision from its merge base. A claim that the head
  still performs a store that its new cancellation check skips is
  substantively wrong even when a narrower post-check concurrency hypothesis
  remains possible.
- Unsupported product applicability does not prove the opposite prohibition.
  If the response asserts blanket impossibility or an explicit provider
  prohibition, grade that claim against the source facts.
- A plausible finding outside the source label is neither automatically a
  false positive nor an automated pass. Return a binary failure and include
  `MANUAL_ADJUDICATION_REQUIRED` in the rationale for every unlisted
  actionable finding, even when the response also contains the required
  finding. Automated acceptance remains withheld until source adjudication.

Process integrity is evaluated separately. Native skill activation, required
guide reads, model and case provenance, prohibited tools, and hidden-reference
staging are necessary gates for accepting a future subject trial, but they
receive no semantic credit here.
