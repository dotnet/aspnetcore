using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.AspNetCore.Server.Kestrel.Core.Tests.Http2TestBase;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public partial class Http3TestBase : TestApplicationErrorLoggerLoggedTest, IDisposable
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
        private TestHttp3ControlStream _outboundControlStream;
        private TestHttp3ControlStream _outboundEncoderStream;
        private TestHttp3ControlStream _outboundDecoderStream;

        private TestHttp3ControlStream _inboundControlStream;
        private TaskCompletionSource<object> _controlStreamCreated = new TaskCompletionSource<object>();
        private TestHttp3ControlStream _inboundEncoderStream;
        private TestHttp3ControlStream _inboundDecoderStream;

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

        internal async ValueTask<TestHttp3RequestStream> InitializeConnectionAndStreamsAsync(RequestDelegate application)
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
                var stream = new TestHttp3ControlStream(this, streamContext);
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

        private async ValueTask<long> TryReadStreamIdAsync(TestHttp3ControlStream controlStream)
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
            Assert.Equal((long)expectedErrorCode, _multiplexedContext.Features.Get<IProtocolErrorCodeFeature>().Error);
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


        internal async ValueTask<TestHttp3ControlStream> CreateOutboundControlStream(long id)
        {
            var inputPipeOptions = GetInputPipeOptions(_serviceContext, _memoryPool, PipeScheduler.ThreadPool);
            var outputPipeOptions = GetOutputPipeOptions(_serviceContext, _memoryPool, PipeScheduler.ThreadPool);

            var pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);

            var remoteStreamContext = new TestStreamContext(canRead: true, canWrite: false, pair.Transport, pair.Application);
            var localStreamContext = new TestStreamContext(canRead: false, canWrite: true, pair.Application, pair.Transport);

            var stream = new TestHttp3ControlStream(this, localStreamContext);

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

            _multiplexedContext.AcceptQueue.Writer.TryWrite(remoteStreamContext);
            await stream.WriteStreamIdAsync(id);
            return stream;
        }

        internal async Task WriteSettings(IList<Http3PeerSetting> settings)
        {
            await _outboundControlStream.WriteSettings(settings);
        }

        internal ValueTask<TestHttp3RequestStream> CreateRequestStream()
        {
            var stream = new TestHttp3RequestStream(this, _connection);
            _multiplexedContext.AcceptQueue.Writer.TryWrite(stream.StreamContext);
            return new ValueTask<TestHttp3RequestStream>(stream);
        }
    }
}
