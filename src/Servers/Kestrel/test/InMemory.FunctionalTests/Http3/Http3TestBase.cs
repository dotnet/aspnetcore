using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.QPack;
using System.Reflection;
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
    public class Http3TestBase : TestApplicationErrorLoggerLoggedTest, IDisposable
    {
        internal TestServiceContext _serviceContext;
        internal Http3Connection _connection;
        internal readonly TimeoutControl _timeoutControl;
        internal readonly Mock<IKestrelTrace> _mockKestrelTrace = new Mock<IKestrelTrace>();
        internal readonly Mock<ITimeoutHandler> _mockTimeoutHandler = new Mock<ITimeoutHandler>();
        internal readonly Mock<MockTimeoutControlBase> _mockTimeoutControl;
        internal readonly MemoryPool<byte> _memoryPool = SlabMemoryPoolFactory.Create();
        protected Task _connectionTask;
        private TestMultiplexedConnectionContext _multiplexedContext;
        private Http3ControlStream _outboundControlStream;
        private Http3ControlStream _outboundEncoderStream;
        private Http3ControlStream _outboundDecoderStream;

        private Http3ControlStream _inboundControlStream;
        private TaskCompletionSource<object> _controlStreamCreated = new TaskCompletionSource<object>();
        private Http3ControlStream _inboundEncoderStream;
        private Http3ControlStream _inboundDecoderStream;

        private readonly CancellationTokenSource _connectionClosingCts = new CancellationTokenSource();

        protected readonly RequestDelegate _noopApplication;
        protected readonly RequestDelegate _echoApplication;
        protected readonly RequestDelegate _echoMethod;
        protected readonly RequestDelegate _echoPath;
        protected readonly RequestDelegate _echoHost;

        protected const long ControlStreamId = 0;
        protected const long EncoderStreamId = 2;
        protected const long DecoderStreamId = 3;

        public readonly Channel<ConnectionContext> AcceptQueue = Channel.CreateUnbounded<ConnectionContext>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        public Http3TestBase()
        {
            _timeoutControl = new TimeoutControl(_mockTimeoutHandler.Object);
            _mockTimeoutControl = new Mock<MockTimeoutControlBase>(_timeoutControl) { CallBase = true };
            _timeoutControl.Debugger = Mock.Of<IDebugger>();

            _noopApplication = context => Task.CompletedTask;

            _echoApplication = async context =>
            {
                var buffer = new byte[10000]; // TODO 
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

        public override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

            _serviceContext = new TestServiceContext(LoggerFactory, _mockKestrelTrace.Object)
            {
                Scheduler = PipeScheduler.Inline,
            };
        }

        protected async Task InitializeConnectionAsync(RequestDelegate application)
        {
            if (_connection == null)
            {
                CreateConnection();
            }

            // Skip all heartbeat and lifetime notification feature registrations.
            _connectionTask = _connection.InnerProcessRequestsAsync(new DummyApplication(application));

            await Task.CompletedTask;
        }

        internal async ValueTask<Http3RequestStream> InitializeConnectionAndStreamsAsync(RequestDelegate application)
        {
            await InitializeConnectionAsync(application);

            _outboundControlStream = await CreateOutboundControlStream(ControlStreamId);
            _outboundEncoderStream = await CreateOutboundControlStream(EncoderStreamId);
            _outboundDecoderStream = await CreateOutboundControlStream(DecoderStreamId);

            return await CreateRequestStream();
        }

        protected void CreateConnection()
        {
            var limits = _serviceContext.ServerOptions.Limits;

            var features = new FeatureCollection();

            _multiplexedContext = new TestMultiplexedConnectionContext(this);

            var httpConnectionContext = new Http3ConnectionContext
            {
                ConnectionContext = _multiplexedContext,
                ConnectionFeatures = features,
                ServiceContext = _serviceContext,
                MemoryPool = _memoryPool,
                TimeoutControl = _mockTimeoutControl.Object
            };

            _connection = new Http3Connection(httpConnectionContext);
            _mockTimeoutHandler.Setup(h => h.OnTimeout(It.IsAny<TimeoutReason>()))
                           .Callback<TimeoutReason>(r => _connection.OnTimeout(r));
            _ = ReadIncomingConnections();
        }

        protected async Task WaitForInboundControlStreamCreated()
        {
            await _controlStreamCreated.Task;
        }

        private async Task ReadIncomingConnections()
        {
            while (true)
            {
                var streamContext = await AcceptQueue.Reader.ReadAsync();
                var quicStreamFeature = streamContext.Features.Get<IStreamDirectionFeature>();

                // Always Unidirectional stream
                var stream = new Http3ControlStream(this, streamContext);
                var streamType = await TryReadStreamIdAsync(stream);

                if (streamType == ControlStreamId)
                {
                    _inboundControlStream = stream;
                    _controlStreamCreated.SetResult(null);
                }
                else if (streamType == EncoderStreamId)
                {
                    _inboundEncoderStream = stream;
                }
                else if (streamType == DecoderStreamId)
                {
                    _inboundDecoderStream = stream;
                }
            }
        }

        private async ValueTask<long> TryReadStreamIdAsync(Http3ControlStream controlStream)
        {
            while (true)
            {
                // TODO think about this
                var result = await controlStream.StreamContext.Transport.Input.ReadAsync();
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
                    controlStream.StreamContext.Transport.Input.AdvanceTo(consumed, examined);
                }
            }
        }

        protected Task StopConnectionAsync(long expectedLastStreamId)
        {
            // Graceful end of connection.
            _multiplexedContext.AcceptQueue.Writer.TryWrite(null);
            return WaitForConnectionStopAsync(expectedLastStreamId);
        }

        protected Task WaitForConnectionStopAsync(long expectedLastStreamId)
        {
            return WaitForConnectionErrorAsync<Exception>(expectedLastStreamId, Http3ErrorCode.NoError, expectedErrorMessage: null);
        }

        protected async Task<Dictionary<long, long>> ReadSettings()
        {
            return await _inboundControlStream.ReceiveSettingsFrame();
        }

        internal async Task WaitForConnectionErrorAsync<TException>(long expectedLastStreamId, Http3ErrorCode expectedErrorCode, params string[] expectedErrorMessage)
            where TException : Exception
        {
            await WaitForConnectionErrorAsyncDoNotCloseTransport<TException>(expectedLastStreamId, expectedErrorCode, expectedErrorMessage);
        }

        internal async Task WaitForConnectionErrorAsyncDoNotCloseTransport<TException>(long expectedLastStreamId, Http3ErrorCode expectedErrorCode, params string[] expectedErrorMessage)
          where TException : Exception
        {
            _outboundControlStream.VerifyGoAway(expectedLastStreamId);

            VerifyErrorCode(expectedErrorCode);

            if (expectedErrorMessage?.Length > 0)
            {
                var message = Assert.Single(TestApplicationErrorLogger.Messages, m => m.Exception is TException);

                Assert.Contains(expectedErrorMessage, expected => message.Exception.Message.Contains(expected));
            }

            await _connectionTask.DefaultTimeout();
            TestApplicationErrorLogger.LogInformation("Stopping Connection From ConnectionErrorAsync");
        }

        private void VerifyErrorCode(Http3ErrorCode expectedErrorCode)
        {
            throw new NotImplementedException();
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


        internal async ValueTask<Http3ControlStream> CreateOutboundControlStream(long id)
        {
            var stream = new Http3ControlStream(this, canRead: false, canWrite: true);
            if (id == ControlStreamId)
            {
                _outboundControlStream = stream;
            }
            else if (id == EncoderStreamId)
            {
                _outboundEncoderStream = stream;
            }
            else if (id == DecoderStreamId)
            {
                _outboundDecoderStream = stream;
            }

            _multiplexedContext.AcceptQueue.Writer.TryWrite(stream.StreamContext);
            await stream.WriteStreamIdAsync(id);
            return stream;
        }

        internal async Task WriteSettings(IList<Http3PeerSetting> settings)
        {
            await _outboundControlStream.WriteSettings(settings);
        }

        internal ValueTask<Http3RequestStream> CreateRequestStream()
        {
            var stream = new Http3RequestStream(this, _connection);
            _multiplexedContext.AcceptQueue.Writer.TryWrite(stream.StreamContext);
            return new ValueTask<Http3RequestStream>(stream);
        }

        internal class Http3StreamBase
        {
            protected IDuplexPipe _application;
            protected IDuplexPipe _transport;

            protected Http3TestBase _testBase;
            protected Http3Connection _connection;
            protected long _bytesReceived;

            protected Task SendAsync(ReadOnlySpan<byte> span)
            {
                var writableBuffer = _application.Output;
                writableBuffer.Write(span);
                return FlushAsync(writableBuffer);
            }

            protected static async Task FlushAsync(PipeWriter writableBuffer)
            {
                await writableBuffer.FlushAsync().AsTask().DefaultTimeout();
            }

            internal async Task<Http3FrameWithPayload> ReceiveFrameAsync()
            {
                var frame = new Http3FrameWithPayload();

                while (true)
                {
                    var result = await _application.Input.ReadAsync().AsTask().DefaultTimeout();
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
                        _bytesReceived += copyBuffer.Slice(copyBuffer.Start, consumed).Length;
                        _application.Input.AdvanceTo(consumed, examined);
                    }
                }
            }
        }

        internal class Http3RequestStream : Http3StreamBase, IHttpHeadersHandler
        {
            internal TestStreamContext StreamContext { get; }

            public bool CanRead => true;
            public bool CanWrite => true;

            public long StreamId => 0;

            private readonly byte[] _headerEncodingBuffer = new byte[4096]; // TODO fill with limit values.
            private QPackEncoder _qpackEncoder = new QPackEncoder();
            private QPackDecoder _qpackDecoder = new QPackDecoder(8192);
            protected readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public Http3RequestStream(Http3TestBase testBase, Http3Connection connection)
            {
                _testBase = testBase;
                _connection = connection;
                var inputPipeOptions = GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var outputPipeOptions = GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);

                var pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);
                _transport = pair.Transport;
                _application = pair.Application;

                StreamContext = new TestStreamContext(canRead: true, canWrite: true, pair.Transport, pair.Application);
            }

            public async Task<bool> SendHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers, bool endStream = false)
            {
                var outputWriter = _application.Output;
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
                    await _application.Output.CompleteAsync();
                }

                return done;
            }

            internal async Task SendDataAsync(Memory<byte> data, bool endStream = false)
            {
                var outputWriter = _application.Output;
                var frame = new Http3RawFrame();
                frame.PrepareData();
                frame.Length = data.Length;
                Http3FrameWriter.WriteHeader(frame, outputWriter);
                await SendAsync(data.Span);

                if (endStream)
                {
                    await _application.Output.CompleteAsync();
                }
            }

            internal async Task<Dictionary<string, string>> ExpectHeadersAsync()
            {
                var http3WithPayload = await ReceiveFrameAsync();
                _qpackDecoder.Decode(http3WithPayload.PayloadSequence, this);
                return _decodedHeaders;
            }

            internal async Task<Memory<byte>> ExpectDataAsync()
            {
                var http3WithPayload = await ReceiveFrameAsync();
                return http3WithPayload.Payload;
            }

            internal async Task ExpectReceiveEndOfStream()
            {
                var result = await _application.Input.ReadAsync().AsTask().DefaultTimeout();
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
                var knownHeader = H3StaticTable.Instance[index];
                _decodedHeaders[((Span<byte>)knownHeader.Name).GetAsciiStringNonNullCharacters()] = HttpUtilities.GetAsciiOrUTF8StringNonNullCharacters(knownHeader.Value);
            }

            public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
            {
                _decodedHeaders[((Span<byte>)H3StaticTable.Instance[index].Name).GetAsciiStringNonNullCharacters()] = value.GetAsciiOrUTF8StringNonNullCharacters();
            }

            internal async Task WaitForStreamErrorAsync(Http3ErrorCode protocolError, string expectedErrorMessage)
            {
                var readResult = await _application.Input.ReadAsync();
                _testBase.Logger.LogTrace("Input is completed");

                Assert.True(readResult.IsCompleted);
                Assert.Equal((long)protocolError, StreamContext.Error);

                if (expectedErrorMessage != null)
                {
                    Assert.Contains(_testBase.TestApplicationErrorLogger.Messages, m => m.Exception?.Message.Contains(expectedErrorMessage) ?? false);
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

        internal class Http3ControlStream : Http3StreamBase
        {
            //private bool _hasReceivedGoAway;

            internal ConnectionContext StreamContext { get; }

            public long StreamId => 0;

            public Dictionary<long, long> SettingsDictionary { get; } = new Dictionary<long, long>();

            public Http3ControlStream(Http3TestBase testBase, bool canRead, bool canWrite)
            {
                _testBase = testBase;
                var inputPipeOptions = GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var outputPipeOptions = GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);
                StreamContext = new TestStreamContext(canRead: false, canWrite: true, pair.Transport, pair.Application);
                _application = pair.Application; 
                _transport = pair.Transport;
            }

            public Http3ControlStream(Http3TestBase testBase, ConnectionContext context)
            {
                _testBase = testBase;
                StreamContext = context;
                _application = context.Transport;
            }

            internal async ValueTask<Dictionary<long, long>> ReceiveSettingsFrame()
            {
                var frame = await ReceiveFrameAsync();
                var payload = frame.PayloadSequence;

                while (true)
                {
                    var id = VariableLengthIntegerHelper.GetInteger(payload, out var consumed, out var examined);
                    if (id == -1)
                    {
                        break;
                    }

                    payload = payload.Slice(consumed);

                    var value = VariableLengthIntegerHelper.GetInteger(payload, out consumed, out examined);
                    if (id == -1)
                    {
                        break;
                    }

                    payload = payload.Slice(consumed);
                    SettingsDictionary[id] = value;
                }

                return SettingsDictionary;
            }

            public async ValueTask WriteStreamIdAsync(long id)
            {
                var writableBuffer = _application.Output;

                void WriteSpan(PipeWriter pw)
                {
                    var buffer = pw.GetSpan(sizeHint: 8);
                    var lengthWritten = VariableLengthIntegerHelper.WriteInteger(buffer, id);
                    pw.Advance(lengthWritten);
                }

                WriteSpan(writableBuffer);

                await FlushAsync(writableBuffer);
            }

            internal void VerifyGoAway(long expectedLastStreamId)
            {
                // Make sure stream has ended first.
                //Assert.True(_hasReceivedGoAway);
            }

            internal Task WriteSettings(IList<Http3PeerSetting> settings)
            {
                var frame = new Http3RawFrame();
                frame.PrepareSettings();

                frame.Length = Http3FrameWriter.GetSettingsLength(settings);
                Http3FrameWriter.WriteHeader(frame, _application.Output);
                var settingsBuffer = _application.Output.GetSpan((int)frame.Length);
                // TODO may want to modify behavior of input frames to mock different client behavior (client can send anything).
                Http3FrameWriter.WriteSettings(settings, settingsBuffer);
                _application.Output.Advance((int)frame.Length);

                return FlushAsync(_application.Output);
            }
        }

        private class TestMultiplexedConnectionContext : MultiplexedConnectionContext
        {
            public readonly Channel<ConnectionContext> AcceptQueue = Channel.CreateUnbounded<ConnectionContext>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

            private readonly Http3TestBase _testBase;

            public TestMultiplexedConnectionContext(Http3TestBase testBase)
            {
                _testBase = testBase;
            }

            public override string ConnectionId { get; set; }

            public override IFeatureCollection Features { get; }

            public override IDictionary<object, object> Items { get; set; }

            public override void Abort()
            {
            }

            public override void Abort(ConnectionAbortedException abortReason)
            {
            }

            public override async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
            {
                while (await AcceptQueue.Reader.WaitToReadAsync())
                {
                    while (AcceptQueue.Reader.TryRead(out var connection))
                    {
                        return connection;
                    }
                }

                return null;
            }

            public override ValueTask<ConnectionContext> ConnectAsync(IFeatureCollection features = null, CancellationToken cancellationToken = default)
            {
                var inputPipeOptions = GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var outputPipeOptions = GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);

                var pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);

                var remoteStreamContext = new TestStreamContext(canRead: true, canWrite: false, pair.Application, pair.Transport);
                var localStreamContext = new TestStreamContext(canRead: false, canWrite: true, pair.Transport, pair.Application);

                _testBase.AcceptQueue.Writer.TryWrite(remoteStreamContext);

                return new ValueTask<ConnectionContext>(localStreamContext);
            }
        }

        internal class TestStreamContext : ConnectionContext, IStreamDirectionFeature, IStreamIdFeature, IProtocolErrorCodeFeature
        {
            private readonly IDuplexPipe _application;

            public TestStreamContext(bool canRead, bool canWrite, IDuplexPipe transport, IDuplexPipe application)
            {
                Features = new FeatureCollection();
                Features.Set<IStreamDirectionFeature>(this);
                Features.Set<IStreamIdFeature>(this);
                CanRead = canRead;
                CanWrite = canWrite;
                Transport = transport;
                _application = application;
            }

            public long Error { get; set; }

            public override string ConnectionId { get; set; }

            public long StreamId { get; }

            public override IFeatureCollection Features { get; }

            public override IDictionary<object, object> Items { get; set; }

            public override IDuplexPipe Transport { get; set; }

            public bool CanRead { get; }

            public bool CanWrite { get; }

            public override void Abort(ConnectionAbortedException abortReason)
            {
                _application.Output.Complete(abortReason);
            }
        }
    }
}
