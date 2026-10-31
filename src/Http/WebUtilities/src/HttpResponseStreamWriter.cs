// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// Writes to the HTTP response <see cref="Stream"/> using the supplied <see cref="System.Text.Encoding"/>.
/// It does not write the BOM and also does not close the stream.
/// </summary>
public class HttpResponseStreamWriter : TextWriter
{
    internal const int DefaultBufferSize = 16 * 1024;

    private readonly Stream _stream;
    private readonly Encoder _encoder;
    private readonly ArrayPool<byte> _bytePool;
    private readonly ArrayPool<char> _charPool;
    private readonly int _charBufferSize;

    private readonly byte[] _byteBuffer;
    private readonly char[] _charBuffer;

    private int _charBufferCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpResponseStreamWriter"/>.
    /// </summary>
    /// <param name="stream">The HTTP response <see cref="Stream"/>.</param>
    /// <param name="encoding">The character encoding to use.</param>
    public HttpResponseStreamWriter(Stream stream, Encoding encoding)
        : this(stream, encoding, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HttpResponseStreamWriter"/>.
    /// </summary>
    /// <param name="stream">The HTTP response <see cref="Stream"/>.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <param name="bufferSize">The minimum buffer size.</param>
    public HttpResponseStreamWriter(Stream stream, Encoding encoding, int bufferSize)
        : this(stream, encoding, bufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HttpResponseStreamWriter"/>.
    /// </summary>
    /// <param name="stream">The HTTP response <see cref="Stream"/>.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <param name="bufferSize">The minimum buffer size.</param>
    /// <param name="bytePool">The byte array pool.</param>
    /// <param name="charPool">The char array pool.</param>
    public HttpResponseStreamWriter(
        Stream stream,
        Encoding encoding,
        int bufferSize,
        ArrayPool<byte> bytePool,
        ArrayPool<char> charPool)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        _bytePool = bytePool ?? throw new ArgumentNullException(nameof(bytePool));
        _charPool = charPool ?? throw new ArgumentNullException(nameof(charPool));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
        if (!_stream.CanWrite)
        {
            throw new ArgumentException(Resources.HttpResponseStreamWriter_StreamNotWritable, nameof(stream));
        }

        _charBufferSize = bufferSize;

        _encoder = encoding.GetEncoder();
        _charBuffer = charPool.Rent(bufferSize);

        try
        {
            var requiredLength = encoding.GetMaxByteCount(bufferSize);
            _byteBuffer = bytePool.Rent(requiredLength);
        }
        catch
        {
            charPool.Return(_charBuffer);

            if (_byteBuffer != null)
            {
                bytePool.Return(_byteBuffer);
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public override Encoding Encoding { get; }

    /// <inheritdoc/>
    public override void Write(char value)
    {
        ThrowIfDisposed();

        if (_charBufferCount == _charBufferSize)
        {
            FlushInternal(flushEncoder: false);
        }

        _charBuffer[_charBufferCount] = value;
        _charBufferCount++;
    }

    /// <inheritdoc/>
    public override void Write(char[] values, int index, int count)
    {
        ThrowIfDisposed();

        if (values == null)
        {
            return;
        }

        Write(values.AsSpan(index, count));
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<char> value)
    {
        ThrowIfDisposed();

        var remaining = value.Length;
        while (remaining > 0)
        {
            if (_charBufferCount == _charBufferSize)
            {
                FlushInternal(flushEncoder: false);
            }

            var written = CopyToCharBuffer(value);

            remaining -= written;
            value = value.Slice(written);
        }
    }

    /// <inheritdoc/>
    public override void Write(string? value)
    {
        ThrowIfDisposed();

        Write(value.AsSpan());
    }

    /// <inheritdoc/>
    public override void WriteLine(ReadOnlySpan<char> value)
    {
        ThrowIfDisposed();

        Write(value);
        Write(NewLine);
    }

    /// <inheritdoc/>
    public override Task WriteAsync(char value)
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        if (_charBufferCount == _charBufferSize)
        {
            return WriteAsyncAwaited(value);
        }
        else
        {
            // Enough room in buffer, no need to go async
            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
            return Task.CompletedTask;
        }
    }

    private async Task WriteAsyncAwaited(char value)
    {
        Debug.Assert(_charBufferCount == _charBufferSize);

        await FlushInternalAsync(flushEncoder: false);

        _charBuffer[_charBufferCount] = value;
        _charBufferCount++;
    }

    /// <inheritdoc/>
    public override Task WriteAsync(char[] values, int index, int count)
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        if (values == null)
        {
            return Task.CompletedTask;
        }

        return WriteAsync(values.AsMemory(index, count));
    }

    /// <inheritdoc/>
    public override Task WriteAsync(string? value)
        => WriteAsync(value.AsMemory());

    /// <inheritdoc/>
    [SuppressMessage("ApiDesign", "RS0027:Public API with optional parameter(s) should have the most parameters amongst its public overloads.", Justification = "Required to maintain compatibility")]
    public override Task WriteAsync(ReadOnlyMemory<char> value, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (value.IsEmpty)
        {
            return Task.CompletedTask;
        }

        var remaining = _charBufferSize - _charBufferCount;
        if (remaining >= value.Length)
        {
            // Enough room in buffer, no need to go async
            CopyToCharBuffer(value.Span);
            return Task.CompletedTask;
        }
        else
        {
            return WriteAsyncAwaited(value);
        }
    }

    private async Task WriteAsyncAwaited(ReadOnlyMemory<char> value)
    {
        Debug.Assert(value.Length > 0);
        Debug.Assert(_charBufferSize - _charBufferCount < value.Length);

        var remaining = value.Length;
        while (remaining > 0)
        {
            if (_charBufferCount == _charBufferSize)
            {
                await FlushInternalAsync(flushEncoder: false);
            }

            var written = CopyToCharBuffer(value.Span);

            remaining -= written;
            value = value.Slice(written);
        }
    }

    /// <inheritdoc/>
    [SuppressMessage("ApiDesign", "RS0027:Public API with optional parameter(s) should have the most parameters amongst its public overloads.", Justification = "Required to maintain compatibility")]
    public override Task WriteLineAsync(ReadOnlyMemory<char> value, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (value.IsEmpty && NewLine.Length == 0)
        {
            return Task.CompletedTask;
        }

        var remaining = _charBufferSize - _charBufferCount;
        if (remaining >= value.Length + NewLine.Length)
        {
            // Enough room in buffer, no need to go async
            CopyToCharBuffer(value.Span);
            CopyToCharBuffer(NewLine.AsSpan());
            return Task.CompletedTask;
        }
        else
        {
            return WriteLineAsyncAwaited(value);
        }
    }

    private async Task WriteLineAsyncAwaited(ReadOnlyMemory<char> value)
    {
        await WriteAsync(value);
        await WriteAsync(NewLine);
    }

    /// <inheritdoc/>
    public override Task WriteLineAsync(char[] values, int index, int count)
        => WriteLineAsync(values.AsMemory(index, count));

    /// <inheritdoc/>
    public override Task WriteLineAsync(char value)
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        var remaining = _charBufferSize - _charBufferCount;
        if (remaining >= NewLine.Length + 1)
        {
            // Enough room in buffer, no need to go async
            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;

            CopyToCharBuffer(NewLine.AsSpan());

            return Task.CompletedTask;
        }
        else
        {
            return WriteLineAsyncAwaited(value);
        }
    }

    private async Task WriteLineAsyncAwaited(char value)
    {
        await WriteAsync(value);
        await WriteAsync(NewLine);
    }

    /// <inheritdoc/>
    public override Task WriteLineAsync(string? value)
        => WriteLineAsync(value.AsMemory());

    // We want to flush the stream when Flush/FlushAsync is explicitly
    // called by the user (example: from a Razor view).

    /// <inheritdoc/>
    public override void Flush()
    {
        ThrowIfDisposed();

        FlushInternal(flushEncoder: true);
    }

    /// <inheritdoc/>
    public override Task FlushAsync()
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        return FlushInternalAsync(flushEncoder: true);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            try
            {
                FlushInternal(flushEncoder: true);
            }
            finally
            {
                _bytePool.Return(_byteBuffer);
                _charPool.Return(_charBuffer);
            }
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            try
            {
                await FlushInternalAsync(flushEncoder: true);
            }
            finally
            {
                _bytePool.Return(_byteBuffer);
                _charPool.Return(_charBuffer);
            }
        }

        await base.DisposeAsync();
    }

    // Note: our FlushInternal method does NOT flush the underlying stream. This would result in
    // chunking.
    private void FlushInternal(bool flushEncoder)
    {
        if (_charBufferCount == 0)
        {
            return;
        }

        var count = _encoder.GetBytes(_charBuffer.AsSpan(0, _charBufferCount), _byteBuffer, flush: flushEncoder);

        _charBufferCount = 0;

        if (count > 0)
        {
            _stream.Write(_byteBuffer, 0, count);
        }
    }

    // Note: our FlushInternalAsync method does NOT flush the underlying stream. This would result in
    // chunking.
    private async Task FlushInternalAsync(bool flushEncoder)
    {
        if (_charBufferCount == 0)
        {
            return;
        }

        var count = _encoder.GetBytes(_charBuffer.AsSpan(0, _charBufferCount), _byteBuffer, flush: flushEncoder);

        _charBufferCount = 0;

        if (count > 0)
        {
            await _stream.WriteAsync(_byteBuffer.AsMemory(0, count));
        }
    }

    private int CopyToCharBuffer(ReadOnlySpan<char> value)
    {
        var remaining = Math.Min(_charBufferSize - _charBufferCount, value.Length);

        var source = value.Slice(0, remaining);
        var destination = new Span<char>(_charBuffer, _charBufferCount, remaining);
        source.CopyTo(destination);

        _charBufferCount += remaining;

        return remaining;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task GetObjectDisposedTask()
    {
        return Task.FromException(new ObjectDisposedException(nameof(HttpResponseStreamWriter)));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
