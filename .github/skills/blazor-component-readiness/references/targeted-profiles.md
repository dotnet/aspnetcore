# Targeted review starter profiles

These profiles reduce arbitrary ID selection. They are non-authoritative starting points, not
miniature readiness certifications. Record every added or removed ID and why it changes the bounded
question.

Targeted selection changes which IDs are evaluated, not their rubric-owned scope.

## Distributed-package supplement

When a package is publicly listed but exact bytes remain unavailable after the acquisition protocol,
consider adding `LP-05` through `LP-10` and `PI-01` through `PI-04`. Classify blocked direct checks
`not tested`; do not mark them `not applicable`.

## Simple text/value input

Start with:

`A11Y-06,A11Y-07,A11Y-08,A11Y-11,BEQ-02,BEQ-04,BEQ-05,BEQ-06,BEQ-07,BEQ-09,BEQ-11,BEQ-12,BEQ-13,BEQ-15,BEQ-18,BEQ-19,BEQ-21,BEQ-22`

Emphasize binding pairs, input/change/composition behavior, EditContext/validation integration,
callbacks, persistence, cleanup, accessible naming, localization, and documented render modes.

## Binary or selection control

Start with:

`A11Y-06,A11Y-07,A11Y-08,A11Y-09,BEQ-02,BEQ-04,BEQ-06,BEQ-07,BEQ-09,BEQ-11,BEQ-12,BEQ-13,BEQ-15,BEQ-19,BEQ-21`

Add tri-state, checked/selected reconciliation, disabled/read-only behavior, keyboard operation,
forms integration, and accessible state probes.

## Grouped or dynamic selection control

Start with:

`A11Y-06,A11Y-07,A11Y-08,A11Y-09,BEQ-09,BEQ-11,BEQ-12,BEQ-13,BEQ-15,BEQ-19,PERF-02`

Use the dynamic child/value matrix for registration order, selected removal, disabled selection,
keyed reorder, callback order, focus, and accessible selection.

## Numeric or culture-sensitive input

Start with:

`A11Y-06,A11Y-08,A11Y-11,BEQ-09,BEQ-11,BEQ-12,BEQ-13,BEQ-15,BEQ-19,BEQ-21,BEQ-22`

Exercise inbound and outbound typed values, nullable values, invalid text, min/max/step, culture,
formatting, localization, callback ordering, and JS serialization.

## File/upload boundary

Start with:

`SEC-10,SEC-12,SEC-13,A11Y-06,A11Y-07,A11Y-08,A11Y-09,BEQ-11,BEQ-12,BEQ-15,BEQ-16,BEQ-17,BEQ-19`

Exercise untrusted filenames/content metadata, size/type claims, application/server enforcement,
multiple files, cancellation/progress, disposal/listeners, keyboard operation, and announcements.
State attacker capability and server trust boundaries before making security claims.

## Selection record

Every targeted report should record:

- selected starter profile, if any;
- IDs added or removed and the reason;
- package supplement choice;
- probes intentionally excluded by the timebox;
- the prominent statement that targeted validation is not complete readiness.
