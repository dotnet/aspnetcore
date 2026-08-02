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

        while (count > 0)
        {
            if (_charBufferCount == _charBufferSize)
            {
                FlushInternal(flushEncoder: false);
            }

            CopyToCharBuffer(values, ref index, ref count);
        }
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
        };
    }

    /// <inheritdoc/>
    public override void Write(string? value)
    {
        ThrowIfDisposed();

        if (value == null)
        {
            return;
        }

        var count = value.Length;
        var index = 0;
        while (count > 0)
        {
            if (_charBufferCount == _charBufferSize)
            {
                FlushInternal(flushEncoder: false);
            }

            CopyToCharBuffer(value, ref index, ref count);
        }
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

        if (values == null || count == 0)
        {
            return Task.CompletedTask;
        }

        var remaining = _charBufferSize - _charBufferCount;
        if (remaining >= count)
        {
            // Enough room in buffer, no need to go async
            CopyToCharBuffer(values, ref index, ref count);
            return Task.CompletedTask;
        }
        else
        {
            return WriteAsyncAwaited(values, index, count);
        }
    }

    private async Task WriteAsyncAwaited(char[] values, int index, int count)
    {
        Debug.Assert(count > 0);
        Debug.Assert(_charBufferSize - _charBufferCount < count);

        while (count > 0)
        {
            if (_charBufferCount == _charBufferSize)
            {
                await FlushInternalAsync(flushEncoder: false);
            }

            CopyToCharBuffer(values, ref index, ref count);
        }
    }

    /// <inheritdoc/>
    public override Task WriteAsync(string? value)
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        var remaining = _charBufferSize - _charBufferCount;
        if (remaining >= value.Length)
        {
            // Enough room in buffer, no need to go async
            CopyToCharBuffer(value);
            return Task.CompletedTask;
        }
        else
        {
            return WriteAsyncAwaited(value);
        }
    }

    private async Task WriteAsyncAwaited(string value)
    {
        var count = value.Length;

        Debug.Assert(count > 0);
        Debug.Assert(_charBufferSize - _charBufferCount < count);

        var index = 0;
        while (count > 0)
        {
            if (_charBufferCount == _charBufferSize)
            {
                await FlushInternalAsync(flushEncoder: false);
            }

            CopyToCharBuffer(value, ref index, ref count);
        }
    }

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
        };
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
            CopyToCharBuffer(NewLine);
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
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        if ((values == null || count == 0) && NewLine.Length == 0)
        {
            return Task.CompletedTask;
        }

        values ??= Array.Empty<char>();

        var remaining = _charBufferSize - _charBufferCount;
        if (remaining >= values.Length + NewLine.Length)
        {
            // Enough room in buffer, no need to go async
            CopyToCharBuffer(values, ref index, ref count);
            CopyToCharBuffer(NewLine);
            return Task.CompletedTask;
        }
        else
        {
            return WriteLineAsyncAwaited(values, index, count);
        }
    }

    private async Task WriteLineAsyncAwaited(char[] values, int index, int count)
    {
        await WriteAsync(values, index, count);
        await WriteAsync(NewLine);
    }

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

            CopyToCharBuffer(NewLine);

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
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        if (string.IsNullOrEmpty(value) && NewLine.Length == 0)
        {
            return Task.CompletedTask;
        }

        value ??= string.Empty;

        var remaining = _charBufferSize - _charBufferCount;
        if (remaining >= value.Length + NewLine.Length)
        {
            // Enough room in buffer, no need to go async
            CopyToCharBuffer(value);
            CopyToCharBuffer(NewLine);
            return Task.CompletedTask;
        }
        else
        {
            return WriteLineAsyncAwaited(value);
        }
    }

    private async Task WriteLineAsyncAwaited(string value)
    {
        await WriteAsync(value);
        await WriteAsync(NewLine);
    }

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

        var count = _encoder.GetBytes(
            _charBuffer,
            0,
            _charBufferCount,
            _byteBuffer,
            0,
            flush: flushEncoder);

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

        var count = _encoder.GetBytes(
            _charBuffer,
            0,
            _charBufferCount,
            _byteBuffer,
            0,
            flush: flushEncoder);

        _charBufferCount = 0;

        if (count > 0)
        {
            await _stream.WriteAsync(_byteBuffer.AsMemory(0, count));
        }
    }

    private void CopyToCharBuffer(string value)
    {
        Debug.Assert(_charBufferSize - _charBufferCount >= value.Length);

        value.CopyTo(
            sourceIndex: 0,
            destination: _charBuffer,
            destinationIndex: _charBufferCount,
            count: value.Length);

        _charBufferCount += value.Length;
    }

    private void CopyToCharBuffer(string value, ref int index, ref int count)
    {
        var remaining = Math.Min(_charBufferSize - _charBufferCount, count);

        value.CopyTo(
            sourceIndex: index,
            destination: _charBuffer,
            destinationIndex: _charBufferCount,
            count: remaining);

        _charBufferCount += remaining;
        index += remaining;
        count -= remaining;
    }

    private void CopyToCharBuffer(char[] values, ref int index, ref int count)
    {
        var remaining = Math.Min(_charBufferSize - _charBufferCount, count);

        Buffer.BlockCopy(
            src: values,
            srcOffset: index * sizeof(char),
            dst: _charBuffer,
            dstOffset: _charBufferCount * sizeof(char),
            count: remaining * sizeof(char));

        _charBufferCount += remaining;
        index += remaining;
        count -= remaining;
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
