#if !DOTNET5_4 // TODO: Temp copy. Remove once we target net46.
using System;
using System.Runtime.InteropServices;
namespace System.Threading
{
    public sealed class ThreadPoolBoundHandle : IDisposable
    {
        private readonly SafeHandle _handle;
        private bool _isDisposed;
        public SafeHandle Handle
        {
            get
            {
                return this._handle;
            }
        }
        private ThreadPoolBoundHandle(SafeHandle handle)
        {
            this._handle = handle;
        }
        public static ThreadPoolBoundHandle BindHandle(SafeHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException("handle");
            }
            if (handle.IsClosed || handle.IsInvalid)
            {
                throw new ArgumentException("Invalid Handle", "handle");
            }
            try
            {
                ThreadPool.BindHandle(handle);
            }
            catch (Exception expr_38)
            {
                if (expr_38.HResult == -2147024890)
                {
                    throw new ArgumentException("Invalid Handle", "handle");
                }
                if (expr_38.HResult == -2147024809)
                {
                    throw new ArgumentException("Already Bound", "handle");
                }
                throw;
            }
            return new ThreadPoolBoundHandle(handle);
        }
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object state, object pinData)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }
            this.EnsureNotDisposed();
            return new ThreadPoolBoundHandleOverlapped(callback, state, pinData, null)
            {
                _boundHandle = this
            }._nativeOverlapped;
        }
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(PreAllocatedOverlapped preAllocated)
        {
            if (preAllocated == null)
            {
                throw new ArgumentNullException("preAllocated");
            }
            this.EnsureNotDisposed();
            preAllocated.AddRef();
            NativeOverlapped* nativeOverlapped;
            try
            {
                ThreadPoolBoundHandleOverlapped expr_21 = preAllocated._overlapped;
                if (expr_21._boundHandle != null)
                {
                    throw new ArgumentException("Already Allocated", "preAllocated");
                }
                expr_21._boundHandle = this;
                nativeOverlapped = expr_21._nativeOverlapped;
            }
            catch
            {
                preAllocated.Release();
                throw;
            }
            return nativeOverlapped;
        }
        [CLSCompliant(false)]
        public unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
            {
                throw new ArgumentNullException("overlapped");
            }
            ThreadPoolBoundHandleOverlapped overlappedWrapper = ThreadPoolBoundHandle.GetOverlappedWrapper(overlapped, this);
            if (overlappedWrapper._boundHandle != this)
            {
                throw new ArgumentException("Wrong bound handle", "overlapped");
            }
            if (overlappedWrapper._preAllocated != null)
            {
                overlappedWrapper._preAllocated.Release();
                return;
            }
            Overlapped.Free(overlapped);
        }
        [CLSCompliant(false)]
        public unsafe static object GetNativeOverlappedState(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
            {
                throw new ArgumentNullException("overlapped");
            }
            return ThreadPoolBoundHandle.GetOverlappedWrapper(overlapped, null)._userState;
        }
        private unsafe static ThreadPoolBoundHandleOverlapped GetOverlappedWrapper(NativeOverlapped* overlapped, ThreadPoolBoundHandle expectedBoundHandle)
        {
            ThreadPoolBoundHandleOverlapped result;
            try
            {
                result = (ThreadPoolBoundHandleOverlapped)Overlapped.Unpack(overlapped);
            }
            catch (NullReferenceException ex)
            {
                throw new ArgumentException("Already freed", "overlapped", ex);
            }
            return result;
        }
        public void Dispose()
        {
            this._isDisposed = true;
        }
        private void EnsureNotDisposed()
        {
            if (this._isDisposed)
            {
                throw new ObjectDisposedException(base.GetType().ToString());
            }
        }
    }
}
#endif