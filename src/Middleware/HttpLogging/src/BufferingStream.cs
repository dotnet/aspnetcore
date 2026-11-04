// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging;

internal abstract class BufferingStream : Stream, IBufferWriter<byte>
{
    private const int MinimumBufferSize = 4096; // 4K
    protected int _bytesBuffered;
    private BufferSegment? _head;
    private BufferSegment? _tail;
    protected Memory<byte> _tailMemory; // remainder of tail memory
    protected int _tailBytesBuffered;
    protected ILogger _logger;
    protected Stream _innerStream;

    public BufferingStream(Stream innerStream, ILogger logger)
    {
        _logger = logger;
        _innerStream = innerStream;
    }

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanWrite => _innerStream.CanWrite;

    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override int WriteTimeout
    {
        get => _innerStream.WriteTimeout;
        set => _innerStream.WriteTimeout = value;
    }

    public string GetString(Encoding encoding)
    {
        try
        {
            if (_head == null || _tail == null)
            {
                // nothing written
                return "";
            }

            // Only place where we are actually using the buffered data.
            // update tail here.
            _tail.End = _tailBytesBuffered;

            var ros = new ReadOnlySequence<byte>(_head, 0, _tail, _tailBytesBuffered);

            var bufferWriter = new ArrayBufferWriter<char>();

            var decoder = encoding.GetDecoder();
            // First calls convert on the entire ReadOnlySequence, with flush: false.
            // flush: false is required as we don't want to write invalid characters that
            // are spliced due to truncation. If we set flush: true, if effectively means
            // we expect EOF in this array, meaning it will try to write any bytes at the end of it.
            EncodingExtensions.Convert(decoder, ros, bufferWriter, flush: false, out var charUsed, out var completed);

            // Afterwards, we need to call convert in a loop until complete is true.
            // The first call to convert many return true, but if it doesn't, we call
            // Convert with a empty ReadOnlySequence and flush: true until we get completed: true.

            // This should never infinite due to the contract for decoders.
            // But for safety, call this only 10 times, throwing a decode failure if it fails.
            for (var i = 0; i < 10; i++)
            {
                if (completed)
                {
                    return new string(bufferWriter.WrittenSpan);
                }
                else
                {
                    EncodingExtensions.Convert(decoder, ReadOnlySequence<byte>.Empty, bufferWriter, flush: true, out charUsed, out completed);
                }
            }

            throw new DecoderFallbackException("Failed to decode after 10 calls to Decoder.Convert");
        }
        catch (DecoderFallbackException ex)
        {
            _logger.DecodeFailure(ex);
            return "<Decoder failure>";
        }
        finally
        {
            Reset();
        }
    }

    public void Advance(int bytes)
    {
        if ((uint)bytes > (uint)_tailMemory.Length)
        {
            ThrowArgumentOutOfRangeException(nameof(bytes));
        }

        _tailBytesBuffered += bytes;
        _bytesBuffered += bytes;
        _tailMemory = _tailMemory.Slice(bytes);
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        AllocateMemory(sizeHint);
        return _tailMemory;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        AllocateMemory(sizeHint);
        return _tailMemory.Span;
    }

    private void AllocateMemory(int sizeHint)
    {
        if (_head is null)
        {
            // We need to allocate memory to write since nobody has written before
            var newSegment = AllocateSegment(sizeHint);

            // Set all the pointers
            _head = _tail = newSegment;
            _tailBytesBuffered = 0;
        }
        else
        {
            var bytesLeftInBuffer = _tailMemory.Length;

            if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < sizeHint)
            {
                Debug.Assert(_tail != null);

                if (_tailBytesBuffered > 0)
                {
                    // Flush buffered data to the segment
                    _tail.End += _tailBytesBuffered;
                    _tailBytesBuffered = 0;
                }

                var newSegment = AllocateSegment(sizeHint);

                _tail.SetNext(newSegment);
                _tail = newSegment;
            }
        }
    }

    private BufferSegment AllocateSegment(int sizeHint)
    {
        var newSegment = CreateSegment();

        // We can't use the recommended pool so use the ArrayPool
        newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(GetSegmentSize(sizeHint)));

        _tailMemory = newSegment.AvailableMemory;

        return newSegment;
    }

    private static BufferSegment CreateSegment()
    {
        return new BufferSegment();
    }

    private static int GetSegmentSize(int sizeHint, int maxBufferSize = int.MaxValue)
    {
        // First we need to handle case where hint is smaller than minimum segment size
        sizeHint = Math.Max(MinimumBufferSize, sizeHint);
        // After that adjust it to fit into pools max buffer size
        var adjustedToMaximumSize = Math.Min(maxBufferSize, sizeHint);
        return adjustedToMaximumSize;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Reset();
        }
    }

    public void Reset()
    {
        var segment = _head;
        while (segment != null)
        {
            var returnSegment = segment;
            segment = segment.NextSegment;

            // We haven't reached the tail of the linked list yet, so we can always return the returnSegment.
            returnSegment.ResetMemory();
        }

        _head = _tail = null;

        _bytesBuffered = 0;
        _tailBytesBuffered = 0;
    }

    // Copied from https://github.com/dotnet/corefx/blob/de3902bb56f1254ec1af4bf7d092fc2c048734cc/src/System.Memory/src/System/ThrowHelper.cs
    private static void ThrowArgumentOutOfRangeException(string argumentName) { throw CreateArgumentOutOfRangeException(argumentName); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Exception CreateArgumentOutOfRangeException(string argumentName) { return new ArgumentOutOfRangeException(argumentName); }

    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _innerStream.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _innerStream.Read(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _innerStream.WriteAsync(buffer, cancellationToken);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _innerStream.Write(buffer);
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _innerStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _innerStream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return _innerStream.EndRead(asyncResult);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        _innerStream.EndWrite(asyncResult);
    }

    // Do not override CopyTo/Async, they call Read/Async internally.

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _innerStream.ReadAsync(buffer, cancellationToken);
    }

    public override ValueTask DisposeAsync()
    {
        return _innerStream.DisposeAsync();
    }
}
