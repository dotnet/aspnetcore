# Common optimization recipes

Multi-step transforms that stay allocation-free (or allocation-minimal) in the interim. Each recipe chains the area techniques into an end-to-end pattern: take an input, transform it, and produce the output without materializing throwaway strings, arrays, or boxes along the way. Apply on hot paths; see [decision-framework.md](decision-framework.md) for when the complexity is justified, and [measuring.md](measuring.md) to confirm the win.

## Async and tasks

### Async handoff without Task.Run

Task.Run around an async method queues work and allocates an extra Task when the goal is only to avoid running subsequent code synchronously.

Steps:
1. Call the async helper directly instead of wrapping it in Task.Run.
2. At the start or at the chosen boundary, await a completed or quickly returned task with ConfigureAwaitOptions.ForceYielding.
3. Continue with the rest of the async work after that await.
4. Keep ConfigureAwait(false) or ConfigureAwaitOptions.None for later awaits that do not need context capture.

APIs: `System.Threading.Tasks.ConfigureAwaitOptions.ForceYielding`, `System.Threading.Tasks.Task.ConfigureAwait`, `System.Threading.Tasks.Task.Run`
Available since: .NET 8

### Bounded async fan-out over a stream

Creating one Task per input item or buffering an entire async sequence can explode memory and overload downstream services.

Steps:
1. Expose or consume the input as IAsyncEnumerable<T> when items arrive over time.
2. Create ParallelOptions with MaxDegreeOfParallelism and CancellationToken.
3. Call Parallel.ForEachAsync over the async stream with a static async body when possible.
4. Use Channel<T> when producers and consumers need an explicit asynchronous queue boundary.

APIs: `System.Collections.Generic.IAsyncEnumerable<T>`, `System.Threading.Tasks.Parallel.ForEachAsync`, `System.Threading.Tasks.ParallelOptions`, `System.Threading.Channels.Channel<T>`
Available since: .NET 10, .NET 6

### Completion-order task processing without O(N^2) removals

A loop around Task.WhenAny over a List<Task> repeatedly hooks continuations and removes completed tasks, making large batches expensive.

Steps:
1. Collect the tasks once in an array, list, or enumerable.
2. Iterate await foreach over Task.WhenEach(tasks).
3. Inside the loop, handle or await the completed task to observe exceptions and results.
4. Do not mutate a remaining-task list just to find the next completion.

APIs: `System.Threading.Tasks.Task.WhenEach`, `System.Collections.Generic.IAsyncEnumerable<T>`, `System.Threading.Tasks.Task.WhenAny`
Available since: .NET 9

### Reusable timeout CancellationTokenSource

Allocating a CancellationTokenSource per operation with CancelAfter or timeout constructors is costly, but unsafe reuse can race with timer cancellation.

Steps:
1. Rent or create a CancellationTokenSource for the operation.
2. Use CancelAfter or pass its token to async operations.
3. After the operation, call TryReset before returning it to a pool.
4. Dispose or discard the source when TryReset returns false.

APIs: `System.Threading.CancellationTokenSource`, `System.Threading.CancellationTokenSource.TryReset`, `System.Threading.CancellationTokenSource.CancelAfter`
Available since: .NET 6

### Task timeout without helper tasks

Task.WhenAny(Task.Delay(timeout), operation) allocates helper state and is easy to get wrong when cancellation and exception propagation are involved.

Steps:
1. Start or receive the operation task.
2. Call operation.WaitAsync(timeout, cancellationToken) or the TimeProvider overload.
3. Await the returned task and let it propagate timeout, cancellation, or the operation exception.
4. Remove manual CancellationTokenSource cancellation used only to stop a Task.Delay.

APIs: `System.Threading.Tasks.Task.WaitAsync`, `System.Threading.Tasks.Task<TResult>.WaitAsync`, `System.TimeProvider`
Available since: .NET 8, .NET 6

## Collections

### Fill a List without per-element Add overhead

Creating a List<T> with a known final size by repeatedly calling Add pays capacity checks and bounds checks for every item.

Steps:
1. Create the List<T> and determine the final count.
2. Call CollectionsMarshal.SetCount(list, count) to set Count and ensure backing storage.
3. Call CollectionsMarshal.AsSpan(list) to get a writable span over the initialized range.
4. Fill, copy, or transform directly into the span, and do not mutate the list while the span is in use.

APIs: `System.Runtime.InteropServices.CollectionsMarshal.SetCount`, `System.Runtime.InteropServices.CollectionsMarshal.AsSpan`, `System.Span<T>.Fill`, `System.Span<T>.CopyTo`
Available since: .NET 8, .NET 9

### Random sample without full shuffle buffer

Shuffling an entire source and then taking a small subset allocates and processes the whole input even when only N results are needed.

Steps:
1. Keep the source as IEnumerable<T> or a known collection.
2. Call source.Shuffle().Take(n) to let LINQ use optimized sampling where applicable.
3. If only membership in the sample is needed, call source.Shuffle().Take(n).Contains(value) directly so LINQ can use the specialized probability path.

APIs: `System.Linq.Enumerable.Shuffle`, `System.Linq.Enumerable.Take`, `System.Linq.Enumerable.Contains`
Available since: .NET 10

### Span word counting with one allocation per distinct key

Counting words in a ReadOnlySpan<char> into a Dictionary<string,int> normally allocates a string for every occurrence just to probe the dictionary.

Steps:
1. Create Dictionary<string,int> with StringComparer.Ordinal or StringComparer.OrdinalIgnoreCase.
2. Get Dictionary<string,int>.AlternateLookup<ReadOnlySpan<char>> from the dictionary.
3. Parse matches with Regex.EnumerateMatches over the input span and slice each word.
4. Update counts with CollectionsMarshal.GetValueRefOrAddDefault(alternate, word, out _)++ so only new distinct words are materialized.

APIs: `System.Text.RegularExpressions.Regex.EnumerateMatches`, `System.Collections.Generic.Dictionary<TKey,TValue>.GetAlternateLookup`, `System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault`, `System.StringComparer`
Available since: .NET 9

### Static read-mostly lookup table

A Dictionary or ImmutableDictionary used for a table built once and read frequently either exposes mutation or spends too much time in tree lookups.

Steps:
1. Build the data with a temporary mutable Dictionary or HashSet using an appropriate comparer and capacity.
2. After the data is complete, call ToFrozenDictionary or ToFrozenSet.
3. Store the frozen collection in a readonly field and perform all hot-path lookups against the frozen instance.
4. For small compile-time ordinal string sets, consider a string switch instead of a collection.

APIs: `System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary`, `System.Collections.Frozen.FrozenSet.ToFrozenSet`, `System.Collections.Frozen.FrozenDictionary<TKey,TValue>`, `System.Collections.Frozen.FrozenSet<T>`
Available since: .NET 9, .NET 8

## Encoding

### Build a UTF-8 protocol line in one byte buffer

Formatting a protocol line with interpolation and then Encoding.UTF8.GetBytes allocates a string and a byte array.

Steps:
1. Choose a stackalloc, pooled, or IBufferWriter-provided Span<byte> large enough for the line.
2. Use u8 literals for fixed ASCII tokens where they are copied separately.
3. Use Utf8.TryWrite to format dynamic values directly into the byte span.
4. Advance or slice by bytesWritten and send the bytes without creating a string.

APIs: `System.Text.Unicode.Utf8.TryWrite`, `System.IUtf8SpanFormattable.TryFormat`, `System.ReadOnlySpan<byte>`
Available since: .NET 7, .NET 8

### Encode JWT segments with Base64Url directly

JWT and URI payload code that Base64-encodes then replaces characters performs an avoidable second pass and may allocate intermediate text.

Steps:
1. Keep the segment payload as ReadOnlySpan<byte>.
2. Size the destination using Base64.GetMaxEncodedToUtf8Length or the corresponding Base64Url maximum length helper if available.
3. Call Base64Url.EncodeToUtf8 for byte-oriented output or Base64Url.EncodeToChars for char-oriented output.
4. Slice the destination to the written length and write it directly to the token or URI builder.

APIs: `System.Buffers.Text.Base64Url.EncodeToUtf8`, `System.Buffers.Text.Base64Url.EncodeToChars`, `System.Buffers.Text.Base64.GetMaxEncodedToUtf8Length`
Available since: .NET 9

### Hash bytes then hex encode without allocations

A digest must be rendered as hex, but HashData plus Convert.ToHexString allocates a byte array and a string when the caller only needs to write to an existing buffer.

Steps:
1. Allocate or rent a byte span sized to the hash length, using stackalloc for small fixed-size digests.
2. Compute the hash into the byte span with the algorithm's TryHashData or span-based hash API.
3. Allocate or use a destination char span sized to 2 * hashLength.
4. Call Convert.TryToHexString or Convert.TryToHexStringLower to format directly into the destination.
5. Write the chars to the final sink and clear or return pooled buffers if needed.

APIs: `System.Security.Cryptography.HashAlgorithm.TryHashData`, `System.Convert.TryToHexString`, `System.Convert.TryToHexStringLower`
Available since: .NET 9

### Parse UTF-8 identifiers without transcoding

Parsers for UTF-8 protocols often create strings just to parse Guid, Version, or other primitive values.

Steps:
1. Keep the token as ReadOnlySpan<byte> from the input buffer.
2. For supported types, call their ReadOnlySpan<byte> Parse or TryParse overloads or generic IUtf8SpanParsable<TSelf> helpers.
3. For older target frameworks, use a stackalloc char scratch buffer with Encoding.UTF8.TryGetChars before falling back to Encoding.UTF8.GetString.
4. Avoid keeping the temporary string or char data after parsing.

APIs: `System.IUtf8SpanParsable<TSelf>`, `System.Guid.TryParse`, `System.Version.Parse`, `System.Text.Encoding.TryGetChars`, `System.Text.Encoding.UTF8.GetString`
Available since: .NET 10

### Validate and decode Base64 into an exact buffer

Base64 input often arrives as text and naive code decodes into a newly allocated byte array or validates with a slow manual parser.

Steps:
1. Trim any protocol framing while keeping the payload as ReadOnlySpan<char> or ReadOnlySpan<byte>.
2. Call Base64.IsValid(payload, out decodedLength) to validate and compute the exact decoded size.
3. Use stackalloc for small decodedLength values or rent from ArrayPool for larger values.
4. Decode with Convert.TryFromBase64Chars for UTF-16 input or Base64.DecodeFromUtf8 for UTF-8 input.
5. Process the decoded bytes in the destination span and return pooled buffers promptly.

APIs: `System.Buffers.Text.Base64.IsValid`, `System.Convert.TryFromBase64Chars`, `System.Buffers.Text.Base64.DecodeFromUtf8`, `System.Buffers.ArrayPool<T>`
Available since: .NET 8, .NET 9

## IO and buffers

### PipeReader parse to PipeWriter response

Parse inbound bytes and produce outbound bytes without copying through MemoryStream or allocating per message.

Steps:
1. Await PipeReader.ReadAsync and inspect the returned ReadOnlySequence<byte> for complete frames.
2. Parse directly from sequence segments or copy only split frame fragments that require contiguity.
3. Reserve output with PipeWriter.GetSpan or GetMemory and encode or format directly into that buffer.
4. Call PipeWriter.Advance for produced bytes, PipeReader.AdvanceTo for consumed and examined positions, and FlushAsync when a response batch is ready.
5. Use native PipeWriter-targeting APIs, such as JsonSerializer.SerializeAsync(PipeWriter, ...), when available.

APIs: `System.IO.Pipelines.PipeReader.ReadAsync`, `System.Buffers.ReadOnlySequence<T>`, `System.IO.Pipelines.PipeReader.AdvanceTo`, `System.IO.Pipelines.PipeWriter.GetSpan`, `System.IO.Pipelines.PipeWriter.Advance`, `System.Text.Json.JsonSerializer.SerializeAsync`
Available since: .NET 9, .NET 6, .NET Core 3.0

### Pooled buffer read-transform-write loop

Read bytes from a stream, transform them, and write them onward without allocating a new input or output array per chunk.

Steps:
1. Rent a byte[] from ArrayPool<byte>.Shared sized for the expected chunk, or reuse a caller-owned Memory<byte>.
2. In a try/finally, call source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).
3. Process only the slice containing the returned byte count and write transformed output into an IBufferWriter<byte>, PipeWriter, or second pooled buffer.
4. Call Advance with the exact number of bytes produced and FlushAsync at batching or protocol boundaries.
5. Return rented arrays in finally, clearing only when data sensitivity or reference retention requires it.

APIs: `System.Buffers.ArrayPool<T>`, `System.IO.Stream.ReadAsync`, `System.Buffers.IBufferWriter<T>`, `System.IO.Pipelines.PipeWriter.Advance`, `System.IO.Pipelines.PipeWriter.FlushAsync`
Available since: .NET 10, .NET 7, .NET 6, .NET Core 3.0

### Streaming decompression loop that handles partial reads

Decompress network or file data without deadlocking bidirectional protocols or buffering complete payloads.

Steps:
1. Wrap the source in DeflateStream, GZipStream, ZLibStream, or BrotliStream with leaveOpen set according to ownership.
2. Rent or reuse a buffer and call ReadAsync repeatedly.
3. Process every positive byte count immediately, even when it is smaller than the requested buffer length.
4. Stop only when ReadAsync returns 0, or when the framing protocol says the compressed member is complete.
5. Return pooled buffers in finally and avoid Flush calls except where the compression protocol or message boundary requires them.

APIs: `System.IO.Compression.DeflateStream`, `System.IO.Compression.GZipStream`, `System.IO.Compression.ZLibStream`, `System.IO.Compression.BrotliStream`, `System.IO.Stream.ReadAsync`, `System.Buffers.ArrayPool<T>`
Available since: .NET 10, .NET 6, .NET Core 3.0

### UTF-8 encode to file or native memory without temporary arrays

Convert text to UTF-8 for a file, pipe, process argument, or native call without allocating Encoding.UTF8.GetBytes(string) results per item.

Steps:
1. For constants, prefer a UTF-8 literal and write the ReadOnlySpan<byte> directly.
2. For dynamic text, call Encoding.UTF8.GetByteCount to size an existing span, pooled buffer, stack allocation, or native destination.
3. Call Encoding.UTF8.GetBytes(ReadOnlySpan<char>, Span<byte>) to encode directly into the final destination buffer.
4. Write the produced slice with Stream.WriteAsync, RandomAccess.WriteAsync, or PipeWriter.Advance as appropriate.
5. Return any pooled buffer in a finally block after the write completes.

APIs: `System.Text.Encoding.GetByteCount`, `System.Text.Encoding.GetBytes`, `System.IO.Stream.WriteAsync`, `System.IO.RandomAccess.WriteAsync`, `System.IO.Pipelines.PipeWriter`
Available since: .NET 8, .NET 7

## General

### Consume MemoryStream data without copying

MemoryStream.ToArray allocates and copies even when the next step can consume a segment.

Steps:
1. Call TryGetBuffer to obtain ArraySegment<byte>.
2. Pass array, offset, and count or create a ReadOnlySpan<byte> over that range.
3. Fall back to ToArray only when an exact independent array is required.

APIs: `System.IO.MemoryStream.TryGetBuffer`, `System.IO.MemoryStream.GetBuffer`, `System.IO.MemoryStream.ToArray`
Available since: .NET 8

### Format a path into one final string

Building fixed text plus numeric values with ToString and concatenation allocates intermediates.

Steps:
1. Confirm culture-sensitive symbols are not needed.
2. Use string.Create with an interpolated string handler and stackalloc char buffer.
3. Cast known non-negative signed values to unsigned when appropriate.
4. Return the final string directly.

APIs: `System.String.Create`, `System.Runtime.CompilerServices.DefaultInterpolatedStringHandler`
Available since: .NET 6

### Parse a string segment without allocation

Substringing before parsing or comparing creates a short-lived string.

Steps:
1. Take a ReadOnlySpan<char> with AsSpan(start).
2. Compare tokens with SequenceEqual or StartsWith.
3. Parse numeric slices with span Parse or TryParse overloads.
4. Allocate a string only if the text must escape independently.

APIs: `System.String.AsSpan`, `System.MemoryExtensions.SequenceEqual`, `System.MemoryExtensions.StartsWith`
Available since: .NET 6

## Numerics

### Bulk numeric transform with TensorPrimitives

A per-element numeric loop over large arrays leaves SIMD and shared optimized implementations unused.

Steps:
1. Keep data in arrays, Span<T>, or ReadOnlySpan<T> with a separate destination span unless the API documents in-place support.
2. Choose the matching TensorPrimitives operation such as Add, Divide, ConvertTruncating, SoftMax, or HammingDistance.
3. Use this only for measured hot paths where the additional package/API complexity is justified.

APIs: `System.Numerics.Tensors.TensorPrimitives`, `System.Numerics.Tensors.TensorPrimitives.Add`, `System.Numerics.Tensors.TensorPrimitives.ConvertTruncating`, `System.Numerics.Tensors.TensorPrimitives.HammingDistance`
Available since: .NET 10, .NET 9

### Generate random tokens with built-in sampling

Generating random text with a loop over Next can make one RNG call per character and can introduce biased indexing.

Steps:
1. Represent the allowed alphabet as a ReadOnlySpan<char> or string.
2. For pseudo-random text, call Random.Shared.GetItems or Random.Shared.GetString with the alphabet and desired length.
3. For security-sensitive text, use RandomNumberGenerator.GetItems or other RandomNumberGenerator APIs instead of Random.

APIs: `System.Random.GetItems`, `System.Random.GetString`, `System.Random.Shared`, `System.Security.Cryptography.RandomNumberGenerator.GetItems`
Available since: .NET 10, .NET 8

### Invariant DateTime protocol round trip without allocation

Protocol date/time values in common invariant formats are often parsed and formatted through culture-sensitive strings.

Steps:
1. Parse ReadOnlySpan<char> input with DateTimeOffset.ParseExact or TryParseExact using CultureInfo.InvariantCulture and the protocol format such as r or o.
2. Store or transform the DateTimeOffset value without converting to text.
3. Write to Span<char> or Span<byte> with TryFormat and the same invariant format.

APIs: `System.DateTimeOffset.ParseExact`, `System.DateTimeOffset.TryFormat`, `System.DateTime.TryFormat`, `System.Globalization.CultureInfo.InvariantCulture`
Available since: .NET 9, .NET 8, .NET Core 3.0

### Parse, compute, and UTF-8 format without strings

A UTF-8 numeric field must be parsed, transformed, and written back as UTF-8, and the naive path creates strings on both sides.

Steps:
1. Parse the input with the type's IUtf8SpanParsable<TSelf> or concrete TryParse(ReadOnlySpan<byte>, ...) overload.
2. Run the transform using generic math constraints such as INumber<T> when the algorithm should support multiple numeric types.
3. Format the result with IUtf8SpanFormattable.TryFormat or a concrete TryFormat(Span<byte>, out bytesWritten, ...) overload into the output buffer.

APIs: `System.IUtf8SpanParsable<TSelf>`, `System.Numerics.INumber<TSelf>`, `System.IUtf8SpanFormattable.TryFormat`
Available since: .NET 10, .NET 8, .NET 7

### UTF-8 GUID parse without intermediate string

A protocol supplies GUID text as UTF-8 bytes and the naive path decodes to a string before parsing.

Steps:
1. Keep the input as ReadOnlySpan<byte>.
2. If the span contains exactly one GUID, call Guid.TryParse(input, out Guid value).
3. Use Utf8Parser.TryParse only when parsing a GUID prefix from a larger buffer and you need bytesConsumed.

APIs: `System.Guid.TryParse`, `System.Guid.Parse`, `System.Buffers.Text.Utf8Parser.TryParse`
Available since: .NET 10

## Reflection

### Cache dynamic method invocation for unknown signatures

A framework discovers methods dynamically and invokes the same MethodInfo many times, but the exact signature is not known at compile time.

Steps:
1. Resolve and validate MethodInfo once during setup or first use.
2. Create a System.Reflection.MethodInvoker with MethodInvoker.Create(methodInfo).
3. Cache the MethodInvoker by MethodInfo or framework descriptor.
4. Invoke through MethodInvoker.Invoke overloads with cached or reused argument objects.

APIs: `System.Reflection.MethodInvoker.Create`, `System.Reflection.MethodInvoker.Invoke`, `System.Reflection.MethodBase.Invoke`
Available since: .NET 8, .NET 7

### Migrate private reflection to UnsafeAccessor

A library uses BindingFlags.NonPublic and FieldInfo.GetValue or MethodInfo.Invoke to cross an internal boundary on a hot path.

Steps:
1. Define a static extern accessor whose signature matches the target member and annotate it with UnsafeAccessor.
2. For generic target members, make the accessor generic and return the precise generic type or ref.
3. For .NET 10 hidden target types or static targets, annotate object-typed parameters with UnsafeAccessorType.
4. Replace per-call reflection lookup and invoke with calls to the accessor method.

APIs: `System.Runtime.CompilerServices.UnsafeAccessorAttribute`, `System.Runtime.CompilerServices.UnsafeAccessorTypeAttribute`, `System.Runtime.CompilerServices.UnsafeAccessorKind`
Available since: .NET 10, .NET 9

### Precompute DI activation factories

A framework repeatedly creates the same implementation type through dependency injection and pays constructor selection or reflection activation costs on each call.

Steps:
1. Mark the intended constructor with ActivatorUtilitiesConstructorAttribute when multiple constructors exist.
2. Call ActivatorUtilities.CreateFactory once with the implementation type and explicit argument types.
3. Cache the returned ObjectFactory by implementation type and argument shape.
4. Invoke the ObjectFactory for each activation rather than rebuilding the activation plan.

APIs: `Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructorAttribute`, `Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory`, `Microsoft.Extensions.DependencyInjection.ObjectFactory`
Available since: .NET 10, .NET 9, .NET 8

### Replace startup reflection scan with generated metadata

A framework scans assemblies at startup, discovers attributed members, and invokes them later, causing startup allocations and late-bound hot-path calls.

Steps:
1. Move discovery into a source generator or an explicit registration step that runs at build time or startup once.
2. Emit or create strongly typed delegates for known signatures with MethodInfo.CreateDelegate when generation is not available.
3. Store immutable lookup tables keyed by type, route, name, or format rather than rescanning assemblies.
4. Use generated code or cached delegates on the per-request path and keep reflection only as a fallback.

APIs: `System.Text.Json.Serialization.JsonSerializableAttribute`, `Microsoft.Extensions.Logging.LoggerMessageAttribute`, `System.Text.RegularExpressions.GeneratedRegexAttribute`, `System.Reflection.MethodInfo.CreateDelegate`
Available since: .NET 10, .NET 9, .NET 8, .NET 7, .NET 6

## Searching

### Allocation-free regex counting and existence checks

Regex.Match(...).Success and Regex.Matches(...).Count allocate or compute match data that callers do not use.

Steps:
1. Use GeneratedRegex for a compile-time-known pattern or cache a Regex for a dynamic reused pattern.
2. Call IsMatch for existence checks.
3. Call Count for match counts.
4. Use EnumerateMatches when only match offsets and lengths are needed.

APIs: `System.Text.RegularExpressions.GeneratedRegexAttribute`, `System.Text.RegularExpressions.Regex.IsMatch`, `System.Text.RegularExpressions.Regex.Count`, `System.Text.RegularExpressions.Regex.EnumerateMatches`
Available since: .NET 10, .NET 7

### Multi-token substring dispatch without regex allocation

A hot path needs to detect any of several literal tokens, possibly case-insensitively, and a regex alternation or repeated IndexOf calls adds overhead.

Steps:
1. Create a static readonly SearchValues<string> with the token array and StringComparison.Ordinal or OrdinalIgnoreCase.
2. Search the input span with ContainsAny or IndexOfAny using the cached SearchValues<string>.
3. Use Regex only when the pattern needs regex semantics beyond literal substring matching.
4. Fall back to string.IndexOf or span.IndexOf for one-off or older-target searches.

APIs: `System.Buffers.SearchValues.Create`, `System.MemoryExtensions.ContainsAny`, `System.MemoryExtensions.IndexOfAny`, `System.StringComparison.OrdinalIgnoreCase`, `System.String.IndexOf`
Available since: .NET 10

### Repeated delimiter scan with cached SearchValues

A parser or encoder repeatedly scans a span for any of several delimiter bytes or chars, and rebuilding the needle analysis on every IndexOfAny call wastes CPU.

Steps:
1. Create a static readonly SearchValues<byte> or SearchValues<char> for the delimiter set.
2. Convert the input to ReadOnlySpan<byte> or ReadOnlySpan<char> once.
3. Loop with span.IndexOfAny(searchValues), process the prefix, then slice past the match.
4. Use ContainsAny or ContainsAnyExcept instead when only validation is required.

APIs: `System.Buffers.SearchValues.Create`, `System.MemoryExtensions.IndexOfAny`, `System.MemoryExtensions.ContainsAny`, `System.ReadOnlySpan<T>.Slice`
Available since: .NET 10, .NET 8

### Validate then transform without allocating unchanged output

Encoders and normalizers often scan to find characters that need escaping or replacement, then allocate even when no change is needed.

Steps:
1. Use SearchValues or IndexOfAnyInRange to find the first character that needs transformation.
2. If no match is found, return the original string or report success without allocating.
3. Only allocate or rent an output buffer after the first required change is found.
4. Continue scanning the remaining span with the same cached search primitive.

APIs: `System.Buffers.SearchValues`, `System.MemoryExtensions.IndexOfAny`, `System.MemoryExtensions.IndexOfAnyInRange`, `System.String.ReplaceLineEndings`
Available since: .NET 10, .NET 8

## Serialization

### Consume concatenated streaming JSON objects without pre-splitting

A service emits multiple top-level JSON objects in one stream, and manual splitting creates strings or byte arrays before deserialization.

Steps:
1. Keep the input as a Stream of UTF-8 bytes.
2. Call JsonSerializer.DeserializeAsyncEnumerable<T>(stream, topLevelValues: true) with cached options or generated metadata when available.
3. Process each yielded item as it arrives and stop early when the desired item is found.
4. Avoid materializing the entire stream or each object as an intermediate string.

APIs: `System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable`, `System.Text.Json.Utf8JsonReader`, `System.IO.Stream`
Available since: .NET 9

### Serialize known DTOs straight to a PipeWriter

A server has typed response objects and a PipeWriter destination; serializing through reflection and Stream adapters adds metadata, adapter, and allocation overhead.

Steps:
1. Define a partial JsonSerializerContext with JsonSerializableAttribute for the DTO types.
2. Cache and reuse any required JsonSerializerOptions through the context or a singleton options instance.
3. Call JsonSerializer.SerializeAsync(pipeWriter, value, MyContext.Default.MyDto) directly.
4. Flush or complete the PipeWriter according to the surrounding pipeline ownership rules.

APIs: `System.Text.Json.Serialization.JsonSerializerContext`, `System.Text.Json.Serialization.JsonSerializableAttribute`, `System.Text.Json.JsonSerializer.SerializeAsync`, `System.IO.Pipelines.PipeWriter`
Available since: .NET 9, .NET 8, .NET 6

### Stream a large binary payload as a Base64 JSON property

A large binary stream must be included as a Base64 JSON string; buffering all bytes or all encoded text increases latency and working set.

Steps:
1. Create Utf8JsonWriter over the final Stream, IBufferWriter<byte>, or PipeWriter-backed destination.
2. Write the object start and the property name for the binary payload.
3. Read source bytes into a reusable rented buffer and pass each filled span to WriteBase64StringSegment with isFinalSegment false.
4. Call WriteBase64StringSegment with an empty final segment, write the object end, flush, and return the rented buffer.

APIs: `System.Text.Json.Utf8JsonWriter`, `System.Text.Json.Utf8JsonWriter.WritePropertyName`, `System.Text.Json.Utf8JsonWriter.WriteBase64StringSegment`, `System.Buffers.ArrayPool<T>`
Available since: .NET 10

### Write JSON to a pooled buffer without intermediate string or byte array

A library needs JSON bytes but naive code serializes to string and then encodes to UTF-8, allocating both representations.

Steps:
1. Rent or create an IBufferWriter<byte>, for example ArrayBufferWriter<byte> or a pooled implementation.
2. Create Utf8JsonWriter over that IBufferWriter<byte>.
3. Write tokens directly with Utf8JsonWriter or call JsonSerializer.Serialize(writer, value, generatedJsonTypeInfo).
4. Consume the written memory and return any pooled buffers after the bytes are sent or copied to their owner.

APIs: `System.Buffers.IBufferWriter<T>`, `System.Buffers.ArrayBufferWriter<T>`, `System.Text.Json.Utf8JsonWriter`, `System.Text.Json.JsonSerializer.Serialize`
Available since: .NET 10, .NET 8, .NET 6

## Strings and spans

### Base64Url encode without post-processing

URL-safe Base64 built on Convert requires scanning to replace characters and trim padding.

Steps:
1. Allocate, rent, or stackalloc a byte or char destination using the maximum encoded length.
2. Call Base64Url.EncodeToUtf8 or Base64Url.EncodeToChars into that destination.
3. Use the returned written count as the exact output slice.
4. Decode with the matching Base64Url span overloads.

APIs: `System.Buffers.Text.Base64Url.EncodeToUtf8`, `System.Buffers.Text.Base64Url.EncodeToChars`, `System.Buffers.Text.Base64Url.DecodeFromUtf8`, `System.Buffers.Text.Base64Url.DecodeFromChars`
Available since: .NET 9

### Comma-separated token match without allocations

A header or protocol field must be split, trimmed, and compared, while string.Split allocates an array and strings.

Steps:
1. Keep the original value as string or ReadOnlySpan<char>.
2. Enumerate foreach (Range r in source.AsSpan().Split(',')).
3. Slice with source.AsSpan(r), then call Trim on the span.
4. Compare with Equals using StringComparison.OrdinalIgnoreCase.
5. Return as soon as a match is found.

APIs: `System.MemoryExtensions.Split`, `System.MemoryExtensions.Trim`, `System.MemoryExtensions.Equals`
Available since: .NET 9

### Escape characters in StringBuilder with stackalloc spans

Escaping by formatting replacement strings and calling StringBuilder.Replace(string,string) allocates per character.

Steps:
1. Allocate a small stackalloc Span<char> for one escaped representation.
2. For each character, format the replacement into the stack buffer with TryWrite or TryFormat.
3. Pass a one-character ReadOnlySpan<char> and the written slice to StringBuilder.Replace.
4. Reuse the stack buffer for each escaped character.

APIs: `System.MemoryExtensions.TryWrite`, `System.Text.StringBuilder.Replace`, `System.ReadOnlySpan<T>.Slice`
Available since: .NET 9

### Lowercase hex encode and decode with reusable buffers

Hex conversion often uses ToHexString plus ToLowerInvariant or FromHexString(string), causing extra strings or arrays.

Steps:
1. Provide a Span<char> destination sized to inputBytes.Length * 2.
2. Call Convert.TryToHexStringLower(inputBytes, destination, out charsWritten).
3. Provide a Span<byte> destination sized to hexChars.Length / 2 for decoding.
4. Call Convert.FromHexString(hexChars, destination, out charsConsumed, out bytesWritten).

APIs: `System.Convert.TryToHexStringLower`, `System.Convert.FromHexString`
Available since: .NET 9

### UTF-8 protocol line formatting into an output buffer

A protocol writer needs UTF-8 bytes, but interpolation plus Encoding.UTF8.GetBytes allocates and transcodes a UTF-16 string.

Steps:
1. Receive, rent, or stackalloc a Span<byte> destination.
2. Use Utf8.TryWrite(destination, interpolatedString, out bytesWritten).
3. Let IUtf8SpanFormattable values format directly as UTF-8.
4. Advance the writer by bytesWritten or retry with a larger buffer.

APIs: `System.Text.Unicode.Utf8.TryWrite`, `System.IUtf8SpanFormattable.TryFormat`, `System.Buffers.IBufferWriter<T>`
Available since: .NET 8
