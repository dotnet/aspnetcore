// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal unsafe class RequestStreamAsyncResult : IAsyncResult, IDisposable
    {
        private static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(Callback);

        private SafeNativeOverlapped _overlapped;
        private IntPtr _pinnedBuffer;
        private uint _dataAlreadyRead;
        private TaskCompletionSource<int> _tcs;
        private RequestStream _requestStream;
        private AsyncCallback _callback;
        private CancellationTokenRegistration _cancellationRegistration;

        internal RequestStreamAsyncResult(RequestStream requestStream, object userState, AsyncCallback callback)
        {
            _requestStream = requestStream;
            _tcs = new TaskCompletionSource<int>(userState);
            _callback = callback;
        }

        internal RequestStreamAsyncResult(RequestStream requestStream, object userState, AsyncCallback callback, uint dataAlreadyRead)
            : this(requestStream, userState, callback)
        {
            _dataAlreadyRead = dataAlreadyRead;
        }

        internal RequestStreamAsyncResult(RequestStream requestStream, object userState, AsyncCallback callback, byte[] buffer, int offset, uint dataAlreadyRead)
            : this(requestStream, userState, callback, buffer, offset, dataAlreadyRead, new CancellationTokenRegistration())
        {
        }

        internal RequestStreamAsyncResult(RequestStream requestStream, object userState, AsyncCallback callback, byte[] buffer, int offset, uint dataAlreadyRead, CancellationTokenRegistration cancellationRegistration)
            : this(requestStream, userState, callback)
        {
            _dataAlreadyRead = dataAlreadyRead;
            var boundHandle = requestStream.RequestContext.Server.RequestQueue.BoundHandle;
            _overlapped = new SafeNativeOverlapped(boundHandle,
                boundHandle.AllocateNativeOverlapped(IOCallback, this, buffer));
            _pinnedBuffer = (Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
            _cancellationRegistration = cancellationRegistration;
        }

        internal RequestStream RequestStream
        {
            get { return _requestStream; }
        }

        internal SafeNativeOverlapped NativeOverlapped
        {
            get { return _overlapped; }
        }

        internal IntPtr PinnedBuffer
        {
            get { return _pinnedBuffer; }
        }

        internal uint DataAlreadyRead
        {
            get { return _dataAlreadyRead; }
        }

        internal Task<int> Task
        {
            get { return _tcs.Task; }
        }

        internal bool EndCalled { get; set; }

        internal void IOCompleted(uint errorCode, uint numBytes)
        {
            IOCompleted(this, errorCode, numBytes);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Redirecting to callback")]
        private static void IOCompleted(RequestStreamAsyncResult asyncResult, uint errorCode, uint numBytes)
        {
            try
            {
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF)
                {
                    asyncResult.Fail(new IOException(string.Empty, new HttpSysException((int)errorCode)));
                }
                else
                {
                    // TODO: Verbose log dump data read
                    asyncResult.Complete((int)numBytes, errorCode);
                }
            }
            catch (Exception e)
            {
                asyncResult.Fail(new IOException(string.Empty, e));
            }
        }

        private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            var asyncResult = (RequestStreamAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
            IOCompleted(asyncResult, errorCode, numBytes);
        }

        internal void Complete(int read, uint errorCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
        {
            if (_requestStream.TryCheckSizeLimit(read + (int)DataAlreadyRead, out var exception))
            {
                _tcs.TrySetException(exception);
            }
            else if (_tcs.TrySetResult(read + (int)DataAlreadyRead))
            {
                RequestStream.UpdateAfterRead((uint)errorCode, (uint)(read + DataAlreadyRead));
                if (_callback != null)
                {
                    try
                    {
                        _callback(this);
                    }
                    catch (Exception)
                    {
                        // TODO: Exception handling? This may be an IO callback thread and throwing here could crash the app.
                    }
                }
            }
            Dispose();
        }

        internal void Fail(Exception ex)
        {
            if (_tcs.TrySetException(ex) && _callback != null)
            {
                try
                {
                    _callback(this);
                }
                catch (Exception)
                {
                    // TODO: Exception handling? This may be an IO callback thread and throwing here could crash the app.
                    // TODO: Log
                }
            }
            Dispose();
            _requestStream.Abort();
        }

        [SuppressMessage("Microsoft.Usage", "CA2216:DisposableTypesShouldDeclareFinalizer", Justification = "The disposable resource referenced does have a finalizer.")]
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_overlapped != null)
                {
                    _overlapped.Dispose();
                }
                _cancellationRegistration.Dispose();
            }
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
    }
}