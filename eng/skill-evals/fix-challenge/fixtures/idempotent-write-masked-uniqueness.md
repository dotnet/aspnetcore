# Frozen idempotent-write evidence

## Change

A persistence patch sends a newly supported conditional request through a
completion pipeline that contains both a retry callback and a terminal callback.
The previously supported request shape is unchanged.

## Accepted contract

The public operation and its source-level handoff contract require one durable
commit for each successful request. Retry attempts may prepare more than once,
but commit authority belongs to one winning completion.

## Available observation

The backing operation is an idempotent upsert. The new conditional request
finishes with the expected record and response. Source inspection shows that
both callbacks can reach the upsert call, but the retained run has no counted
adapter, non-idempotent witness, trace identifier, or other observation that
distinguishes one commit from two.

The ordinary request shape produces the same final record on base and head. The
new conditional request is not available on base.

No production correction has been executed.
