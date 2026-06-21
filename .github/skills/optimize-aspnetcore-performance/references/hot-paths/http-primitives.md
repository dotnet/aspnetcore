# HTTP primitives and headers (ASP.NET Core source patterns)

Source-proven performance patterns from this repository, by hot-path component. These are the primary worked examples: when you touch this component, match these patterns. Before adding raw BCL primitives, check [../repo-helpers.md](../repo-helpers.md) for an existing shared helper. Paths are relative to `src`.

## ASCII-first string.Create conversion

StringUtilities.GetAsciiOrUTF8String creates a string of the byte span length, attempts ASCII widening directly into it, and marks the string for UTF-8 fallback only on non-ASCII input.

- Do: Use string.Create with System.Text.Ascii helpers for ASCII-dominant protocol data and fall back only when conversion reports failure.
- Why: Header bytes that are ASCII become strings in one allocation without going through Encoding.GetString.
- Source: `Shared\ServerInfrastructure\StringUtilities.cs#L18-L65` (header byte-to-string conversion)
- Hot path: yes | Complexity: low
- APIs: `string.Create`, `System.Text.Ascii`, `System.Buffers.OperationStatus`

## ArrayPool copy buffer for bounded stream copy

StreamCopyOperationInternal rents one byte buffer for a ranged copy loop and returns it after all async reads and writes complete.

- Do: Rent a single ArrayPool<byte> buffer around the copy loop and return it in a finally block.
- Why: Repeated file-to-network copies avoid per-request buffer allocations and cap each read to the remaining byte count.
- Source: `Http\Shared\StreamCopyOperationInternal.cs#L32-L80` (HTTP stream copy)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.ArrayPool<T>`, `Stream.ReadAsync`, `Stream.WriteAsync`

## Lazy request id materialization with string.Create

HttpRequestIdentifierFeature creates the TraceIdentifier only when requested and formats the incrementing id as fixed-width base32 directly into a string.Create buffer.

- Do: Defer derived string creation until the property is read and use string.Create for fixed-width encodings.
- Why: Requests that never read TraceIdentifier avoid the cost entirely, and requests that do allocate only the final string.
- Source: `Http\Http\src\Features\HttpRequestIdentifierFeature.cs#L21-L58` (HTTP request identifier feature)
- Hot path: yes | Complexity: low
- APIs: `string.Create`, `System.Threading.Interlocked`

## PipeReader parsing with single-segment fast path

FormPipeReader reads form data from PipeReader, dispatches single-segment buffers to a span parser, and keeps multi-segment parsing in a separate SequenceReader path.

- Do: Branch on ReadOnlySequence<T>.IsSingleSegment and keep the fast span parser separate from the multi-segment parser.
- Why: The common contiguous-buffer case avoids SequenceReader overhead while preserving correctness across pipeline segment boundaries.
- Source: `Http\WebUtilities\src\FormPipeReader.cs#L91-L150; Http\WebUtilities\src\FormPipeReader.cs#L152-L260` (application/x-www-form-urlencoded parser)
- Hot path: yes | Complexity: low
- APIs: `System.IO.Pipelines.PipeReader`, `System.Buffers.ReadOnlySequence<T>`, `System.Buffers.SequenceReader<T>`

## Readonly value object with SearchValues fast path

PathString is a readonly struct and returns the original path string when a span search finds no characters that need escaping.

- Do: Model small HTTP primitives as readonly structs and use IndexOfAnyExcept before falling back to escaping.
- Why: Common already-safe paths avoid defensive copies, allocations, and per-character escape loops.
- Source: `Http\Http.Abstractions\src\PathString.cs#L20-L87` (PathString URI formatting)
- Hot path: yes | Complexity: low
- APIs: `readonly struct`, `System.Buffers.SearchValues`, `ReadOnlySpan<char>.IndexOfAnyExcept`

## Safe-host scan before IDN conversion

HostString checks for only safe host characters and returns the stored value directly before doing IDN parsing and string concatenation.

- Do: Guard expensive normalization with ContainsAnyExcept over a precomputed SearchValues set.
- Why: The normal ASCII host case avoids punycode conversion and new string creation.
- Source: `Http\Http.Abstractions\src\HostString.cs#L18-L153` (HostString URI formatting)
- Hot path: yes | Complexity: low
- APIs: `readonly struct`, `System.Buffers.SearchValues`, `ReadOnlySpan<char>.ContainsAnyExcept`

## SearchValues cookie-octet scanning

CookieHeaderParserShared scans cookie values with IndexOfAnyExcept over the RFC cookie-octet set.

- Do: Use SearchValues for RFC value character classes and slice StringSegment around the match.
- Why: Cookie parsing identifies the end of the value with one optimized span operation instead of checking delimiter and control characters in a loop.
- Source: `Http\Shared\CookieHeaderParserShared.cs#L13-L16; Http\Shared\CookieHeaderParserShared.cs#L213-L240` (Cookie header parsing)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SearchValues`, `StringSegment`, `ReadOnlySpan<char>.IndexOfAnyExcept`

## SearchValues for RFC character class validation

HttpCharacters precomputes allowed authority, host, token, and field-value character sets and validates spans with IndexOfAnyExcept or IndexOfAny.

- Do: Use SearchValues.Create once and validate ReadOnlySpan<T> with IndexOfAnyExcept or ContainsAnyExcept.
- Why: The framework gets vectorized set membership over byte and char spans instead of per-character branching in header and request validation paths.
- Source: `Shared\ServerInfrastructure\HttpCharacters.cs#L22-L54` (HTTP character validation)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SearchValues`, `System.ReadOnlySpan<T>.IndexOfAnyExcept`

## SearchValues token parser

HttpRuleParser.GetTokenLength uses a precomputed RFC token character set and one span search to find the first non-token character.

- Do: Represent RFC token character classes with SearchValues and return IndexOfAnyExcept results directly.
- Why: Header parsers can measure token length with optimized span search rather than a custom loop with separator tests.
- Source: `Shared\HttpRuleParser.cs#L14-L65` (HTTP header token parsing)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SearchValues`, `StringSegment.AsSpan`

## Segmented escaping with vectorized skip-ahead

PathString.ToEscapedUriComponent appends valid and invalid segments in batches and uses IndexOfAnyExcept to skip runs of safe characters.

- Do: After entering an escape loop, copy contiguous safe spans and escape contiguous unsafe spans instead of processing every char uniformly.
- Why: Batching minimizes StringBuilder work and lets safe stretches be scanned by optimized span primitives.
- Source: `Http\Http.Abstractions\src\PathString.cs#L89-L171` (PathString URI escaping)
- Hot path: yes | Complexity: low
- APIs: `System.Text.StringBuilder`, `System.Uri.EscapeDataString`, `ReadOnlySpan<char>.IndexOfAnyExcept`

## SkipLocalsInit with stackalloc-or-pool sequence flattening

FormPipeReader flattens multi-segment form data to a stack buffer when short and rents a byte array for larger sequences before decoding.

- Do: For ReadOnlySequence<T>, prefer IsSingleSegment, then stackalloc below a threshold, then ArrayPool for large copies.
- Why: It handles split pipeline segments without allocating for small keys and values and without risking large stack allocations.
- Source: `Http\WebUtilities\src\FormPipeReader.cs#L344-L371` (form value decoding)
- Hot path: yes | Complexity: low
- APIs: `System.Runtime.CompilerServices.SkipLocalsInitAttribute`, `System.Buffers.ReadOnlySequence<T>`, `stackalloc`, `System.Buffers.ArrayPool<T>`

## Stackalloc threshold for URI decode buffers

PathString.FromUriComponent uses stackalloc for short decoded path buffers and falls back to heap arrays for larger inputs.

- Do: Use a small constant StackAllocThreshold and branch to stackalloc only for bounded input sizes.
- Why: Short request paths decode without heap allocation while the threshold prevents excessive stack usage.
- Source: `Http\Http.Abstractions\src\PathString.cs#L180-L207` (PathString URI decoding)
- Hot path: yes | Complexity: low
- APIs: `stackalloc`, `System.Span<T>`

## UTF-8 delimiter literals for form parsing

FormPipeReader uses static u8 spans for '=' and '&' when the request encoding is UTF-8 or ASCII, falling back to encoded byte arrays only for other encodings.

- Do: Represent fixed ASCII protocol delimiters as ReadOnlySpan<byte> properties backed by u8 literals.
- Why: The common encoding path uses non-allocating static data for delimiter searches.
- Source: `Http\WebUtilities\src\FormPipeReader.cs#L22-L35; Http\WebUtilities\src\FormPipeReader.cs#L161-L198` (form delimiter scanning)
- Hot path: yes | Complexity: low
- APIs: `u8 string literal`, `ReadOnlySpan<byte>`, `ReadOnlySpan<byte>.IndexOf`

## Lazy query parsing with compact accumulators

QueryFeature parses only when the raw query string changes and KvpAccumulator stores the first value as StringValues before switching duplicate keys to a list-backed expanding accumulator.

- Do: Cache parsed protocol data by original raw value and promote storage only when duplicate values require it.
- Why: The common zero-or-one-value query key case avoids list allocations and reparsing unchanged query strings.
- Source: `Http\Http\src\Features\QueryFeature.cs#L50-L70; Http\Http\src\Features\QueryFeature.cs#L96-L115; Http\Http\src\Features\QueryFeature.cs#L116-L215` (query string parsing)
- Hot path: yes | Complexity: medium
- APIs: `SkipLocalsInitAttribute`, `StringValues`, `AdaptiveCapacityDictionary`

## Precomputed length string.Create for Set-Cookie

SetCookieHeaderValue.ToString computes the final header length from optional attributes and uses string.Create to append all segments into the destination span.

- Do: Precalculate wire-format length, then append tokens and formatted values into a string.Create span.
- Why: Large cookie headers are built in one allocation with no repeated string concatenation or StringBuilder growth.
- Source: `Http\Headers\src\SetCookieHeaderValue.cs#L230-L274` (Set-Cookie header formatting)
- Hot path: yes | Complexity: medium
- APIs: `string.Create`, `Span<T>`, `DateTimeOffset.TryFormat`

## Ref struct wrapper over IBufferWriter

BufferWriter<T> is an internal ref struct that caches the current span from IBufferWriter<byte>, aggressively inlines small write methods, and moves buffer refresh to a no-inline slow path.

- Do: Wrap IBufferWriter<T> in a ref struct that tracks the current span, aggressively inline Advance/Write/Ensure, and mark refill helpers NoInlining.
- Why: Hot writes operate directly on spans with minimal interface calls and keep the uncommon buffer refill out of the inlined path.
- Source: `Shared\ServerInfrastructure\BufferWriter.cs#L8-L151` (server infrastructure buffer writing)
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.IBufferWriter<T>`, `ref struct`, `MethodImplOptions.AggressiveInlining`, `MethodImplOptions.NoInlining`

## Hardware intrinsic hex suffix formatter

StringUtilities.ConcatAsHexSuffix uses string.Create and, when SSSE3 is available, Vector128 shuffle operations to format eight hex digits into the result span.

- Do: Keep intrinsic code behind IsSupported checks and provide a simple scalar fallback with non-allocating u8 lookup data.
- Why: The hot formatter avoids Number.ToString and uses SIMD lookup to reduce scalar shifts and bounds checks.
- Source: `Shared\ServerInfrastructure\StringUtilities.cs#L110-L198` (server infrastructure hex formatting)
- Hot path: yes | Complexity: high
- APIs: `string.Create`, `System.Runtime.Intrinsics.Vector128`, `System.Runtime.Intrinsics.X86.Ssse3`, `u8 string literal`

## SearchValues for RFC5987 attr-char runs

ContentDispositionHeaderValue.Encode5987 appends runs of RFC5987 attr-chars and only percent-encodes the remaining rune spans.

- Do: Build encoders as alternating safe-run and unsafe-run span scans using IndexOfAnyExcept and IndexOfAny.
- Why: Valid ASCII runs are copied as whole spans and expensive UTF-8 hex encoding is limited to characters that require it.
- Source: `Http\Headers\src\ContentDispositionHeaderValue.cs#L38-L41; Http\Headers\src\ContentDispositionHeaderValue.cs#L615-L648` (Content-Disposition RFC5987 encoding)
- Hot path: either | Complexity: low
- APIs: `System.Buffers.SearchValues`, `System.Text.Rune`, `System.Text.StringBuilder`

## SearchValues to choose Base64Url fast path

WebEncoders.Base64UrlDecode uses a SearchValues set for '+', '/', '-', and '_' to quickly decide whether Base64Url decoding can be used.

- Do: Use a small SearchValues differentiator set to classify input before choosing a more expensive compatibility path.
- Why: The scan stops as soon as a differentiating character is found and avoids rewriting input when the modern Base64Url decoder can handle it directly.
- Source: `Shared\WebEncoders\WebEncoders.cs#L31-L33; Shared\WebEncoders\WebEncoders.cs#L75-L91` (Base64Url decoding)
- Hot path: either | Complexity: low
- APIs: `System.Buffers.SearchValues`, `ReadOnlySpan<char>.IndexOfAny`, `System.Buffers.Text.Base64Url`

## SkipLocalsInit with stackalloc-or-pool Base64Url encode

WebEncoders.Base64UrlEncode computes the exact output size, uses stackalloc up to 128 chars, rents from ArrayPool for larger values, and returns the buffer after creating the result string.

- Do: Compute required output size first, then choose stackalloc or ArrayPool based on a constant threshold.
- Why: Small encodes avoid heap arrays and large encodes reuse buffers while the stackalloc guard keeps stack usage predictable.
- Source: `Shared\WebEncoders\WebEncoders.cs#L398-L427` (Base64Url encoding)
- Hot path: either | Complexity: low
- APIs: `System.Runtime.CompilerServices.SkipLocalsInitAttribute`, `stackalloc`, `System.Buffers.ArrayPool<T>`

## string.Create for fixed-size quoted date formatting

HeaderUtilities.FormatDate uses string.Create for the quoted RFC1123 form and writes the quotes plus TryFormat output directly into the destination span.

- Do: For fixed-size formatted strings, call string.Create with the final length and fill the destination span in place.
- Why: The quoted date path creates exactly one string and avoids temporary concatenation around the formatted date.
- Source: `Http\Headers\src\HeaderUtilities.cs#L570-L581` (HTTP date formatting)
- Hot path: either | Complexity: low
- APIs: `string.Create`, `DateTimeOffset.TryFormat`, `Span<T>`

## Paged pooled buffer with PipeWriter drain

PagedByteBuffer rents fixed-size pages from ArrayPool, appends data across pages, drains pages to Stream or PipeWriter, and returns all pages when cleared or disposed.

- Do: Store growing byte content as pooled pages and expose drain methods that write each page to Stream or PipeWriter before returning pages.
- Why: Writes avoid contiguous reallocation as buffered content grows and can flush directly to pipelines without copying into a single array.
- Source: `Http\WebUtilities\src\PagedByteBuffer.cs#L9-L140` (response buffering)
- Hot path: either | Complexity: medium
- APIs: `System.Buffers.ArrayPool<T>`, `System.IO.Pipelines.PipeWriter`, `ReadOnlyMemory<T>`

## SkipLocalsInit with stackalloc-or-pool MIME buffer

ContentDispositionHeaderValue.EncodeMimeWithQuotes builds the MIME encoded value in a byte span using u8 literals, stackalloc for small output, and ArrayPool for larger output.

- Do: Pair [SkipLocalsInit] with a size guard, stackalloc for small spans, ArrayPool<T>.Shared.Rent for large spans, and return pooled arrays in finally-equivalent cleanup.
- Why: It avoids intermediate strings and arrays while bounding stack usage and skipping local zero initialization for the temporary buffer method.
- Source: `Http\Headers\src\ContentDispositionHeaderValue.cs#L35-L36; Http\Headers\src\ContentDispositionHeaderValue.cs#L541-L570` (Content-Disposition MIME encoding)
- Hot path: either | Complexity: medium
- APIs: `System.Runtime.CompilerServices.SkipLocalsInitAttribute`, `stackalloc`, `System.Buffers.ArrayPool<T>`, `u8 string literal`

## Thresholded in-memory buffering with pooled spill copy

FileBufferingReadStream rents an initial memory buffer up to 1 MB and, when the threshold is exceeded, spools buffered content to a temp file using a pooled copy buffer or the rented backing buffer directly.

- Do: Use a memory threshold, cap rented buffer size, and return pooled buffers immediately after moving data to the spill target.
- Why: Small bodies stay in memory with pooled storage while large bodies avoid unbounded memory growth and still avoid extra allocation during spill.
- Source: `Http\WebUtilities\src\FileBufferingReadStream.cs#L19-L95; Http\WebUtilities\src\FileBufferingReadStream.cs#L274-L304` (request body buffering)
- Hot path: either | Complexity: medium
- APIs: `System.Buffers.ArrayPool<T>`, `System.IO.MemoryStream`, `System.IO.FileStream`

## Two-pass quoted-string sizing with string.Create

HeaderUtilities counts backslashes or escaping needs first, then uses string.Create to unescape or escape quoted strings into an exactly sized destination.

- Do: Precompute the exact output length for header quoting transforms, then fill the string.Create span.
- Why: The second pass writes directly to the result string with no growable builder or intermediate strings.
- Source: `Http\Headers\src\HeaderUtilities.cs#L618-L651; Http\Headers\src\HeaderUtilities.cs#L690-L717` (quoted-string header utilities)
- Hot path: either | Complexity: medium
- APIs: `string.Create`, `StringSegment`, `Span<T>`
