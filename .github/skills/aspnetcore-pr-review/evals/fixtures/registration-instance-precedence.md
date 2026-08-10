# Registration instance precedence fixture

## Contract

The documented registration contract says that a compatible handler instance
configured by the caller takes precedence over type-based fallback activation.

## Frozen patch

The patch removes an assignment that replaced the configured instance with a
type-created handler. A checked-in regression asserts only that resolution
returns a compatible handler.

## Retained evidence

- The exact configured instance is distinguishable from the fallback instance.
- An assertion that checks exact instance identity passes on untouched patched
  head through Options, dependency injection, and the real consumer.
- Reintroducing the removed assignment makes that assertion fail.
- A narrow compatible/incompatible/missing registration matrix passes.
- The local focused test requires an unrelated frontend-build target bypass.

