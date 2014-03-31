// ------------------------------------------------------------------------------
// <copyright file="_HttpResponseStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.WebListener
{
    internal class ResponseStream : Stream
    {
        private static readonly byte[] ChunkTerminator = new byte[] { (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

        private RequestContext _requestContext;
        private long _leftToWrite = long.MinValue;
        private bool _closed;
        private bool _inOpaqueMode;
        // The last write needs special handling to cancel.
        private ResponseStreamAsyncResult _lastWrite;

        internal ResponseStream(RequestContext requestContext)
        {
            _requestContext = requestContext;
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
                return true;
            }
        }

        public override bool CanRead
        {
            get
            {
                return false;
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

        // Send headers
        public override void Flush()
        {
            if (_closed || _requestContext.Response.SentHeaders)
            {
                return;
            }
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
            // TODO: Verbose log

            try
            {
                uint statusCode;
                unsafe
                {
                    // TODO: Don't add MoreData flag if content-length == 0?
                    flags |= UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
                    statusCode = _requestContext.Response.SendHeaders(null, null, flags, false);
                }

                if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF)
                {
                    throw new WebListenerException((int)statusCode);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(_requestContext.Logger, "Flush", e);
                _closed = true;
                _requestContext.Abort();
                throw;
            }
        }

        // Send headers
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_closed || _requestContext.Response.SentHeaders)
            {
                return Helpers.CompletedTask();
            }
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
            // TODO: Verbose log

            // TODO: Real cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // TODO: Don't add MoreData flag if content-length == 0?
            flags |= UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
            ResponseStreamAsyncResult asyncResult = new ResponseStreamAsyncResult(this, null, null, null, 0, 0, _requestContext.Response.BoundaryType == BoundaryType.Chunked, false);

            try
            {
                uint statusCode;
                unsafe
                {
                    statusCode = _requestContext.Response.SendHeaders(null, asyncResult, flags, false);
                }

                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && OwinWebListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously - callback won't be called to signal completion.
                    asyncResult.IOCompleted(statusCode);
                }
                else if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
                {
                    throw new WebListenerException((int)statusCode);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(_requestContext.Logger, "FlushAsync", e);
                asyncResult.Dispose();
                _closed = true;
                _requestContext.Abort();
                throw;
            }

            return asyncResult.Task;
        }

        #region NotSupported Read/Seek

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(Resources.Exception_NoSeek);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(Resources.Exception_NoSeek);
        }

        public override int Read([In, Out] byte[] buffer, int offset, int size)
        {
            throw new InvalidOperationException(Resources.Exception_WriteOnlyStream);
        }

#if NET45
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException(Resources.Exception_WriteOnlyStream);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new InvalidOperationException(Resources.Exception_WriteOnlyStream);
        }
#endif

        #endregion

        private UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS ComputeLeftToWrite(bool endOfRequest = false)
        {
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE;
            if (!_requestContext.Response.ComputedHeaders)
            {
                flags = _requestContext.Response.ComputeHeaders(endOfRequest: endOfRequest);
            }
            if (_leftToWrite == long.MinValue)
            {
                UnsafeNclNativeMethods.HttpApi.HTTP_VERB method = _requestContext.Request.GetKnownMethod();
                if (method == UnsafeNclNativeMethods.HttpApi.HTTP_VERB.HttpVerbHEAD)
                {
                    _leftToWrite = 0;
                }
                else if (_requestContext.Response.BoundaryType == BoundaryType.ContentLength)
                {
                    _leftToWrite = _requestContext.Response.CalculatedLength;
                }
                else
                {
                    _leftToWrite = -1; // unlimited
                }
            }
            return flags;
        }

        public override unsafe void Write(byte[] buffer, int offset, int size)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            if (_closed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            // TODO: Verbose log parameters
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
            if (size == 0 && _leftToWrite != 0)
            {
                return;
            }
            if (_leftToWrite >= 0 && size > _leftToWrite)
            {
                throw new InvalidOperationException(Resources.Exception_TooMuchWritten);
            }
            // TODO: Verbose log

            uint statusCode;
            uint dataToWrite = (uint)size;
            SafeLocalFree bufferAsIntPtr = null;
            IntPtr pBufferAsIntPtr = IntPtr.Zero;
            bool sentHeaders = _requestContext.Response.SentHeaders;
            try
            {
                if (size == 0)
                {
                    // TODO: Is this code path accessible? Is this like a Flush?
                    statusCode = _requestContext.Response.SendHeaders(null, null, flags, false);
                }
                else
                {
                    fixed (byte* pDataBuffer = buffer)
                    {
                        byte* pBuffer = pDataBuffer;
                        if (_requestContext.Response.BoundaryType == BoundaryType.Chunked)
                        {
                            // TODO:
                            // here we need some heuristics, some time it is definitely better to split this in 3 write calls
                            // but for small writes it is probably good enough to just copy the data internally.
                            string chunkHeader = size.ToString("x", CultureInfo.InvariantCulture);
                            dataToWrite = dataToWrite + (uint)(chunkHeader.Length + 4);
                            bufferAsIntPtr = SafeLocalFree.LocalAlloc((int)dataToWrite);
                            pBufferAsIntPtr = bufferAsIntPtr.DangerousGetHandle();
                            for (int i = 0; i < chunkHeader.Length; i++)
                            {
                                Marshal.WriteByte(pBufferAsIntPtr, i, (byte)chunkHeader[i]);
                            }
                            Marshal.WriteInt16(pBufferAsIntPtr, chunkHeader.Length, 0x0A0D);
                            Marshal.Copy(buffer, offset, IntPtrHelper.Add(pBufferAsIntPtr, chunkHeader.Length + 2), size);
                            Marshal.WriteInt16(pBufferAsIntPtr, (int)(dataToWrite - 2), 0x0A0D);
                            pBuffer = (byte*)pBufferAsIntPtr;
                            offset = 0;
                        }
                        UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK dataChunk = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                        dataChunk.DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                        dataChunk.fromMemory.pBuffer = (IntPtr)(pBuffer + offset);
                        dataChunk.fromMemory.BufferLength = dataToWrite;

                        flags |= _leftToWrite == size ? UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE : UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
                        if (!sentHeaders)
                        {
                            statusCode = _requestContext.Response.SendHeaders(&dataChunk, null, flags, false);
                        }
                        else
                        {
                            statusCode =
                                UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(
                                    _requestContext.RequestQueueHandle,
                                    _requestContext.RequestId,
                                    (uint)flags,
                                    1,
                                    &dataChunk,
                                    null,
                                    SafeLocalFree.Zero,
                                    0,
                                    SafeNativeOverlapped.Zero,
                                    IntPtr.Zero);

                            if (_requestContext.Server.IgnoreWriteExceptions)
                            {
                                statusCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (bufferAsIntPtr != null)
                {
                    // free unmanaged buffer
                    bufferAsIntPtr.Dispose();
                }
            }

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF)
            {
                Exception exception = new WebListenerException((int)statusCode);
                LogHelper.LogException(_requestContext.Logger, "Write", exception);
                _closed = true;
                _requestContext.Abort();
                throw exception;
            }
            UpdateWritenCount(dataToWrite);

            // TODO: Verbose log data written
        }
#if NET45
        public override unsafe IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
#else
        public unsafe IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
#endif
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            if (_closed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
            if (size == 0 && _leftToWrite != 0)
            {
                ResponseStreamAsyncResult result = new ResponseStreamAsyncResult(this, state, callback);
                result.Complete();
                return result;
            }
            if (_leftToWrite >= 0 && size > _leftToWrite)
            {
                throw new InvalidOperationException(Resources.Exception_TooMuchWritten);
            }
            // TODO: Verbose log parameters

            uint statusCode;
            uint bytesSent = 0;
            flags |= _leftToWrite == size ? UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE : UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
            bool sentHeaders = _requestContext.Response.SentHeaders;
            ResponseStreamAsyncResult asyncResult = new ResponseStreamAsyncResult(this, state, callback, buffer, offset, size, _requestContext.Response.BoundaryType == BoundaryType.Chunked, sentHeaders);

            // Update m_LeftToWrite now so we can queue up additional BeginWrite's without waiting for EndWrite.
            UpdateWritenCount((uint)((_requestContext.Response.BoundaryType == BoundaryType.Chunked) ? 0 : size));

            try
            {
                if (!sentHeaders)
                {
                    statusCode = _requestContext.Response.SendHeaders(null, asyncResult, flags, false);
                    bytesSent = asyncResult.BytesSent;
                }
                else
                {
                    statusCode =
                        UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(
                            _requestContext.RequestQueueHandle,
                            _requestContext.RequestId,
                            (uint)flags,
                            asyncResult.DataChunkCount,
                            asyncResult.DataChunks,
                            &bytesSent,
                            SafeLocalFree.Zero,
                            0,
                            asyncResult.NativeOverlapped,
                            IntPtr.Zero);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(_requestContext.Logger, "BeginWrite", e);
                asyncResult.Dispose();
                _closed = true;
                _requestContext.Abort();
                throw;
            }

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
            {
                asyncResult.Dispose();
                if (_requestContext.Server.IgnoreWriteExceptions && sentHeaders)
                {
                    asyncResult.Complete();
                }
                else
                {
                    Exception exception = new WebListenerException((int)statusCode);
                    LogHelper.LogException(_requestContext.Logger, "BeginWrite", exception);
                    _closed = true;
                    _requestContext.Abort();
                    throw exception;
                }
            }

            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && OwinWebListener.SkipIOCPCallbackOnSuccess)
            {
                // IO operation completed synchronously - callback won't be called to signal completion.
                asyncResult.IOCompleted(statusCode, bytesSent);
            }

            // Last write, cache it for special cancelation handling.
            if ((flags & UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA) == 0)
            {
                _lastWrite = asyncResult;
            }

            return asyncResult;
        }
#if NET45
        public override void EndWrite(IAsyncResult asyncResult)
#else
        public void EndWrite(IAsyncResult asyncResult)
#endif
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            ResponseStreamAsyncResult castedAsyncResult = asyncResult as ResponseStreamAsyncResult;
            if (castedAsyncResult == null || castedAsyncResult.ResponseStream != this)
            {
                throw new ArgumentException(Resources.Exception_WrongIAsyncResult, "asyncResult");
            }
            if (castedAsyncResult.EndCalled)
            {
                throw new InvalidOperationException(Resources.Exception_EndCalledMultipleTimes);
            }
            castedAsyncResult.EndCalled = true;

            try
            {
                // wait & then check for errors
                // TODO: Gracefull re-throw
                castedAsyncResult.Task.Wait();
            }
            catch (Exception exception)
            {
                LogHelper.LogException(_requestContext.Logger, "EndWrite", exception);
                _closed = true;
                _requestContext.Abort();
                throw;
            }
        }

        public override unsafe Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancel)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            if (_closed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
            if (size == 0 && _leftToWrite != 0)
            {
                return Helpers.CompletedTask();
            }
            if (_leftToWrite >= 0 && size > _leftToWrite)
            {
                throw new InvalidOperationException(Resources.Exception_TooMuchWritten);
            }
            // TODO: Verbose log

            // TODO: Real cancelation
            cancel.ThrowIfCancellationRequested();

            uint statusCode;
            uint bytesSent = 0;
            flags |= _leftToWrite == size ? UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE : UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
            bool sentHeaders = _requestContext.Response.SentHeaders;
            ResponseStreamAsyncResult asyncResult = new ResponseStreamAsyncResult(this, null, null, buffer, offset, size, _requestContext.Response.BoundaryType == BoundaryType.Chunked, sentHeaders);

            // Update m_LeftToWrite now so we can queue up additional BeginWrite's without waiting for EndWrite.
            UpdateWritenCount((uint)((_requestContext.Response.BoundaryType == BoundaryType.Chunked) ? 0 : size));

            try
            {
                if (!sentHeaders)
                {
                    statusCode = _requestContext.Response.SendHeaders(null, asyncResult, flags, false);
                    bytesSent = asyncResult.BytesSent;
                }
                else
                {
                    statusCode =
                        UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(
                            _requestContext.RequestQueueHandle,
                            _requestContext.RequestId,
                            (uint)flags,
                            asyncResult.DataChunkCount,
                            asyncResult.DataChunks,
                            &bytesSent,
                            SafeLocalFree.Zero,
                            0,
                            asyncResult.NativeOverlapped,
                            IntPtr.Zero);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(_requestContext.Logger, "WriteAsync", e);
                asyncResult.Dispose();
                _closed = true;
                _requestContext.Abort();
                throw;
            }

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
            {
                asyncResult.Dispose();
                if (_requestContext.Server.IgnoreWriteExceptions && sentHeaders)
                {
                    asyncResult.Complete();
                }
                else
                {
                    Exception exception = new WebListenerException((int)statusCode);
                    LogHelper.LogException(_requestContext.Logger, "WriteAsync", exception);
                    _closed = true;
                    _requestContext.Abort();
                    throw exception;
                }
            }

            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && OwinWebListener.SkipIOCPCallbackOnSuccess)
            {
                // IO operation completed synchronously - callback won't be called to signal completion.
                asyncResult.IOCompleted(statusCode, bytesSent);
            }

            // Last write, cache it for special cancelation handling.
            if ((flags & UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA) == 0)
            {
                _lastWrite = asyncResult;
            }

            return asyncResult.Task;
        }

        internal unsafe Task SendFileAsync(string fileName, long offset, long? size, CancellationToken cancel)
        {
            // It's too expensive to validate the file attributes before opening the file. Open the file and then check the lengths.
            // This all happens inside of ResponseStreamAsyncResult.
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }
            if (_closed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            // TODO: Real cancellation
            cancel.ThrowIfCancellationRequested();

            UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite();
            if (size == 0 && _leftToWrite != 0)
            {
                return Helpers.CompletedTask();
            }
            if (_leftToWrite >= 0 && size > _leftToWrite)
            {
                throw new InvalidOperationException(Resources.Exception_TooMuchWritten);
            }
            // TODO: Verbose log

            uint statusCode;
            uint bytesSent = 0;
            flags |= _leftToWrite == size ? UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE : UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
            bool sentHeaders = _requestContext.Response.SentHeaders;
            ResponseStreamAsyncResult asyncResult = new ResponseStreamAsyncResult(this, null, null, fileName, offset, size,
                _requestContext.Response.BoundaryType == BoundaryType.Chunked, sentHeaders);

            long bytesWritten;
            if (_requestContext.Response.BoundaryType == BoundaryType.Chunked)
            {
                bytesWritten = 0;
            }
            else if (size.HasValue)
            {
                bytesWritten = size.Value;
            }
            else
            {
                bytesWritten = asyncResult.FileLength - offset;
            }
            // Update m_LeftToWrite now so we can queue up additional calls to SendFileAsync.
            UpdateWritenCount((uint)bytesWritten);

            try
            {
                if (!sentHeaders)
                {
                    statusCode = _requestContext.Response.SendHeaders(null, asyncResult, flags, false);
                    bytesSent = asyncResult.BytesSent;
                }
                else
                {
                    // TODO: If opaque then include the buffer data flag.
                    statusCode =
                        UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(
                            _requestContext.RequestQueueHandle,
                            _requestContext.RequestId,
                            (uint)flags,
                            asyncResult.DataChunkCount,
                            asyncResult.DataChunks,
                            &bytesSent,
                            SafeLocalFree.Zero,
                            0,
                            asyncResult.NativeOverlapped,
                            IntPtr.Zero);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(_requestContext.Logger, "SendFileAsync", e);
                asyncResult.Dispose();
                _closed = true;
                _requestContext.Abort();
                throw;
            }

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
            {
                asyncResult.Dispose();
                if (_requestContext.Server.IgnoreWriteExceptions && sentHeaders)
                {
                    asyncResult.Complete();
                }
                else
                {
                    Exception exception = new WebListenerException((int)statusCode);
                    LogHelper.LogException(_requestContext.Logger, "SendFileAsync", exception);
                    _closed = true;
                    _requestContext.Abort();
                    throw exception;
                }
            }

            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && OwinWebListener.SkipIOCPCallbackOnSuccess)
            {
                // IO operation completed synchronously - callback won't be called to signal completion.
                asyncResult.IOCompleted(statusCode, bytesSent);
            }

            // Last write, cache it for special cancellation handling.
            if ((flags & UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA) == 0)
            {
                _lastWrite = asyncResult;
            }

            return asyncResult.Task;
        }

        private void UpdateWritenCount(uint dataWritten)
        {
            if (!_inOpaqueMode)
            {
                if (_leftToWrite > 0)
                {
                    // keep track of the data transferred
                    _leftToWrite -= dataWritten;
                }
                if (_leftToWrite == 0)
                {
                    // in this case we already passed 0 as the flag, so we don't need to call HttpSendResponseEntityBody() when we Close()
                    _closed = true;
                }
            }
        }

        protected override unsafe void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_closed)
                    {
                        return;
                    }
                    _closed = true;
                    UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS flags = ComputeLeftToWrite(endOfRequest: true);
                    if (_leftToWrite > 0 && !_inOpaqueMode)
                    {
                        _requestContext.Abort();
                        // TODO: Reduce this to a logged warning, it is thrown too late to be visible in user code.
                        LogHelper.LogError(_requestContext.Logger, "ResponseStream::Dispose", "Fewer bytes were written than were specified in the Content-Length.");
                        return;
                    }
                    bool sentHeaders = _requestContext.Response.SentHeaders;
                    if (sentHeaders && _leftToWrite == 0)
                    {
                        return;
                    }

                    uint statusCode = 0;
                    if ((_requestContext.Response.BoundaryType == BoundaryType.Chunked || _requestContext.Response.BoundaryType == BoundaryType.None) && (String.Compare(_requestContext.Request.Method, "HEAD", StringComparison.OrdinalIgnoreCase) != 0))
                    {
                        if (_requestContext.Response.BoundaryType == BoundaryType.None)
                        {
                            flags |= UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
                        }
                        fixed (void* pBuffer = ChunkTerminator)
                        {
                            UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK* pDataChunk = null;
                            if (_requestContext.Response.BoundaryType == BoundaryType.Chunked)
                            {
                                UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK dataChunk = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                                dataChunk.DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                                dataChunk.fromMemory.pBuffer = (IntPtr)pBuffer;
                                dataChunk.fromMemory.BufferLength = (uint)ChunkTerminator.Length;
                                pDataChunk = &dataChunk;
                            }
                            if (!sentHeaders)
                            {
                                statusCode = _requestContext.Response.SendHeaders(pDataChunk, null, flags, false);
                            }
                            else
                            {
                                statusCode =
                                    UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(
                                        _requestContext.RequestQueueHandle,
                                        _requestContext.RequestId,
                                        (uint)flags,
                                        pDataChunk != null ? (ushort)1 : (ushort)0,
                                        pDataChunk,
                                        null,
                                        SafeLocalFree.Zero,
                                        0,
                                        SafeNativeOverlapped.Zero,
                                        IntPtr.Zero);

                                if (_requestContext.Server.IgnoreWriteExceptions)
                                {
                                    statusCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!sentHeaders)
                        {
                            statusCode = _requestContext.Response.SendHeaders(null, null, flags, false);
                        }
                    }
                    if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF
                        // Don't throw for disconnects, we were already finished with the response.
                        && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_CONNECTION_INVALID)
                    {
                        Exception exception = new WebListenerException((int)statusCode);
                        LogHelper.LogException(_requestContext.Logger, "Dispose", exception);
                        _requestContext.Abort();
                        throw exception;
                    }
                    _leftToWrite = 0;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        internal void SwitchToOpaqueMode()
        {
            _inOpaqueMode = true;
            _leftToWrite = long.MaxValue;
        }

        // The final Content-Length async write can only be cancelled by CancelIoEx.
        // Sync can only be cancelled by CancelSynchronousIo, but we don't attempt this right now.
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification =
            "It is safe to ignore the return value on a cancel operation because the connection is being closed")]
        internal unsafe void CancelLastWrite(SafeHandle requestQueueHandle)
        {
            ResponseStreamAsyncResult asyncState = _lastWrite;
            if (asyncState != null && !asyncState.IsCompleted)
            {
                UnsafeNclNativeMethods.CancelIoEx(requestQueueHandle, asyncState.NativeOverlapped);
            }
        }
    }
}
