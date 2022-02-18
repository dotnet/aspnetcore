// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Stream wrapper that creates a specific decompression stream if necessary.
/// </summary>
internal class RequestDecompressionBody : Stream
{
    private readonly IRequestDecompressionProvider _provider;
    private readonly Stream _innerStream;

    private readonly IDecompressionProvider? _decompressionProvider;
    private readonly Stream? _decompressionStream;
    private bool _complete;

    internal RequestDecompressionBody(HttpContext context, IRequestDecompressionProvider provider)
    {
        _provider = provider;
        _innerStream = context.Request.Body;

        _decompressionProvider = _provider.GetDecompressionProvider(context);
        _decompressionStream = _decompressionProvider?.CreateStream(_innerStream);
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => _innerStream.CanWrite;

    public override long Length
    {
        get { throw new NotSupportedException(); }
    }

    public override long Position
    {
        get { throw new NotSupportedException(); }
        set { throw new NotSupportedException(); }
    }

    public Stream Stream => this;

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_decompressionStream != null)
        {
            return _decompressionStream.Read(buffer, offset, count);
        }

        return _innerStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

    public override int EndRead(IAsyncResult asyncResult)
        => TaskToApm.End<int>(asyncResult);

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_decompressionStream != null)
        {
            return await _decompressionStream.ReadAsync(buffer, cancellationToken);
        }

        return await _innerStream.ReadAsync(buffer, cancellationToken);
    }

    public async Task CompleteAsync()
    {
        if (_complete)
        {
            return;
        }

        await FinishDecompressionAsync();
    }

    internal async Task FinishDecompressionAsync()
    {
        if (_complete)
        {
            return;
        }

        _complete = true;

        if (_decompressionStream != null)
        {
            await _decompressionStream.DisposeAsync();
        }
    }
}
