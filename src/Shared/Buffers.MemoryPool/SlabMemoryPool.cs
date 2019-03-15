// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace System.Buffers
{
    /// <summary>
    /// Used to allocate and distribute re-usable blocks of memory.
    /// </summary>
    internal class SlabMemoryPool : MemoryPool<byte>
    {
        /// <summary>
        /// The size of a block. 4096 is chosen because most operating systems use 4k pages.
        /// </summary>
        private const int _blockSize = 4096;

        /// <summary>
        /// Allocating 32 contiguous blocks per slab makes the slab size 128k. This is larger than the 85k size which will place the memory
        /// in the large object heap. This means the GC will not try to relocate this array, so the fact it remains pinned does not negatively
        /// affect memory management's compactification.
        /// </summary>
        private const int _blockCount = 32;

        /// <summary>
        /// Max allocation block size for pooled blocks,
        /// larger values can be leased but they will be disposed after use rather than returned to the pool.
        /// </summary>
        public override int MaxBufferSize { get; } = _blockSize;

        /// <summary>
        /// 4096 * 32 gives you a slabLength of 128k contiguous bytes allocated per slab
        /// </summary>
        private static readonly int _slabLength = _blockSize * _blockCount;

        /// <summary>
        /// Thread-safe collection of blocks which are currently in the pool. A slab will pre-allocate all of the block tracking objects
        /// and add them to this collection. When memory is requested it is taken from here first, and when it is returned it is re-added.
        /// </summary>
        private readonly ConcurrentQueue<MemoryPoolBlock> _blocks = new ConcurrentQueue<MemoryPoolBlock>();

        /// <summary>
        /// Thread-safe collection of slabs which have been allocated by this pool. As long as a slab is in this collection and slab.IsActive,
        /// the blocks will be added to _blocks when returned.
        /// </summary>
        private readonly ConcurrentStack<MemoryPoolSlab> _slabs = new ConcurrentStack<MemoryPoolSlab>();

        /// <summary>
        /// This is part of implementing the IDisposable pattern.
        /// </summary>
        private bool _isDisposed; // To detect redundant calls

        private int _totalAllocatedBlocks;

        private readonly object _disposeSync = new object();

        /// <summary>
        /// This default value passed in to Rent to use the default value for the pool.
        /// </summary>
        private const int AnySize = -1;

        public override IMemoryOwner<byte> Rent(int size = AnySize)
        {
            if (size > _blockSize)
            {
                MemoryPoolThrowHelper.ThrowArgumentOutOfRangeException_BufferRequestTooLarge(_blockSize);
            }

            var block = Lease();
            return block;
        }

        /// <summary>
        /// Called to take a block from the pool.
        /// </summary>
        /// <returns>The block that is reserved for the called. It must be passed to Return when it is no longer being used.</returns>
        private MemoryPoolBlock Lease()
        {
            if (_isDisposed)
            {
                MemoryPoolThrowHelper.ThrowObjectDisposedException(MemoryPoolThrowHelper.ExceptionArgument.MemoryPool);
            }

            if (_blocks.TryDequeue(out MemoryPoolBlock block))
            {
                // block successfully taken from the stack - return it

                block.Lease();
                return block;
            }
            // no blocks available - grow the pool
            block = AllocateSlab();
            block.Lease();
            return block;
        }

        /// <summary>
        /// Internal method called when a block is requested and the pool is empty. It allocates one additional slab, creates all of the
        /// block tracking objects, and adds them all to the pool.
        /// </summary>
        private MemoryPoolBlock AllocateSlab()
        {
            var slab = MemoryPoolSlab.Create(_slabLength);
            _slabs.Push(slab);

            var basePtr = slab.NativePointer;
            // Page align the blocks
            var offset = (int)((((ulong)basePtr + (uint)_blockSize - 1) & ~((uint)_blockSize - 1)) - (ulong)basePtr);
            // Ensure page aligned
            Debug.Assert(((ulong)basePtr + (uint)offset) % _blockSize == 0);

            var blockCount = (_slabLength - offset) / _blockSize;
            Interlocked.Add(ref _totalAllocatedBlocks, blockCount);

            MemoryPoolBlock block = null;

            for (int i = 0; i < blockCount; i++)
            {
                block = new MemoryPoolBlock(this, slab, offset, _blockSize);

                if (i != blockCount - 1) // last block
                {
#if BLOCK_LEASE_TRACKING
                    block.IsLeased = true;
#endif
                    Return(block);
                }

                offset += _blockSize;
            }

            return block;
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
                _blocks.Enqueue(block);
            }
            else
            {
                GC.SuppressFinalize(block);
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
                    while (_slabs.TryPop(out MemoryPoolSlab slab))
                    {
                        // dispose managed state (managed objects).
                        slab.Dispose();
                    }
                }

                // Discard blocks in pool
                while (_blocks.TryDequeue(out MemoryPoolBlock block))
                {
                    GC.SuppressFinalize(block);
                }
            }
        }
    }
}
