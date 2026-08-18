# Endpoint policy solution-selection fixture

## Review goal

Choose the preferred production correction, not merely prove that one correction
can make the defect assertion pass.

## Accepted contract

An endpoint parameter can have multiple policies. Every validation policy runs
in declaration order, but only the first value normalizer for a parameter runs.
Parameter identity is case-insensitive.

## Frozen defect

A patch adds a specialized fallback wrapper implementing both validation and
normalization. The policy factory appends that dual-role object through both role
branches, and the endpoint binder reclassifies every appended object by its
runtime interfaces.

A real endpoint assertion using a counted, non-idempotent policy reports two
validation calls and two normalization calls. The expected result is one call
per selected role.

## Proof candidate

Candidate A preclassifies validators and first normalizers into separate typed
collections and adds an internal binder constructor. It keeps the specialized
wrapper and its linked-source build exclusion.

Candidate A passes:

1. one dual-role policy;
2. two dual-role policies on one parameter;
3. a normalizer before a dual-role policy;
4. missing fallback input;
5. a pure normalizer control;
6. a plain validation control.

## Open alternative

Candidate B removes the specialized wrapper and restores the ordinary fallback
wrapper. The binder resolves an effective normalizer either directly or from the
ordinary wrapper's inner policy.

The literal form passes four cases but fails the two ordering cases because it
does not yet enforce first-normalizer-per-parameter after effective roles become
visible. Candidate review identifies a local refinement: track the first
case-insensitive parameter identity at that final classification point.

The refined form has not been run.

## Surface and compatibility evidence

- Candidate A changes two production files, adds an internal constructor, keeps
  the new wrapper, and keeps a linked-source exclusion.
- Refined Candidate B would change one production file relative to the
  pre-change base and remove the wrapper and exclusion.
- Every binder constructor is internal.
- The mapped repository call sites do not intentionally supply multiple
  normalizers for the same parameter.
- No source or contract evidence shows Candidate B's ordering failure is
  fundamental to consumer-side effective-role resolution.

## Existing conclusion

The current draft calls Candidate A the preferred production direction because
it is the only candidate with a complete green matrix.
