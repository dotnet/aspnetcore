// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http2ConnectionTests : IDisposable, IHttpHeadersHandler
    {
        private static readonly string _largeHeaderValue = new string('a', HPackDecoder.MaxStringOctets);

        private static readonly IEnumerable<KeyValuePair<string, string>> _postRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(":method", "POST"),
            new KeyValuePair<string, string>(":path", "/"),
            new KeyValuePair<string, string>(":scheme", "http"),
        };

        private static readonly IEnumerable<KeyValuePair<string, string>> _expectContinueRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(":method", "POST"),
            new KeyValuePair<string, string>(":path", "/"),
            new KeyValuePair<string, string>(":authority", "127.0.0.1"),
            new KeyValuePair<string, string>(":scheme", "https"),
            new KeyValuePair<string, string>("expect", "100-continue"),
        };

        private static readonly IEnumerable<KeyValuePair<string, string>> _browserRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(":method", "GET"),
            new KeyValuePair<string, string>(":path", "/"),
            new KeyValuePair<string, string>(":scheme", "http"),
            new KeyValuePair<string, string>("user-agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:54.0) Gecko/20100101 Firefox/54.0"),
            new KeyValuePair<string, string>("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"),
            new KeyValuePair<string, string>("accept-language", "en-US,en;q=0.5"),
            new KeyValuePair<string, string>("accept-encoding", "gzip, deflate, br"),
            new KeyValuePair<string, string>("upgrade-insecure-requests", "1"),
        };

        private static readonly IEnumerable<KeyValuePair<string, string>> _requestTrailers = new[]
        {
            new KeyValuePair<string, string>("trailer-one", "1"),
            new KeyValuePair<string, string>("trailer-two", "2"),
        };

        private static readonly IEnumerable<KeyValuePair<string, string>> _oneContinuationRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(":method", "GET"),
            new KeyValuePair<string, string>(":path", "/"),
            new KeyValuePair<string, string>(":scheme", "http"),
            new KeyValuePair<string, string>("a", _largeHeaderValue),
            new KeyValuePair<string, string>("b", _largeHeaderValue),
            new KeyValuePair<string, string>("c", _largeHeaderValue),
            new KeyValuePair<string, string>("d", _largeHeaderValue)
        };

        private static readonly IEnumerable<KeyValuePair<string, string>> _twoContinuationsRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(":method", "GET"),
            new KeyValuePair<string, string>(":path", "/"),
            new KeyValuePair<string, string>(":scheme", "http"),
            new KeyValuePair<string, string>("a", _largeHeaderValue),
            new KeyValuePair<string, string>("b", _largeHeaderValue),
            new KeyValuePair<string, string>("c", _largeHeaderValue),
            new KeyValuePair<string, string>("d", _largeHeaderValue),
            new KeyValuePair<string, string>("e", _largeHeaderValue),
            new KeyValuePair<string, string>("f", _largeHeaderValue),
            new KeyValuePair<string, string>("g", _largeHeaderValue),
            new KeyValuePair<string, string>("h", _largeHeaderValue)
        };

        private static readonly byte[] _helloBytes = Encoding.ASCII.GetBytes("hello");
        private static readonly byte[] _worldBytes = Encoding.ASCII.GetBytes("world");
        private static readonly byte[] _helloWorldBytes = Encoding.ASCII.GetBytes("hello, world");
        private static readonly byte[] _noData = new byte[0];
        private static readonly byte[] _maxData = Encoding.ASCII.GetBytes(new string('a', Http2Frame.MinAllowedMaxFrameSize));

        private readonly BufferPool _bufferPool = new MemoryPool();
        private readonly (IPipeConnection Transport, IPipeConnection Application) _pair;
        private readonly TestApplicationErrorLogger _logger;
        private readonly Http2ConnectionContext _connectionContext;
        private readonly Http2Connection _connection;
        private readonly Http2PeerSettings _clientSettings = new Http2PeerSettings();
        private readonly HPackEncoder _hpackEncoder = new HPackEncoder();
        private readonly HPackDecoder _hpackDecoder;

        private readonly ConcurrentDictionary<int, TaskCompletionSource<object>> _runningStreams = new ConcurrentDictionary<int, TaskCompletionSource<object>>();
        private readonly Dictionary<string, string> _receivedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<int> _abortedStreamIds = new HashSet<int>();
        private readonly object _abortedStreamIdsLock = new object();

        private readonly RequestDelegate _noopApplication;
        private readonly RequestDelegate _readHeadersApplication;
        private readonly RequestDelegate _readTrailersApplication;
        private readonly RequestDelegate _bufferingApplication;
        private readonly RequestDelegate _echoApplication;
        private readonly RequestDelegate _echoWaitForAbortApplication;
        private readonly RequestDelegate _largeHeadersApplication;
        private readonly RequestDelegate _waitForAbortApplication;
        private readonly RequestDelegate _waitForAbortFlushingApplication;

        private Task _connectionTask;

        public Http2ConnectionTests()
        {
            _pair = PipeFactory.CreateConnectionPair(_bufferPool);

            _noopApplication = context => Task.CompletedTask;

            _readHeadersApplication = context =>
            {
                foreach (var header in context.Request.Headers)
                {
                    _receivedHeaders[header.Key] = header.Value.ToString();
                }

                return Task.CompletedTask;
            };

            _readTrailersApplication = async context =>
            {
                using (var ms = new MemoryStream())
                {
                    // Consuming the entire request body guarantees trailers will be available
                    await context.Request.Body.CopyToAsync(ms);
                }

                foreach (var header in context.Request.Headers)
                {
                    _receivedHeaders[header.Key] = header.Value.ToString();
                }
            };

            _bufferingApplication = async context =>
            {
                var data = new List<byte>();
                var buffer = new byte[1024];
                var received = 0;

                while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    data.AddRange(new ArraySegment<byte>(buffer, 0, received));
                }

                await context.Response.Body.WriteAsync(data.ToArray(), 0, data.Count);
            };

            _echoApplication = async context =>
            {
                var buffer = new byte[Http2Frame.MinAllowedMaxFrameSize];
                var received = 0;

                while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer, 0, received);
                }
            };

            _echoWaitForAbortApplication = async context =>
            {
                var buffer = new byte[Http2Frame.MinAllowedMaxFrameSize];
                var received = 0;

                while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer, 0, received);
                }

                var sem = new SemaphoreSlim(0);

                context.RequestAborted.Register(() =>
                {
                    sem.Release();
                });

                await sem.WaitAsync().TimeoutAfter(TimeSpan.FromSeconds(10));
            };

            _largeHeadersApplication = context =>
            {
                foreach (var name in new[] { "a", "b", "c", "d", "e", "f", "g", "h" })
                {
                    context.Response.Headers[name] = _largeHeaderValue;
                }

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

                await sem.WaitAsync().TimeoutAfter(TimeSpan.FromSeconds(10));

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

                await sem.WaitAsync().TimeoutAfter(TimeSpan.FromSeconds(10));

                await context.Response.Body.FlushAsync();

                _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
            };

            _hpackDecoder = new HPackDecoder((int)_clientSettings.HeaderTableSize);

            _logger = new TestApplicationErrorLogger();

            _connectionContext = new Http2ConnectionContext
            {
                ServiceContext = new TestServiceContext()
                {
                    Log = new TestKestrelTrace(_logger)
                },
                BufferPool = _bufferPool,
                Application = _pair.Application,
                Transport = _pair.Transport
            };
            _connection = new Http2Connection(_connectionContext);
        }

        public void Dispose()
        {
            _bufferPool.Dispose();
        }

        void IHttpHeadersHandler.OnHeader(Span<byte> name, Span<byte> value)
        {
            _decodedHeaders[name.GetAsciiStringNonNullCharacters()] = value.GetAsciiStringNonNullCharacters();
        }

        [Fact]
        public async Task DATA_Received_ReadByStream()
        {
            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _helloWorldBytes, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
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

            Assert.Equal(dataFrame.DataPayload, _helloWorldBytes);
        }

        [Fact]
        public async Task DATA_Received_MaxSize_ReadByStream()
        {
            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _maxData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            var dataFrame = await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            Assert.Equal(dataFrame.DataPayload, _maxData);
        }

        [Fact]
        public async Task DATA_Received_Multiple_ReadByStream()
        {
            await InitializeConnectionAsync(_bufferingApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

            for (var i = 0; i < _helloWorldBytes.Length; i++)
            {
                await SendDataAsync(1, new ArraySegment<byte>(_helloWorldBytes, i, 1), endStream: false);
            }

            await SendDataAsync(1, _noData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
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

            Assert.Equal(dataFrame.DataPayload, _helloWorldBytes);
        }

        [Fact]
        public async Task DATA_Received_Multiplexed_ReadByStreams()
        {
            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await StartStreamAsync(3, _browserRequestHeaders, endStream: false);

            await SendDataAsync(1, _helloBytes, endStream: false);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            var stream1DataFrame1 = await ExpectAsync(Http2FrameType.DATA,
                withLength: 5,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            await SendDataAsync(3, _helloBytes, endStream: false);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 3);
            var stream3DataFrame1 = await ExpectAsync(Http2FrameType.DATA,
                withLength: 5,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 3);

            await SendDataAsync(3, _worldBytes, endStream: false);

            var stream3DataFrame2 = await ExpectAsync(Http2FrameType.DATA,
                withLength: 5,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 3);

            await SendDataAsync(1, _worldBytes, endStream: false);

            var stream1DataFrame2 = await ExpectAsync(Http2FrameType.DATA,
                withLength: 5,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            await SendDataAsync(1, _noData, endStream: true);

            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await SendDataAsync(3, _noData, endStream: true);

            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 3);

            await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);

            Assert.Equal(stream1DataFrame1.DataPayload, _helloBytes);
            Assert.Equal(stream1DataFrame2.DataPayload, _worldBytes);
            Assert.Equal(stream3DataFrame1.DataPayload, _helloBytes);
            Assert.Equal(stream3DataFrame2.DataPayload, _worldBytes);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(255)]
        public async Task DATA_Received_WithPadding_ReadByStream(byte padLength)
        {
            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataWithPaddingAsync(1, _helloWorldBytes, padLength, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
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

            Assert.Equal(dataFrame.DataPayload, _helloWorldBytes);
        }

        [Fact]
        public async Task DATA_Received_StreamIdZero_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendDataAsync(0, _noData, endStream: false);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdZero(Http2FrameType.DATA));
        }

        [Fact]
        public async Task DATA_Received_StreamIdEven_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendDataAsync(2, _noData, endStream: false);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdEven(Http2FrameType.DATA, streamId: 2));
        }

        [Fact]
        public async Task DATA_Received_PaddingEqualToFramePayloadLength_ConnectionError()
        {
            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendInvalidDataFrameAsync(1, frameLength: 5, padLength: 5);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: true,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorPaddingTooLong(Http2FrameType.DATA));
        }

        [Fact]
        public async Task DATA_Received_PaddingGreaterThanFramePayloadLength_ConnectionError()
        {
            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendInvalidDataFrameAsync(1, frameLength: 5, padLength: 6);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: true,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorPaddingTooLong(Http2FrameType.DATA));
        }

        [Fact]
        public async Task DATA_Received_FrameLengthZeroPaddingZero_ConnectionError()
        {
            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendInvalidDataFrameAsync(1, frameLength: 0, padLength: 0);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: true,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorPaddingTooLong(Http2FrameType.DATA));
        }

        [Fact]
        public async Task DATA_Received_InterleavedWithHeaders_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
            await SendDataAsync(1, _helloWorldBytes, endStream: true);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.DATA, streamId: 1, headersStreamId: 1));
        }

        [Fact]
        public async Task DATA_Received_StreamIdle_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendDataAsync(1, _helloWorldBytes, endStream: false);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdle(Http2FrameType.DATA, streamId: 1));
        }

        [Fact]
        public async Task DATA_Received_StreamHalfClosedRemote_ConnectionError()
        {
            // Use _waitForAbortApplication so we know the stream will still be active when we send the illegal DATA frame
            await InitializeConnectionAsync(_waitForAbortApplication);

            await StartStreamAsync(1, _postRequestHeaders, endStream: true);

            await SendDataAsync(1, _helloWorldBytes, endStream: false);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(Http2FrameType.DATA, streamId: 1));
        }

        [Fact]
        public async Task DATA_Received_StreamClosed_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(1, _postRequestHeaders, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await SendDataAsync(1, _helloWorldBytes, endStream: false);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.DATA, streamId: 1));
        }

        [Fact]
        public async Task DATA_Received_StreamClosedImplicitly_ConnectionError()
        {
            // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1.1
            //
            // The first use of a new stream identifier implicitly closes all streams in the "idle" state that
            // might have been initiated by that peer with a lower-valued stream identifier. For example, if a
            // client sends a HEADERS frame on stream 7 without ever sending a frame on stream 5, then stream 5
            // transitions to the "closed" state when the first frame for stream 7 is sent or received.

            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 3);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 3);

            await SendDataAsync(1, _helloWorldBytes, endStream: true);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 3,
                expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.DATA, streamId: 1));
        }

        [Fact]
        public async Task HEADERS_Received_Decoded()
        {
            await InitializeConnectionAsync(_readHeadersApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            VerifyDecodedRequestHeaders(_browserRequestHeaders);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(255)]
        public async Task HEADERS_Received_WithPadding_Decoded(byte padLength)
        {
            await InitializeConnectionAsync(_readHeadersApplication);

            await SendHeadersWithPaddingAsync(1, _browserRequestHeaders, padLength, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            VerifyDecodedRequestHeaders(_browserRequestHeaders);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task HEADERS_Received_WithPriority_Decoded()
        {
            await InitializeConnectionAsync(_readHeadersApplication);

            await SendHeadersWithPriorityAsync(1, _browserRequestHeaders, priority: 42, streamDependency: 0, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            VerifyDecodedRequestHeaders(_browserRequestHeaders);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(255)]
        public async Task HEADERS_Received_WithPriorityAndPadding_Decoded(byte padLength)
        {
            await InitializeConnectionAsync(_readHeadersApplication);

            await SendHeadersWithPaddingAndPriorityAsync(1, _browserRequestHeaders, padLength, priority: 42, streamDependency: 0, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            VerifyDecodedRequestHeaders(_browserRequestHeaders);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HEADERS_Received_WithTrailers_Decoded(bool sendData)
        {
            await InitializeConnectionAsync(_readTrailersApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);

            // Initialize another stream with a higher stream ID, and verify that after trailers are
            // decoded by the other stream, the highest opened stream ID is not reset to the lower ID
            // (the highest opened stream ID is sent by the server in the GOAWAY frame when shutting
            // down the connection).
            await SendHeadersAsync(3, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);

            // The second stream should end first, since the first one is waiting for the request body.
            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 3);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 3);

            if (sendData)
            {
                await SendDataAsync(1, _helloBytes, endStream: false);
            }

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, _requestTrailers);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            VerifyDecodedRequestHeaders(_browserRequestHeaders.Concat(_requestTrailers));

            await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task HEADERS_Received_ContainsExpect100Continue_100ContinueSent()
        {
            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _expectContinueRequestHeaders, false);

            var frame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 5,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);

            await SendDataAsync(1, _helloBytes, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 5,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            Assert.Equal(new byte[] { 0x08, 0x03, (byte)'1', (byte)'0', (byte)'0' }, frame.HeadersPayload.ToArray());

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task HEADERS_Received_StreamIdZero_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(0, _browserRequestHeaders, endStream: true);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdZero(Http2FrameType.HEADERS));
        }

        [Fact]
        public async Task HEADERS_Received_StreamIdEven_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(2, _browserRequestHeaders, endStream: true);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdEven(Http2FrameType.HEADERS, streamId: 2));
        }

        [Fact]
        public async Task HEADERS_Received_StreamClosed_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            // Try to re-use the stream ID (http://httpwg.org/specs/rfc7540.html#rfc.section.5.1.1)
            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1));
        }

        [Fact]
        public async Task HEADERS_Received_StreamHalfClosedRemote_ConnectionError()
        {
            // Use _waitForAbortApplication so we know the stream will still be active when we send the illegal DATA frame
            await InitializeConnectionAsync(_waitForAbortApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(Http2FrameType.HEADERS, streamId: 1));
        }

        [Fact]
        public async Task HEADERS_Received_StreamClosedImplicitly_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 3);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 3);

            // Stream 1 was implicitly closed by opening stream 3 before (http://httpwg.org/specs/rfc7540.html#rfc.section.5.1.1)
            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 3,
                expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(255)]
        public async Task HEADERS_Received_PaddingEqualToFramePayloadLength_ConnectionError(byte padLength)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendInvalidHeadersFrameAsync(1, frameLength: padLength, padLength: padLength);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: true,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorPaddingTooLong(Http2FrameType.HEADERS));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(254, 255)]
        public async Task HEADERS_Received_PaddingGreaterThanFramePayloadLength_ConnectionError(int frameLength, byte padLength)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendInvalidHeadersFrameAsync(1, frameLength, padLength);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: true,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorPaddingTooLong(Http2FrameType.HEADERS));
        }

        [Fact]
        public async Task HEADERS_Received_InterleavedWithHeaders_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
            await SendHeadersAsync(3, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.HEADERS, streamId: 3, headersStreamId: 1));
        }

        [Fact]
        public async Task HEADERS_Received_WithPriority_StreamDependencyOnSelf_ConnectionError()
        {
            await InitializeConnectionAsync(_readHeadersApplication);

            await SendHeadersWithPriorityAsync(1, _browserRequestHeaders, priority: 42, streamDependency: 1, endStream: true);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamSelfDependency(Http2FrameType.HEADERS, streamId: 1));
        }

        [Fact]
        public async Task HEADERS_Received_IncompleteHeaderBlock_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendIncompleteHeadersFrameAsync(streamId: 1);

            await WaitForConnectionErrorAsync<HPackDecodingException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.COMPRESSION_ERROR,
                expectedErrorMessage: CoreStrings.HPackErrorIncompleteHeaderBlock);
        }

        [Theory]
        [MemberData(nameof(IllegalTrailerData))]
        public async Task HEADERS_Received_WithTrailers_ContainsIllegalTrailer_ConnectionError(byte[] trailers, string expectedErrorMessage)
        {
            await InitializeConnectionAsync(_readTrailersApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, trailers);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: expectedErrorMessage);
        }

        [Theory]
        [InlineData(Http2HeadersFrameFlags.NONE)]
        [InlineData(Http2HeadersFrameFlags.END_HEADERS)]
        public async Task HEADERS_Received_WithTrailers_EndStreamNotSet_ConnectionError(Http2HeadersFrameFlags flags)
        {
            await InitializeConnectionAsync(_readTrailersApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);
            await SendHeadersAsync(1, flags, _requestTrailers);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.Http2ErrorHeadersWithTrailersNoEndStream);
        }

        [Theory]
        [MemberData(nameof(UpperCaseHeaderNameData))]
        public async Task HEADERS_Received_HeaderNameContainsUpperCaseCharacter_StreamError(byte[] headerBlock)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headerBlock);
            await WaitForStreamErrorAsync(
                ignoreNonRstStreamFrames: false,
                expectedStreamId: 1,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.Http2ErrorHeaderNameUppercase);

            // Verify that the stream ID can't be re-used
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);
            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1));
        }

        [Fact]
        public Task HEADERS_Received_HeaderBlockContainsUnknownPseudoHeaderField_StreamError()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(":method", "GET"),
                new KeyValuePair<string, string>(":path", "/"),
                new KeyValuePair<string, string>(":scheme", "http"),
                new KeyValuePair<string, string>(":unknown", "0"),
            };

            return HEADERS_Received_InvalidHeaderFields_StreamError(headers, expectedErrorMessage: CoreStrings.Http2ErrorUnknownPseudoHeaderField);
        }

        [Fact]
        public Task HEADERS_Received_HeaderBlockContainsResponsePseudoHeaderField_StreamError()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(":method", "GET"),
                new KeyValuePair<string, string>(":path", "/"),
                new KeyValuePair<string, string>(":scheme", "http"),
                new KeyValuePair<string, string>(":status", "200"),
            };

            return HEADERS_Received_InvalidHeaderFields_StreamError(headers, expectedErrorMessage: CoreStrings.Http2ErrorResponsePseudoHeaderField);
        }

        [Theory]
        [MemberData(nameof(DuplicatePseudoHeaderFieldData))]
        public Task HEADERS_Received_HeaderBlockContainsDuplicatePseudoHeaderField_StreamError(IEnumerable<KeyValuePair<string, string>> headers)
        {
            return HEADERS_Received_InvalidHeaderFields_StreamError(headers, expectedErrorMessage: CoreStrings.Http2ErrorDuplicatePseudoHeaderField);
        }

        [Theory]
        [MemberData(nameof(MissingPseudoHeaderFieldData))]
        public Task HEADERS_Received_HeaderBlockDoesNotContainMandatoryPseudoHeaderField_StreamError(IEnumerable<KeyValuePair<string, string>> headers)
        {
            return HEADERS_Received_InvalidHeaderFields_StreamError(headers, expectedErrorMessage: CoreStrings.Http2ErrorMissingMandatoryPseudoHeaderFields);
        }

        [Theory]
        [MemberData(nameof(ConnectMissingPseudoHeaderFieldData))]
        public async Task HEADERS_Received_HeaderBlockDoesNotContainMandatoryPseudoHeaderField_MethodIsCONNECT_NoError(IEnumerable<KeyValuePair<string, string>> headers)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Theory]
        [MemberData(nameof(PseudoHeaderFieldAfterRegularHeadersData))]
        public Task HEADERS_Received_HeaderBlockContainsPseudoHeaderFieldAfterRegularHeaders_StreamError(IEnumerable<KeyValuePair<string, string>> headers)
        {
            return HEADERS_Received_InvalidHeaderFields_StreamError(headers, expectedErrorMessage: CoreStrings.Http2ErrorPseudoHeaderFieldAfterRegularHeaders);
        }

        private async Task HEADERS_Received_InvalidHeaderFields_StreamError(IEnumerable<KeyValuePair<string, string>> headers, string expectedErrorMessage)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);
            await WaitForStreamErrorAsync(
                ignoreNonRstStreamFrames: false,
                expectedStreamId: 1,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: expectedErrorMessage);

            // Verify that the stream ID can't be re-used
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);
            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1));
        }

        [Fact]
        public Task HEADERS_Received_HeaderBlockContainsConnectionHeader_StreamError()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(":method", "GET"),
                new KeyValuePair<string, string>(":path", "/"),
                new KeyValuePair<string, string>(":scheme", "http"),
                new KeyValuePair<string, string>("connection", "keep-alive")
            };

            return HEADERS_Received_InvalidHeaderFields_StreamError(headers, CoreStrings.Http2ErrorConnectionSpecificHeaderField);
        }

        [Fact]
        public Task HEADERS_Received_HeaderBlockContainsTEHeader_ValueIsNotTrailers_StreamError()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(":method", "GET"),
                new KeyValuePair<string, string>(":path", "/"),
                new KeyValuePair<string, string>(":scheme", "http"),
                new KeyValuePair<string, string>("te", "trailers, deflate")
            };

            return HEADERS_Received_InvalidHeaderFields_StreamError(headers, CoreStrings.Http2ErrorConnectionSpecificHeaderField);
        }

        [Fact]
        public async Task HEADERS_Received_HeaderBlockContainsTEHeader_ValueIsTrailers_NoError()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(":method", "GET"),
                new KeyValuePair<string, string>(":path", "/"),
                new KeyValuePair<string, string>(":scheme", "http"),
                new KeyValuePair<string, string>("te", "trailers")
            };

            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task PRIORITY_Received_StreamIdZero_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendPriorityAsync(0);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdZero(Http2FrameType.PRIORITY));
        }

        [Fact]
        public async Task PRIORITY_Received_StreamIdEven_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendPriorityAsync(2);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdEven(Http2FrameType.PRIORITY, streamId: 2));
        }

        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        public async Task PRIORITY_Received_LengthNotFive_ConnectionError(int length)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendInvalidPriorityFrameAsync(1, length);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(Http2FrameType.PRIORITY, expectedLength: 5));
        }

        [Fact]
        public async Task PRIORITY_Received_InterleavedWithHeaders_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
            await SendPriorityAsync(1);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.PRIORITY, streamId: 1, headersStreamId: 1));
        }

        [Fact]
        public async Task PRIORITY_Received_StreamDependencyOnSelf_ConnectionError()
        {
            await InitializeConnectionAsync(_readHeadersApplication);

            await SendPriorityAsync(1, streamDependency: 1);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamSelfDependency(Http2FrameType.PRIORITY, 1));
        }

        [Fact]
        public async Task RST_STREAM_Received_AbortsStream()
        {
            await InitializeConnectionAsync(_waitForAbortApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
            await SendRstStreamAsync(1);

            // No data is received from the stream since it was aborted before writing anything

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            await WaitForAllStreamsAsync();
            Assert.Contains(1, _abortedStreamIds);
        }

        [Fact]
        public async Task RST_STREAM_Received_AbortsStream_FlushedDataIsSent()
        {
            await InitializeConnectionAsync(_waitForAbortFlushingApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
            await SendRstStreamAsync(1);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);

            // No END_STREAM DATA frame is received since the stream was aborted

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            Assert.Contains(1, _abortedStreamIds);
        }

        [Fact]
        public async Task RST_STREAM_Received_StreamIdZero_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendRstStreamAsync(0);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdZero(Http2FrameType.RST_STREAM));
        }

        [Fact]
        public async Task RST_STREAM_Received_StreamIdEven_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendRstStreamAsync(2);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdEven(Http2FrameType.RST_STREAM, streamId: 2));
        }

        [Fact]
        public async Task RST_STREAM_Received_StreamIdle_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendRstStreamAsync(1);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdle(Http2FrameType.RST_STREAM, streamId: 1));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(5)]
        public async Task RST_STREAM_Received_LengthNotFour_ConnectionError(int length)
        {
            await InitializeConnectionAsync(_noopApplication);

            // Start stream 1 so it's legal to send it RST_STREAM frames
            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

            await SendInvalidRstStreamFrameAsync(1, length);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: true,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(Http2FrameType.RST_STREAM, expectedLength: 4));
        }

        [Fact]
        public async Task RST_STREAM_Received_InterleavedWithHeaders_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
            await SendRstStreamAsync(1);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.RST_STREAM, streamId: 1, headersStreamId: 1));
        }

        [Fact]
        public async Task SETTINGS_Received_Sends_ACK()
        {
            await InitializeConnectionAsync(_noopApplication);

            await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task SETTINGS_Received_StreamIdNotZero_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendSettingsWithInvalidStreamIdAsync(1);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdNotZero(Http2FrameType.SETTINGS));
        }

        [Theory]
        [InlineData(Http2SettingsParameter.SETTINGS_ENABLE_PUSH, 2, Http2ErrorCode.PROTOCOL_ERROR)]
        [InlineData(Http2SettingsParameter.SETTINGS_ENABLE_PUSH, uint.MaxValue, Http2ErrorCode.PROTOCOL_ERROR)]
        [InlineData(Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE, (uint)int.MaxValue + 1, Http2ErrorCode.FLOW_CONTROL_ERROR)]
        [InlineData(Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE, uint.MaxValue, Http2ErrorCode.FLOW_CONTROL_ERROR)]
        [InlineData(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE, 0, Http2ErrorCode.PROTOCOL_ERROR)]
        [InlineData(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE, 1, Http2ErrorCode.PROTOCOL_ERROR)]
        [InlineData(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE, 16 * 1024 - 1, Http2ErrorCode.PROTOCOL_ERROR)]
        [InlineData(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE, 16 * 1024 * 1024, Http2ErrorCode.PROTOCOL_ERROR)]
        [InlineData(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE, uint.MaxValue, Http2ErrorCode.PROTOCOL_ERROR)]
        public async Task SETTINGS_Received_InvalidParameterValue_ConnectionError(Http2SettingsParameter parameter, uint value, Http2ErrorCode expectedErrorCode)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendSettingsWithInvalidParameterValueAsync(parameter, value);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: expectedErrorCode,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorSettingsParameterOutOfRange(parameter));
        }

        [Fact]
        public async Task SETTINGS_Received_InterleavedWithHeaders_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
            await SendSettingsAsync();

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.SETTINGS, streamId: 0, headersStreamId: 1));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(16 * 1024 - 9)] // Min. max. frame size minus header length
        public async Task SETTINGS_Received_WithACK_LengthNotZero_ConnectionError(int length)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendSettingsAckWithInvalidLengthAsync(length);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
                expectedErrorMessage: CoreStrings.Http2ErrorSettingsAckLengthNotZero);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(34)]
        [InlineData(37)]
        public async Task SETTINGS_Received_LengthNotMultipleOfSix_ConnectionError(int length)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendSettingsWithInvalidLengthAsync(length);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
                expectedErrorMessage: CoreStrings.Http2ErrorSettingsLengthNotMultipleOfSix);
        }

        [Fact]
        public async Task PUSH_PROMISE_Received_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendPushPromiseFrameAsync();

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.Http2ErrorPushPromiseReceived);
        }

        [Fact]
        public async Task PING_Received_SendsACK()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendPingAsync(Http2PingFrameFlags.NONE);
            await ExpectAsync(Http2FrameType.PING,
                withLength: 8,
                withFlags: (byte)Http2PingFrameFlags.ACK,
                withStreamId: 0);

            await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task PING_Received_WithACK_DoesNotSendACK()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendPingAsync(Http2PingFrameFlags.ACK);

            await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task PING_Received_InterleavedWithHeaders_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
            await SendPingAsync(Http2PingFrameFlags.NONE);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.PING, streamId: 0, headersStreamId: 1));
        }

        [Fact]
        public async Task PING_Received_StreamIdNotZero_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendPingWithInvalidStreamIdAsync(streamId: 1);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdNotZero(Http2FrameType.PING));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(9)]
        public async Task PING_Received_LengthNotEight_ConnectionError(int length)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendPingWithInvalidLengthAsync(length);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(Http2FrameType.PING, expectedLength: 8));
        }

        [Fact]
        public async Task GOAWAY_Received_ConnectionStops()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendGoAwayAsync();

            await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task GOAWAY_Received_AbortsAllStreams()
        {
            await InitializeConnectionAsync(_waitForAbortApplication);

            // Start some streams
            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
            await StartStreamAsync(3, _browserRequestHeaders, endStream: true);
            await StartStreamAsync(5, _browserRequestHeaders, endStream: true);

            await SendGoAwayAsync();

            await WaitForConnectionStopAsync(expectedLastStreamId: 5, ignoreNonGoAwayFrames: true);

            await WaitForAllStreamsAsync();
            Assert.Contains(1, _abortedStreamIds);
            Assert.Contains(3, _abortedStreamIds);
            Assert.Contains(5, _abortedStreamIds);
        }

        [Fact]
        public async Task GOAWAY_Received_StreamIdNotZero_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendInvalidGoAwayFrameAsync();

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdNotZero(Http2FrameType.GOAWAY));
        }

        [Fact]
        public async Task GOAWAY_Received_InterleavedWithHeaders_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
            await SendGoAwayAsync();

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.GOAWAY, streamId: 0, headersStreamId: 1));
        }

        [Fact]
        public async Task WINDOW_UPDATE_Received_StreamIdEven_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendWindowUpdateAsync(2, sizeIncrement: 42);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdEven(Http2FrameType.WINDOW_UPDATE, streamId: 2));
        }

        [Fact]
        public async Task WINDOW_UPDATE_Received_InterleavedWithHeaders_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
            await SendWindowUpdateAsync(1, sizeIncrement: 42);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.WINDOW_UPDATE, streamId: 1, headersStreamId: 1));
        }

        [Theory]
        [InlineData(0, 3)]
        [InlineData(0, 5)]
        [InlineData(1, 3)]
        [InlineData(1, 5)]
        public async Task WINDOW_UPDATE_Received_LengthNotFour_ConnectionError(int streamId, int length)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendInvalidWindowUpdateAsync(streamId, sizeIncrement: 42, length: length);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(Http2FrameType.WINDOW_UPDATE, expectedLength: 4));
        }

        [Fact]
        public async Task WINDOW_UPDATE_Received_OnConnection_SizeIncrementZero_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendWindowUpdateAsync(0, sizeIncrement: 0);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.Http2ErrorWindowUpdateIncrementZero);
        }

        [Fact]
        public async Task WINDOW_UPDATE_Received_OnStream_SizeIncrementZero_ConnectionError()
        {
            await InitializeConnectionAsync(_waitForAbortApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
            await SendWindowUpdateAsync(1, sizeIncrement: 0);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.Http2ErrorWindowUpdateIncrementZero);
        }

        [Fact]
        public async Task WINDOW_UPDATE_Received_StreamIdle_ConnectionError()
        {
            await InitializeConnectionAsync(_waitForAbortApplication);

            await SendWindowUpdateAsync(1, sizeIncrement: 1);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdle(Http2FrameType.WINDOW_UPDATE, streamId: 1));
        }

        [Fact]
        public async Task CONTINUATION_Received_Decoded()
        {
            await InitializeConnectionAsync(_readHeadersApplication);

            await StartStreamAsync(1, _twoContinuationsRequestHeaders, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
                withStreamId: 1);

            VerifyDecodedRequestHeaders(_twoContinuationsRequestHeaders);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CONTINUATION_Received_WithTrailers_Decoded(bool sendData)
        {
            await InitializeConnectionAsync(_readTrailersApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);

            // Initialize another stream with a higher stream ID, and verify that after trailers are
            // decoded by the other stream, the highest opened stream ID is not reset to the lower ID
            // (the highest opened stream ID is sent by the server in the GOAWAY frame when shutting
            // down the connection).
            await SendHeadersAsync(3, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);

            // The second stream should end first, since the first one is waiting for the request body.
            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 3);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 3);

            if (sendData)
            {
                await SendDataAsync(1, _helloBytes, endStream: false);
            }

            // Trailers encoded as Literal Header Field without Indexing - New Name
            //   trailer-1: 1
            //   trailer-2: 2
            var trailers = new byte[] { 0x00, 0x09 }
                .Concat(Encoding.ASCII.GetBytes("trailer-1"))
                .Concat(new byte[] { 0x01, (byte)'1' })
                .Concat(new byte[] { 0x00, 0x09 })
                .Concat(Encoding.ASCII.GetBytes("trailer-2"))
                .Concat(new byte[] { 0x01, (byte)'2' })
                .ToArray();
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, new byte[0]);
            await SendContinuationAsync(1, Http2ContinuationFrameFlags.END_HEADERS, trailers);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            VerifyDecodedRequestHeaders(_browserRequestHeaders.Concat(new[]
            {
                new KeyValuePair<string, string>("trailer-1", "1"),
                new KeyValuePair<string, string>("trailer-2", "2")
            }));

            await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task CONTINUATION_Received_StreamIdMismatch_ConnectionError()
        {
            await InitializeConnectionAsync(_readHeadersApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _oneContinuationRequestHeaders);
            await SendContinuationAsync(3, Http2ContinuationFrameFlags.END_HEADERS);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.CONTINUATION, streamId: 3, headersStreamId: 1));
        }

        [Fact]
        public async Task CONTINUATION_Received_IncompleteHeaderBlock_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _postRequestHeaders);
            await SendIncompleteContinuationFrameAsync(streamId: 1);

            await WaitForConnectionErrorAsync<HPackDecodingException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.COMPRESSION_ERROR,
                expectedErrorMessage: CoreStrings.HPackErrorIncompleteHeaderBlock);
        }

        [Theory]
        [MemberData(nameof(IllegalTrailerData))]
        public async Task CONTINUATION_Received_WithTrailers_ContainsIllegalTrailer_ConnectionError(byte[] trailers, string expectedErrorMessage)
        {
            await InitializeConnectionAsync(_readTrailersApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, new byte[0]);
            await SendContinuationAsync(1, Http2ContinuationFrameFlags.END_HEADERS, trailers);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: expectedErrorMessage);
        }

        [Theory]
        [MemberData(nameof(MissingPseudoHeaderFieldData))]
        public async Task CONTINUATION_Received_HeaderBlockDoesNotContainMandatoryPseudoHeaderField_StreamError(IEnumerable<KeyValuePair<string, string>> headers)
        {
            await InitializeConnectionAsync(_noopApplication);

            Assert.True(await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, headers));
            await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.END_HEADERS);

            await WaitForStreamErrorAsync(
                ignoreNonRstStreamFrames: false,
                expectedStreamId: 1,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.Http2ErrorMissingMandatoryPseudoHeaderFields);

            // Verify that the stream ID can't be re-used
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);
            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1));
        }

        [Theory]
        [MemberData(nameof(ConnectMissingPseudoHeaderFieldData))]
        public async Task CONTINUATION_Received_HeaderBlockDoesNotContainMandatoryPseudoHeaderField_MethodIsCONNECT_NoError(IEnumerable<KeyValuePair<string, string>> headers)
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, headers);
            await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.END_HEADERS);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task CONTINUATION_Sent_WhenHeadersLargerThanFrameLength()
        {
            await InitializeConnectionAsync(_largeHeadersApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

            var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 12361,
                withFlags: (byte)Http2HeadersFrameFlags.NONE,
                withStreamId: 1);
            var continuationFrame1 = await ExpectAsync(Http2FrameType.CONTINUATION,
                withLength: 12306,
                withFlags: (byte)Http2ContinuationFrameFlags.NONE,
                withStreamId: 1);
            var continuationFrame2 = await ExpectAsync(Http2FrameType.CONTINUATION,
                withLength: 8204,
                withFlags: (byte)Http2ContinuationFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _hpackDecoder.Decode(headersFrame.HeadersPayload, endHeaders: false, handler: this);
            _hpackDecoder.Decode(continuationFrame1.HeadersPayload, endHeaders: false, handler: this);
            _hpackDecoder.Decode(continuationFrame2.HeadersPayload, endHeaders: true, handler: this);

            Assert.Equal(11, _decodedHeaders.Count);
            Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("200", _decodedHeaders[":status"]);
            Assert.Equal("0", _decodedHeaders["content-length"]);
            Assert.Equal(_largeHeaderValue, _decodedHeaders["a"]);
            Assert.Equal(_largeHeaderValue, _decodedHeaders["b"]);
            Assert.Equal(_largeHeaderValue, _decodedHeaders["c"]);
            Assert.Equal(_largeHeaderValue, _decodedHeaders["d"]);
            Assert.Equal(_largeHeaderValue, _decodedHeaders["e"]);
            Assert.Equal(_largeHeaderValue, _decodedHeaders["f"]);
            Assert.Equal(_largeHeaderValue, _decodedHeaders["g"]);
            Assert.Equal(_largeHeaderValue, _decodedHeaders["h"]);
        }

        [Fact]
        public async Task UnknownFrameType_Received_Ignored()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendUnknownFrameTypeAsync(streamId: 1, frameType: 42);

            // Check that the connection is still alive
            await SendPingAsync(Http2PingFrameFlags.NONE);
            await ExpectAsync(Http2FrameType.PING,
                withLength: 8,
                withFlags: (byte)Http2PingFrameFlags.ACK,
                withStreamId: 0);

            await StopConnectionAsync(0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task UnknownFrameType_Received_InterleavedWithHeaders_ConnectionError()
        {
            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
            await SendUnknownFrameTypeAsync(streamId: 1, frameType: 42);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 0,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(frameType: 42, streamId: 1, headersStreamId: 1));
        }

        [Fact]
        public async Task ConnectionErrorAbortsAllStreams()
        {
            await InitializeConnectionAsync(_waitForAbortApplication);

            // Start some streams
            await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
            await StartStreamAsync(3, _browserRequestHeaders, endStream: true);
            await StartStreamAsync(5, _browserRequestHeaders, endStream: true);

            // Cause a connection error by sending an invalid frame
            await SendDataAsync(0, _noData, endStream: false);

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 5,
                expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
                expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdZero(Http2FrameType.DATA));

            await WaitForAllStreamsAsync();
            Assert.Contains(1, _abortedStreamIds);
            Assert.Contains(3, _abortedStreamIds);
            Assert.Contains(5, _abortedStreamIds);
        }

        [Fact]
        public async Task ConnectionResetLoggedWithActiveStreams()
        {
            await InitializeConnectionAsync(_waitForAbortApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);

            _pair.Application.Output.Complete(new ConnectionResetException(string.Empty));

            var result = await _pair.Application.Input.ReadAsync();
            Assert.True(result.IsCompleted);
            Assert.Single(_logger.Messages, m => m.Exception is ConnectionResetException);
        }

        [Fact]
        public async Task ConnectionResetNotLoggedWithNoActiveStreams()
        {
            await InitializeConnectionAsync(_waitForAbortApplication);

            _pair.Application.Output.Complete(new ConnectionResetException(string.Empty));

            var result = await _pair.Application.Input.ReadAsync();
            Assert.True(result.IsCompleted);
            Assert.DoesNotContain(_logger.Messages, m => m.Exception is ConnectionResetException);
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

        private async Task SendHeadersWithPaddingAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte padLength, bool endStream)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _runningStreams[streamId] = tcs;

            var frame = new Http2Frame();

            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PADDED, streamId);
            frame.HeadersPadLength = padLength;

            _hpackEncoder.BeginEncode(headers, frame.HeadersPayload, out var length);

            frame.Length = 1 + length + padLength;
            frame.Payload.Slice(1 + length).Fill(0);

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            await SendAsync(frame.Raw);
        }

        private async Task SendHeadersWithPriorityAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte priority, int streamDependency, bool endStream)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _runningStreams[streamId] = tcs;

            var frame = new Http2Frame();
            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PRIORITY, streamId);
            frame.HeadersPriority = priority;
            frame.HeadersStreamDependency = streamDependency;

            _hpackEncoder.BeginEncode(headers, frame.HeadersPayload, out var length);

            frame.Length = 5 + length;

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            await SendAsync(frame.Raw);
        }

        private async Task SendHeadersWithPaddingAndPriorityAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte padLength, byte priority, int streamDependency, bool endStream)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _runningStreams[streamId] = tcs;

            var frame = new Http2Frame();
            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PADDED | Http2HeadersFrameFlags.PRIORITY, streamId);
            frame.HeadersPadLength = padLength;
            frame.HeadersPriority = priority;
            frame.HeadersStreamDependency = streamDependency;

            _hpackEncoder.BeginEncode(headers, frame.HeadersPayload, out var length);

            frame.Length = 6 + length + padLength;
            frame.Payload.Slice(6 + length).Fill(0);

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            await SendAsync(frame.Raw);
        }

        private Task SendStreamDataAsync(int streamId, Span<byte> data)
        {
            var tasks = new List<Task>();
            var frame = new Http2Frame();

            frame.PrepareData(streamId);

            while (data.Length > frame.Length)
            {
                data.Slice(0, frame.Length).CopyTo(frame.Payload);
                data = data.Slice(frame.Length);
                tasks.Add(SendAsync(frame.Raw));
            }

            frame.Length = data.Length;
            frame.DataFlags = Http2DataFrameFlags.END_STREAM;
            data.CopyTo(frame.Payload);
            tasks.Add(SendAsync(frame.Raw));

            return Task.WhenAll(tasks);
        }

        private Task WaitForAllStreamsAsync()
        {
            return Task.WhenAll(_runningStreams.Values.Select(tcs => tcs.Task)).TimeoutAfter(TimeSpan.FromSeconds(30));
        }

        private async Task SendAsync(ArraySegment<byte> span)
        {
            var writableBuffer = _pair.Application.Output.Alloc(1);
            writableBuffer.Write(span);
            await writableBuffer.FlushAsync();
        }

        private Task SendPreambleAsync() => SendAsync(new ArraySegment<byte>(Http2Connection.ClientPreface));

        private Task SendSettingsAsync()
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE, _clientSettings);
            return SendAsync(frame.Raw);
        }

        private Task SendSettingsAckWithInvalidLengthAsync(int length)
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.ACK);
            frame.Length = length;
            return SendAsync(frame.Raw);
        }

        private Task SendSettingsWithInvalidStreamIdAsync(int streamId)
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE, _clientSettings);
            frame.StreamId = streamId;
            return SendAsync(frame.Raw);
        }

        private Task SendSettingsWithInvalidLengthAsync(int length)
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE, _clientSettings);
            frame.Length = length;
            return SendAsync(frame.Raw);
        }

        private Task SendSettingsWithInvalidParameterValueAsync(Http2SettingsParameter parameter, uint value)
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE);
            frame.Length = 6;

            frame.Payload[0] = (byte)((ushort)parameter >> 8);
            frame.Payload[1] = (byte)(ushort)parameter;
            frame.Payload[2] = (byte)(value >> 24);
            frame.Payload[3] = (byte)(value >> 16);
            frame.Payload[4] = (byte)(value >> 8);
            frame.Payload[5] = (byte)value;

            return SendAsync(frame.Raw);
        }

        private Task SendPushPromiseFrameAsync()
        {
            var frame = new Http2Frame();
            frame.Length = 0;
            frame.Type = Http2FrameType.PUSH_PROMISE;
            frame.StreamId = 1;
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

        private Task SendHeadersAsync(int streamId, Http2HeadersFrameFlags flags, byte[] headerBlock)
        {
            var frame = new Http2Frame();

            frame.PrepareHeaders(flags, streamId);
            frame.Length = headerBlock.Length;
            headerBlock.CopyTo(frame.HeadersPayload);

            return SendAsync(frame.Raw);
        }

        private Task SendInvalidHeadersFrameAsync(int streamId, int frameLength, byte padLength)
        {
            Assert.True(padLength >= frameLength, $"{nameof(padLength)} must be greater than or equal to {nameof(frameLength)} to create an invalid frame.");

            var frame = new Http2Frame();

            frame.PrepareHeaders(Http2HeadersFrameFlags.PADDED, streamId);
            frame.Payload[0] = padLength;

            // Set length last so .Payload can be written to
            frame.Length = frameLength;

            return SendAsync(frame.Raw);
        }

        private Task SendIncompleteHeadersFrameAsync(int streamId)
        {
            var frame = new Http2Frame();

            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS, streamId);
            frame.Length = 3;

            // Set up an incomplete Literal Header Field w/ Incremental Indexing frame,
            // with an incomplete new name
            frame.Payload[0] = 0;
            frame.Payload[1] = 2;
            frame.Payload[2] = (byte)'a';

            return SendAsync(frame.Raw);
        }

        private async Task<bool> SendContinuationAsync(int streamId, Http2ContinuationFrameFlags flags)
        {
            var frame = new Http2Frame();

            frame.PrepareContinuation(flags, streamId);
            var done = _hpackEncoder.Encode(frame.Payload, out var length);
            frame.Length = length;

            await SendAsync(frame.Raw);

            return done;
        }

        private async Task SendContinuationAsync(int streamId, Http2ContinuationFrameFlags flags, byte[] payload)
        {
            var frame = new Http2Frame();

            frame.PrepareContinuation(flags, streamId);
            frame.Length = payload.Length;
            payload.CopyTo(frame.Payload);

            await SendAsync(frame.Raw);
        }

        private Task SendEmptyContinuationFrameAsync(int streamId, Http2ContinuationFrameFlags flags)
        {
            var frame = new Http2Frame();

            frame.PrepareContinuation(flags, streamId);
            frame.Length = 0;

            return SendAsync(frame.Raw);
        }

        private Task SendIncompleteContinuationFrameAsync(int streamId)
        {
            var frame = new Http2Frame();

            frame.PrepareContinuation(Http2ContinuationFrameFlags.END_HEADERS, streamId);
            frame.Length = 3;

            // Set up an incomplete Literal Header Field w/ Incremental Indexing frame,
            // with an incomplete new name
            frame.Payload[0] = 0;
            frame.Payload[1] = 2;
            frame.Payload[2] = (byte)'a';

            return SendAsync(frame.Raw);
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

        private Task SendDataWithPaddingAsync(int streamId, Span<byte> data, byte padLength, bool endStream)
        {
            var frame = new Http2Frame();

            frame.PrepareData(streamId, padLength);
            frame.Length = data.Length + 1 + padLength;
            data.CopyTo(frame.DataPayload);

            if (endStream)
            {
                frame.DataFlags |= Http2DataFrameFlags.END_STREAM;
            }

            return SendAsync(frame.Raw);
        }

        private Task SendInvalidDataFrameAsync(int streamId, int frameLength, byte padLength)
        {
            Assert.True(padLength >= frameLength, $"{nameof(padLength)} must be greater than or equal to {nameof(frameLength)} to create an invalid frame.");

            var frame = new Http2Frame();

            frame.PrepareData(streamId);
            frame.DataFlags = Http2DataFrameFlags.PADDED;
            frame.Payload[0] = padLength;

            // Set length last so .Payload can be written to
            frame.Length = frameLength;

            return SendAsync(frame.Raw);
        }

        private Task SendPingAsync(Http2PingFrameFlags flags)
        {
            var pingFrame = new Http2Frame();
            pingFrame.PreparePing(flags);
            return SendAsync(pingFrame.Raw);
        }

        private Task SendPingWithInvalidLengthAsync(int length)
        {
            var pingFrame = new Http2Frame();
            pingFrame.PreparePing(Http2PingFrameFlags.NONE);
            pingFrame.Length = length;
            return SendAsync(pingFrame.Raw);
        }

        private Task SendPingWithInvalidStreamIdAsync(int streamId)
        {
            Assert.NotEqual(0, streamId);

            var pingFrame = new Http2Frame();
            pingFrame.PreparePing(Http2PingFrameFlags.NONE);
            pingFrame.StreamId = streamId;
            return SendAsync(pingFrame.Raw);
        }

        private Task SendPriorityAsync(int streamId, int streamDependency = 0)
        {
            var priorityFrame = new Http2Frame();
            priorityFrame.PreparePriority(streamId, streamDependency: streamDependency, exclusive: false, weight: 0);
            return SendAsync(priorityFrame.Raw);
        }

        private Task SendInvalidPriorityFrameAsync(int streamId, int length)
        {
            var priorityFrame = new Http2Frame();
            priorityFrame.PreparePriority(streamId, streamDependency: 0, exclusive: false, weight: 0);
            priorityFrame.Length = length;
            return SendAsync(priorityFrame.Raw);
        }

        private Task SendRstStreamAsync(int streamId)
        {
            var rstStreamFrame = new Http2Frame();
            rstStreamFrame.PrepareRstStream(streamId, Http2ErrorCode.CANCEL);
            return SendAsync(rstStreamFrame.Raw);
        }

        private Task SendInvalidRstStreamFrameAsync(int streamId, int length)
        {
            var frame = new Http2Frame();
            frame.PrepareRstStream(streamId, Http2ErrorCode.CANCEL);
            frame.Length = length;
            return SendAsync(frame.Raw);
        }

        private Task SendGoAwayAsync()
        {
            var frame = new Http2Frame();
            frame.PrepareGoAway(0, Http2ErrorCode.NO_ERROR);
            return SendAsync(frame.Raw);
        }

        private Task SendInvalidGoAwayFrameAsync()
        {
            var frame = new Http2Frame();
            frame.PrepareGoAway(0, Http2ErrorCode.NO_ERROR);
            frame.StreamId = 1;
            return SendAsync(frame.Raw);
        }

        private Task SendWindowUpdateAsync(int streamId, int sizeIncrement)
        {
            var frame = new Http2Frame();
            frame.PrepareWindowUpdate(streamId, sizeIncrement);
            return SendAsync(frame.Raw);
        }

        private Task SendInvalidWindowUpdateAsync(int streamId, int sizeIncrement, int length)
        {
            var frame = new Http2Frame();
            frame.PrepareWindowUpdate(streamId, sizeIncrement);
            frame.Length = length;
            return SendAsync(frame.Raw);
        }

        private Task SendUnknownFrameTypeAsync(int streamId, int frameType)
        {
            var frame = new Http2Frame();
            frame.StreamId = streamId;
            frame.Type = (Http2FrameType)frameType;
            frame.Length = 0;
            return SendAsync(frame.Raw);
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

                    if (Http2FrameReader.ReadFrame(buffer, frame, out consumed, out examined))
                    {
                        return frame;
                    }
                }
                finally
                {
                    _pair.Application.Input.Advance(consumed, examined);
                }
            }
        }

        private async Task ReceiveSettingsAck()
        {
            var frame = await ReceiveFrameAsync();

            Assert.Equal(Http2FrameType.SETTINGS, frame.Type);
            Assert.Equal(Http2SettingsFrameFlags.ACK, frame.SettingsFlags);
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

        private async Task WaitForStreamErrorAsync(bool ignoreNonRstStreamFrames, int expectedStreamId, Http2ErrorCode expectedErrorCode, string expectedErrorMessage)
        {
            var frame = await ReceiveFrameAsync();

            if (ignoreNonRstStreamFrames)
            {
                while (frame.Type != Http2FrameType.RST_STREAM)
                {
                    frame = await ReceiveFrameAsync();
                }
            }

            Assert.Equal(Http2FrameType.RST_STREAM, frame.Type);
            Assert.Equal(4, frame.Length);
            Assert.Equal(0, frame.Flags);
            Assert.Equal(expectedStreamId, frame.StreamId);
            Assert.Equal(expectedErrorCode, frame.RstStreamErrorCode);

            if (expectedErrorMessage != null)
            {
                var message = Assert.Single(_logger.Messages, m => m.Exception is Http2StreamErrorException);
                Assert.Contains(expectedErrorMessage, message.Exception.Message);
            }
        }

        private void VerifyDecodedRequestHeaders(IEnumerable<KeyValuePair<string, string>> expectedHeaders)
        {
            foreach (var header in expectedHeaders)
            {
                Assert.True(_receivedHeaders.TryGetValue(header.Key, out var value), header.Key);
                Assert.Equal(header.Value, value, ignoreCase: true);
            }
        }

        public static TheoryData<byte[]> UpperCaseHeaderNameData
        {
            get
            {
                // We can't use HPackEncoder here because it will convert header names to lowercase
                var headerName = "abcdefghijklmnopqrstuvwxyz";

                var headerBlockStart = new byte[]
                {
                    0x82,                    // Indexed Header Field - :method: GET
                    0x84,                    // Indexed Header Field - :path: /
                    0x86,                    // Indexed Header Field - :scheme: http
                    0x00,                    // Literal Header Field without Indexing - New Name
                    (byte)headerName.Length, // Header name length
                };

                var headerBlockEnd = new byte[]
                {
                    0x01, // Header value length
                    0x30  // "0"
                };

                var data = new TheoryData<byte[]>();

                for (var i = 0; i < headerName.Length; i++)
                {
                    var bytes = Encoding.ASCII.GetBytes(headerName);
                    bytes[i] &= 0xdf;

                    var headerBlock = headerBlockStart.Concat(bytes).Concat(headerBlockEnd).ToArray();
                    data.Add(headerBlock);
                }

                return data;
            }
        }

        public static TheoryData<IEnumerable<KeyValuePair<string, string>>> DuplicatePseudoHeaderFieldData
        {
            get
            {
                var data = new TheoryData<IEnumerable<KeyValuePair<string, string>>>();
                var requestHeaders = new[]
                {
                    new KeyValuePair<string, string>(":method", "GET"),
                    new KeyValuePair<string, string>(":path", "/"),
                    new KeyValuePair<string, string>(":authority", "127.0.0.1"),
                    new KeyValuePair<string, string>(":scheme", "http"),
                };

                foreach (var headerField in requestHeaders)
                {
                    var headers = requestHeaders.Concat(new[] { new KeyValuePair<string, string>(headerField.Key, headerField.Value) });
                    data.Add(headers);
                }

                return data;
            }
        }

        public static TheoryData<IEnumerable<KeyValuePair<string, string>>> MissingPseudoHeaderFieldData
        {
            get
            {
                var data = new TheoryData<IEnumerable<KeyValuePair<string, string>>>();
                var requestHeaders = new[]
                {
                    new KeyValuePair<string, string>(":method", "GET"),
                    new KeyValuePair<string, string>(":path", "/"),
                    new KeyValuePair<string, string>(":scheme", "http"),
                };

                foreach (var headerField in requestHeaders)
                {
                    var headers = requestHeaders.Except(new[] { headerField });
                    data.Add(headers);
                }

                return data;
            }
        }

        public static TheoryData<IEnumerable<KeyValuePair<string, string>>> ConnectMissingPseudoHeaderFieldData
        {
            get
            {
                var data = new TheoryData<IEnumerable<KeyValuePair<string, string>>>();
                var methodHeader = new[] { new KeyValuePair<string, string>(":method", "CONNECT") };
                var requestHeaders = new[]
                {
                    new KeyValuePair<string, string>(":path", "/"),
                    new KeyValuePair<string, string>(":scheme", "http"),
                    new KeyValuePair<string, string>(":authority", "127.0.0.1"),
                };

                foreach (var headerField in requestHeaders)
                {
                    var headers = methodHeader.Concat(requestHeaders.Except(new[] { headerField }));
                    data.Add(headers);
                }

                return data;
            }
        }

        public static TheoryData<IEnumerable<KeyValuePair<string, string>>> PseudoHeaderFieldAfterRegularHeadersData
        {
            get
            {
                var data = new TheoryData<IEnumerable<KeyValuePair<string, string>>>();
                var requestHeaders = new[]
                {
                    new KeyValuePair<string, string>(":method", "GET"),
                    new KeyValuePair<string, string>(":path", "/"),
                    new KeyValuePair<string, string>(":authority", "127.0.0.1"),
                    new KeyValuePair<string, string>(":scheme", "http"),
                    new KeyValuePair<string, string>("content-length", "0")
                };

                foreach (var headerField in requestHeaders.Where(h => h.Key.StartsWith(":")))
                {
                    var headers = requestHeaders.Except(new[] { headerField }).Concat(new[] { headerField });
                    data.Add(headers);
                }

                return data;
            }
        }

        public static TheoryData<byte[], string> IllegalTrailerData
        {
            get
            {
                // We can't use HPackEncoder here because it will convert header names to lowercase
                var data = new TheoryData<byte[], string>();

                // Indexed Header Field - :method: GET
                data.Add(new byte[] { 0x82 }, CoreStrings.Http2ErrorTrailersContainPseudoHeaderField);

                // Indexed Header Field - :path: /
                data.Add(new byte[] { 0x84 }, CoreStrings.Http2ErrorTrailersContainPseudoHeaderField);

                // Indexed Header Field - :scheme: http
                data.Add(new byte[] { 0x86 }, CoreStrings.Http2ErrorTrailersContainPseudoHeaderField);

                // Literal Header Field without Indexing - Indexed Name - :authority: 127.0.0.1
                data.Add(new byte[] { 0x01, 0x09 }.Concat(Encoding.ASCII.GetBytes("127.0.0.1")).ToArray(), CoreStrings.Http2ErrorTrailersContainPseudoHeaderField);

                // Literal Header Field without Indexing - New Name - contains-Uppercase: 0
                data.Add(new byte[] { 0x00, 0x12 }
                    .Concat(Encoding.ASCII.GetBytes("contains-Uppercase"))
                    .Concat(new byte[] { 0x01, (byte)'0' })
                    .ToArray(), CoreStrings.Http2ErrorTrailerNameUppercase);

                return data;
            }
        }
    }
}
