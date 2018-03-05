// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        private int _hr;

        public static readonly NativeMethods.PFN_WEBSOCKET_ASYNC_COMPLETION ReadCallback = (IntPtr pHttpContext, IntPtr pCompletionInfo, IntPtr pvCompletionContext) =>
        {
            var context = (IISHttpContext)GCHandle.FromIntPtr(pvCompletionContext).Target;

            NativeMethods.http_get_completion_info(pCompletionInfo, out int cbBytes, out int hr);

            context.CompleteReadWebSockets(hr, cbBytes);

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        };

        public static readonly NativeMethods.PFN_WEBSOCKET_ASYNC_COMPLETION WriteCallback = (IntPtr pHttpContext, IntPtr pCompletionInfo, IntPtr pvCompletionContext) =>
        {
            var context = (IISHttpContext)GCHandle.FromIntPtr(pvCompletionContext).Target;

            NativeMethods.http_get_completion_info(pCompletionInfo, out int cbBytes, out int hr);

            context.CompleteWriteWebSockets(hr, cbBytes);

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        };

        public IISAwaitable GetAwaiter() => this;
        public bool IsCompleted => _callback == _callbackCompleted;

        public bool HasContinuation => _callback != null && !IsCompleted;

        public int GetResult()
        {
            var exception = _exception;
            var cbBytes = _cbBytes;
            var hr = _hr;

            // Reset the awaitable state
            _exception = null;
            _cbBytes = 0;
            _callback = null;
            _hr = 0;

            if (exception != null)
            {
                // If the exception was an aborted read operation,
                // return -1 to notify NativeReadAsync that the write was cancelled.
                // E_OPERATIONABORTED == 0x800703e3 == -2147023901
                // We also don't throw the exception here as this is expected behavior
                // and can negatively impact perf if we catch an exception for each
                // cann
                if (hr != IISServerConstants.HResultCancelIO)   
                {
                    throw exception;
                }
                else
                {
                    cbBytes = -1;
                }
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
            _hr = hr;
            _exception = Marshal.GetExceptionForHR(hr);
            _cbBytes = cbBytes;
            var continuation = Interlocked.Exchange(ref _callback, _callbackCompleted);
            continuation?.Invoke();
        }

        public Action GetCompletion(int hr, int cbBytes)
        {
            _hr = hr;
            _exception = Marshal.GetExceptionForHR(hr);
            _cbBytes = cbBytes;

            return Interlocked.Exchange(ref _callback, _callbackCompleted);
        }
    }
}
