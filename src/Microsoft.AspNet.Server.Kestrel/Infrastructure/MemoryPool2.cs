using System;
using System.Collections.Concurrent;

namespace Microsoft.AspNet.Server.Kestrel.Infrastructure
{
    /// <summary>
    /// Used to allocate and distribute re-usable blocks of memory.
    /// </summary>
    public class MemoryPool2 : IDisposable
    {
        /// <summary>
        /// The gap between blocks' starting address. 4096 is chosen because most operating systems are 4k pages in size and alignment.
        /// </summary>
        private const int blockStride = 4096;

        /// <summary>
        /// The last 64 bytes of a block are unused to prevent CPU from pre-fetching the next 64 byte into it's memory cache. 
        /// See https://github.com/aspnet/KestrelHttpServer/issues/117 and https://www.youtube.com/watch?v=L7zSU9HI-6I
        /// </summary>
        private const int blockUnused = 64;

        /// <summary>
        /// Allocating 32 contiguous blocks per slab makes the slab size 128k. This is larger than the 85k size which will place the memory
        /// in the large object heap. This means the GC will not try to relocate this array, so the fact it remains pinned does not negatively
        /// affect memory management's compactification.
        /// </summary>
        private const int blockCount = 32;

        /// <summary>
        /// 4096 - 64 gives you a blockLength of 4032 usable bytes per block.
        /// </summary>
        private const int blockLength = blockStride - blockUnused;

        /// <summary>
        /// 4096 * 32 gives you a slabLength of 128k contiguous bytes allocated per slab
        /// </summary>
        private const int slabLength = blockStride * blockCount;

        /// <summary>
        /// Thread-safe collection of blocks which are currently in the pool. A slab will pre-allocate all of the block tracking objects
        /// and add them to this collection. When memory is requested it is taken from here first, and when it is returned it is re-added.
        /// </summary>
        private readonly ConcurrentStack<MemoryPoolBlock2> _blocks = new ConcurrentStack<MemoryPoolBlock2>();

        /// <summary>
        /// Thread-safe collection of slabs which have been allocated by this pool. As long as a slab is in this collection and slab.IsActive, 
        /// the blocks will be added to _blocks when returned.
        /// </summary>
        private readonly ConcurrentStack<MemoryPoolSlab2> _slabs = new ConcurrentStack<MemoryPoolSlab2>();

        /// <summary>
        /// This is part of implementing the IDisposable pattern.
        /// </summary>
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Called to take a block from the pool.
        /// </summary>
        /// <param name="minimumSize">The block returned must be at least this size. It may be larger than this minimum size, and if so,
        /// the caller may write to the block's entire size rather than being limited to the minumumSize requested.</param>
        /// <returns>The block that is reserved for the called. It must be passed to Return when it is no longer being used.</returns>
        public MemoryPoolBlock2 Lease(int minimumSize)
        {
            if (minimumSize > blockLength)
            {
                // The requested minimumSize is actually larger then the usable memory of a single block.
                // Because this is the degenerate case, a one-time-use byte[] array and tracking object are allocated.
                // When this block tracking object is returned it is not added to the pool - instead it will be 
                // allowed to be garbage collected normally.
                return MemoryPoolBlock2.Create(
                    new ArraySegment<byte>(new byte[minimumSize]),
                    dataPtr: IntPtr.Zero,
                    pool: null,
                    slab: null);
            }

            while (true)
            {
                MemoryPoolBlock2 block;
                if (_blocks.TryPop(out block))
                {
                    // block successfully taken from the stack - return it
                    return block;
                }
                // no blocks available - grow the pool and try again
                AllocateSlab();
            }
        }

        /// <summary>
        /// Internal method called when a block is requested and the pool is empty. It allocates one additional slab, creates all of the 
        /// block tracking objects, and adds them all to the pool.
        /// </summary>
        private void AllocateSlab()
        {
            var slab = MemoryPoolSlab2.Create(slabLength);
            _slabs.Push(slab);

            var basePtr = slab.ArrayPtr;
            var firstOffset = (blockStride - 1) - ((ushort)(basePtr + blockStride - 1) % blockStride);

            for (var offset = firstOffset;
                offset + blockLength <= slabLength;
                offset += blockStride)
            {
                var block = MemoryPoolBlock2.Create(
                    new ArraySegment<byte>(slab.Array, offset, blockLength),
                    basePtr,
                    this,
                    slab);
                Return(block);
            }
        }

        /// <summary>
        /// Called to return a block to the pool. Once Return has been called the memory no longer belongs to the caller, and
        /// Very Bad Things will happen if the memory is read of modified subsequently. If a caller fails to call Return and the
        /// block tracking object is garbage collected, the block tracking object's finalizer will automatically re-create and return
        /// a new tracking object into the pool. This will only happen if there is a bug in the server, however it is necessary to avoid
        /// leaving "dead zones" in the slab due to lost block tracking objects.
        /// </summary>
        /// <param name="block">The block to return. It must have been acquired by calling Lease on the same memory pool instance.</param>
        public void Return(MemoryPoolBlock2 block)
        {
            block.Reset();
            _blocks.Push(block);
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    MemoryPoolSlab2 slab;
                    while (_slabs.TryPop(out slab))
                    {
                        // dispose managed state (managed objects).
                        slab.Dispose();
                    }
                }

                // N/A: free unmanaged resources (unmanaged objects) and override a finalizer below.

                // N/A: set large fields to null.

                disposedValue = true;
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
        #endregion
    }
}
