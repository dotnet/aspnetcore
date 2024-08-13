// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http1ConnectionTests : Http1ConnectionTestsBase
{
    [Fact]
    public async Task TakeMessageHeadersSucceedsWhenHeaderValueContainsUTF8()
    {
        var headerName = "Header";
        var headerValueBytes = new byte[] { 0x46, 0x72, 0x61, 0x6e, 0xc3, 0xa7, 0x6f, 0x69, 0x73 };
        var headerValue = Encoding.UTF8.GetString(headerValueBytes);
        _http1Connection.Reset();

        await _application.Output.WriteAsync(Encoding.UTF8.GetBytes($"{headerName}: "));
        await _application.Output.WriteAsync(headerValueBytes);
        await _application.Output.WriteAsync(Encoding.UTF8.GetBytes("\r\n\r\n"));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        TakeMessageHeaders(readableBuffer, trailers: false, out _consumed, out _examined);
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.Equal(headerValue, _http1Connection.RequestHeaders[headerName]);
    }

    [Fact]
    public async Task TakeMessageHeadersThrowsWhenHeaderValueContainsExtendedASCII()
    {
        var extendedAsciiEncoding = Encoding.GetEncoding("ISO-8859-1");
        var headerName = "Header";
        var headerValueBytes = new byte[] { 0x46, 0x72, 0x61, 0x6e, 0xe7, 0x6f, 0x69, 0x73 };
        _http1Connection.Reset();

        await _application.Output.WriteAsync(extendedAsciiEncoding.GetBytes($"{headerName}: "));
        await _application.Output.WriteAsync(headerValueBytes);
        await _application.Output.WriteAsync(extendedAsciiEncoding.GetBytes("\r\n\r\n"));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var exception = Assert.Throws<InvalidOperationException>(() => TakeMessageHeaders(readableBuffer, trailers: false, out _consumed, out _examined));
    }

    [Fact]
    public async Task MaxRequestHeadersTotalSizeDoesNotThrowForMaxValue()
    {
        const string headerLine = "Header: value\r\n";
        _serviceContext.ServerOptions.Limits.MaxRequestHeadersTotalSize = int.MaxValue;
        _http1Connection.Reset();

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine}\r\n"));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;
    }

    [Fact]
    public async Task TakeMessageHeadersThrowsWhenHeadersExceedTotalSizeLimit()
    {
        const string headerLine = "Header: value\r\n";
        _serviceContext.ServerOptions.Limits.MaxRequestHeadersTotalSize = headerLine.Length - 1;
        _http1Connection.Reset();

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine}\r\n"));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var exception = Assert.ThrowsAny<Http.BadHttpRequestException>(() => TakeMessageHeaders(readableBuffer, trailers: false, out _consumed, out _examined));
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.Equal(CoreStrings.BadRequest_HeadersExceedMaxTotalSize, exception.Message);
        Assert.Equal(StatusCodes.Status431RequestHeaderFieldsTooLarge, exception.StatusCode);
    }

    [Fact]
    public async Task TakeMessageHeadersThrowsWhenHeadersExceedCountLimit()
    {
        const string headerLines = "Header-1: value1\r\nHeader-2: value2\r\n";
        _serviceContext.ServerOptions.Limits.MaxRequestHeaderCount = 1;
        _http1Connection.Initialize(_http1ConnectionContext);

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"{headerLines}\r\n"));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var exception = Assert.ThrowsAny<Http.BadHttpRequestException>(() => TakeMessageHeaders(readableBuffer, trailers: false, out _consumed, out _examined));
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.Equal(CoreStrings.BadRequest_TooManyHeaders, exception.Message);
        Assert.Equal(StatusCodes.Status431RequestHeaderFieldsTooLarge, exception.StatusCode);
    }

    [Fact]
    public async Task TakeMessageHeadersDoesNotCountAlreadyConsumedBytesTowardsSizeLimit()
    {
        const string startLine = "GET / HTTP/1.1\r\n";

        // This doesn't actually need to be larger than the start line to cause the regression,
        // but doing so gives us a nice HeadersExceedMaxTotalSize error rather than an invalid slice
        // when we do see the regression.
        const string headerLine = "Header: makethislargerthanthestartline\r\n";

        _serviceContext.ServerOptions.Limits.MaxRequestHeadersTotalSize = headerLine.Length;
        _http1Connection.Reset();

        // Don't send header initially because the regression is only caught if TakeMessageHeaders
        // is called multiple times. The first call overcounted the header bytes consumed, and the
        // subsequent calls overslice the buffer due to the overcounting.
        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"{startLine}"));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        SequencePosition TakeStartLineAndMessageHeaders()
        {
            var reader = new SequenceReader<byte>(readableBuffer);
            Assert.True(_http1Connection.TakeStartLine(ref reader));
            Assert.False(_http1Connection.TakeMessageHeaders(ref reader, trailers: false));
            return reader.Position;
        }

        _transport.Input.AdvanceTo(TakeStartLineAndMessageHeaders());

        Assert.Empty(_http1Connection.RequestHeaders);

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine}\r\n"));
        readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        SequencePosition TakeMessageHeaders()
        {
            var reader = new SequenceReader<byte>(readableBuffer);
            Assert.True(_http1Connection.TakeMessageHeaders(ref reader, trailers: false));
            return reader.Position;
        }

        _transport.Input.AdvanceTo(TakeMessageHeaders());

        Assert.Single(_http1Connection.RequestHeaders);
        Assert.Equal("makethislargerthanthestartline", _http1Connection.RequestHeaders["Header"]);
    }

    [Fact]
    public void ResetResetsScheme()
    {
        _http1Connection.Scheme = "https";

        // Act
        _http1Connection.Reset();

        // Assert
        Assert.Equal("http", ((IFeatureCollection)_http1Connection).Get<IHttpRequestFeature>().Scheme);
    }

    [Fact]
    public void ResetResetsMinRequestBodyDataRate()
    {
        _http1Connection.MinRequestBodyDataRate = new MinDataRate(bytesPerSecond: 1, gracePeriod: TimeSpan.MaxValue);

        _http1Connection.Reset();

        Assert.Same(_serviceContext.ServerOptions.Limits.MinRequestBodyDataRate, _http1Connection.MinRequestBodyDataRate);
    }

    [Fact]
    public void ResetResetsMinResponseDataRate()
    {
        _http1Connection.MinResponseDataRate = new MinDataRate(bytesPerSecond: 1, gracePeriod: TimeSpan.MaxValue);

        _http1Connection.Reset();

        Assert.Same(_serviceContext.ServerOptions.Limits.MinResponseDataRate, _http1Connection.MinResponseDataRate);
    }

    [Fact]
    public async Task TraceIdentifierCountsRequestsPerHttp1Connection()
    {
        var connectionId = _http1ConnectionContext.ConnectionId;
        var feature = ((IFeatureCollection)_http1Connection).Get<IHttpRequestIdentifierFeature>();

        var requestProcessingTask = _http1Connection.ProcessRequestsAsync(new DummyApplication());

        var count = 1;
        async Task SendRequestAsync()
        {
            var data = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n");
            await _application.Output.WriteAsync(data);

            while (true)
            {
                var read = await _application.Input.ReadAsync();
                SequencePosition consumed = read.Buffer.Start;
                SequencePosition examined = read.Buffer.End;
                try
                {
                    if (TryReadResponse(read, out consumed, out examined))
                    {
                        break;
                    }
                }
                finally
                {
                    _application.Input.AdvanceTo(consumed, examined);
                }
            }

            count++;
        }

        static bool TryReadResponse(ReadResult read, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = read.Buffer.Start;
            examined = read.Buffer.End;

            SequenceReader<byte> reader = new SequenceReader<byte>(read.Buffer);
            if (reader.TryReadTo(out ReadOnlySequence<byte> _, new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' }, advancePastDelimiter: true))
            {
                consumed = reader.Position;
                examined = reader.Position;
                return true;
            }

            return false;
        }

        var nextId = feature.TraceIdentifier;
        Assert.Equal($"{connectionId}:00000001", nextId);

        await SendRequestAsync();

        var secondId = feature.TraceIdentifier;
        Assert.Equal($"{connectionId}:00000002", secondId);

        var big = 10_000;
        while (big-- > 0)
        {
            await SendRequestAsync();
        }
        Assert.Equal($"{connectionId}:{count:X8}", feature.TraceIdentifier);

        _http1Connection.StopProcessingNextRequest(ConnectionEndReason.AppShutdownTimeout);
        await requestProcessingTask.DefaultTimeout();
    }

    [Fact]
    public void TraceIdentifierGeneratesWhenNull()
    {
        _http1Connection.TraceIdentifier = null;
        var id = _http1Connection.TraceIdentifier;
        Assert.NotNull(id);
        Assert.Equal(id, _http1Connection.TraceIdentifier);
    }

    [Fact]
    public async Task ResetResetsHeaderLimits()
    {
        const string headerLine1 = "Header-1: value1\r\n";
        const string headerLine2 = "Header-2: value2\r\n";

        var options = new KestrelServerOptions();
        options.Limits.MaxRequestHeadersTotalSize = headerLine1.Length;
        options.Limits.MaxRequestHeaderCount = 1;
        _serviceContext.ServerOptions = options;

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine1}\r\n"));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var takeMessageHeaders = TakeMessageHeaders(readableBuffer, trailers: false, out _consumed, out _examined);
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.True(takeMessageHeaders);
        Assert.Single(_http1Connection.RequestHeaders);
        Assert.Equal("value1", _http1Connection.RequestHeaders["Header-1"]);

        _http1Connection.Reset();

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"{headerLine2}\r\n"));
        readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        takeMessageHeaders = TakeMessageHeaders(readableBuffer, trailers: false, out _consumed, out _examined);
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.True(takeMessageHeaders);
        Assert.Single(_http1Connection.RequestHeaders);
        Assert.Equal("value2", _http1Connection.RequestHeaders["Header-2"]);
    }

    [Fact]
    public async Task ThrowsWhenStatusCodeIsSetAfterResponseStarted()
    {
        // Act
        await _http1Connection.WriteAsync(new ArraySegment<byte>(new byte[1]));

        // Assert
        Assert.True(_http1Connection.HasResponseStarted);
        Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)_http1Connection).StatusCode = StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ThrowsWhenReasonPhraseIsSetAfterResponseStarted()
    {
        // Act
        await _http1Connection.WriteAsync(new ArraySegment<byte>(new byte[1]));

        // Assert
        Assert.True(_http1Connection.HasResponseStarted);
        Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)_http1Connection).ReasonPhrase = "Reason phrase");
    }

    [Fact]
    public async Task ThrowsWhenOnStartingIsSetAfterResponseStarted()
    {
        await _http1Connection.WriteAsync(new ArraySegment<byte>(new byte[1]));

        // Act/Assert
        Assert.True(_http1Connection.HasResponseStarted);
        Assert.Throws<InvalidOperationException>(() => ((IHttpResponseFeature)_http1Connection).OnStarting(_ => Task.CompletedTask, null));
    }

    [Theory]
    [MemberData(nameof(MinDataRateData))]
    public void ConfiguringIHttpMinRequestBodyDataRateFeatureSetsMinRequestBodyDataRate(MinDataRate minDataRate)
    {
        ((IFeatureCollection)_http1Connection).Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate = minDataRate;

        Assert.Same(minDataRate, _http1Connection.MinRequestBodyDataRate);
    }

    [Theory]
    [MemberData(nameof(MinDataRateData))]
    public void ConfiguringIHttpMinResponseDataRateFeatureSetsMinResponseDataRate(MinDataRate minDataRate)
    {
        ((IFeatureCollection)_http1Connection).Get<IHttpMinResponseDataRateFeature>().MinDataRate = minDataRate;

        Assert.Same(minDataRate, _http1Connection.MinResponseDataRate);
    }

    [Fact]
    public void ResetResetsRequestHeaders()
    {
        // Arrange
        var originalRequestHeaders = _http1Connection.RequestHeaders;
        _http1Connection.RequestHeaders = new HttpRequestHeaders();

        // Act
        _http1Connection.Reset();

        // Assert
        Assert.Same(originalRequestHeaders, _http1Connection.RequestHeaders);
    }

    [Fact]
    public void ResetResetsResponseHeaders()
    {
        // Arrange
        var originalResponseHeaders = _http1Connection.ResponseHeaders;
        _http1Connection.ResponseHeaders = new HttpResponseHeaders();

        // Act
        _http1Connection.Reset();

        // Assert
        Assert.Same(originalResponseHeaders, _http1Connection.ResponseHeaders);
    }

    [Fact]
    public void InitializeStreamsResetsStreams()
    {
        // Arrange
        var messageBody = Http1MessageBody.For(Kestrel.Core.Internal.Http.HttpVersion.Http11, (HttpRequestHeaders)_http1Connection.RequestHeaders, _http1Connection);
        _http1Connection.InitializeBodyControl(messageBody);

        var originalRequestBody = _http1Connection.RequestBody;
        var originalResponseBody = _http1Connection.ResponseBody;
        _http1Connection.RequestBody = new MemoryStream();
        _http1Connection.ResponseBody = new MemoryStream();

        // Act
        _http1Connection.InitializeBodyControl(messageBody);

        // Assert
        Assert.Same(originalRequestBody, _http1Connection.RequestBody);
        Assert.Same(originalResponseBody, _http1Connection.ResponseBody);
    }

    [Theory]
    [MemberData(nameof(RequestLineValidData))]
    public async Task TakeStartLineSetsHttpProtocolProperties(
        string requestLine,
        string expectedMethod,
        string expectedRawTarget,
            // This warns that theory methods should use all of their parameters,
            // but this method is using a shared data collection with HttpParserTests.ParsesRequestLine and others.
#pragma warning disable xUnit1026
            string expectedRawPath,
#pragma warning restore xUnit1026
            string expectedDecodedPath,
        string expectedQueryString,
        string expectedHttpVersion)
    {
        var requestLineBytes = Encoding.ASCII.GetBytes(requestLine);
        await _application.Output.WriteAsync(requestLineBytes);
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var returnValue = TakeStartLine(readableBuffer, out _consumed, out _examined);
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.True(returnValue);
        Assert.Equal(expectedMethod, ((IHttpRequestFeature)_http1Connection).Method);
        Assert.Equal(expectedRawTarget, _http1Connection.RawTarget);
        Assert.Equal(expectedDecodedPath, _http1Connection.Path);
        Assert.Equal(expectedQueryString, _http1Connection.QueryString);
        Assert.Equal(expectedHttpVersion, _http1Connection.HttpVersion);
    }

    [Theory]
    [MemberData(nameof(RequestLineDotSegmentData))]
    public async Task TakeStartLineRemovesDotSegmentsFromTarget(
        string requestLine,
        string expectedRawTarget,
        string expectedDecodedPath,
        string expectedQueryString)
    {
        var requestLineBytes = Encoding.ASCII.GetBytes(requestLine);
        await _application.Output.WriteAsync(requestLineBytes);
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var returnValue = TakeStartLine(readableBuffer, out _consumed, out _examined);
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.True(returnValue);
        Assert.Equal(expectedRawTarget, _http1Connection.RawTarget);
        Assert.Equal(expectedDecodedPath, _http1Connection.Path);
        Assert.Equal(expectedQueryString, _http1Connection.QueryString);
    }

    [Fact]
    public async Task ParseRequestStartsRequestHeadersTimeoutOnFirstByteAvailable()
    {
        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes("G"));

        ParseRequest((await _transport.Input.ReadAsync()).Buffer, out _consumed, out _examined);
        _transport.Input.AdvanceTo(_consumed, _examined);

        _timeoutControl.Verify(cc => cc.ResetTimeout(_serviceContext.ServerOptions.Limits.RequestHeadersTimeout, TimeoutReason.RequestHeaders));
    }

    [Fact]
    public async Task TakeStartLineThrowsWhenTooLong()
    {
        _serviceContext.ServerOptions.Limits.MaxRequestLineSize = "GET / HTTP/1.1\r\n".Length;

        var requestLineBytes = Encoding.ASCII.GetBytes("GET /a HTTP/1.1\r\n");
        await _application.Output.WriteAsync(requestLineBytes);

        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;
        var exception = Assert.ThrowsAny<Http.BadHttpRequestException>(() => TakeStartLine(readableBuffer, out _consumed, out _examined));
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.Equal(CoreStrings.BadRequest_RequestLineTooLong, exception.Message);
        Assert.Equal(StatusCodes.Status414UriTooLong, exception.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TargetWithEncodedNullCharData))]
    public async Task TakeStartLineThrowsOnEncodedNullCharInTarget(string target)
    {
        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"GET {target} HTTP/1.1\r\n"));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var exception = Assert.ThrowsAny<Http.BadHttpRequestException>(() =>
        TakeStartLine(readableBuffer, out _consumed, out _examined));
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(target), exception.Message);
    }

    [Theory]
    [MemberData(nameof(TargetWithNullCharData))]
    public async Task TakeStartLineThrowsOnNullCharInTarget(string target)
    {
        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"GET {target} HTTP/1.1\r\n"));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var exception = Assert.ThrowsAny<Http.BadHttpRequestException>(() =>
        TakeStartLine(readableBuffer, out _consumed, out _examined));
        _transport.Input.AdvanceTo(_consumed, _examined);

        var partialTarget = target.AsSpan(0, target.IndexOf('\0') + 1).ToString();
        Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail($"GET {partialTarget.EscapeNonPrintable()}"), exception.Message);
    }

    [Theory]
    [MemberData(nameof(MethodWithNullCharData))]
    public async Task TakeStartLineThrowsOnNullCharInMethod(string method)
    {
        var requestLine = $"{method} / HTTP/1.1\r\n";

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes(requestLine));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var exception = Assert.ThrowsAny<Http.BadHttpRequestException>(() =>
        TakeStartLine(readableBuffer, out _consumed, out _examined));
        _transport.Input.AdvanceTo(_consumed, _examined);

        var partialRequestLine = requestLine.AsSpan(0, requestLine.IndexOf('\0') + 1).ToString();
        Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(partialRequestLine.EscapeNonPrintable()), exception.Message);
    }

    [Theory]
    [MemberData(nameof(QueryStringWithNullCharData))]
    public async Task TakeStartLineThrowsOnNullCharInQueryString(string queryString)
    {
        var target = $"/{queryString}";

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"GET {target} HTTP/1.1\r\n"));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var exception = Assert.ThrowsAny<Http.BadHttpRequestException>(() =>
         TakeStartLine(readableBuffer, out _consumed, out _examined));
        _transport.Input.AdvanceTo(_consumed, _examined);

        var partialTarget = target.AsSpan(0, target.IndexOf('\0') + 1).ToString();
        Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail($"GET {partialTarget.EscapeNonPrintable()}"), exception.Message);
    }

    [Theory]
    [MemberData(nameof(TargetInvalidData))]
    public async Task TakeStartLineThrowsWhenRequestTargetIsInvalid(string method, string target)
    {
        var requestLine = $"{method} {target} HTTP/1.1\r\n";

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes(requestLine));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

        var exception = Assert.ThrowsAny<Http.BadHttpRequestException>(() =>
        TakeStartLine(readableBuffer, out _consumed, out _examined));
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(target.EscapeNonPrintable()), exception.Message);
    }

    [Theory]
    [MemberData(nameof(MethodNotAllowedTargetData))]
    public async Task TakeStartLineThrowsWhenMethodNotAllowed(string requestLine, int intAllowedMethod)
    {
        var allowedMethod = (HttpMethod)intAllowedMethod;
        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes(requestLine));
        var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(() =>
#pragma warning restore CS0618 // Type or member is obsolete
                TakeStartLine(readableBuffer, out _consumed, out _examined));
        _transport.Input.AdvanceTo(_consumed, _examined);

        Assert.Equal(405, exception.StatusCode);
        Assert.Equal(CoreStrings.BadRequest_MethodNotAllowed, exception.Message);
        Assert.Equal(HttpUtilities.MethodToString(allowedMethod), exception.AllowedHeader);
    }

    [Fact]
    public async Task ProcessRequestsAsyncEnablesKeepAliveTimeout()
    {
        var requestProcessingTask = _http1Connection.ProcessRequestsAsync<object>(null);

        var expectedKeepAliveTimeout = _serviceContext.ServerOptions.Limits.KeepAliveTimeout;
        _timeoutControl.Verify(cc => cc.SetTimeout(expectedKeepAliveTimeout, TimeoutReason.KeepAlive));

        _http1Connection.StopProcessingNextRequest(ConnectionEndReason.AppShutdownTimeout);
        _application.Output.Complete();

        await requestProcessingTask.DefaultTimeout();
    }

    [Fact]
    public async Task WriteThrowsForNonBodyResponse()
    {
        // Arrange
        ((IHttpResponseFeature)_http1Connection).StatusCode = StatusCodes.Status304NotModified;

        // Act/Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _http1Connection.WriteAsync(new ArraySegment<byte>(new byte[1])));
    }

    [Fact]
    public async Task WriteAsyncThrowsForNonBodyResponse()
    {
        // Arrange
        _http1Connection.HttpVersion = "HTTP/1.1";
        ((IHttpResponseFeature)_http1Connection).StatusCode = StatusCodes.Status304NotModified;

        // Act/Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _http1Connection.WriteAsync(new ArraySegment<byte>(new byte[1]), default(CancellationToken)));
    }

    [Fact]
    public async Task WriteDoesNotThrowForHeadResponse()
    {
        // Arrange
        _http1Connection.HttpVersion = "HTTP/1.1";
        _http1Connection.Method = HttpMethod.Head;

        // Act/Assert
        await _http1Connection.WriteAsync(new ArraySegment<byte>(new byte[1]));
    }

    [Fact]
    public async Task WriteAsyncDoesNotThrowForHeadResponse()
    {
        // Arrange
        _http1Connection.HttpVersion = "HTTP/1.1";
        _http1Connection.Method = HttpMethod.Head;

        // Act/Assert
        await _http1Connection.WriteAsync(new ArraySegment<byte>(new byte[1]), default(CancellationToken));
    }

    [Fact]
    public async Task ManuallySettingTransferEncodingThrowsForHeadResponse()
    {
        // Arrange
        _http1Connection.HttpVersion = "HTTP/1.1";
        _http1Connection.Method = HttpMethod.Head;

        // Act
        _http1Connection.ResponseHeaders.Add("Transfer-Encoding", "chunked");

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _http1Connection.FlushAsync());
    }

    [Fact]
    public async Task ManuallySettingTransferEncodingThrowsForNoBodyResponse()
    {
        // Arrange
        _http1Connection.HttpVersion = "HTTP/1.1";
        ((IHttpResponseFeature)_http1Connection).StatusCode = StatusCodes.Status304NotModified;

        // Act
        _http1Connection.ResponseHeaders.Add("Transfer-Encoding", "chunked");

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _http1Connection.FlushAsync());
    }

    [Fact]
    public async Task RequestProcessingTaskIsUnwrapped()
    {
        var requestProcessingTask = _http1Connection.ProcessRequestsAsync<object>(null);

        var data = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n");
        await _application.Output.WriteAsync(data);

        _http1Connection.StopProcessingNextRequest(ConnectionEndReason.AppShutdownTimeout);
        Assert.IsNotType<Task<Task>>(requestProcessingTask);

        await requestProcessingTask.DefaultTimeout();
        _application.Output.Complete();
    }

    [Fact]
    public async Task RequestAbortedTokenIsResetBeforeLastWriteWithContentLength()
    {
        _http1Connection.ResponseHeaders["Content-Length"] = "12";

        var original = _http1Connection.RequestAborted;

        foreach (var ch in "hello, worl")
        {
            await _http1Connection.WriteAsync(new ArraySegment<byte>(new[] { (byte)ch }));
            Assert.Equal(original, _http1Connection.RequestAborted);
        }

        await _http1Connection.WriteAsync(new ArraySegment<byte>(new[] { (byte)'d' }));
        Assert.NotEqual(original, _http1Connection.RequestAborted);

        _http1Connection.Abort(new ConnectionAbortedException(), ConnectionEndReason.AbortedByApp);

        Assert.False(original.IsCancellationRequested);
        Assert.False(_http1Connection.RequestAborted.IsCancellationRequested);
    }

    [Fact]
    public async Task RequestAbortedTokenIsResetBeforeLastWriteAsyncWithContentLength()
    {
        _http1Connection.ResponseHeaders["Content-Length"] = "12";

        var original = _http1Connection.RequestAborted;

        foreach (var ch in "hello, worl")
        {
            await _http1Connection.WriteAsync(new ArraySegment<byte>(new[] { (byte)ch }), default(CancellationToken));
            Assert.Equal(original, _http1Connection.RequestAborted);
        }

        await _http1Connection.WriteAsync(new ArraySegment<byte>(new[] { (byte)'d' }), default(CancellationToken));
        Assert.NotEqual(original, _http1Connection.RequestAborted);

        _http1Connection.Abort(new ConnectionAbortedException(), ConnectionEndReason.AbortedByApp);

        Assert.False(original.IsCancellationRequested);
        Assert.False(_http1Connection.RequestAborted.IsCancellationRequested);
    }

    [Fact]
    public async Task BodyWriter_OnAbortedConnection_ReturnsFlushResultWithIsCompletedTrue()
    {
        var payload = Encoding.UTF8.GetBytes("hello, web browser" + new string(' ', 512) + "\n");
        var writer = _application.Output;

        var successResult = await writer.WriteAsync(payload);
        Assert.False(successResult.IsCompleted);

        _http1Connection.Abort(new ConnectionAbortedException(), ConnectionEndReason.AbortedByApp);
        var failResult = await _http1Connection.FlushPipeAsync(new CancellationToken());
        Assert.True(failResult.IsCompleted);
    }

    [Fact]
    public async Task BodyWriter_OnConnectionWithCanceledPendingFlush_ReturnsFlushResultWithIsCanceledTrue()
    {
        var payload = Encoding.UTF8.GetBytes("hello, web browser" + new string(' ', 512) + "\n");
        var writer = _application.Output;

        var successResult = await writer.WriteAsync(payload);
        Assert.False(successResult.IsCanceled);

        _http1Connection.CancelPendingFlush();

        var canceledResult = await _http1Connection.FlushPipeAsync(new CancellationToken());
        Assert.True(canceledResult.IsCanceled);

        //Cancel pending should cancel only next flush
        var goodResult = await _http1Connection.FlushPipeAsync(new CancellationToken());
        Assert.False(goodResult.IsCanceled);
    }

    [Fact]
    public async Task RequestAbortedTokenIsResetBeforeLastWriteAsyncAwaitedWithContentLength()
    {
        _http1Connection.ResponseHeaders["Content-Length"] = "12";

        var original = _http1Connection.RequestAborted;

        // Only first write can be WriteAsyncAwaited
        var startingTask = _http1Connection.InitializeResponseAwaited(Task.CompletedTask, 1);
        await _http1Connection.WriteAsyncAwaited(startingTask, new ArraySegment<byte>(new[] { (byte)'h' }), default(CancellationToken));
        Assert.Equal(original, _http1Connection.RequestAborted);

        foreach (var ch in "ello, worl")
        {
            await _http1Connection.WriteAsync(new ArraySegment<byte>(new[] { (byte)ch }), default(CancellationToken));
            Assert.Equal(original, _http1Connection.RequestAborted);
        }

        await _http1Connection.WriteAsync(new ArraySegment<byte>(new[] { (byte)'d' }), default(CancellationToken));
        Assert.NotEqual(original, _http1Connection.RequestAborted);

        _http1Connection.Abort(new ConnectionAbortedException(), ConnectionEndReason.AbortedByApp);

        Assert.False(original.IsCancellationRequested);
        Assert.False(_http1Connection.RequestAborted.IsCancellationRequested);
    }

    [Fact]
    public async Task RequestAbortedTokenIsResetBeforeLastWriteWithChunkedEncoding()
    {
        var original = _http1Connection.RequestAborted;

        _http1Connection.HttpVersion = "HTTP/1.1";
        await _http1Connection.WriteAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes("hello, world")), default(CancellationToken));
        Assert.Equal(original, _http1Connection.RequestAborted);

        await _http1Connection.ProduceEndAsync();
        Assert.NotEqual(original, _http1Connection.RequestAborted);

        _http1Connection.Abort(new ConnectionAbortedException(), ConnectionEndReason.AbortedByApp);

        Assert.False(original.IsCancellationRequested);
        Assert.False(_http1Connection.RequestAborted.IsCancellationRequested);
    }

    [Fact]
    public void RequestAbortedTokenIsFullyUsableAfterCancellation()
    {
        var originalToken = _http1Connection.RequestAborted;
        var originalRegistration = originalToken.Register(() => { });

        _http1Connection.Abort(new ConnectionAbortedException(), ConnectionEndReason.AbortedByApp);

        Assert.True(originalToken.WaitHandle.WaitOne(TestConstants.DefaultTimeout));
        Assert.True(_http1Connection.RequestAborted.WaitHandle.WaitOne(TestConstants.DefaultTimeout));

        Assert.Equal(originalToken, originalRegistration.Token);
    }

    [Fact]
    public void RequestAbortedTokenIsUsableAfterCancellation()
    {
        var originalToken = _http1Connection.RequestAborted;
        var originalRegistration = originalToken.Register(() => { });

        _http1Connection.Abort(new ConnectionAbortedException(), ConnectionEndReason.AbortedByApp);

        // The following line will throw an ODE because the original CTS backing the token has been diposed.
        // See https://github.com/dotnet/aspnetcore/pull/4447 for the history behind this test.
        //Assert.True(originalToken.WaitHandle.WaitOne(TestConstants.DefaultTimeout));
        Assert.True(_http1Connection.RequestAborted.WaitHandle.WaitOne(TestConstants.DefaultTimeout));

        Assert.Equal(originalToken, originalRegistration.Token);
    }

    [Fact]
    public async Task RequestAbortedTokenIsFiredAfterTransportReturnsCompletedFlushResult()
    {
        var originalToken = _http1Connection.RequestAborted;

        // Ensure the next call to _transport.Output.FlushAsync() returns a completed FlushResult.
        _application.Input.Complete();

        await _http1Connection.WritePipeAsync(ReadOnlyMemory<byte>.Empty, default).DefaultTimeout();

        Assert.True(originalToken.WaitHandle.WaitOne(TestConstants.DefaultTimeout));
        Assert.True(_http1Connection.RequestAborted.WaitHandle.WaitOne(TestConstants.DefaultTimeout));
    }

    [Fact]
    public async Task ExceptionDetailNotIncludedWhenLogLevelInformationNotEnabled()
    {
        var previousLog = _serviceContext.Log;

        try
        {
            _serviceContext.Log = new KestrelTrace(NullLoggerFactory.Instance);

            await _application.Output.WriteAsync(Encoding.ASCII.GetBytes($"GET /%00 HTTP/1.1\r\n"));
            var readableBuffer = (await _transport.Input.ReadAsync()).Buffer;

            var exception = Assert.ThrowsAny<Http.BadHttpRequestException>(() =>
                TakeStartLine(readableBuffer, out _consumed, out _examined));
            _transport.Input.AdvanceTo(_consumed, _examined);

            Assert.Equal(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(string.Empty), exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }
        finally
        {
            _serviceContext.Log = previousLog;
        }
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(100, 100)]
    [InlineData(600, 100)]
    [InlineData(700, 1)]
    [InlineData(1, 700)]
    public async Task AcceptsHeadersAcrossSends(int header0Count, int header1Count)
    {
        _serviceContext.ServerOptions.Limits.MaxRequestHeaderCount = header0Count + header1Count;

        var headers0 = MakeHeaders(header0Count);
        var headers1 = MakeHeaders(header1Count, header0Count);

        var requestProcessingTask = _http1Connection.ProcessRequestsAsync<object>(null);

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes("GET / HTTP/1.0\r\n"));
        await WaitForCondition(TestConstants.DefaultTimeout, () => _http1Connection.RequestHeaders != null);
        Assert.Empty(_http1Connection.RequestHeaders);

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes(headers0));
        await WaitForCondition(TestConstants.DefaultTimeout, () => _http1Connection.RequestHeaders.Count >= header0Count);
        Assert.Equal(header0Count, _http1Connection.RequestHeaders.Count);

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes(headers1));
        await WaitForCondition(TestConstants.DefaultTimeout, () => _http1Connection.RequestHeaders.Count >= header0Count + header1Count);
        Assert.Equal(header0Count + header1Count, _http1Connection.RequestHeaders.Count);

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes("\r\n"));
        await requestProcessingTask.DefaultTimeout();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(100, 100)]
    [InlineData(600, 100)]
    [InlineData(700, 1)]
    [InlineData(1, 700)]
    public async Task KeepsSameHeaderCollectionAcrossSends(int header0Count, int header1Count)
    {
        _serviceContext.ServerOptions.Limits.MaxRequestHeaderCount = header0Count + header1Count;

        var headers0 = MakeHeaders(header0Count);
        var headers1 = MakeHeaders(header1Count, header0Count);

        var requestProcessingTask = _http1Connection.ProcessRequestsAsync<object>(null);

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes("GET / HTTP/1.0\r\n"));
        await WaitForCondition(TestConstants.DefaultTimeout, () => _http1Connection.RequestHeaders != null);
        Assert.Empty(_http1Connection.RequestHeaders);

        var newRequestHeaders = new RequestHeadersWrapper(_http1Connection.RequestHeaders);
        _http1Connection.RequestHeaders = newRequestHeaders;
        Assert.Same(newRequestHeaders, _http1Connection.RequestHeaders);

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes(headers0));
        await WaitForCondition(TestConstants.DefaultTimeout, () => _http1Connection.RequestHeaders.Count >= header0Count);
        Assert.Same(newRequestHeaders, _http1Connection.RequestHeaders);
        Assert.Equal(header0Count, _http1Connection.RequestHeaders.Count);

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes(headers1));
        await WaitForCondition(TestConstants.DefaultTimeout, () => _http1Connection.RequestHeaders.Count >= header0Count + header1Count);
        Assert.Same(newRequestHeaders, _http1Connection.RequestHeaders);
        Assert.Equal(header0Count + header1Count, _http1Connection.RequestHeaders.Count);

        await _application.Output.WriteAsync(Encoding.ASCII.GetBytes("\r\n"));
        await requestProcessingTask.TimeoutAfter(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void ThrowsWhenMaxRequestBodySizeIsSetAfterReadingFromRequestBody()
    {
        // Act
        // This would normally be set by the MessageBody during the first read.
        _http1Connection.HasStartedConsumingRequestBody = true;

        // Assert
        Assert.True(((IHttpMaxRequestBodySizeFeature)_http1Connection).IsReadOnly);
        var ex = Assert.Throws<InvalidOperationException>(() => ((IHttpMaxRequestBodySizeFeature)_http1Connection).MaxRequestBodySize = 1);
        Assert.Equal(CoreStrings.MaxRequestBodySizeCannotBeModifiedAfterRead, ex.Message);
    }

    [Fact]
    public void ThrowsWhenMaxRequestBodySizeIsSetToANegativeValue()
    {
        // Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ((IHttpMaxRequestBodySizeFeature)_http1Connection).MaxRequestBodySize = -1);
        Assert.StartsWith(CoreStrings.NonNegativeNumberOrNullRequired, ex.Message);
    }

    [Fact]
    public async Task ConsumesRequestWhenApplicationDoesNotConsumeIt()
    {
        var httpApplication = new DummyApplication(async context =>
        {
            var buffer = new byte[10];
            await context.Response.Body.WriteAsync(buffer, 0, 10);
        });
        var mockMessageBody = new Mock<MessageBody>(null);
        _http1Connection.NextMessageBody = mockMessageBody.Object;

        var requestProcessingTask = _http1Connection.ProcessRequestsAsync(httpApplication);

        var data = Encoding.ASCII.GetBytes("POST / HTTP/1.1\r\nHost:\r\nConnection: close\r\ncontent-length: 1\r\n\r\n");
        await _application.Output.WriteAsync(data);
        await requestProcessingTask.DefaultTimeout();

        mockMessageBody.Verify(body => body.ConsumeAsync(), Times.Once);
    }

    [Fact]
    public void Http10HostHeaderNotRequired()
    {
        _http1Connection.HttpVersion = "HTTP/1.0";
        _http1Connection.EnsureHostHeaderExists();
    }

    [Fact]
    public void Http10HostHeaderAllowed()
    {
        _http1Connection.HttpVersion = "HTTP/1.0";
        _http1Connection.RequestHeaders.Host = "localhost:5000";
        _http1Connection.EnsureHostHeaderExists();
    }

    [Fact]
    public void Http11EmptyHostHeaderAccepted()
    {
        _http1Connection.HttpVersion = "HTTP/1.1";
        _http1Connection.RequestHeaders.Host = "";
        _http1Connection.EnsureHostHeaderExists();
    }

    [Fact]
    public void Http11ValidHostHeadersAccepted()
    {
        _http1Connection.HttpVersion = "HTTP/1.1";
        _http1Connection.RequestHeaders.Host = "localhost:5000";
        _http1Connection.EnsureHostHeaderExists();
    }

    [Fact]
    public void BadRequestFor10BadHostHeaderFormat()
    {
        _http1Connection.HttpVersion = "HTTP/1.0";
        _http1Connection.RequestHeaders.Host = "a=b";
        var ex = Assert.ThrowsAny<Http.BadHttpRequestException>(() => _http1Connection.EnsureHostHeaderExists());
        Assert.Equal(CoreStrings.FormatBadRequest_InvalidHostHeader_Detail("a=b"), ex.Message);
    }

    [Fact]
    public void BadRequestFor11BadHostHeaderFormat()
    {
        _http1Connection.HttpVersion = "HTTP/1.1";
        _http1Connection.RequestHeaders.Host = "a=b";
        var ex = Assert.ThrowsAny<Http.BadHttpRequestException>(() => _http1Connection.EnsureHostHeaderExists());
        Assert.Equal(CoreStrings.FormatBadRequest_InvalidHostHeader_Detail("a=b"), ex.Message);
    }

    [Fact]
    public void ContentLengthShouldBeRemovedWhenBothTransferEncodingAndContentLengthRequestHeadersExist()
    {
        // Arrange
        string contentLength = "1024";
        _http1Connection.RequestHeaders.Add(HeaderNames.ContentLength, contentLength);
        _http1Connection.RequestHeaders.Add(HeaderNames.TransferEncoding, "chunked");

        // Act
        Http1MessageBody.For(Kestrel.Core.Internal.Http.HttpVersion.Http11, (HttpRequestHeaders)_http1Connection.RequestHeaders, _http1Connection);

        // Assert
        Assert.True(_http1Connection.RequestHeaders.ContainsKey("X-Content-Length"));
        Assert.Equal(contentLength, _http1Connection.RequestHeaders["X-Content-Length"]);
        Assert.True(_http1Connection.RequestHeaders.ContainsKey(HeaderNames.TransferEncoding));
        Assert.Equal("chunked", _http1Connection.RequestHeaders[HeaderNames.TransferEncoding]);
        Assert.False(_http1Connection.RequestHeaders.ContainsKey(HeaderNames.ContentLength));
    }

    private bool TakeMessageHeaders(ReadOnlySequence<byte> readableBuffer, bool trailers, out SequencePosition consumed, out SequencePosition examined)
    {
        var reader = new SequenceReader<byte>(readableBuffer);
        if (_http1Connection.TakeMessageHeaders(ref reader, trailers: trailers))
        {
            consumed = reader.Position;
            examined = reader.Position;
            return true;
        }
        else
        {
            consumed = reader.Position;
            examined = readableBuffer.End;
            return false;
        }
    }

    private bool TakeStartLine(ReadOnlySequence<byte> readableBuffer, out SequencePosition consumed, out SequencePosition examined)
    {
        var reader = new SequenceReader<byte>(readableBuffer);
        if (_http1Connection.TakeStartLine(ref reader))
        {
            consumed = reader.Position;
            examined = reader.Position;
            return true;
        }
        else
        {
            consumed = reader.Position;
            examined = readableBuffer.End;
            return false;
        }
    }

    private bool ParseRequest(ReadOnlySequence<byte> readableBuffer, out SequencePosition consumed, out SequencePosition examined)
    {
        var reader = new SequenceReader<byte>(readableBuffer);
        if (_http1Connection.ParseRequest(ref reader))
        {
            consumed = reader.Position;
            examined = reader.Position;
            return true;
        }
        else
        {
            consumed = reader.Position;
            examined = readableBuffer.End;
            return false;
        }
    }

    private static async Task WaitForCondition(TimeSpan timeout, Func<bool> condition)
    {
        const int MaxWaitLoop = 150;

        var delay = (int)Math.Ceiling(timeout.TotalMilliseconds / MaxWaitLoop);

        var waitLoop = 0;
        while (waitLoop < MaxWaitLoop && !condition())
        {
            // Wait for parsing condition to trigger
            await Task.Delay(delay);
            waitLoop++;
        }
    }

    private static string MakeHeaders(int count, int startAt = 0)
    {
        return string.Join("", Enumerable
            .Range(0, count)
            .Select(i => $"Header-{startAt + i}: value{startAt + i}\r\n"));
    }

    public static IEnumerable<object[]> RequestLineValidData => HttpParsingData.RequestLineValidData;

    public static IEnumerable<object[]> RequestLineDotSegmentData => HttpParsingData.RequestLineDotSegmentData;

    public static TheoryData<string> TargetWithEncodedNullCharData
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var target in HttpParsingData.TargetWithEncodedNullCharData)
            {
                data.Add(target);
            }

            return data;
        }
    }

    public static TheoryData<string, string> TargetInvalidData
        => HttpParsingData.TargetInvalidData;

    public static TheoryData<string, int> MethodNotAllowedTargetData
        => HttpParsingData.MethodNotAllowedRequestLine;

    public static TheoryData<string> TargetWithNullCharData
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var target in HttpParsingData.TargetWithNullCharData)
            {
                data.Add(target);
            }

            return data;
        }
    }

    public static TheoryData<string> MethodWithNullCharData
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var target in HttpParsingData.MethodWithNullCharData)
            {
                data.Add(target);
            }

            return data;
        }
    }

    public static TheoryData<string> QueryStringWithNullCharData
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var target in HttpParsingData.QueryStringWithNullCharData)
            {
                data.Add(target);
            }

            return data;
        }
    }

    public static TheoryData<MinDataRate> MinDataRateData => new TheoryData<MinDataRate>
        {
            null,
            new MinDataRate(bytesPerSecond: 1, gracePeriod: TimeSpan.MaxValue)
        };

    private class RequestHeadersWrapper : IHeaderDictionary
    {
        readonly IHeaderDictionary _innerHeaders;

        public RequestHeadersWrapper(IHeaderDictionary headers)
        {
            _innerHeaders = headers;
        }

        public StringValues this[string key] { get => _innerHeaders[key]; set => _innerHeaders[key] = value; }
        public long? ContentLength { get => _innerHeaders.ContentLength; set => _innerHeaders.ContentLength = value; }
        public ICollection<string> Keys => _innerHeaders.Keys;
        public ICollection<StringValues> Values => _innerHeaders.Values;
        public int Count => _innerHeaders.Count;
        public bool IsReadOnly => _innerHeaders.IsReadOnly;
        public void Add(string key, StringValues value) => _innerHeaders.Add(key, value);
        public void Add(KeyValuePair<string, StringValues> item) => _innerHeaders.Add(item);
        public void Clear() => _innerHeaders.Clear();
        public bool Contains(KeyValuePair<string, StringValues> item) => _innerHeaders.Contains(item);
        public bool ContainsKey(string key) => _innerHeaders.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex) => _innerHeaders.CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => _innerHeaders.GetEnumerator();
        public bool Remove(string key) => _innerHeaders.Remove(key);
        public bool Remove(KeyValuePair<string, StringValues> item) => _innerHeaders.Remove(item);
        public bool TryGetValue(string key, out StringValues value) => _innerHeaders.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => _innerHeaders.GetEnumerator();
    }
}
