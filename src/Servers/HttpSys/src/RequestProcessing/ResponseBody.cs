// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;
using static Microsoft.AspNetCore.HttpSys.Internal.UnsafeNclNativeMethods;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class ResponseBody : Stream
    {
        private RequestContext _requestContext;
        private long _leftToWrite = long.MinValue;
        private bool _skipWrites;
        private bool _disposed;

        // The last write needs special handling to cancel.
        private ResponseStreamAsyncResult _lastWrite;

        internal ResponseBody(RequestContext requestContext)
        {
            _requestContext = requestContext;
        }

        internal RequestContext RequestContext
        {
            get { return _requestContext; }
        }

        private SafeHandle RequestQueueHandle => RequestContext.Server.RequestQueue.Handle;

        private ulong RequestId => RequestContext.Request.RequestId;

        private ILogger Logger => RequestContext.Server.Logger;

        internal bool ThrowWriteExceptions => RequestContext.Server.Options.ThrowWriteExceptions;

        internal bool IsDisposed => _disposed;

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
            if (!RequestContext.AllowSynchronousIO)
            {
                throw new InvalidOperationException("Synchronous IO APIs are disabled, see AllowSynchronousIO.");
            }

            if (_disposed)
            {
                return;
            }

            FlushInternal(endOfRequest: false);
        }

        // We never expect endOfRequest and data at the same time
        private unsafe void FlushInternal(bool endOfRequest, ArraySegment<byte> data = new ArraySegment<byte>())
        {
            Debug.Assert(!(endOfRequest && data.Count > 0), "Data is not supported at the end of the request.");

            if (_skipWrites)
            {
                return;
            }

            var started = _requestContext.Response.HasStarted;
            if (data.Count == 0 && started && !endOfRequest)
            {
                // No data to send and we've already sent the headers
                return;
            }

            // Make sure all validation is performed before this computes the headers
            var flags = ComputeLeftToWrite(data.Count, endOfRequest);
            if (endOfRequest && _leftToWrite > 0)
            {
                if (!RequestContext.DisconnectToken.IsCancellationRequested)
                {
                    // This is logged rather than thrown because it is too late for an exception to be visible in user code.
                    Logger.LogError(LoggerEventIds.FewerBytesThanExpected, "ResponseStream::Dispose; Fewer bytes were written than were specified in the Content-Length.");
                }
                _requestContext.Abort();
                return;
            }

            uint statusCode = 0;
            HttpApiTypes.HTTP_DATA_CHUNK[] dataChunks;
            var pinnedBuffers = PinDataBuffers(endOfRequest, data, out dataChunks);
            try
            {
                if (!started)
                {
                    statusCode = _requestContext.Response.SendHeaders(dataChunks, null, flags, false);
                }
                else
                {
                    fixed (HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks = dataChunks)
                    {
                        statusCode = HttpApi.HttpSendResponseEntityBody(
                                RequestQueueHandle,
                                RequestId,
                                (uint)flags,
                                (ushort)dataChunks.Length,
                                pDataChunks,
                                null,
                                IntPtr.Zero,
                                0,
                                SafeNativeOverlapped.Zero,
                                IntPtr.Zero);
                    }
                }
            }
            finally
            {
                FreeDataBuffers(pinnedBuffers);
            }

            if (statusCode != ErrorCodes.ERROR_SUCCESS && statusCode != ErrorCodes.ERROR_HANDLE_EOF
                // Don't throw for disconnects, we were already finished with the response.
                && (!endOfRequest || (statusCode != ErrorCodes.ERROR_CONNECTION_INVALID && statusCode != ErrorCodes.ERROR_INVALID_PARAMETER)))
            {
                if (ThrowWriteExceptions)
                {
                    var exception = new IOException(string.Empty, new HttpSysException((int)statusCode));
                    Logger.LogError(LoggerEventIds.WriteError, exception, "Flush");
                    Abort();
                    throw exception;
                }
                else
                {
                    // Abort the request but do not close the stream, let future writes complete silently
                    Logger.LogDebug(LoggerEventIds.WriteErrorIgnored, $"Flush; Ignored write exception: {statusCode}");
                    Abort(dispose: false);
                }
            }
        }

        private List<GCHandle> PinDataBuffers(bool endOfRequest, ArraySegment<byte> data, out HttpApiTypes.HTTP_DATA_CHUNK[] dataChunks)
        {
            var pins = new List<GCHandle>();
            var hasData = data.Count > 0;
            var chunked = _requestContext.Response.BoundaryType == BoundaryType.Chunked;
            var addTrailers = endOfRequest && _requestContext.Response.HasTrailers;
            Debug.Assert(!(addTrailers && chunked), "Trailers aren't currently supported for HTTP/1.1 chunking.");

            var currentChunk = 0;
            // Figure out how many data chunks
            if (chunked && !hasData && endOfRequest)
            {
                dataChunks = new HttpApiTypes.HTTP_DATA_CHUNK[1];
                SetDataChunk(dataChunks, ref currentChunk, pins, new ArraySegment<byte>(Helpers.ChunkTerminator));
                return pins;
            }
            else if (!hasData && !addTrailers)
            {
                // No data
                dataChunks = new HttpApiTypes.HTTP_DATA_CHUNK[0];
                return pins;
            }

            var chunkCount = hasData ? 1 : 0;
            if (addTrailers)
            {
                chunkCount++;
            }
            else if (chunked) // HTTP/1.1 chunking, not currently supported with trailers
            {
                Debug.Assert(hasData);
                // Chunk framing
                chunkCount += 2;

                if (endOfRequest)
                {
                    // Chunk terminator
                    chunkCount += 1;
                }
            }

            dataChunks = new HttpApiTypes.HTTP_DATA_CHUNK[chunkCount];

            if (chunked)
            {
                var chunkHeaderBuffer = Helpers.GetChunkHeader(data.Count);
                SetDataChunk(dataChunks, ref currentChunk, pins, chunkHeaderBuffer);
            }

            if (hasData)
            {
                SetDataChunk(dataChunks, ref currentChunk, pins, data);
            }

            if (chunked)
            {
                SetDataChunk(dataChunks, ref currentChunk, pins, new ArraySegment<byte>(Helpers.CRLF));

                if (endOfRequest)
                {
                    SetDataChunk(dataChunks, ref currentChunk, pins, new ArraySegment<byte>(Helpers.ChunkTerminator));
                }
            }

            if (addTrailers)
            {
                _requestContext.Response.SerializeTrailers(dataChunks, currentChunk, pins);
            }
            else if (endOfRequest)
            {
                _requestContext.Response.MakeTrailersReadOnly();
            }

            return pins;
        }

        private static void SetDataChunk(HttpApiTypes.HTTP_DATA_CHUNK[] chunks, ref int chunkIndex, List<GCHandle> pins, ArraySegment<byte> buffer)
        {
            var handle = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
            pins.Add(handle);
            chunks[chunkIndex].DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
            chunks[chunkIndex].fromMemory.pBuffer = handle.AddrOfPinnedObject() + buffer.Offset;
            chunks[chunkIndex].fromMemory.BufferLength = (uint)buffer.Count;
            chunkIndex++;
        }

        private void FreeDataBuffers(List<GCHandle> pinnedBuffers)
        {
            foreach (var pin in pinnedBuffers)
            {
                if (pin.IsAllocated)
                {
                    pin.Free();
                }
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }
            return FlushInternalAsync(new ArraySegment<byte>(), cancellationToken);
        }

        // Simpler than Flush because it will never be called at the end of the request from Dispose.
        private unsafe Task FlushInternalAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            if (_skipWrites)
            {
                return Task.CompletedTask;
            }

            var started = _requestContext.Response.HasStarted;
            if (data.Count == 0 && started)
            {
                // No data to send and we've already sent the headers
                return Task.CompletedTask;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Abort(ThrowWriteExceptions);
                return Task.FromCanceled<int>(cancellationToken);
            }

            // Make sure all validation is performed before this computes the headers
            var flags = ComputeLeftToWrite(data.Count);
            uint statusCode = 0;
            var chunked = _requestContext.Response.BoundaryType == BoundaryType.Chunked;
            var asyncResult = new ResponseStreamAsyncResult(this, data, chunked, cancellationToken);
            uint bytesSent = 0;
            try
            {
                if (!started)
                {
                    statusCode = _requestContext.Response.SendHeaders(null, asyncResult, flags, false);
                    bytesSent = asyncResult.BytesSent;
                }
                else
                {
                    statusCode = HttpApi.HttpSendResponseEntityBody(
                        RequestQueueHandle,
                        RequestId,
                        (uint)flags,
                        asyncResult.DataChunkCount,
                        asyncResult.DataChunks,
                        &bytesSent,
                        IntPtr.Zero,
                        0,
                        asyncResult.NativeOverlapped,
                        IntPtr.Zero);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(LoggerEventIds.ErrorWhenFlushAsync, e, "FlushAsync");
                asyncResult.Dispose();
                Abort();
                throw;
            }

            if (statusCode != ErrorCodes.ERROR_SUCCESS && statusCode != ErrorCodes.ERROR_IO_PENDING)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogDebug(LoggerEventIds.WriteFlushCancelled,$"FlushAsync; Write cancelled with error code: {statusCode}");
                    asyncResult.Cancel(ThrowWriteExceptions);
                }
                else if (ThrowWriteExceptions)
                {
                    asyncResult.Dispose();
                    Exception exception = new IOException(string.Empty, new HttpSysException((int)statusCode));
                    Logger.LogError(LoggerEventIds.ErrorWhenFlushAsync, exception, "FlushAsync");
                    Abort();
                    throw exception;
                }
                else
                {
                    // Abort the request but do not close the stream, let future writes complete silently
                    Logger.LogDebug(LoggerEventIds.WriteErrorIgnored,$"FlushAsync; Ignored write exception: {statusCode}");
                    asyncResult.FailSilently();
                }
            }

            if (statusCode == ErrorCodes.ERROR_SUCCESS && HttpSysListener.SkipIOCPCallbackOnSuccess)
            {
                // IO operation completed synchronously - callback won't be called to signal completion.
                asyncResult.IOCompleted(statusCode, bytesSent);
            }

            // Last write, cache it for special cancellation handling.
            if ((flags & HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA) == 0)
            {
                _lastWrite = asyncResult;
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

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException(Resources.Exception_WriteOnlyStream);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException(Resources.Exception_WriteOnlyStream);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new InvalidOperationException(Resources.Exception_WriteOnlyStream);
        }

        #endregion

        internal void Abort(bool dispose = true)
        {
            if (dispose)
            {
                _disposed = true;
            }
            else
            {
                _skipWrites = true;
            }
            _requestContext.Abort();
        }

        private HttpApiTypes.HTTP_FLAGS ComputeLeftToWrite(long writeCount, bool endOfRequest = false)
        {
            var flags = HttpApiTypes.HTTP_FLAGS.NONE;
            if (!_requestContext.Response.HasComputedHeaders)
            {
                flags = _requestContext.Response.ComputeHeaders(writeCount, endOfRequest);
            }
            if (_leftToWrite == long.MinValue)
            {
                if (_requestContext.Request.IsHeadMethod)
                {
                    _leftToWrite = 0;
                }
                else if (_requestContext.Response.BoundaryType == BoundaryType.ContentLength)
                {
                    _leftToWrite = _requestContext.Response.ExpectedBodyLength;
                }
                else
                {
                    _leftToWrite = -1; // unlimited
                }
            }

            if (endOfRequest && _requestContext.Response.BoundaryType == BoundaryType.Close)
            {
                flags |= HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
            }
            else if (!endOfRequest
                && (_leftToWrite != writeCount || _requestContext.Response.TrailersExpected))
            {
                flags |= HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
            }

            // Update _leftToWrite now so we can queue up additional async writes.
            if (_leftToWrite > 0)
            {
                // keep track of the data transferred
                _leftToWrite -= writeCount;
            }
            if (_leftToWrite == 0 && !_requestContext.Response.TrailersExpected)
            {
                // in this case we already passed 0 as the flag, so we don't need to call HttpSendResponseEntityBody() when we Close()
                _requestContext.Response.MakeTrailersReadOnly();
                _disposed = true;
            }
            // else -1 unlimited

            return flags;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!RequestContext.AllowSynchronousIO)
            {
                throw new InvalidOperationException("Synchronous IO APIs are disabled, see AllowSynchronousIO.");
            }

            // Validates for null and bounds. Allows count == 0.
            // TODO: Verbose log parameters
            var data = new ArraySegment<byte>(buffer, offset, count);

            CheckDisposed();

            CheckWriteCount(count);

            FlushInternal(endOfRequest: false, data: data);
        }

        private void CheckWriteCount(long? count)
        {
            var contentLength = _requestContext.Response.ContentLength;
            // First write with more bytes written than the entire content-length
            if (!_requestContext.Response.HasComputedHeaders && contentLength < count)
            {
                throw new InvalidOperationException("More bytes written than specified in the Content-Length header.");
            }
            // A write in a response that has already started where the count exceeds the remainder of the content-length
            else if (_requestContext.Response.HasComputedHeaders && _requestContext.Response.BoundaryType == BoundaryType.ContentLength
                && _leftToWrite < count)
            {
                throw new InvalidOperationException("More bytes written than specified in the Content-Length header.");
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return WriteAsync(buffer, offset, count).ToIAsyncResult(callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
            ((Task)asyncResult).GetAwaiter().GetResult();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Validates for null and bounds. Allows count == 0.
            // TODO: Verbose log parameters
            var data = new ArraySegment<byte>(buffer, offset, count);
            CheckDisposed();

            CheckWriteCount(count);

            return FlushInternalAsync(data, cancellationToken);
        }

        internal async Task SendFileAsync(string fileName, long offset, long? count, CancellationToken cancellationToken)
        {
            // It's too expensive to validate the file attributes before opening the file. Open the file and then check the lengths.
            // This all happens inside of ResponseStreamAsyncResult.
            // TODO: Verbose log parameters
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }
            CheckDisposed();

            CheckWriteCount(count);

            // We can't mix await and unsafe so separate the unsafe code into another method.
            await SendFileAsyncCore(fileName, offset, count, cancellationToken);
        }

        internal unsafe Task SendFileAsyncCore(string fileName, long offset, long? count, CancellationToken cancellationToken)
        {
            if (_skipWrites)
            {
                return Task.CompletedTask;
            }

            var started = _requestContext.Response.HasStarted;
            if (count == 0 && started)
            {
                // No data to send and we've already sent the headers
                return Task.CompletedTask;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Abort(ThrowWriteExceptions);
                return Task.FromCanceled<int>(cancellationToken);
            }

            // We are setting buffer size to 1 to prevent FileStream from allocating it's internal buffer
            // It's too expensive to validate anything before opening the file. Open the file and then check the lengths.
            var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan); // Extremely expensive.

            try
            {
                var length = fileStream.Length; // Expensive, only do it once
                if (!count.HasValue)
                {
                    count = length - offset;
                }
                if (offset < 0 || offset > length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
                }
                if (count < 0 || count > length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
                }

                CheckWriteCount(count);
            }
            catch
            {
                fileStream.Dispose();
                throw;
            }

            // Make sure all validation is performed before this computes the headers
            var flags = ComputeLeftToWrite(count.Value);
            uint statusCode;
            uint bytesSent = 0;
            var chunked = _requestContext.Response.BoundaryType == BoundaryType.Chunked;
            var asyncResult = new ResponseStreamAsyncResult(this, fileStream, offset, count.Value, chunked, cancellationToken);

            try
            {
                if (!started)
                {
                    statusCode = _requestContext.Response.SendHeaders(null, asyncResult, flags, false);
                    bytesSent = asyncResult.BytesSent;
                }
                else
                {
                    // TODO: If opaque then include the buffer data flag.
                    statusCode = HttpApi.HttpSendResponseEntityBody(
                            RequestQueueHandle,
                            RequestId,
                            (uint)flags,
                            asyncResult.DataChunkCount,
                            asyncResult.DataChunks,
                            &bytesSent,
                            IntPtr.Zero,
                            0,
                            asyncResult.NativeOverlapped,
                            IntPtr.Zero);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(LoggerEventIds.FileSendAsyncError, e, "SendFileAsync");
                asyncResult.Dispose();
                Abort();
                throw;
            }

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogDebug(LoggerEventIds.FileSendAsyncCancelled,$"SendFileAsync; Write cancelled with error code: {statusCode}");
                    asyncResult.Cancel(ThrowWriteExceptions);
                }
                else if (ThrowWriteExceptions)
                {
                    asyncResult.Dispose();
                    var exception = new IOException(string.Empty, new HttpSysException((int)statusCode));
                    Logger.LogError(LoggerEventIds.FileSendAsyncError, exception, "SendFileAsync");
                    Abort();
                    throw exception;
                }
                else
                {
                    // Abort the request but do not close the stream, let future writes complete silently
                    Logger.LogDebug(LoggerEventIds.FileSendAsyncErrorIgnored,$"SendFileAsync; Ignored write exception: {statusCode}");
                    asyncResult.FailSilently();
                }
            }

            if (statusCode == ErrorCodes.ERROR_SUCCESS && HttpSysListener.SkipIOCPCallbackOnSuccess)
            {
                // IO operation completed synchronously - callback won't be called to signal completion.
                asyncResult.IOCompleted(statusCode, bytesSent);
            }

            // Last write, cache it for special cancellation handling.
            if ((flags & HttpApiTypes.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA) == 0)
            {
                _lastWrite = asyncResult;
            }

            return asyncResult.Task;
        }

        protected override unsafe void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    FlushInternal(endOfRequest: true);
                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        internal void SwitchToOpaqueMode()
        {
            _leftToWrite = -1;
        }

        // The final Content-Length async write can only be Canceled by CancelIoEx.
        // Sync can only be Canceled by CancelSynchronousIo, but we don't attempt this right now.
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification =
            "It is safe to ignore the return value on a cancel operation because the connection is being closed")]
        internal unsafe void CancelLastWrite()
        {
            ResponseStreamAsyncResult asyncState = _lastWrite;
            if (asyncState != null && !asyncState.IsCompleted)
            {
                UnsafeNclNativeMethods.CancelIoEx(RequestQueueHandle, asyncState.NativeOverlapped);
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
