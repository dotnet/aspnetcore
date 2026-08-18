# Cache observer lifecycle evidence

## Change

An output-cache patch adds a correlation field to invalidation notifications.
It does not change observer registration or invocation order.

## Accepted contract

The documented observer contract is per cache layer, not per request. A request
that populates both the process-local and distributed layers invokes the
observer twice. Consumers are required to tolerate repeated invalidation
notifications, and no exactly-once or uniqueness guarantee is documented.

## Retained observations

Base and head each invoke the observer twice for the two-layer case and once for
the one-layer control. The invalidation operation is idempotent, final cache
state is correct, and the head payload contains the expected correlation field
in every invocation.

No input or configuration gains an additional observer invocation because of
the patch.
