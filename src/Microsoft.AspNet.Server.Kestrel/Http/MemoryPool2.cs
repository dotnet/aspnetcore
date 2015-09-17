using System;
using System.Collections.Concurrent;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class MemoryPool2 : IDisposable
    {
        private const int blockStride = 4096;
        private const int blockUnused = 64;
        private const int blockCount = 32;
        private const int blockLength = blockStride - blockUnused;
        private const int slabLength = blockStride * blockCount;

        private ConcurrentStack<MemoryPoolBlock2> _blocks = new ConcurrentStack<MemoryPoolBlock2>();
        private ConcurrentStack<MemoryPoolSlab2> _slabs = new ConcurrentStack<MemoryPoolSlab2>();
        private bool disposedValue = false; // To detect redundant calls

        public MemoryPoolBlock2 Lease(int minimumSize)
        {
            if (minimumSize > blockLength)
            {
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
                    return block;
                }
                AllocateSlab();
            }
        }

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
