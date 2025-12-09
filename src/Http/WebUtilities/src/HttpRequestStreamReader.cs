// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// A <see cref="TextReader"/> to read the HTTP request stream.
/// </summary>
public class HttpRequestStreamReader : TextReader
{
    private const int DefaultBufferSize = 1024;

    private readonly Stream _stream;
    private readonly Encoding _encoding;
    private readonly Decoder _decoder;

    private readonly ArrayPool<byte> _bytePool;
    private readonly ArrayPool<char> _charPool;

    private readonly int _byteBufferSize;
    private readonly byte[] _byteBuffer;
    private readonly char[] _charBuffer;

    private int _charBufferIndex;
    private int _charsRead;
    private int _bytesRead;

    private bool _isBlocked;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpRequestStreamReader"/>.
    /// </summary>
    /// <param name="stream">The HTTP request <see cref="Stream"/>.</param>
    /// <param name="encoding">The character encoding to use.</param>
    public HttpRequestStreamReader(Stream stream, Encoding encoding)
        : this(stream, encoding, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HttpRequestStreamReader"/>.
    /// </summary>
    /// <param name="stream">The HTTP request <see cref="Stream"/>.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <param name="bufferSize">The minimum buffer size.</param>
    public HttpRequestStreamReader(Stream stream, Encoding encoding, int bufferSize)
        : this(stream, encoding, bufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HttpRequestStreamReader"/>.
    /// </summary>
    /// <param name="stream">The HTTP request <see cref="Stream"/>.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <param name="bufferSize">The minimum buffer size.</param>
    /// <param name="bytePool">The byte array pool to use.</param>
    /// <param name="charPool">The char array pool to use.</param>
    public HttpRequestStreamReader(
        Stream stream,
        Encoding encoding,
        int bufferSize,
        ArrayPool<byte> bytePool,
        ArrayPool<char> charPool)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        _bytePool = bytePool ?? throw new ArgumentNullException(nameof(bytePool));
        _charPool = charPool ?? throw new ArgumentNullException(nameof(charPool));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
        if (!stream.CanRead)
        {
            throw new ArgumentException(Resources.HttpRequestStreamReader_StreamNotReadable, nameof(stream));
        }

        _byteBufferSize = bufferSize;

        _decoder = encoding.GetDecoder();
        _byteBuffer = _bytePool.Rent(bufferSize);

        try
        {
            var requiredLength = encoding.GetMaxCharCount(bufferSize);
            _charBuffer = _charPool.Rent(requiredLength);
        }
        catch
        {
            _bytePool.Return(_byteBuffer);

            if (_charBuffer != null)
            {
                _charPool.Return(_charBuffer);
            }

            throw;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;

            _bytePool.Return(_byteBuffer);
            _charPool.Return(_charBuffer);
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override int Peek()
    {
        ThrowIfDisposed();

        if (_charBufferIndex == _charsRead)
        {
            if (_isBlocked || ReadIntoBuffer() == 0)
            {
                return -1;
            }
        }

        return _charBuffer[_charBufferIndex];
    }

    /// <inheritdoc />
    public override int Read()
    {
        ThrowIfDisposed();

        if (_charBufferIndex == _charsRead)
        {
            if (ReadIntoBuffer() == 0)
            {
                return -1;
            }
        }

        return _charBuffer[_charBufferIndex++];
    }

    /// <inheritdoc />
    public override int Read(char[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (count < 0 || index + count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var span = new Span<char>(buffer, index, count);
        return Read(span);
    }

    /// <inheritdoc />
    public override int Read(Span<char> buffer)
    {
        ThrowIfDisposed();

        var count = buffer.Length;
        var charsRead = 0;
        while (count > 0)
        {
            var charsRemaining = _charsRead - _charBufferIndex;
            if (charsRemaining == 0)
            {
                charsRemaining = ReadIntoBuffer();
            }

            if (charsRemaining == 0)
            {
                break;  // We're at EOF
            }

            if (charsRemaining > count)
            {
                charsRemaining = count;
            }

            var source = new ReadOnlySpan<char>(_charBuffer, _charBufferIndex, charsRemaining);
            source.CopyTo(buffer);

            _charBufferIndex += charsRemaining;

            charsRead += charsRemaining;
            count -= charsRemaining;

            buffer = buffer.Slice(charsRemaining, count);

            // If we got back fewer chars than we asked for, then it's likely the underlying stream is blocked.
            // Send the data back to the caller so they can process it.
            if (_isBlocked)
            {
                break;
            }
        }

        return charsRead;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(char[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (count < 0 || index + count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var memory = new Memory<char>(buffer, index, count);
        return ReadAsync(memory).AsTask();
    }

    /// <inheritdoc />
    [SuppressMessage("ApiDesign", "RS0027:Public API with optional parameter(s) should have the most parameters amongst its public overloads.", Justification = "Required to maintain compatibility")]
    public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_charBufferIndex == _charsRead && await ReadIntoBufferAsync() == 0)
        {
            return 0;
        }

        var count = buffer.Length;

        var charsRead = 0;
        while (count > 0)
        {
            // n is the characters available in _charBuffer
            var charsRemaining = _charsRead - _charBufferIndex;

            // charBuffer is empty, let's read from the stream
            if (charsRemaining == 0)
            {
                _charsRead = 0;
                _charBufferIndex = 0;
                _bytesRead = 0;

                // We loop here so that we read in enough bytes to yield at least 1 char.
                // We break out of the loop if the stream is blocked (EOF is reached).
                do
                {
                    Debug.Assert(charsRemaining == 0);
                    _bytesRead = await _stream.ReadAsync(_byteBuffer.AsMemory(0, _byteBufferSize), cancellationToken);
                    if (_bytesRead == 0)  // EOF
                    {
                        _isBlocked = true;
                        break;
                    }

                    // _isBlocked == whether we read fewer bytes than we asked for.
                    _isBlocked = (_bytesRead < _byteBufferSize);

                    Debug.Assert(charsRemaining == 0);

                    _charBufferIndex = 0;
                    charsRemaining = _decoder.GetChars(
                        _byteBuffer,
                        0,
                        _bytesRead,
                        _charBuffer,
                        0);

                    Debug.Assert(charsRemaining > 0);

                    _charsRead += charsRemaining; // Number of chars in StreamReader's buffer.
                }
                while (charsRemaining == 0);

                if (charsRemaining == 0)
                {
                    break; // We're at EOF
                }
            }

            // Got more chars in charBuffer than the user requested
            if (charsRemaining > count)
            {
                charsRemaining = count;
            }

            var source = new Memory<char>(_charBuffer, _charBufferIndex, charsRemaining);
            source.CopyTo(buffer);

            _charBufferIndex += charsRemaining;

            charsRead += charsRemaining;
            count -= charsRemaining;

            buffer = buffer.Slice(charsRemaining, count);

            // This function shouldn't block for an indefinite amount of time,
            // or reading from a network stream won't work right.  If we got
            // fewer bytes than we requested, then we want to break right here.
            if (_isBlocked)
            {
                break;
            }
        }

        return charsRead;
    }

    /// <inheritdoc />
    public override async Task<string?> ReadLineAsync()
    {
        ThrowIfDisposed();

        StringBuilder? sb = null;
        var consumeLineFeed = false;

        while (true)
        {
            if (_charBufferIndex == _charsRead)
            {
                if (await ReadIntoBufferAsync() == 0)
                {
                    // reached EOF, we need to return null if we were at EOF from the beginning
                    return sb?.ToString();
                }
            }

            var stepResult = ReadLineStep(ref sb, ref consumeLineFeed);

            if (stepResult.Completed)
            {
                return stepResult.Result ?? sb?.ToString();
            }

            continue;
        }
    }

    // Reads a line. A line is defined as a sequence of characters followed by
    // a carriage return ('\r'), a line feed ('\n'), or a carriage return
    // immediately followed by a line feed. The resulting string does not
    // contain the terminating carriage return and/or line feed. The returned
    // value is null if the end of the input stream has been reached.
    /// <inheritdoc />
    public override string? ReadLine()
    {
        ThrowIfDisposed();

        StringBuilder? sb = null;
        var consumeLineFeed = false;

        while (true)
        {
            if (_charBufferIndex == _charsRead)
            {
                if (ReadIntoBuffer() == 0)
                {
                    // reached EOF, we need to return null if we were at EOF from the beginning
                    return sb?.ToString();
                }
            }

            var stepResult = ReadLineStep(ref sb, ref consumeLineFeed);

            if (stepResult.Completed)
            {
                return stepResult.Result ?? sb?.ToString();
            }
        }
    }

    private ReadLineStepResult ReadLineStep(ref StringBuilder? sb, ref bool consumeLineFeed)
    {
        const char carriageReturn = '\r';
        const char lineFeed = '\n';

        if (consumeLineFeed)
        {
            if (_charBuffer[_charBufferIndex] == lineFeed)
            {
                _charBufferIndex++;
            }
            return ReadLineStepResult.Done;
        }

        var span = new Span<char>(_charBuffer, _charBufferIndex, _charsRead - _charBufferIndex);

        var index = span.IndexOfAny(carriageReturn, lineFeed);

        if (index != -1)
        {
            if (span[index] == carriageReturn)
            {
                span = span.Slice(0, index);
                _charBufferIndex += index + 1;

                if (_charBufferIndex < _charsRead)
                {
                    // consume following line feed
                    if (_charBuffer[_charBufferIndex] == lineFeed)
                    {
                        _charBufferIndex++;
                    }

                    if (sb != null)
                    {
                        sb.Append(span);
                        return ReadLineStepResult.Done;
                    }

                    // perf: if the new line is found in first pass, we skip the StringBuilder
                    return ReadLineStepResult.FromResult(span.ToString());
                }

                // we where at the end of buffer, we need to read more to check for a line feed to consume
                sb ??= new StringBuilder();
                sb.Append(span);
                consumeLineFeed = true;
                return ReadLineStepResult.Continue;
            }

            if (span[index] == lineFeed)
            {
                span = span.Slice(0, index);
                _charBufferIndex += index + 1;

                if (sb != null)
                {
                    sb.Append(span);
                    return ReadLineStepResult.Done;
                }

                // perf: if the new line is found in first pass, we skip the StringBuilder
                return ReadLineStepResult.FromResult(span.ToString());
            }
        }

        sb ??= new StringBuilder();
        sb.Append(span);
        _charBufferIndex = _charsRead;

        return ReadLineStepResult.Continue;
    }

    private int ReadIntoBuffer()
    {
        _charsRead = 0;
        _charBufferIndex = 0;
        _bytesRead = 0;

        do
        {
            _bytesRead = _stream.Read(_byteBuffer, 0, _byteBufferSize);
            if (_bytesRead == 0)  // We're at EOF
            {
                return _charsRead;
            }

            _isBlocked = (_bytesRead < _byteBufferSize);
            _charsRead += _decoder.GetChars(
                _byteBuffer,
                0,
                _bytesRead,
                _charBuffer,
                _charsRead);
        }
        while (_charsRead == 0);

        return _charsRead;
    }

    private async Task<int> ReadIntoBufferAsync()
    {
        _charsRead = 0;
        _charBufferIndex = 0;
        _bytesRead = 0;

        do
        {
            _bytesRead = await _stream.ReadAsync(_byteBuffer.AsMemory(0, _byteBufferSize)).ConfigureAwait(false);
            if (_bytesRead == 0)
            {
                // We're at EOF
                return _charsRead;
            }

            // _isBlocked == whether we read fewer bytes than we asked for.
            _isBlocked = (_bytesRead < _byteBufferSize);

            _charsRead += _decoder.GetChars(
                _byteBuffer,
                0,
                _bytesRead,
                _charBuffer,
                _charsRead);
        }
        while (_charsRead == 0);

        return _charsRead;
    }

    /// <inheritdoc />
    public override async Task<string> ReadToEndAsync()
    {
        StringBuilder sb = new StringBuilder(_charsRead - _charBufferIndex);
        do
        {
            int tmpCharPos = _charBufferIndex;
            sb.Append(_charBuffer, tmpCharPos, _charsRead - tmpCharPos);
            _charBufferIndex = _charsRead;  // We consumed these characters
            await ReadIntoBufferAsync().ConfigureAwait(false);
        } while (_charsRead > 0);

        return sb.ToString();
    }

    private readonly struct ReadLineStepResult
    {
        public static readonly ReadLineStepResult Done = new ReadLineStepResult(true, null);
        public static readonly ReadLineStepResult Continue = new ReadLineStepResult(false, null);

        public static ReadLineStepResult FromResult(string value) => new ReadLineStepResult(true, value);

        private ReadLineStepResult(bool completed, string? result)
        {
            Completed = completed;
            Result = result;
        }

        public bool Completed { get; }
        public string? Result { get; }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
