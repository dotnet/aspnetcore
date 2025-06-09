// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.AspNetCore;

/// <summary>
/// Used to allocate and distribute re-usable blocks of memory.
/// </summary>
internal sealed class PinnedBlockMemoryPool : MemoryPool<byte>, IThreadPoolWorkItem
{
    /// <summary>
    /// The size of a block. 4096 is chosen because most operating systems use 4k pages.
    /// </summary>
    private const int _blockSize = 4096;

    // 10 seconds chosen arbitrarily. Trying to avoid running eviction too frequently
    // to avoid trashing if the server gets batches of requests, but want to run often
    // enough to avoid memory bloat if the server is idle for a while.
    // This can be tuned later if needed.
    public static readonly TimeSpan DefaultEvictionDelay = TimeSpan.FromSeconds(10);

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

    private readonly PinnedBlockMemoryPoolMetrics? _metrics;
    private readonly ILogger? _logger;

    private long _currentMemory;
    private long _evictedMemory;
    private DateTimeOffset _nextEviction = DateTime.UtcNow.Add(DefaultEvictionDelay);

    private uint _rentCount;
    private uint _returnCount;

    private readonly object _disposeSync = new object();

    private Action<object?, PinnedBlockMemoryPool>? _onPoolDisposed;
    private object? _onPoolDisposedState;

    /// <summary>
    /// This default value passed in to Rent to use the default value for the pool.
    /// </summary>
    private const int AnySize = -1;

    public PinnedBlockMemoryPool(IMeterFactory? meterFactory = null, ILogger? logger = null)
    {
        _metrics = meterFactory is null ? null : new PinnedBlockMemoryPoolMetrics(meterFactory);
        _logger = logger;
    }

    /// <summary>
    /// Register a callback to call when the pool is being disposed.
    /// </summary>
    public void OnPoolDisposed(Action<object?, PinnedBlockMemoryPool> onPoolDisposed, object? state = null)
    {
        _onPoolDisposed = onPoolDisposed;
        _onPoolDisposedState = state;
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

        Interlocked.Increment(ref _rentCount);

        if (_blocks.TryDequeue(out var block))
        {
            _metrics?.UpdateCurrentMemory(-block.Memory.Length);
            _metrics?.Rent(block.Memory.Length);
            Interlocked.Add(ref _currentMemory, -block.Memory.Length);

            // block successfully taken from the stack - return it
            return block;
        }

        _metrics?.IncrementTotalMemory(BlockSize);
        _metrics?.Rent(BlockSize);

        // We already counted this Rent call above, but since we're now allocating (need more blocks)
        // that means the pool is 'very' active and we probably shouldn't evict blocks, so we count again
        // to reduce the chance of eviction occurring this cycle.
        Interlocked.Increment(ref _rentCount);

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

        Interlocked.Increment(ref _returnCount);

        if (!_isDisposed)
        {
            _metrics?.UpdateCurrentMemory(block.Memory.Length);
            Interlocked.Add(ref _currentMemory, block.Memory.Length);

            _blocks.Enqueue(block);
        }
    }

    public bool TryScheduleEviction(DateTimeOffset now)
    {
        if (now >= _nextEviction)
        {
            _nextEviction = now.Add(DefaultEvictionDelay);
            ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
            return true;
        }

        return false;
    }

    void IThreadPoolWorkItem.Execute()
    {
        try
        {
            PerformEviction();
        }
        catch (Exception ex)
        {
            // Log the exception, but don't let it crash the thread pool
            _logger?.LogError(ex, "Error while performing eviction in PinnedBlockMemoryPool.");
        }
    }

    /// <summary>
    /// Examines the current usage and activity of the memory pool and evicts a calculated number of unused memory blocks.
    /// The eviction policy is adaptive: if the pool is underutilized or idle, more blocks are evicted;
    /// if activity is high, fewer or no blocks are evicted.
    /// Evicted blocks are removed from the pool and their memory is unrooted for garbage collection.
    /// </summary>
    internal void PerformEviction()
    {
        var currentCount = (uint)_blocks.Count;
        var burstAmount = 0u;

        var rentCount = _rentCount;
        var returnCount = _returnCount;
        _rentCount = 0;
        _returnCount = 0;

        // If any activity
        if (rentCount + returnCount > 0)
        {
            // Trending less traffic
            if (returnCount > rentCount)
            {
                // Remove the lower of 1% of the current blocks and 20% of the difference between rented and returned
                burstAmount = Math.Min(currentCount / 100, (returnCount - rentCount) / 5);
            }
            // Traffic staying the same, try removing some blocks since we probably have excess
            else if (returnCount == rentCount)
            {
                // Remove 1% of the current blocks (or at least 1)
                burstAmount = Math.Max(1, currentCount / 100);
            }
            // else trending more traffic so we don't want to evict anything
        }
        // If no activity
        else
        {
            // Remove 5% of the current blocks (or at least 10)
            burstAmount = Math.Max(10, currentCount / 20);
        }

        // Remove from queue and let GC clean the memory up
        while (burstAmount > 0 && _blocks.TryDequeue(out var block))
        {
            _metrics?.UpdateCurrentMemory(-block.Memory.Length);
            _metrics?.EvictBlock(block.Memory.Length);
            Interlocked.Add(ref _currentMemory, -block.Memory.Length);
            Interlocked.Add(ref _evictedMemory, block.Memory.Length);

            burstAmount--;
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
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _onPoolDisposed?.Invoke(_onPoolDisposedState, this);

            if (disposing)
            {
                // Discard blocks in pool
                while (_blocks.TryDequeue(out _))
                {

                }
            }
        }
    }

    // Used for testing
    public int BlockCount() => _blocks.Count;
}
