using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    /// <summary>
    /// Used to allocate and distribute re-usable blocks of memory.
    /// </summary>
    public class MemoryPool : IDisposable
    {
        /// <summary>
        /// The gap between blocks' starting address. 4096 is chosen because most operating systems are 4k pages in size and alignment.
        /// </summary>
        private const int _blockStride = 4096;

        /// <summary>
        /// The last 64 bytes of a block are unused to prevent CPU from pre-fetching the next 64 byte into it's memory cache. 
        /// See https://github.com/aspnet/KestrelHttpServer/issues/117 and https://www.youtube.com/watch?v=L7zSU9HI-6I
        /// </summary>
        private const int _blockUnused = 64;

        /// <summary>
        /// Allocating 32 contiguous blocks per slab makes the slab size 128k. This is larger than the 85k size which will place the memory
        /// in the large object heap. This means the GC will not try to relocate this array, so the fact it remains pinned does not negatively
        /// affect memory management's compactification.
        /// </summary>
        private const int _blockCount = 32;

        /// <summary>
        /// 4096 - 64 gives you a blockLength of 4032 usable bytes per block.
        /// </summary>
        private const int _blockLength = _blockStride - _blockUnused;
        
        /// <summary>
        /// Max allocation block size for pooled blocks, 
        /// larger values can be leased but they will be disposed after use rather than returned to the pool.
        /// </summary>
        public const int MaxPooledBlockLength = _blockLength;

        /// <summary>
        /// 4096 * 32 gives you a slabLength of 128k contiguous bytes allocated per slab
        /// </summary>
        private const int _slabLength = _blockStride * _blockCount;

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
        private bool _disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Called to take a block from the pool.
        /// </summary>
        /// <returns>The block that is reserved for the called. It must be passed to Return when it is no longer being used.</returns>
#if DEBUG
        public MemoryPoolBlock Lease(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Debug.Assert(!_disposedValue, "Block being leased from disposed pool!");
#else
        public MemoryPoolBlock Lease()
        {
#endif
            MemoryPoolBlock block;
            if (_blocks.TryDequeue(out block))
            {
                // block successfully taken from the stack - return it
#if DEBUG
                block.Leaser = memberName + ", " + sourceFilePath + ", " + sourceLineNumber;
                block.IsLeased = true;
#endif
                return block;
            }
            // no blocks available - grow the pool
            block = AllocateSlab();
#if DEBUG
            block.Leaser = memberName + ", " + sourceFilePath + ", " + sourceLineNumber;
            block.IsLeased = true;
#endif
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

            var basePtr = slab.ArrayPtr;
            var firstOffset = (int)((_blockStride - 1) - ((ulong)(basePtr + _blockStride - 1) % _blockStride));

            var poolAllocationLength = _slabLength - _blockStride;

            var offset = firstOffset;
            for (;
                offset + _blockLength < poolAllocationLength;
                offset += _blockStride)
            {
                var block = MemoryPoolBlock.Create(
                    new ArraySegment<byte>(slab.Array, offset, _blockLength),
                    basePtr,
                    this,
                    slab);
#if DEBUG
                block.IsLeased = true;
#endif
                Return(block);
            }

            // return last block rather than adding to pool
            var newBlock = MemoryPoolBlock.Create(
                    new ArraySegment<byte>(slab.Array, offset, _blockLength),
                    basePtr,
                    this,
                    slab);

            return newBlock;
        }

        /// <summary>
        /// Called to return a block to the pool. Once Return has been called the memory no longer belongs to the caller, and
        /// Very Bad Things will happen if the memory is read of modified subsequently. If a caller fails to call Return and the
        /// block tracking object is garbage collected, the block tracking object's finalizer will automatically re-create and return
        /// a new tracking object into the pool. This will only happen if there is a bug in the server, however it is necessary to avoid
        /// leaving "dead zones" in the slab due to lost block tracking objects.
        /// </summary>
        /// <param name="block">The block to return. It must have been acquired by calling Lease on the same memory pool instance.</param>
        public void Return(MemoryPoolBlock block)
        {
#if DEBUG
            Debug.Assert(block.Pool == this, "Returned block was not leased from this pool");
            Debug.Assert(block.IsLeased, $"Block being returned to pool twice: {block.Leaser}{Environment.NewLine}");
            block.IsLeased = false;
#endif

            if (block.Slab != null && block.Slab.IsActive)
            {
                block.Reset();
                _blocks.Enqueue(block);
            }
            else
            {
                GC.SuppressFinalize(block);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _disposedValue = true;
#if DEBUG
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
#endif
                if (disposing)
                {
                    MemoryPoolSlab slab;
                    while (_slabs.TryPop(out slab))
                    {
                        // dispose managed state (managed objects).
                        slab.Dispose();
                    }
                }

                // Discard blocks in pool
                MemoryPoolBlock block;
                while (_blocks.TryDequeue(out block))
                {
                    GC.SuppressFinalize(block);
                }

                // N/A: free unmanaged resources (unmanaged objects) and override a finalizer below.

                // N/A: set large fields to null.

            }
        }

        // N/A: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MemoryPool2() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // N/A: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
    }
}
