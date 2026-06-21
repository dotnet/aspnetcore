# IO and buffers performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Avoid per-call MemoryPool owners for default pipelines

Prefer the default Pipe configuration or ArrayPool-backed buffers when MemoryPool ownership objects add no value.

- Do: Use new Pipe() defaults unless a custom pool is required, and avoid wrapping pooled arrays in extra owner objects in hot paths.
- Instead of: Custom MemoryPool plumbing that allocates IMemoryOwner<byte> objects for every pipe segment without a concrete need.
- Why: The optimized default pipe path bypasses MemoryPool<byte>.Shared owner allocations and uses ArrayPool<byte>.Shared directly.
- Since .NET Core 3.0. Supersedes: System.IO.Pipelines 4.5 default-pool path via MemoryPool<byte>.Shared
- Hot path: yes | Complexity: low
- APIs: `System.IO.Pipelines.Pipe`, `System.IO.Pipelines.PipeOptions`, `System.Buffers.MemoryPool<T>.Shared`, `System.Buffers.ArrayPool<T>.Shared`

## Copy from PipeReader without stream adapters when possible

Use optimized PipeReader.CopyToAsync or native PipeWriter-targeting APIs instead of adapting through Stream when the endpoint is already a pipe.

- Do: Call PipeReader.CopyToAsync(destinationStream) or APIs such as JsonSerializer.SerializeAsync(PipeWriter, value, options, cancellationToken) when available.
- Instead of: Converting PipeReader/PipeWriter to Stream solely because an older overload only accepted Stream.
- Why: It first drains already-buffered pipe data and avoids adapter overhead between streams and pipelines.
- Since .NET 9. Supersedes: Stream adapters for JSON serialization to System.IO.Pipelines before .NET 9
- Hot path: yes | Complexity: low
- APIs: `System.IO.Pipelines.PipeReader.CopyToAsync`, `System.IO.Pipelines.PipeWriter`, `System.Text.Json.JsonSerializer.SerializeAsync`

## Do not assume compression reads fill the requested buffer

Write read loops that process any positive byte count from DeflateStream, GZipStream, and BrotliStream.

- Do: Loop until ReadAsync returns 0 for EOF, processing each positive count immediately and preserving any framing state externally.
- Instead of: Assuming a compression stream read blocks until the caller's whole buffer is filled.
- Why: Modern compression streams may return as soon as decompressed data is available, which improves latency and prevents bidirectional protocol deadlocks.
- Since .NET 6. Supersedes: Depending on pre-.NET 6 DeflateStream behavior that tried to fill the requested buffer
- Hot path: yes | Complexity: low
- APIs: `System.IO.Compression.DeflateStream.ReadAsync`, `System.IO.Compression.GZipStream.ReadAsync`, `System.IO.Compression.BrotliStream.ReadAsync`

## Encode text into existing spans

Use Encoding.GetByteCount plus span-based Encoding.GetBytes to encode directly into the destination buffer.

- Do: Compute the required size with Encoding.UTF8.GetByteCount and call Encoding.UTF8.GetBytes(ReadOnlySpan<char>, Span<byte>).
- Instead of: Encoding.UTF8.GetBytes(string) followed by copying the allocated byte[] into another buffer.
- Why: It avoids temporary byte arrays when encoding strings or spans for native calls, files, pipes, or network writes.
- Since .NET Core 2.1. Supersedes: Array-returning Encoding.GetBytes patterns in hot paths
- Hot path: yes | Complexity: low
- APIs: `System.Text.Encoding.GetByteCount`, `System.Text.Encoding.GetBytes`, `System.Text.UTF8Encoding`

## Prefer Memory-based Stream async overloads

Use ReadAsync(Memory<byte>) and WriteAsync(ReadOnlyMemory<byte>) with reusable buffers for async stream loops.

- Do: Pass Memory<byte> or ReadOnlyMemory<byte> slices to Stream.ReadAsync and Stream.WriteAsync and await the returned ValueTask where applicable.
- Instead of: Repeatedly allocating byte arrays or using older offset/count overloads when Memory slices are already available.
- Why: The Memory overloads pair with pooled buffers and ValueTask-returning implementations, reducing Task and array-slice overhead in hot I/O paths.
- Since .NET Core 2.1. Supersedes: byte[] offset/count-only async loops used before span and memory overloads
- Hot path: yes | Complexity: low
- APIs: `System.IO.Stream.ReadAsync`, `System.IO.Stream.WriteAsync`, `System.Memory<T>`, `System.ReadOnlyMemory<T>`
- Snippet: [code](../snippets/bcl/io.md#prefer-memory-based-stream-async-overloads)

## Use BufferedStream only to batch small operations

Wrap an expensive underlying stream in BufferedStream when callers issue many small reads or writes and cannot batch them themselves.

- Do: Place BufferedStream above streams such as compression streams when small WriteByte or small Write calls are unavoidable, and flush only at protocol boundaries.
- Instead of: Calling Flush after every byte or layering BufferedStream where the underlying stream or caller already buffers effectively.
- Why: BufferedStream coalesces small operations and, in .NET 10, WriteByte no longer forces an expensive flush of the underlying stream.
- Since .NET 10. Supersedes: BufferedStream.WriteByte behavior before .NET 10 that flushed the underlying stream in one case
- Hot path: yes | Complexity: low
- APIs: `System.IO.BufferedStream`, `System.IO.Stream.WriteByte`, `System.IO.Stream.Flush`

## Use FileOptions.Asynchronous for true async file handles

Open FileStream instances intended for scalable async file I/O with FileOptions.Asynchronous or an async FileStream constructor option.

- Do: Construct FileStream or SafeFileHandle with FileOptions.Asynchronous when most operations will be ReadAsync or WriteAsync.
- Instead of: Opening a synchronous file handle and then using ReadAsync or WriteAsync for high-concurrency workloads.
- Why: It avoids async-over-sync worker thread emulation and lets the platform use its optimized asynchronous file I/O path.
- Since .NET 6. Supersedes: Older FileStream async patterns that incurred sync-over-async or async-over-sync overheads
- Hot path: yes | Complexity: low
- APIs: `System.IO.FileOptions.Asynchronous`, `System.IO.FileStream.ReadAsync`, `System.IO.FileStream.WriteAsync`

## Disable FileStream buffering when caller owns buffering

Avoid an extra FileStream buffer when higher layers already batch reads and writes or when RandomAccess is a better fit.

- Do: Use RandomAccess with File.OpenHandle, or create FileStream with a buffer size that disables managed buffering when the caller controls buffer sizes.
- Instead of: Layering FileStream's default buffer under pipelines, compression, or custom pooling when it adds only another copy.
- Why: Removing redundant buffering reduces allocations, copies, and state while preserving throughput when the caller already uses appropriately sized buffers.
- Since .NET 6. Supersedes: Always relying on default FileStream buffering before the .NET 6 rewrite
- Hot path: yes | Complexity: medium
- APIs: `System.IO.FileStream`, `System.IO.File.OpenHandle`, `System.IO.RandomAccess`

## Return pooled buffers promptly and safely

Rent arrays from ArrayPool<T>.Shared for large reusable buffers, but return them promptly and never use a buffer after returning it.

- Do: Rent once for the operation, slice to the actual byte count, return in a finally block, and pass clearArray: true only for sensitive data or reference-containing arrays that need clearing.
- Instead of: Allocating large buffers per operation, keeping rented arrays in idle objects, returning the wrong array, or using a returned array.
- Why: Pooling removes large array allocations, but retained or misused pooled buffers increase working set, corrupt data, or make the pool ineffective.
- Since .NET Core 2.0. Supersedes: Per-call large byte[] and char[] allocation patterns
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.ArrayPool<T>.Shared`, `System.Buffers.ArrayPool<T>.Rent`, `System.Buffers.ArrayPool<T>.Return`
- Snippet: [code](../snippets/bcl/io.md#return-pooled-buffers-promptly-and-safely)

## Use System.IO.Pipelines for producer-consumer byte pipelines

Use PipeReader and PipeWriter for high-throughput producer-consumer code that parses, transforms, or forwards byte sequences.

- Do: Read with PipeReader.ReadAsync, process the ReadOnlySequence<byte>, call AdvanceTo, and write with PipeWriter.GetMemory/GetSpan plus Advance and FlushAsync.
- Instead of: Ad hoc queues of byte arrays or MemoryStream handoffs between producer and consumer stages.
- Why: Pipelines centralize buffer management, reduce allocations, and avoid repeated copying between producer and consumer stages.
- Since .NET Core 2.1. Supersedes: Manual stream-plus-byte-array coordination for hot server pipelines
- Hot path: yes | Complexity: medium
- APIs: `System.IO.Pipelines.Pipe`, `System.IO.Pipelines.PipeReader`, `System.IO.Pipelines.PipeWriter`, `System.Buffers.ReadOnlySequence<T>`

## Use zero-byte reads as readiness probes

For streams that support it, issue zero-byte reads to wait for data before renting or allocating a payload buffer.

- Do: Call ReadAsync with Memory<byte>.Empty on supporting streams, then rent or reuse a real buffer when readiness is signaled.
- Instead of: Keeping a 4 KB or larger buffer pinned to every idle connection while waiting for data.
- Why: This reduces working set for servers with many idle connections because buffers are acquired only when data is actually available.
- Since .NET 6. Supersedes: Always pending reads with full-size buffers for idle connections
- Hot path: yes | Complexity: medium
- APIs: `System.IO.Stream.ReadAsync`, `System.Net.Sockets.NetworkStream`, `System.Net.Security.SslStream`, `System.IO.Compression.DeflateStream`

## Write to IBufferWriter directly

When an API can produce bytes incrementally, target IBufferWriter<byte> so callers provide the destination buffer.

- Do: Use IBufferWriter<byte>.GetSpan or GetMemory, fill the returned buffer, and call Advance with exactly the number of bytes written.
- Instead of: Build a byte[] or MemoryStream first and then copy it to the real destination.
- Why: It lets producers write directly into pooled or pipeline-owned buffers and avoids intermediate byte arrays or MemoryStream growth.
- Since .NET Standard 2.1. Supersedes: Intermediate byte[] serialization buffers before IBufferWriter-based APIs became common
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.IBufferWriter<T>.GetSpan`, `System.Buffers.IBufferWriter<T>.GetMemory`, `System.Buffers.IBufferWriter<T>.Advance`, `System.IO.Pipelines.PipeWriter`
- Snippet: [code](../snippets/bcl/io.md#write-to-ibufferwriter-directly)

## Loop on ReadLineAsync result instead of EndOfStream

In asynchronous text-reading loops, await ReadLineAsync and stop when it returns null rather than checking StreamReader.EndOfStream.

- Do: Use while (await reader.ReadLineAsync(cancellationToken) is string line) { ... }.
- Instead of: while (!reader.EndOfStream) { await reader.ReadLineAsync(); }
- Why: EndOfStream may perform synchronous blocking I/O, which can stall an async method and duplicate work.
- Since .NET 10. Supersedes: EndOfStream-controlled async reader loops now flagged by analyzer CA2024
- Hot path: either | Complexity: low
- APIs: `System.IO.StreamReader.EndOfStream`, `System.IO.TextReader.ReadLineAsync`, `Microsoft.CodeAnalysis.NetAnalyzers.CA2024`

## Prefer built-in whole-file helpers for simple complete payloads

Use File.ReadAllBytesAsync and File.WriteAllTextAsync for simple whole-file operations when the complete payload is already needed.

- Do: Call File.ReadAllBytesAsync, File.WriteAllTextAsync, File.WriteAllText, or File.AppendAllTextAsync for one-shot complete file reads and writes.
- Instead of: Manually constructing FileStream and StreamWriter solely to read or write an entire file in one operation.
- Why: Newer implementations use SafeFileHandle and RandomAccess internally to avoid unnecessary FileStream, StreamWriter, and temporary-buffer overhead.
- Since .NET 7. Supersedes: Manual FileStream/StreamWriter wrappers for whole-file helpers before .NET 7 optimizations
- Hot path: either | Complexity: low
- APIs: `System.IO.File.ReadAllBytesAsync`, `System.IO.File.WriteAllText`, `System.IO.File.WriteAllTextAsync`, `System.IO.File.AppendAllTextAsync`

## Respect cancellation on stream and reader APIs

Thread CancellationToken through async file, pipe, stream, and text-reader operations that may block or become unnecessary.

- Do: Use Stream.ReadAsync(..., cancellationToken), Stream.WriteAsync(..., cancellationToken), TextReader.ReadLineAsync(cancellationToken), and File.ReadLinesAsync(..., cancellationToken).
- Instead of: Async loops that ignore cancellation or use older uncancelable TextReader methods.
- Why: Prompt cancellation releases buffers, handles, threads, and pending operations sooner and newer implementations avoid many historical cancellation gaps.
- Since .NET 7. Supersedes: Uncancelable TextReader.ReadLineAsync and partially cancelable pipe/file async paths in older releases
- Hot path: either | Complexity: low
- APIs: `System.IO.Stream.ReadAsync`, `System.IO.Stream.WriteAsync`, `System.IO.TextReader.ReadLineAsync`, `System.IO.TextReader.ReadToEndAsync`, `System.IO.File.ReadLinesAsync`

## Use Stream ReadExactly and ReadAtLeast helpers

Use Stream.ReadExactly, ReadExactlyAsync, ReadAtLeast, and ReadAtLeastAsync when protocol code requires a minimum or exact number of bytes.

- Do: Call ReadExactlyAsync(Memory<byte>, CancellationToken) for fixed headers and ReadAtLeastAsync(Memory<byte>, minimumBytes, throwOnEndOfStream, CancellationToken) for framed data.
- Instead of: Manual loops that assume one ReadAsync fills the buffer or that repeatedly allocate per iteration.
- Why: The built-in helpers avoid incorrect open-coded loops, avoid unnecessary Task allocations, and read as much as each operation can provide.
- Since .NET 7. Supersedes: Open-coded exact-read loops before .NET 7
- Hot path: either | Complexity: low
- APIs: `System.IO.Stream.ReadExactly`, `System.IO.Stream.ReadExactlyAsync`, `System.IO.Stream.ReadAtLeast`, `System.IO.Stream.ReadAtLeastAsync`

## Use TextWriter.Null to discard text output

Use TextWriter.Null when output should be suppressed without paying formatting and synchronization costs.

- Do: Set Console.SetOut(TextWriter.Null) or pass TextWriter.Null to APIs that optionally write diagnostics.
- Instead of: Writing to a real StreamWriter or synchronized Console writer and discarding the result downstream.
- Why: Newer null-writer implementations override more write methods and Console avoids needless locking when output is TextWriter.Null.
- Since .NET 8. Supersedes: Partial TextWriter.Null overrides and locked Console writes before .NET 8
- Hot path: either | Complexity: low
- APIs: `System.IO.TextWriter.Null`, `System.Console.SetOut`, `System.Console.WriteLine`

## Use UTF-8 literals for constant byte payloads

Represent fixed UTF-8 protocol tokens and headers with C# UTF-8 string literals instead of runtime encoding.

- Do: Use "literal"u8 as ReadOnlySpan<byte> for constant UTF-8 data.
- Instead of: Encoding.UTF8.GetBytes("literal") on every call.
- Why: The compiler embeds the bytes and avoids runtime transcoding and byte[] allocation.
- Since .NET 7. Supersedes: Runtime Encoding.UTF8.GetBytes for fixed literals before C# 11 UTF-8 literals
- Hot path: either | Complexity: low
- APIs: `System.ReadOnlySpan<T>`, `System.Text.Encoding.UTF8`

## Use streaming compression APIs

Use DeflateStream, GZipStream, ZLibStream, and BrotliStream over streams or pipelines instead of buffering complete compressed payloads in memory.

- Do: Wrap the destination or source stream with the compression stream, use ReadAsync and WriteAsync loops, and leave the underlying stream open when composing layers.
- Instead of: Materializing whole compressed or decompressed payloads in byte arrays before forwarding them.
- Why: Streaming allows overlap between I/O and compression work and benefits from newer zlib-ng and Brotli implementations without extra application buffering.
- Since .NET 6. Supersedes: Whole-payload compression buffering when stream composition is possible
- Hot path: either | Complexity: low
- APIs: `System.IO.Compression.DeflateStream`, `System.IO.Compression.GZipStream`, `System.IO.Compression.ZLibStream`, `System.IO.Compression.BrotliStream`

## Use RandomAccess for positioned file I/O

Use System.IO.RandomAccess with File.OpenHandle when code needs offset-based, one-shot, or concurrent file reads and writes rather than a Stream abstraction.

- Do: Open a SafeFileHandle with File.OpenHandle and call RandomAccess.Read, RandomAccess.ReadAsync, RandomAccess.Write, or RandomAccess.WriteAsync with explicit file offsets.
- Instead of: Creating FileStream solely to perform a single positioned read or write or to coordinate concurrent offset operations.
- Why: It avoids FileStream state and buffering overhead and enables parallel operations against the same SafeFileHandle.
- Since .NET 6. Supersedes: FileStream-only positioned I/O patterns before .NET 6
- Hot path: either | Complexity: medium
- APIs: `System.IO.File.OpenHandle`, `System.IO.RandomAccess.Read`, `System.IO.RandomAccess.ReadAsync`, `System.IO.RandomAccess.Write`, `System.IO.RandomAccess.WriteAsync`, `Microsoft.Win32.SafeHandles.SafeFileHandle`
