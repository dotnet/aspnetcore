# C# language-level performance patterns

Patterns controlled by how you write C#, not which BCL API you call: avoiding allocations and helping codegen through language features. Write for the latest C# (currently C# 15); each version-gated item notes the C# version it needs and, where a feature needs a newer language or target framework than a project uses, whether the repo has a polyfill to reuse or one could be added. See [decision-framework.md](decision-framework.md) for the complexity rubric and [repo-helpers.md](repo-helpers.md) for shared helpers. Some items here are cross-referenced from [bcl-patterns](bcl-patterns) and [hot-paths](hot-paths).

## Value types and copying

### readonly struct

Mark immutable structs as readonly so instance members do not trigger defensive copies from readonly storage.

- Do: readonly struct with readonly properties and fields
- Instead of: mutable struct stored in readonly locations
- Why: Readonly structs let the compiler and JIT avoid hidden copies when accessing members through readonly fields, in parameters, and readonly locals.
- Requires: C# 7.2
- Older targets: Native language feature on modern TFMs; older language versions require a normal struct and careful copying.
- Hot path: either | Complexity: low
- APIs: `readonly struct`
- Repo example: `Http\Http.Abstractions\src\PathString.cs`
- Snippet: [code](snippets/language.md#readonly-struct)

### scoped refs and spans

Add scoped to ref-like parameters when the method only consumes the span or ref and does not store it.

- Do: internal void PushPrefix(scoped ReadOnlySpan<char> key)
- Instead of: force callers to allocate a string just to satisfy conservative lifetime rules
- Why: The annotation widens safe call sites while still letting the compiler prevent lifetime escapes and heap captures.
- Requires: C# 11
- Older targets: Native language feature on modern TFMs; no equivalent polyfill, but older code can use narrower Span overloads or string overloads.
- Hot path: either | Complexity: low
- APIs: `scoped`, `ReadOnlySpan<T>`
- Repo example: `Components\Endpoints\src\FormMapping\FormDataReader.cs`

### small value struct

Use a struct for small, immutable, short-lived values that are passed by value and do not need identity.

- Do: public readonly struct QueryString-like value object
- Instead of: small reference type wrapper for every request value
- Why: The value can live inline in containing objects, arrays, locals, or stack frames instead of requiring a separate heap object and GC tracking.
- Hot path: either | Complexity: low
- APIs: `struct`, `readonly struct`
- Repo example: `Http\Http.Abstractions\src\QueryString.cs`

### stack-only ref struct

Use ref struct for types that contain Span<T> or other stack-only references and must never be boxed or captured.

- Do: ref struct BufferWriter<T> holding Span<byte>
- Instead of: class wrapper around Span<T> or boxing-capable struct
- Why: The compiler enforces stack-only lifetime rules, preventing accidental heap escape while keeping buffer access allocation-free.
- Requires: C# 7.2
- Older targets: Native language feature on modern TFMs; no real polyfill because the safety rule is compiler-enforced.
- Hot path: yes | Complexity: low
- APIs: `ref struct`, `Span<T>`
- Repo example: `Shared\ServerInfrastructure\BufferWriter.cs`

### ref access to large structs

Use in, ref readonly, ref returns, and ref locals when repeated indexing or calls would copy a non-trivial struct.

- Do: ref var frame = ref buffer[index]
- Instead of: var frame = buffer[index] followed by write-back or repeated indexer copies
- Why: Ref access edits or reads the storage in place and avoids repeated struct copies in tight loops or render-tree manipulation.
- Requires: C# 7.2; ref readonly parameters C# 12
- Older targets: Native language feature on modern TFMs; on older language versions keep the local index and minimize repeated copies.
- Hot path: yes | Complexity: medium
- APIs: `in`, `ref readonly`, `ref return`, `ref local`
- Repo example: `Components\Components\src\Rendering\RenderTreeBuilder.cs`
- Snippet: [code](snippets/language.md#ref-access-to-large-structs)

## Enumeration

### concrete enumerable type

Prefer concrete collection types in hot APIs when callers need to enumerate and the concrete type has a struct enumerator.

- Do: FormCollection or HeaderDictionary when API shape permits
- Instead of: IEnumerable<KeyValuePair<...>> on hot enumeration paths
- Why: Keeping the static type concrete lets foreach bind to the non-boxing pattern enumerator instead of the interface enumerator.
- Hot path: yes | Complexity: low
- APIs: `foreach pattern`, `concrete collection return type`
- Repo example: `Http\Http\src\FormCollection.cs`

### foreach over span

Foreach directly over Span<T> or ReadOnlySpan<T> when the data is already contiguous.

- Do: foreach (var item in CollectionsMarshal.AsSpan(list))
- Instead of: foreach through IEnumerable<T> after copying to an array
- Why: Span foreach is pattern-based and allocation-free, and it avoids interface dispatch and enumerator boxing.
- Requires: C# 7.3
- Older targets: Native language feature on modern TFMs; older code can use a for loop over span length.
- Hot path: either | Complexity: low
- APIs: `Span<T>`, `ReadOnlySpan<T>`, `foreach`, `CollectionsMarshal.AsSpan`
- Repo example: `Components\Forms\src\EditContextDataAnnotationsExtensions.cs`

### struct enumerator

Expose a public pattern-based GetEnumerator that returns a struct enumerator for concrete collection types.

- Do: public Enumerator GetEnumerator() returning a nested struct Enumerator
- Instead of: only IEnumerable<T>.GetEnumerator() returning IEnumerator<T>
- Why: Foreach over the concrete type can use the struct enumerator directly instead of boxing through IEnumerator<T>.
- Hot path: yes | Complexity: low
- APIs: `foreach pattern`, `struct Enumerator`, `IEnumerator<T>`
- Repo example: `Http\Http\src\HeaderDictionary.cs`
- Snippet: [code](snippets/language.md#struct-enumerator)

## Closures and delegates

### state-passing delegate

Use overloads that pass explicit state into the callback instead of capturing locals.

- Do: GetOrAdd(key, static (runtimeType, state) => Create(runtimeType, state), state)
- Instead of: GetOrAdd(key, runtimeType => Create(runtimeType, capturedState))
- Why: The callback stays static and reusable, avoiding closure allocation and making captured state obvious at the call site.
- Requires: C# 9 for the static lambda part
- Older targets: Native language feature on modern TFMs; if an API lacks a state overload, add one locally when the call is hot.
- Hot path: either | Complexity: low
- APIs: `ConcurrentDictionary.GetOrAdd`, `static lambda`, `state parameter`
- Repo example: `Components\Components\src\PersistentState\PersistentServicesRegistry.cs`

### static lambdas

Use static lambdas or static local functions in hot delegates to make accidental captures a compile-time error.

- Do: GetOrAdd(key, static type => Compute(type))
- Instead of: GetOrAdd(key, type => Compute(type, capturedLocal))
- Why: Preventing captures avoids closure allocations; the compiler already caches static method-group conversions and non-capturing lambdas in C# 11+, so manual static-readonly delegate caching is usually unnecessary.
- Requires: C# 9 for static lambdas; C# 8 for static local functions
- Older targets: Native language feature on modern TFMs; on older language versions use a named static method and avoid manual delegate fields unless measurement proves a need.
- Hot path: either | Complexity: low
- APIs: `static lambda`, `static local function`
- Repo example: `Components\Components\src\RouteView.cs`
- Snippet: [code](snippets/language.md#static-lambdas)

## Lazy and deferred initialization

### field keyword lazy property

Use the field keyword for simple lazy auto-properties when the initialization is single-threaded or benignly racy.

- Do: get => field ??= Create();
- Instead of: private field plus verbose property boilerplate for trivial lazy state
- Why: The property stays compact without a separate named backing field while avoiding a Lazy<T> allocation.
- Requires: C# 14
- Older targets: No repo polyfill for a keyword; on older language versions use an explicit backing field, and on modern TFMs use the keyword directly.
- Hot path: either | Complexity: low
- APIs: `field keyword`, `??=`

### module initializer

Use ModuleInitializer for one-time module setup that would otherwise require a per-call initialized check.

- Do: [ModuleInitializer] internal static void Initialize()
- Instead of: if (!initialized) Initialize() inside every hot method
- Why: The initialization runs once when the module loads, removing a branch from every hot call.
- Requires: C# 9
- Older targets: No production repo example found; on older TFMs a module initializer can sometimes be generated with IL weaving or replaced by an explicit type initializer.
- Hot path: yes | Complexity: low
- APIs: `ModuleInitializerAttribute`

### plain lazy field

Use a nullable backing field with ??= when thread-safety and exactly-once initialization are not required.

- Do: _field ??= new Value()
- Instead of: Lazy<T> for simple non-thread-safe per-instance state
- Why: A direct null check avoids the Lazy<T> object and delegate allocation while keeping the common path short.
- Requires: C# 8 for ??=
- Older targets: Native language feature on modern TFMs; older language versions can spell the same pattern as if (_field is null) _field = new Value().
- Hot path: either | Complexity: low
- APIs: `??=`, `nullable backing field`
- Repo example: `Components\Components\src\Dispatcher.cs`

## Strings and literals

### UTF-8 literals

Use UTF-8 string literals for fixed byte sequences.

- Do: private static ReadOnlySpan<byte> Prefix => "..."u8
- Instead of: Encoding.UTF8.GetBytes("...") on a hot path
- Why: The compiler emits bytes directly, avoiding runtime Encoding.UTF8.GetBytes work and intermediate strings for protocol constants.
- Requires: C# 11
- Older targets: Native language feature on modern TFMs; older language versions can use cached byte arrays in shared source.
- Hot path: yes | Complexity: low
- APIs: `UTF-8 string literal`, `ReadOnlySpan<byte>`
- Repo example: `Http\Headers\src\ContentDispositionHeaderValue.cs`

### compile-time names and strings

Use const or compiler-folded strings and nameof for member names instead of runtime concatenation or reflection-derived names.

- Do: $"Displaying {nameof(NotFound)} ..." in an attribute or const context
- Instead of: typeof(T).Name or string concatenation at runtime for stable member names
- Why: The compiler embeds the result and keeps names refactor-safe without per-call allocation or reflection.
- Requires: C# 10 for constant interpolated strings; nameof is C# 6
- Older targets: Native language feature on modern TFMs; older language versions can use const string concatenation and nameof where available.
- Hot path: either | Complexity: low
- APIs: `const`, `nameof`, `constant interpolated string`
- Repo example: `Components\Components\src\Routing\Router.cs`

### interpolated string handlers

Use interpolated string handlers when the callee can decide whether and how to append formatted parts.

- Do: custom [InterpolatedStringHandler] or DefaultInterpolatedStringHandler with conditional AppendFormatted calls
- Instead of: build the interpolated string before checking whether it is needed
- Why: Handlers can skip formatting and allocation when disabled, especially for logging or conditional diagnostics.
- Requires: C# 10
- Older targets: No production repo example found; where older targets need the shape, add a small shared handler polyfill or keep LoggerMessage source generation for logging.
- Hot path: yes | Complexity: medium
- APIs: `DefaultInterpolatedStringHandler`, `InterpolatedStringHandlerAttribute`

## Boxing and generics

### constrained value generics

Constrain value-type generics so parsing, equality, and switches stay strongly typed instead of flowing through object.

- Do: where T : struct, Enum with Enum.TryParse<T>
- Instead of: object parameter plus pattern matching or switch over boxed values
- Why: The JIT can generate value-type-specific code and avoid boxing that appears when value types are cast to object or unconstrained interfaces.
- Requires: C# 7.3 for Enum constraints
- Older targets: Native language feature on modern TFMs; older language versions can use runtime type checks but cannot express the same constraint.
- Hot path: either | Complexity: low
- APIs: `where T : struct`, `where T : Enum`, `generic constraints`
- Repo example: `Components\Components\src\BindConverter.cs`

### enum no-boxing

Keep enum operations generic or bitwise instead of going through object-based Enum APIs.

- Do: Enum.GetValues<TEnum>(), Enum.TryParse<TEnum>(), or (value & flag) != 0
- Instead of: Enum.GetValues(typeof(TEnum)) or value.HasFlag(flag) in hot loops
- Why: Generic enum APIs and bitwise tests avoid boxing each enum value for reflection-style Enum calls or HasFlag-style object paths.
- Requires: C# 7.3 for Enum constraints
- Older targets: Native generic enum APIs are available on modern TFMs; older shared code can add cached typed arrays or helper methods.
- Hot path: either | Complexity: low
- APIs: `Enum.GetValues<TEnum>`, `Enum.TryParse<TEnum>`, `bitwise enum flags`
- Repo example: `Components\Components\src\BindConverter.cs`

### static abstract members

Use static abstract interface members when generic code must call type-specific static operations.

- Do: interface IAdapter<T> { static abstract T Create(); }
- Instead of: Activator.CreateInstance or boxed strategy object for every operation
- Why: The generic call is statically resolved for each T, avoiding reflection, virtual dispatch, and boxing through helper objects.
- Requires: C# 11
- Older targets: Native language feature on modern TFMs; for older targets use source generation or cached delegates as a compatibility option.
- Hot path: either | Complexity: low
- APIs: `static abstract interface members`, `generic math shape`
- Repo example: `Components\Endpoints\src\FormMapping\Converters\CollectionAdapters\ICollectionBufferAdapter.cs`

## Fixed and stack buffers

### params spans

Use params ReadOnlySpan<T> with collection expressions for small call-site lists that should not allocate params arrays.

- Do: void Add(params ReadOnlySpan<T> values) and call Add([a, b, c])
- Instead of: void Add(params T[] values) on hot paths
- Why: The compiler can pass inline data as a span, avoiding a new T[] for each call.
- Requires: C# 13
- Older targets: No production repo example found; add overloads as an option for hot shared APIs, and use native support on modern TFMs.
- Hot path: yes | Complexity: low
- APIs: `params ReadOnlySpan<T>`, `collection expressions`

### stackalloc skip init

Use stackalloc for small bounded buffers and SkipLocalsInit when every element is written before it is read.

- Do: [SkipLocalsInit] method with Span<T> buffer = stackalloc T[count]
- Instead of: new T[count] for a small per-call temporary buffer
- Why: The buffer avoids heap allocation and SkipLocalsInit removes zeroing cost when the local write-before-read invariant is self-evident.
- Requires: C# 9 for SkipLocalsInit; stackalloc spans from C# 7.2
- Older targets: SkipLocalsInitAttribute exists in modern TFMs; older shared code can define the attribute if needed, otherwise omit it and keep stackalloc.
- Hot path: yes | Complexity: low
- APIs: `stackalloc`, `SkipLocalsInitAttribute`, `Span<T>`
- Repo example: `Http\Routing\src\Matching\DfaMatcher.cs`
- Snippet: [code](snippets/language.md#stackalloc-skip-init)

### inline arrays

Use InlineArray for small fixed-size buffers inside a struct when the size is known and the buffer should be inline.

- Do: [InlineArray(N)] private struct Buffer { private T _element0; }
- Instead of: private T[] _buffer for every small fixed-size value
- Why: The data is stored inline with the containing value, avoiding a separate array allocation and improving locality.
- Requires: C# 12
- Older targets: No production repo example found; where older TFMs need this, add a shared fixed-buffer or explicit-field type, or use native InlineArray on modern TFMs.
- Hot path: yes | Complexity: medium
- APIs: `InlineArrayAttribute`

## Codegen hints

### BCL throw helpers

Prefer BCL static throw helpers such as ArgumentNullException.ThrowIfNull and ObjectDisposedException.ThrowIf in code that targets supporting TFMs.

- Do: ArgumentNullException.ThrowIfNull(arg)
- Instead of: manual [DoesNotReturn] helper or Microsoft.AspNetCore.Shared.ThrowHelpers when the BCL API is available
- Why: They keep hot methods small and inlineable while using the modern API; repo ThrowHelpers are only for shared source targeting TFMs without the BCL API.
- Requires: C# 10 for CallerArgumentExpression used by many helpers
- Older targets: Repo polyfills live under Shared\ThrowHelpers, such as Shared\ThrowHelpers\ArgumentNullThrowHelper.cs, for TFMs without the BCL helpers.
- Hot path: either | Complexity: low
- APIs: `ArgumentNullException.ThrowIfNull`, `ObjectDisposedException.ThrowIf`, `ArgumentOutOfRangeException.ThrowIfNegative`
- Repo example: `Components\Media\src\MediaSource.cs`
- Snippet: [code](snippets/language.md#bcl-throw-helpers)

### sealed for codegen

Seal classes or overrides that are not intended for inheritance, especially on hot dispatch paths.

- Do: sealed class or sealed override
- Instead of: virtual/open type by default when no extension point is intended
- Why: Sealing gives the JIT stronger devirtualization and inlining opportunities and documents the type shape.
- Hot path: either | Complexity: low
- APIs: `sealed class`, `sealed override`
- Repo example: `Http\Http.Abstractions\src\Routing\EndpointMetadataCollection.cs`
- Snippet: [code](snippets/language.md#sealed-for-codegen)

## Allocation-avoiding data design

### cached boxed values

Cache boxed values for booleans, small enums, or small integers when an object-typed API forces boxing repeatedly.

- Do: private static readonly object BoxedTrue = true
- Instead of: new KeyValuePair<string, object?>(name, boolValue) on every hot call
- Why: The unavoidable object API still gets stable boxed instances instead of allocating a new box for each event or measurement.
- Hot path: yes | Complexity: low
- APIs: `boxing`, `static readonly object`
- Repo example: `Http\Routing\src\RoutingMetrics.cs`
- Snippet: [code](snippets/language.md#cached-boxed-values)

### reinterpret spans

Reinterpret contiguous data without copying when the element-size and layout invariant is local and obvious.

- Do: MemoryMarshal.AsBytes(chars.AsSpan()) or MemoryMarshal.Cast<TFrom,TTo>(span)
- Instead of: allocate and copy a converted array when the representation can be safely reinterpreted
- Why: MemoryMarshal and Unsafe casts can hand existing bytes to a consumer without allocating or copying a second buffer.
- Hot path: yes | Complexity: low
- APIs: `MemoryMarshal.Cast`, `MemoryMarshal.AsBytes`, `MemoryMarshal.Read`, `Unsafe.As`, `MemoryMarshal.CreateSpan`, `MemoryMarshal.GetArrayDataReference`
- Repo example: `Components\Server\src\Circuits\CircuitId.cs`
- Snippet: [code](snippets/language.md#reinterpret-spans)

### small object optimization

Represent the common one-item case without allocating the multi-item container, and promote only when a second or later item arrives.

- Do: StringValues-style single value, then string[], then expanding dictionary or list
- Instead of: allocate List<T> or Dictionary<TKey,List<T>> for every key immediately
- Why: Most request collections are small, so single-or-array or array-then-dictionary designs avoid allocating List<T> or Dictionary<TKey,TValue> until needed.
- Hot path: yes | Complexity: medium
- APIs: `readonly struct`, `StringValues`, `lazy promotion`
- Repo example: `Http\WebUtilities\src\KeyValueAccumulator.cs`
- Snippet: [code](snippets/language.md#small-object-optimization)
