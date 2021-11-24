// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Http;
using System.Net.Http.HPack;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

// https://datatracker.ietf.org/doc/html/rfc8441
public class Http2WebSocketTests : Http2TestBase
{
    // Core CONNECT happy path
    // Trailers from client?
    // Content-length from client?
    // Reset from client?
    // Non success response
    // Response trailers
    // Response RST

    [Fact]
    public async Task HEADERS_Received_CONNECTMethod_Accepted()
    {
        await InitializeConnectionAsync(async context =>
        {
            Assert.Equal(HttpMethods.Connect, context.Request.Method);
            Assert.Equal("websocket", context.Features.Get<IHttpRequestFeature>().ConnectProtocol);
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
            new KeyValuePair<string, string>(HeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(HeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);
        await SendDataAsync(1, Array.Empty<byte>(), endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders["content-length"]);
    }

    [Theory]
    [InlineData(":path", "/")]
    [InlineData(":scheme", "http")]
    public async Task HEADERS_Received_CONNECTMethod_WithSchemeOrPath_Reset(string headerName, string value)
    {
        await InitializeConnectionAsync(_noopApplication);

        // :path and :scheme are not allowed, :authority is optional
        var headers = new[] { new KeyValuePair<string, string>(HeaderNames.Method, "CONNECT"),
                new KeyValuePair<string, string>(headerName, value) };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

        await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.Http2ErrorConnectMustNotSendSchemeOrPath);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MaxRequestBodySize_AppCanRaiseLimit(bool includeContentLength)
    {
        _serviceContext.ServerOptions.Limits.MaxRequestBodySize = 10;
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        if (includeContentLength)
        {
            headers.Concat(new[]
                {
                        new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
                    });
        }
        await InitializeConnectionAsync(async context =>
        {
            Assert.False(context.Features.Get<IHttpMaxRequestBodySizeFeature>().IsReadOnly);
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 12;
            var buffer = new byte[100];
            var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(12, read);
            Assert.True(context.Features.Get<IHttpMaxRequestBodySizeFeature>().IsReadOnly);
            read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(0, read);
        });

        await StartStreamAsync(1, headers, endStream: false);
        await SendDataAsync(1, new byte[12], endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task ResponseHeaders_WithNonAscii_Throws()
    {
        await InitializeConnectionAsync(async context =>
        {
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("Custom你好Name", "Custom Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.ContentType = "Custom 你好 Type");
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("CustomName", "Custom 你好 Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("CustomName", "Custom \r Value"));
            await context.Response.WriteAsync("Hello World");
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
    }

    [Fact]
    public async Task ResponseHeaders_WithNonAsciiAndCustomEncoder_Works()
    {
        _serviceContext.ServerOptions.ResponseHeaderEncodingSelector = _ => Encoding.UTF8;
        _serviceContext.ServerOptions.RequestHeaderEncodingSelector = _ => Encoding.UTF8; // Used for decoding response.

        await InitializeConnectionAsync(async context =>
        {
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("Custom你好Name", "Custom Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("CustomName", "Custom \r Value"));
            context.Response.ContentType = "Custom 你好 Type";
            context.Response.Headers.Append("CustomName", "Custom 你好 Value");
            await context.Response.WriteAsync("Hello World");
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 84,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(4, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("Custom 你好 Type", _decodedHeaders[HeaderNames.ContentType]);
        Assert.Equal("Custom 你好 Value", _decodedHeaders["CustomName"]);
    }

    [Fact]
    public async Task ResponseHeaders_WithInvalidValuesAndCustomEncoder_AbortsConnection()
    {
        var encoding = Encoding.GetEncoding(Encoding.Latin1.CodePage, EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
        _serviceContext.ServerOptions.ResponseHeaderEncodingSelector = _ => encoding;

        await InitializeConnectionAsync(async context =>
        {
            context.Response.Headers.Append("CustomName", "Custom 你好 Value");
            await context.Response.WriteAsync("Hello World");
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await WaitForConnectionErrorAsync<Exception>(ignoreNonGoAwayFrames: false, int.MaxValue, Http2ErrorCode.INTERNAL_ERROR);
    }

    [Fact]
    public async Task ResponseTrailers_WithoutData_Sent()
    {
        await InitializeConnectionAsync(context =>
        {
            context.Response.AppendTrailer("CustomName", "Custom Value");
            return Task.CompletedTask;
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        var trailersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 25,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);

        _decodedHeaders.Clear();

        _hpackDecoder.Decode(trailersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Single(_decodedHeaders);
        Assert.Equal("Custom Value", _decodedHeaders["CustomName"]);
    }

    [Fact]
    public async Task ResponseTrailers_WithExeption500_Cleared()
    {
        await InitializeConnectionAsync(context =>
        {
            context.Response.AppendTrailer("CustomName", "Custom Value");
            throw new NotImplementedException("Test Exception");
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_STREAM | Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("500", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task ResponseTrailers_WorksAcrossMultipleStreams_Cleared()
    {
        await InitializeConnectionAsync(context =>
        {
            Assert.True(context.Response.SupportsTrailers(), "SupportsTrailers");

            var trailers = context.Features.Get<IHttpResponseTrailersFeature>().Trailers;
            Assert.False(trailers.IsReadOnly);

            context.Response.AppendTrailer("CustomName", "Custom Value");
            return Task.CompletedTask;
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame1 = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1);
        var trailersFrame1 = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 25,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        var headersFrame2 = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 6,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 3);

        var trailersFrame2 = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 1,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(trailersFrame1.PayloadSequence, endHeaders: true, handler: this);

        Assert.Single(_decodedHeaders);
        Assert.Equal("Custom Value", _decodedHeaders["CustomName"]);

        _decodedHeaders.Clear();

        _hpackDecoder.Decode(trailersFrame2.PayloadSequence, endHeaders: true, handler: this);

        Assert.Single(_decodedHeaders);
        Assert.Equal("Custom Value", _decodedHeaders["CustomName"]);
    }

    [Fact]
    public async Task ResponseTrailers_WithData_Sent()
    {
        await InitializeConnectionAsync(async context =>
        {
            await context.Response.WriteAsync("Hello World");
            context.Response.AppendTrailer("CustomName", "Custom Value");
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        var trailersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 25,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        _decodedHeaders.Clear();

        _hpackDecoder.Decode(trailersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Single(_decodedHeaders);
        Assert.Equal("Custom Value", _decodedHeaders["CustomName"]);
    }

    [Fact]
    public async Task ResponseTrailers_WithContinuation_Sent()
    {
        var largeHeader = new string('a', 1024 * 3);
        await InitializeConnectionAsync(async context =>
        {
            await context.Response.WriteAsync("Hello World");
                // The first five fill the first frame
                context.Response.AppendTrailer("CustomName0", largeHeader);
            context.Response.AppendTrailer("CustomName1", largeHeader);
            context.Response.AppendTrailer("CustomName2", largeHeader);
            context.Response.AppendTrailer("CustomName3", largeHeader);
            context.Response.AppendTrailer("CustomName4", largeHeader);
                // This one spills over to the next frame
                context.Response.AppendTrailer("CustomName5", largeHeader);
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        var trailersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 15440,
            withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
            withStreamId: 1);

        var trailersContinuationFrame = await ExpectAsync(Http2FrameType.CONTINUATION,
            withLength: 3088,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        _decodedHeaders.Clear();

        _hpackDecoder.Decode(trailersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(5, _decodedHeaders.Count);
        Assert.Equal(largeHeader, _decodedHeaders["CustomName0"]);
        Assert.Equal(largeHeader, _decodedHeaders["CustomName1"]);
        Assert.Equal(largeHeader, _decodedHeaders["CustomName2"]);
        Assert.Equal(largeHeader, _decodedHeaders["CustomName3"]);
        Assert.Equal(largeHeader, _decodedHeaders["CustomName4"]);

        _decodedHeaders.Clear();

        _hpackDecoder.Decode(trailersContinuationFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Single(_decodedHeaders);
        Assert.Equal(largeHeader, _decodedHeaders["CustomName5"]);
    }

    [Fact]
    public async Task ResponseTrailers_WithNonAscii_Throws()
    {
        await InitializeConnectionAsync(async context =>
        {
            await context.Response.WriteAsync("Hello World");
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer("Custom你好Name", "Custom Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer("CustomName", "Custom 你好 Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer("CustomName", "Custom \r Value"));
                // ETag is one of the few special cased trailers. Accept is not.
                Assert.Throws<InvalidOperationException>(() => context.Features.Get<IHttpResponseTrailersFeature>().Trailers.ETag = "Custom 你好 Tag");
            Assert.Throws<InvalidOperationException>(() => context.Features.Get<IHttpResponseTrailersFeature>().Trailers.Accept = "Custom 你好 Tag");
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
    }

    [Fact]
    public async Task ResponseTrailers_WithNonAsciiAndCustomEncoder_Works()
    {
        _serviceContext.ServerOptions.ResponseHeaderEncodingSelector = _ => Encoding.UTF8;
        _serviceContext.ServerOptions.RequestHeaderEncodingSelector = _ => Encoding.UTF8; // Used for decoding response.

        await InitializeConnectionAsync(async context =>
        {
            await context.Response.WriteAsync("Hello World");
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer("Custom你好Name", "Custom Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer("CustomName", "Custom \r Value"));
            context.Response.AppendTrailer("CustomName", "Custom 你好 Value");
                // ETag is one of the few special cased trailers. Accept is not.
                context.Features.Get<IHttpResponseTrailersFeature>().Trailers.ETag = "Custom 你好 Tag";
            context.Features.Get<IHttpResponseTrailersFeature>().Trailers.Accept = "Custom 你好 Accept";
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        var trailersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 80,
            withFlags: (byte)(Http2HeadersFrameFlags.END_STREAM | Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        _decodedHeaders.Clear();

        _hpackDecoder.Decode(trailersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Equal("Custom 你好 Value", _decodedHeaders["CustomName"]);
        Assert.Equal("Custom 你好 Tag", _decodedHeaders[HeaderNames.ETag]);
        Assert.Equal("Custom 你好 Accept", _decodedHeaders[HeaderNames.Accept]);
    }

    [Fact]
    public async Task ResponseTrailers_WithInvalidValuesAndCustomEncoder_AbortsConnection()
    {
        var encoding = Encoding.GetEncoding(Encoding.Latin1.CodePage, EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
        _serviceContext.ServerOptions.ResponseHeaderEncodingSelector = _ => encoding;

        await InitializeConnectionAsync(async context =>
        {
            await context.Response.WriteAsync("Hello World");
            context.Response.AppendTrailer("CustomName", "Custom 你好 Value");
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        await WaitForConnectionErrorAsync<Exception>(ignoreNonGoAwayFrames: false, int.MaxValue, Http2ErrorCode.INTERNAL_ERROR);
    }

    [Fact]
    public async Task ResponseTrailers_TooLong_Throws()
    {
        await InitializeConnectionAsync(async context =>
        {
            await context.Response.WriteAsync("Hello World");
            context.Response.AppendTrailer("too_long", new string('a', (int)Http2PeerSettings.DefaultMaxFrameSize));
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        var goAway = await ExpectAsync(Http2FrameType.GOAWAY,
            withLength: 8,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 0);

        VerifyGoAway(goAway, int.MaxValue, Http2ErrorCode.INTERNAL_ERROR);

        _pair.Application.Output.Complete();
        await _connectionTask;

        var message = Assert.Single(LogMessages, m => m.Exception is HPackEncodingException);
        Assert.Contains(SR.net_http_hpack_encode_failure, message.Exception.Message);
    }

    [Fact]
    public async Task ResponseTrailers_WithLargeUnflushedData_DataExceedsFlowControlAvailableAndNotSentWithTrailers()
    {
        const int windowSize = (int)Http2PeerSettings.DefaultMaxFrameSize;
        _clientSettings.InitialWindowSize = windowSize;

        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            await context.Response.StartAsync();

                // Body exceeds flow control available and requires the client to allow more
                // data via updating the window
                context.Response.BodyWriter.GetMemory(windowSize + 1);
            context.Response.BodyWriter.Advance(windowSize + 1);

            context.Response.AppendTrailer("CustomName", "Custom Value");
        }).DefaultTimeout();

        await StartStreamAsync(1, headers, endStream: true).DefaultTimeout();

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1).DefaultTimeout();

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 16384,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1).DefaultTimeout();

        var dataTask = ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1).DefaultTimeout();

        // Reading final frame of data requires window update
        // Verify this data task is waiting on window update
        Assert.False(dataTask.IsCompletedSuccessfully);

        await SendWindowUpdateAsync(1, 1);

        await dataTask;

        var trailersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 25,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1).DefaultTimeout();

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false).DefaultTimeout();

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        _decodedHeaders.Clear();
        _hpackDecoder.Decode(trailersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Single(_decodedHeaders);
        Assert.Equal("Custom Value", _decodedHeaders["CustomName"]);
    }

    [Fact]
    public async Task ResponseTrailers_WithUnflushedData_DataSentWithTrailers()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            await context.Response.StartAsync();

            var s = context.Response.BodyWriter.GetMemory(1);
            s.Span[0] = byte.MaxValue;
            context.Response.BodyWriter.Advance(1);

            context.Response.AppendTrailer("CustomName", "Custom Value");
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        var trailersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 25,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        _decodedHeaders.Clear();
        _hpackDecoder.Decode(trailersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Single(_decodedHeaders);
        Assert.Equal("Custom Value", _decodedHeaders["CustomName"]);
    }

    [Fact]
    public async Task ApplicationException_BeforeFirstWrite_Sends500()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(context =>
        {
            throw new Exception("App Faulted");
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        Assert.Contains(LogMessages, m => (m.Exception?.Message.Contains("App Faulted") ?? false) && m.LogLevel == LogLevel.Error);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("500", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task ApplicationException_AfterFirstWrite_Resets()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            await context.Response.WriteAsync("hello,");
            throw new Exception("App Faulted");
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 6,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await WaitForStreamErrorAsync(1, Http2ErrorCode.INTERNAL_ERROR, "App Faulted");

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
    }

    [Fact]
    public async Task RST_STREAM_Received_AbortsStream()
    {
        await InitializeConnectionAsync(_waitForAbortApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
        await SendRstStreamAsync(1);
        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task RST_STREAM_Received_AbortsStream_StreamFlushedDataNotSent()
    {
        await InitializeConnectionAsync(async context =>
        {
            var streamIdFeature = context.Features.Get<IHttp2StreamIdFeature>();
            var sem = new SemaphoreSlim(0);

            context.RequestAborted.Register(() =>
            {
                lock (_abortedStreamIdsLock)
                {
                    _abortedStreamIds.Add(streamIdFeature.StreamId);
                }

                sem.Release();
            });

            await sem.WaitAsync().DefaultTimeout();

            await context.Response.Body.WriteAsync(new byte[10], 0, 10);

            _runningStreams[streamIdFeature.StreamId].TrySetResult();
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
        await SendRstStreamAsync(1);
        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task RST_STREAM_Received_AbortsStream_PipeWriterFlushedDataNotSent()
    {
        await InitializeConnectionAsync(async context =>
        {
            var streamIdFeature = context.Features.Get<IHttp2StreamIdFeature>();
            var sem = new SemaphoreSlim(0);

            context.RequestAborted.Register(() =>
            {
                lock (_abortedStreamIdsLock)
                {
                    _abortedStreamIds.Add(streamIdFeature.StreamId);
                }

                sem.Release();
            });

            await sem.WaitAsync().DefaultTimeout();

            context.Response.BodyWriter.GetMemory();
            context.Response.BodyWriter.Advance(10);
            await context.Response.BodyWriter.FlushAsync();

            _runningStreams[streamIdFeature.StreamId].TrySetResult();
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
        await SendRstStreamAsync(1);
        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task RST_STREAM_WaitingForRequestBody_RequestBodyThrows()
    {
        var sem = new SemaphoreSlim(0);
        await InitializeConnectionAsync(async context =>
        {
            var streamIdFeature = context.Features.Get<IHttp2StreamIdFeature>();

            try
            {
                var readTask = context.Request.Body.ReadAsync(new byte[100], 0, 100).DefaultTimeout();
                sem.Release();
                await readTask;

                _runningStreams[streamIdFeature.StreamId].TrySetException(new Exception("ReadAsync was expected to throw."));
            }
            catch (IOException) // Expected failure
                {
                await context.Response.Body.WriteAsync(new byte[10], 0, 10);

                lock (_abortedStreamIdsLock)
                {
                    _abortedStreamIds.Add(streamIdFeature.StreamId);
                }

                _runningStreams[streamIdFeature.StreamId].TrySetResult();
            }
            catch (Exception ex)
            {
                _runningStreams[streamIdFeature.StreamId].TrySetException(ex);
            }
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await sem.WaitAsync().DefaultTimeout();
        await SendRstStreamAsync(1);
        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task RST_STREAM_IncompleteRequest_RequestBodyThrows()
    {
        var sem = new SemaphoreSlim(0);
        await InitializeConnectionAsync(async context =>
        {
            var streamIdFeature = context.Features.Get<IHttp2StreamIdFeature>();

            try
            {
                var read = await context.Request.Body.ReadAsync(new byte[100], 0, 100).DefaultTimeout();
                var readTask = context.Request.Body.ReadAsync(new byte[100], 0, 100).DefaultTimeout();
                sem.Release();
                await readTask;

                _runningStreams[streamIdFeature.StreamId].TrySetException(new Exception("ReadAsync was expected to throw."));
            }
            catch (IOException) // Expected failure
                {
                await context.Response.Body.WriteAsync(new byte[10], 0, 10);

                lock (_abortedStreamIdsLock)
                {
                    _abortedStreamIds.Add(streamIdFeature.StreamId);
                }

                _runningStreams[streamIdFeature.StreamId].TrySetResult();
            }
            catch (Exception ex)
            {
                _runningStreams[streamIdFeature.StreamId].TrySetException(ex);
            }
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, new byte[10], endStream: false);
        await sem.WaitAsync().DefaultTimeout();
        await SendRstStreamAsync(1);
        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task RequestAbort_SendsRstStream()
    {
        await InitializeConnectionAsync(async context =>
        {
            var streamIdFeature = context.Features.Get<IHttp2StreamIdFeature>();

            try
            {
                context.RequestAborted.Register(() =>
                {
                    lock (_abortedStreamIdsLock)
                    {
                        _abortedStreamIds.Add(streamIdFeature.StreamId);
                    }

                    _runningStreams[streamIdFeature.StreamId].TrySetResult();
                });

                context.Abort();

                    // Not sent
                    await context.Response.Body.WriteAsync(new byte[10], 0, 10);

                await _runningStreams[streamIdFeature.StreamId].Task;
            }
            catch (Exception ex)
            {
                _runningStreams[streamIdFeature.StreamId].TrySetException(ex);
            }
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
        await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.INTERNAL_ERROR, CoreStrings.ConnectionAbortedByApplication);
        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task RequestAbort_AfterDataSent_SendsRstStream()
    {
        await InitializeConnectionAsync(async context =>
        {
            var streamIdFeature = context.Features.Get<IHttp2StreamIdFeature>();

            try
            {
                context.RequestAborted.Register(() =>
                {
                    lock (_abortedStreamIdsLock)
                    {
                        _abortedStreamIds.Add(streamIdFeature.StreamId);
                    }

                    _runningStreams[streamIdFeature.StreamId].TrySetResult();
                });

                await context.Response.Body.WriteAsync(new byte[10], 0, 10);

                context.Abort();

                    // Not sent
                    await context.Response.Body.WriteAsync(new byte[11], 0, 11);

                await _runningStreams[streamIdFeature.StreamId].Task;
            }
            catch (Exception ex)
            {
                _runningStreams[streamIdFeature.StreamId].TrySetException(ex);
            }
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 10,
            withFlags: 0,
            withStreamId: 1);

        await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.INTERNAL_ERROR, CoreStrings.ConnectionAbortedByApplication);
        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task RequestAbort_ThrowsOperationCanceledExceptionFromSubsequentRequestBodyStreamRead()
    {
        OperationCanceledException thrownEx = null;

        await InitializeConnectionAsync(async context =>
        {
            context.Abort();

            var buffer = new byte[100];
            var thrownExTask = Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Request.Body.ReadAsync(buffer, 0, buffer.Length));

            Assert.True(thrownExTask.IsCompleted);

            thrownEx = await thrownExTask;
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.INTERNAL_ERROR, CoreStrings.ConnectionAbortedByApplication);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        Assert.NotNull(thrownEx);
        Assert.IsType<ConnectionAbortedException>(thrownEx);
        Assert.Equal(CoreStrings.ConnectionAbortedByApplication, thrownEx.Message);
    }

    [Fact]
    public async Task RequestAbort_ThrowsOperationCanceledExceptionFromOngoingRequestBodyStreamRead()
    {
        OperationCanceledException thrownEx = null;

        await InitializeConnectionAsync(async context =>
        {
            var buffer = new byte[100];
            var thrownExTask = Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Request.Body.ReadAsync(buffer, 0, buffer.Length));

            Assert.False(thrownExTask.IsCompleted);

            context.Abort();

            thrownEx = await thrownExTask.DefaultTimeout();
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.INTERNAL_ERROR, CoreStrings.ConnectionAbortedByApplication);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        Assert.NotNull(thrownEx);
        Assert.IsType<TaskCanceledException>(thrownEx);
        Assert.Equal("The request was aborted", thrownEx.Message);
        Assert.IsType<ConnectionAbortedException>(thrownEx.InnerException);
        Assert.Equal(CoreStrings.ConnectionAbortedByApplication, thrownEx.InnerException.Message);
    }

    // Sync writes after async writes could block the write loop if the callback is not dispatched.
    // https://github.com/aspnet/KestrelHttpServer/issues/2878
    [Fact]
    public async Task Write_DoesNotBlockWriteLoop()
    {
        const int windowSize = (int)Http2PeerSettings.DefaultMaxFrameSize;
        _clientSettings.InitialWindowSize = windowSize;

        await InitializeConnectionAsync(async context =>
        {
            var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
            bodyControlFeature.AllowSynchronousIO = true;
                // Fill the flow control window to create async back pressure.
                await context.Response.Body.WriteAsync(new byte[windowSize + 1], 0, windowSize + 1);
            context.Response.Body.Write(new byte[1], 0, 1);
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: windowSize,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await SendWindowUpdateAsync(1, 2);
        await SendWindowUpdateAsync(0, 2);

        // Remaining 1 byte from the first write and then the second write
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task ResponseWithHeadersTooLarge_AbortsConnection()
    {
        var appFinished = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        await InitializeConnectionAsync(async context =>
        {
            context.Response.Headers["too_long"] = new string('a', (int)Http2PeerSettings.DefaultMaxFrameSize);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.WriteAsync("Hello World")).DefaultTimeout();
            appFinished.TrySetResult(ex.InnerException.Message);
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var message = await appFinished.Task.DefaultTimeout();
        Assert.Equal(SR.net_http_hpack_encode_failure, message);

        // Just the StatusCode gets written before aborting in the continuation frame
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.NONE,
            withStreamId: 1);

        _pair.Application.Output.Complete();

        await WaitForConnectionErrorAsync<HPackEncodingException>(ignoreNonGoAwayFrames: false, expectedLastStreamId: int.MaxValue, Http2ErrorCode.INTERNAL_ERROR,
            SR.net_http_hpack_encode_failure);
    }

    [Fact]
    public async Task WriteAsync_PreCancelledCancellationToken_DoesNotAbort()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
                // The cancellation is checked at the start of WriteAsync and no application state is changed.
                await Assert.ThrowsAsync<OperationCanceledException>(() => context.Response.WriteAsync("hello,", new CancellationToken(true)));
            Assert.False(context.Response.HasStarted);
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task WriteAsync_CancellationTokenTriggeredDueToFlowControl_SendRST()
    {
        var cts = new CancellationTokenSource();
        var writeStarted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            await context.Response.Body.FlushAsync(); // https://github.com/aspnet/KestrelHttpServer/issues/3031
                var writeTask = context.Response.WriteAsync("hello,", cts.Token);
            writeStarted.SetResult(0);
            await Assert.ThrowsAsync<OperationCanceledException>(() => writeTask);
        });

        _clientSettings.InitialWindowSize = 0;
        await SendSettingsAsync();
        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await writeStarted.Task;

        cts.Cancel();

        await WaitForStreamErrorAsync(1, Http2ErrorCode.INTERNAL_ERROR, null);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
    }

    [Fact]
    public async Task WriteAsync_GetMemoryLargeWriteBeforeFirstFlush()
    {
        var headers = new[]
         {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async httpContext =>
        {
            var response = httpContext.Response;
            await response.StartAsync();
            var memory = response.BodyWriter.GetMemory();
            Assert.Equal(4096, memory.Length);
            var fisrtPartOfResponse = Encoding.ASCII.GetBytes(new string('a', memory.Length));
            fisrtPartOfResponse.CopyTo(memory);
            response.BodyWriter.Advance(memory.Length);

            memory = response.BodyWriter.GetMemory();
            var secondPartOfResponse = Encoding.ASCII.GetBytes("aaaaaa");
            secondPartOfResponse.CopyTo(memory);
            response.BodyWriter.Advance(6);

            await response.BodyWriter.FlushAsync();
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 4102,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal(Encoding.ASCII.GetBytes(new string('a', 4102)), dataFrame.PayloadSequence.ToArray());
    }

    [Fact]
    public async Task WriteAsync_WithGetMemoryWithInitialFlushWorks()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async httpContext =>
        {
            var response = httpContext.Response;

            await response.BodyWriter.FlushAsync();

            var memory = response.BodyWriter.GetMemory();
            var fisrtPartOfResponse = Encoding.ASCII.GetBytes(new string('a', memory.Length));
            fisrtPartOfResponse.CopyTo(memory);
            response.BodyWriter.Advance(memory.Length);

            memory = response.BodyWriter.GetMemory();
            var secondPartOfResponse = Encoding.ASCII.GetBytes("aaaaaa");
            secondPartOfResponse.CopyTo(memory);
            response.BodyWriter.Advance(6);

            await response.BodyWriter.FlushAsync();
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 4102,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal(Encoding.ASCII.GetBytes(new string('a', 4102)), dataFrame.PayloadSequence.ToArray());
    }

    [Fact]
    public async Task WriteAsync_GetMemoryMultipleAdvance()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async httpContext =>
        {
            var response = httpContext.Response;

            await response.BodyWriter.FlushAsync();

            var memory = response.BodyWriter.GetMemory(4096);
            var fisrtPartOfResponse = Encoding.ASCII.GetBytes("hello,");
            fisrtPartOfResponse.CopyTo(memory);
            response.BodyWriter.Advance(6);

            var secondPartOfResponse = Encoding.ASCII.GetBytes(" world");
            secondPartOfResponse.CopyTo(memory.Slice(6));
            response.BodyWriter.Advance(6);
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.True(_helloWorldBytes.AsSpan().SequenceEqual(dataFrame.PayloadSequence.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_GetSpanMultipleAdvance()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async httpContext =>
        {
            var response = httpContext.Response;

            await response.BodyWriter.FlushAsync();

            void NonAsyncMethod()
            {
                var span = response.BodyWriter.GetSpan();
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("hello,");
                fisrtPartOfResponse.CopyTo(span);
                response.BodyWriter.Advance(6);

                var secondPartOfResponse = Encoding.ASCII.GetBytes(" world");
                secondPartOfResponse.CopyTo(span.Slice(6));
                response.BodyWriter.Advance(6);
            }
            NonAsyncMethod();
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.True(_helloWorldBytes.AsSpan().SequenceEqual(dataFrame.PayloadSequence.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_GetMemoryAndWrite()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
        };
        await InitializeConnectionAsync(async httpContext =>
        {
            var response = httpContext.Response;

            var memory = response.BodyWriter.GetMemory(4096);
            var fisrtPartOfResponse = Encoding.ASCII.GetBytes("hello,");
            fisrtPartOfResponse.CopyTo(memory);
            response.BodyWriter.Advance(6);

            await response.WriteAsync(" world");
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);
        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.True(_helloWorldBytes.AsSpan().SequenceEqual(dataFrame.PayloadSequence.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_GetMemoryWithSizeHintAlwaysReturnsSameSize()
    {
        var headers = new[]
{
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async httpContext =>
        {
            var response = httpContext.Response;

            await response.StartAsync();

            var memory = response.BodyWriter.GetMemory(0);
            Assert.Equal(4096, memory.Length);

            memory = response.BodyWriter.GetMemory(4096);
            Assert.Equal(4096, memory.Length);
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
    }

    [Fact]
    public async Task WriteAsync_GetMemoryWithSizeHintAlwaysReturnsSameSizeStartAsync()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async httpContext =>
        {
            var response = httpContext.Response;

            var memory = response.BodyWriter.GetMemory(0);
            Assert.Equal(4096, memory.Length);

            memory = response.BodyWriter.GetMemory(4096);
            Assert.Equal(4096, memory.Length);

            await Task.CompletedTask;
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task WriteAsync_BothPipeAndStreamWorks()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async httpContext =>
        {
            var response = httpContext.Response;
            await response.StartAsync();
            var memory = response.BodyWriter.GetMemory(4096);
            var fisrtPartOfResponse = Encoding.ASCII.GetBytes("hello,");
            fisrtPartOfResponse.CopyTo(memory);
            response.BodyWriter.Advance(6);
            var secondPartOfResponse = Encoding.ASCII.GetBytes(" world");
            secondPartOfResponse.CopyTo(memory.Slice(6));
            response.BodyWriter.Advance(6);

            await response.BodyWriter.FlushAsync();

            await response.Body.WriteAsync(Encoding.ASCII.GetBytes("hello, world"));
            await response.BodyWriter.WriteAsync(Encoding.ASCII.GetBytes("hello, world"));
            await response.WriteAsync("hello, world");
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
    }

    [Fact]
    public async Task ContentLengthWithGetSpanWorks()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async httpContext =>
        {
            var response = httpContext.Response;
            response.ContentLength = 12;
            await Task.CompletedTask;

            void NonAsyncMethod()
            {
                var span = response.BodyWriter.GetSpan(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("hello,");
                fisrtPartOfResponse.CopyTo(span);
                response.BodyWriter.Advance(6);

                var secondPartOfResponse = Encoding.ASCII.GetBytes(" world");
                secondPartOfResponse.CopyTo(span.Slice(6));
                response.BodyWriter.Advance(6);
            }

            NonAsyncMethod();
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 37,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("12", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task ContentLengthWithGetMemoryWorks()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(httpContext =>
        {
            var response = httpContext.Response;
            response.ContentLength = 12;

            var memory = response.BodyWriter.GetMemory(4096);
            var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
            fisrtPartOfResponse.CopyTo(memory);
            response.BodyWriter.Advance(6);

            var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
            secondPartOfResponse.CopyTo(memory.Slice(6));
            response.BodyWriter.Advance(6);
            return Task.CompletedTask;
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 37,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("12", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task ResponseBodyCanWrite()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async httpContext =>
        {
            httpContext.Response.ContentLength = 12;
            await httpContext.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("hello, world"));
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 37,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("12", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task ResponseBodyAndResponsePipeWorks()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async httpContext =>
        {
            var response = httpContext.Response;
            response.ContentLength = 54;
            var memory = response.BodyWriter.GetMemory(4096);

            var fisrtPartOfResponse = Encoding.ASCII.GetBytes("hello,");
            fisrtPartOfResponse.CopyTo(memory);
            response.BodyWriter.Advance(6);
            var secondPartOfResponse = Encoding.ASCII.GetBytes(" world\r\n");
            secondPartOfResponse.CopyTo(memory.Slice(6));
            response.BodyWriter.Advance(8);
            await response.BodyWriter.FlushAsync();
            await response.Body.WriteAsync(Encoding.ASCII.GetBytes("hello, world\r\n"));
            await response.BodyWriter.WriteAsync(Encoding.ASCII.GetBytes("hello, world\r\n"));
            await response.WriteAsync("hello, world");
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 37,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 14,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 14,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 14,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("54", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task ResponseBodyPipeCompleteWithoutExceptionDoesNotThrow()
    {
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            context.Response.BodyWriter.Complete();
            await Task.CompletedTask;
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
    }

    [Fact]
    public async Task ResponseBodyPipeCompleteWithoutExceptionWritesDoesThrow()
    {
        InvalidOperationException writeEx = null;
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            context.Response.BodyWriter.Complete();
            writeEx = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.WriteAsync(""));
        });

        await StartStreamAsync(1, headers, endStream: true);

        // Don't receive content length because we called WriteAsync which caused an invalid response
        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS | (byte)Http2HeadersFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.NotNull(writeEx);
    }

    [Fact]
    public async Task ResponseBodyPipeCompleteWithExceptionThrows()
    {
        var expectedException = new Exception();
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            context.Response.BodyWriter.Complete(expectedException);
            await Task.CompletedTask;
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("500", _decodedHeaders[HeaderNames.Status]);

        Assert.Contains(TestSink.Writes, w => w.EventId.Id == 13 && w.LogLevel == LogLevel.Error
                   && w.Exception is ConnectionAbortedException && w.Exception.InnerException == expectedException);
    }

    [Fact]
    public async Task CompleteAsync_BeforeBodyStarted_SendsHeadersWithEndStream()
    {
        var startingTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(0); return Task.CompletedTask; });
                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                    Assert.True(context.Response.Headers.IsReadOnly);
                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                    // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                    await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult(0);
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        clientTcs.SetResult(0);
        await appTcs.Task;

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders["content-length"]);
    }

    [Fact]
    public async Task CompleteAsync_BeforeBodyStarted_WithTrailers_SendsHeadersAndTrailersWithEndStream()
    {
        var startingTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(0); return Task.CompletedTask; });
                context.Response.AppendTrailer("CustomName", "Custom Value");

                await context.Response.CompleteAsync().DefaultTimeout();
                await context.Response.CompleteAsync().DefaultTimeout(); // Can be called twice, no-ops

                    Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                    Assert.True(context.Response.Headers.IsReadOnly);
                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                    // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                    await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult(0);
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1);
        var trailersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 25,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        clientTcs.SetResult(0);
        await appTcs.Task;

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders["content-length"]);

        _decodedHeaders.Clear();

        _hpackDecoder.Decode(trailersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Single(_decodedHeaders);
        Assert.Equal("Custom Value", _decodedHeaders["CustomName"]);
    }

    [Fact]
    public async Task CompleteAsync_BeforeBodyStarted_WithTrailers_TruncatedContentLength_ThrowsAnd500()
    {
        var startingTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(0); return Task.CompletedTask; });

                context.Response.ContentLength = 25;
                context.Response.AppendTrailer("CustomName", "Custom Value");

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.CompleteAsync().DefaultTimeout());
                Assert.Equal(CoreStrings.FormatTooFewBytesWritten(0, 25), ex.Message);

                Assert.True(startingTcs.Task.IsCompletedSuccessfully);
                Assert.False(context.Response.Headers.IsReadOnly);
                Assert.False(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                appTcs.SetResult(0);
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await appTcs.Task;

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("500", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task CompleteAsync_AfterBodyStarted_SendsBodyWithEndStream()
    {
        var startingTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(0); return Task.CompletedTask; });

                await context.Response.WriteAsync("Hello World");
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                    Assert.True(context.Response.Headers.IsReadOnly);

                await context.Response.CompleteAsync().DefaultTimeout();
                await context.Response.CompleteAsync().DefaultTimeout(); // Can be called twice, no-ops

                    Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                    // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                    await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult(0);
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1);
        var bodyFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)(Http2HeadersFrameFlags.NONE),
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)(Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        clientTcs.SetResult(0);
        await appTcs.Task;

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        Assert.Equal("Hello World", Encoding.UTF8.GetString(bodyFrame.Payload.Span));
    }

    [Fact]
    public async Task CompleteAsync_WriteAfterComplete_Throws()
    {
        var startingTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(0); return Task.CompletedTask; });
                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                    Assert.True(context.Response.Headers.IsReadOnly);
                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.WriteAsync("2 Hello World").DefaultTimeout());
                Assert.Equal("Writing is not allowed after writer was completed.", ex.Message);

                    // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                    await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult(0);
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        clientTcs.SetResult(0);
        await appTcs.Task;

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task CompleteAsync_WriteAgainAfterComplete_Throws()
    {
        var startingTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(0); return Task.CompletedTask; });

                await context.Response.WriteAsync("Hello World").DefaultTimeout();
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                    Assert.True(context.Response.Headers.IsReadOnly);

                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.WriteAsync("2 Hello World").DefaultTimeout());
                Assert.Equal("Writing is not allowed after writer was completed.", ex.Message);

                    // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                    await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult(0);
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1);
        var bodyFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)(Http2HeadersFrameFlags.NONE),
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)(Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        clientTcs.SetResult(0);
        await appTcs.Task;

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        Assert.Equal("Hello World", Encoding.UTF8.GetString(bodyFrame.Payload.Span));
    }

    [Fact]
    public async Task CompleteAsync_AfterBodyStarted_WithTrailers_SendsBodyAndTrailersWithEndStream()
    {
        var startingTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(0); return Task.CompletedTask; });

                await context.Response.WriteAsync("Hello World");
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                    Assert.True(context.Response.Headers.IsReadOnly);

                context.Response.AppendTrailer("CustomName", "Custom Value");

                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                    // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                    await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult(0);
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        });

        await StartStreamAsync(1, headers, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1);
        var bodyFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 11,
            withFlags: (byte)(Http2HeadersFrameFlags.NONE),
            withStreamId: 1);
        var trailersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 25,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        clientTcs.SetResult(0);
        await appTcs.Task;

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        Assert.Equal("Hello World", Encoding.UTF8.GetString(bodyFrame.Payload.Span));

        _decodedHeaders.Clear();

        _hpackDecoder.Decode(trailersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Single(_decodedHeaders);
        Assert.Equal("Custom Value", _decodedHeaders["CustomName"]);
    }
}
