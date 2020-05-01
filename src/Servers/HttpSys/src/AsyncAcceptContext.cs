// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal unsafe class AsyncAcceptContext : TaskCompletionSource<RequestContext>, IDisposable
    {
        internal static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(IOWaitCallback);

        private NativeRequestContext _nativeRequestContext;

        internal AsyncAcceptContext(HttpSysListener server)
        {
            Server = server;
            AllocateNativeRequest();
        }

        internal HttpSysListener Server { get; }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Redirecting to callback")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by callback")]
        private static void IOCompleted(AsyncAcceptContext asyncContext, uint errorCode, uint numBytes)
        {
            bool complete = false;
            try
            {
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                    errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                {
                    asyncContext.TrySetException(new HttpSysException((int)errorCode));
                    complete = true;
                }
                else
                {
                    HttpSysListener server = asyncContext.Server;
                    if (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
                    {
                        // at this point we have received an unmanaged HTTP_REQUEST and memoryBlob
                        // points to it we need to hook up our authentication handling code here.
                        try
                        {
                            if (server.ValidateRequest(asyncContext._nativeRequestContext) && server.ValidateAuth(asyncContext._nativeRequestContext))
                            {
                                RequestContext requestContext = new RequestContext(server, asyncContext._nativeRequestContext);
                                asyncContext.TrySetResult(requestContext);
                                complete = true;
                            }
                        }
                        catch (Exception)
                        {
                            server.SendError(asyncContext._nativeRequestContext.RequestId, StatusCodes.Status400BadRequest);
                            throw;
                        }
                        finally
                        {
                            // The request has been handed to the user, which means this code can't reuse the blob.  Reset it here.
                            if (complete)
                            {
                                asyncContext._nativeRequestContext = null;
                            }
                            else
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
                            asyncContext.TrySetException(new HttpSysException((int)statusCode));
                            complete = true;
                        }
                    }
                    if (!complete)
                    {
                        return;
                    }
                }

                if (complete)
                {
                    asyncContext.Dispose();
                }
            }
            catch (Exception exception)
            {
                // Logged by caller
                asyncContext.TrySetException(exception);
                asyncContext.Dispose();
            }
        }

        private static unsafe void IOWaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            // take the ListenerAsyncResult object from the state
            var asyncResult = (AsyncAcceptContext)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
            IOCompleted(asyncResult, errorCode, numBytes);
        }

        internal uint QueueBeginGetContext()
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
                    _nativeRequestContext.NativeOverlapped);

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

        internal void AllocateNativeRequest(uint? size = null, ulong requestId = 0)
        {
            _nativeRequestContext?.ReleasePins();
            _nativeRequestContext?.Dispose();

            // We can't reuse overlapped objects
            var boundHandle = Server.RequestQueue.BoundHandle;
            var nativeOverlapped = new SafeNativeOverlapped(boundHandle,
                boundHandle.AllocateNativeOverlapped(IOCallback, this, pinData: null));

            // nativeRequest
            _nativeRequestContext = new NativeRequestContext(nativeOverlapped, Server.MemoryPool, size, requestId);
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
                }
            }
        }
    }
}
