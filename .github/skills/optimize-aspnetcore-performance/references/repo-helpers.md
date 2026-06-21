# Shared performance helpers in this repo

This repo keeps a set of performance helpers in `src/Shared`, compiled into projects as shared source (`<Compile Include>`), not shipped as public API. Use this catalog to decide between a BCL type and a repo helper.

## The rule

Prefer the BCL abstraction by default. Use a shared helper only for the specific benefit it documents (pooling, ref-struct stack storage, or a multi-target-framework polyfill). When both a BCL type and a custom helper exist for the same job, the reason for the custom one must be explicit in code; if the BCL has since gained the same benefit, switch to the BCL type and remove the custom use. Many helpers below are polyfills that already forward to the BCL API on modern target frameworks, so calling the helper is correct across every TFM the project targets.

## Polyfills (forward to the BCL on modern TFMs)

Use these freely. They call the real BCL API where it exists and provide it where it does not, so multi-targeted projects stay correct.

### ArgumentNullThrowHelper

A shared null and null-or-empty validation helper that forwards to BCL throw helpers on modern target frameworks.

- BCL: ArgumentNullException.ThrowIfNull and ArgumentException.ThrowIfNullOrEmpty
- Use it because: It preserves the modern CallerArgumentExpression and NotNull validation APIs for older targets, using local throws under !NET7_0_OR_GREATER or NETSTANDARD or NETFRAMEWORK and forwarding to the BCL otherwise.
- When to use: Use the BCL APIs directly in code that only targets modern TFMs; use this shared helper only in shared-source files that must also compile for older target frameworks.
- Source: `Shared\ThrowHelpers\ArgumentNullThrowHelper.cs`

### ArgumentOutOfRangeThrowHelper

A shared range validation helper that polyfills the static ArgumentOutOfRangeException.ThrowIf* APIs on older TFMs.

- BCL: ArgumentOutOfRangeException.ThrowIfZero, ThrowIfNegative, ThrowIfNegativeOrZero, ThrowIfGreaterThan, ThrowIfGreaterThanOrEqual, ThrowIfLessThan, and ThrowIfLessThanOrEqual
- Use it because: The file uses local DoesNotReturn throw methods under !NET7_0_OR_GREATER and calls the BCL ThrowIf* APIs under NET7_0_OR_GREATER, giving shared source one API surface across TFMs.
- When to use: Use the BCL ArgumentOutOfRangeException helpers directly when all targets support them; use this helper for shared source with pre-NET7 targets.
- Source: `Shared\ThrowHelpers\ArgumentOutOfRangeThrowHelper.cs`

### ArgumentThrowHelper

A shared string argument validation helper for null, empty, and whitespace checks with modern BCL forwarding where available.

- BCL: ArgumentException.ThrowIfNullOrEmpty and ArgumentException.ThrowIfNullOrWhiteSpace
- Use it because: It polyfills ThrowIfNullOrEmpty for pre-NET7 and ThrowIfNullOrWhiteSpace for pre-NET8, while forwarding to the BCL on newer TFMs and preserving caller argument names and NotNull annotations.
- When to use: Use the BCL ArgumentException helpers directly on modern-only code; use this helper only where shared source must build against older TFMs.
- Source: `Shared\ThrowHelpers\ArgumentThrowHelper.cs`

### HashCode

An internal mutable xxHash32-based hash combiner with Combine overloads and incremental Add/ToHashCode APIs.

- BCL: System.HashCode
- Use it because: This is a source polyfill derived from Roslyn analyzers and xxHash32; comments explain bit diffusion for limited input hash spaces, lazy xxHash state initialization because structs have no default constructor, and why GetHashCode and Equals are intentionally disallowed.
- When to use: Use System.HashCode in modern-only code; use this helper only where shared source must compile for targets that lack System.HashCode or where this internal type is already the shared-source compatibility layer.
- Source: `Shared\HashCode.cs`

### ObjectDisposedThrowHelper

A shared object-disposed validation helper that forwards to ObjectDisposedException.ThrowIf on modern TFMs.

- BCL: ObjectDisposedException.ThrowIf(bool, object) and ObjectDisposedException.ThrowIf(bool, Type)
- Use it because: It implements the ThrowIf overloads locally under !NET7_0_OR_GREATER and forwards to ObjectDisposedException.ThrowIf under NET7_0_OR_GREATER, preserving DoesNotReturnIf annotations for shared source.
- When to use: Use ObjectDisposedException.ThrowIf directly in modern-only code; use this helper when shared source must compile for older target frameworks.
- Source: `Shared\ThrowHelpers\ObjectDisposedThrowHelper.cs`

### ValueStopwatch

A lightweight value-type stopwatch that stores a start timestamp and computes elapsed time without allocating a Stopwatch object.

- BCL: System.Diagnostics.Stopwatch and Stopwatch.GetElapsedTime(long, long)
- Use it because: It avoids Stopwatch object allocation by storing only the start timestamp; for older TFMs it computes ticks using Stopwatch.Frequency, and under NET7_0_OR_GREATER it forwards elapsed-time calculation to Stopwatch.GetElapsedTime.
- When to use: Use Stopwatch.GetElapsedTime directly in modern-only code, especially when only timestamps are needed; use ValueStopwatch where shared code needs a small active/default guard and older-TFM compatibility.
- Source: `Shared\ValueStopwatch\ValueStopwatch.cs`

## Custom helpers with a specific benefit

Each of these beats the closest BCL type for a documented reason. Use them on the hot paths where that benefit matters; justify any new use in code, and switch to the BCL type if it catches up.

### BufferSegment

A pooled ReadOnlySequenceSegment<byte> node that can own memory from IMemoryOwner<byte> or ArrayPool<byte> and reset it for reuse.

- BCL: System.Buffers.ReadOnlySequenceSegment<byte>, but it is abstract and does not manage pooled ownership
- Why custom: Copied from System.IO.Pipelines internals; comments describe linked-list active memory spans and note that the order of field clears in ResetMemory is significant for performance per a corefx PR.
- When to use: Use this helper for pipeline-like shared buffering internals that need pooled segment ownership and reuse; use ReadOnlySequence<T> and public sequence APIs for consumers.
- Switch to BCL if: Switch if the BCL exposes a public reusable pooled byte sequence segment with equivalent ownership and reset semantics.
- Hot path: yes | Complexity: medium
- Key members: `End`, `NextSegment`, `SetOwnedMemory(IMemoryOwner<byte>)`, `SetOwnedMemory(byte[])`, `ResetMemory`, `AvailableMemory`, `Length`, `WritableBytes`, `SetNext`, `GetLength`
- Source: `Shared\Buffers\BufferSegment.cs`

### BufferSegmentStack

A small stack specialized for BufferSegment references with a value-type wrapper to avoid array covariance checks.

- BCL: System.Collections.Generic.Stack<BufferSegment>
- Why custom: Copied from System.IO.Pipelines internals; the nested wrapper remarks say it bypasses CLR covariant checks when writing to arrays and that this was recognized as a perf win in ETL traces for JIT_Stelem_Ref and ArrayStoreCheck frames.
- When to use: Use this helper only in buffer segment pooling hot paths where ETW or benchmarks show Stack<T> or array covariance checks matter; use Stack<T> for ordinary stacks.
- Switch to BCL if: Switch if the BCL Stack<T> or array writes remove the covariance-check cost, or if measurements no longer justify the custom stack.
- Hot path: yes | Complexity: medium
- Key members: `BufferSegmentStack(int)`, `Count`, `TryPop`, `Push`
- Source: `Shared\Buffers\BufferSegmentStack.cs`

### BufferWriter<T>

A ref struct wrapper around an IBufferWriter<byte> that caches the current Span<byte> and batches Advance calls until Commit.

- BCL: System.Buffers.IBufferWriter<byte> extension methods such as MemoryExtensions.Write, but no BCL cached ref struct wrapper
- Why custom: The XML doc calls it 'A fast access struct that wraps IBufferWriter<T>'; it stores the last GetSpan result, tracks uncommitted bytes, and only calls the underlying Advance on Commit to reduce repeated interface calls and buffer acquisition in write loops.
- When to use: Use this helper inside tight server serialization loops that write bytes to IBufferWriter<byte>; use IBufferWriter<byte> directly for simpler or cold code.
- Switch to BCL if: Switch if the BCL provides an equivalent fast writer facade that caches spans and batches Advance without losing safety.
- Hot path: yes | Complexity: medium
- Key members: `BufferWriter(T)`, `Span`, `BytesCommitted`, `Advance`, `Commit`, `Write`, `Ensure`
- Source: `Shared\ServerInfrastructure\BufferWriter.cs`

### CancellationTokenSourcePool

A bounded concurrent pool of reusable CancellationTokenSource instances whose nested pooled source returns itself to the pool on Dispose.

- BCL: System.Threading.CancellationTokenSource with TryReset, but no built-in CTS pool
- Why custom: It caps the queue at 1024, rents from ConcurrentQueue, and Return only reuses a source if TryReset succeeds; the nested XML doc says Dispose will return the CTS to the pool.
- When to use: Use this helper when hot server code creates many short-lived CTS instances and can obey pooled lifetime rules; use plain CancellationTokenSource for normal ownership, linked sources, or complex cancellation lifetimes.
- Switch to BCL if: Switch if the BCL adds a safe bounded CTS pool or another reuse API with equivalent allocation savings and disposal semantics.
- Hot path: yes | Complexity: medium
- Key members: `Rent`, `PooledCancellationTokenSource.Dispose`, `CancellationTokenSource.TryReset`
- Source: `Shared\CancellationTokenSourcePool.cs`
- Snippet: [code](snippets/repo-helpers.md#cancellationtokensourcepool)

### HttpCharacters

A set of precomputed SearchValues tables and span helpers for validating HTTP authority, host, token, and field-value characters.

- BCL: System.Buffers.SearchValues<T> plus MemoryExtensions.IndexOfAny/IndexOfAnyExcept
- Why custom: The custom part is ASP.NET Core's HTTP grammar and Http.Sys compatibility tables; comments cite RFC authority, token, and field-value rules and note exclusions that match Http.Sys.
- When to use: Use this helper for shared ASP.NET Core HTTP character validation; use raw SearchValues<T> only for unrelated grammars or when defining a new validated character set.
- Switch to BCL if: Switch only if the BCL provides an HTTP-specific validator with the same RFC and Http.Sys compatibility behavior; otherwise keep custom use justified by protocol rules.
- Hot path: yes | Complexity: low
- Key members: `ContainsInvalidAuthorityChar`, `IndexOfInvalidHostChar`, `IndexOfInvalidTokenChar(char)`, `IndexOfInvalidTokenChar(byte)`, `IndexOfInvalidFieldValueChar`, `IndexOfInvalidFieldValueCharExtended`
- Source: `Shared\ServerInfrastructure\HttpCharacters.cs`

### ManualResetValueTaskSource<T>

A reusable class wrapper around ManualResetValueTaskSourceCore<T> that implements both generic and non-generic IValueTaskSource interfaces.

- BCL: System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<T>, but that is a mutable struct helper rather than an IValueTaskSource object
- Why custom: It packages ManualResetValueTaskSourceCore<T> into an object with Reset, SetResult, SetException, Version, status, continuation, and TrySetResult helpers so async operations can reuse a ValueTask source without allocating a Task per operation.
- When to use: Use this helper for reusable async operation sources in hot server infrastructure; use TaskCompletionSource<T> or async methods for cold or simple code where allocation is acceptable.
- Switch to BCL if: Switch if the BCL provides a reusable ManualResetValueTaskSource<T> class with the same interfaces and TrySetResult behavior.
- Hot path: yes | Complexity: medium
- Key members: `RunContinuationsAsynchronously`, `Version`, `Reset`, `SetResult`, `SetException`, `GetResult`, `GetStatus`, `OnCompleted`, `TrySetResult`
- Source: `Shared\ServerInfrastructure\ManualResetValueTaskSource.cs`

### MemoryPoolBlock

An IMemoryOwner<byte> wrapper for one pinned byte array block that returns itself to its PinnedBlockMemoryPool on Dispose.

- BCL: System.Buffers.IMemoryOwner<byte> and MemoryPool<byte>.Rent owners
- Why custom: The XML doc says it 'Wraps an array allocated in the pinned object heap in a reusable block of managed memory'; it uses GC.AllocateUninitializedArray<byte>(pinned: true) and MemoryMarshal.CreateFromPinnedArray to expose pinned Memory<byte>.
- When to use: Use only as the block type owned by PinnedBlockMemoryPool; use the IMemoryOwner<byte> returned by MemoryPool<byte>.Rent for normal pooled memory ownership.
- Switch to BCL if: Switch if the BCL exposes reusable pinned memory owners with return-to-pool semantics matching PinnedBlockMemoryPool.
- Hot path: yes | Complexity: medium
- Key members: `Pool`, `Memory`, `Dispose`
- Source: `Shared\Buffers.MemoryPool\MemoryPoolBlock.cs`

### MemoryPoolThrowHelper

A throw helper for memory pool argument, disposal, pin-count, double-dispose, and diagnostics errors.

- BCL: standard exception constructors and BCL throw helper APIs for some basic argument/disposed cases
- Why custom: It centralizes no-inline exception construction for memory-pool-specific errors and builds diagnostic messages that include block lease stack traces, such as 'Block leased from:'.
- When to use: Use this helper inside the shared memory pool implementation where the exception text and diagnostics are pool-specific; use BCL throw helpers for ordinary argument or disposed checks outside this subsystem.
- Switch to BCL if: Switch basic cases to BCL throw helpers if all targets support them and no memory-pool diagnostic context is needed; keep custom diagnostic errors justified by pool-specific state.
- Hot path: cold | Complexity: medium
- Key members: `ThrowArgumentOutOfRangeException`, `ThrowInvalidOperationException_PinCountZero`, `ThrowInvalidOperationException_ReturningPinnedBlock`, `ThrowInvalidOperationException_DoubleDispose`, `ThrowInvalidOperationException_BlockDoubleDispose`, `ThrowInvalidOperationException_BlockReturnedToDisposedPool`, `ThrowInvalidOperationException_BlockIsBackedByDisposedSlab`, `ThrowInvalidOperationException_DisposingPoolWithActiveBlocks`, `ThrowInvalidOperationException_BlocksWereNotReturnedInTime`, `ThrowArgumentOutOfRangeException_BufferRequestTooLarge`, `ThrowObjectDisposedException`
- Source: `Shared\Buffers.MemoryPool\MemoryPoolThrowHelper.cs`

### PinnedBlockMemoryPool

A MemoryPool<byte> implementation that rents reusable 4096 byte pinned blocks and adaptively evicts idle pooled blocks.

- BCL: System.Buffers.MemoryPool<byte>.Shared and MemoryPool<byte>
- Why custom: Its docs say it is 'Used to allocate and distribute re-usable blocks of memory' and that block size 4096 is chosen because most operating systems use 4k pages; it adds pinned object heap allocation, owner metrics, disposal callbacks, and adaptive eviction comments describe reducing memory bloat while avoiding pool trashing.
- When to use: Use this helper for ASP.NET Core server transport code that needs reusable pinned 4 KB blocks, metrics, ownership, and eviction; use MemoryPool<byte>.Shared for general pooled memory without these server-specific behaviors.
- Switch to BCL if: Switch if the BCL MemoryPool<byte> exposes pinned block pooling, metrics hooks, disposal tracking, and adaptive eviction with equivalent behavior.
- Hot path: yes | Complexity: high
- Key members: `BlockSize`, `DefaultEvictionDelay`, `MaxBufferSize`, `Rent`, `Return`, `TryScheduleEviction`, `PerformEviction`, `OnPoolDisposed`, `BlockCount`
- Source: `Shared\Buffers.MemoryPool\PinnedBlockMemoryPool.cs`

### PooledArrayBufferWriter<T>

A disposable IBufferWriter<T> backed by ArrayPool<T> that exposes written span, memory, count, capacity, and free capacity.

- BCL: System.Buffers.ArrayBufferWriter<T>, but the BCL writer owns a growable managed array and is not pooled
- Why custom: Copied from an old System.Text.Json ArrayBufferWriter implementation but changed to rent and return buffers via ArrayPool<T>; Dispose says it 'Returns the rented buffer back to the pool'.
- When to use: Use this helper when pooled backing storage and explicit disposal are required to reduce large or repeated buffer allocations; use ArrayBufferWriter<T> when pooling is unnecessary or lifetime safety is simpler.
- Switch to BCL if: Switch if the BCL provides a pooled ArrayBufferWriter<T> or another disposable pooled IBufferWriter<T> with equivalent semantics.
- Hot path: yes | Complexity: medium
- Key members: `PooledArrayBufferWriter()`, `PooledArrayBufferWriter(int)`, `WrittenSpan`, `WrittenMemory`, `WrittenCount`, `Capacity`, `FreeCapacity`, `Clear`, `Advance`, `GetMemory`, `GetSpan`, `Dispose`
- Source: `Shared\PooledArrayBufferWriter.cs`
- Snippet: [code](snippets/repo-helpers.md#pooledarraybufferwritert)

### RefPooledArrayBufferWriter<T>

A ref struct IBufferWriter<T> that starts with a caller supplied Span<T> and rents from ArrayPool<T> only when it must grow.

- BCL: System.Buffers.ArrayBufferWriter<T>, but that is a class, not stack-only, and is not pooled
- Why custom: The XML doc calls it 'A high-performance struct-based IBufferWriter<byte> implementation that uses ArrayPool for allocations' and says it is 'Designed for zero-allocation scenarios when used with generic methods via `allows ref struct` constraint.'
- When to use: Use this helper for stack-bound generic write paths that can use spans and need zero or pooled allocations; use ArrayBufferWriter<T> when a heap object or Memory<T> support is required.
- Switch to BCL if: Switch if the BCL provides a public ref struct pooled IBufferWriter<T> that supports initial spans and the same allocation profile.
- Hot path: yes | Complexity: medium
- Key members: `RefPooledArrayBufferWriter(Span<T>)`, `WrittenSpan`, `GetSpan`, `Advance`, `Dispose`
- Source: `Shared\Buffers\RefPooledArrayBufferWriter.cs`

### UnmanagedBufferAllocator

An unsafe disposable bump allocator for unmanaged memory blocks with pointer-aligned commits and UTF-8 header string encoding support.

- BCL: System.Runtime.InteropServices.NativeMemory.Alloc/Free, but not a block allocator
- Why custom: The XML doc says it is an 'Allocator that manages blocks of unmanaged memory'; comments say DefaultBlockSize assumes a common page size and accounts for the pointer chain, allocations are uninitialized, large requests get exclusive blocks, and Dispose follows the pointer chain to delete all allocations.
- When to use: Use this helper for short-lived batches of unmanaged allocations that can be freed together; use NativeMemory directly for isolated allocations or where ownership must be explicit per allocation.
- Switch to BCL if: Switch if the BCL provides a block or arena allocator for unmanaged memory with comparable pointer-aligned bump allocation and bulk free semantics.
- Hot path: yes | Complexity: high
- Key members: `DefaultBlockSize`, `UnmanagedBufferAllocator(int)`, `AllocAsPointer<T>`, `AllocAsSpan<T>`, `GetHeaderEncodedBytes`, `Dispose`
- Source: `Shared\Buffers.MemoryPool\UnmanagedBufferAllocator.cs`

### ValueListBuilder<T>

An internal ref struct list builder that starts with a Span<T> and rents from ArrayPool<T> only when more capacity is required.

- BCL: none (no public BCL equivalent); closest public type is System.Collections.Generic.List<T>, but it is heap based
- Why custom: Copied from System.Private.CoreLib with unused members removed; comments require it to grow only when absolutely required because consumers compare the storage reference against the initial span, and it includes a workaround for dotnet/runtime#72004.
- When to use: Use this helper for short-lived hot-path list construction where stack storage and ArrayPool<T> fallback avoid List<T> allocations; use List<T> for normal collections or when data must escape the stack.
- Switch to BCL if: Switch if the BCL exposes a public ValueListBuilder<T>-like stack/span first collection with equivalent pooling semantics.
- Hot path: yes | Complexity: medium
- Key members: `ValueListBuilder(Span<T>)`, `Add`, `AsSpan`, `Dispose`
- Source: `Shared\ValueStringBuilder\ValueListBuilder.cs`

### ValueStringBuilder

An internal ref struct character builder for formatting strings into a caller supplied span and only renting an array when it grows.

- BCL: none (no public BCL equivalent); closest public type is System.Text.StringBuilder, but it is heap based
- Why custom: Copied from dotnet/runtime internal ValueStringBuilder; it uses ref struct storage, stack or caller supplied Span<char>, ArrayPool<char> growth, and Dispose returns rented arrays, avoiding StringBuilder allocations in hot formatting paths.
- When to use: Use this helper inside shared hot-path code when stack or pooled character storage materially avoids allocations; use StringBuilder or string.Create for ordinary code where allocation pressure is not proven.
- Switch to BCL if: Switch if the BCL exposes a public ValueStringBuilder-like API with stack/span first storage and pooled growth; otherwise every custom use should remain limited to measured hot paths.
- Hot path: yes | Complexity: medium
- Key members: `ValueStringBuilder(Span<char>)`, `ValueStringBuilder(int)`, `Append`, `AppendSpan`, `AsSpan`, `TryCopyTo`, `EnsureCapacity`, `ToString`, `Dispose`
- Source: `Shared\ValueStringBuilder\ValueStringBuilder.cs`
- Snippet: [code](snippets/repo-helpers.md#valuestringbuilder)

### ValueTaskExtensions

Extensions that convert ValueTask<FlushResult> to Task or ValueTask while avoiding AsTask allocation when the flush already completed successfully.

- BCL: ValueTask<T>.AsTask and ValueTask constructors
- Why custom: The comments say 'Try to avoid the allocation from AsTask' and 'Signal consumption to the IValueTaskSource'; completed flushes return Task.CompletedTask or default ValueTask after GetAwaiter().GetResult().
- When to use: Use this helper for PipeWriter FlushAsync hot paths where completed ValueTask<FlushResult> is common and allocation avoidance matters; use AsTask directly when completion is uncommon or the result must be preserved.
- Switch to BCL if: Switch if the BCL adds an allocation-free API for erasing a completed ValueTask<T> to Task or ValueTask with equivalent IValueTaskSource consumption semantics.
- Hot path: yes | Complexity: low
- Key members: `GetAsTask`, `GetAsValueTask`
- Source: `Shared\ValueTaskExtensions\ValueTaskExtensions.cs`
