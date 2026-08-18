# Transport input-provenance ownership fixture

## Accepted behavior

A resumable connection keeps the server recovery cursor authoritative until the
client explicitly acknowledges a position with an authenticated inbound frame.
Server keepalives, flush completions, and recovery replays do not transfer cursor
ownership.

## Changed path

The transport listens for authenticated inbound frames and for a generic
`activityobserved` notification. The patch moves cursor takeover into the
generic notification handler. Both client acknowledgments and server-generated
keepalives/recovery writes emit that notification, so server activity now
transfers ownership in direct conflict with the accepted contract.

The patch also adds a separate resume-control-frame branch. It updates the same
ownership state, but no transport-provider integration test executes that
branch.

## Existing tests

- A unit test calls the generic notification handler and observes takeover.
- A transport test assigns the recovery cursor and dispatches a synthetic
  `activityobserved` event; it never sends an authenticated client frame.
- Existing provider-gated tests cover ordinary acknowledgments, but not the new
  resume-control-frame branch.
- No retained runtime run covers a keepalive during recovery or the new resume
  branch.

## Review boundary

The task is to distinguish the direct structural contradiction in the generic
notification change from the separate missing coverage for the resume branch.
