// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

// https://datatracker.ietf.org/doc/html/rfc8441
public class Http2WebSocketTests : Http2TestBase
{
    [Fact]
    public async Task HEADERS_Received_ExtendedCONNECTMethod_Received()
    {
        await InitializeConnectionAsync(async context =>
        {
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            Assert.True(connectFeature.IsExtendedConnect);
            Assert.Equal(HttpMethods.Connect, context.Request.Method);
            Assert.Equal("websocket", connectFeature.Protocol);
            Assert.False(context.Request.Headers.TryGetValue(":protocol", out var _));
            Assert.Equal("http", context.Request.Scheme);
            Assert.Equal("/chat", context.Request.Path.Value);
            Assert.Equal("server.example.com", context.Request.Host.Value);
            Assert.Equal("chat, superchat", context.Request.Headers.WebSocketSubProtocols);
            Assert.Equal("permessage-deflate", context.Request.Headers.SecWebSocketExtensions);
            Assert.Equal("13", context.Request.Headers.SecWebSocketVersion);
            Assert.Equal("http://www.example.com", context.Request.Headers.Origin);

            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1]));
        });

        // HEADERS + END_HEADERS
        // :method = CONNECT
        // :protocol = websocket
        // :scheme = https
        // :path = /chat
        // :authority = server.example.com
        // sec-websocket-protocol = chat, superchat
        // sec-websocket-extensions = permessage-deflate
        // sec-websocket-version = 13
        // origin = http://www.example.com
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[InternalHeaderNames.Status]);
    }

    [Fact]
    public async Task HEADERS_Received_ExtendedCONNECTMethod_Accepted()
    {
        await InitializeConnectionAsync(async context =>
        {
            var requestBodyDetectionFeature = context.Features.Get<IHttpRequestBodyDetectionFeature>();
            Assert.False(requestBodyDetectionFeature.CanHaveBody);

            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            Assert.True(connectFeature.IsExtendedConnect);
            Assert.Equal(HttpMethods.Connect, context.Request.Method);
            Assert.Equal("websocket", connectFeature.Protocol);
            Assert.False(context.Request.Headers.TryGetValue(":protocol", out var _));
            Assert.Equal("http", context.Request.Scheme);
            Assert.Equal("/chat", context.Request.Path.Value);
            Assert.Equal("server.example.com", context.Request.Host.Value);
            Assert.Equal("chat, superchat", context.Request.Headers.WebSocketSubProtocols);
            Assert.Equal("permessage-deflate", context.Request.Headers.SecWebSocketExtensions);
            Assert.Equal("13", context.Request.Headers.SecWebSocketVersion);
            Assert.Equal("http://www.example.com", context.Request.Headers.Origin);

            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1]));

            var stream = await connectFeature.AcceptAsync();
            Assert.Equal(0, await stream.ReadAsync(new byte[1]));
            await stream.WriteAsync(new byte[] { 0x01 });
        });

        // HEADERS + END_HEADERS
        // :method = CONNECT
        // :protocol = websocket
        // :scheme = https
        // :path = /chat
        // :authority = server.example.com
        // sec-websocket-protocol = chat, superchat
        // sec-websocket-extensions = permessage-deflate
        // sec-websocket-version = 13
        // origin = http://www.example.com
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);
        await SendDataAsync(1, Array.Empty<byte>(), endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[InternalHeaderNames.Status]);

        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task HEADERS_Received_SecondRequest_Accepted()
    {
        var appDelegateTcs = new TaskCompletionSource();
        await InitializeConnectionAsync(async context =>
        {
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            Assert.True(connectFeature.IsExtendedConnect);
            Assert.Equal(HttpMethods.Connect, context.Request.Method);
            Assert.Equal("websocket", connectFeature.Protocol);
            Assert.False(context.Request.Headers.TryGetValue(":protocol", out var _));
            Assert.Equal("http", context.Request.Scheme);
            Assert.Equal("/chat", context.Request.Path.Value);
            Assert.Equal("server.example.com", context.Request.Host.Value);
            Assert.Equal("chat, superchat", context.Request.Headers.WebSocketSubProtocols);
            Assert.Equal("permessage-deflate", context.Request.Headers.SecWebSocketExtensions);
            Assert.Equal("13", context.Request.Headers.SecWebSocketVersion);
            Assert.Equal("http://www.example.com", context.Request.Headers.Origin);

            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1]));

            context.Response.StatusCode = StatusCodes.Status201Created; // Any 2XX should work
            var stream = await connectFeature.AcceptAsync();
            Assert.Equal(0, await stream.ReadAsync(new byte[1]));
            await stream.WriteAsync(new byte[] { 0x01 });
            await appDelegateTcs.Task;
        });

        var originalHandler = _connection._streamLifetimeHandler;
        var tcs = new TaskCompletionSource();
        var streamLifetimeHandler = new Mock<IHttp2StreamLifetimeHandler>();
        streamLifetimeHandler.Setup(o => o.OnStreamCompleted(It.IsAny<Http2Stream>())).Callback((Http2Stream stream) =>
        {
            // Add stream to Http2Connection._completedStreams.
            originalHandler.OnStreamCompleted(stream);

            // Unblock test code that will call TriggerTick and return the stream to the pool
            tcs.TrySetResult();
        });
        _connection._streamLifetimeHandler = streamLifetimeHandler.Object;

        // HEADERS + END_HEADERS
        // :method = CONNECT
        // :protocol = websocket
        // :scheme = https
        // :path = /chat
        // :authority = server.example.com
        // sec-websocket-protocol = chat, superchat
        // sec-websocket-extensions = permessage-deflate
        // sec-websocket-version = 13
        // origin = http://www.example.com
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);
        await SendDataAsync(1, Array.Empty<byte>(), endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("201", _decodedHeaders[InternalHeaderNames.Status]);

        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        appDelegateTcs.TrySetResult();
        await tcs.Task.DefaultTimeout();

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream has been returned to the pool
        Assert.Equal(1, _connection.StreamPool.Count);
        Assert.True(_connection.StreamPool.TryPeek(out var pooledStream));

        await SendHeadersAsync(3, Http2HeadersFrameFlags.END_HEADERS, headers);
        await SendDataAsync(3, Array.Empty<byte>(), endStream: true);

        headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);

        _decodedHeaders.Clear();
        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("201", _decodedHeaders[InternalHeaderNames.Status]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 3);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
    }

    [Theory]
    [InlineData(":path", "/")]
    [InlineData(":scheme", "http")]
    public async Task HEADERS_Received_ExtendedCONNECTMethod_WithoutSchemeOrPath_Reset(string headerName, string value)
    {
        await InitializeConnectionAsync(_noopApplication);

        // :path and :scheme are required with :protocol, :authority is optional
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "WebSocket"),
            new KeyValuePair<string, string>(headerName, value)
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

        await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.ConnectRequestsWithProtocolRequireSchemeAndPath);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task HEADERS_Received_ProtocolWithoutCONNECTMethod_Reset()
    {
        await InitializeConnectionAsync(_noopApplication);

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "example.com"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "WebSocket")
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

        await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.ProtocolRequiresConnect);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task ExtendedCONNECT_AcceptAsyncStream_IsNotLimitedByMinRequestBodyDataRate()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        await InitializeConnectionAsync(async context =>
        {
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            var stream = await connectFeature.AcceptAsync();
            Assert.Equal(0, await stream.ReadAsync(new byte[1]));
            await stream.WriteAsync(new byte[] { 0x01 });
        });

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);

        // Don't send any more data and advance just to and then past the grace period.
        AdvanceTime(limits.MinRequestBodyDataRate.GracePeriod + TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        await SendDataAsync(1, Array.Empty<byte>(), endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[InternalHeaderNames.Status]);

        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task ExtendedCONNECT_AcceptAsyncStream_IsNotLimitedByMaxRequestBodySize()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // We're going to send more than the MaxRequestBodySize bytes from the client to the server over the connection
        // Since this is not a request body, this should be allowed like it would be for an upgraded connection.
        limits.MaxRequestBodySize = 5;

        await InitializeConnectionAsync(async context =>
        {
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            var maxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            // Extended connects don't have a meaningful request body to limit.
            Assert.True(maxRequestBodySizeFeature.IsReadOnly);
            var stream = await connectFeature.AcceptAsync();
            Assert.True(maxRequestBodySizeFeature.IsReadOnly);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            Assert.Equal(_serviceContext.ServerOptions.Limits.MaxRequestBodySize + 1, memoryStream.Length);
            await stream.WriteAsync(new byte[] { 0x01 });
        });

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);

        await SendDataAsync(1, new byte[(int)limits.MaxRequestBodySize + 1], endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[InternalHeaderNames.Status]);

        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task ExtendedCONNECTMethod_DoesNotProvideUsableBodyStreams()
    {
        await InitializeConnectionAsync(async context =>
        {
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            // We could throw, but no-oping is adequate.
            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1]));
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.Body.WriteAsync(new byte[1] { 0x00 }).AsTask());
            Assert.Equal(CoreStrings.FormatConnectResponseCanNotHaveBody(200), ex.Message);
            ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.BodyWriter.WriteAsync(new byte[1] { 0x00 }).AsTask());
            Assert.Equal(CoreStrings.FormatConnectResponseCanNotHaveBody(200), ex.Message);

            var stream = await connectFeature.AcceptAsync();

            // The body streams still shouldn't work after Accept
            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1]));
            ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.Body.WriteAsync(new byte[1] { 0x00 }).AsTask());
            Assert.Equal(CoreStrings.FormatConnectResponseCanNotHaveBody(200), ex.Message);
            ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.BodyWriter.WriteAsync(new byte[1] { 0x00 }).AsTask());
            Assert.Equal(CoreStrings.FormatConnectResponseCanNotHaveBody(200), ex.Message);

            // The connect stream should work
            Assert.Equal(1, await stream.ReadAsync(new byte[1]));
            await stream.WriteAsync(new byte[] { 0x01 });
        });

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);

        await SendDataAsync(1, new byte[10241], endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[InternalHeaderNames.Status]);

        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task ExtendedCONNECTMethod_CanHaveNon200ResponseWithBody()
    {
        var finishedSendingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await InitializeConnectionAsync(async context =>
        {
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            Assert.True(connectFeature.IsExtendedConnect);

            // The EndStreamReceived flag might not have been sent let alone received by the server by the time application code completes
            // which would result in a RST_STREAM being sent to the client. We wait for the data frame to finish sending
            // before allowing application code to complete (relies on inline Pipe completions which we use for tests)
            await finishedSendingTcs.Task;

            context.Response.StatusCode = Http.StatusCodes.Status418ImATeapot;
            context.Response.ContentLength = 2;
            await context.Response.Body.WriteAsync(new byte[1] { 0x01 });
            await context.Response.BodyWriter.WriteAsync(new byte[1] { 0x02 });
        });

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);

        await SendDataAsync(1, new byte[10241], endStream: true);

        finishedSendingTcs.SetResult();

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 40,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("418", _decodedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("2", _decodedHeaders[HeaderNames.ContentLength]);

        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        Assert.Equal(0x02, dataFrame.Payload.Span[0]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task HEADERS_Received_SecondRequest_ConnectProtocolReset()
    {
        var appDelegateTcs = new TaskCompletionSource();
        var requestCount = 0;
        await InitializeConnectionAsync(async context =>
        {
            requestCount++;

            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();

            if (requestCount == 1)
            {
                Assert.True(connectFeature.IsExtendedConnect);
                Assert.Equal(HttpMethods.Connect, context.Request.Method);
                Assert.Equal("websocket", connectFeature.Protocol);
                context.Response.StatusCode = StatusCodes.Status201Created; // Any 2XX should work

                var stream = await connectFeature.AcceptAsync();
                await stream.WriteAsync(new byte[] { 0x01 });
                await appDelegateTcs.Task;
            }
            else
            {
                if (connectFeature.Protocol != null)
                {
                    throw new Exception("ConnectProtocol should be null here. The fact that it is not indicates that we are not resetting properly between requests.");
                }

                // We've done the test. Now just return the normal echo server behavior.
                await _echoApplication(context);
            }
        });

        var originalHandler = _connection._streamLifetimeHandler;
        var tcs = new TaskCompletionSource();
        var streamLifetimeHandler = new Mock<IHttp2StreamLifetimeHandler>();
        streamLifetimeHandler.Setup(o => o.OnStreamCompleted(It.IsAny<Http2Stream>())).Callback((Http2Stream stream) =>
        {
            // Add stream to Http2Connection._completedStreams.
            originalHandler.OnStreamCompleted(stream);

            if (requestCount == 1)
            {
                // Unblock test code that will call TriggerTick and return the stream to the pool
                tcs.TrySetResult();
            }
        });
        _connection._streamLifetimeHandler = streamLifetimeHandler.Object;

        // HEADERS + END_HEADERS
        // :method = CONNECT
        // :protocol = websocket
        // :scheme = https
        // :path = /chat
        // :authority = server.example.com
        // sec-websocket-protocol = chat, superchat
        // sec-websocket-extensions = permessage-deflate
        // sec-websocket-version = 13
        // origin = http://www.example.com
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);
        await SendDataAsync(1, Array.Empty<byte>(), endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        appDelegateTcs.TrySetResult();
        await tcs.Task.DefaultTimeout();

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream has been returned to the pool
        Assert.Equal(1, _connection.StreamPool.Count);
        Assert.True(_connection.StreamPool.TryPeek(out var pooledStream));

        // Next is a plain GET.
        var headers2 = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "example.com"),
        };

        await StartStreamAsync(3, headers2, endStream: false);
        await SendDataAsync(3, _helloBytes, endStream: true);

        // If the echo server doesn't give us the expected responses, the test has failed.
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 3);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
    }
}
