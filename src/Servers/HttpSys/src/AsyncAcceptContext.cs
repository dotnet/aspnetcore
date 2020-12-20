// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal unsafe class AsyncAcceptContext : Overlapped, IValueTaskSource<RequestContext>, IDisposable
    {
        private static readonly IOCompletionCallback IOCallback = IOWaitCallback;
        private readonly IRequestContextFactory _requestContextFactory;
        private readonly NativeOverlapped* _overlapped;
        private readonly SafeHandle _requestQueueHandle;

        // mutable struct; do not make this readonly
        private ManualResetValueTaskSourceCore<RequestContext> _mrvts = new()
        {
            // We want to run continuations on the IO threads
            RunContinuationsAsynchronously = false
        };

        private RequestContext _requestContext;

        internal AsyncAcceptContext(HttpSysListener server, IRequestContextFactory requestContextFactory)
        {
            _requestQueueHandle = server.RequestQueue.Handle;
            _requestContextFactory = requestContextFactory;
            _overlapped = UnsafePack(IOCallback, userData: null);
            _overlapped->OffsetHigh = 0;
            _overlapped->OffsetLow = 0;
        }

        internal ValueTask<RequestContext> AcceptAsync()
        {
            AllocateNativeRequest();

            uint statusCode = QueueBeginGetContext(yieldOnSynchronousCompletion: true);
            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
            {
                // some other bad error, possible(?) return values are:
                // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                return ValueTask.FromException<RequestContext>(new HttpSysException((int)statusCode));
            }

            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
            {
                _mrvts.Reset();

                return new ValueTask<RequestContext>(this, _mrvts.Version);
            }

            var requestContext = _requestContext;
            _requestContext = null;
            return ValueTask.FromResult(requestContext);
        }

        private void IOCompleted(uint errorCode, uint numBytes)
        {
            try
            {
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                    errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                {
                    _mrvts.SetException(new HttpSysException((int)errorCode));
                    return;
                }

                if (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
                {
                    var requestContext = _requestContext;
                    // It's important that we clear the request context before we set the result
                    // we want to reuse the acceptContext object for future accepts.
                    _requestContext = null;

                    _mrvts.SetResult(requestContext);
                }
                else
                {
                    //  (uint)backingBuffer.Length - AlignmentPadding
                    AllocateNativeRequest(numBytes, _requestContext.RequestId);

                    // We need to issue a new request, either because auth failed, or because our buffer was too small the first time.
                    uint statusCode = QueueBeginGetContext();

                    if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                        statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
                    {
                        // someother bad error, possible(?) return values are:
                        // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                        _mrvts.SetException(new HttpSysException((int)statusCode));
                    }
                }
            }
            catch (Exception exception)
            {
                _mrvts.SetException(exception);
            }
        }

        private static unsafe void IOWaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            var acceptContext = (AsyncAcceptContext)Unpack(nativeOverlapped);
            acceptContext.IOCompleted(errorCode, numBytes);
        }

        private uint QueueBeginGetContext(bool yieldOnSynchronousCompletion = false)
        {
            uint statusCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS;
            bool retry;
            do
            {
                retry = false;
                uint bytesTransferred = 0;
                statusCode = HttpApi.HttpReceiveHttpRequest(
                    _requestQueueHandle,
                    _requestContext.RequestId,
                    // Small perf impact by not using HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY
                    // if the request sends header+body in a single TCP packet 
                    (uint)HttpApiTypes.HTTP_FLAGS.NONE,
                    _requestContext.NativeRequest,
                    _requestContext.Size,
                    &bytesTransferred,
                    _overlapped);

                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_INVALID_PARAMETER && _requestContext.RequestId != 0)
                {
                    // we might get this if somebody stole our RequestId,
                    // set RequestId to 0 and start all over again with the buffer we just allocated
                    // BUGBUG: how can someone steal our request ID?  seems really bad and in need of fix.
                    _requestContext.RequestId = 0;
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
                    if (yieldOnSynchronousCompletion)
                    {
                        break;
                    }

                    // IO operation completed synchronously - callback won't be called to signal completion.
                    IOCompleted(statusCode, bytesTransferred);
                }
            }
            while (retry);
            return statusCode;
        }

        private void AllocateNativeRequest(uint? size = null, ulong requestId = 0)
        {
            _requestContext?.ReleasePins();
            _requestContext?.Dispose();

            _requestContext = _requestContextFactory.CreateRequestContext(size, requestId);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_requestContext != null)
                {
                    _requestContext.ReleasePins();
                    _requestContext.Dispose();
                    _requestContext = null;
                }

                Free(_overlapped);
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
