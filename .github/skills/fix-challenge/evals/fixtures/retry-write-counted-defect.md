# Frozen retry/write evidence

## Change

A response-caching patch routes conditional writes through a retry coordinator.
The new path can reach both the retry completion hook and the ordinary
completion hook.

## Accepted contract

Accepted criteria and the coordinator's source contract require one committed
store write per successful request. Retries may repeat preparation, but only the
winning attempt may commit. This is an exactly-once authority rule, not an
expectation inferred from the proposed correction.

## Retained observations

The store operation is an idempotent set, so the final bytes and response are
identical after one or two commits. A counted store adapter on the real request
path records:

| Case | Base commits | Head commits | Candidate commits | Final bytes |
|---|---:|---:|---:|---|
| Conditional write, retry wins | 1 | 2 | 1 | identical |
| Conditional write, first attempt wins | 1 | 1 | 1 | identical |
| Unconditional write | 1 | 1 | 1 | identical |

The candidate centralizes commit authority in the winning-attempt handoff. The
identical counted assertion fails on frozen head, passes with the candidate,
and the supplied controls pass.
