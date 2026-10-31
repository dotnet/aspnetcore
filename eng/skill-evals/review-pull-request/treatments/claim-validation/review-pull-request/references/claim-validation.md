# Claim-triggered source validation

Run this phase after candidate generation and before final acceptance. Validate the
existing candidate set only; do not generate replacement findings in this phase.

For every candidate, record:

1. **Producer and support:** the real producer or application entry point that reaches
   the claimed state, plus source or a primary contract establishing that the producer
   and execution mode are supported.
2. **Schedule and call path:** the exact input sequence, overlap, callback, or call
   chain needed for the effect, including the actual overload and return path.
3. **Base-to-head causality:** the old-side and head behavior for that sequence, and
   the changed line's causal contribution. A mechanism that exists on both sides is
   not a new regression by itself.
4. **Material effect:** the concrete consumer-visible incorrect behavior and the
   source path connecting the trigger to that effect.
5. **Binding obligation:** for an incomplete-fix or new-feature omission, the issue,
   API, repository contract, or advertised behavior that requires this pull request to
   cover the path. Mark this dimension `N/A` for an ordinary diff regression. Ordinary
   supported defaults established at the real source or framework producer boundary do
   not require an additional external-spec quotation. Pre-existing state is not an
   automatic veto when a binding obligation is established.

Accept a finding only when all applicable fields are source-supported. Use
`UNRESOLVED` when evidence is insufficient, unavailable, or ambiguous, including a
plausible material claim that depends on missing support, producer, runtime, or
contract evidence. Use `DISCARDED` only when source affirmatively disproves the
trigger, causal path, material effect, or applicable binding obligation. Use `N/A` for
a genuinely nonapplicable dimension. Neither uncertainty nor pre-existence alone proves
that a candidate is not a defect. Do not turn missing evidence into an assumption, and
do not use a blanket rule that concurrency findings are invalid.

Emit `VALIDATION: claim-triggered`, followed by one compact entry per candidate:

`file:line | producer/support | schedule/path | base-to-head | effect | obligation | ACCEPTED, UNRESOLVED, or DISCARDED`
