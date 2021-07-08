// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Net.Http.QPack;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;
using Xunit.Abstractions;
using static System.IO.Pipelines.DuplexPipe;
using static Microsoft.AspNetCore.Server.Kestrel.Core.Tests.Http2TestBase;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public abstract class Http3TestBase : TestApplicationErrorLoggerLoggedTest, IDisposable
    {
        protected static readonly int MaxRequestHeaderFieldSize = 16 * 1024;
        protected static readonly string _4kHeaderValue = new string('a', 4096);
        protected static readonly byte[] _helloWorldBytes = Encoding.ASCII.GetBytes("hello, world");
        protected static readonly byte[] _maxData = Encoding.ASCII.GetBytes(new string('a', 16 * 1024));

        internal TestServiceContext _serviceContext;
        internal HttpConnection _httpConnection;
        internal readonly TimeoutControl _timeoutControl;
        internal readonly Mock<IKestrelTrace> _mockKestrelTrace = new Mock<IKestrelTrace>();
        internal readonly Mock<ITimeoutHandler> _mockTimeoutHandler = new Mock<ITimeoutHandler>();
        internal readonly Mock<MockTimeoutControlBase> _mockTimeoutControl;
        internal readonly MemoryPool<byte> _memoryPool = PinnedBlockMemoryPoolFactory.Create();
        internal readonly ConcurrentQueue<TestStreamContext> _streamContextPool = new ConcurrentQueue<TestStreamContext>();
        protected Task _connectionTask;
        protected readonly TaskCompletionSource _closedStateReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        internal readonly ConcurrentDictionary<long, Http3StreamBase> _runningStreams = new ConcurrentDictionary<long, Http3StreamBase>();
        internal readonly Channel<KeyValuePair<Internal.Http3.Http3SettingType, long>> _serverReceivedSettings;
        protected readonly RequestDelegate _noopApplication;
        protected readonly RequestDelegate _echoApplication;
        protected readonly RequestDelegate _readRateApplication;
        protected readonly RequestDelegate _echoMethod;
        protected readonly RequestDelegate _echoPath;
        protected readonly RequestDelegate _echoHost;

        internal Func<TestStreamContext, Http3ControlStream> OnCreateServerControlStream;
        private Http3ControlStream _inboundControlStream;
        private long _currentStreamId;

        protected static readonly IEnumerable<KeyValuePair<string, string>> _browserRequestHeaders = new[]
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

        protected static IEnumerable<KeyValuePair<string, string>> ReadRateRequestHeaders(int expectedBytes) => new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/" + expectedBytes),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
        };

        public Http3TestBase()
        {
            _timeoutControl = new TimeoutControl(_mockTimeoutHandler.Object);
            _mockTimeoutControl = new Mock<MockTimeoutControlBase>(_timeoutControl) { CallBase = true };
            _timeoutControl.Debugger = Mock.Of<IDebugger>();

            _mockKestrelTrace
                .Setup(m => m.Http3ConnectionClosed(It.IsAny<string>(), It.IsAny<long>()))
                .Callback(() => _closedStateReached.SetResult());

            _serverReceivedSettings = Channel.CreateUnbounded<KeyValuePair<Internal.Http3.Http3SettingType, long>>();

            _noopApplication = context => Task.CompletedTask;

            _echoApplication = async context =>
            {
                var buffer = new byte[16 * 1024];
                var received = 0;

                while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer, 0, received);
                }
            };

            _readRateApplication = async context =>
            {
                var expectedBytes = int.Parse(context.Request.Path.Value.Substring(1), CultureInfo.InvariantCulture);

                var buffer = new byte[16 * 1024];
                var received = 0;

                while (received < expectedBytes)
                {
                    received += await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
                }

                var stalledReadTask = context.Request.Body.ReadAsync(buffer, 0, buffer.Length);

                // Write to the response so the test knows the app started the stalled read.
                await context.Response.Body.WriteAsync(new byte[1], 0, 1);

                await stalledReadTask;
            };

            _echoMethod = context =>
            {
                context.Response.Headers["Method"] = context.Request.Method;

                return Task.CompletedTask;
            };

            _echoPath = context =>
            {
                context.Response.Headers["path"] = context.Request.Path.ToString();
                context.Response.Headers["rawtarget"] = context.Features.Get<IHttpRequestFeature>().RawTarget;

                return Task.CompletedTask;
            };

            _echoHost = context =>
            {
                context.Response.Headers.Host = context.Request.Headers.Host;

                return Task.CompletedTask;
            };
        }

        internal Http3Connection Connection { get; private set; }

        internal Http3ControlStream OutboundControlStream { get; private set; }

        internal ChannelReader<KeyValuePair<Internal.Http3.Http3SettingType, long>> ServerReceivedSettingsReader => _serverReceivedSettings.Reader;

        internal TestMultiplexedConnectionContext MultiplexedConnectionContext { get; set; }

        public override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

            _serviceContext = new TestServiceContext(LoggerFactory, _mockKestrelTrace.Object)
            {
                Scheduler = PipeScheduler.Inline,
            };
        }

        internal long GetStreamId(long mask)
        {
            var id = (_currentStreamId << 2) | mask;

            _currentStreamId += 1;

            return id;
        }

        internal async ValueTask<Http3ControlStream> GetInboundControlStream()
        {
            if (_inboundControlStream == null)
            {
                var reader = MultiplexedConnectionContext.ToClientAcceptQueue.Reader;
                while (await reader.WaitToReadAsync().DefaultTimeout())
                {
                    while (reader.TryRead(out var stream))
                    {
                        _inboundControlStream = stream;
                        var streamId = await stream.TryReadStreamIdAsync();

                        // -1 means stream was completed.
                        Debug.Assert(streamId == 0 || streamId == -1, "StreamId sent that was non-zero, which isn't handled by tests");

                        return _inboundControlStream;
                    }
                }
            }

            return _inboundControlStream;
        }

        internal void CloseConnectionGracefully()
        {
            MultiplexedConnectionContext.ConnectionClosingCts.Cancel();
        }

        internal Task WaitForConnectionStopAsync(long expectedLastStreamId, bool ignoreNonGoAwayFrames, Http3ErrorCode? expectedErrorCode = null)
        {
            return WaitForConnectionErrorAsync<Exception>(ignoreNonGoAwayFrames, expectedLastStreamId, expectedErrorCode: expectedErrorCode ?? 0, expectedErrorMessage: null);
        }

        internal async Task WaitForConnectionErrorAsync<TException>(bool ignoreNonGoAwayFrames, long? expectedLastStreamId, Http3ErrorCode expectedErrorCode, params string[] expectedErrorMessage)
            where TException : Exception
        {
            var frame = await _inboundControlStream.ReceiveFrameAsync();

            if (ignoreNonGoAwayFrames)
            {
                while (frame.Type != Http3FrameType.GoAway)
                {
                    frame = await _inboundControlStream.ReceiveFrameAsync();
                }
            }

            if (expectedLastStreamId != null)
            {
                VerifyGoAway(frame, expectedLastStreamId.GetValueOrDefault());
            }

            AssertConnectionError<TException>(expectedErrorCode, expectedErrorMessage);

            // Verify HttpConnection.ProcessRequestsAsync has exited.
            await _connectionTask.DefaultTimeout();

            // Verify server-to-client control stream has completed.
            await _inboundControlStream.ReceiveEndAsync();
        }

        internal void AssertConnectionError<TException>(Http3ErrorCode expectedErrorCode, params string[] expectedErrorMessage) where TException : Exception
        {
            Assert.Equal((Http3ErrorCode)expectedErrorCode, (Http3ErrorCode)MultiplexedConnectionContext.Error);

            if (expectedErrorMessage?.Length > 0)
            {
                var message = Assert.Single(LogMessages, m => m.Exception is TException);

                Assert.Contains(expectedErrorMessage, expected => message.Exception.Message.Contains(expected));
            }
        }

        internal void VerifyGoAway(Http3FrameWithPayload frame, long expectedLastStreamId)
        {
            Assert.Equal(Http3FrameType.GoAway, frame.Type);
            var payload = frame.Payload;
            Assert.True(VariableLengthIntegerHelper.TryRead(payload.Span, out var streamId, out var _));
            Assert.Equal(expectedLastStreamId, streamId);
        }

        protected void AdvanceClock(TimeSpan timeSpan)
        {
            var clock = _serviceContext.MockSystemClock;
            var endTime = clock.UtcNow + timeSpan;

            while (clock.UtcNow + Heartbeat.Interval < endTime)
            {
                clock.UtcNow += Heartbeat.Interval;
                _timeoutControl.Tick(clock.UtcNow);
            }

            clock.UtcNow = endTime;
            _timeoutControl.Tick(clock.UtcNow);
        }

        protected void TriggerTick(DateTimeOffset now)
        {
            _serviceContext.MockSystemClock.UtcNow = now;
            Connection?.Tick(now);
        }

        protected async Task InitializeConnectionAsync(RequestDelegate application)
        {
            MultiplexedConnectionContext = new TestMultiplexedConnectionContext(this);

            var httpConnectionContext = new HttpMultiplexedConnectionContext(
                connectionId: "TestConnectionId",
                connectionContext: MultiplexedConnectionContext,
                connectionFeatures: MultiplexedConnectionContext.Features,
                serviceContext: _serviceContext,
                memoryPool: _memoryPool,
                localEndPoint: null,
                remoteEndPoint: null);
            httpConnectionContext.TimeoutControl = _mockTimeoutControl.Object;

            _httpConnection = new HttpConnection(httpConnectionContext);
            _httpConnection.Initialize(Connection);
            _mockTimeoutHandler.Setup(h => h.OnTimeout(It.IsAny<TimeoutReason>()))
                               .Callback<TimeoutReason>(r => _httpConnection.OnTimeout(r));

            // ProcessRequestAsync will create the Http3Connection
            _connectionTask = _httpConnection.ProcessRequestsAsync(new DummyApplication(application));

            Connection = (Http3Connection)_httpConnection._requestProcessor;
            Connection._streamLifetimeHandler = new LifetimeHandlerInterceptor(Connection, this);

            await GetInboundControlStream();
        }

        internal async ValueTask<Http3RequestStream> InitializeConnectionAndStreamsAsync(RequestDelegate application)
        {
            await InitializeConnectionAsync(application);

            OutboundControlStream = await CreateControlStream();

            return await CreateRequestStream();
        }

        private class LifetimeHandlerInterceptor : IHttp3StreamLifetimeHandler
        {
            private readonly IHttp3StreamLifetimeHandler _inner;
            private readonly Http3TestBase _http3TestBase;

            public LifetimeHandlerInterceptor(IHttp3StreamLifetimeHandler inner, Http3TestBase http3TestBase)
            {
                _inner = inner;
                _http3TestBase = http3TestBase;
            }

            public bool OnInboundControlStream(Internal.Http3.Http3ControlStream stream)
            {
                return _inner.OnInboundControlStream(stream);
            }

            public void OnInboundControlStreamSetting(Internal.Http3.Http3SettingType type, long value)
            {
                _inner.OnInboundControlStreamSetting(type, value);

                var success = _http3TestBase._serverReceivedSettings.Writer.TryWrite(
                    new KeyValuePair<Internal.Http3.Http3SettingType, long>(type, value));
                Debug.Assert(success);
            }

            public bool OnInboundDecoderStream(Internal.Http3.Http3ControlStream stream)
            {
                return _inner.OnInboundDecoderStream(stream);
            }

            public bool OnInboundEncoderStream(Internal.Http3.Http3ControlStream stream)
            {
                return _inner.OnInboundEncoderStream(stream);
            }

            public void OnStreamCompleted(IHttp3Stream stream)
            {
                _inner.OnStreamCompleted(stream);

                if (_http3TestBase._runningStreams.TryGetValue(stream.StreamId, out var testStream))
                {
                    testStream._onStreamCompletedTcs.TrySetResult();
                }
            }

            public void OnStreamConnectionError(Http3ConnectionErrorException ex)
            {
                _inner.OnStreamConnectionError(ex);
            }

            public void OnStreamCreated(IHttp3Stream stream)
            {
                _inner.OnStreamCreated(stream);

                if (_http3TestBase._runningStreams.TryGetValue(stream.StreamId, out var testStream))
                {
                    testStream._onStreamCreatedTcs.TrySetResult();
                }
            }

            public void OnStreamHeaderReceived(IHttp3Stream stream)
            {
                _inner.OnStreamHeaderReceived(stream);

                if (_http3TestBase._runningStreams.TryGetValue(stream.StreamId, out var testStream))
                {
                    testStream._onHeaderReceivedTcs.TrySetResult();
                }
            }
        }

        protected void ConnectionClosed()
        {

        }

        private static PipeOptions GetInputPipeOptions(ServiceContext serviceContext, MemoryPool<byte> memoryPool, PipeScheduler writerScheduler) => new PipeOptions
        (
          pool: memoryPool,
          readerScheduler: serviceContext.Scheduler,
          writerScheduler: writerScheduler,
          pauseWriterThreshold: serviceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
          resumeWriterThreshold: serviceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
          useSynchronizationContext: false,
          minimumSegmentSize: memoryPool.GetMinimumSegmentSize()
        );

        private static PipeOptions GetOutputPipeOptions(ServiceContext serviceContext, MemoryPool<byte> memoryPool, PipeScheduler readerScheduler) => new PipeOptions
        (
            pool: memoryPool,
            readerScheduler: readerScheduler,
            writerScheduler: serviceContext.Scheduler,
            pauseWriterThreshold: GetOutputResponseBufferSize(serviceContext),
            resumeWriterThreshold: GetOutputResponseBufferSize(serviceContext),
            useSynchronizationContext: false,
            minimumSegmentSize: memoryPool.GetMinimumSegmentSize()
        );

        private static long GetOutputResponseBufferSize(ServiceContext serviceContext)
        {
            var bufferSize = serviceContext.ServerOptions.Limits.MaxResponseBufferSize;
            if (bufferSize == 0)
            {
                // 0 = no buffering so we need to configure the pipe so the writer waits on the reader directly
                return 1;
            }

            // null means that we have no back pressure
            return bufferSize ?? 0;
        }

        internal ValueTask<Http3ControlStream> CreateControlStream()
        {
            return CreateControlStream(id: 0);
        }

        internal async ValueTask<Http3ControlStream> CreateControlStream(int? id)
        {
            var testStreamContext = new TestStreamContext(canRead: true, canWrite: false, this);
            testStreamContext.Initialize(GetStreamId(0x02));

            var stream = new Http3ControlStream(this, testStreamContext);
            _runningStreams[stream.StreamId] = stream;

            MultiplexedConnectionContext.ToServerAcceptQueue.Writer.TryWrite(stream.StreamContext);
            if (id != null)
            {
                await stream.WriteStreamIdAsync(id.GetValueOrDefault());
            }
            return stream;
        }

        internal ValueTask<Http3RequestStream> CreateRequestStream()
        {
            if (!_streamContextPool.TryDequeue(out var testStreamContext))
            {
                testStreamContext = new TestStreamContext(canRead: true, canWrite: true, this);
            }
            testStreamContext.Initialize(GetStreamId(0x00));

            var stream = new Http3RequestStream(this, Connection, testStreamContext);
            _runningStreams[stream.StreamId] = stream;

            MultiplexedConnectionContext.ToServerAcceptQueue.Writer.TryWrite(stream.StreamContext);
            return new ValueTask<Http3RequestStream>(stream);
        }

        internal class Http3StreamBase : IProtocolErrorCodeFeature
        {
            internal TaskCompletionSource _onStreamCreatedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            internal TaskCompletionSource _onStreamCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            internal TaskCompletionSource _onHeaderReceivedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            internal ConnectionContext StreamContext { get; }
            internal IProtocolErrorCodeFeature _protocolErrorCodeFeature;
            internal DuplexPipe.DuplexPipePair _pair;
            internal Http3TestBase _testBase;
            internal Http3Connection _connection;
            public long BytesReceived { get; private set; }
            public long Error
            {
                get => _protocolErrorCodeFeature.Error;
                set => _protocolErrorCodeFeature.Error = value;
            }

            public Task OnStreamCreatedTask => _onStreamCreatedTcs.Task;
            public Task OnStreamCompletedTask => _onStreamCompletedTcs.Task;
            public Task OnHeaderReceivedTask => _onHeaderReceivedTcs.Task;

            public Http3StreamBase(TestStreamContext testStreamContext)
            {
                StreamContext = testStreamContext;
                _protocolErrorCodeFeature = testStreamContext;
                _pair = testStreamContext._pair;
            }

            protected Task SendAsync(ReadOnlySpan<byte> span)
            {
                var writableBuffer = _pair.Application.Output;
                writableBuffer.Write(span);
                return FlushAsync(writableBuffer);
            }

            protected static async Task FlushAsync(PipeWriter writableBuffer)
            {
                await writableBuffer.FlushAsync().AsTask().DefaultTimeout();
            }

            internal async Task ReceiveEndAsync()
            {
                var result = await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout();
                Assert.True(result.IsCompleted);
            }

            internal async Task<Http3FrameWithPayload> ReceiveFrameAsync()
            {
                var frame = new Http3FrameWithPayload();

                while (true)
                {
                    var result = await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout();
                    var buffer = result.Buffer;
                    var consumed = buffer.Start;
                    var examined = buffer.Start;
                    var copyBuffer = buffer;

                    try
                    {
                        Assert.True(buffer.Length > 0);

                        if (Http3FrameReader.TryReadFrame(ref buffer, frame, out var framePayload))
                        {
                            consumed = examined = framePayload.End;
                            frame.Payload = framePayload.ToArray();
                            return frame;
                        }
                        else
                        {
                            examined = buffer.End;
                        }

                        if (result.IsCompleted)
                        {
                            throw new IOException("The reader completed without returning a frame.");
                        }
                    }
                    finally
                    {
                        BytesReceived += copyBuffer.Slice(copyBuffer.Start, consumed).Length;
                        _pair.Application.Input.AdvanceTo(consumed, examined);
                    }
                }
            }

            internal async Task SendFrameAsync(Http3RawFrame frame, Memory<byte> data, bool endStream = false)
            {
                var outputWriter = _pair.Application.Output;
                frame.Length = data.Length;
                Http3FrameWriter.WriteHeader(frame, outputWriter);

                if (!endStream)
                {
                    await SendAsync(data.Span);
                }
                else
                {
                    // Write and end stream at the same time.
                    // Avoid race condition of frame read separately from end of stream.
                    await EndStreamAsync(data.Span);
                }
            }

            internal Task EndStreamAsync(ReadOnlySpan<byte> span = default)
            {
                var writableBuffer = _pair.Application.Output;
                if (span.Length > 0)
                {
                    writableBuffer.Write(span);
                }
                return writableBuffer.CompleteAsync().AsTask();
            }

            internal async Task WaitForStreamErrorAsync(Http3ErrorCode protocolError, string expectedErrorMessage)
            {
                var readResult = await _pair.Application.Input.ReadAsync().DefaultTimeout();
                _testBase.Logger.LogTrace("Input is completed");

                Assert.True(readResult.IsCompleted);
                Assert.Equal(protocolError, (Http3ErrorCode)Error);

                if (expectedErrorMessage != null)
                {
                    Assert.Contains(_testBase.LogMessages, m => m.Exception?.Message.Contains(expectedErrorMessage) ?? false);
                }
            }
        }

        internal class Http3RequestStream : Http3StreamBase, IHttpHeadersHandler
        {
            private readonly TestStreamContext _testStreamContext;
            private readonly long _streamId;

            public bool CanRead => true;
            public bool CanWrite => true;

            public long StreamId => _streamId;

            public bool Disposed => _testStreamContext.Disposed;

            private readonly byte[] _headerEncodingBuffer = new byte[64 * 1024];
            private readonly QPackDecoder _qpackDecoder = new QPackDecoder(8192);
            protected readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public Http3RequestStream(Http3TestBase testBase, Http3Connection connection, TestStreamContext testStreamContext)
                : base(testStreamContext)
            {
                _testBase = testBase;
                _connection = connection;
                _streamId = testStreamContext.StreamId;
                _testStreamContext = testStreamContext;
            }

            public async Task SendHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers, bool endStream = false)
            {
                var headersTotalSize = 0;

                var frame = new Http3RawFrame();
                frame.PrepareHeaders();
                var buffer = _headerEncodingBuffer.AsMemory();
                var done = QPackHeaderWriter.BeginEncode(headers.GetEnumerator(), buffer.Span, ref headersTotalSize, out var length);
                Assert.True(done);

                await SendFrameAsync(frame, buffer.Slice(0, length), endStream);
            }

            internal async Task SendHeadersPartialAsync()
            {
                // Send HEADERS frame header without content.
                var outputWriter = _pair.Application.Output;
                var frame = new Http3RawFrame();
                frame.PrepareData();
                frame.Length = 10;
                Http3FrameWriter.WriteHeader(frame, outputWriter);
                await SendAsync(Span<byte>.Empty);
            }

            internal async Task SendDataAsync(Memory<byte> data, bool endStream = false)
            {
                var frame = new Http3RawFrame();
                frame.PrepareData();
                await SendFrameAsync(frame, data, endStream);
            }

            internal async Task<Dictionary<string, string>> ExpectHeadersAsync()
            {
                var http3WithPayload = await ReceiveFrameAsync();
                Assert.Equal(Http3FrameType.Headers, http3WithPayload.Type);

                _decodedHeaders.Clear();
                _qpackDecoder.Decode(http3WithPayload.PayloadSequence, this);
                _qpackDecoder.Reset();
                return _decodedHeaders.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, _decodedHeaders.Comparer);
            }

            internal async Task<Memory<byte>> ExpectDataAsync()
            {
                var http3WithPayload = await ReceiveFrameAsync();
                return http3WithPayload.Payload;
            }

            internal async Task ExpectReceiveEndOfStream()
            {
                var result = await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout();
                Assert.True(result.IsCompleted);
            }

            public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            {
                _decodedHeaders[name.GetAsciiStringNonNullCharacters()] = value.GetAsciiOrUTF8StringNonNullCharacters();
            }

            public void OnHeadersComplete(bool endHeaders)
            {
            }

            public void OnStaticIndexedHeader(int index)
            {
                var knownHeader = H3StaticTable.GetHeaderFieldAt(index);
                _decodedHeaders[((Span<byte>)knownHeader.Name).GetAsciiStringNonNullCharacters()] = HttpUtilities.GetAsciiOrUTF8StringNonNullCharacters((ReadOnlySpan<byte>)knownHeader.Value);
            }

            public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
            {
                _decodedHeaders[((Span<byte>)H3StaticTable.GetHeaderFieldAt(index).Name).GetAsciiStringNonNullCharacters()] = value.GetAsciiOrUTF8StringNonNullCharacters();
            }
        }

        internal class Http3FrameWithPayload : Http3RawFrame
        {
            public Http3FrameWithPayload() : base()
            {
            }

            // This does not contain extended headers
            public Memory<byte> Payload { get; set; }

            public ReadOnlySequence<byte> PayloadSequence => new ReadOnlySequence<byte>(Payload);
        }

        public enum StreamInitiator
        {
            Client,
            Server
        }

        internal class Http3ControlStream : Http3StreamBase
        {
            private readonly long _streamId;

            public bool CanRead => true;
            public bool CanWrite => false;

            public long StreamId => _streamId;

            public Http3ControlStream(Http3TestBase testBase, TestStreamContext testStreamContext)
                : base(testStreamContext)
            {
                _testBase = testBase;
                _streamId = testStreamContext.StreamId;
            }

            internal async Task<Dictionary<long, long>> ExpectSettingsAsync()
            {
                var http3WithPayload = await ReceiveFrameAsync();
                Assert.Equal(Http3FrameType.Settings, http3WithPayload.Type);

                var payload = http3WithPayload.PayloadSequence;

                var settings = new Dictionary<long, long>();
                while (true)
                {
                    var id = VariableLengthIntegerHelper.GetInteger(payload, out var consumed, out _);
                    if (id == -1)
                    {
                        break;
                    }

                    payload = payload.Slice(consumed);

                    var value = VariableLengthIntegerHelper.GetInteger(payload, out consumed, out _);
                    if (value == -1)
                    {
                        break;
                    }

                    payload = payload.Slice(consumed);
                    settings.Add(id, value);
                }

                return settings;
            }

            public async Task WriteStreamIdAsync(int id)
            {
                var writableBuffer = _pair.Application.Output;

                void WriteSpan(PipeWriter pw)
                {
                    var buffer = pw.GetSpan(sizeHint: 8);
                    var lengthWritten = VariableLengthIntegerHelper.WriteInteger(buffer, id);
                    pw.Advance(lengthWritten);
                }

                WriteSpan(writableBuffer);

                await FlushAsync(writableBuffer);
            }

            internal async Task SendGoAwayAsync(long streamId, bool endStream = false)
            {
                var frame = new Http3RawFrame();
                frame.PrepareGoAway();

                var data = new byte[VariableLengthIntegerHelper.GetByteCount(streamId)];
                VariableLengthIntegerHelper.WriteInteger(data, streamId);

                await SendFrameAsync(frame, data, endStream);
            }

            internal async Task SendSettingsAsync(List<Http3PeerSetting> settings, bool endStream = false)
            {
                var frame = new Http3RawFrame();
                frame.PrepareSettings();

                var settingsLength = CalculateSettingsSize(settings);
                var buffer = new byte[settingsLength];
                WriteSettings(settings, buffer);

                await SendFrameAsync(frame, buffer, endStream);
            }

            internal static int CalculateSettingsSize(List<Http3PeerSetting> settings)
            {
                var length = 0;
                foreach (var setting in settings)
                {
                    length += VariableLengthIntegerHelper.GetByteCount((long)setting.Parameter);
                    length += VariableLengthIntegerHelper.GetByteCount(setting.Value);
                }
                return length;
            }

            internal static void WriteSettings(List<Http3PeerSetting> settings, Span<byte> destination)
            {
                foreach (var setting in settings)
                {
                    var parameterLength = VariableLengthIntegerHelper.WriteInteger(destination, (long)setting.Parameter);
                    destination = destination.Slice(parameterLength);

                    var valueLength = VariableLengthIntegerHelper.WriteInteger(destination, (long)setting.Value);
                    destination = destination.Slice(valueLength);
                }
            }

            public async ValueTask<long> TryReadStreamIdAsync()
            {
                while (true)
                {
                    var result = await _pair.Application.Input.ReadAsync();
                    var readableBuffer = result.Buffer;
                    var consumed = readableBuffer.Start;
                    var examined = readableBuffer.End;

                    try
                    {
                        if (!readableBuffer.IsEmpty)
                        {
                            var id = VariableLengthIntegerHelper.GetInteger(readableBuffer, out consumed, out examined);
                            if (id != -1)
                            {
                                return id;
                            }
                        }

                        if (result.IsCompleted)
                        {
                            return -1;
                        }
                    }
                    finally
                    {
                        _pair.Application.Input.AdvanceTo(consumed, examined);
                    }
                }
            }
        }

        internal class TestMultiplexedConnectionContext : MultiplexedConnectionContext, IConnectionLifetimeNotificationFeature, IConnectionLifetimeFeature, IConnectionHeartbeatFeature, IProtocolErrorCodeFeature
        {
            public readonly Channel<ConnectionContext> ToServerAcceptQueue = Channel.CreateUnbounded<ConnectionContext>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

            public readonly Channel<Http3ControlStream> ToClientAcceptQueue = Channel.CreateUnbounded<Http3ControlStream>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

            private readonly Http3TestBase _testBase;
            private long _error;

            public TestMultiplexedConnectionContext(Http3TestBase testBase)
            {
                _testBase = testBase;
                Features = new FeatureCollection();
                Features.Set<IConnectionLifetimeNotificationFeature>(this);
                Features.Set<IConnectionHeartbeatFeature>(this);
                Features.Set<IProtocolErrorCodeFeature>(this);
                ConnectionClosedRequested = ConnectionClosingCts.Token;
            }

            public override string ConnectionId { get; set; }

            public override IFeatureCollection Features { get; }

            public override IDictionary<object, object> Items { get; set; }

            public CancellationToken ConnectionClosedRequested { get; set; }

            public CancellationTokenSource ConnectionClosingCts { get; set; } = new CancellationTokenSource();

            public long Error
            {
                get => _error;
                set => _error = value;
            }

            public override void Abort()
            {
                Abort(new ConnectionAbortedException());
            }

            public override void Abort(ConnectionAbortedException abortReason)
            {
                ToServerAcceptQueue.Writer.TryComplete();
                ToClientAcceptQueue.Writer.TryComplete();
            }

            public override async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
            {
                while (await ToServerAcceptQueue.Reader.WaitToReadAsync())
                {
                    while (ToServerAcceptQueue.Reader.TryRead(out var connection))
                    {
                        return connection;
                    }
                }

                return null;
            }

            public override ValueTask<ConnectionContext> ConnectAsync(IFeatureCollection features = null, CancellationToken cancellationToken = default)
            {
                var testStreamContext = new TestStreamContext(canRead: true, canWrite: false, _testBase);
                testStreamContext.Initialize(_testBase.GetStreamId(0x03));

                var stream = _testBase.OnCreateServerControlStream?.Invoke(testStreamContext) ?? new Http3ControlStream(_testBase, testStreamContext);
                ToClientAcceptQueue.Writer.WriteAsync(stream);
                return new ValueTask<ConnectionContext>(stream.StreamContext);
            }

            public void OnHeartbeat(Action<object> action, object state)
            {
            }

            public void RequestClose()
            {
                throw new NotImplementedException();
            }
        }

        internal class TestStreamContext : ConnectionContext, IStreamDirectionFeature, IStreamIdFeature, IProtocolErrorCodeFeature
        {
            private readonly Http3TestBase _testBase;

            internal DuplexPipePair _pair;
            private Pipe _inputPipe;
            private Pipe _outputPipe;
            private CompletionPipeReader _transportPipeReader;
            private CompletionPipeWriter _transportPipeWriter;

            private bool _isAborted;

            public TestStreamContext(bool canRead, bool canWrite, Http3TestBase testBase)
            {
                Features = new FeatureCollection();
                CanRead = canRead;
                CanWrite = canWrite;
                _testBase = testBase;
            }

            public void Initialize(long streamId)
            {
                // Create new pipes when test stream context is reused rather than reseting them.
                // This is required because the client tests read from these directly from these pipes.
                // When a request is finished they'll check to see whether there is anymore content
                // in the Application.Output pipe. If it has been reset then that code will error.
                var inputOptions = GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var outputOptions = GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);

                _inputPipe = new Pipe(inputOptions);
                _outputPipe = new Pipe(outputOptions);

                _transportPipeReader = new CompletionPipeReader(_inputPipe.Reader);
                _transportPipeWriter = new CompletionPipeWriter(_outputPipe.Writer);

                _pair = new DuplexPipePair(
                    new DuplexPipe(_transportPipeReader, _transportPipeWriter),
                    new DuplexPipe(_outputPipe.Reader, _inputPipe.Writer));

                Features.Set<IStreamDirectionFeature>(this);
                Features.Set<IStreamIdFeature>(this);
                Features.Set<IProtocolErrorCodeFeature>(this);

                StreamId = streamId;

                Disposed = false;
            }

            public bool Disposed { get; private set; }

            public override string ConnectionId { get; set; }

            public long StreamId { get; private set; }

            public override IFeatureCollection Features { get; }

            public override IDictionary<object, object> Items { get; set; }

            public override IDuplexPipe Transport
            {
                get
                {
                    return _pair.Transport;
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public bool CanRead { get; }

            public bool CanWrite { get; }

            public long Error { get; set; }

            public override void Abort(ConnectionAbortedException abortReason)
            {
                _isAborted = true;
                _pair.Application.Output.Complete(abortReason);
            }

            public override ValueTask DisposeAsync()
            {
                Disposed = true;

                if (!_isAborted &&
                    _transportPipeReader.IsComplete && _transportPipeReader.CompleteException == null &&
                    _transportPipeWriter.IsComplete && _transportPipeWriter.CompleteException == null)
                {
                    _testBase._streamContextPool.Enqueue(this);
                }

                return ValueTask.CompletedTask;
            }
        }
    }
}
