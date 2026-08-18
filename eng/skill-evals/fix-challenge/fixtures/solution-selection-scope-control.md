# Correctness-review scope control

## Requested review

Determine whether the current patch is correct and provide actionable review
feedback. Do not choose or implement the best production correction.

## Accepted contract and evidence

A request filter must release its pooled lease exactly once after a successful
dispatch. Untouched frozen code releases the lease twice on one concrete
exception path. The same real-path assertion passes after a local proof candidate
makes ownership transfer explicit.

The defect case, normal success control, and cancellation control pass with the
proof candidate. The proof is targeted to one local configuration. Two other
implementation ideas were suggested but not evaluated.
