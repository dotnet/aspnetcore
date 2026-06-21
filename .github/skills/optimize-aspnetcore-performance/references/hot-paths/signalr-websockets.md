# SignalR and WebSockets (ASP.NET Core source patterns)

Source-proven performance patterns from this repository, by hot-path component. These are the primary worked examples: when you touch this component, match these patterns. Before adding raw BCL primitives, check [../repo-helpers.md](../repo-helpers.md) for an existing shared helper. Paths are relative to `src`.

## IBufferWriter single-byte record separator

TextMessageFormatter writes the JSON record separator by requesting one byte from IBufferWriter and advancing by one.

- Do: Use IBufferWriter<byte>.GetSpan(size) plus Advance(count) for tiny protocol markers.
- Why: It appends framing without allocating an array or going through Stream abstractions.
- Source: `SignalR\common\Shared\TextMessageFormatter.cs#L14-L18` (SignalR text message formatter)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.IBufferWriter<T>`

## ReadOnlySequence Stream write without coalescing

StreamExtensions writes single-segment sequences directly and iterates segments for multi-segment buffers.

- Do: Write ReadOnlySequence<byte> to Stream through single-segment and segment-iteration paths.
- Why: HTTP and transport writes avoid flattening pipeline buffers into temporary arrays.
- Source: `SignalR\common\Shared\StreamExtensions.cs#L14-L45` (SignalR shared stream output)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.ReadOnlySequence<T>`, `System.IO.Stream.WriteAsync`

## Span-based VarInt encoder reusable by callers

The lower-level length-prefix overload writes directly into a caller-provided Span<byte> and returns the byte count.

- Do: Expose Span<byte> overloads like BinaryMessageFormatter.WriteLengthPrefix(long, Span<byte>) for framing primitives.
- Why: Separating encoding from allocation lets callers reuse stack or pooled memory and keeps the encoder allocation-free.
- Source: `SignalR\common\Shared\BinaryMessageFormatter.cs#L20-L38` (SignalR binary message formatter)
- Hot path: yes | Complexity: low
- APIs: `System.Span<T>`

## Utf8JsonReader directly over ReadOnlySequence

JsonHubProtocol parses the framed payload by constructing Utf8JsonReader over the ReadOnlySequence<byte> returned by TextMessageParser.

- Do: Parse protocol JSON with Utf8JsonReader(ReadOnlySequence<byte>, ...) after pipeline framing.
- Why: The JSON hub protocol avoids converting UTF-8 payloads to strings before parsing.
- Source: `SignalR\common\Protocols.Json\src\Protocol\JsonHubProtocol.cs#L89-L97, L115-L151` (SignalR JSON hub protocol parser)
- Hot path: yes | Complexity: low
- APIs: `System.Text.Json.Utf8JsonReader`, `System.Buffers.ReadOnlySequence<T>`

## aggressively inlined text frame delimiter scan

TextMessageParser inlines the record-separator parser, scans Span<byte> with IndexOf for single segments, and uses PositionOf for multi-segment buffers.

- Do: Use TextMessageParser.TryParseMessage with Span.IndexOf and ReadOnlySequence.PositionOf split fast paths.
- Why: The common single-segment JSON frame path avoids virtual or iterator overhead and keeps multi-segment handling zero-copy.
- Source: `SignalR\common\Shared\TextMessageParser.cs#L12-L49` (SignalR text message parser)
- Hot path: yes | Complexity: low
- APIs: `System.Runtime.CompilerServices.MethodImplAttribute`, `System.Buffers.ReadOnlySequence<T>`, `System.MemoryExtensions.IndexOf`

## pre-encoded JSON property names with ValueTextEquals

JsonHubProtocol stores JsonEncodedText for known property names and compares incoming UTF-8 property tokens with ValueTextEquals.

- Do: Cache JsonEncodedText and compare Utf8JsonReader.ValueTextEquals(encoded.EncodedUtf8Bytes).
- Why: Property dispatch avoids allocating strings for every JSON property name in inbound hub messages.
- Source: `SignalR\common\Protocols.Json\src\Protocol\JsonHubProtocol.cs#L26-L47, L146-L169` (SignalR JSON hub protocol parser)
- Hot path: yes | Complexity: low
- APIs: `System.Text.Json.JsonEncodedText`, `System.Text.Json.Utf8JsonReader.ValueTextEquals`

## single-segment fast path with rare multi-segment copy

BinaryMessageParser gets a direct span for single-segment prefixes and only calls ToArray for rare multi-segment length prefixes.

- Do: Branch on ReadOnlySequence<T>.IsSingleSegment and isolate rare fallback copies in helpers.
- Why: The common case remains zero-copy while correctness is preserved when the short prefix crosses segment boundaries.
- Source: `SignalR\common\Shared\BinaryMessageParser.cs#L75-L83` (SignalR binary message parser)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.ReadOnlySequence<T>`, `System.ReadOnlySpan<T>`

## stackalloc VarInt prefix before IBufferWriter write

BinaryMessageFormatter encodes the SignalR binary payload length into a 5 byte stack buffer and writes only the used slice to the output writer.

- Do: Use BinaryMessageFormatter.WriteLengthPrefix(long, IBufferWriter<byte>) with a bounded stackalloc buffer.
- Why: The hot framing path avoids allocating a temporary byte array for the common small length prefix.
- Source: `SignalR\common\Shared\BinaryMessageFormatter.cs#L11-L18` (SignalR binary message formatter)
- Hot path: yes | Complexity: low
- APIs: `System.Span<T>`, `System.Buffers.IBufferWriter<T>`, `System.Buffers.BuffersExtensions.Write`

## PipeWriter-backed Stream adapter with synchronous ValueTask completion

PipeWriterStream writes spans and memories directly to a PipeWriter and returns default ValueTask when WriteAsync completes synchronously.

- Do: Adapt Stream to PipeWriter with Write(ReadOnlySpan<byte>) and an IsCompletedSuccessfully fast path.
- Why: Stream-based callers can feed pipelines without extra buffers, and completed writes avoid async state-machine allocation.
- Source: `SignalR\common\Shared\PipeWriterStream.cs#L50-L90` (SignalR HTTP connections pipeline adapter)
- Hot path: yes | Complexity: medium
- APIs: `System.IO.Pipelines.PipeWriter`, `System.Threading.Tasks.ValueTask<T>`, `System.Buffers.ReadOnlyMemory<T>`

## ReadOnlySequence WebSocket send without coalescing

WebSocketExtensions sends single-segment sequences directly and streams multi-segment sequences segment by segment with the final segment marking endOfMessage.

- Do: Send ReadOnlySequence<byte> by special-casing IsSingleSegment and iterating segments for the fallback.
- Why: Large or segmented SignalR payloads do not need to be copied into one contiguous array before WebSocket send.
- Source: `SignalR\common\Shared\WebSocketExtensions.cs#L14-L65` (SignalR WebSocket transport send)
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.ReadOnlySequence<T>`, `System.Net.WebSockets.WebSocket.SendAsync`

## ReadOnlySequence slicing parser over pipelines

BinaryMessageParser parses a VarInt length from a ReadOnlySequence<byte>, returns a payload slice, and advances the input sequence in place.

- Do: Use BinaryMessageParser.TryParseMessage(ref ReadOnlySequence<byte>, out ReadOnlySequence<byte>) style APIs for framed protocols.
- Why: Parsing over sequence slices avoids copying message bytes out of pipelines and naturally handles partial frames.
- Source: `SignalR\common\Shared\BinaryMessageParser.cs#L13-L72` (SignalR binary message parser)
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.ReadOnlySequence<T>`

## case-sensitive UTF-8 lookup before transcoding

Utf8HashLookup first hashes and compares encoded UTF-8 bytes for hub method names before falling back to case-insensitive char lookup.

- Do: Use Utf8HashLookup.TryGetValue(ReadOnlySpan<byte>, out string) for UTF-8 protocol-name lookup.
- Why: The normal exact-case method dispatch path avoids allocating or transcoding the incoming target name.
- Source: `SignalR\server\Core\src\Internal\Utf8HashLookup.cs#L58-L73` (SignalR hub method dispatch)
- Hot path: yes | Complexity: medium
- APIs: `System.HashCode.AddBytes`, `System.ReadOnlySpan<T>.SequenceEqual`

## dispatcher resolves UTF-8 target names through cache

DefaultHubDispatcher adds bound hub method names to Utf8HashLookup and later resolves target UTF-8 bytes without first creating a string.

- Do: Populate UTF-8 lookup caches during endpoint or method discovery and query them from protocol bytes.
- Why: Inbound invocations can map protocol bytes to canonical method names with minimal allocation on the dispatch path.
- Source: `SignalR\server\Core\src\Internal\DefaultHubDispatcher.cs#L785-L800, L820-L827` (SignalR hub dispatcher)
- Hot path: yes | Complexity: medium
- APIs: `Microsoft.AspNetCore.SignalR.Internal.Utf8HashLookup`, `System.ReadOnlySpan<T>`

## precomputed dual hash tables for method names

Utf8HashLookup stores both case-insensitive string hashes and case-sensitive UTF-8 byte hashes when hub methods are added.

- Do: Pre-encode names and store separate hash chains for exact UTF-8 and fallback comparisons.
- Why: Precomputing dispatch metadata moves encoding and table setup out of per-invocation lookup.
- Source: `SignalR\server\Core\src\Internal\Utf8HashLookup.cs#L29-L55, L112-L120` (SignalR hub method dispatch)
- Hot path: yes | Complexity: medium
- APIs: `System.Text.Encoding.UTF8.GetBytes`, `System.HashCode.AddBytes`, `string.GetHashCode(ReadOnlySpan<char>, StringComparison)`

## reusable Utf8JsonWriter over IBufferWriter

JsonHubProtocol gets a thread-cached ReusableUtf8JsonWriter, writes directly to an IBufferWriter<byte>, flushes, and returns the wrapper.

- Do: Use ReusableUtf8JsonWriter.Get(IBufferWriter<byte>) around Utf8JsonWriter.Reset(stream) in serializers.
- Why: JSON serialization avoids per-message Utf8JsonWriter allocation and avoids buffering through Stream or string layers.
- Source: `SignalR\common\Protocols.Json\src\Protocol\JsonHubProtocol.cs#L516-L571` (SignalR JSON hub protocol writer)
- Hot path: yes | Complexity: medium
- APIs: `System.Text.Json.Utf8JsonWriter`, `System.Buffers.IBufferWriter<T>`

## thread-static reusable pooled buffer writer

MemoryBufferWriter caches one writer per thread, rents byte segments from ArrayPool, and resets or returns all segments after use.

- Do: Use MemoryBufferWriter.Get/Return and ArrayPool-backed segmented buffers for temporary protocol output.
- Why: High-frequency message serialization can reuse writer objects and pooled byte arrays instead of allocating fresh buffers.
- Source: `SignalR\common\Shared\MemoryBufferWriter.cs#L17-L79, L149-L188` (SignalR shared buffer writer)
- Hot path: yes | Complexity: medium
- APIs: `System.ThreadStaticAttribute`, `System.Buffers.ArrayPool<T>`, `System.Buffers.IBufferWriter<T>`

## stackalloc then ArrayPool fallback for UTF-8 slow lookup

Utf8HashLookup transcodes to UTF-16 into stack memory up to 128 chars and rents char arrays only for larger names.

- Do: Guard stackalloc with a threshold and use ArrayPool<T>.Shared.Rent for larger buffers.
- Why: The rare case-insensitive fallback keeps small lookups allocation-free while bounding stack usage.
- Source: `SignalR\server\Core\src\Internal\Utf8HashLookup.cs#L76-L90` (SignalR hub method dispatch)
- Hot path: either | Complexity: low
- APIs: `System.Span<T>`, `System.Buffers.ArrayPool<T>`, `System.Text.Encoding.UTF8`

## u8 literal and stackalloc buffers for WebSocket accept key

HandshakeHelpers keeps the WebSocket GUID as a UTF-8 literal, validates the request key into a 16-byte stack buffer, hashes a 60-byte stack buffer, and base64-encodes the 20-byte hash.

- Do: Use u8 literals plus stackalloc Span<byte> for fixed-size handshake crypto buffers.
- Why: The handshake avoids allocating intermediate byte arrays for the fixed-size RFC6455 accept-key computation.
- Source: `Middleware\WebSockets\src\HandshakeHelpers.cs#L17-L71` (ASP.NET Core WebSocket handshake)
- Hot path: either | Complexity: low
- APIs: `UTF-8 string literals`, `System.Span<T>`, `System.Security.Cryptography.SHA1.HashData`, `System.Convert.TryFromBase64String`

## raw JSON result as sequence slice

JsonHubProtocol handles RawResult by recording Utf8JsonReader byte offsets, skipping the token, and slicing the original ReadOnlySequence<byte>.

- Do: Use Utf8JsonReader.BytesConsumed and ReadOnlySequence.Slice for raw JSON pass-through values.
- Why: Known raw payloads can be carried forward without deserializing and reserializing the JSON value.
- Source: `SignalR\common\Protocols.Json\src\Protocol\JsonHubProtocol.cs#L814-L825` (SignalR JSON hub protocol binding)
- Hot path: either | Complexity: medium
- APIs: `System.Text.Json.Utf8JsonReader.BytesConsumed`, `System.Buffers.ReadOnlySequence<T>.Slice`

## ref struct owner for pooled written buffers

MemoryBufferWriter detaches completed segments into a readonly ref struct that must be disposed to return buffers to the pool.

- Do: Use readonly ref struct ownership wrappers for short-lived pooled buffer leases that must not escape.
- Why: Stack-only ownership makes pooled buffer lifetime explicit and prevents accidental heap capture.
- Source: `SignalR\common\Shared\MemoryBufferWriter.cs#L356-L402` (SignalR shared buffer writer)
- Hot path: either | Complexity: medium
- APIs: `ref struct`, `System.Buffers.ArrayPool<T>`

## stackalloc random connection id bytes

HttpConnectionManager creates a 16-byte stack buffer, fills it with RandomNumberGenerator, and base64url-encodes it as the connection id.

- Do: Use stackalloc Span<byte> with RandomNumberGenerator.Fill for fixed-size random tokens.
- Why: Connection setup avoids allocating the random byte buffer while still producing a cryptographically random id.
- Source: `SignalR\common\Http.Connections\src\Internal\HttpConnectionManager.cs#L108-L115` (SignalR HTTP connection manager)
- Hot path: cold | Complexity: low
- APIs: `System.Span<T>`, `System.Security.Cryptography.RandomNumberGenerator.Fill`, `Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode`

## negotiate response prebuffered with exact Content-Length

HttpConnectionDispatcher writes negotiation JSON into a MemoryBufferWriter, sets ContentLength from the bytes written, copies to the response, and resets the writer in finally.

- Do: Serialize protocol payloads to an IBufferWriter<byte> buffer when exact Content-Length is required, then CopyToAsync and Reset.
- Why: The response is serialized once to pooled buffers and can send an exact content length without string serialization.
- Source: `SignalR\common\Http.Connections\src\Internal\HttpConnectionDispatcher.cs#L386-L404, L407-L423` (SignalR negotiate endpoint)
- Hot path: cold | Complexity: medium
- APIs: `Microsoft.AspNetCore.Internal.MemoryBufferWriter`, `System.Buffers.IBufferWriter<T>`

## span parser and ValueStringBuilder for deflate negotiation

HandshakeHelpers parses permessage-deflate extension parameters as ReadOnlySpan<char> slices and builds the response with ValueStringBuilder and TryFormat into appended spans.

- Do: Parse header parameters with ReadOnlySpan<char> slicing and build bounded responses with ValueStringBuilder.AppendSpan plus TryFormat.
- Why: Negotiation avoids substring churn and formats small numeric values directly into the response buffer.
- Source: `Middleware\WebSockets\src\HandshakeHelpers.cs#L75-L174, L261-L274` (ASP.NET Core WebSocket compression negotiation)
- Hot path: cold | Complexity: medium
- APIs: `System.ReadOnlySpan<T>`, `Microsoft.AspNetCore.WebUtilities.ValueStringBuilder`, `System.ISpanFormattable.TryFormat`
