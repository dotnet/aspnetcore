// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    // Primarily copied from https://github.com/aspnet/KestrelHttpServer/blob/dev/src/Kestrel.Transport.Libuv/Internal/LibuvAwaitable.cs
    internal class IISAwaitable : ICriticalNotifyCompletion
    {
        private readonly static Action _callbackCompleted = () => { };

        private Action _callback;

        private Exception _exception;

        private int _cbBytes;

        public static readonly NativeMethods.PFN_ASYNC_COMPLETION ReadCallback = (IntPtr pHttpContext, IntPtr pCompletionInfo, IntPtr pvCompletionContext) =>
        {
            var context = (HttpProtocol)GCHandle.FromIntPtr(pvCompletionContext).Target;

            NativeMethods.http_get_completion_info(pCompletionInfo, out int cbBytes, out int hr);

            context.CompleteRead(hr, cbBytes);

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        };

        public static readonly NativeMethods.PFN_ASYNC_COMPLETION WriteCallback = (IntPtr pHttpContext, IntPtr pCompletionInfo, IntPtr pvCompletionContext) =>
        {
            var context = (HttpProtocol)GCHandle.FromIntPtr(pvCompletionContext).Target;

            NativeMethods.http_get_completion_info(pCompletionInfo, out int cbBytes, out int hr);

            context.CompleteWrite(hr, cbBytes);

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        };

        public static readonly NativeMethods.PFN_ASYNC_COMPLETION FlushCallback = (IntPtr pHttpContext, IntPtr pCompletionInfo, IntPtr pvCompletionContext) =>
        {
            var context = (HttpProtocol)GCHandle.FromIntPtr(pvCompletionContext).Target;

            NativeMethods.http_get_completion_info(pCompletionInfo, out int cbBytes, out int hr);

            context.CompleteFlush(hr, cbBytes);

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        };

        public IISAwaitable GetAwaiter() => this;
        public bool IsCompleted => _callback == _callbackCompleted;

        public bool HasContinuation => _callback != null && !IsCompleted;

        public int GetResult()
        {
            var exception = _exception;
            var cbBytes = _cbBytes;

            // Reset the awaitable state
            _exception = null;
            _cbBytes = 0;
            _callback = null;

            if (exception != null)
            {
                throw exception;
            }

            return cbBytes;
        }

        public void OnCompleted(Action continuation)
        {
            // There should never be a race between IsCompleted and OnCompleted since both operations
            // should always be on the libuv thread

            if (_callback == _callbackCompleted ||
                Interlocked.CompareExchange(ref _callback, continuation, null) == _callbackCompleted)
            {
                // Just run it inline
                Task.Run(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void Complete(int hr, int cbBytes)
        {
            _exception = Marshal.GetExceptionForHR(hr);
            _cbBytes = cbBytes;

            var continuation = Interlocked.Exchange(ref _callback, _callbackCompleted);

            continuation?.Invoke();
        }
    }
}
