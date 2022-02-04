// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging;

internal sealed class RequestBufferingStream : Stream
{
    private readonly Encoding _encoding;
    private readonly int _limit;
    private readonly ILogger _logger;
    private readonly Stream _innerStream;
    private readonly BufferingWriter _bufferingWriter;

    public bool HasLogged { get; private set; }

    public RequestBufferingStream(Stream innerStream, int limit, ILogger logger, Encoding encoding)
    {
        _logger = logger;
        _limit = limit;
        _innerStream = innerStream;
        _encoding = encoding;
        _bufferingWriter = new BufferingWriter();
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

    public string GetString(Encoding? encoding)
    {
        try
        {
            if (_bufferingWriter.Head == null || _bufferingWriter.Tail == null)
            {
                // nothing written
                return "";
            }

            if (encoding == null)
            {
                _logger.UnrecognizedMediaType();
                return "";
            }

            // Only place where we are actually using the buffered data.
            // update tail here.
            _bufferingWriter.Tail.End = _bufferingWriter.TailBytesBuffered;

            var ros = new ReadOnlySequence<byte>(_bufferingWriter.Head, 0, _bufferingWriter.Tail, _bufferingWriter.TailBytesBuffered);

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

    public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        var res = await _innerStream.ReadAsync(destination, cancellationToken);

        WriteToBuffer(destination.Slice(0, res).Span);

        return res;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var res = await _innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        WriteToBuffer(buffer.AsSpan(offset, res));

        return res;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var res = _innerStream.Read(buffer, offset, count);

        WriteToBuffer(buffer.AsSpan(offset, res));

        return res;
    }

    private void WriteToBuffer(ReadOnlySpan<byte> span)
    {
        // get what was read into the buffer
        var remaining = _limit - _bufferingWriter.BytesBuffered;

        if (remaining == 0)
        {
            return;
        }

        if (span.Length == 0 && !HasLogged)
        {
            // Done reading, log the string.
            LogRequestBody();
            return;
        }

        var innerCount = Math.Min(remaining, span.Length);

        if (span.Slice(0, innerCount).TryCopyTo(_bufferingWriter.TailMemory.Span))
        {
            _bufferingWriter.TailBytesBuffered += innerCount;
            _bufferingWriter.BytesBuffered += innerCount;
            _bufferingWriter.TailMemory = _bufferingWriter.TailMemory.Slice(innerCount);
        }
        else
        {
            BuffersExtensions.Write(_bufferingWriter, span.Slice(0, innerCount));
        }

        if (_limit - _bufferingWriter.BytesBuffered == 0 && !HasLogged)
        {
            LogRequestBody();
        }
    }

    public void LogRequestBody()
    {
        var requestBody = GetString(_encoding);
        if (requestBody != null)
        {
            _logger.RequestBody(requestBody);
        }
        HasLogged = true;
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return TaskToApm.End<int>(asyncResult);
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
        var segment = _bufferingWriter.Head;
        while (segment != null)
        {
            var returnSegment = segment;
            segment = segment.NextSegment;

            // We haven't reached the tail of the linked list yet, so we can always return the returnSegment.
            returnSegment.ResetMemory();
        }

        _bufferingWriter.Head = _bufferingWriter.Tail = null;

        _bufferingWriter.BytesBuffered = 0;
        _bufferingWriter.TailBytesBuffered = 0;
    }

    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _innerStream.FlushAsync(cancellationToken);
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

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _innerStream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        _innerStream.EndWrite(asyncResult);
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        _innerStream.CopyTo(destination, bufferSize);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override ValueTask DisposeAsync()
    {
        return _innerStream.DisposeAsync();
    }

    public override int Read(Span<byte> buffer)
    {
        return _innerStream.Read(buffer);
    }
}
