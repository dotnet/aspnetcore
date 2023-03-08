// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed class ResponseStream : Stream
{
    private readonly Stream _innerStream;
    private readonly Func<object?, Task> _onStart;
    private readonly object? _onStartContext;

    internal ResponseStream(Stream innerStream, Func<object?, Task> onStart, object? onStartContext)
    {
        _innerStream = innerStream;
        _onStartContext = onStartContext;
        _onStart = onStart;
    }

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => _innerStream.CanWrite;

    public override long Length => _innerStream.Length;

    public override long Position
    {
        get { return _innerStream.Position; }
        set { _innerStream.Position = value; }
    }

    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

    public override void SetLength(long value) => _innerStream.SetLength(value);

    public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _innerStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return _innerStream.EndRead(asyncResult);
    }
    public override void Flush()
    {
        _onStart(_onStartContext).GetAwaiter().GetResult();
        _innerStream.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _onStart(_onStartContext);
        await _innerStream.FlushAsync(cancellationToken);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var started = _onStart(_onStartContext);
        if (!started.IsCompletedSuccessfully)
        {   // either incomplete or faulted
            started.GetAwaiter().GetResult();
        }
        _innerStream.Write(buffer, offset, count);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        // elide async machinery if already started
        var started = _onStart(_onStartContext);
        return started.IsCompletedSuccessfully ? _innerStream.WriteAsync(buffer, cancellationToken) :
            Awaited(started, _innerStream, buffer, cancellationToken);

        static async ValueTask Awaited(Task started, Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            await started;
            await stream.WriteAsync(buffer, cancellationToken);
        }
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);

    public override void EndWrite(IAsyncResult asyncResult)
        => TaskToApm.End(asyncResult);
}
