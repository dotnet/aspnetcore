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
internal sealed class ResponseBufferingStream : BufferingStream, IHttpResponseBodyFeature
{
    private readonly IHttpResponseBodyFeature _innerBodyFeature;
    private readonly int _limit;
    private PipeWriter? _pipeAdapter;

    private readonly HttpContext _context;
    private readonly List<MediaTypeState> _encodings;
    private readonly HttpLoggingOptions _options;
    private Encoding? _encoding;

    private static readonly StreamPipeWriterOptions _pipeWriterOptions = new StreamPipeWriterOptions(leaveOpen: true);

    internal ResponseBufferingStream(IHttpResponseBodyFeature innerBodyFeature,
        int limit,
        ILogger logger,
        HttpContext context,
        List<MediaTypeState> encodings,
        HttpLoggingOptions options)
        : base(innerBodyFeature.Stream, logger)
    {
        _innerBodyFeature = innerBodyFeature;
        _innerStream = innerBodyFeature.Stream;
        _limit = limit;
        _context = context;
        _encodings = encodings;
        _options = options;
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
        var remaining = _limit - _bytesBuffered;
        var innerCount = Math.Min(remaining, span.Length);

        OnFirstWrite();

        if (innerCount > 0)
        {
            if (span.Slice(0, innerCount).TryCopyTo(_tailMemory.Span))
            {
                _tailBytesBuffered += innerCount;
                _bytesBuffered += innerCount;
                _tailMemory = _tailMemory.Slice(innerCount);
            }
            else
            {
                BuffersExtensions.Write(this, span.Slice(0, innerCount));
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
        var remaining = _limit - _bytesBuffered;
        var innerCount = Math.Min(remaining, buffer.Length);

        OnFirstWrite();

        if (innerCount > 0)
        {
            if (_tailMemory.Length - innerCount > 0)
            {
                buffer.Slice(0, innerCount).CopyTo(_tailMemory);
                _tailBytesBuffered += innerCount;
                _bytesBuffered += innerCount;
                _tailMemory = _tailMemory.Slice(innerCount);
            }
            else
            {
                BuffersExtensions.Write(this, buffer.Span);
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

    public override void Flush()
    {
        OnFirstWrite();
        base.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        OnFirstWrite();
        return base.FlushAsync(cancellationToken);
    }
}
