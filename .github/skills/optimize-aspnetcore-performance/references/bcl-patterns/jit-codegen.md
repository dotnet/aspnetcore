# JIT and codegen performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Avoid boxing in generic hot paths

Constrain and call generics so value types stay unboxed through comparisons, formatting, and interface-like operations.

- Do: Use generic constraints such as where T : IEquatable<T> and call strongly typed members.
- Instead of: Cast value types to object or non-generic interfaces for repeated operations.
- Why: Boxing allocates and hides exact value-type information from the JIT, reducing opportunities for inlining and promotion.
- Since .NET 8
- Hot path: yes | Complexity: low
- APIs: `System.IEquatable<T>`

## Avoid generic casts when the type can stay generic or exact

Keep values strongly typed through generic code instead of casting object back to T in hot paths.

- Do: Carry T values in generic fields, parameters, or collections rather than object, and cast only at boundaries.
- Instead of: Store values as object and repeatedly perform (T)o inside tight loops.
- Why: Generic casts are faster in .NET 8 but still require type checks; avoiding object round-trips removes the check and any boxing risk.
- Since .NET 8. Supersedes: The .NET 8 JIT inlines a fast success path for generic casts that previously called CastHelpers.ChkCastAny.
- Hot path: yes | Complexity: low

## Avoid redundant casts in hot arithmetic

Keep numeric expressions simple and avoid unnecessary intermediate casts that do not change semantics.

- Do: Write short Add(short x, short y) => (short)(x + y) and avoid extra nested casts around the same value.
- Instead of: Layer multiple redundant casts or conversions around arithmetic in tight loops.
- Why: Simple cast patterns are easier for the JIT to remove or combine into fewer instructions.
- Since .NET 8. Supersedes: The .NET 8 JIT added peepholes to remove some unnecessary casts previously emitted in .NET 7.
- Hot path: yes | Complexity: low

## Avoid redundant type checks with sealed array element types

Use sealed element types in arrays and type tests when the domain is closed.

- Do: Use string[], sealed-type arrays, or arrays of closed implementations where mutation through refs is needed.
- Instead of: Use base-class arrays for mutable ref access when every element is actually a closed derived type.
- Why: For sealed T, .NET 8 can turn is T[] and ldelema cases into simpler exact-type or bounds-checked code without helper calls.
- Since .NET 8. Supersedes: The .NET 7 JIT used helper calls such as CORINFO_HELP_ISINSTANCEOFARRAY or CORINFO_HELP_LDELEMA_REF in more cases.
- Hot path: yes | Complexity: low
- APIs: `System.Threading.Volatile.Read`

## Cache Length in simple span and array loops

Write loops with a simple local length and monotonic index over the same span or array.

- Do: Use for (int i = 0, length = span.Length; i < length; i++) { Use(span[i]); }.
- Instead of: Mutate the collection, recompute through aliases, or index a different span than the one whose length was checked.
- Why: Straight-line loop shapes make it easier for the JIT to hoist or remove repeated bounds checks.
- Since .NET 8
- Hot path: yes | Complexity: low
- APIs: `System.Span<T>.Length`, `System.ReadOnlySpan<T>.Length`

## Pair SkipLocalsInit with stackalloc to skip buffer zeroing

A stackalloc buffer is zero-initialized by default; when filled before reading, that zeroing is pure overhead that [SkipLocalsInit] removes.

- Do: Apply [SkipLocalsInit] at method scope on hot-path methods that stackalloc a scratch buffer they write before reading, as Kestrel, routing (DfaMatcher), header parsing, and WebEncoders do in this repo.
- Instead of: Paying automatic .locals init zeroing on every call, or reading/exposing the untouched tail of the buffer.
- Why: Cheap method-local win that removes wasted zeroing; the only caveat is to not read slots you did not write (slice to the written length).
- Since .NET 5
- Hot path: yes | Complexity: low
- APIs: `System.Runtime.CompilerServices.SkipLocalsInitAttribute`, `System.Span<T>`

## Prefer simple ternaries for conditional selection

Write simple conditional selection as a ternary expression and let the JIT choose branches or conditional moves.

- Do: Use condition ? whenTrue : whenFalse for simple scalar selection such as option flags or min/max-like code.
- Instead of: Use complex arithmetic branchless formulas unless measurement shows they beat the straightforward ternary.
- Why: The .NET 8 JIT can emit cmov or csel for suitable patterns, reducing branch misprediction costs without hand-coded tricks.
- Since .NET 8. Supersedes: Older manual branchless patterns such as multiplying by condition ? 1 : 0 are often worse than .NET 8 conditional moves.
- Hot path: yes | Complexity: low

## Use AggressiveInlining only for tiny hot helpers

Apply MethodImplOptions.AggressiveInlining sparingly to tiny helpers where inlining exposes constants, bounds proofs, or branch elimination at the caller.

- Do: Annotate very small, frequently called helpers such as guarded Slice wrappers after measuring.
- Instead of: Blanket-annotate large methods, rarely used methods, or methods that are already naturally inlineable.
- Why: Inlining can unlock constant folding and redundant check removal, but overuse can bloat code and hurt instruction-cache behavior.
- Since .NET 8
- Hot path: yes | Complexity: low
- APIs: `System.Runtime.CompilerServices.MethodImplAttribute`, `System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining`

## Use modulo-by-length for hash bucket indexing

Map hash codes to bucket arrays with an unsigned modulo by the bucket array length.

- Do: Index buckets[(uint)hashCode % buckets.Length] when buckets is the same array whose Length is used.
- Instead of: Compute a bucket index through unrelated helpers that obscure the relationship to the target array length.
- Why: In .NET 8 the JIT can prove (uint)hash % buckets.Length is in range and remove the following bounds check.
- Since .NET 8. Supersedes: The .NET 7 JIT emitted an additional range check after the modulo operation.
- Hot path: yes | Complexity: low

## Use structs for small hot-path data carried by value

Use small, simple structs for data that is frequently created or enumerated, especially when fields can be promoted and kept in registers.

- Do: Model compact per-item state as a struct and access only the needed fields in hot code.
- Instead of: Allocate short-lived classes for tiny per-item state or copy large structs repeatedly.
- Why: Struct promotion in .NET 8 can avoid heap allocation and skip copying unused fields, improving throughput and code size.
- Since .NET 8. Supersedes: The .NET 7 JIT had stricter promotion limits, such as four-field and nested-struct limitations.
- Hot path: yes | Complexity: low

## Use unsigned range checks for index validation

Use a single unsigned comparison when validating that an int index is non-negative and below Length.

- Do: Use if ((uint)index < (uint)span.Length) before span[index] or span.Slice(index).
- Instead of: Use separate index >= 0 and index < Length checks when the hot path can use the recognized unsigned idiom.
- Why: The pattern is compact and the JIT recognizes it as proving subsequent span or array accesses are safe.
- Since .NET 8. Supersedes: Older redundant branch cases around guarded Slice calls in .NET 7.
- Hot path: yes | Complexity: low
- APIs: `System.ReadOnlySpan<T>.Slice`, `System.Span<T>.Slice`

## Write bounds-check-friendly length guards

Guard string, array, and span accesses with length checks that directly prove every subsequent index is in range.

- Do: Check s.Length >= 2 before reading s[0] and s[^1], or span.Length before Slice and indexing.
- Instead of: Index first and rely on exceptions, or hide length relationships behind opaque helper calls.
- Why: The JIT can remove bounds checks when it sees patterns such as Length >= n before indexing 0 or Length - 1.
- Since .NET 8. Supersedes: The .NET 7 JIT often kept a bounds check for s[s.Length - 1] even after a length guard.
- Hot path: yes | Complexity: low
- APIs: `System.String.Length`, `System.ReadOnlySpan<T>.Length`, `System.Span<T>.Length`

## Avoid copying large structs just to read a few fields

Read fields directly or pass large structs by readonly reference when only a subset of fields is needed.

- Do: Use in parameters, ref readonly returns, or direct field reads for large structs on hot paths.
- Instead of: Assign a large struct to a local and then use only one or two fields.
- Why: Avoiding full struct copies lets the JIT load only the used fields and can eliminate rep movs or stack temporaries.
- Since .NET 8. Supersedes: Older JITs were more likely to materialize full copies of large structs.
- Hot path: yes | Complexity: medium

## Feed constants through small inlineable APIs

Design hot helper APIs so constant arguments, string literals, and span literals reach the implementation after inlining.

- Do: Keep format parsing helpers small and inlineable, and pass literal formats such as "B" or "http://"u8 directly when known.
- Instead of: Route constants through opaque virtual calls or large non-inlineable methods before branching on them.
- Why: The JIT can fold length checks, indexing, switches, and even constructor work when constants are visible at the call site.
- Since .NET 8. Supersedes: The .NET 7 JIT folded fewer string and span contents through inlined calls.
- Hot path: yes | Complexity: medium
- APIs: `System.ReadOnlySpan<T>`, `System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining`

## Shape struct fields for the hot access pattern

Keep hot structs compact and arrange fields to minimize padding and make commonly accessed fields cheap to load together.

- Do: Group fields by size and access pattern, keep cold fields out of hot structs, and validate size with Unsafe.SizeOf<T>() when needed.
- Instead of: Put many rarely used fields into one large mutable struct that is copied through hot code.
- Why: Smaller, better laid-out structs reduce copy cost, cache pressure, and the amount of data the JIT must promote or load.
- Since .NET 8
- Hot path: yes | Complexity: medium
- APIs: `System.Runtime.CompilerServices.Unsafe.SizeOf<T>`

## Use branchless comparisons only for unpredictable hot branches

Consider branchless compare or boolean-to-0-or-1 patterns only when branch outcomes are highly unpredictable and measured as costly.

- Do: Measure patterns like int gt = x > y ? 1 : 0; int lt = x < y ? 1 : 0; return gt - lt; before keeping them.
- Instead of: Rewrite ordinary clear if/else code into branchless arithmetic by default.
- Why: Branchless code can stabilize throughput, but it can also produce more instructions and prevent better inlined branch fusion.
- Since .NET 8. Supersedes: Manual branchless selection patterns are less often needed now that .NET 8 emits conditional moves for simple ternaries.
- Hot path: yes | Complexity: medium

## Use ref struct only for stack-only byref-like data

Use ref struct for span-like stack-only abstractions that must carry byrefs without allocation or boxing.

- Do: Use ref struct for small parser/enumerator/buffer views that wrap Span<T> or ReadOnlySpan<T>.
- Instead of: Use classes or boxed interfaces for tiny stack-only views in tight loops.
- Why: Byref-like structs keep data on the stack and can avoid heap allocation, interface boxing, and extra indirections.
- Since .NET 8
- Hot path: yes | Complexity: medium
- APIs: `System.Span<T>`, `System.ReadOnlySpan<T>`

## Let stackalloc zeroing be optimized when zeroed buffers are required

If a stackalloc buffer must start zeroed, rely on normal initialization and keep sizes straightforward rather than hand-writing clearing loops.

- Do: Use Span<byte> buffer = stackalloc byte[size] when zero initialization is semantically required.
- Instead of: Manually loop to clear a fresh stackalloc buffer unless measurement proves a need.
- Why: The .NET 8 JIT emits vectorized zeroing or optimized memset for stackalloc initialization.
- Since .NET 8. Supersedes: The .NET 7 JIT often emitted a push-based zeroing loop for stackalloc initialization.
- Hot path: either | Complexity: low
- APIs: `System.Span<T>`

## Prefer static helpers when instance state is unnecessary

Make helper methods static when they do not use instance state so the call has no receiver and cannot require virtual dispatch.

- Do: Use static helper methods for pure computations and pass only the data needed.
- Instead of: Put pure helpers on extensible instance types where calls may be virtual or carry an unused this pointer.
- Why: Static calls are easier for the JIT to inline, propagate constants through, and eliminate unused work around.
- Since .NET 8
- Hot path: either | Complexity: low

## Seal hot-path classes to unlock exact-type codegen

Mark classes sealed when you do not intend inheritance so the JIT can reason about the exact implementation and avoid some virtual dispatch and type-test helpers.

- Do: Use sealed classes or sealed overrides for closed implementations on hot paths.
- Instead of: Leave every class extensible by default when no derived type is expected.
- Why: Exact types give the JIT more opportunities for devirtualization, inlining, and simpler casts or array checks.
- Since .NET 8. Supersedes: Older runtimes had fewer exact-type optimizations for sealed array/type tests in .NET 7 and earlier.
- Hot path: either | Complexity: low

## Use UTF-8 literals for fixed byte data

Use C# UTF-8 string literals for fixed byte sequences so the compiler emits readonly static data the JIT can inspect.

- Do: Use private static ReadOnlySpan<byte> Prefix => "http://"u8 for fixed protocol tokens.
- Instead of: Allocate byte arrays or encode strings at runtime for fixed literals.
- Why: Indexing into compiler-emitted RVA static data can be constant folded when the index is known.
- Since .NET 8. Supersedes: Older patterns used Encoding.UTF8.GetBytes or manually initialized arrays for fixed UTF-8 data.
- Hot path: either | Complexity: low
- APIs: `System.ReadOnlySpan<T>`

## Use readonly struct for immutable value types

Declare immutable value types as readonly struct so instance members do not require defensive copies when accessed through readonly locations.

- Do: Use readonly struct with readonly fields and non-mutating members for immutable value objects.
- Instead of: Use mutable structs for immutable data that is commonly stored in readonly fields or passed by in.
- Why: Avoiding defensive copies reduces hidden memory traffic and exposes fields more clearly to struct promotion.
- Since .NET 8
- Hot path: either | Complexity: low

## Use static readonly immutable data for foldable lengths and fields

Store immutable strings, arrays, and value-type configuration in static readonly fields when their values are process-wide constants.

- Do: Use private static readonly string or static readonly value-type fields for fixed configuration read in hot code.
- Instead of: Recompute fixed values per call or expose mutable static data that prevents safe folding.
- Why: The JIT can fold lengths and primitive fields from static readonly data and remove dead branches that depend on them.
- Since .NET 8. Supersedes: Earlier JITs were less able to fold string or array lengths and value-type fields stored in static readonly fields.
- Hot path: either | Complexity: low
