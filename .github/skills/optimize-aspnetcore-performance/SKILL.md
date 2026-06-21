---
name: optimize-aspnetcore-performance
description: >-
  Write and review performance-sensitive code in the dotnet/aspnetcore repo. USE FOR optimizing
  hot paths (per-request in Kestrel, routing, middleware, headers; per-frame in Components render
  tree; per-message in SignalR), removing allocations, choosing span and buffer-based APIs,
  picking the right collection or search primitive (SearchValues, FrozenDictionary), reusing the
  repo's shared helpers (ValueStringBuilder, ValueListBuilder, PooledArrayBufferWriter, the
  ThrowHelpers, PinnedBlockMemoryPool, CancellationTokenSourcePool) versus BCL types, source
  generators over reflection, trimming/AOT-safe authoring, and reviewing a PR for performance.
  Also USE FOR "make this faster", "remove allocations", "is this the fastest API here", and
  "which helper should I use". DO NOT USE FOR authoring Roslyn analyzers (use author-analyzer-skill),
  feature correctness, general non-repo .NET app code, or runtime-internal GC/JIT tuning that needs
  no code change.
---

# Optimizing performance in ASP.NET Core

This repo sits on the hot path of every ASP.NET Core app in the world. Code in `src/Servers`, `src/Http`, `src/Middleware`, `src/Components`, and `src/SignalR` runs per request, per frame, or per message, under load, in code others cannot change. Hold a high bar there: remove allocations, prefer span and buffer APIs, and reuse the shared helpers this repo already maintains.

This skill assumes the repo's general conventions in [.github/copilot-instructions.md](../../copilot-instructions.md) (C# 13, file-scoped namespaces, `is null`, nullable, formatting). It adds only the performance specifics. For writing analyzers that enforce these patterns, use the `author-analyzer-skill` instead.

## How to use this skill

For a full authoring or review pass, follow the Authoring and reviewing workflow near the end of this file; it ties the steps below into a traceable set of findings. To apply individual guidance:

1. Decide if it is worth it. Read [decision-framework.md](references/decision-framework.md): be aggressive with low-complexity wins on hot paths, reserve high-complexity wins (manual SIMD, unsafe) for measured cases.
2. Reuse before you build. Check [repo-helpers.md](references/repo-helpers.md) for an existing shared helper before reaching for a raw BCL primitive or hand-rolling one.
3. Match the component. Open the relevant [hot-paths](references/hot-paths) reference and follow the patterns already used there.
4. Fall back to BCL patterns. For the general fast-API guidance behind these patterns, see [bcl-patterns](references/bcl-patterns) and the multi-step [recipes.md](references/recipes.md). For language-level patterns (value types, enumeration, closures, lazy init, boxing), see [language.md](references/language.md).
5. Prove it. For anything non-trivial, measure before and after with [measuring.md](references/measuring.md).

## Reuse repo helpers, but prefer the BCL when it is equivalent

This repo keeps shared performance helpers in `src/Shared` (compiled as shared source). The rule, detailed in [repo-helpers.md](references/repo-helpers.md):

- Prefer the BCL abstraction by default.
- Use a shared helper only for the specific benefit it documents: pooling (`PooledArrayBufferWriter` over `ArrayBufferWriter<T>`), ref-struct stack storage (`ValueStringBuilder`, `ValueListBuilder`), a pooled `CancellationTokenSource` (`CancellationTokenSourcePool`), or a multi-TFM polyfill (the `ThrowHelpers`, `HashCode`, `ValueStopwatch`).
- The polyfill helpers already forward to the BCL API on modern target frameworks, so calling them is correct across every TFM a project targets.
- When both exist for the same job, the reason for the custom one must be explicit in code. If the BCL has since gained the same benefit, switch to the BCL type.

## The cheap wins (apply by default on hot paths)

Low complexity, high impact. Prefer them whenever you write or touch hot-path code, including refactors.

- Prefer the span or `Try*` overload over the allocating one: `ReadOnlySpan<char>` slicing instead of `Substring`, `TryFormat`/`TryParse` instead of `ToString`/`Parse`, span-based `Stream`/`Encoding` overloads.
- Pre-size and pool: set `capacity`, rent from `ArrayPool<T>.Shared` or the repo pools, write through `IBufferWriter<T>`/`PipeWriter`.
- Use the specialized type: cached `SearchValues<T>` (see `HttpCharacters`) for repeated set or substring search, `FrozenDictionary`/`FrozenSet` for build-once lookup, `CollectionsMarshal` for in-place access.
- Replace runtime reflection with a source generator: System.Text.Json `JsonSerializerContext`, `[GeneratedRegex]`, `[LoggerMessage]`.
- `stackalloc` a small, bounded, short-lived buffer (guard the size) instead of allocating, with `[SkipLocalsInit]` on the method to skip the zeroing since you write before reading.
- Let the JIT help you: `sealed` types (most internal types should be sealed), avoid closures and captures on hot paths, avoid boxing, prefer `readonly struct`.

## Be cautious with (measure first, isolate, keep trim-safe)

High complexity. Apply only on a proven hot path, behind a clean API, with a benchmark. Even when you recommend the simpler approach, report the faster high-complexity option and its tradeoff so the author can choose; see [decision-framework.md](references/decision-framework.md) for the complexity rubric.

- Manual SIMD and hardware intrinsics. Prefer ready-made vectorized helpers (`SearchValues`, `TensorPrimitives`, vectorized BCL APIs).
- `unsafe`, raw pointers, and other correctness-sensitive switches.
- Reflection-based code on AOT/trim paths (minimal APIs, `RequestDelegateFactory`): keep it source-generated and trim-safe.

## Hot-path references (primary)

How performance-sensitive code is actually written in this repo, by component. When you touch one, match its patterns.

| Component | Reference |
|-----------|-----------|
| HTTP primitives and headers | [hot-paths/http-primitives.md](references/hot-paths/http-primitives.md) |
| Kestrel and transport (HTTP/1, HTTP/2, HTTP/3) | [hot-paths/kestrel-transport.md](references/hot-paths/kestrel-transport.md) |
| Routing and endpoint matching | [hot-paths/routing.md](references/hot-paths/routing.md) |
| Components and Blazor rendering | [hot-paths/components.md](references/hot-paths/components.md) |
| SignalR and WebSockets | [hot-paths/signalr-websockets.md](references/hot-paths/signalr-websockets.md) |
| Data Protection and Identity | [hot-paths/dataprotection-identity.md](references/hot-paths/dataprotection-identity.md) |

## BCL pattern references (foundation)

The general fast-API guidance behind the patterns above, reconciled across .NET releases (newest wins). Use when the hot-path references do not cover your case.

| Area | Reference |
|------|-----------|
| Strings and spans | [bcl-patterns/strings-spans.md](references/bcl-patterns/strings-spans.md) |
| Collections | [bcl-patterns/collections.md](references/bcl-patterns/collections.md) |
| Searching | [bcl-patterns/searching.md](references/bcl-patterns/searching.md) |
| Serialization | [bcl-patterns/serialization.md](references/bcl-patterns/serialization.md) |
| Encoding | [bcl-patterns/encoding.md](references/bcl-patterns/encoding.md) |
| Numerics and primitives | [bcl-patterns/numerics.md](references/bcl-patterns/numerics.md) |
| Async and tasks | [bcl-patterns/async.md](references/bcl-patterns/async.md) |
| IO and buffers | [bcl-patterns/io.md](references/bcl-patterns/io.md) |
| Reflection and startup | [bcl-patterns/reflection.md](references/bcl-patterns/reflection.md) |
| JIT and codegen | [bcl-patterns/jit-codegen.md](references/bcl-patterns/jit-codegen.md) |
| Performance analyzers and AOT | [bcl-patterns/analyzers-aot.md](references/bcl-patterns/analyzers-aot.md) |
| Broad wins | [bcl-patterns/general.md](references/bcl-patterns/general.md) |

Multi-step allocation-free transforms: [recipes.md](references/recipes.md).

C# language-level patterns (value types and copying, enumeration, closures and delegates, lazy init, boxing and generics, fixed and stack buffers, codegen hints, allocation-avoiding data design): [language.md](references/language.md).

## Authoring and reviewing workflow

Authoring and review use one process that produces a traceable, line-level set of suggestions. The canonical record is a set of findings in your session database (`perf_findings`, with sources in `perf_finding_sources`); the markdown report and any PR comments are rendered from it. Full process and schema: [review.md](references/review.md).

1. Detect: as you write (authoring) or over the diff and named files (review), match each line against [signals.md](references/signals.md) and open the routed section. Insert a candidate finding with file, line range, rule anchor, recommendation, and a before/after.
2. Critique and verify: challenge each candidate against the actual code and the rule (real hot path? rule actually applies? does the fix compile and preserve behavior and any named invariant?). Qualify it as verified, rejected, or needs-info, with the reasoning. Never delete a finding; keep rejected ones with their reason.
3. Act:
   - Authoring: apply low-complexity hot-path fixes inline as you write (record them as applied); record medium and high ones as verified suggestions and report them, including high-complexity options with their tradeoff.
   - Review: do not modify code; record verified findings only.
4. Deliver: render the report from the curated (verified and applied) findings, then let the author choose the destination: write the markdown to disk, post one PR summary comment, or post inline PR review comments keyed to file and line. The findings stay in the session database for later retrieval regardless.

The highest-frequency misses to scan for: allocating overloads where a span overload exists, `Substring` instead of slicing, LINQ in tight loops, `IndexOfAny` with a literal set instead of a cached `SearchValues`, a raw `ArrayBufferWriter`/`StringBuilder` where a repo pooled helper fits, reflection or reflection-based serialization on an AOT path, missing `sealed`, and missing capacity hints. Keep suggestions high confidence, per the repo review bar.
