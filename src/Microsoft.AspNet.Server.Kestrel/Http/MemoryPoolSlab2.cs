using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class MemoryPoolSlab2 : IDisposable
    {
        private GCHandle _gcHandle;
        public byte[] Array;
        public IntPtr ArrayPtr;
        public bool IsActive;

        public static MemoryPoolSlab2 Create(int length)
        {
            var array = new byte[length];
            var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            return new MemoryPoolSlab2
            {
                Array = array,
                _gcHandle = gcHandle,
                ArrayPtr = gcHandle.AddrOfPinnedObject(),
                IsActive = true,
            };
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
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

                disposedValue = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~MemoryPoolSlab2()
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
        #endregion
    }
}
