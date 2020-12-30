// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Net.Http.QPack;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Experimental;
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
    public class Http3TestBase : TestApplicationErrorLoggerLoggedTest, IDisposable
    {
        internal TestServiceContext _serviceContext;
        internal readonly TimeoutControl _timeoutControl;
        internal readonly Mock<IKestrelTrace> _mockKestrelTrace = new Mock<IKestrelTrace>();
        internal readonly Mock<ITimeoutHandler> _mockTimeoutHandler = new Mock<ITimeoutHandler>();
        internal readonly Mock<MockTimeoutControlBase> _mockTimeoutControl;
        internal readonly MemoryPool<byte> _memoryPool = SlabMemoryPoolFactory.Create();
        protected Task _connectionTask;
        protected readonly TaskCompletionSource _closedStateReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);


        protected readonly RequestDelegate _noopApplication;
        protected readonly RequestDelegate _echoApplication;
        protected readonly RequestDelegate _echoMethod;
        protected readonly RequestDelegate _echoPath;
        protected readonly RequestDelegate _echoHost;

        private Http3ControlStream _inboundControlStream;

        public Http3TestBase()
        {
            _timeoutControl = new TimeoutControl(_mockTimeoutHandler.Object);
            _mockTimeoutControl = new Mock<MockTimeoutControlBase>(_timeoutControl) { CallBase = true };
            _timeoutControl.Debugger = Mock.Of<IDebugger>();

            _mockKestrelTrace
                .Setup(m => m.Http3ConnectionClosed(It.IsAny<string>(), It.IsAny<long>()))
                .Callback(() => _closedStateReached.SetResult());


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
                context.Response.Headers[HeaderNames.Host] = context.Request.Headers[HeaderNames.Host];

                return Task.CompletedTask;
            };
        }

        internal Http3Connection Connection { get; private set; }

        internal Http3ControlStream OutboundControlStream { get; private set; }

        public TestMultiplexedConnectionContext MultiplexedConnectionContext { get; set; }

        public override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

            _serviceContext = new TestServiceContext(LoggerFactory, _mockKestrelTrace.Object)
            {
                Scheduler = PipeScheduler.Inline,
            };
        }

        internal async ValueTask<Http3ControlStream> GetInboundControlStream()
        {
            if (_inboundControlStream == null)
            {
                var reader = MultiplexedConnectionContext.ToClientAcceptQueue.Reader;
                while (await reader.WaitToReadAsync())
                {
                    while (reader.TryRead(out var stream))
                    {
                        _inboundControlStream = stream;
                        var streamId = await stream.TryReadStreamIdAsync();
                        Debug.Assert(streamId == 0, "StreamId sent that was non-zero, which isn't handled by tests");
                        return _inboundControlStream;
                    }
                }
            }    
            
            return null;
        }

        internal async Task WaitForConnectionErrorAsync(bool ignoreNonGoAwayFrames, long expectedLastStreamId, Http3ErrorCode expectedErrorCode)
        {
            var frame = await _inboundControlStream.ReceiveFrameAsync();

            if (ignoreNonGoAwayFrames)
            {
                while (frame.Type != Http3FrameType.GoAway)
                {
                    frame = await _inboundControlStream.ReceiveFrameAsync();
                }
            }

            VerifyGoAway(frame, expectedLastStreamId, expectedErrorCode);
        }

        internal void VerifyGoAway(Http3FrameWithPayload frame, long expectedLastStreamId, Http3ErrorCode expectedErrorCode)
        {
            Assert.Equal(Http3FrameType.GoAway, frame.Type);
            var payload = frame.Payload;
            Assert.True(VariableLengthIntegerHelper.TryRead(payload.Span, out var streamId, out var _));
            Assert.Equal(expectedLastStreamId, streamId);
        }

        protected async Task InitializeConnectionAsync(RequestDelegate application)
        {
            if (Connection == null)
            {
                CreateConnection();
            }

            // Skip all heartbeat and lifetime notification feature registrations.
            _connectionTask = Connection.ProcessStreamsAsync(new DummyApplication(application));

            await Task.CompletedTask;
        }

        internal async ValueTask<Http3RequestStream> InitializeConnectionAndStreamsAsync(RequestDelegate application)
        {
            await InitializeConnectionAsync(application);

            OutboundControlStream = await CreateControlStream();

            return await CreateRequestStream();
        }

        protected void CreateConnection()
        {
            var limits = _serviceContext.ServerOptions.Limits;


            MultiplexedConnectionContext = new TestMultiplexedConnectionContext(this);

            var httpConnectionContext = new Http3ConnectionContext(
                connectionId: "TestConnectionId",
                connectionContext: MultiplexedConnectionContext,
                connectionFeatures: MultiplexedConnectionContext.Features,
                serviceContext: _serviceContext,
                memoryPool: _memoryPool,
                localEndPoint: null,
                remoteEndPoint: null);
            httpConnectionContext.TimeoutControl = _mockTimeoutControl.Object;

            Connection = new Http3Connection(httpConnectionContext);
            _mockTimeoutHandler.Setup(h => h.OnTimeout(It.IsAny<TimeoutReason>()))
                           .Callback<TimeoutReason>(r => Connection.OnTimeout(r));
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

        public ValueTask<Http3ControlStream> CreateControlStream()
        {
            return CreateControlStream(id: 0);
        }

        public async ValueTask<Http3ControlStream> CreateControlStream(int id)
        {
            var stream = new Http3ControlStream(this);
            MultiplexedConnectionContext.ToServerAcceptQueue.Writer.TryWrite(stream.StreamContext);
            await stream.WriteStreamIdAsync(id);
            return stream;
        }

        internal ValueTask<Http3RequestStream> CreateRequestStream()
        {
            var stream = new Http3RequestStream(this, Connection);
            MultiplexedConnectionContext.ToServerAcceptQueue.Writer.TryWrite(stream.StreamContext);
            return new ValueTask<Http3RequestStream>(stream);
        }

        public ValueTask<ConnectionContext> StartBidirectionalStreamAsync()
        {
            var stream = new Http3RequestStream(this, Connection);
            // TODO put these somewhere to be read.
            return new ValueTask<ConnectionContext>(stream.StreamContext);
        }

        public class Http3StreamBase
        {
            internal DuplexPipe.DuplexPipePair _pair;
            internal Http3TestBase _testBase;
            internal Http3Connection _connection;
            private long _bytesReceived;

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

            internal async Task<Http3FrameWithPayload> ReceiveFrameAsync(uint maxFrameSize = 16 * 1024)
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

                        if (Http3FrameReader.TryReadFrame(ref buffer, frame, maxFrameSize, out var framePayload))
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
                        _bytesReceived += copyBuffer.Slice(copyBuffer.Start, consumed).Length;
                        _pair.Application.Input.AdvanceTo(consumed, examined);
                    }
                }
            }
        }

        internal class Http3RequestStream : Http3StreamBase, IHttpHeadersHandler, IProtocolErrorCodeFeature
        {
            internal ConnectionContext StreamContext { get; }

            public bool CanRead => true;
            public bool CanWrite => true;

            public long StreamId => 0;

            public long Error { get; set; }

            private readonly byte[] _headerEncodingBuffer = new byte[16 * 1024];
            private QPackEncoder _qpackEncoder = new QPackEncoder();
            private QPackDecoder _qpackDecoder = new QPackDecoder(8192);
            protected readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public Http3RequestStream(Http3TestBase testBase, Http3Connection connection)
            {
                _testBase = testBase;
                _connection = connection;
                var inputPipeOptions = GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var outputPipeOptions = GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);

                _pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);

                StreamContext = new TestStreamContext(canRead: true, canWrite: true, _pair, this);
            }

            public async Task<bool> SendHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers, bool endStream = false)
            {
                var outputWriter = _pair.Application.Output;
                var frame = new Http3RawFrame();
                frame.PrepareHeaders();
                var buffer = _headerEncodingBuffer.AsMemory();
                var done = _qpackEncoder.BeginEncode(headers, buffer.Span, out var length);
                frame.Length = length;
                // TODO may want to modify behavior of input frames to mock different client behavior (client can send anything).
                Http3FrameWriter.WriteHeader(frame, outputWriter);
                await SendAsync(buffer.Span.Slice(0, length));

                if (endStream)
                {
                    await _pair.Application.Output.CompleteAsync();
                }

                return done;
            }

            internal async Task SendDataAsync(Memory<byte> data, bool endStream = false)
            {
                var outputWriter = _pair.Application.Output;
                var frame = new Http3RawFrame();
                frame.PrepareData();
                frame.Length = data.Length;
                Http3FrameWriter.WriteHeader(frame, outputWriter);
                await SendAsync(data.Span);

                if (endStream)
                {
                    await _pair.Application.Output.CompleteAsync();
                }
            }

            internal async Task<Dictionary<string, string>> ExpectHeadersAsync()
            {
                var http3WithPayload = await ReceiveFrameAsync();
                Assert.Equal(Http3FrameType.Headers, http3WithPayload.Type);

                _decodedHeaders.Clear();
                _qpackDecoder.Decode(http3WithPayload.PayloadSequence, this);
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

            internal async Task WaitForStreamErrorAsync(Http3ErrorCode protocolError, string expectedErrorMessage)
            {
                var readResult = await _pair.Application.Input.ReadAsync().DefaultTimeout();
                _testBase.Logger.LogTrace("Input is completed");

                Assert.True(readResult.IsCompleted);
                Assert.Equal((long)protocolError, Error);

                if (expectedErrorMessage != null)
                {
                    Assert.Contains(_testBase.LogMessages, m => m.Exception?.Message.Contains(expectedErrorMessage) ?? false);
                }
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

        public class Http3ControlStream : Http3StreamBase, IProtocolErrorCodeFeature
        {
            internal ConnectionContext StreamContext { get; }

            public bool CanRead => true;
            public bool CanWrite => false;

            public long StreamId => 0;

            public long Error { get; set; }

            public Http3ControlStream(Http3TestBase testBase)
            {
                _testBase = testBase;
                var inputPipeOptions = GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var outputPipeOptions = GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                _pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);
                StreamContext = new TestStreamContext(canRead: false, canWrite: true, _pair, this);
            }

            public Http3ControlStream(ConnectionContext streamContext)
            {
                StreamContext = streamContext;
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

        public class TestMultiplexedConnectionContext : MultiplexedConnectionContext, IConnectionLifetimeNotificationFeature, IConnectionLifetimeFeature, IConnectionHeartbeatFeature
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

            public TestMultiplexedConnectionContext(Http3TestBase testBase)
            {
                _testBase = testBase;
                Features = new FeatureCollection();
                Features.Set<IConnectionLifetimeNotificationFeature>(this);
                Features.Set<IConnectionHeartbeatFeature>(this);
                ConnectionClosedRequested = ConnectionClosingCts.Token;
            }

            public override string ConnectionId { get; set; }

            public override IFeatureCollection Features { get; }

            public override IDictionary<object, object> Items { get; set; }

            public CancellationToken ConnectionClosedRequested { get; set; }

            public CancellationTokenSource ConnectionClosingCts { get; set; } = new CancellationTokenSource();

            public override void Abort()
            {
                Abort(new ConnectionAbortedException());
            }

            public override void Abort(ConnectionAbortedException abortReason)
            {
                ToServerAcceptQueue.Writer.TryComplete();
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
                var stream = new Http3ControlStream(_testBase);
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

        private class TestStreamContext : ConnectionContext, IStreamDirectionFeature, IStreamIdFeature
        {
            private DuplexPipePair _pair;
            public TestStreamContext(bool canRead, bool canWrite, DuplexPipePair pair, IProtocolErrorCodeFeature feature)
            {
                _pair = pair;
                Features = new FeatureCollection();
                Features.Set<IStreamDirectionFeature>(this);
                Features.Set<IStreamIdFeature>(this);
                Features.Set(feature);

                CanRead = canRead;
                CanWrite = canWrite;
            }

            public override string ConnectionId { get; set; }

            public long StreamId { get; }

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

            public override void Abort(ConnectionAbortedException abortReason)
            {
                _pair.Application.Output.Complete(abortReason);
            }
        }
    }
}
