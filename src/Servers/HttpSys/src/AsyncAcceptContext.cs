// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal unsafe class AsyncAcceptContext : IValueTaskSource<RequestContext>, IDisposable
    {
        private static readonly IOCompletionCallback IOCallback = IOWaitCallback;
        private readonly PreAllocatedOverlapped _preallocatedOverlapped;
        private NativeOverlapped* _overlapped;

        // mutable struct; do not make this readonly
        private ManualResetValueTaskSourceCore<RequestContext> _mrvts = new()
        {
            // We want to run continuations on the IO threads
            RunContinuationsAsynchronously = false
        };

        private NativeRequestContext _nativeRequestContext;

        internal AsyncAcceptContext(HttpSysListener server)
        {
            Server = server;
            _preallocatedOverlapped = new(IOCallback, state: this, pinData: null);
        }

        internal HttpSysListener Server { get; }

        internal ValueTask<RequestContext> AcceptAsync()
        {
            _mrvts.Reset();

            AllocateNativeRequest();

            uint statusCode = QueueBeginGetContext();
            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
            {
                // some other bad error, possible(?) return values are:
                // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                return ValueTask.FromException<RequestContext>(new HttpSysException((int)statusCode));
            }

            return new ValueTask<RequestContext>(this, _mrvts.Version);
        }

        private static void IOCompleted(AsyncAcceptContext asyncContext, uint errorCode, uint numBytes)
        {
            bool complete = false;

            try
            {
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                    errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                {
                    asyncContext._mrvts.SetException(new HttpSysException((int)errorCode));
                    return;
                }

                HttpSysListener server = asyncContext.Server;
                if (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
                {
                    // at this point we have received an unmanaged HTTP_REQUEST and memoryBlob
                    // points to it we need to hook up our authentication handling code here.
                    try
                    {
                        var nativeContext = asyncContext._nativeRequestContext;

                        if (server.ValidateRequest(nativeContext) && server.ValidateAuth(nativeContext))
                        {
                            // It's important that we clear the native request context before we set the result
                            // we want to reuse this object for future accepts.
                            asyncContext._nativeRequestContext = null;

                            var requestContext = new RequestContext(server, nativeContext);
                            asyncContext._mrvts.SetResult(requestContext);

                            complete = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        asyncContext._mrvts.SetException(ex);
                    }
                    finally
                    {
                        if (!complete)
                        {
                            asyncContext.AllocateNativeRequest(size: asyncContext._nativeRequestContext.Size);
                        }
                    }
                }
                else
                {
                    //  (uint)backingBuffer.Length - AlignmentPadding
                    asyncContext.AllocateNativeRequest(numBytes, asyncContext._nativeRequestContext.RequestId);
                }

                // We need to issue a new request, either because auth failed, or because our buffer was too small the first time.
                if (!complete)
                {
                    uint statusCode = asyncContext.QueueBeginGetContext();

                    if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                        statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
                    {
                        // someother bad error, possible(?) return values are:
                        // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                        asyncContext._mrvts.SetException(new HttpSysException((int)statusCode));
                    }
                }
            }
            catch (Exception exception)
            {
                asyncContext._mrvts.SetException(exception);
            }
        }

        private static unsafe void IOWaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            var asyncResult = (AsyncAcceptContext)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
            IOCompleted(asyncResult, errorCode, numBytes);
        }

        private uint QueueBeginGetContext()
        {
            uint statusCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS;
            bool retry;
            do
            {
                retry = false;
                uint bytesTransferred = 0;
                statusCode = HttpApi.HttpReceiveHttpRequest(
                    Server.RequestQueue.Handle,
                    _nativeRequestContext.RequestId,
                    // Small perf impact by not using HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY
                    // if the request sends header+body in a single TCP packet 
                    (uint)HttpApiTypes.HTTP_FLAGS.NONE,
                    _nativeRequestContext.NativeRequest,
                    _nativeRequestContext.Size,
                    &bytesTransferred,
                    _overlapped);

                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_INVALID_PARAMETER && _nativeRequestContext.RequestId != 0)
                {
                    // we might get this if somebody stole our RequestId,
                    // set RequestId to 0 and start all over again with the buffer we just allocated
                    // BUGBUG: how can someone steal our request ID?  seems really bad and in need of fix.
                    _nativeRequestContext.RequestId = 0;
                    retry = true;
                }
                else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                {
                    // the buffer was not big enough to fit the headers, we need
                    // to read the RequestId returned, allocate a new buffer of the required size
                    //  (uint)backingBuffer.Length - AlignmentPadding
                    AllocateNativeRequest(bytesTransferred);
                    retry = true;
                }
                else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS
                    && HttpSysListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously - callback won't be called to signal completion.
                    IOCompleted(this, statusCode, bytesTransferred);
                }
            }
            while (retry);
            return statusCode;
        }

        private void AllocateNativeRequest(uint? size = null, ulong requestId = 0)
        {
            _nativeRequestContext?.ReleasePins();
            _nativeRequestContext?.Dispose();

            var boundHandle = Server.RequestQueue.BoundHandle;

            if (_overlapped != null)
            {
                boundHandle.FreeNativeOverlapped(_overlapped);
            }

            _nativeRequestContext = new NativeRequestContext(Server.MemoryPool, size, requestId);
            _overlapped = boundHandle.AllocateNativeOverlapped(_preallocatedOverlapped);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_nativeRequestContext != null)
                {
                    _nativeRequestContext.ReleasePins();
                    _nativeRequestContext.Dispose();
                    _nativeRequestContext = null;

                    var boundHandle = Server.RequestQueue.BoundHandle;

                    if (_overlapped != null)
                    {
                        boundHandle.FreeNativeOverlapped(_overlapped);
                        _overlapped = null;
                    }
                }
            }
        }

        public RequestContext GetResult(short token)
        {
            return _mrvts.GetResult(token);
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return _mrvts.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _mrvts.OnCompleted(continuation, state, token, flags);
        }
    }
}
