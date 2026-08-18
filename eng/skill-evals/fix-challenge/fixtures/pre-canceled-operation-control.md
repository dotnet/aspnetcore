# Pre-canceled public operation control fixture

## Accepted behavior

`ReplaceSubscriptionAsync(CancellationToken)` documents that every invocation is
a replacement command: it synchronously revokes the current subscription before
honoring the caller token. A pre-canceled caller prevents creation of the
replacement but does not preserve the old subscription.

## Changed path

The patch extracts revocation into `RevokeCurrentSubscription`. The public method
still revokes first, checks the token, and starts transport work only for an
uncanceled token.

## Existing tests

- A pre-canceled call returns canceled, revokes the old subscription exactly
  once, and performs no transport call.
- In-flight cancellation after transport starts releases the replacement.
- An uncanceled replacement revokes the old subscription and activates the new
  one.
- The mapped neighboring subscription tests pass.

## Review boundary

Evaluate ordering against the documented replacement-command contract rather
than assuming all pre-canceled APIs must have zero side effects.
