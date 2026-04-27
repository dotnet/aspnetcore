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
    private readonly bool _isUtf8Encoding;

    private readonly byte[] _byteBuffer;
    private readonly char[] _charBuffer;

    private int _charBufferCount;
    private int _byteBufferCount;
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
        _isUtf8Encoding = ReferenceEquals(encoding, Encoding.UTF8) || encoding is UTF8Encoding;

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

    /// <summary>
    /// Writes pre-encoded UTF-8 bytes to the underlying stream, bypassing character encoding.
    /// </summary>
    /// <param name="utf8Value">The UTF-8 encoded bytes to write.</param>
    /// <remarks>
    /// This method buffers the raw bytes and flushes them to the underlying stream when the buffer is full
    /// or when <see cref="Flush"/> is called, similar to how character writes are buffered. Any pending
    /// character data is encoded into the byte buffer first to maintain correct write ordering. The writer's
    /// <see cref="Encoding"/> must be <see cref="System.Text.Encoding.UTF8"/> or a <see cref="UTF8Encoding"/>;
    /// otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The writer's encoding is not UTF-8.
    /// </exception>
    public void WriteUtf8(ReadOnlySpan<byte> utf8Value)
    {
        ThrowIfDisposed();
        ThrowIfNotUtf8Encoding();

        if (utf8Value.IsEmpty)
        {
            return;
        }

        // Encode pending chars into byte buffer to maintain write ordering
        if (_charBufferCount > 0)
        {
            FlushInternal(flushEncoder: true);
        }

        // Buffer the UTF-8 bytes
        BufferUtf8Bytes(utf8Value);
    }

    /// <summary>
    /// Asynchronously writes pre-encoded UTF-8 bytes to the underlying stream, bypassing character encoding.
    /// </summary>
    /// <param name="utf8Value">The UTF-8 encoded bytes to write.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <remarks>
    /// This method buffers the raw bytes and flushes them to the underlying stream when the buffer is full
    /// or when <see cref="FlushAsync"/> is called, similar to how character writes are buffered. Any pending
    /// character data is encoded into the byte buffer first to maintain correct write ordering. The writer's
    /// <see cref="Encoding"/> must be <see cref="System.Text.Encoding.UTF8"/> or a <see cref="UTF8Encoding"/>;
    /// otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The writer's encoding is not UTF-8.
    /// </exception>
    public Task WriteUtf8Async(ReadOnlyMemory<byte> utf8Value, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        ThrowIfNotUtf8Encoding();

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (utf8Value.IsEmpty)
        {
            return Task.CompletedTask;
        }

        // Fast path: no pending chars, bytes fit in remaining buffer space.
        // Just memcpy and return — no async state machine, no stream I/O.
        if (_charBufferCount == 0 && _byteBufferCount + utf8Value.Length <= _byteBuffer.Length)
        {
            utf8Value.Span.CopyTo(_byteBuffer.AsSpan(_byteBufferCount));
            _byteBufferCount += utf8Value.Length;
            return Task.CompletedTask;
        }

        // Second fast path: pending chars + UTF-8 bytes all fit in byte buffer.
        // Encode chars synchronously, then memcpy — still no async, no stream I/O.
        if (_charBufferCount > 0)
        {
            var maxBytesForChars = Encoding.GetMaxByteCount(_charBufferCount);
            if (_byteBufferCount + maxBytesForChars + utf8Value.Length <= _byteBuffer.Length)
            {
                var encodedCount = _encoder.GetBytes(
                    _charBuffer,
                    0,
                    _charBufferCount,
                    _byteBuffer,
                    _byteBufferCount,
                    flush: true);

                _charBufferCount = 0;
                _byteBufferCount += encodedCount;

                utf8Value.Span.CopyTo(_byteBuffer.AsSpan(_byteBufferCount));
                _byteBufferCount += utf8Value.Length;
                return Task.CompletedTask;
            }
        }

        return WriteUtf8AsyncCore(utf8Value, cancellationToken);
    }

    private Task WriteUtf8AsyncCore(ReadOnlyMemory<byte> utf8Value, CancellationToken cancellationToken)
    {
        // Encode pending chars into byte buffer (may flush byte buffer to stream if needed)
        if (_charBufferCount > 0)
        {
            var flushTask = FlushInternalAsync(flushEncoder: true);
            if (!flushTask.IsCompletedSuccessfully)
            {
                return WriteUtf8AsyncCoreAwaited(flushTask, utf8Value, cancellationToken);
            }
        }

        // Flush byte buffer if the new bytes don't fit
        if (_byteBufferCount > 0 && _byteBufferCount + utf8Value.Length > _byteBuffer.Length)
        {
            var pendingCount = _byteBufferCount;
            _byteBufferCount = 0;
            var writeTask = WriteToStreamAsync(_byteBuffer.AsMemory(0, pendingCount), cancellationToken);
            if (!writeTask.IsCompletedSuccessfully)
            {
                return BufferUtf8BytesAfterWriteAsync(writeTask, utf8Value, cancellationToken);
            }
        }

        // Buffer the bytes, or write directly if larger than the entire buffer
        if (utf8Value.Length <= _byteBuffer.Length - _byteBufferCount)
        {
            utf8Value.Span.CopyTo(_byteBuffer.AsSpan(_byteBufferCount));
            _byteBufferCount += utf8Value.Length;
        }
        else
        {
            // Large payload: write directly, bypassing buffer
            return WriteToStreamAsync(utf8Value, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private async Task WriteUtf8AsyncCoreAwaited(Task flushTask, ReadOnlyMemory<byte> utf8Value, CancellationToken cancellationToken)
    {
        await flushTask;
        await WriteUtf8AsyncCore(utf8Value, cancellationToken);
    }

    private async Task BufferUtf8BytesAfterWriteAsync(Task writeTask, ReadOnlyMemory<byte> utf8Value, CancellationToken cancellationToken)
    {
        await writeTask;
        await WriteUtf8AsyncCore(utf8Value, cancellationToken);
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        ThrowIfDisposed();

        FlushInternal(flushEncoder: true);
        FlushByteBuffer();
    }

    /// <inheritdoc/>
    public override Task FlushAsync()
    {
        if (_disposed)
        {
            return GetObjectDisposedTask();
        }

        return FlushAllAsync();
    }

    private async Task FlushAllAsync()
    {
        await FlushInternalAsync(flushEncoder: true);
        await FlushByteBufferAsync();
    }

    private void FlushByteBuffer()
    {
        if (_byteBufferCount > 0)
        {
            var count = _byteBufferCount;
            _byteBufferCount = 0;
            _stream.Write(_byteBuffer, 0, count);
        }
    }

    private Task FlushByteBufferAsync()
    {
        if (_byteBufferCount == 0)
        {
            return Task.CompletedTask;
        }

        return FlushByteBufferAsyncCore();
    }

    private Task FlushByteBufferAsyncCore()
    {
        var count = _byteBufferCount;
        _byteBufferCount = 0;
        return WriteToStreamAsync(_byteBuffer.AsMemory(0, count));
    }

    private void BufferUtf8Bytes(ReadOnlySpan<byte> utf8Value)
    {
        while (utf8Value.Length > 0)
        {
            var available = _byteBuffer.Length - _byteBufferCount;

            if (available == 0)
            {
                // Buffer full, flush to stream (reset count before write for exception safety)
                var count = _byteBufferCount;
                _byteBufferCount = 0;
                _stream.Write(_byteBuffer, 0, count);
                available = _byteBuffer.Length;
            }

            // Large payload: bypass buffer entirely
            if (_byteBufferCount == 0 && utf8Value.Length >= _byteBuffer.Length)
            {
                _stream.Write(utf8Value);
                return;
            }

            var toCopy = Math.Min(utf8Value.Length, available);
            utf8Value[..toCopy].CopyTo(_byteBuffer.AsSpan(_byteBufferCount));
            _byteBufferCount += toCopy;
            utf8Value = utf8Value.Slice(toCopy);
        }
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
                FlushByteBuffer();
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
                await FlushByteBufferAsync();
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
    // chunking. It encodes pending chars into the byte buffer at the current _byteBufferCount
    // offset, flushing the byte buffer to the stream first if needed to make room.
    private void FlushInternal(bool flushEncoder)
    {
        if (_charBufferCount == 0)
        {
            return;
        }

        // Check if the encoded chars will fit in the remaining byte buffer space
        var maxBytesNeeded = Encoding.GetMaxByteCount(_charBufferCount);
        if (_byteBufferCount + maxBytesNeeded > _byteBuffer.Length)
        {
            // Flush pending bytes to make room
            if (_byteBufferCount > 0)
            {
                var pendingCount = _byteBufferCount;
                _byteBufferCount = 0;
                _stream.Write(_byteBuffer, 0, pendingCount);
            }
        }

        var count = _encoder.GetBytes(
            _charBuffer,
            0,
            _charBufferCount,
            _byteBuffer,
            _byteBufferCount,
            flush: flushEncoder);

        _charBufferCount = 0;
        _byteBufferCount += count;
    }

    // Note: our FlushInternalAsync method does NOT flush the underlying stream. This would result in
    // chunking. It encodes pending chars into the byte buffer, with a sync fast path when the
    // encoded chars fit in the remaining byte buffer space (avoiding async state machine creation).
    private Task FlushInternalAsync(bool flushEncoder)
    {
        if (_charBufferCount == 0)
        {
            return Task.CompletedTask;
        }

        // Fast path: encoded chars fit in remaining byte buffer space — pure sync
        var maxBytesNeeded = Encoding.GetMaxByteCount(_charBufferCount);
        if (_byteBufferCount + maxBytesNeeded <= _byteBuffer.Length)
        {
            var count = _encoder.GetBytes(
                _charBuffer,
                0,
                _charBufferCount,
                _byteBuffer,
                _byteBufferCount,
                flush: flushEncoder);

            _charBufferCount = 0;
            _byteBufferCount += count;
            return Task.CompletedTask;
        }

        return FlushInternalAsyncCore(flushEncoder);
    }

    private Task FlushInternalAsyncCore(bool flushEncoder)
    {
        // Flush pending bytes to make room for encoded chars
        if (_byteBufferCount > 0)
        {
            var pendingCount = _byteBufferCount;
            _byteBufferCount = 0;
            var writeTask = WriteToStreamAsync(_byteBuffer.AsMemory(0, pendingCount));
            if (!writeTask.IsCompletedSuccessfully)
            {
                return FlushInternalAsyncCoreAwaited(writeTask, flushEncoder);
            }
        }

        EncodeCharBuffer(flushEncoder);

        return Task.CompletedTask;
    }

    private async Task FlushInternalAsyncCoreAwaited(Task writeTask, bool flushEncoder)
    {
        await writeTask;
        EncodeCharBuffer(flushEncoder);
    }

    private void EncodeCharBuffer(bool flushEncoder)
    {
        var count = _encoder.GetBytes(
            _charBuffer,
            0,
            _charBufferCount,
            _byteBuffer,
            _byteBufferCount,
            flush: flushEncoder);

        _charBufferCount = 0;
        _byteBufferCount += count;
    }

    private Task WriteToStreamAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var writeTask = _stream.WriteAsync(buffer, cancellationToken);
        return writeTask.IsCompletedSuccessfully ? Task.CompletedTask : writeTask.AsTask();
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

    private void ThrowIfNotUtf8Encoding()
    {
        if (!_isUtf8Encoding)
        {
            throw new InvalidOperationException($"WriteUtf8 requires a UTF-8 encoding, but the writer's encoding is '{Encoding.WebName}'.");
        }
    }
}
