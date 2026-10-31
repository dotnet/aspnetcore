# Claim-triggered validation treatment

This directory preserves the exact reviewer-process candidate measured by the
bounded #70106/#57972 comparison. It is an opt-in eval artifact, not the default
reviewer skill and not a demonstrated fix.

- `review-pull-request/SKILL.md` is the measured treatment skill, Git blob
  `a8c9856cd260ad22938a2e1bfe638294abe635f3`.
- `review-pull-request/references/claim-validation.md` is the instruction-owned
  reference, Git blob
  `f9fa346b2001a5cb551748a369c7fd78d8ca22a7`.
- `../../reviewer-capability-treatment.vally.yaml` selects this skill while
  preserving the same two cases, prompt, labels, and rubric as the default
  baseline suite.

The one-trial-per-cell comparison scored 0 for baseline and treatment on both
cases. Treatment #70106 retained the unsupported actionable P2 and violated the
minimal pre-read ledger contract. Treatment #57972 executed the phase but still
missed the required positive. Do not infer a repair or broader reviewer
improvement from this candidate.
