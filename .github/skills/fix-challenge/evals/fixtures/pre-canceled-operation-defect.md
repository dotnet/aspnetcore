# Pre-canceled public operation fixture

## Accepted behavior

`RefreshViewAsync(CancellationToken)` documents that a token canceled before the
call causes the returned task to be canceled without starting refresh work or
disturbing an already active refresh.

## Changed path

The implementation first cancels and disposes the active refresh, creates a new
generation, and invokes a browser module. The token is observed only by the
first awaited provider call after those side effects.

## Existing tests

- In-flight cancellation is covered after the provider call starts.
- Superseding an active refresh is covered for an uncanceled invocation.
- A pre-canceled test asserts only that awaiting the returned task throws
  `OperationCanceledException`.
- No test observes the active generation, browser invocation count, or active
  refresh state after a pre-canceled call.

## Review boundary

Use the public contract as the oracle. Keep pre-canceled entry behavior distinct
from in-flight cancellation and disposal races.
