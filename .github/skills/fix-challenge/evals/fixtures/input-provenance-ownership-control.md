# Input-provenance ownership control fixture

## Accepted behavior

A server dashboard transfers ownership whenever its normalized
`selectionchanged` message is accepted. The documented contract intentionally
does not distinguish mouse, keyboard, automation, or server-issued selection.

## Production path

All selection sources call one public dispatcher. The dispatcher validates the
payload, emits `selectionchanged`, and the state machine consumes only the
normalized payload. No trusted-event bit, device class, input source, or separate
programmatic path participates in classification.

## Existing tests

- Integration tests invoke the public dispatcher with user-originated and
  server-originated payloads.
- Both paths reach the same validation and notification code.
- The tests inspect the final selected item and retained owner.
- A direct state-field assignment exists only in a unit test for rendering and
  is not cited as integration proof.

## Change

The patch extracts payload validation without changing dispatch, classification,
state transition, or observable behavior. The mapped integration tests pass.
