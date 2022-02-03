// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.TestHost;

/// <summary>
/// The client's view of the response body.
/// </summary>
internal class ResponseBodyReaderStream : Stream
{
    private bool _readerComplete;
    private bool _aborted;
    private Exception? _abortException;

    private readonly object _abortLock = new object();
    private readonly Action _abortRequest;
    private readonly Action _readComplete;
    private readonly Pipe _pipe;

    internal ResponseBodyReaderStream(Pipe pipe, Action abortRequest, Action readComplete)
    {
        _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
        _abortRequest = abortRequest ?? throw new ArgumentNullException(nameof(abortRequest));
        _readComplete = readComplete;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    #region NotSupported

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Flush() => throw new NotSupportedException();

    public override Task FlushAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    // Write with count 0 will still trigger OnFirstWrite
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

    #endregion NotSupported

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        VerifyBuffer(buffer, offset, count);

        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        CheckAborted();

        if (_readerComplete)
        {
            return 0;
        }

        using var registration = cancellationToken.Register(Cancel);
        var result = await _pipe.Reader.ReadAsync(cancellationToken);

        if (result.IsCanceled)
        {
            throw new OperationCanceledException();
        }

        if (result.Buffer.IsEmpty && result.IsCompleted)
        {
            _readComplete();
            _readerComplete = true;
            return 0;
        }

        var readableBuffer = result.Buffer;
        var actual = Math.Min(readableBuffer.Length, buffer.Length);
        readableBuffer = readableBuffer.Slice(0, actual);
        readableBuffer.CopyTo(buffer.Span);
        _pipe.Reader.AdvanceTo(readableBuffer.End);
        return (int)actual;
    }

    private static void VerifyBuffer(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }
        if (offset < 0 || offset > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
        }
        if (count <= 0 || count > buffer.Length - offset)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
        }
    }

    internal void Cancel()
    {
        Abort(new OperationCanceledException());
    }

    internal void Abort(Exception innerException)
    {
        Debug.Assert(innerException != null);

        lock (_abortLock)
        {
            _abortException = innerException;
            _aborted = true;
        }

        _pipe.Reader.CancelPendingRead();
    }

    private void CheckAborted()
    {
        lock (_abortLock)
        {
            if (_aborted)
            {
                throw new IOException(string.Empty, _abortException);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _abortRequest();
        }

        _pipe.Reader.Complete();

        base.Dispose(disposing);
    }
}
