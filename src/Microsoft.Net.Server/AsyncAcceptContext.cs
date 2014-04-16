//------------------------------------------------------------------------------
// <copyright file="_ListenerAsyncResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Net.Server
{
    internal unsafe class AsyncAcceptContext : IAsyncResult, IDisposable
    {
        internal static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(IOWaitCallback);

        private TaskCompletionSource<RequestContext> _tcs;
        private WebListener _server;
        private NativeRequestContext _nativeRequestContext;

        internal AsyncAcceptContext(WebListener server)
        {
            _server = server;
            _tcs = new TaskCompletionSource<RequestContext>();
            _nativeRequestContext = new NativeRequestContext(this);
        }

        internal Task<RequestContext> Task
        {
            get
            {
                return _tcs.Task;
            }
        }

        private TaskCompletionSource<RequestContext> Tcs
        {
            get
            {
                return _tcs;
            }
        }

        private WebListener Server
        {
            get
            {
                return _server;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Redirecting to callback")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by callback")]
        private static void IOCompleted(AsyncAcceptContext asyncResult, uint errorCode, uint numBytes)
        {
            bool complete = false;
            try
            {
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                    errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                {
                    asyncResult.Tcs.TrySetException(new WebListenerException((int)errorCode));
                    complete = true;
                }
                else
                {
                    WebListener server = asyncResult.Server;
                    if (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
                    {
                        // at this point we have received an unmanaged HTTP_REQUEST and memoryBlob
                        // points to it we need to hook up our authentication handling code here.
                        bool stoleBlob = false;
                        try
                        {
                            if (server.ValidateRequest(asyncResult._nativeRequestContext))
                            {
                                stoleBlob = true;
                                RequestContext requestContext = new RequestContext(server, asyncResult._nativeRequestContext);
                                asyncResult.Tcs.TrySetResult(requestContext);
                                complete = true;
                            }
                        }
                        finally
                        {
                            if (stoleBlob)
                            {
                                // The request has been handed to the user, which means this code can't reuse the blob.  Reset it here.
                                asyncResult._nativeRequestContext = complete ? null : new NativeRequestContext(asyncResult);
                            }
                            else
                            {
                                asyncResult._nativeRequestContext.Reset(0, 0);
                            }
                        }
                    }
                    else
                    {
                        asyncResult._nativeRequestContext.Reset(asyncResult._nativeRequestContext.RequestBlob->RequestId, numBytes);
                    }

                    // We need to issue a new request, either because auth failed, or because our buffer was too small the first time.
                    if (!complete)
                    {
                        uint statusCode = asyncResult.QueueBeginGetContext();
                        if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                            statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
                        {
                            // someother bad error, possible(?) return values are:
                            // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                            asyncResult.Tcs.TrySetException(new WebListenerException((int)statusCode));
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
                    asyncResult.Dispose();
                }
            }
            catch (Exception exception)
            {
                // Logged by caller
                asyncResult.Tcs.TrySetException(exception);
                asyncResult.Dispose();
            }
        }

        private static unsafe void IOWaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            // take the ListenerAsyncResult object from the state
            Overlapped callbackOverlapped = Overlapped.Unpack(nativeOverlapped);
            AsyncAcceptContext asyncResult = (AsyncAcceptContext)callbackOverlapped.AsyncResult;

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
                statusCode = UnsafeNclNativeMethods.HttpApi.HttpReceiveHttpRequest(
                    Server.RequestQueueHandle,
                    _nativeRequestContext.RequestBlob->RequestId,
                    (uint)UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY,
                    _nativeRequestContext.RequestBlob,
                    _nativeRequestContext.Size,
                    &bytesTransferred,
                    _nativeRequestContext.NativeOverlapped);

                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_INVALID_PARAMETER && _nativeRequestContext.RequestBlob->RequestId != 0)
                {
                    // we might get this if somebody stole our RequestId,
                    // set RequestId to 0 and start all over again with the buffer we just allocated
                    // BUGBUG: how can someone steal our request ID?  seems really bad and in need of fix.
                    _nativeRequestContext.RequestBlob->RequestId = 0;
                    retry = true;
                }
                else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                {
                    // the buffer was not big enough to fit the headers, we need
                    // to read the RequestId returned, allocate a new buffer of the required size
                    _nativeRequestContext.Reset(_nativeRequestContext.RequestBlob->RequestId, bytesTransferred);
                    retry = true;
                }
                else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS
                    && WebListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously - callback won't be called to signal completion.
                    IOCompleted(this, statusCode, bytesTransferred);
                }
            }
            while (retry);
            return statusCode;
        }

        public object AsyncState
        {
            get { return _tcs.Task.AsyncState; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return ((IAsyncResult)_tcs.Task).AsyncWaitHandle; }
        }

        public bool CompletedSynchronously
        {
            get { return ((IAsyncResult)_tcs.Task).CompletedSynchronously; }
        }

        public bool IsCompleted
        {
            get { return _tcs.Task.IsCompleted; }
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
