// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Stream that buffers reads
/// </summary>
internal sealed class ResponseBufferingStream : BufferingStream, IHttpResponseBodyFeature
{
    private readonly IHttpResponseBodyFeature _innerBodyFeature;
    private int _limit;
    private PipeWriter? _pipeAdapter;

    private readonly HttpLoggingContext _logContext;
    private readonly HttpLoggingOptions _options;
    private readonly IHttpLoggingInterceptor[] _interceptors;
    private bool _logBody;
    private Encoding? _encoding;

    private static readonly StreamPipeWriterOptions _pipeWriterOptions = new StreamPipeWriterOptions(leaveOpen: true);

    internal ResponseBufferingStream(IHttpResponseBodyFeature innerBodyFeature,
        ILogger logger,
        HttpLoggingContext logContext,
        HttpLoggingOptions options,
        IHttpLoggingInterceptor[] interceptors)
        : base(innerBodyFeature.Stream, logger)
    {
        _innerBodyFeature = innerBodyFeature;
        _innerStream = innerBodyFeature.Stream;
        _logContext = logContext;
        _options = options;
        _interceptors = interceptors;
    }

    public bool HeadersWritten { get; private set; }

    public Stream Stream => this;

    public PipeWriter Writer => _pipeAdapter ??= PipeWriter.Create(Stream, _pipeWriterOptions);

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
        CommonWrite(span);

        _innerStream.Write(span);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await WriteAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        CommonWrite(buffer.Span);

        await _innerStream.WriteAsync(buffer, cancellationToken);
    }

    private void CommonWrite(ReadOnlySpan<byte> span)
    {
        OnFirstWrite();
        var remaining = _limit - _bytesBuffered;
        var innerCount = Math.Min(remaining, span.Length);

        if (_logBody && innerCount > 0)
        {
            var slice = span.Slice(0, innerCount);
            if (slice.TryCopyTo(_tailMemory.Span))
            {
                _tailBytesBuffered += innerCount;
                _bytesBuffered += innerCount;
                _tailMemory = _tailMemory.Slice(innerCount);
            }
            else
            {
                BuffersExtensions.Write(this, slice);
            }
        }
    }

    private void OnFirstWrite()
    {
        if (!HeadersWritten)
        {
            // Log headers as first write occurs (headers locked now)
            HttpLoggingMiddleware.LogResponseHeaders(_logContext, _options._internalResponseHeaders, _interceptors, _logger);

            // The callback in LogResponseHeaders could disable body logging or adjust limits.
            if (_logContext.LoggingFields.HasFlag(HttpLoggingFields.ResponseBody) && _logContext.ResponseBodyLogLimit > 0)
            {
                if (MediaTypeHelpers.TryGetEncodingForMediaType(_logContext.HttpContext.Response.ContentType,
                    _options.MediaTypeOptions.MediaTypeStates, out _encoding))
                {
                    _logBody = true;
                    _limit = _logContext.ResponseBodyLogLimit;
                }
                else
                {
                    _logger.UnrecognizedMediaType("response");
                }
            }

            HeadersWritten = true;
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

    public void LogResponseBody()
    {
        if (_logBody)
        {
            var requestBody = GetString(_encoding!);
            _logger.ResponseBody(requestBody);
        }
    }
}
