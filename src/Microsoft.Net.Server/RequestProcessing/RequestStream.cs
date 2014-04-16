// ------------------------------------------------------------------------------
// <copyright file="_HttpRequestStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Net.Server
{
    internal class RequestStream : Stream
    {
        private const int MaxReadSize = 0x20000; // http.sys recommends we limit reads to 128k

        private RequestContext _requestContext;
        private uint _dataChunkOffset;
        private int _dataChunkIndex;
        private bool _closed;

        internal RequestStream(RequestContext httpContext)
        {
            _requestContext = httpContext;
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException(Resources.Exception_NoSeek);
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException(Resources.Exception_NoSeek);
            }
            set
            {
                throw new NotSupportedException(Resources.Exception_NoSeek);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(Resources.Exception_NoSeek);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(Resources.Exception_NoSeek);
        }

        public override void Flush()
        {
            throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
        }

        private void ValidateReadBuffer(byte[] buffer, int offset, int size)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset", offset, string.Empty);
            }
            if (size <= 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size", size, string.Empty);
            }
        }

        public override unsafe int Read([In, Out] byte[] buffer, int offset, int size)
        {
            ValidateReadBuffer(buffer, offset, size);
            if (_closed)
            {
                return 0;
            }
            // TODO: Verbose log parameters

            uint dataRead = 0;

            if (_dataChunkIndex != -1)
            {
                dataRead = UnsafeNclNativeMethods.HttpApi.GetChunks(_requestContext.Request.RequestBuffer, _requestContext.Request.OriginalBlobAddress, ref _dataChunkIndex, ref _dataChunkOffset, buffer, offset, size);
            }

            if (_dataChunkIndex == -1 && dataRead < size)
            {
                uint statusCode = 0;
                uint extraDataRead = 0;
                offset += (int)dataRead;
                size -= (int)dataRead;

                // the http.sys team recommends that we limit the size to 128kb
                if (size > MaxReadSize)
                {
                    size = MaxReadSize;
                }

                fixed (byte* pBuffer = buffer)
                {
                    // issue unmanaged blocking call

                    uint flags = 0;

                    statusCode =
                        UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody(
                            _requestContext.RequestQueueHandle,
                            _requestContext.RequestId,
                            flags,
                            (IntPtr)(pBuffer + offset),
                            (uint)size,
                            out extraDataRead,
                            SafeNativeOverlapped.Zero);

                    dataRead += extraDataRead;
                }
                if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF)
                {
                    Exception exception = new WebListenerException((int)statusCode);
                    LogHelper.LogException(_requestContext.Logger, "Read", exception);
                    throw exception;
                }
                UpdateAfterRead(statusCode, dataRead);
            }

            // TODO: Verbose log dump data read
            return (int)dataRead;
        }

        private void UpdateAfterRead(uint statusCode, uint dataRead)
        {
            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF || dataRead == 0)
            {
                Dispose();
            }
        }

#if NET45
        public override unsafe IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
#else
        public unsafe IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
#endif
        {
            ValidateReadBuffer(buffer, offset, size);
            if (_closed)
            {
                RequestStreamAsyncResult result = new RequestStreamAsyncResult(this, state, callback);
                result.Complete(0);
                return result;
            }
            // TODO: Verbose log parameters

            RequestStreamAsyncResult asyncResult = null;

            uint dataRead = 0;
            if (_dataChunkIndex != -1)
            {
                dataRead = UnsafeNclNativeMethods.HttpApi.GetChunks(_requestContext.Request.RequestBuffer, _requestContext.Request.OriginalBlobAddress, ref _dataChunkIndex, ref _dataChunkOffset, buffer, offset, size);
                if (_dataChunkIndex != -1 && dataRead == size)
                {
                    asyncResult = new RequestStreamAsyncResult(this, state, callback, buffer, offset, 0);
                    asyncResult.Complete((int)dataRead);
                }
            }

            if (_dataChunkIndex == -1 && dataRead < size)
            {
                uint statusCode = 0;
                offset += (int)dataRead;
                size -= (int)dataRead;

                // the http.sys team recommends that we limit the size to 128kb
                if (size > MaxReadSize)
                {
                    size = MaxReadSize;
                }

                asyncResult = new RequestStreamAsyncResult(this, state, callback, buffer, offset, dataRead);
                uint bytesReturned;

                try
                {
                    fixed (byte* pBuffer = buffer)
                    {
                        uint flags = 0;

                        statusCode =
                            UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody(
                                _requestContext.RequestQueueHandle,
                                _requestContext.RequestId,
                                flags,
                                asyncResult.PinnedBuffer,
                                (uint)size,
                                out bytesReturned,
                                asyncResult.NativeOverlapped);
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogException(_requestContext.Logger, "BeginRead", e);
                    asyncResult.Dispose();
                    throw;
                }

                if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
                {
                    asyncResult.Dispose();
                    if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF)
                    {
                        asyncResult = new RequestStreamAsyncResult(this, state, callback, dataRead);
                        asyncResult.Complete((int)bytesReturned);
                    }
                    else
                    {
                        Exception exception = new WebListenerException((int)statusCode);
                        LogHelper.LogException(_requestContext.Logger, "BeginRead", exception);
                        throw exception;
                    }
                }
                else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                         WebListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously - callback won't be called to signal completion.
                    asyncResult.IOCompleted(statusCode, bytesReturned);
                }
            }
            return asyncResult;
        }

#if NET45
        public override int EndRead(IAsyncResult asyncResult)
#else
        public int EndRead(IAsyncResult asyncResult)
#endif
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            RequestStreamAsyncResult castedAsyncResult = asyncResult as RequestStreamAsyncResult;
            if (castedAsyncResult == null || castedAsyncResult.RequestStream != this)
            {
                throw new ArgumentException(Resources.Exception_WrongIAsyncResult, "asyncResult");
            }
            if (castedAsyncResult.EndCalled)
            {
                throw new InvalidOperationException(Resources.Exception_EndCalledMultipleTimes);
            }
            castedAsyncResult.EndCalled = true;
            // wait & then check for errors
            // Throws on failure
            int dataRead = castedAsyncResult.Task.Result;
            // TODO: Verbose log #dataRead.
            return dataRead;
        }

        public override unsafe Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
        {
            ValidateReadBuffer(buffer, offset, size);
            if (_closed)
            {
                return Task.FromResult<int>(0);
            }

            // TODO: Needs full cancellation integration
            cancellationToken.ThrowIfCancellationRequested();
            // TODO: Verbose log parameters

            RequestStreamAsyncResult asyncResult = null;

            uint dataRead = 0;
            if (_dataChunkIndex != -1)
            {
                dataRead = UnsafeNclNativeMethods.HttpApi.GetChunks(_requestContext.Request.RequestBuffer, _requestContext.Request.OriginalBlobAddress, ref _dataChunkIndex, ref _dataChunkOffset, buffer, offset, size);
                if (_dataChunkIndex != -1 && dataRead == size)
                {
                    UpdateAfterRead(UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS, dataRead);
                    // TODO: Verbose log #dataRead
                    return Task.FromResult<int>((int)dataRead);
                }
            }
            
            if (_dataChunkIndex == -1 && dataRead < size)
            {
                uint statusCode = 0;
                offset += (int)dataRead;
                size -= (int)dataRead;

                // the http.sys team recommends that we limit the size to 128kb
                if (size > MaxReadSize)
                {
                    size = MaxReadSize;
                }

                asyncResult = new RequestStreamAsyncResult(this, null, null, buffer, offset, dataRead);
                uint bytesReturned;

                try
                {
                    uint flags = 0;

                    statusCode =
                        UnsafeNclNativeMethods.HttpApi.HttpReceiveRequestEntityBody(
                            _requestContext.RequestQueueHandle,
                            _requestContext.RequestId,
                            flags,
                            asyncResult.PinnedBuffer,
                            (uint)size,
                            out bytesReturned,
                            asyncResult.NativeOverlapped);
                }
                catch (Exception e)
                {
                    asyncResult.Dispose();
                    LogHelper.LogException(_requestContext.Logger, "ReadAsync", e);
                    throw;
                }

                if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
                {
                    asyncResult.Dispose();
                    if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF)
                    {
                        uint totalRead = dataRead + bytesReturned;
                        UpdateAfterRead(statusCode, totalRead);
                        // TODO: Verbose log totalRead
                        return Task.FromResult<int>((int)totalRead);
                    }
                    else
                    {
                        Exception exception = new WebListenerException((int)statusCode);
                        LogHelper.LogException(_requestContext.Logger, "ReadAsync", exception);
                        throw exception;
                    }
                }
                else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                         WebListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously - callback won't be called to signal completion.
                    asyncResult.Dispose();
                    uint totalRead = dataRead + bytesReturned;
                    UpdateAfterRead(statusCode, totalRead);
                    // TODO: Verbose log
                    return Task.FromResult<int>((int)totalRead);
                }
            }
            return asyncResult.Task;
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
        }
        
#if NET45
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
#else
        public IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
#endif
        {
            throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
        }

#if NET45
        public override void EndWrite(IAsyncResult asyncResult)
#else
        public void EndWrite(IAsyncResult asyncResult)
#endif
        {
            throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _closed = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private unsafe class RequestStreamAsyncResult : IAsyncResult, IDisposable
        {
            private static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(Callback);

            private SafeNativeOverlapped _overlapped;
            private IntPtr _pinnedBuffer;
            private uint _dataAlreadyRead = 0;
            private TaskCompletionSource<int> _tcs;
            private RequestStream _requestStream;
            private AsyncCallback _callback;

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
                : this(requestStream, userState, callback)
            {
                _dataAlreadyRead = dataAlreadyRead;
                Overlapped overlapped = new Overlapped();
                overlapped.AsyncResult = this;
                _overlapped = new SafeNativeOverlapped(overlapped.Pack(IOCallback, buffer));
                _pinnedBuffer = (Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
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
                        asyncResult.Fail(new WebListenerException((int)errorCode));
                    }
                    else
                    {
                        // TODO: Verbose log dump data read
                        asyncResult.Complete((int)numBytes, errorCode);
                    }
                }
                catch (Exception e)
                {
                    asyncResult.Fail(e);
                }
            }

            private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
            {
                Overlapped callbackOverlapped = Overlapped.Unpack(nativeOverlapped);
                RequestStreamAsyncResult asyncResult = callbackOverlapped.AsyncResult as RequestStreamAsyncResult;

                IOCompleted(asyncResult, errorCode, numBytes);
            }

            internal void Complete(int read, uint errorCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                if (_tcs.TrySetResult(read + (int)DataAlreadyRead))
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
}
