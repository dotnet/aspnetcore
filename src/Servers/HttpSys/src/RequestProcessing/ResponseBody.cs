// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys;

#pragma warning disable CA1844 // Provide memory-based overrides of async methods when subclassing 'Stream'. Fixing this is too gnarly.
internal sealed partial class ResponseBody : Stream
#pragma warning restore CA1844
{
    private readonly RequestContext _requestContext;
    private long _leftToWrite = long.MinValue;
    private bool _skipWrites;
    private bool _disposed;

    // The last write needs special handling to cancel.
    private ResponseStreamAsyncResult? _lastWrite;

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

    internal bool EnableKernelResponseBuffering => RequestContext.Server.Options.EnableKernelResponseBuffering;

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

    public void MarkDelegated()
    {
        _skipWrites = true;
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
                Log.FewerBytesThanExpected(Logger);
            }
            _requestContext.Abort();
            return;
        }

        uint statusCode = 0;

        UnmanagedBufferAllocator allocator = new();
        Span<GCHandle> pinnedBuffers = default;
        try
        {
            BuildDataChunks(ref allocator, endOfRequest, data, out var dataChunks, out pinnedBuffers);
            if (!started)
            {
                statusCode = _requestContext.Response.SendHeaders(ref allocator, dataChunks, null, flags, false);
            }
            else
            {
                statusCode = PInvoke.HttpSendResponseEntityBody(
                    RequestQueueHandle,
                    RequestId,
                    flags,
                    dataChunks,
                    null,
                    null,
                    null);
            }
        }
        finally
        {
            FreeDataBuffers(pinnedBuffers);
            allocator.Dispose();
        }

        if (statusCode != ErrorCodes.ERROR_SUCCESS && statusCode != ErrorCodes.ERROR_HANDLE_EOF
            // Don't throw for disconnects, we were already finished with the response.
            && (!endOfRequest || (statusCode != ErrorCodes.ERROR_CONNECTION_INVALID && statusCode != ErrorCodes.ERROR_INVALID_PARAMETER)))
        {
            if (ThrowWriteExceptions)
            {
                var exception = new IOException(string.Empty, new HttpSysException((int)statusCode));
                Log.WriteError(Logger, exception);
                Abort();
                throw exception;
            }
            else
            {
                // Abort the request but do not close the stream, let future writes complete silently
                Log.WriteErrorIgnored(Logger, statusCode);
                Abort(dispose: false);
            }
        }
    }

    private unsafe void BuildDataChunks(scoped ref UnmanagedBufferAllocator allocator, bool endOfRequest, ArraySegment<byte> data, out Span<HTTP_DATA_CHUNK> dataChunks, out Span<GCHandle> pins)
    {
        var hasData = data.Count > 0;
        var chunked = _requestContext.Response.BoundaryType == BoundaryType.Chunked;
        var addTrailers = endOfRequest && _requestContext.Response.HasTrailers;
        Debug.Assert(!(addTrailers && chunked), "Trailers aren't currently supported for HTTP/1.1 chunking.");

        int pinsIndex = 0;
        var currentChunk = 0;
        // Figure out how many data chunks
        if (chunked && !hasData && endOfRequest)
        {
            dataChunks = allocator.AllocAsSpan<HTTP_DATA_CHUNK>(1);
            SetDataChunkWithPinnedData(dataChunks, ref currentChunk, Helpers.ChunkTerminator);
            pins = default;
            return;
        }
        else if (!hasData && !addTrailers)
        {
            // No data
            dataChunks = default;
            pins = default;
            return;
        }

        // Recompute chunk count based on presence of data.
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

        // We know the max pin count.
        pins = allocator.AllocAsSpan<GCHandle>(2);

        // Manually initialize the allocated GCHandles
        pins.Clear();

        dataChunks = allocator.AllocAsSpan<HTTP_DATA_CHUNK>(chunkCount);

        if (chunked)
        {
            var chunkHeaderBuffer = Helpers.GetChunkHeader(data.Count);
            SetDataChunk(dataChunks, ref currentChunk, chunkHeaderBuffer, out pins[pinsIndex++]);
        }

        if (hasData)
        {
            SetDataChunk(dataChunks, ref currentChunk, data, out pins[pinsIndex++]);
        }

        if (chunked)
        {
            SetDataChunkWithPinnedData(dataChunks, ref currentChunk, Helpers.CRLF);

            if (endOfRequest)
            {
                SetDataChunkWithPinnedData(dataChunks, ref currentChunk, Helpers.ChunkTerminator);
            }
        }

        if (addTrailers)
        {
            _requestContext.Response.SerializeTrailers(ref allocator, out dataChunks[currentChunk++]);
        }
        else if (endOfRequest)
        {
            _requestContext.Response.MakeTrailersReadOnly();
        }

        Debug.Assert(currentChunk == dataChunks.Length, "All chunks should be accounted for");
    }

    private static unsafe void SetDataChunk(
        Span<HTTP_DATA_CHUNK> chunks,
        ref int chunkIndex,
        ArraySegment<byte> buffer,
        out GCHandle handle)
    {
        handle = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
        SetDataChunkWithPinnedData(chunks, ref chunkIndex, new ReadOnlySpan<byte>((void*)(handle.AddrOfPinnedObject() + buffer.Offset), buffer.Count));
    }

    private static unsafe void SetDataChunkWithPinnedData(
        Span<HTTP_DATA_CHUNK> chunks,
        ref int chunkIndex,
        ReadOnlySpan<byte> bytes)
    {
        ref var chunk = ref chunks[chunkIndex++];
        chunk.DataChunkType = HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
        fixed (byte* ptr = bytes)
        {
            chunk.Anonymous.FromMemory.pBuffer = ptr;
        }
        chunk.Anonymous.FromMemory.BufferLength = (uint)bytes.Length;
    }

    private static void FreeDataBuffers(Span<GCHandle> pinnedBuffers)
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
        uint statusCode;
        var chunked = _requestContext.Response.BoundaryType == BoundaryType.Chunked;
        var asyncResult = new ResponseStreamAsyncResult(this, data, chunked, cancellationToken);
        uint bytesSent = 0;

        UnmanagedBufferAllocator allocator = new();
        try
        {
            if (!started)
            {
                statusCode = _requestContext.Response.SendHeaders(ref allocator, null, asyncResult, flags, false);
                bytesSent = asyncResult.BytesSent;
            }
            else
            {
                statusCode = HttpApi.HttpSendResponseEntityBody(
                    RequestQueueHandle,
                    RequestId,
                    flags,
                    asyncResult.DataChunkCount,
                    asyncResult.DataChunks,
                    &bytesSent,
                    IntPtr.Zero,
                    0,
                    asyncResult.NativeOverlapped!,
                    IntPtr.Zero);
            }
        }
        catch (Exception e)
        {
            Log.ErrorWhenFlushAsync(Logger, e);
            asyncResult.Dispose();
            Abort();
            throw;
        }
        finally
        {
            allocator.Dispose();
        }

        if (statusCode != ErrorCodes.ERROR_SUCCESS && statusCode != ErrorCodes.ERROR_IO_PENDING)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Log.WriteFlushCancelled(Logger, statusCode);
                asyncResult.Cancel(ThrowWriteExceptions);
            }
            else if (ThrowWriteExceptions)
            {
                asyncResult.Dispose();
                Exception exception = new IOException(string.Empty, new HttpSysException((int)statusCode));
                Log.ErrorWhenFlushAsync(Logger, exception);
                Abort();
                throw exception;
            }
            else
            {
                // Abort the request but do not close the stream, let future writes complete silently
                Log.WriteErrorIgnored(Logger, statusCode);
                asyncResult.FailSilently();
            }
        }

        if (statusCode == ErrorCodes.ERROR_SUCCESS && HttpSysListener.SkipIOCPCallbackOnSuccess)
        {
            // IO operation completed synchronously - callback won't be called to signal completion.
            asyncResult.IOCompleted(statusCode);
        }

        // Last write, cache it for special cancellation handling.
        if ((flags & PInvoke.HTTP_SEND_RESPONSE_FLAG_MORE_DATA) == 0)
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

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
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

    private uint ComputeLeftToWrite(long writeCount, bool endOfRequest = false)
    {
        var flags = 0u;
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
            flags |= PInvoke.HTTP_SEND_RESPONSE_FLAG_DISCONNECT;
        }
        else if (!endOfRequest
            && (_leftToWrite != writeCount || _requestContext.Response.TrailersExpected))
        {
            flags |= PInvoke.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
        }
        if (EnableKernelResponseBuffering)
        {
            // "When this flag is set, it should also be used consistently in calls to the HttpSendResponseEntityBody function."
            // so: make sure we add it in *all* scenarios where it applies - our "close" could be at the end of a bunch
            // of buffered chunks
            flags |= PInvoke.HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA;
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
        ValidateBufferArguments(buffer, offset, count);

        if (!RequestContext.AllowSynchronousIO)
        {
            throw new InvalidOperationException("Synchronous IO APIs are disabled, see AllowSynchronousIO.");
        }

        if (count == 0 && _requestContext.Response.HasStarted)
        {
            // avoid trivial writes (unless we haven't started the response yet)
            // note that this precedes disposal check, since writing the last bytes
            // may have completed the response (marking us disposed) - a trailing
            // empty write is *not* harmful
            return;
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

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        ArgumentNullException.ThrowIfNull(asyncResult);

        TaskToApm.End(asyncResult);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        if (count == 0 && _requestContext.Response.HasStarted)
        {
            // avoid trivial writes (unless we haven't started the response yet)
            // note that this precedes disposal check, since writing the last bytes
            // may have completed the response (marking us disposed) - a trailing
            // empty write is *not* harmful
            return Task.CompletedTask;
        }

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
        ArgumentException.ThrowIfNullOrEmpty(fileName);
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

        UnmanagedBufferAllocator allocator = new();
        try
        {
            if (!started)
            {
                statusCode = _requestContext.Response.SendHeaders(ref allocator, null, asyncResult, flags, false);
                bytesSent = asyncResult.BytesSent;
            }
            else
            {
                // TODO: If opaque then include the buffer data flag.
                statusCode = HttpApi.HttpSendResponseEntityBody(
                        RequestQueueHandle,
                        RequestId,
                        flags,
                        asyncResult.DataChunkCount,
                        asyncResult.DataChunks,
                        &bytesSent,
                        IntPtr.Zero,
                        0,
                        asyncResult.NativeOverlapped!,
                        IntPtr.Zero);
            }
        }
        catch (Exception e)
        {
            Log.FileSendAsyncError(Logger, e);
            asyncResult.Dispose();
            Abort();
            throw;
        }
        finally
        {
            allocator.Dispose();
        }

        if (statusCode != ErrorCodes.ERROR_SUCCESS && statusCode != ErrorCodes.ERROR_IO_PENDING)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Log.FileSendAsyncCancelled(Logger, statusCode);
                asyncResult.Cancel(ThrowWriteExceptions);
            }
            else if (ThrowWriteExceptions)
            {
                asyncResult.Dispose();
                var exception = new IOException(string.Empty, new HttpSysException((int)statusCode));
                Log.FileSendAsyncError(Logger, exception);
                Abort();
                throw exception;
            }
            else
            {
                // Abort the request but do not close the stream, let future writes complete
                Log.FileSendAsyncErrorIgnored(Logger, statusCode);
                asyncResult.FailSilently();
            }
        }

        if (statusCode == ErrorCodes.ERROR_SUCCESS && HttpSysListener.SkipIOCPCallbackOnSuccess)
        {
            // IO operation completed synchronously - callback won't be called to signal completion.
            asyncResult.IOCompleted(statusCode);
        }

        // Last write, cache it for special cancellation handling.
        if ((flags & PInvoke.HTTP_SEND_RESPONSE_FLAG_MORE_DATA) == 0)
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
    internal unsafe void CancelLastWrite()
    {
        ResponseStreamAsyncResult? asyncState = _lastWrite;
        if (asyncState != null && !asyncState.IsCompleted)
        {
            HttpApi.CancelIoEx(RequestQueueHandle, asyncState.NativeOverlapped!);
        }
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.FewerBytesThanExpected, LogLevel.Error, "ResponseStream::Dispose; Fewer bytes were written than were specified in the Content-Length.", EventName = "FewerBytesThanExpected")]
        public static partial void FewerBytesThanExpected(ILogger logger);

        [LoggerMessage(LoggerEventIds.WriteError, LogLevel.Error, "Flush", EventName = "WriteError")]
        public static partial void WriteError(ILogger logger, IOException exception);

        [LoggerMessage(LoggerEventIds.WriteErrorIgnored, LogLevel.Debug, "Flush; Ignored write exception: {StatusCode}", EventName = "WriteFlushedIgnored")]
        public static partial void WriteErrorIgnored(ILogger logger, uint statusCode);

        [LoggerMessage(LoggerEventIds.ErrorWhenFlushAsync, LogLevel.Debug, "FlushAsync", EventName = "ErrorWhenFlushAsync")]
        public static partial void ErrorWhenFlushAsync(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.WriteFlushCancelled, LogLevel.Debug, "FlushAsync; Write cancelled with error code: {StatusCode}", EventName = "WriteFlushCancelled")]
        public static partial void WriteFlushCancelled(ILogger logger, uint statusCode);

        [LoggerMessage(LoggerEventIds.FileSendAsyncError, LogLevel.Error, "SendFileAsync", EventName = "FileSendAsyncError")]
        public static partial void FileSendAsyncError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.FileSendAsyncCancelled, LogLevel.Debug, "SendFileAsync; Write cancelled with error code: {StatusCode}", EventName = "FileSendAsyncCancelled")]
        public static partial void FileSendAsyncCancelled(ILogger logger, uint statusCode);

        [LoggerMessage(LoggerEventIds.FileSendAsyncErrorIgnored, LogLevel.Debug, "SendFileAsync; Ignored write exception: {StatusCode}", EventName = "FileSendAsyncErrorIgnored")]
        public static partial void FileSendAsyncErrorIgnored(ILogger logger, uint statusCode);
    }
}
