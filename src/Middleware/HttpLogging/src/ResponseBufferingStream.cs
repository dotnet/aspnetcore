// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging;

internal sealed class ResponseBufferingStream : BufferingStream, IHttpResponseBodyFeature
{
    private readonly IHttpResponseBodyFeature _innerBodyFeature;
    private int _limit;
    private PipeWriter? _pipeAdapter;

    private readonly HttpLoggingInterceptorContext _logContext;
    private readonly HttpLoggingOptions _options;
    private readonly IHttpLoggingInterceptor[] _interceptors;
    private bool _logBody;
    private Encoding? _encoding;

    private static readonly StreamPipeWriterOptions _pipeWriterOptions = new StreamPipeWriterOptions(leaveOpen: true);

    internal ResponseBufferingStream(IHttpResponseBodyFeature innerBodyFeature,
        ILogger logger,
        HttpLoggingInterceptorContext logContext,
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
        OnFirstWriteSync();
        CommonWrite(span);

        _innerStream.Write(span);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await WriteAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await OnFirstWriteAsync();
        CommonWrite(buffer.Span);

        await _innerStream.WriteAsync(buffer, cancellationToken);
    }

    private void CommonWrite(ReadOnlySpan<byte> span)
    {
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

    private void OnFirstWriteSync()
    {
        if (!HeadersWritten)
        {
            // Log headers as first write occurs (headers locked now)
            HttpLoggingMiddleware.LogResponseHeadersSync(_logContext, _options, _interceptors, _logger);
            OnFirstWriteCore();
        }
    }

    private async ValueTask OnFirstWriteAsync()
    {
        if (!HeadersWritten)
        {
            // Log headers as first write occurs (headers locked now)
            await HttpLoggingMiddleware.LogResponseHeadersAsync(_logContext, _options, _interceptors, _logger);
            OnFirstWriteCore();
        }
    }

    private void OnFirstWriteCore()
    {
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

    public void DisableBuffering()
    {
        _innerBodyFeature.DisableBuffering();
    }

    public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
    {
        await OnFirstWriteAsync();
        await _innerBodyFeature.SendFileAsync(path, offset, count, cancellation);
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        await OnFirstWriteAsync();
        await _innerBodyFeature.StartAsync(token);
    }

    public async Task CompleteAsync()
    {
        await OnFirstWriteAsync();
        await _innerBodyFeature.CompleteAsync();
    }

    public override void Flush()
    {
        OnFirstWriteSync();
        base.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await OnFirstWriteAsync();
        await base.FlushAsync(cancellationToken);
    }

    public void LogResponseBody()
    {
        if (_logBody)
        {
            var responseBody = GetString(_encoding!);
            _logger.ResponseBody(responseBody);
        }
    }

    public void LogResponseBody(HttpLoggingInterceptorContext logContext)
    {
        if (_logBody)
        {
            logContext.AddParameter("ResponseBody", GetString(_encoding!));
        }
    }
}
