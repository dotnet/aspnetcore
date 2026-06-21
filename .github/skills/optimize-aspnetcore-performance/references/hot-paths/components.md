# Components and Blazor (ASP.NET Core source patterns)

Source-proven performance patterns from this repository, by hot-path component. These are the primary worked examples: when you touch this component, match these patterns. Before adding raw BCL primitives, check [../repo-helpers.md](../repo-helpers.md) for an existing shared helper. Paths are relative to `src`.

## ISpanFormattable into stackalloc digits

ReverseStringBuilder formats span-formattable values into an 11-character stack buffer before inserting the written slice.

- Do: Prefer ISpanFormattable.TryFormat into a bounded stackalloc Span<char>, falling back to IFormattable only on failure.
- Why: Common numeric indexer values format without allocating an intermediate string.
- Source: `Components\Shared\src\ExpressionFormatting\ReverseStringBuilder.cs#L91-L108` (expression formatting)
- Hot path: yes | Complexity: low
- APIs: `System.ISpanFormattable`, `stackalloc`, `System.Globalization.CultureInfo.InvariantCulture`

## ReverseStringBuilder with stack buffer fallback

ExpressionFormatter builds field paths from right to left using ReverseStringBuilder over a stackalloc char buffer before renting arrays.

- Do: Use ReverseStringBuilder(stackalloc char[ExpressionFormatter.StackAllocBufferSize]) and dispose it after ToString.
- Why: Most expression names fit in the stack buffer and avoid heap allocation while preserving a pooled fallback for long paths.
- Source: `Components\Shared\src\ExpressionFormatting\ExpressionFormatter.cs#L23-L44` (expression formatting)
- Hot path: yes | Complexity: low
- APIs: `stackalloc`, `System.Span<T>`, `System.Buffers.ArrayPool<T>`

## clear only live pooled entries

ArrayBuilder<T>.ReturnBuffer clears only _itemsInUse before returning the array to the pool instead of clearing full capacity.

- Do: Before ArrayPool<T>.Return, Array.Clear only the populated slice when all remaining slots are already default.
- Why: Render buffers can be much larger than their live count, so clearing only used entries reduces GC-reference cleanup work.
- Source: `Components\Shared\src\ArrayBuilder.cs#L192-L200` (render tree buffers)
- Hot path: yes | Complexity: low
- APIs: `System.Array.Clear`, `System.Buffers.ArrayPool<T>.Return`

## form key buffer rented per mapping operation

HttpContextFormValueMapper rents a char buffer sized to MaxKeyBufferSize and passes it as memory to FormDataReader.

- Do: Rent ArrayPool<char> scratch buffers around form parsing and return them in finally.
- Why: Form mapping can reuse a bounded scratch key buffer without allocating a new char array for every request.
- Source: `Components\Endpoints\src\FormMapping\HttpContextFormValueMapper.cs#L122-L155` (form mapping)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.ArrayPool<T>`, `System.Memory<T>`

## route URL decode stackalloc with pooled fallback

RouteContext decodes percent-encoded paths into a stackalloc byte buffer for short paths and rents an ArrayPool<byte> buffer for longer paths.

- Do: Guard stackalloc by length and wrap rented fallback storage in a disposable ref struct.
- Why: Routing avoids allocation for common short paths while still handling long paths without large stack allocations.
- Source: `Components\Components\src\Routing\RouteContext.cs#L16-L39` (routing)
- Hot path: yes | Complexity: low
- APIs: `System.Runtime.CompilerServices.SkipLocalsInitAttribute`, `stackalloc`, `System.Buffers.ArrayPool<T>`, `System.Text.Encoding.UTF8`

## ReverseStringBuilder pooled segment chain

ReverseStringBuilder starts with caller-provided Span<char> and rents one or more char arrays only when inserting at the front overflows the current buffer.

- Do: Copy into the current span first, then rent larger ArrayPool<char> buffers and return them from Dispose.
- Why: It avoids repeated string concatenation and keeps rare large expressions allocation-friendly with reusable ArrayPool storage.
- Source: `Components\Shared\src\ExpressionFormatting\ReverseStringBuilder.cs#L13-L89` (expression formatting)
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.ArrayPool<T>`, `System.Buffers.ReadOnlySequence<T>`, `System.Span<T>`

## cached compiled captured-value formatters

ExpressionFormatter caches per-member delegates that read captured values and write them directly to ReverseStringBuilder.

- Do: Use ConcurrentDictionary<MemberInfo, delegate> and compile typed member evaluators with Expression.Lambda once.
- Why: Reflection and expression compilation happen once per captured member instead of on every formatted lambda.
- Source: `Components\Shared\src\ExpressionFormatting\ExpressionFormatter.cs#L237-L285` (expression formatting)
- Hot path: yes | Complexity: medium
- APIs: `System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>`, `System.Linq.Expressions.Expression.Compile`, `System.Reflection.MemberInfo`

## cheap string hash for mostly unique keys

SimplifiedStringHashComparer hashes attribute names using length plus middle and last characters, while Equals still performs ordinal-ignore-case comparison.

- Do: For specialized dictionaries with mostly misses, consider a deliberately cheap hash paired with correct Equals semantics.
- Why: The duplicate-attribute dictionary mostly sees non-matching keys, so a cheaper hash wins despite possible collisions.
- Source: `Components\Components\src\Rendering\SimplifiedStringHashComparer.cs#L6-L43` (render tree building)
- Hot path: yes | Complexity: medium
- APIs: `System.Collections.Generic.IEqualityComparer<T>`, `string.Equals`

## collection converter pooled growable buffer

ArrayPoolBufferAdapter stores mapped collection elements in a pooled array that doubles when full and converts to the target collection before returning the array.

- Do: Use an adapter with CreateBuffer, Add by ref, and ToResult that owns returning the pooled backing array.
- Why: Form collection binding avoids repeated allocations while preserving amortized O(1) appends.
- Source: `Components\Endpoints\src\FormMapping\Converters\CollectionAdapters\ArrayPoolBufferAdapter.cs#L8-L39` (form mapping)
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.ArrayPool<T>`, `System.Array.Copy`

## duplicate-attribute cleanup with simplified hash comparer

RenderTreeBuilder tracks duplicate attribute names in a lazily allocated dictionary using SimplifiedStringHashComparer and compacts overwritten frames in place.

- Do: Use a lazy scratch dictionary plus a cheap hash comparer for mostly-miss duplicate detection in render output.
- Why: Duplicate attributes are uncommon, and the comparer avoids hashing every character for mostly unique names.
- Source: `Components\Components\src\Rendering\RenderTreeBuilder.cs#L821-L915` (render tree building)
- Hot path: yes | Complexity: medium
- APIs: `System.Collections.Generic.Dictionary<TKey,TValue>`, `IEqualityComparer<string>`

## pooled ArrayBuilder for render buffers

ArrayBuilder<T> rents backing arrays, doubles capacity, and returns buffers on Clear or Dispose.

- Do: Use ArrayBuilder<T> for render-time append buffers and dispose or clear between batches.
- Why: Long-lived components can re-render with varying tree sizes without retaining oversized arrays or allocating List<T> buffers repeatedly.
- Source: `Components\Shared\src\ArrayBuilder.cs#L22-L80` (render tree buffers)
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.ArrayPool<T>`, `System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining`

## pooled dictionary for keyed diffing

RenderTreeDiffBuilder obtains the keyed-item lookup dictionary from RenderBatchBuilder's StackObjectPool and returns it after clearing.

- Do: Use StackObjectPool<Dictionary<object, KeyedItemInfo>> around rare but expensive keyed-diff lookup tables.
- Why: Keyed diffing is conditional, and pooling avoids allocating dictionaries every render batch that contains keyed siblings.
- Source: `Components\Components\src\RenderTree\RenderTreeDiffBuilder.cs#L291-L321` (render tree diffing)
- Hot path: yes | Complexity: medium
- APIs: `System.Collections.Generic.Dictionary<TKey,TValue>`, `Microsoft.AspNetCore.Components.Rendering.StackObjectPool<T>`

## ref DiffContext to avoid parameter lists and copies

RenderTreeDiffBuilder stores render-diff state in a private mutable struct and always passes it by ref.

- Do: Use private scoped context structs passed by ref for tightly controlled hot algorithms with many shared fields.
- Why: The hot diff helpers avoid long parameter lists while preventing repeated struct copies of mutable sibling state.
- Source: `Components\Components\src\RenderTree\RenderTreeDiffBuilder.cs#L1076-L1113` (render tree diffing)
- Hot path: yes | Complexity: medium
- APIs: `ref`, `System.ValueType`

## ref struct buffer owner for pooled fallback

RouteContext.UriBuffer is a readonly ref struct that exposes Span<byte> and returns the rented byte array from Dispose.

- Do: Use readonly ref struct owners when a Span<T> may point to either stack memory or pooled heap memory.
- Why: The stack-only owner prevents the buffer from escaping and keeps cleanup scoped to the decode operation.
- Source: `Components\Components\src\Routing\RouteContext.cs#L53-L74` (routing)
- Hot path: yes | Complexity: medium
- APIs: `ref struct`, `System.Span<T>`, `System.Buffers.ArrayPool<T>`

## reused attribute diff dictionary

The slow attribute diff path uses RenderBatchBuilder.AttributeDiffSet and clears it after processing additions.

- Do: Keep reusable scratch dictionaries on the batch builder and Clear them after each slow-path operation.
- Why: The dictionary allocation is amortized across batches while the fast path avoids touching it in common cases.
- Source: `Components\Components\src\RenderTree\RenderTreeDiffBuilder.cs#L494-L537` (render tree diffing)
- Hot path: yes | Complexity: medium
- APIs: `System.Collections.Generic.Dictionary<TKey,TValue>`

## sorted pooled prefix lookup

PrefixResolver rents a FormKey array, sorts populated entries, and checks prefixes with Array.BinarySearch.

- Do: Rent an array for transient keys, sort the populated range, and binary-search with a custom comparer before returning the array.
- Why: Repeated form-prefix probes become logarithmic without allocating a sorted collection object per mapper.
- Source: `Components\Endpoints\src\FormMapping\PrefixResolver.cs#L15-L42` (form mapping)
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.ArrayPool<T>`, `System.Array.Sort`, `System.Array.BinarySearch`, `System.ReadOnlyMemory<T>`

## specialized frame append methods

RenderTreeFrameArrayBuilder inlines the capacity check and writes frame fields directly for each frame kind.

- Do: Use specialized append methods such as AppendElement, AppendText, and AppendAttribute when a hot builder writes fixed-shaped structs.
- Why: Avoiding generic helper layers in intensive rendering scenarios is documented as improving FastGrid rendering by about 1 percent.
- Source: `Components\Components\src\RenderTree\RenderTreeFrameArrayBuilder.cs#L10-L30` (render tree building)
- Hot path: yes | Complexity: medium
- APIs: `Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame`, `ArrayBuilder<T>`

## streaming render stack sort buffer

EndpointHtmlRenderer allocates a ComponentIdAndDepth span on the stack for small updated-component batches and sorts it by depth.

- Do: Use a byte-size guard with stackalloc plus MemoryMarshal.Cast for small struct buffers, then MemoryExtensions.Sort.
- Why: Small streaming batches avoid heap arrays while the sorted list prevents duplicated descendant HTML transmission.
- Source: `Components\Endpoints\src\Rendering\EndpointHtmlRenderer.Streaming.cs#L129-L141` (endpoint streaming rendering)
- Hot path: yes | Complexity: medium
- APIs: `stackalloc`, `System.Runtime.InteropServices.MemoryMarshal.Cast`, `System.MemoryExtensions.Sort`

## fast attribute merge join before dictionary fallback

Attribute diffing first walks old and new attributes by sequence and name, using the dictionary slow path only when ordering prevents a simple merge.

- Do: Implement ordered merge-join fast paths with a clearly isolated dictionary fallback for uncommon unordered input.
- Why: Most attributes are emitted in stable order, so the common case avoids hash table setup and lookups.
- Source: `Components\Components\src\RenderTree\RenderTreeDiffBuilder.cs#L403-L490` (render tree diffing)
- Hot path: yes | Complexity: high
- APIs: `System.StringComparer`, `System.Collections.Generic.Dictionary<TKey,TValue>`

## pre-inlined diff loop

RenderTreeDiffBuilder keeps AppendDiffEntriesForRange as one large method rather than extracting small helpers.

- Do: Keep ultra-hot diff loops cohesive and benchmark before refactoring them into private helpers.
- Why: The code comments report that naive extraction worsens Mono WebAssembly diff performance by about 10 percent due to parameter passing.
- Source: `Components\Components\src\RenderTree\RenderTreeDiffBuilder.cs#L43-L56` (render tree diffing)
- Hot path: yes | Complexity: high
- APIs: `Microsoft.AspNetCore.Components.RenderTree.RenderTreeDiffBuilder`

## TypeNameHash stack hashing

TypeNameHash encodes type names into a fixed stack byte buffer and hashes into a stack SHA256 buffer before hex encoding.

- Do: Try Encoding.UTF8.TryGetBytes into stackalloc storage, fallback to Encoding.GetBytes only for oversized values, and hash into stackalloc output.
- Why: Typical type names avoid intermediate byte-array allocations during component marker hashing.
- Source: `Components\Endpoints\src\Rendering\TypeNameHash.cs#L12-L33` (endpoint rendering)
- Hot path: either | Complexity: low
- APIs: `stackalloc`, `System.Text.Encoding.UTF8`, `System.Security.Cryptography.SHA256.HashData`

## resource fingerprint stack buffers and rented UTF-8 fallback

ResourceCollectionUrlEndpoint hashes resource URLs using a 1024-byte stack buffer, rents only for longer UTF-8 data, and stackallocs the final hash and base64 chars.

- Do: Use IncrementalHash with TryGetBytes into a reusable stack Span<byte>, renting and reusing a larger byte[] only when needed.
- Why: Most resource names hash without per-string byte arrays while long names still avoid unbounded stack growth.
- Source: `Components\Endpoints\src\Builder\ResourceCollectionUrlEndpoint.cs#L112-L137` (resource collection endpoints)
- Hot path: either | Complexity: low
- APIs: `System.Security.Cryptography.IncrementalHash`, `stackalloc`, `System.Buffers.ArrayPool<T>`, `System.Text.Encoding.UTF8`

## server component operation duplicate check stackalloc

ServerComponentDeserializer tracks seen SSR component IDs in a stackalloc int span for up to 128 operations and rents for larger batches.

- Do: Use Span<T>.Contains over a populated slice with a small-count stackalloc threshold and ArrayPool<T> fallback.
- Why: Common batches avoid heap allocation while still validating duplicate IDs for arbitrarily large operation sets.
- Source: `Components\Server\src\Circuits\ServerComponentDeserializer.cs#L294-L365` (server circuits)
- Hot path: either | Complexity: low
- APIs: `stackalloc`, `System.Span<T>`, `System.Buffers.ArrayPool<T>`, `System.MemoryExtensions.Contains`

## JS component parameter reflection cache

JSComponentInterop caches per-component parameter metadata in a ConcurrentDictionary and clears it on hot reload.

- Do: Cache reflection-derived component metadata with ConcurrentDictionary<Type, ParameterTypeCache>.GetOrAdd and invalidate on hot reload.
- Why: Repeated JS component parameter binding avoids rescanning bindable properties and reclassifying EventCallback parameter kinds.
- Source: `Components\Web\src\JSComponents\JSComponentInterop.cs#L26-L33` (web JS components)
- Hot path: either | Complexity: medium
- APIs: `System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>`, `Microsoft.AspNetCore.Components.Reflection.ComponentProperties`
