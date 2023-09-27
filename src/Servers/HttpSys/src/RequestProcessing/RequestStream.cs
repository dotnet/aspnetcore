// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Windows.Win32;

namespace Microsoft.AspNetCore.Server.HttpSys;

#pragma warning disable CA1844 // Provide memory-based overrides of async methods when subclassing 'Stream'. Fixing this is too gnarly.
internal sealed partial class RequestStream : Stream
#pragma warning restore CA1844 // Provide memory-based overrides of async methods when subclassing 'Stream'
{
    private const int MaxReadSize = 0x20000; // http.sys recommends we limit reads to 128k

    private readonly RequestContext _requestContext;
    private uint _dataChunkOffset;
    private int _dataChunkIndex;
    private long? _maxSize;
    private long _totalRead;
    private bool _closed;

    internal RequestStream(RequestContext httpContext)
    {
        _requestContext = httpContext;
        _maxSize = _requestContext.Server.Options.MaxRequestBodySize;
    }

    internal RequestContext RequestContext
    {
        get { return _requestContext; }
    }

    private SafeHandle RequestQueueHandle => RequestContext.Server.RequestQueue.Handle;

    private ulong RequestId => RequestContext.Request.RequestId;

    private ILogger Logger => RequestContext.Server.Logger;

    public bool HasStarted { get; private set; }

    public long? MaxSize
    {
        get => _maxSize;
        set
        {
            if (HasStarted)
            {
                throw new InvalidOperationException("The maximum request size cannot be changed after the request body has started reading.");
            }
            if (value.HasValue && value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be greater or equal to zero.");
            }
            _maxSize = value;
        }
    }

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override bool CanRead => true;

    public override long Length => throw new NotSupportedException(Resources.Exception_NoSeek);

    public override long Position
    {
        get => throw new NotSupportedException(Resources.Exception_NoSeek);
        set => throw new NotSupportedException(Resources.Exception_NoSeek);
    }

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException(Resources.Exception_NoSeek);

    public override void SetLength(long value) => throw new NotSupportedException(Resources.Exception_NoSeek);

    public override void Flush() => throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);

    public override Task FlushAsync(CancellationToken cancellationToken)
        => throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);

    internal void SwitchToOpaqueMode()
    {
        HasStarted = true;
        _maxSize = null;
    }

    internal void Abort()
    {
        _closed = true;
        _requestContext.Abort();
    }

    public override unsafe int Read([In, Out] byte[] buffer, int offset, int size)
    {
        if (!RequestContext.AllowSynchronousIO)
        {
            throw new InvalidOperationException("Synchronous IO APIs are disabled, see AllowSynchronousIO.");
        }

        ValidateBufferArguments(buffer, offset, size);
        CheckSizeLimit();
        if (_closed)
        {
            return 0;
        }
        // TODO: Verbose log parameters

        uint dataRead = 0;

        if (_dataChunkIndex != -1)
        {
            dataRead = _requestContext.Request.GetChunks(ref _dataChunkIndex, ref _dataChunkOffset, buffer, offset, size);
        }

        if (_dataChunkIndex == -1 && dataRead == 0)
        {
            uint statusCode = 0;
            uint extraDataRead = 0;

            // the http.sys team recommends that we limit the size to 128kb
            if (size > MaxReadSize)
            {
                size = MaxReadSize;
            }

            fixed (byte* pBuffer = buffer)
            {
                // issue unmanaged blocking call

                uint flags = 0;

                statusCode = PInvoke.HttpReceiveRequestEntityBody(
                    RequestQueueHandle,
                    RequestId,
                    flags,
                    (pBuffer + offset),
                    (uint)size,
                    &extraDataRead,
                    default);

                dataRead += extraDataRead;
            }

            // Zero-byte reads
            if (statusCode == ErrorCodes.ERROR_MORE_DATA && size == 0)
            {
                // extraDataRead returns 1 to let us know there's data available. Don't count it against the request body size yet.
                dataRead = 0;
            }
            else if (statusCode != ErrorCodes.ERROR_SUCCESS && statusCode != ErrorCodes.ERROR_HANDLE_EOF)
            {
                Exception exception = new IOException(string.Empty, new HttpSysException((int)statusCode));
                Log.ErrorWhileRead(Logger, exception);
                Abort();
                throw exception;
            }
            UpdateAfterRead(statusCode, dataRead);
        }
        if (TryCheckSizeLimit((int)dataRead, out var ex))
        {
            throw ex;
        }

        // TODO: Verbose log dump data read
        return (int)dataRead;
    }

    internal void UpdateAfterRead(uint statusCode, uint dataRead)
    {
        if (statusCode == ErrorCodes.ERROR_HANDLE_EOF
            || statusCode != ErrorCodes.ERROR_MORE_DATA && dataRead == 0)
        {
            Dispose();
        }
    }

    public override unsafe IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback? callback, object? state)
        => TaskToApm.Begin(ReadAsync(buffer, offset, size, CancellationToken.None), callback, state);

    public override int EndRead(IAsyncResult asyncResult)
        => TaskToApm.End<int>(asyncResult);

    public override unsafe Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, size);
        CheckSizeLimit();
        if (_closed)
        {
            return Task.FromResult<int>(0);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<int>(cancellationToken);
        }
        // TODO: Verbose log parameters

        RequestStreamAsyncResult? asyncResult = null;

        uint dataRead = 0;
        if (_dataChunkIndex != -1)
        {
            dataRead = _requestContext.Request.GetChunks(ref _dataChunkIndex, ref _dataChunkOffset, buffer, offset, size);
            if (dataRead > 0)
            {
                UpdateAfterRead(ErrorCodes.ERROR_SUCCESS, dataRead);
                if (TryCheckSizeLimit((int)dataRead, out var exception))
                {
                    return Task.FromException<int>(exception);
                }
                // TODO: Verbose log #dataRead
                return Task.FromResult<int>((int)dataRead);
            }
        }

        uint statusCode = 0;
        offset += (int)dataRead;
        size -= (int)dataRead;

        // the http.sys team recommends that we limit the size to 128kb
        if (size > MaxReadSize)
        {
            size = MaxReadSize;
        }

        var cancellationRegistration = default(CancellationTokenRegistration);
        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = RequestContext.RegisterForCancellation(cancellationToken);
        }

        asyncResult = new RequestStreamAsyncResult(this, null, null, buffer, offset, size, dataRead, cancellationRegistration);
        uint bytesReturned;

        try
        {
            uint flags = 0;

            statusCode =
                HttpApi.HttpReceiveRequestEntityBody(
                    RequestQueueHandle,
                    RequestId,
                    flags,
                    asyncResult.PinnedBuffer,
                    (uint)size,
                    out bytesReturned,
                    asyncResult.NativeOverlapped!);
        }
        catch (Exception e)
        {
            asyncResult.Dispose();
            Abort();
            Log.ErrorWhenReadAsync(Logger, e);
            throw;
        }

        if (statusCode != ErrorCodes.ERROR_SUCCESS && statusCode != ErrorCodes.ERROR_IO_PENDING)
        {
            asyncResult.Dispose();
            if (statusCode == ErrorCodes.ERROR_HANDLE_EOF)
            {
                uint totalRead = dataRead + bytesReturned;
                UpdateAfterRead(statusCode, totalRead);
                if (TryCheckSizeLimit((int)totalRead, out var exception))
                {
                    return Task.FromException<int>(exception);
                }
                // TODO: Verbose log totalRead
                return Task.FromResult<int>((int)totalRead);
            }
            else
            {
                Exception exception = new IOException(string.Empty, new HttpSysException((int)statusCode));
                Log.ErrorWhenReadAsync(Logger, exception);
                Abort();
                throw exception;
            }
        }
        else if (statusCode == ErrorCodes.ERROR_SUCCESS &&
                    HttpSysListener.SkipIOCPCallbackOnSuccess)
        {
            // IO operation completed synchronously - callback won't be called to signal completion.
            asyncResult.Dispose();
            uint totalRead = dataRead + bytesReturned;
            UpdateAfterRead(statusCode, totalRead);
            if (TryCheckSizeLimit((int)totalRead, out var exception))
            {
                return Task.FromException<int>(exception);
            }
            // TODO: Verbose log
            return Task.FromResult<int>((int)totalRead);
        }
        return asyncResult.Task;
    }

    public override void Write(byte[] buffer, int offset, int size)
    {
        throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback? callback, object? state)
    {
        throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
    }

    // Called before each read
    private void CheckSizeLimit()
    {
        // Note SwitchToOpaqueMode sets HasStarted and clears _maxSize, so these limits don't apply.
        if (!HasStarted)
        {
            var contentLength = RequestContext.Request.ContentLength;
            if (contentLength.HasValue && _maxSize.HasValue && contentLength.Value > _maxSize.Value)
            {
                throw new BadHttpRequestException(
                    $"The request's Content-Length {contentLength.Value} is larger than the request body size limit {_maxSize.Value}.",
                    StatusCodes.Status413PayloadTooLarge);
            }

            HasStarted = true;
        }
        else if (TryCheckSizeLimit(0, out var exception))
        {
            throw exception;
        }
    }

    // Called after each read.
    internal bool TryCheckSizeLimit(int bytesRead, [NotNullWhen(true)] out Exception? exception)
    {
        _totalRead += bytesRead;
        if (_maxSize.HasValue && _totalRead > _maxSize.Value)
        {
            exception = new BadHttpRequestException(
                $"The total number of bytes read {_totalRead} has exceeded the request body size limit {_maxSize.Value}.",
                StatusCodes.Status413PayloadTooLarge);
            return true;
        }
        exception = null;
        return false;
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
}
