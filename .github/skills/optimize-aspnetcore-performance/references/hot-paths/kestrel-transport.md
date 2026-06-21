# Kestrel and transport (ASP.NET Core source patterns)

Source-proven performance patterns from this repository, by hot-path component. These are the primary worked examples: when you touch this component, match these patterns. Before adding raw BCL primitives, check [../repo-helpers.md](../repo-helpers.md) for an existing shared helper. Paths are relative to `src`.

## Header loop optimized for same-span CRLF

TryParseHeaders first checks for an empty CRLF line, then searches the unread span for CR or LF and handles same-span CRLF before considering split or multi-span cases.

- Do: Structure parser loops so same-buffer terminators are handled before split-buffer slow paths.
- Why: Branching for the most common layout keeps header parsing on the current span and minimizes SequenceReader movement.
- Source: `Servers\Kestrel\Core\src\Internal\Http\HttpParser.cs#L562-L695` (Kestrel HTTP/1 parser)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SequenceReader<T>`, `ReadOnlySpan<T>.IndexOfAny`

## Path validation stackalloc threshold for HTTP/2 and HTTP/3

Http2Stream and Http3Stream TryValidatePath use SkipLocalsInit and stackalloc byte[256].Slice for short paths, falling back to a heap array for longer paths.

- Do: Use a small constant stack buffer plus Slice for better codegen and a non-stack fallback for oversized inputs.
- Why: Most paths are decoded without heap allocation while preserving a safe cap for stack usage.
- Source: `Servers\Kestrel\Core\src\Internal\Http2\Http2Stream.cs#L410-L452; Servers\Kestrel\Core\src\Internal\Http3\Http3Stream.cs#L1188-L1230` (Kestrel HTTP/2 and HTTP/3 path validation)
- Hot path: yes | Complexity: low
- APIs: `SkipLocalsInitAttribute`, `stackalloc`, `Span<T>.Slice`

## SearchValues for HTTP character validation

HttpCharacters defines SearchValues for authority, host, token, and field characters and exposes IndexOfAnyExcept helpers used by Kestrel parser code.

- Do: Create static readonly SearchValues<T> once and validate spans with IndexOfAnyExcept or IndexOfAny.
- Why: Reusable SearchValues enables optimized set membership checks instead of handwritten validation loops.
- Source: `Shared\ServerInfrastructure\HttpCharacters.cs#L22-L54; Servers\Kestrel\Core\src\Internal\Http\HttpParser.cs#L522-L536` (Kestrel HTTP character validation)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SearchValues`, `ReadOnlySpan<T>.IndexOfAnyExcept`

## SequenceReader delimiter parsing for request lines

HttpParser.TryParseRequestLine reads up to LF or NUL from a SequenceReader and parses the request line as a ReadOnlySpan<byte>.

- Do: Use SequenceReader<byte>.TryReadToAny with a ReadOnlySpan<byte> delimiter set, then parse the returned span directly.
- Why: The common request-line path stays over pipeline-owned memory and avoids materializing strings or arrays before dispatching to the handler.
- Source: `Servers\Kestrel\Core\src\Internal\Http\HttpParser.cs#L387-L519` (Kestrel HTTP/1 parser)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SequenceReader<T>`, `ReadOnlySpan<T>`

## Single-span header parsing with direct whitespace trimming

TryTakeSingleHeader locates the colon with IndexOfAny, does unsigned bounds checks, and trims typical leading/trailing whitespace with fast single-byte checks before looping.

- Do: Use ReadOnlySpan<byte>.IndexOfAny plus unsigned range checks and fast common-case tests before entering slower loops.
- Why: Most headers are parsed with a few span reads and slices, avoiding string creation until header storage needs it.
- Source: `Servers\Kestrel\Core\src\Internal\Http\HttpParser.cs#L242-L333` (Kestrel HTTP/1 parser)
- Hot path: yes | Complexity: low
- APIs: `ReadOnlySpan<T>.IndexOfAny`, `ReadOnlySpan<T>.Slice`

## SkipLocalsInit with bounded stack parsing

HttpRequestHeaders.AppendContentLengthCustomEncoding uses SkipLocalsInit and a stackalloc char[20] buffer sized for long.MaxValue before parsing Content-Length.

- Do: Apply [SkipLocalsInit] only with fully-written, fixed-size stackalloc buffers and explicit length validation.
- Why: The method avoids local zeroing and heap allocation while keeping the stack buffer bounded by the maximum decimal length.
- Source: `Servers\Kestrel\Core\src\Internal\Http\HttpRequestHeaders.cs#L93-L133` (Kestrel Content-Length parser)
- Hot path: yes | Complexity: low
- APIs: `System.Runtime.CompilerServices.SkipLocalsInitAttribute`, `stackalloc`, `long.TryParse`

## Socket send path preserves ReadOnlySequence shape

SocketSender sends a single-segment ReadOnlySequence directly and only builds a BufferList for multi-segment sends.

- Do: Branch on ReadOnlySequence<T>.IsSingleSegment and use SocketAsyncEventArgs.BufferList only for multi-segment data.
- Why: The common single-buffer write avoids list creation and segment enumeration while multi-buffer writes still avoid coalescing copies.
- Source: `Servers\Kestrel\Transport.Sockets\src\Internal\SocketSender.cs#L20-L94` (Kestrel socket transport send loop)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.ReadOnlySequence<T>`, `SocketAsyncEventArgs.BufferList`, `MemoryMarshal.AsMemory`

## String creation directly from byte spans

HttpUtilities.GetHeaderName and StringUtilities.GetAsciiString use string.Create with Ascii.ToUtf16 to populate the target string directly from bytes.

- Do: Use string.Create(length, sourceSpan, static callback) with Ascii.ToUtf16 for ASCII byte-to-string conversion.
- Why: Direct string construction avoids temporary char arrays and performs validation while filling the final string.
- Source: `Servers\Kestrel\Core\src\Internal\Infrastructure\HttpUtilities.cs#L83-L106; Shared\ServerInfrastructure\StringUtilities.cs#L18-L65` (Kestrel header materialization)
- Hot path: yes | Complexity: low
- APIs: `string.Create`, `System.Text.Ascii`

## Variable-length integer fast path over unread span

VariableLengthIntegerHelper.TryRead first parses from SequenceReader.UnreadSpan and advances the reader, using a stackalloc byte[8] copy only for split-buffer integers.

- Do: Implement a span-based parser first, then wrap SequenceReader with a fast UnreadSpan path and a tiny stackalloc slow path.
- Why: QUIC variable-length integers are usually contiguous, so parsing avoids temporary storage and only pays copy cost across segment boundaries.
- Source: `Shared\runtime\Http3\Helpers\VariableLengthIntegerHelper.cs#L40-L129` (HTTP/3 variable-length integer parser)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SequenceReader<T>`, `System.Buffers.Binary.BinaryPrimitives`, `stackalloc`

## Header value reuse across requests

HttpRequestHeaders.ClearFast records previous known-header bits and OnHeadersComplete clears only headers not reused or overwritten.

- Do: Track known slots with bit masks and clear only stale slots when object instances are reused.
- Why: Reusing known header storage reduces per-request string and collection churn on persistent connections.
- Source: `Servers\Kestrel\Core\src\Internal\Http\HttpRequestHeaders.cs#L30-L81` (Kestrel request headers)
- Hot path: yes | Complexity: medium
- APIs: `bit masks`, `StringValues`

## Pooled SocketAsyncEventArgs senders

SocketConnection rents a SocketSender for each non-empty output buffer and SocketSenderPool returns reset instances to a bounded ConcurrentQueue.

- Do: Pool reusable async socket operation objects with a soft maximum and reset buffers before enqueueing.
- Why: Pooling SocketAsyncEventArgs amortizes native async socket setup costs without allowing unbounded retained senders.
- Source: `Servers\Kestrel\Transport.Sockets\src\Internal\SocketConnection.cs#L266-L344; Servers\Kestrel\Transport.Sockets\src\Internal\SocketSenderPool.cs#L9-L60` (Kestrel socket transport send pooling)
- Hot path: yes | Complexity: medium
- APIs: `SocketAsyncEventArgs`, `ConcurrentQueue<T>`, `Interlocked`

## QPACK decoder pooled string buffers

QPackDecoder rents string, header-name, and header-value buffers from ArrayPool and returns them on Dispose, growing with copy-on-rent only when needed.

- Do: Centralize buffer growth in EnsureStringCapacity and return every rented buffer during disposal.
- Why: Reusable pooled buffers avoid repeated large byte[] allocations during HTTP/3 header decoding.
- Source: `Shared\runtime\Http3\QPack\QPackDecoder.cs#L121-L155; Shared\runtime\Http3\QPack\QPackDecoder.cs#L674-L691` (HTTP/3 QPACK decoder)
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.ArrayPool<T>`, `IDisposable`

## Socket receive waits before buffer allocation

SocketConnection.DoReceive optionally calls SocketReceiver.WaitForDataAsync with an empty buffer before taking memory from the PipeWriter.

- Do: Use a zero-byte receive or readiness wait before calling PipeWriter.GetMemory for idle-prone sockets.
- Why: Idle connections do not pin or reserve transport buffers until data is actually available.
- Source: `Servers\Kestrel\Transport.Sockets\src\Internal\SocketConnection.cs#L133-L183; Servers\Kestrel\Transport.Sockets\src\Internal\SocketReceiver.cs#L15-L47` (Kestrel socket transport receive loop)
- Hot path: yes | Complexity: medium
- APIs: `System.IO.Pipelines.PipeWriter`, `SocketAsyncEventArgs`, `MemoryPool<T>`

## Custom ValueTaskSource for socket operations

SocketAwaitableEventArgs implements IValueTaskSource<SocketOperationResult> and suppresses execution-context flow to provide awaitable socket operations without Task allocation.

- Do: Implement IValueTaskSource for reusable async operation objects when you control the await pattern and reset lifecycle.
- Why: The socket transport avoids per-operation Task objects and scheduler captures on every receive or send completion.
- Source: `Servers\Kestrel\Transport.Sockets\src\Internal\SocketAwaitableEventArgs.cs#L10-L81; Servers\Kestrel\Transport.Sockets\src\Internal\SocketOperationResult.cs#L9-L29` (Kestrel socket transport async operations)
- Hot path: yes | Complexity: high
- APIs: `System.Threading.Tasks.Sources.IValueTaskSource<T>`, `ValueTask<T>`, `SocketAsyncEventArgs`

## Dedicated IOQueue scheduler with batched drain

IOQueue enqueues readonly Work structs, schedules one ThreadPool work item with UnsafeQueueUserWorkItem, drains the queue in a loop, and uses barriers to avoid missed work.

- Do: Build PipeScheduler implementations that drain queued work batches and use Interlocked plus memory barriers for race-free rescheduling.
- Why: Batching queued callbacks reduces scheduler overhead and caps transport callback parallelism with a processor-count heuristic.
- Source: `Servers\Kestrel\Transport.Sockets\src\Internal\IOQueue.cs#L11-L95` (Kestrel socket transport scheduler)
- Hot path: yes | Complexity: high
- APIs: `System.IO.Pipelines.PipeScheduler`, `IThreadPoolWorkItem`, `ThreadPool.UnsafeQueueUserWorkItem`

## HPACK decoder state machine over ReadOnlySequence segments

HPackDecoder.Decode iterates ReadOnlySequence segments and feeds each Span into a compact state machine that continues across segment boundaries.

- Do: Expose both ReadOnlySequence<byte> and ReadOnlySpan<byte> decode paths and keep cross-segment state in fields.
- Why: Header blocks can be decoded from pipeline buffers without concatenating the whole block first.
- Source: `Shared\runtime\Http2\Hpack\HPackDecoder.cs#L126-L202` (HTTP/2 HPACK decoder)
- Hot path: yes | Complexity: high
- APIs: `System.Buffers.ReadOnlySequence<T>`, `ReadOnlySpan<T>`

## HPACK zero-copy string fast path

HPackDecoder records header name and value ranges into the current data span when the whole un-Huffman string is present, copying only if the range must outlive the segment.

- Do: Store span ranges for same-buffer data and defer copying until the source buffer is about to be replaced.
- Why: Literal header names and values avoid temporary buffer copies in the common non-Huffman contiguous case.
- Source: `Shared\runtime\Http2\Hpack\HPackDecoder.cs#L190-L201; Shared\runtime\Http2\Hpack\HPackDecoder.cs#L422-L489` (HTTP/2 HPACK decoder)
- Hot path: yes | Complexity: high
- APIs: `ReadOnlySpan<T>.Slice`, `Span<T>.CopyTo`

## In-place request target parsing over span

HttpParser scans the request target with IndexOfAny for space, question mark, and percent and passes a mutable span view of the start line for path normalization.

- Do: Use ReadOnlySpan<byte>.IndexOfAny for delimiters and MemoryMarshal.CreateSpan only when an in-place byte transformation is required.
- Why: Delimiter scanning over the original bytes finds path/query boundaries and encoded paths without allocating intermediate strings.
- Source: `Servers\Kestrel\Core\src\Internal\Http\HttpParser.cs#L433-L518` (Kestrel HTTP/1 parser)
- Hot path: yes | Complexity: high
- APIs: `ReadOnlySpan<T>.IndexOfAny`, `System.Runtime.InteropServices.MemoryMarshal.CreateSpan`

## Known HTTP method lookup with raw little-endian integer reads

HttpUtilities.GetKnownMethod checks GET with a UInt32 read and other methods with a UInt64 read, mask, and small perfect-hash-style table.

- Do: Use BinaryPrimitives.ReadUInt32LittleEndian/ReadUInt64LittleEndian with masks for fixed ASCII tokens.
- Why: Comparing packed bytes avoids per-character branching and string comparisons in the request-line hot path.
- Source: `Servers\Kestrel\Core\src\Internal\Infrastructure\HttpUtilities.cs#L195-L243; Servers\Kestrel\Core\src\Internal\Infrastructure\HttpUtilities.Generated.cs#L15-L68` (Kestrel HTTP method parser)
- Hot path: yes | Complexity: high
- APIs: `System.Buffers.Binary.BinaryPrimitives`, `MethodImplOptions.AggressiveInlining`

## Known HTTP version lookup with UInt64 comparisons

HttpUtilities.GetKnownVersion reads eight bytes and compares them to precomputed constants for HTTP/1.1 and HTTP/1.0.

- Do: Precompute ASCII protocol constants and compare with BinaryPrimitives.TryReadUInt64LittleEndian.
- Why: The parser validates the version with one integer load and two comparisons instead of decoding or allocating a string.
- Source: `Servers\Kestrel\Core\src\Internal\Infrastructure\HttpUtilities.cs#L370-L423` (Kestrel HTTP version parser)
- Hot path: yes | Complexity: high
- APIs: `System.Buffers.Binary.BinaryPrimitives`, `MethodImplOptions.AggressiveInlining`

## QPACK zero-copy string fast path

QPackDecoder stores name and value ranges for contiguous, non-Huffman strings and passes slices directly to the header handler.

- Do: Use nullable range fields over the current span and choose between pooled buffers and data.Slice in ProcessHeaderValue.
- Why: The decoder avoids copying header bytes when compressed headers already reside contiguously in the current span.
- Source: `Shared\runtime\Http3\QPack\QPackDecoder.cs#L284-L320; Shared\runtime\Http3\QPack\QPackDecoder.cs#L370-L405; Shared\runtime\Http3\QPack\QPackDecoder.cs#L591-L615` (HTTP/3 QPACK decoder)
- Hot path: yes | Complexity: high
- APIs: `ReadOnlySpan<T>.Slice`, `System.Buffers.ReadOnlySequence<T>`

## Multi-segment header fallback with stackalloc and ArrayPool

TryParseMultiSpanHeader scans ReadOnlySequence segments for CR/LF, stackallocs 256 bytes for short headers, rents for longer ones, copies once, and returns the rented array.

- Do: Prefer a small stackalloc buffer guarded by a length threshold and fall back to ArrayPool<byte>.Shared.Rent/Return.
- Why: The common segmented-header fallback avoids heap allocation for small headers while still supporting large cross-segment headers safely.
- Source: `Servers\Kestrel\Core\src\Internal\Http\HttpParser.cs#L113-L240` (Kestrel HTTP/1 parser)
- Hot path: either | Complexity: low
- APIs: `System.Buffers.ReadOnlySequence<T>`, `System.Buffers.ArrayPool<T>`, `stackalloc`

## Small stack buffers for HPACK and QPACK encoders

HPackEncoder and QPackEncoder use stackalloc buffers for compact header encodings and allocate only when values exceed bounded thresholds.

- Do: Encode into a caller-provided Span<byte>, and for ToArray helpers use stackalloc for small maximum sizes with an allocation fallback.
- Why: Most static and short literal header encodings avoid heap allocation while preserving a correctness fallback for long values.
- Source: `Shared\runtime\Http2\Hpack\HPackEncoder.cs#L595-L640; Shared\runtime\Http3\QPack\QPackEncoder.cs#L37-L107; Shared\runtime\Http3\QPack\QPackEncoder.cs#L162-L180` (HPACK and QPACK encoders)
- Hot path: either | Complexity: low
- APIs: `stackalloc`, `Span<T>`, `Encoding.GetBytes`
