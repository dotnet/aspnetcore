using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.QPack;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions.Features;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.AspNetCore.Server.Kestrel.Core.Tests.Http2TestBase;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3TestBase : TestApplicationErrorLoggerLoggedTest, IDisposable, IQuicCreateStreamFeature, IQuicStreamListenerFeature
    {
        internal TestServiceContext _serviceContext;
        internal Http3Connection _connection;
        internal readonly TimeoutControl _timeoutControl;
        internal readonly Mock<IKestrelTrace> _mockKestrelTrace = new Mock<IKestrelTrace>();
        protected readonly Mock<ConnectionContext> _mockConnectionContext = new Mock<ConnectionContext>();
        internal readonly Mock<ITimeoutHandler> _mockTimeoutHandler = new Mock<ITimeoutHandler>();
        internal readonly Mock<MockTimeoutControlBase> _mockTimeoutControl;
        internal readonly MemoryPool<byte> _memoryPool = SlabMemoryPoolFactory.Create();
        protected Task _connectionTask;
        protected readonly RequestDelegate _echoApplication;

        private readonly Channel<ConnectionContext> _acceptConnectionQueue = Channel.CreateUnbounded<ConnectionContext>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        public Http3TestBase()
        {
            _timeoutControl = new TimeoutControl(_mockTimeoutHandler.Object);
            _mockTimeoutControl = new Mock<MockTimeoutControlBase>(_timeoutControl) { CallBase = true };
            _timeoutControl.Debugger = Mock.Of<IDebugger>();
            _echoApplication = async context =>
            {
                var buffer = new byte[Http3PeerSettings.MinAllowedMaxFrameSize];
                var received = 0;

                while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer, 0, received);
                }
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

            _connectionTask = _connection.ProcessRequestsAsync(new DummyApplication(application));

            await Task.CompletedTask;
        }

        internal async ValueTask<Http3RequestStream> InitializeConnectionAndStreamsAsync(RequestDelegate application)
        {
            await InitializeConnectionAsync(application);

            var controlStream1 = await CreateControlStream(0);
            var controlStream2 = await CreateControlStream(2);
            var controlStream3 = await CreateControlStream(3);

            return await CreateRequestStream();
        }

        protected void CreateConnection()
        {
            var limits = _serviceContext.ServerOptions.Limits;

            var features = new FeatureCollection();
            features.Set<IQuicCreateStreamFeature>(this);
            features.Set<IQuicStreamListenerFeature>(this);

            var httpConnectionContext = new HttpConnectionContext
            {
                ConnectionContext = _mockConnectionContext.Object,
                ConnectionFeatures = features,
                ServiceContext = _serviceContext,
                MemoryPool = _memoryPool,
                Transport = null, // Make sure it's null
                TimeoutControl = _mockTimeoutControl.Object
            };

            _connection = new Http3Connection(httpConnectionContext);
            var httpConnection = new HttpConnection(httpConnectionContext);
            httpConnection.Initialize(_connection);
            _mockTimeoutHandler.Setup(h => h.OnTimeout(It.IsAny<TimeoutReason>()))
                           .Callback<TimeoutReason>(r => httpConnection.OnTimeout(r));
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

        public ValueTask<ConnectionContext> StartUnidirectionalStreamAsync()
        {
            var stream = new Http3ControlStream(this, _connection);
            // TODO put these somewhere to be read.
            return new ValueTask<ConnectionContext>(stream.ConnectionContext);
        }

        public async ValueTask<ConnectionContext> AcceptAsync()
        {
            while (await _acceptConnectionQueue.Reader.WaitToReadAsync())
            {
                while (_acceptConnectionQueue.Reader.TryRead(out var connection))
                {
                    return connection;
                }
            }

            return null;
        }

        internal async ValueTask<Http3ControlStream> CreateControlStream(int id)
        {
            var stream = new Http3ControlStream(this, _connection);
            _acceptConnectionQueue.Writer.TryWrite(stream.ConnectionContext);
            await stream.WriteStreamIdAsync(id);
            return stream;
        }

        internal ValueTask<Http3RequestStream> CreateRequestStream()
        {
            var stream = new Http3RequestStream(this, _connection);
            _acceptConnectionQueue.Writer.TryWrite(stream.ConnectionContext);
            return new ValueTask<Http3RequestStream>(stream);
        }

        public ValueTask<ConnectionContext> StartBidirectionalStreamAsync()
        {
            var stream = new Http3RequestStream(this, _connection);
            // TODO put these somewhere to be read.
            return new ValueTask<ConnectionContext>(stream.ConnectionContext);
        }

        internal class Http3StreamBase
        {
            protected DuplexPipe.DuplexPipePair _pair;
            protected Http3TestBase _testBase;
            protected Http3Connection _connection;

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
        }

        internal class Http3RequestStream : Http3StreamBase, IHttpHeadersHandler, IQuicStreamFeature
        {
            internal ConnectionContext ConnectionContext { get; }

            public bool CanRead => true;
            public bool CanWrite => true;

            public long StreamId => 0;

            private readonly byte[] _headerEncodingBuffer = new byte[Http3PeerSettings.MinAllowedMaxFrameSize];
            private QPackEncoder _qpackEncoder = new QPackEncoder();
            private QPackDecoder _qpackDecoder = new QPackDecoder(8192);
            private long _bytesReceived;
            protected readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public Http3RequestStream(Http3TestBase testBase, Http3Connection connection)
            {
                _testBase = testBase;
                _connection = connection;
                var inputPipeOptions = GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var outputPipeOptions = GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);

                _pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);

                ConnectionContext = new DefaultConnectionContext();
                ConnectionContext.Transport = _pair.Transport;
                ConnectionContext.Features.Set<IQuicStreamFeature>(this);
            }

            public async Task<bool> SendHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
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
                return done;
            }

            internal async Task SendDataAsync(Memory<byte> data)
            {
                var outputWriter = _pair.Application.Output;
                var frame = new Http3RawFrame();
                frame.PrepareData();
                frame.Length = data.Length;
                Http3FrameWriter.WriteHeader(frame, outputWriter);
                await SendAsync(data.Span);
            }

            internal async Task<IEnumerable<KeyValuePair<string, string>>> ExpectHeadersAsync()
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

            internal async Task<Http3FrameWithPayload> ReceiveFrameAsync(uint maxFrameSize = Http3PeerSettings.DefaultMaxFrameSize)
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
                var knownHeader = H3StaticTable.Instance[index];
                _decodedHeaders[((Span<byte>)knownHeader.Name).GetAsciiStringNonNullCharacters()] = ((Span<byte>)knownHeader.Value).GetAsciiOrUTF8StringNonNullCharacters();
            }

            public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
            {
                _decodedHeaders[((Span<byte>)H3StaticTable.Instance[index].Name).GetAsciiStringNonNullCharacters()] = value.GetAsciiOrUTF8StringNonNullCharacters();
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


        internal class Http3ControlStream : Http3StreamBase, IQuicStreamFeature
        {
            internal ConnectionContext ConnectionContext { get; }

            public bool CanRead => true;
            public bool CanWrite => false;

            // TODO
            public long StreamId => 0;

            public Http3ControlStream(Http3TestBase testBase, Http3Connection connection)
            {
                _testBase = testBase;
                _connection = connection;
                var inputPipeOptions = GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var outputPipeOptions = GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);

                _pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);

                ConnectionContext = new DefaultConnectionContext();
                ConnectionContext.Transport = _pair.Transport;
                ConnectionContext.Features.Set<IQuicStreamFeature>(this);
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
        }
    }
}
