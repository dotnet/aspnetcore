using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    /// <summary>
    /// Slab tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
    /// individual blocks are then treated as independant array segments.
    /// </summary>
    public class MemoryPoolSlab : IDisposable
    {
        /// <summary>
        /// This handle pins the managed array in memory until the slab is disposed. This prevents it from being
        /// relocated and enables any subsections of the array to be used as native memory pointers to P/Invoked API calls.
        /// </summary>
        private GCHandle _gcHandle;

        /// <summary>
        /// The managed memory allocated in the large object heap.
        /// </summary>
        public byte[] Array;

        /// <summary>
        /// The native memory pointer of the pinned Array. All block native addresses are pointers into the memory 
        /// ranging from ArrayPtr to ArrayPtr + Array.Length
        /// </summary>
        public IntPtr ArrayPtr;

        /// <summary>
        /// True as long as the blocks from this slab are to be considered returnable to the pool. In order to shrink the 
        /// memory pool size an entire slab must be removed. That is done by (1) setting IsActive to false and removing the
        /// slab from the pool's _slabs collection, (2) as each block currently in use is Return()ed to the pool it will
        /// be allowed to be garbage collected rather than re-pooled, and (3) when all block tracking objects are garbage
        /// collected and the slab is no longer references the slab will be garbage collected and the memory unpinned will
        /// be unpinned by the slab's Dispose.
        /// </summary>
        public bool IsActive;

        /// <summary>
        /// Part of the IDisposable implementation
        /// </summary>
        private bool _disposedValue = false; // To detect redundant calls

        public static MemoryPoolSlab Create(int length)
        {
            // allocate and pin requested memory length
            var array = new byte[length];
            var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);

            // allocate and return slab tracking object
            return new MemoryPoolSlab
            {
                Array = array,
                _gcHandle = gcHandle,
                ArrayPtr = gcHandle.AddrOfPinnedObject(),
                IsActive = true,
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // N/A: dispose managed state (managed objects).
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                IsActive = false;
                _gcHandle.Free();

                // set large fields to null.
                Array = null;

                _disposedValue = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~MemoryPoolSlab()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
    }
}
