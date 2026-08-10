# Stateless lookup fixture

## Contract

An authoritative contract requires an exact key lookup to select the matching
registered value and preserve the existing fallback behavior for the two
nearest key shapes.

## Frozen patch and evidence

- The patch changes one lookup expression.
- The real consumer has no asynchronous work, retained ownership, callbacks,
  cancellation, disposal, or background processing on this path.
- The exact real-path assertion fails on frozen head and passes with the patch.
- The two nearest key-shape counterexamples pass with the patch.
- Only one local configuration has been executed.

