// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

#nullable enable

namespace System.Buffers;

/// <summary>
/// Used to allocate and distribute re-usable blocks of memory.
/// </summary>
internal sealed class PinnedBlockMemoryPool : MemoryPool<byte>
{
    /// <summary>
    /// The size of a block. 4096 is chosen because most operating systems use 4k pages.
    /// </summary>
    private const int _blockSize = 4096;

    /// <summary>
    /// Max allocation block size for pooled blocks,
    /// larger values can be leased but they will be disposed after use rather than returned to the pool.
    /// </summary>
    public override int MaxBufferSize { get; } = _blockSize;

    /// <summary>
    /// The size of a block. 4096 is chosen because most operating systems use 4k pages.
    /// </summary>
    public static int BlockSize => _blockSize;

    /// <summary>
    /// Thread-safe collection of blocks which are currently in the pool. A slab will pre-allocate all of the block tracking objects
    /// and add them to this collection. When memory is requested it is taken from here first, and when it is returned it is re-added.
    /// </summary>
    private readonly ConcurrentQueue<MemoryPoolBlock> _blocks = new ConcurrentQueue<MemoryPoolBlock>();

    /// <summary>
    /// This is part of implementing the IDisposable pattern.
    /// </summary>
    private bool _isDisposed; // To detect redundant calls

    private readonly long _memoryLimit = 30_000;

    private readonly PinnedBlockMemoryPoolMetrics _metrics;

    private long _evictionDelays; // Total time eviction tasks were delayed (ms)
    private long _evictionDurations; // Total time spent on eviction (ms)
    private long _currentMemory;

    private volatile bool _evictionInProgress;
    private long _lastEvictionStartTimestamp;
    private readonly EvictionWorkItem _evictionWorkItem;

    private readonly object _disposeSync = new object();

    /// <summary>
    /// This default value passed in to Rent to use the default value for the pool.
    /// </summary>
    private const int AnySize = -1;

    public PinnedBlockMemoryPool(IMeterFactory meterFactory)
    {
        _metrics = new(meterFactory);

        _evictionWorkItem = new EvictionWorkItem(this);
        var conserveMemory = Environment.GetEnvironmentVariable("DOTNET_GCConserveMemory")
            ?? Environment.GetEnvironmentVariable("COMPlus_GCConserveMemory")
            ?? "0";

        if (!int.TryParse(conserveMemory, out var conserveSetting))
        {
            conserveSetting = 0;
        }

        conserveSetting = Math.Clamp(conserveSetting, 0, 9);

        // Will be a value between 1 and .1f
        var conserveRatio = 1.0f - (conserveSetting / 10.0f);
        // Adjust memory limit to be between 100% and 10% of original value
        _memoryLimit = (long)(_memoryLimit * conserveRatio);
    }

    public override IMemoryOwner<byte> Rent(int size = AnySize)
    {
        if (size > _blockSize)
        {
            MemoryPoolThrowHelper.ThrowArgumentOutOfRangeException_BufferRequestTooLarge(_blockSize);
        }

        if (_isDisposed)
        {
            MemoryPoolThrowHelper.ThrowObjectDisposedException(MemoryPoolThrowHelper.ExceptionArgument.MemoryPool);
        }

        if (_blocks.TryDequeue(out var block))
        {
            _metrics.UpdateCurrentMemory(-block.Memory.Length);
            Interlocked.Add(ref _currentMemory, -block.Memory.Length);
            _metrics.Rent(block.Memory.Length);

            // block successfully taken from the stack - return it
            return block;
        }

        _metrics.IncrementTotalMemory(BlockSize);
        _metrics.Rent(BlockSize);

        return new MemoryPoolBlock(this, BlockSize);
    }

    /// <summary>
    /// Called to return a block to the pool. Once Return has been called the memory no longer belongs to the caller, and
    /// Very Bad Things will happen if the memory is read of modified subsequently. If a caller fails to call Return and the
    /// block tracking object is garbage collected, the block tracking object's finalizer will automatically re-create and return
    /// a new tracking object into the pool. This will only happen if there is a bug in the server, however it is necessary to avoid
    /// leaving "dead zones" in the slab due to lost block tracking objects.
    /// </summary>
    /// <param name="block">The block to return. It must have been acquired by calling Lease on the same memory pool instance.</param>
    internal void Return(MemoryPoolBlock block)
    {
#if BLOCK_LEASE_TRACKING
            Debug.Assert(block.Pool == this, "Returned block was not leased from this pool");
            Debug.Assert(block.IsLeased, $"Block being returned to pool twice: {block.Leaser}{Environment.NewLine}");
            block.IsLeased = false;
#endif

        if (!_isDisposed)
        {
            _metrics.UpdateCurrentMemory(block.Memory.Length);
            Interlocked.Add(ref _currentMemory, block.Memory.Length);

            _blocks.Enqueue(block);
        }

        TryScheduleEviction();
    }

    private void TryScheduleEviction()
    {
        if (_currentMemory > _memoryLimit && !_evictionInProgress)
        {
            if (Interlocked.CompareExchange(ref _evictionInProgress, true, false) == false)
            {
                _metrics.StartEviction();

                _lastEvictionStartTimestamp = Stopwatch.GetTimestamp();
                ThreadPool.UnsafeQueueUserWorkItem(_evictionWorkItem, preferLocal: false);
            }
        }
    }

    private void PerformEviction()
    {
        var now = Stopwatch.GetTimestamp();

        try
        {
            // Measure delay since the eviction was triggered
            
            var delayMs = Stopwatch.GetElapsedTime(_lastEvictionStartTimestamp).TotalMilliseconds;
            Interlocked.Add(ref _evictionDelays, (long)delayMs);

            long evictedMemoryThisPass = 0;

            // Remove from queue and let GC clean the memory up
            while (_currentMemory > _memoryLimit && _blocks.TryDequeue(out var block))
            {
                Interlocked.Add(ref _currentMemory, -block.Memory.Length);
                _metrics.UpdateCurrentMemory(-block.Memory.Length);
                _metrics.EvictBlock(block.Memory.Length);

                evictedMemoryThisPass += block.Memory.Length;
            }

            Debug.WriteLine($"Evicted {evictedMemoryThisPass} bytes.");
        }
        finally
        {
            Interlocked.Add(ref _evictionDurations, (long)Stopwatch.GetElapsedTime(now).TotalMilliseconds);
            _evictionInProgress = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        lock (_disposeSync)
        {
            _isDisposed = true;

            if (disposing)
            {
                // Discard blocks in pool
                while (_blocks.TryDequeue(out _))
                {

                }
            }
        }
    }

    private sealed class EvictionWorkItem : IThreadPoolWorkItem
    {
        private readonly PinnedBlockMemoryPool _pool;

        public EvictionWorkItem(PinnedBlockMemoryPool pool)
        {
            _pool = pool;
        }

        public void Execute()
        {
            _pool.PerformEviction();
        }
    }
}
