// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http2StreamTests : IDisposable, IHttpHeadersHandler
    {
        private static readonly string _largeHeaderValue = new string('a', HPackDecoder.MaxStringOctets);

        private static readonly IEnumerable<KeyValuePair<string, string>> _browserRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>("user-agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:54.0) Gecko/20100101 Firefox/54.0"),
            new KeyValuePair<string, string>("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"),
            new KeyValuePair<string, string>("accept-language", "en-US,en;q=0.5"),
            new KeyValuePair<string, string>("accept-encoding", "gzip, deflate, br"),
            new KeyValuePair<string, string>("upgrade-insecure-requests", "1"),
        };

        private MemoryPool<byte> _memoryPool = KestrelMemoryPool.Create();
        private DuplexPipe.DuplexPipePair _pair;
        private readonly TestApplicationErrorLogger _logger;
        private Http2ConnectionContext _connectionContext;
        private Http2Connection _connection;
        private readonly Http2PeerSettings _clientSettings = new Http2PeerSettings();
        private readonly HPackEncoder _hpackEncoder = new HPackEncoder();
        private readonly HPackDecoder _hpackDecoder;

        private readonly ConcurrentDictionary<int, TaskCompletionSource<object>> _runningStreams = new ConcurrentDictionary<int, TaskCompletionSource<object>>();
        private readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<int> _abortedStreamIds = new HashSet<int>();
        private readonly object _abortedStreamIdsLock = new object();

        private readonly RequestDelegate _noopApplication;
        private readonly RequestDelegate _echoMethod;
        private readonly RequestDelegate _echoHost;
        private readonly RequestDelegate _echoPath;
        private readonly RequestDelegate _waitForAbortApplication;
        private readonly RequestDelegate _waitForAbortFlushingApplication;
        private readonly RequestDelegate _waitForAbortWithDataApplication;

        private Task _connectionTask;

        public Http2StreamTests()
        {
            _noopApplication = context => Task.CompletedTask;

            _echoMethod = context =>
            {
                context.Response.Headers["Method"] = context.Request.Method;

                return Task.CompletedTask;
            };

            _echoHost = context =>
            {
                context.Response.Headers[HeaderNames.Host] = context.Request.Headers[HeaderNames.Host];

                return Task.CompletedTask;
            };

            _echoPath = context =>
            {
                context.Response.Headers["path"] = context.Request.Path.ToString();
                context.Response.Headers["rawtarget"] = context.Features.Get<IHttpRequestFeature>().RawTarget;

                return Task.CompletedTask;
            };

            _waitForAbortApplication = async context =>
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

                _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
            };

            _waitForAbortFlushingApplication = async context =>
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

                await context.Response.Body.FlushAsync();

                _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
            };

            _waitForAbortWithDataApplication = async context =>
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

                _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
            };

            _hpackDecoder = new HPackDecoder((int)_clientSettings.HeaderTableSize);

            _logger = new TestApplicationErrorLogger();

            InitializeConnectionFields(KestrelMemoryPool.Create());
        }

        private void InitializeConnectionFields(MemoryPool<byte> memoryPool)
        {
            _memoryPool = memoryPool;

            // Always dispatch test code back to the ThreadPool. This prevents deadlocks caused by continuing
            // Http2Connection.ProcessRequestsAsync() loop with writer locks acquired. Run product code inline to make
            // it easier to verify request frames are processed correctly immediately after sending the them.
            var inputPipeOptions = new PipeOptions(
                pool: _memoryPool,
                readerScheduler: PipeScheduler.Inline,
                writerScheduler: PipeScheduler.ThreadPool,
                useSynchronizationContext: false
            );
            var outputPipeOptions = new PipeOptions(
                pool: _memoryPool,
                readerScheduler: PipeScheduler.ThreadPool,
                writerScheduler: PipeScheduler.Inline,
                useSynchronizationContext: false
            );

            _pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);

            _connectionContext = new Http2ConnectionContext
            {
                ConnectionFeatures = new FeatureCollection(),
                ServiceContext = new TestServiceContext()
                {
                    Log = new TestKestrelTrace(_logger)
                },
                MemoryPool = _memoryPool,
                Application = _pair.Application,
                Transport = _pair.Transport
            };

            _connection = new Http2Connection(_connectionContext);
        }

        public void Dispose()
        {
            _pair.Application.Input.Complete();
            _pair.Application.Output.Complete();
            _pair.Transport.Input.Complete();
            _pair.Transport.Output.Complete();
            _memoryPool.Dispose();
        }

        void IHttpHeadersHandler.OnHeader(Span<byte> name, Span<byte> value)
        {
            _decodedHeaders[name.GetAsciiStringNonNullCharacters()] = value.GetAsciiStringNonNullCharacters();
        }

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

        [Fact]
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

            Assert.Contains(_logger.Messages, m => m.Exception?.Message.Contains("Response Content-Length mismatch: too many bytes written (12 of 11).") ?? false);

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

            Assert.Contains(_logger.Messages, m => m.Exception?.Message.Contains("Response Content-Length mismatch: too few bytes written (0 of 11).") ?? false);

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

            Assert.Contains(_logger.Messages, m => (m.Exception?.Message.Contains("App Faulted") ?? false) && m.LogLevel == LogLevel.Error);

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

        private async Task InitializeConnectionAsync(RequestDelegate application)
        {
            _connectionTask = _connection.ProcessRequestsAsync(new DummyApplication(application));

            await SendPreambleAsync().ConfigureAwait(false);
            await SendSettingsAsync();

            await ExpectAsync(Http2FrameType.SETTINGS,
                withLength: 0,
                withFlags: 0,
                withStreamId: 0);

            await ExpectAsync(Http2FrameType.SETTINGS,
                withLength: 0,
                withFlags: (byte)Http2SettingsFrameFlags.ACK,
                withStreamId: 0);
        }

        private async Task StartStreamAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, bool endStream)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _runningStreams[streamId] = tcs;

            var frame = new Http2Frame();
            frame.PrepareHeaders(Http2HeadersFrameFlags.NONE, streamId);
            var done = _hpackEncoder.BeginEncode(headers, frame.HeadersPayload, out var length);
            frame.Length = length;

            if (done)
            {
                frame.HeadersFlags = Http2HeadersFrameFlags.END_HEADERS;
            }

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            await SendAsync(frame.Raw);

            while (!done)
            {
                frame.PrepareContinuation(Http2ContinuationFrameFlags.NONE, streamId);
                done = _hpackEncoder.Encode(frame.HeadersPayload, out length);
                frame.Length = length;

                if (done)
                {
                    frame.ContinuationFlags = Http2ContinuationFrameFlags.END_HEADERS;
                }

                await SendAsync(frame.Raw);
            }
        }

        private Task WaitForAllStreamsAsync()
        {
            return Task.WhenAll(_runningStreams.Values.Select(tcs => tcs.Task)).DefaultTimeout();
        }

        private Task SendAsync(ReadOnlySpan<byte> span)
        {
            var writableBuffer = _pair.Application.Output;
            writableBuffer.Write(span);
            return FlushAsync(writableBuffer);
        }

        private static async Task FlushAsync(PipeWriter writableBuffer)
        {
            await writableBuffer.FlushAsync();
        }

        private Task SendPreambleAsync() => SendAsync(new ArraySegment<byte>(Http2Connection.ClientPreface));

        private Task SendSettingsAsync()
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE, _clientSettings);
            return SendAsync(frame.Raw);
        }

        private async Task<bool> SendHeadersAsync(int streamId, Http2HeadersFrameFlags flags, IEnumerable<KeyValuePair<string, string>> headers)
        {
            var frame = new Http2Frame();

            frame.PrepareHeaders(flags, streamId);
            var done = _hpackEncoder.BeginEncode(headers, frame.Payload, out var length);
            frame.Length = length;

            await SendAsync(frame.Raw);

            return done;
        }

        private Task SendDataAsync(int streamId, Span<byte> data, bool endStream)
        {
            var frame = new Http2Frame();

            frame.PrepareData(streamId);
            frame.Length = data.Length;
            frame.DataFlags = endStream ? Http2DataFrameFlags.END_STREAM : Http2DataFrameFlags.NONE;
            data.CopyTo(frame.DataPayload);

            return SendAsync(frame.Raw);
        }

        private Task SendRstStreamAsync(int streamId)
        {
            var rstStreamFrame = new Http2Frame();
            rstStreamFrame.PrepareRstStream(streamId, Http2ErrorCode.CANCEL);
            return SendAsync(rstStreamFrame.Raw);
        }

        private async Task<Http2Frame> ReceiveFrameAsync()
        {
            var frame = new Http2Frame();

            while (true)
            {
                var result = await _pair.Application.Input.ReadAsync();
                var buffer = result.Buffer;
                var consumed = buffer.Start;
                var examined = buffer.End;

                try
                {
                    Assert.True(buffer.Length > 0);

                    if (Http2FrameReader.ReadFrame(buffer, frame, 16_384, out consumed, out examined))
                    {
                        return frame;
                    }

                    if (result.IsCompleted)
                    {
                        throw new IOException("The reader completed without returning a frame.");
                    }
                }
                finally
                {
                    _pair.Application.Input.AdvanceTo(consumed, examined);
                }
            }
        }

        private async Task<Http2Frame> ExpectAsync(Http2FrameType type, int withLength, byte withFlags, int withStreamId)
        {
            var frame = await ReceiveFrameAsync();

            Assert.Equal(type, frame.Type);
            Assert.Equal(withLength, frame.Length);
            Assert.Equal(withFlags, frame.Flags);
            Assert.Equal(withStreamId, frame.StreamId);

            return frame;
        }

        private Task StopConnectionAsync(int expectedLastStreamId, bool ignoreNonGoAwayFrames)
        {
            _pair.Application.Output.Complete();

            return WaitForConnectionStopAsync(expectedLastStreamId, ignoreNonGoAwayFrames);
        }

        private Task WaitForConnectionStopAsync(int expectedLastStreamId, bool ignoreNonGoAwayFrames)
        {
            return WaitForConnectionErrorAsync<Exception>(ignoreNonGoAwayFrames, expectedLastStreamId, Http2ErrorCode.NO_ERROR, expectedErrorMessage: null);
        }

        private async Task WaitForConnectionErrorAsync<TException>(bool ignoreNonGoAwayFrames, int expectedLastStreamId, Http2ErrorCode expectedErrorCode, string expectedErrorMessage)
            where TException : Exception
        {
            var frame = await ReceiveFrameAsync();

            if (ignoreNonGoAwayFrames)
            {
                while (frame.Type != Http2FrameType.GOAWAY)
                {
                    frame = await ReceiveFrameAsync();
                }
            }

            Assert.Equal(Http2FrameType.GOAWAY, frame.Type);
            Assert.Equal(8, frame.Length);
            Assert.Equal(0, frame.Flags);
            Assert.Equal(0, frame.StreamId);
            Assert.Equal(expectedLastStreamId, frame.GoAwayLastStreamId);
            Assert.Equal(expectedErrorCode, frame.GoAwayErrorCode);

            if (expectedErrorMessage != null)
            {
                var message = Assert.Single(_logger.Messages, m => m.Exception is TException);
                Assert.Contains(expectedErrorMessage, message.Exception.Message);
            }

            await _connectionTask;
            _pair.Application.Output.Complete();
        }

        private async Task WaitForStreamErrorAsync(int expectedStreamId, Http2ErrorCode expectedErrorCode, string expectedErrorMessage)
        {
            var frame = await ReceiveFrameAsync();

            Assert.Equal(Http2FrameType.RST_STREAM, frame.Type);
            Assert.Equal(4, frame.Length);
            Assert.Equal(0, frame.Flags);
            Assert.Equal(expectedStreamId, frame.StreamId);
            Assert.Equal(expectedErrorCode, frame.RstStreamErrorCode);

            if (expectedErrorMessage != null)
            {
                Assert.Contains(_logger.Messages, m => m.Exception?.Message.Contains(expectedErrorMessage) ?? false);
            }
        }
    }
}