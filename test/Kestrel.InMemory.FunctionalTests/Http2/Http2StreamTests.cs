// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http2StreamTests : Http2TestBase
    {
        [Fact]
        public async Task HEADERS_Received_EmptyMethod_Reset()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, ""),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            };
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(1, headers, endStream: true);

            await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.FormatHttp2ErrorMethodInvalid(""));

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task HEADERS_Received_InvlaidCustomMethod_Reset()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "Hello,World"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            };
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(1, headers, endStream: true);

            await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.FormatHttp2ErrorMethodInvalid("Hello,World"));

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task HEADERS_Received_CustomMethod_Accepted()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "Custom"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            };
            await InitializeConnectionAsync(_echoMethod);

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 70,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(4, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("Custom", _decodedHeaders["Method"]);
            Assert.Equal("0", _decodedHeaders["content-length"]);
        }

        [Fact]
        public async Task HEADERS_Received_CONNECTMethod_Accepted()
        {
            await InitializeConnectionAsync(_echoMethod);

            // :path and :scheme are not allowed, :authority is optional
            var headers = new[] { new KeyValuePair<string, string>(HeaderNames.Method, "CONNECT") };
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 71,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(4, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("CONNECT", _decodedHeaders["Method"]);
            Assert.Equal("0", _decodedHeaders["content-length"]);
        }

        [Fact]
        public async Task HEADERS_Received_OPTIONSStar_LeftOutOfPath()
        {
            await InitializeConnectionAsync(_echoPath);

            // :path and :scheme are not allowed, :authority is optional
            var headers = new[] { new KeyValuePair<string, string>(HeaderNames.Method, "OPTIONS"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Path, "*")};
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 75,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(5, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("", _decodedHeaders["path"]);
            Assert.Equal("*", _decodedHeaders["rawtarget"]);
            Assert.Equal("0", _decodedHeaders["content-length"]);
        }

        [Fact]
        public async Task HEADERS_Received_OPTIONSSlash_Accepted()
        {
            await InitializeConnectionAsync(_echoPath);

            // :path and :scheme are not allowed, :authority is optional
            var headers = new[] { new KeyValuePair<string, string>(HeaderNames.Method, "OPTIONS"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/")};
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 76,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(5, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("/", _decodedHeaders["path"]);
            Assert.Equal("/", _decodedHeaders["rawtarget"]);
            Assert.Equal("0", _decodedHeaders["content-length"]);
        }

        [Fact]
        public async Task HEADERS_Received_PathAndQuery_Seperated()
        {
            await InitializeConnectionAsync(context =>
            {
                context.Response.Headers["path"] = context.Request.Path.Value;
                context.Response.Headers["query"] = context.Request.QueryString.Value;
                context.Response.Headers["rawtarget"] = context.Features.Get<IHttpRequestFeature>().RawTarget;
                return Task.CompletedTask;
            });

            // :path and :scheme are not allowed, :authority is optional
            var headers = new[] { new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/a/path?a&que%35ry")};
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 118,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(6, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("/a/path", _decodedHeaders["path"]);
            Assert.Equal("?a&que%35ry", _decodedHeaders["query"]);
            Assert.Equal("/a/path?a&que%35ry", _decodedHeaders["rawtarget"]);
            Assert.Equal("0", _decodedHeaders["content-length"]);
        }

        [Theory]
        [InlineData("/","/")]
        [InlineData("/a%5E", "/a^")]
        [InlineData("/a%E2%82%AC", "/a€")]
        [InlineData("/a%2Fb", "/a%2Fb")] // Forward slash, not decoded
        [InlineData("/a%b", "/a%b")] // Incomplete encoding, not decoded
        [InlineData("/a/b/c/../d", "/a/b/d")] // Navigation processed
        [InlineData("/a/b/c/../../../../d", "/d")] // Navigation escape prevented
        [InlineData("/a/b/c/.%2E/d", "/a/b/d")] // Decode before navigation processing
        public async Task HEADERS_Received_Path_DecodedAndNormalized(string input, string expected)
        {
            await InitializeConnectionAsync(context =>
            {
                Assert.Equal(expected, context.Request.Path.Value);
                Assert.Equal(input, context.Features.Get<IHttpRequestFeature>().RawTarget);
                return Task.CompletedTask;
            });

            // :path and :scheme are not allowed, :authority is optional
            var headers = new[] { new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Path, input)};
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders["content-length"]);
        }

        [Theory]
        [InlineData(HeaderNames.Path, "/")]
        [InlineData(HeaderNames.Scheme, "http")]
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

        [Fact]
        public async Task HEADERS_Received_SchemeMismatch_Reset()
        {
            await InitializeConnectionAsync(_noopApplication);

            // :path and :scheme are not allowed, :authority is optional
            var headers = new[] { new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "https") }; // Not the expected "http"
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

            await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR,
                CoreStrings.FormatHttp2StreamErrorSchemeMismatch("https", "http"));

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task HEADERS_Received_MissingAuthority_200Status()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders["content-length"]);
        }

        [Fact]
        public async Task HEADERS_Received_EmptyAuthority_200Status()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, ""),
            };
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders["content-length"]);
        }

        [Fact]
        public async Task HEADERS_Received_MissingAuthorityFallsBackToHost_200Status()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>("Host", "abc"),
            };
            await InitializeConnectionAsync(_echoHost);

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 65,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(4, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
            Assert.Equal("abc", _decodedHeaders[HeaderNames.Host]);
        }

        [Fact]
        public async Task HEADERS_Received_EmptyAuthorityIgnoredOverHost_200Status()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, ""),
                new KeyValuePair<string, string>("Host", "abc"),
            };
            await InitializeConnectionAsync(_echoHost);

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 65,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(4, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
            Assert.Equal("abc", _decodedHeaders[HeaderNames.Host]);
        }

        [Fact]
        public async Task HEADERS_Received_AuthorityOverridesHost_200Status()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "def"),
                new KeyValuePair<string, string>("Host", "abc"),
            };
            await InitializeConnectionAsync(_echoHost);

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 65,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(4, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
            Assert.Equal("def", _decodedHeaders[HeaderNames.Host]);
        }

        [Fact]
        public async Task HEADERS_Received_AuthorityOverridesInvalidHost_200Status()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "def"),
                new KeyValuePair<string, string>("Host", "a=bc"),
            };
            await InitializeConnectionAsync(_echoHost);

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 65,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(4, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
            Assert.Equal("def", _decodedHeaders[HeaderNames.Host]);
        }

        [Fact]
        public async Task HEADERS_Received_InvalidAuthority_Reset()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "local=host:80"),
            };
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(1, headers, endStream: true);

            await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR,
                CoreStrings.FormatBadRequest_InvalidHostHeader_Detail("local=host:80"));

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task HEADERS_Received_InvalidAuthorityWithValidHost_Reset()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "d=ef"),
                new KeyValuePair<string, string>("Host", "abc"),
            };
            await InitializeConnectionAsync(_echoHost);

            await StartStreamAsync(1, headers, endStream: true);

            await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR,
                CoreStrings.FormatBadRequest_InvalidHostHeader_Detail("d=ef"));

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task HEADERS_Received_TwoHosts_StreamReset()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>("Host", "host1"),
                new KeyValuePair<string, string>("Host", "host2"),
            };
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(1, headers, endStream: true);

            await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR,
                CoreStrings.FormatBadRequest_InvalidHostHeader_Detail("host1,host2"));

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task ContentLength_Received_SingleDataFrame_Verified()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await InitializeConnectionAsync(async context =>
            {
                var buffer = new byte[100];
                var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(12, read);
            });

            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[12].AsSpan(), endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task ContentLength_ReceivedInContinuation_SingleDataFrame_Verified()
        {
            await InitializeConnectionAsync(async context =>
            {
                var buffer = new byte[100];
                var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(12, read);
            });

            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>("a", _largeHeaderValue),
                new KeyValuePair<string, string>("b", _largeHeaderValue),
                new KeyValuePair<string, string>("c", _largeHeaderValue),
                new KeyValuePair<string, string>("d", _largeHeaderValue),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[12].AsSpan(), endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task ContentLength_Received_MultipleDataFrame_Verified()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await InitializeConnectionAsync(async context =>
            {
                var buffer = new byte[100];
                var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
                var total = read;
                while (read > 0)
                {
                    read = await context.Request.Body.ReadAsync(buffer, total, buffer.Length - total);
                    total += read;
                }
                Assert.Equal(12, total);
            });


            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[1].AsSpan(), endStream: false);
            await SendDataAsync(1, new byte[3].AsSpan(), endStream: false);
            await SendDataAsync(1, new byte[8].AsSpan(), endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task ContentLength_Received_NoDataFrames_Reset()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(1, headers, endStream: true);

            await WaitForStreamErrorAsync(1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.Http2StreamErrorLessDataThanLength);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task ContentLength_ReceivedInContinuation_NoDataFrames_Reset()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>("a", _largeHeaderValue),
                new KeyValuePair<string, string>("b", _largeHeaderValue),
                new KeyValuePair<string, string>("c", _largeHeaderValue),
                new KeyValuePair<string, string>("d", _largeHeaderValue),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(1, headers, endStream: true);

            await WaitForStreamErrorAsync(1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.Http2StreamErrorLessDataThanLength);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task ContentLength_Received_SingleDataFrameOverSize_Reset()
        {
            IOException thrownEx = null;

            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await InitializeConnectionAsync(async context =>
            {
                thrownEx = await Assert.ThrowsAsync<IOException>(async () =>
                {
                    var buffer = new byte[100];
                    while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) { }
                });
            });

            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[13].AsSpan(), endStream: true);

            await WaitForStreamErrorAsync(1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.Http2StreamErrorMoreDataThanLength);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            var expectedError = new Http2StreamErrorException(1, CoreStrings.Http2StreamErrorMoreDataThanLength, Http2ErrorCode.PROTOCOL_ERROR);

            Assert.NotNull(thrownEx);
            Assert.Equal(expectedError.Message, thrownEx.Message);
            Assert.IsType<Http2StreamErrorException>(thrownEx.InnerException);
        }

        [Fact]
        public async Task ContentLength_Received_SingleDataFrameUnderSize_Reset()
        {
            IOException thrownEx = null;

            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await InitializeConnectionAsync(async context =>
            {
                thrownEx = await Assert.ThrowsAsync<IOException>(async () =>
                {
                    var buffer = new byte[100];
                    while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) { }
                });
            });

            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[11].AsSpan(), endStream: true);

            await WaitForStreamErrorAsync(1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.Http2StreamErrorLessDataThanLength);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            var expectedError = new Http2StreamErrorException(1, CoreStrings.Http2StreamErrorLessDataThanLength, Http2ErrorCode.PROTOCOL_ERROR);

            Assert.NotNull(thrownEx);
            Assert.Equal(expectedError.Message, thrownEx.Message);
            Assert.IsType<Http2StreamErrorException>(thrownEx.InnerException);
        }

        [Fact(Skip = "Flaky test #2799, #2832")]
        public async Task ContentLength_Received_MultipleDataFramesOverSize_Reset()
        {
            IOException thrownEx = null;

            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await InitializeConnectionAsync(async context =>
            {
                thrownEx = await Assert.ThrowsAsync<IOException>(async () =>
                {
                    var buffer = new byte[100];
                    while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) { }
                });
            });

            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[1].AsSpan(), endStream: false);
            await SendDataAsync(1, new byte[2].AsSpan(), endStream: false);
            await SendDataAsync(1, new byte[10].AsSpan(), endStream: false);
            await SendDataAsync(1, new byte[2].AsSpan(), endStream: true);

            await WaitForStreamErrorAsync(1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.Http2StreamErrorMoreDataThanLength);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            var expectedError = new Http2StreamErrorException(1, CoreStrings.Http2StreamErrorMoreDataThanLength, Http2ErrorCode.PROTOCOL_ERROR);

            Assert.NotNull(thrownEx);
            Assert.Equal(expectedError.Message, thrownEx.Message);
            Assert.IsType<Http2StreamErrorException>(thrownEx.InnerException);
        }

        [Fact]
        public async Task ContentLength_Received_MultipleDataFramesUnderSize_Reset()
        {
            IOException thrownEx = null;

            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await InitializeConnectionAsync(async context =>
            {
                thrownEx = await Assert.ThrowsAsync<IOException>(async () =>
                {
                    var buffer = new byte[100];
                    while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) { }
                });
            });

            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[1].AsSpan(), endStream: false);
            await SendDataAsync(1, new byte[2].AsSpan(), endStream: true);

            await WaitForStreamErrorAsync(1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.Http2StreamErrorLessDataThanLength);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            var expectedError = new Http2StreamErrorException(1, CoreStrings.Http2StreamErrorLessDataThanLength, Http2ErrorCode.PROTOCOL_ERROR);

            Assert.NotNull(thrownEx);
            Assert.Equal(expectedError.Message, thrownEx.Message);
            Assert.IsType<Http2StreamErrorException>(thrownEx.InnerException);
        }

        [Fact]
        public async Task ContentLength_Response_FirstWriteMoreBytesWritten_Throws_Sends500()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
            await InitializeConnectionAsync(async context =>
            {
                context.Response.ContentLength = 11;
                await context.Response.WriteAsync("hello, world"); // 12
            });

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Exception?.Message.Contains("Response Content-Length mismatch: too many bytes written (12 of 11).") ?? false);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("500", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task ContentLength_Response_MoreBytesWritten_ThrowsAndResetsStream()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
            await InitializeConnectionAsync(async context =>
            {
                context.Response.ContentLength = 11;
                await context.Response.WriteAsync("hello,");
                await context.Response.WriteAsync(" world");
            });

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 56,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 6,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            await WaitForStreamErrorAsync(1, Http2ErrorCode.INTERNAL_ERROR, "Response Content-Length mismatch: too many bytes written (12 of 11).");

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("11", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task ContentLength_Response_NoBytesWritten_Sends500()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
            await InitializeConnectionAsync(context =>
            {
                context.Response.ContentLength = 11;
                return Task.CompletedTask;
            });

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Exception?.Message.Contains("Response Content-Length mismatch: too few bytes written (0 of 11).") ?? false);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("500", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task ContentLength_Response_TooFewBytesWritten_Resets()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
            await InitializeConnectionAsync(context =>
            {
                context.Response.ContentLength = 11;
                return context.Response.WriteAsync("hello,");
            });

            await StartStreamAsync(1, headers, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 56,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 6,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            await WaitForStreamErrorAsync(1, Http2ErrorCode.INTERNAL_ERROR, "Response Content-Length mismatch: too few bytes written (6 of 11).");

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("11", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task MaxRequestBodySize_ContentLengthUnder_200()
        {
            _connectionContext.ServiceContext.ServerOptions.Limits.MaxRequestBodySize = 15;
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await InitializeConnectionAsync(async context =>
            {
                var buffer = new byte[100];
                var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(12, read);
            });

            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[12].AsSpan(), endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task MaxRequestBodySize_ContentLengthOver_413()
        {
            BadHttpRequestException exception = null;
            _connectionContext.ServiceContext.ServerOptions.Limits.MaxRequestBodySize = 10;
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
            };
            await InitializeConnectionAsync(async context =>
            {
                exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
                {
                    var buffer = new byte[100];
                    while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) { }
                });
                ExceptionDispatchInfo.Capture(exception).Throw();
            });

            await StartStreamAsync(1, headers, endStream: false);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 59,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("413", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);

            Assert.NotNull(exception);
        }

        [Fact]
        public async Task MaxRequestBodySize_NoContentLength_Under_200()
        {
            _connectionContext.ServiceContext.ServerOptions.Limits.MaxRequestBodySize = 15;
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
            await InitializeConnectionAsync(async context =>
            {
                var buffer = new byte[100];
                var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(12, read);
            });

            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[12].AsSpan(), endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task MaxRequestBodySize_NoContentLength_Over_413()
        {
            BadHttpRequestException exception = null;
            _connectionContext.ServiceContext.ServerOptions.Limits.MaxRequestBodySize = 10;
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            };
            await InitializeConnectionAsync(async context =>
            {
                exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
                {
                    var buffer = new byte[100];
                    while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) { }
                });
                ExceptionDispatchInfo.Capture(exception).Throw();
            });

            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[6].AsSpan(), endStream: false);
            await SendDataAsync(1, new byte[6].AsSpan(), endStream: false);
            await SendDataAsync(1, new byte[6].AsSpan(), endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 59,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("413", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);

            Assert.NotNull(exception);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MaxRequestBodySize_AppCanLowerLimit(bool includeContentLength)
        {
            BadHttpRequestException exception = null;
            _connectionContext.ServiceContext.ServerOptions.Limits.MaxRequestBodySize = 20;
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
                        new KeyValuePair<string, string>(HeaderNames.ContentLength, "18"),
                    });
            }
            await InitializeConnectionAsync(async context =>
            {
                Assert.False(context.Features.Get<IHttpMaxRequestBodySizeFeature>().IsReadOnly);
                context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 17;
                exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
                {
                    var buffer = new byte[100];
                    while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) { }
                });
                Assert.True(context.Features.Get<IHttpMaxRequestBodySizeFeature>().IsReadOnly);
                ExceptionDispatchInfo.Capture(exception).Throw();
            });

            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[6].AsSpan(), endStream: false);
            await SendDataAsync(1, new byte[6].AsSpan(), endStream: false);
            await SendDataAsync(1, new byte[6].AsSpan(), endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 59,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("413", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);

            Assert.NotNull(exception);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MaxRequestBodySize_AppCanRaiseLimit(bool includeContentLength)
        {
            _connectionContext.ServiceContext.ServerOptions.Limits.MaxRequestBodySize = 10;
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
            });

            await StartStreamAsync(1, headers, endStream: false);
            await SendDataAsync(1, new byte[12].AsSpan(), endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task ApplicationExeption_BeforeFirstWrite_Sends500()
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
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            Assert.Contains(TestApplicationErrorLogger.Messages, m => (m.Exception?.Message.Contains("App Faulted") ?? false) && m.LogLevel == LogLevel.Error);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

            Assert.Equal(3, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("500", _decodedHeaders[HeaderNames.Status]);
            Assert.Equal("0", _decodedHeaders[HeaderNames.ContentLength]);
        }

        [Fact]
        public async Task ApplicationExeption_AfterFirstWrite_Resets()
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
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 6,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            await WaitForStreamErrorAsync(1, Http2ErrorCode.INTERNAL_ERROR, "App Faulted");

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);

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
        public async Task RST_STREAM_Received_AbortsStream_FlushedHeadersNotSent()
        {
            await InitializeConnectionAsync(_waitForAbortFlushingApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
            await SendRstStreamAsync(1);
            await WaitForAllStreamsAsync();
            Assert.Contains(1, _abortedStreamIds);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task RST_STREAM_Received_AbortsStream_FlushedDataNotSent()
        {
            await InitializeConnectionAsync(_waitForAbortWithDataApplication);

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

                    _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
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

                    _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
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

                        _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
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

                        _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
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
                withLength: 37,
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
    }
}