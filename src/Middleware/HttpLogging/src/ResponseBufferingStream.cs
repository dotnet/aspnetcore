// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using static Microsoft.AspNetCore.HttpLogging.MediaTypeOptions;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Stream that buffers reads
/// </summary>
internal sealed class ResponseBufferingStream : Stream, IHttpResponseBodyFeature
{
    private readonly IHttpResponseBodyFeature _innerBodyFeature;
    private readonly Stream _innerStream;
    private readonly int _limit;
    private PipeWriter? _pipeAdapter;
    private readonly BufferingWriter _bufferingWriter;

    private readonly HttpContext _context;
    private readonly List<MediaTypeState> _encodings;
    private readonly HttpLoggingOptions _options;
    private Encoding? _encoding;
    private readonly ILogger _logger;

    private static readonly StreamPipeWriterOptions _pipeWriterOptions = new StreamPipeWriterOptions(leaveOpen: true);

    internal ResponseBufferingStream(IHttpResponseBodyFeature innerBodyFeature,
        int limit,
        ILogger logger,
        HttpContext context,
        List<MediaTypeState> encodings,
        HttpLoggingOptions options)
    {
        _innerBodyFeature = innerBodyFeature;
        _innerStream = innerBodyFeature.Stream;
        _limit = limit;
        _context = context;
        _encodings = encodings;
        _options = options;
        _logger = logger;
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

    public bool FirstWrite { get; private set; }

    public Stream Stream => this;

    public PipeWriter Writer => _pipeAdapter ??= PipeWriter.Create(Stream, _pipeWriterOptions);

    public Encoding? Encoding { get => _encoding; }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(buffer.AsSpan(offset, count));
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        TaskToApm.End(asyncResult);
    }

    public override void Write(ReadOnlySpan<byte> span)
    {
        var remaining = _limit - _bufferingWriter.BytesBuffered;
        var innerCount = Math.Min(remaining, span.Length);

        OnFirstWrite();

        if (innerCount > 0)
        {
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
        }

        _innerStream.Write(span);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await WriteAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var remaining = _limit - _bufferingWriter.BytesBuffered;
        var innerCount = Math.Min(remaining, buffer.Length);

        OnFirstWrite();

        if (innerCount > 0)
        {
            if (_bufferingWriter.TailMemory.Length - innerCount > 0)
            {
                buffer.Slice(0, innerCount).CopyTo(_bufferingWriter.TailMemory);
                _bufferingWriter.TailBytesBuffered += innerCount;
                _bufferingWriter.BytesBuffered += innerCount;
                _bufferingWriter.TailMemory = _bufferingWriter.TailMemory.Slice(innerCount);
            }
            else
            {
                BuffersExtensions.Write(_bufferingWriter, buffer.Span);
            }
        }

        await _innerStream.WriteAsync(buffer, cancellationToken);
    }

    private void OnFirstWrite()
    {
        if (!FirstWrite)
        {
            // Log headers as first write occurs (headers locked now)
            HttpLoggingMiddleware.LogResponseHeaders(_context.Response, _options, _logger);

            MediaTypeHelpers.TryGetEncodingForMediaType(_context.Response.ContentType, _encodings, out _encoding);
            FirstWrite = true;
        }
    }

    public void DisableBuffering()
    {
        _innerBodyFeature.DisableBuffering();
    }

    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
    {
        OnFirstWrite();
        return _innerBodyFeature.SendFileAsync(path, offset, count, cancellation);
    }

    public Task StartAsync(CancellationToken token = default)
    {
        OnFirstWrite();
        return _innerBodyFeature.StartAsync(token);
    }

    public async Task CompleteAsync()
    {
        await _innerBodyFeature.CompleteAsync();
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
        OnFirstWrite();
        _innerStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        OnFirstWrite();
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

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _innerStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return _innerStream.EndRead(asyncResult);
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        _innerStream.CopyTo(destination, bufferSize);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _innerStream.ReadAsync(buffer, cancellationToken);
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
