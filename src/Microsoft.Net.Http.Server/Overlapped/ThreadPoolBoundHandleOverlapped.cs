#if !DOTNET5_4 // TODO: Temp copy. Remove once we target net46.
using System;
namespace System.Threading
{
    internal sealed class ThreadPoolBoundHandleOverlapped : Overlapped
    {
        private readonly IOCompletionCallback _userCallback;
        internal readonly object _userState;
        internal PreAllocatedOverlapped _preAllocated;
        internal unsafe NativeOverlapped* _nativeOverlapped;
        internal ThreadPoolBoundHandle _boundHandle;
        internal bool _completed;
        public unsafe ThreadPoolBoundHandleOverlapped(IOCompletionCallback callback, object state, object pinData, PreAllocatedOverlapped preAllocated)
        {
            this._userCallback = callback;
            this._userState = state;
            this._preAllocated = preAllocated;
            this._nativeOverlapped = base.Pack(new IOCompletionCallback(ThreadPoolBoundHandleOverlapped.CompletionCallback), pinData);
            this._nativeOverlapped->OffsetLow = 0;
            this._nativeOverlapped->OffsetHigh = 0;
        }
        private unsafe static void CompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            ThreadPoolBoundHandleOverlapped expr_0B = (ThreadPoolBoundHandleOverlapped)Overlapped.Unpack(nativeOverlapped);
            if (expr_0B._completed)
            {
                throw new InvalidOperationException("Native Overlapped reused");
            }
            expr_0B._completed = true;
            if (expr_0B._boundHandle == null)
            {
                throw new InvalidOperationException("Already freed");
            }
            expr_0B._userCallback.Invoke(errorCode, numBytes, nativeOverlapped);
        }
    }
}
#endif