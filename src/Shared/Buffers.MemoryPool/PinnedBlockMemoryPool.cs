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

    private readonly PinnedBlockMemoryPoolMetrics _metrics;

    public long _currentMemory;
    public long _evictedMemory;

    private uint _rentCount;
    private uint _returnCount;

    private readonly object _disposeSync = new object();

    /// <summary>
    /// This default value passed in to Rent to use the default value for the pool.
    /// </summary>
    private const int AnySize = -1;

    public PinnedBlockMemoryPool()
        : this(NoopMeterFactory.Instance)
    {
    }

    public PinnedBlockMemoryPool(IMeterFactory meterFactory)
    {
        _metrics = new(meterFactory);
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

        //Interlocked.Increment(ref _rentCount);
        //++_rentCount;
        ScalableCount(ref _rentCount);

        if (_blocks.TryDequeue(out var block))
        {
            _metrics.UpdateCurrentMemory(-block.Memory.Length);
            _metrics.Rent(block.Memory.Length);
            Interlocked.Add(ref _currentMemory, -block.Memory.Length);

            // block successfully taken from the stack - return it
            return block;
        }

        _metrics.IncrementTotalMemory(BlockSize);
        _metrics.Rent(BlockSize);
        //Interlocked.Increment(ref _rentCount);
        //++_rentCount;
        ScalableCount(ref _rentCount);

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

        //Interlocked.Increment(ref _returnCount);
        //++_returnCount;
        ScalableCount(ref _returnCount);

        if (!_isDisposed)
        {
            _metrics.UpdateCurrentMemory(block.Memory.Length);
            Interlocked.Add(ref _currentMemory, block.Memory.Length);

            _blocks.Enqueue(block);
        }
    }

    public void PerformEviction()
    {
        long evictedMemoryThisPass = 0;
        var currentCount = (uint)_blocks.Count;
        var burstAmount = 0u;
        // If any activity
        if (_rentCount + _returnCount > 0)
        {
            // Trending less traffic
            if (_returnCount > _rentCount)
            {
                burstAmount = Math.Min(currentCount / 100, (_returnCount - _rentCount) / 5);
            }
            // Traffic staying the same, try removing some blocks since we probably have excess
            else if (_returnCount == _rentCount)
            {
                burstAmount = Math.Max(1, currentCount / 100);
            }
        }
        // If no activity
        else
        {
            burstAmount = Math.Max(10, currentCount / 20);
        }
        _rentCount = 0;
        _returnCount = 0;

        // Remove from queue and let GC clean the memory up
        while (burstAmount > 0 && _blocks.TryDequeue(out var block))
        {
            _metrics.UpdateCurrentMemory(-block.Memory.Length);
            _metrics.EvictBlock(block.Memory.Length);
            Interlocked.Add(ref _currentMemory, -block.Memory.Length);
            Interlocked.Add(ref _evictedMemory, block.Memory.Length);

            evictedMemoryThisPass += block.Memory.Length;
            burstAmount--;
        }

        Debug.WriteLine($"Evicted {evictedMemoryThisPass} bytes.");
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

    private sealed class NoopMeterFactory : IMeterFactory
    {
        public static NoopMeterFactory Instance { get; } = new();

        public Meter Create(MeterOptions options) => new Meter(options);

        public void Dispose()
        {
        }
    }

    // https://github.com/dotnet/runtime/blob/db681fb307d754c3746ffb40e0634e4c4e0caa9e/docs/design/features/ScalableApproximateCounting.md
    static void ScalableCount(ref uint counter)
    {
        // Start using random for counting after 2^12 (4096)
        const int threshold = 8;
        uint count = counter;
        uint delta = 1;
#if true
        if (count > 0)
        {
            int logCount = 31 - (int)uint.LeadingZeroCount(count);

            if (logCount >= threshold)
            {
                delta = 1u << (logCount - (threshold - 1));
                uint rand = (uint)Random.Shared.Next();
                bool update = (rand & (delta - 1)) == 0;
                if (!update)
                {
                    return;
                }
            }
        }
#endif

        Interlocked.Add(ref counter, delta);
    }
}
