# Performance decision framework

Use this to decide whether a given optimization is worth applying. The goal is not maximum micro-optimization everywhere. The goal is to spend complexity budget where it buys the most performance, and to leave simple code simple where performance does not matter.

## The two axes

Every candidate optimization is scored on two axes.

- Impact: how much faster, lower-allocation, or lower-latency the optimized version is, multiplied by how often the code runs. A 10x win on code that runs once at startup has low impact. A 1.2x win on code that runs per request has high impact.
- Complexity: the structural complexity the optimized version leaves in the code. This is its own axis, defined in the next section. Swapping an overload is near-zero complexity. Hand-written SIMD is high complexity.

Optimize by impact divided by complexity (return on investment). Apply the 80/20 rule: a small number of patterns deliver most of the available wins, and those are almost all low complexity.

## Structural complexity: how to score it

Structural complexity is how much an optimization adds to the cost of reading, reasoning about, and safely maintaining the code, independent of how much it speeds things up and independent of how advanced the API looks. A sophisticated API used in a localized, self-evidently correct way is low complexity. A simple-looking change that plants a fragile, non-local invariant is not. It is separate from impact (the win) and from effort (the one-time cost to write it).

Score it by the dominant of these axes:

1. Locality of reasoning: can a reader confirm it is correct from the single expression or method (local), or must they reason across callers, lifetimes, or the whole type (non-local)?
2. Self-evidence of any added invariant: if the change adds a rule the compiler does not enforce (write-before-read, return-buffer-once, matching element size and endianness, single-threaded access), is that rule obvious and confined to the same spot, or subtle and easy for an unrelated later edit to break?
3. Safety surface: does it introduce memory-safety, data-corruption, info-disclosure, or undefined-behavior risk?
4. Shape change: a drop-in swap (overload, attribute, type choice) versus restructured control flow, custom types, or manual state.
5. Verification need: self-evidently correct and faster, or only justified by a benchmark or disassembly.

Levels:

- Low: local reasoning, idiomatic drop-in, no safety surface, and any added invariant is self-evident and contained to the same expression or method. Apply by default on hot paths, including refactors. Examples: span and `Try*` overloads, pooling via a helper, `SearchValues`, `sealed`, a struct enumerator, `field` lazy init, a cached boxed value, `stackalloc` with `[SkipLocalsInit]` (the write-before-read rule is local and self-evident), and a reinterpret such as `ReadOnlySpan<char>` to `ReadOnlySpan<byte>`.
- Medium: mostly local, but it plants one invariant or lifetime/aliasing concern a reader must be told about (comment-worthy), or a modest custom helper or shape change. Correct by inspection, but a careless nearby edit could break it. Apply on hot paths when the win is real, name the invariant in a comment, and keep it contained. Examples: `CollectionsMarshal.AsSpan` with `SetCount`, a custom `[InterpolatedStringHandler]`, ref-returns that expose internal storage, a reinterpret whose correctness depends on endianness or alignment, and the small-object single-or-array struct.
- High: non-local reasoning, multiple or fragile invariants, a real memory-safety or undefined-behavior surface, or a structural rewrite. Reserve for proven hot paths, measure first, isolate behind a small API, comment, and keep a simple reference implementation. Examples: hand-written SIMD, raw-pointer or `unsafe` buffer juggling, custom async method builders, and hand-written state machines.

The discriminator for an added invariant is locality plus self-evidence, not the mere existence of the invariant. That is what keeps `[SkipLocalsInit]` and a `string`-to-bytes reinterpret low, while the same reinterpret becomes medium once correctness depends on endianness you cannot see locally, and `unsafe` pointer arithmetic is high.

Report high-complexity options, do not hide them. When the best-performing approach is medium or high complexity and you are recommending a simpler one, still report the faster option and its tradeoff so the author can choose. Silently omitting it is wrong; the author owns the call on whether the win is worth the complexity on their path.

## The quadrants

Think of impact (low/high) against complexity (low/high).

- High impact, low complexity: always do it. This is the bulk of this skill (span overloads, SearchValues, pooling, TryFormat, source generators, sealing). No justification needed.
- High impact, high complexity: do it only on a proven hot path, and only when measurement shows it matters. Isolate it behind a clean API, comment why, and keep a simple reference implementation in the comments or tests. Manual vectorization and bespoke unsafe code live here.
- Low impact, low complexity: do it when writing new code if it is idiomatic and equally readable. Do not churn existing working code just to apply it.
- Low impact, high complexity: never. Delete the temptation.

## Hot path versus cold path

The same optimization can be mandatory or pointless depending on where it runs.

- Hot path: code that runs per request, per item, per frame, per byte, or in a tight loop. Here, extract as much as is reasonable. If the optimized version is comparable in complexity to the naive version, always use it. If it is more complex, use it when the impact justifies the cost, and measure.
- Cold path: startup, configuration, one-time setup, error paths, admin operations. Here, prefer the simplest, clearest code. Allocations and extra work are fine. Do not add complexity for speed that no one will observe.

How to tell: ask how many times this executes under load, and whether it sits between the caller and a result they are waiting on. Request-processing middleware, header parsing, serialization, routing, and formatting are hot. Reading a config file at boot is cold.

In this repo specifically, treat as hot: Kestrel request and header parsing (`src/Servers/Kestrel`), HTTP/2 and HTTP/3 framing (`src/Shared/runtime`), endpoint matching (`src/Http/Routing` DFA matcher and tokenizer), the middleware pipeline and `HttpContext`/header access (`src/Http`), model binding and the minimal-API request delegate, Components render-tree diffing and expression formatting (`src/Components`), SignalR message framing (`src/SignalR`), and Data Protection encrypt/decrypt (`src/DataProtection`). Treat as cold: host and `IServiceCollection` startup, options binding, configuration providers, one-time builder setup, and admin or error paths. Optimize the hot list aggressively; leave the cold list simple.

## Aggressive by default for cheap wins

Be aggressive (apply without hesitation, including refactoring existing code) for the low-complexity categories:

- Choosing a better overload (span-based, `Try*`, pre-sized capacity).
- Span and slicing instead of substring and copies.
- `stackalloc` for small, bounded, short-lived buffers, paired with `[SkipLocalsInit]` on the method to skip the buffer zeroing when you write before reading.
- Buffer pooling (`ArrayPool`, `IBufferWriter`, `PipeWriter`) for larger or variable buffers.
- Specialized types (`SearchValues`, `FrozenDictionary`) for build-once, use-many data.
- Source generators instead of runtime reflection.
- Sealing types, avoiding closures, avoiding boxing.

Be conservative (apply only on a measured hot path, with isolation and comments) for the high-complexity categories:

- Manual SIMD and hardware intrinsics (`Vector128/256/512`, platform intrinsics). Prefer ready-made helpers such as `TensorPrimitives` or the vectorized BCL APIs instead of hand-rolling.
- `unsafe`, raw pointers, and manual memory management.
- Micro-restructuring purely to nudge the JIT, when it hurts readability.

## Always measure before committing to a high-complexity change

Guidance in this skill tells you what tends to be faster, not what is faster in your exact context. For anything in the high-complexity quadrant, confirm with a benchmark before and after. See [measuring.md](measuring.md) for how.

## Quick checklist

Before applying a non-trivial optimization, answer:

1. Does this code run on a hot path under load? If no, stop unless the change is free and idiomatic.
2. Is there a low-complexity option (overload, pooling, specialized type, source generator) that captures most of the win? If yes, use that first.
3. If the only option is high complexity, do I have a benchmark proving it matters here? If no, measure first.
4. Can I isolate the complex code behind a small, well-named, well-tested API so the rest of the codebase stays simple? If no, reconsider.
