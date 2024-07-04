// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.QPack;
using System.Text;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;
using static System.IO.Pipelines.DuplexPipe;
using Http3SettingType = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.Http3SettingType;

namespace Microsoft.AspNetCore.InternalTesting;

internal class Http3InMemory
{
    protected static readonly int MaxRequestHeaderFieldSize = 16 * 1024;
    protected static readonly string _4kHeaderValue = new string('a', 4096);
    protected static readonly byte[] _helloWorldBytes = Encoding.ASCII.GetBytes("hello, world");
    protected static readonly byte[] _maxData = Encoding.ASCII.GetBytes(new string('a', 16 * 1024));

    public Http3InMemory(ServiceContext serviceContext, FakeTimeProvider fakeTimeProvider, ITimeoutHandler timeoutHandler, ILoggerFactory loggerFactory)
    {
        _serviceContext = serviceContext;
        _timeoutControl = new TimeoutControl(new TimeoutControlConnectionInvoker(this, timeoutHandler), fakeTimeProvider);
        _timeoutControl.Debugger = new TestDebugger();

        _fakeTimeProvider = fakeTimeProvider;

        _serverReceivedSettings = Channel.CreateUnbounded<KeyValuePair<Http3SettingType, long>>();
        Logger = loggerFactory.CreateLogger<Http3InMemory>();
    }

    private class TestDebugger : IDebugger
    {
        public bool IsAttached => false;
    }

    private class TimeoutControlConnectionInvoker : ITimeoutHandler
    {
        private readonly ITimeoutHandler _inner;
        private readonly Http3InMemory _http3;

        public TimeoutControlConnectionInvoker(Http3InMemory http3, ITimeoutHandler inner)
        {
            _http3 = http3;
            _inner = inner;
        }

        public void OnTimeout(TimeoutReason reason)
        {
            _inner.OnTimeout(reason);
            _http3._httpConnection.OnTimeout(reason);
        }
    }

    internal ServiceContext _serviceContext;
    private FakeTimeProvider _fakeTimeProvider;
    internal HttpConnection _httpConnection;
    internal readonly TimeoutControl _timeoutControl;
    internal readonly MemoryPool<byte> _memoryPool = PinnedBlockMemoryPoolFactory.Create();
    internal readonly ConcurrentQueue<TestStreamContext> _streamContextPool = new ConcurrentQueue<TestStreamContext>();
    protected Task _connectionTask;
    internal ILogger Logger { get; }

    internal readonly ConcurrentDictionary<long, Http3StreamBase> _runningStreams = new ConcurrentDictionary<long, Http3StreamBase>();
    internal readonly Channel<KeyValuePair<Http3SettingType, long>> _serverReceivedSettings;

    internal Func<TestStreamContext, Http3ControlStream> OnCreateServerControlStream { get; set; }
    private Http3ControlStream _inboundControlStream;
    private long _currentStreamId;
    internal Http3Connection Connection { get; private set; }

    internal Http3ControlStream OutboundControlStream { get; set; }

    internal ChannelReader<KeyValuePair<Http3SettingType, long>> ServerReceivedSettingsReader => _serverReceivedSettings.Reader;

    internal TestMultiplexedConnectionContext MultiplexedConnectionContext { get; set; }

    internal Dictionary<string, object> ConnectionTags => MultiplexedConnectionContext.Tags.ToDictionary(t => t.Key, t => t.Value);

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
#if IS_TESTS
            while (await reader.WaitToReadAsync().DefaultTimeout())
#else
            while (await reader.WaitToReadAsync())
#endif
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

    internal void CloseServerGracefully()
    {
        MultiplexedConnectionContext.ConnectionClosingCts.Cancel();
    }

    internal Task WaitForConnectionStopAsync(long expectedLastStreamId, bool ignoreNonGoAwayFrames, Http3ErrorCode? expectedErrorCode = null)
    {
        return WaitForConnectionErrorAsync<Exception>(ignoreNonGoAwayFrames, expectedLastStreamId, expectedErrorCode: expectedErrorCode ?? 0, matchExpectedErrorMessage: null);
    }

    internal async Task WaitForConnectionErrorAsync<TException>(bool ignoreNonGoAwayFrames, long? expectedLastStreamId, Http3ErrorCode expectedErrorCode, Action<Type, string[]> matchExpectedErrorMessage = null, params string[] expectedErrorMessage)
        where TException : Exception
    {
        await WaitForGoAwayAsync(ignoreNonGoAwayFrames, expectedLastStreamId);

        AssertConnectionError<TException>(expectedErrorCode, matchExpectedErrorMessage, expectedErrorMessage);

        // Verify HttpConnection.ProcessRequestsAsync has exited.
#if IS_TESTS
        await _connectionTask.DefaultTimeout();
#else
        await _connectionTask;
#endif
    }

    internal async Task WaitForGoAwayAsync(bool ignoreNonGoAwayFrames, long? expectedLastStreamId)
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
    }

    private void AssertConnectionError<TException>(Http3ErrorCode expectedErrorCode, Action<Type, string[]> matchExpectedErrorMessage = null, params string[] expectedErrorMessage) where TException : Exception
    {
        var currentError = (Http3ErrorCode)MultiplexedConnectionContext.Error;
        if (currentError != expectedErrorCode)
        {
            throw new InvalidOperationException($"Expected error code {expectedErrorCode}, got {currentError}.");
        }

        matchExpectedErrorMessage?.Invoke(typeof(TException), expectedErrorMessage);
    }

    internal void VerifyGoAway(Http3FrameWithPayload frame, long expectedLastStreamId)
    {
        AssertFrameType(frame.Type, Http3FrameType.GoAway);
        var payload = frame.Payload;
        if (!VariableLengthIntegerHelper.TryRead(payload.Span, out var streamId, out var _))
        {
            throw new InvalidOperationException("Failed to read GO_AWAY stream ID.");
        }
        if (streamId != expectedLastStreamId)
        {
            throw new InvalidOperationException($"Expected stream ID {expectedLastStreamId}, got {streamId}.");
        }
    }

    public void AdvanceTime(TimeSpan timeSpan)
    {
        Logger.LogDebug("Advancing timeProvider {timeSpan}.", timeSpan);

        var timeProvider = _fakeTimeProvider;
        var endTime = timeProvider.GetTimestamp(timeSpan);

        while (timeProvider.GetTimestamp(Heartbeat.Interval) < endTime)
        {
            timeProvider.Advance(Heartbeat.Interval);
            _timeoutControl.Tick(timeProvider.GetTimestamp());
        }

        timeProvider.Advance(timeProvider.GetElapsedTime(timeProvider.GetTimestamp(), endTime));
        _timeoutControl.Tick(timeProvider.GetTimestamp());
    }

    public void TriggerTick(TimeSpan timeSpan = default)
    {
        _fakeTimeProvider.Advance(timeSpan);
        var timestamp = _fakeTimeProvider.GetTimestamp();
        Connection?.Tick(timestamp);
    }

    public async Task InitializeConnectionAsync(RequestDelegate application)
    {
        MultiplexedConnectionContext = new TestMultiplexedConnectionContext(this)
        {
            ConnectionId = "TEST"
        };

        var metricsContext = MultiplexedConnectionContext.Features.GetRequiredFeature<IConnectionMetricsContextFeature>().MetricsContext;

        var httpConnectionContext = new HttpMultiplexedConnectionContext(
            connectionId: MultiplexedConnectionContext.ConnectionId,
            HttpProtocols.Http3,
            altSvcHeader: null,
            connectionContext: MultiplexedConnectionContext,
            connectionFeatures: MultiplexedConnectionContext.Features,
            serviceContext: _serviceContext,
            memoryPool: _memoryPool,
            localEndPoint: null,
            remoteEndPoint: null,
            metricsContext: metricsContext);
        httpConnectionContext.TimeoutControl = _timeoutControl;

        _httpConnection = new HttpConnection(httpConnectionContext);
        _httpConnection.Initialize(Connection);

        // ProcessRequestAsync will create the Http3Connection
        _connectionTask = _httpConnection.ProcessRequestsAsync(new DummyApplication(application));

        Connection = (Http3Connection)_httpConnection._requestProcessor;
        Connection._streamLifetimeHandler = new LifetimeHandlerInterceptor(Connection, this);

        await GetInboundControlStream();
    }

    public static void AssertFrameType(Http3FrameType actual, Http3FrameType expected)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException($"Expected {expected} frame. Got {actual}.");
        }
    }

    internal async ValueTask<Http3RequestStream> InitializeConnectionAndStreamsAsync(RequestDelegate application, IEnumerable<KeyValuePair<string, string>> headers, bool endStream = false)
    {
        await InitializeConnectionAsync(application);

        OutboundControlStream = await CreateControlStream();

        return await CreateRequestStream(headers, endStream: endStream);
    }

    private class LifetimeHandlerInterceptor : IHttp3StreamLifetimeHandler
    {
        private readonly IHttp3StreamLifetimeHandler _inner;
        private readonly Http3InMemory _http3TestBase;

        public LifetimeHandlerInterceptor(IHttp3StreamLifetimeHandler inner, Http3InMemory http3TestBase)
        {
            _inner = inner;
            _http3TestBase = http3TestBase;
        }

        public bool OnInboundControlStream(Server.Kestrel.Core.Internal.Http3.Http3ControlStream stream)
        {
            return _inner.OnInboundControlStream(stream);
        }

        public void OnInboundControlStreamSetting(Http3SettingType type, long value)
        {
            _inner.OnInboundControlStreamSetting(type, value);

            var success = _http3TestBase._serverReceivedSettings.Writer.TryWrite(
                new KeyValuePair<Http3SettingType, long>(type, value));
            Debug.Assert(success);
        }

        public bool OnInboundDecoderStream(Server.Kestrel.Core.Internal.Http3.Http3ControlStream stream)
        {
            return _inner.OnInboundDecoderStream(stream);
        }

        public bool OnInboundEncoderStream(Server.Kestrel.Core.Internal.Http3.Http3ControlStream stream)
        {
            return _inner.OnInboundEncoderStream(stream);
        }

        public void OnStreamCompleted(IHttp3Stream stream)
        {
            _inner.OnStreamCompleted(stream);

            if (_http3TestBase._runningStreams.TryRemove(stream.StreamId, out var testStream))
            {
                testStream.OnStreamCompletedTcs.TrySetResult();
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
                testStream.OnStreamCreatedTcs.TrySetResult();
            }
        }

        public void OnStreamHeaderReceived(IHttp3Stream stream)
        {
            _inner.OnStreamHeaderReceived(stream);

            if (_http3TestBase._runningStreams.TryGetValue(stream.StreamId, out var testStream))
            {
                testStream.OnHeaderReceivedTcs.TrySetResult();
            }
        }

        public void OnUnidentifiedStreamReceived(Http3PendingStream stream)
        {
            _inner.OnUnidentifiedStreamReceived(stream);

            if (_http3TestBase._runningStreams.TryGetValue(stream.StreamId, out var testStream))
            {
                testStream.OnUnidentifiedStreamCreatedTcs.TrySetResult();
            }
        }
    }

    protected void ConnectionClosed()
    {

    }

    public static PipeOptions GetInputPipeOptions(ServiceContext serviceContext, MemoryPool<byte> memoryPool, PipeScheduler writerScheduler) => new PipeOptions
    (
        pool: memoryPool,
        readerScheduler: serviceContext.Scheduler,
        writerScheduler: writerScheduler,
        pauseWriterThreshold: serviceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
        resumeWriterThreshold: serviceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
        useSynchronizationContext: false,
        minimumSegmentSize: memoryPool.GetMinimumSegmentSize()
    );

    public static PipeOptions GetOutputPipeOptions(ServiceContext serviceContext, MemoryPool<byte> memoryPool, PipeScheduler readerScheduler) => new PipeOptions
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
        testStreamContext.Initialize(streamId: 2);

        var stream = new Http3ControlStream(this, testStreamContext);
        _runningStreams[stream.StreamId] = stream;

        MultiplexedConnectionContext.ToServerAcceptQueue.Writer.TryWrite(stream.StreamContext);
        if (id != null)
        {
            await stream.WriteStreamIdAsync(id.GetValueOrDefault());
        }
        return stream;
    }

    internal async ValueTask<Http3RequestStream> CreateRequestStream(IEnumerable<KeyValuePair<string, string>> headers, Http3RequestHeaderHandler headerHandler = null, bool endStream = false, TaskCompletionSource tsc = null)
    {
        var stream = CreateRequestStreamCore(headerHandler);

        if (tsc is not null)
        {
            stream.StartStreamDisposeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        if (headers is not null)
        {
            await stream.SendHeadersAsync(headers, endStream);
        }

        _runningStreams[stream.StreamId] = stream;

        MultiplexedConnectionContext.ToServerAcceptQueue.Writer.TryWrite(stream.StreamContext);

        return stream;
    }

    internal async ValueTask<Http3RequestStream> CreateRequestStream(Http3HeadersEnumerator headers, Http3RequestHeaderHandler headerHandler = null, bool endStream = false, TaskCompletionSource tsc = null)
    {
        var stream = CreateRequestStreamCore(headerHandler);

        if (tsc is not null)
        {
            stream.StartStreamDisposeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        await stream.SendHeadersAsync(headers, endStream);

        _runningStreams[stream.StreamId] = stream;

        MultiplexedConnectionContext.ToServerAcceptQueue.Writer.TryWrite(stream.StreamContext);

        return stream;
    }

    private Http3RequestStream CreateRequestStreamCore(Http3RequestHeaderHandler headerHandler)
    {
        var requestStreamId = GetStreamId(0x00);
        if (!_streamContextPool.TryDequeue(out var testStreamContext))
        {
            testStreamContext = new TestStreamContext(canRead: true, canWrite: true, this);
        }
        else
        {
            Logger.LogDebug($"Reusing context for request stream {requestStreamId}.");
        }
        testStreamContext.Initialize(requestStreamId);

        return new Http3RequestStream(this, Connection, testStreamContext, headerHandler ?? new Http3RequestHeaderHandler());
    }
}

internal class Http3StreamBase
{
    internal TaskCompletionSource OnUnidentifiedStreamCreatedTcs { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource OnStreamCreatedTcs { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource OnStreamCompletedTcs { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource OnHeaderReceivedTcs { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    internal TestStreamContext StreamContext { get; }
    internal DuplexPipe.DuplexPipePair Pair { get; }
    internal Http3InMemory TestBase { get; private protected set; }
    internal Http3Connection Connection { get; private protected set; }
    public long BytesReceived { get; private set; }
    public long Error
    {
        get => StreamContext.Error;
        set => StreamContext.Error = value;
    }

    public Task OnUnidentifiedStreamCreatedTask => OnUnidentifiedStreamCreatedTcs.Task;
    public Task OnStreamCreatedTask => OnStreamCreatedTcs.Task;
    public Task OnStreamCompletedTask => OnStreamCompletedTcs.Task;
    public Task OnHeaderReceivedTask => OnHeaderReceivedTcs.Task;

    public ConnectionAbortedException AbortReadException => StreamContext.AbortReadException;
    public ConnectionAbortedException AbortWriteException => StreamContext.AbortWriteException;

    public Http3StreamBase(TestStreamContext testStreamContext)
    {
        StreamContext = testStreamContext;
        Pair = testStreamContext._pair;
    }

    protected Task SendAsync(ReadOnlySpan<byte> span)
    {
        var writableBuffer = Pair.Application.Output;
        writableBuffer.Write(span);
        return FlushAsync(writableBuffer);
    }

    protected static Task FlushAsync(PipeWriter writableBuffer)
    {
        var task = writableBuffer.FlushAsync();
#if IS_TESTS
        return task.AsTask().DefaultTimeout();
#else
        return task.GetAsTask();
#endif
    }

    internal async Task ReceiveEndAsync()
    {
        var result = await ReadApplicationInputAsync();
        if (!result.IsCompleted)
        {
            throw new InvalidOperationException("End not received.");
        }
    }

#if IS_TESTS
    protected Task<ReadResult> ReadApplicationInputAsync()
    {
        return Pair.Application.Input.ReadAsync().AsTask().DefaultTimeout();
    }
#else
    protected ValueTask<ReadResult> ReadApplicationInputAsync()
    {
        return Pair.Application.Input.ReadAsync();
    }
#endif

    internal async ValueTask<Http3FrameWithPayload> ReceiveFrameAsync(bool expectEnd = false, bool allowEnd = false, Http3FrameWithPayload frame = null)
    {
        frame ??= new Http3FrameWithPayload();

        while (true)
        {
            var result = await ReadApplicationInputAsync();
            var buffer = result.Buffer;
            var consumed = buffer.Start;
            var examined = buffer.Start;
            var copyBuffer = buffer;

            try
            {
                if (buffer.Length == 0)
                {
                    if (result.IsCompleted && allowEnd)
                    {
                        return null;
                    }

                    throw new InvalidOperationException("No data received.");
                }

                if (Http3FrameReader.TryReadFrame(ref buffer, frame, out var framePayload))
                {
                    consumed = examined = framePayload.End;
                    frame.Payload = framePayload.ToArray();

                    if (expectEnd)
                    {
                        if (!result.IsCompleted || buffer.Length > 0)
                        {
                            throw new Exception("Reader didn't complete with frame");
                        }
                    }

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
                Pair.Application.Input.AdvanceTo(consumed, examined);
            }
        }
    }

    internal async Task SendFrameAsync(Http3FrameType frameType, Memory<byte> data, bool endStream = false)
    {
        var outputWriter = Pair.Application.Output;
        Http3FrameWriter.WriteHeader(frameType, data.Length, outputWriter);

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
        var writableBuffer = Pair.Application.Output;
        if (span.Length > 0)
        {
            writableBuffer.Write(span);
        }
        return writableBuffer.CompleteAsync().AsTask();
    }

    internal async Task WaitForStreamErrorAsync(Http3ErrorCode protocolError, Action<string> matchExpectedErrorMessage = null, string expectedErrorMessage = null)
    {
        try
        {
            var result = await ReadApplicationInputAsync();
            if (!result.IsCompleted)
            {
                throw new InvalidOperationException("Stream not ended.");
            }
        }
        catch (ConnectionAbortedException)
        {
            // no-op, this just means that the stream was aborted prior to the read ending. This is probably
            // intentional, so go onto invoking the comparisons
        }
        finally
        {
            if (protocolError != Http3ErrorCode.NoError && (Http3ErrorCode)Error != protocolError)
            {
                throw new InvalidOperationException($"Expected error code {protocolError}, got {(Http3ErrorCode)Error}.");
            }
            matchExpectedErrorMessage?.Invoke(expectedErrorMessage);
        }
    }
}

internal class Http3RequestHeaderHandler
{
    public readonly byte[] HeaderEncodingBuffer = new byte[96 * 1024];
    public readonly QPackDecoder QpackDecoder = new QPackDecoder(8192);
    public readonly Dictionary<string, string> DecodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

internal class Http3RequestStream : Http3StreamBase, IHttpStreamHeadersHandler
{
    private readonly TestStreamContext _testStreamContext;
    private readonly Http3RequestHeaderHandler _headerHandler;
    private readonly long _streamId;

    public bool CanRead => true;
    public bool CanWrite => true;

    public long StreamId => _streamId;

    public bool Disposed => _testStreamContext.Disposed;
    public Task OnDisposedTask => _testStreamContext.OnDisposedTask;
    public Task OnDisposingTask => _testStreamContext.OnDisposingTask;

    public TaskCompletionSource StartStreamDisposeTcs
    {
        get => _testStreamContext.StartStreamDisposeTcs;
        internal set => _testStreamContext.StartStreamDisposeTcs = value;
    }

    public Http3RequestStream(Http3InMemory testBase, Http3Connection connection, TestStreamContext testStreamContext, Http3RequestHeaderHandler headerHandler)
        : base(testStreamContext)
    {
        TestBase = testBase;
        Connection = connection;
        _streamId = testStreamContext.StreamId;
        _testStreamContext = testStreamContext;
        this._headerHandler = headerHandler;
    }

    public Task SendHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers, bool endStream = false)
    {
        return SendHeadersAsync(GetHeadersEnumerator(headers), endStream);
    }

    public async Task SendHeadersAsync(Http3HeadersEnumerator headers, bool endStream = false)
    {
        var headersTotalSize = 0;

        var buffer = _headerHandler.HeaderEncodingBuffer.AsMemory();
        var done = QPackHeaderWriter.BeginEncodeHeaders(headers, buffer.Span, ref headersTotalSize, out var length);
        if (!done)
        {
            throw new InvalidOperationException("The headers are too large.");
        }
        await SendFrameAsync(Http3FrameType.Headers, buffer.Slice(0, length), endStream);
    }

    internal Http3HeadersEnumerator GetHeadersEnumerator(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var dictionary = headers
            .GroupBy(g => g.Key)
            .ToDictionary(g => g.Key, g => new StringValues(g.Select(values => values.Value).ToArray()));

        var headersEnumerator = new Http3HeadersEnumerator();
        headersEnumerator.Initialize(dictionary);
        return headersEnumerator;
    }

    internal async Task SendHeadersPartialAsync()
    {
        // Send HEADERS frame header without content.
        var outputWriter = Pair.Application.Output;
        Http3FrameWriter.WriteHeader(Http3FrameType.Data, frameLength: 10, outputWriter);
        await SendAsync(Span<byte>.Empty);
    }

    internal async Task SendDataAsync(Memory<byte> data, bool endStream = false)
    {
        await SendFrameAsync(Http3FrameType.Data, data, endStream);
    }

    internal async ValueTask<Dictionary<string, string>> ExpectHeadersAsync(bool expectEnd = false)
    {
        var http3WithPayload = await ReceiveFrameAsync(expectEnd);
        Http3InMemory.AssertFrameType(http3WithPayload.Type, Http3FrameType.Headers);

        _headerHandler.DecodedHeaders.Clear();
        _headerHandler.QpackDecoder.Decode(http3WithPayload.PayloadSequence, endHeaders: true, this);
        _headerHandler.QpackDecoder.Reset();
        return _headerHandler.DecodedHeaders.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, _headerHandler.DecodedHeaders.Comparer);
    }

    internal async ValueTask<Memory<byte>> ExpectDataAsync()
    {
        var http3WithPayload = await ReceiveFrameAsync();
        return http3WithPayload.Payload;
    }

    internal async ValueTask<Dictionary<string, string>> ExpectTrailersAsync()
    {
        var http3WithPayload = await ReceiveFrameAsync(false, true);
        Http3InMemory.AssertFrameType(http3WithPayload.Type, Http3FrameType.Headers);

        _headerHandler.DecodedHeaders.Clear();
        _headerHandler.QpackDecoder.Decode(http3WithPayload.PayloadSequence, endHeaders: true, this);
        _headerHandler.QpackDecoder.Reset();
        return _headerHandler.DecodedHeaders.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, _headerHandler.DecodedHeaders.Comparer);
    }

    internal async Task ExpectReceiveEndOfStream()
    {
        var result = await ReadApplicationInputAsync();
        if (!result.IsCompleted)
        {
            throw new InvalidOperationException("End of stream not received.");
        }
    }

    public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        _headerHandler.DecodedHeaders[name.GetAsciiStringNonNullCharacters()] = value.GetAsciiOrUTF8StringNonNullCharacters();
    }

    public void OnHeadersComplete(bool endHeaders)
    {
    }

    public void OnStaticIndexedHeader(int index)
    {
        var knownHeader = H3StaticTable.Get(index);
        _headerHandler.DecodedHeaders[((Span<byte>)knownHeader.Name).GetAsciiStringNonNullCharacters()] = HttpUtilities.GetAsciiOrUTF8StringNonNullCharacters((ReadOnlySpan<byte>)knownHeader.Value);
    }

    public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
    {
        _headerHandler.DecodedHeaders[((Span<byte>)H3StaticTable.Get(index).Name).GetAsciiStringNonNullCharacters()] = value.GetAsciiOrUTF8StringNonNullCharacters();
    }

    public void Complete()
    {
        _testStreamContext.Complete();
    }

    public void OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        _headerHandler.DecodedHeaders[name.GetAsciiStringNonNullCharacters()] = value.GetAsciiOrUTF8StringNonNullCharacters();
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

    public Http3ControlStream(Http3InMemory testBase, TestStreamContext testStreamContext)
        : base(testStreamContext)
    {
        TestBase = testBase;
        _streamId = testStreamContext.StreamId;
    }

    internal async ValueTask<Dictionary<long, long>> ExpectSettingsAsync()
    {
        var http3WithPayload = await ReceiveFrameAsync();
        Http3InMemory.AssertFrameType(http3WithPayload.Type, Http3FrameType.Settings);

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
        var writableBuffer = Pair.Application.Output;

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
        var data = new byte[VariableLengthIntegerHelper.GetByteCount(streamId)];
        VariableLengthIntegerHelper.WriteInteger(data, streamId);

        await SendFrameAsync(Http3FrameType.GoAway, data, endStream);
    }

    internal async Task SendSettingsAsync(List<Http3PeerSetting> settings, bool endStream = false)
    {
        var settingsLength = CalculateSettingsSize(settings);
        var buffer = new byte[settingsLength];
        WriteSettings(settings, buffer);

        await SendFrameAsync(Http3FrameType.Settings, buffer, endStream);
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
            var result = await ReadApplicationInputAsync();
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
                Pair.Application.Input.AdvanceTo(consumed, examined);
            }
        }
    }

    internal async Task WaitForGoAwayAsync(bool ignoreNonGoAwayFrames, long? expectedLastStreamId)
    {
        var frame = await ReceiveFrameAsync();

        if (ignoreNonGoAwayFrames)
        {
            while (frame.Type != Http3FrameType.GoAway)
            {
                frame = await ReceiveFrameAsync();
            }
        }

        if (expectedLastStreamId != null)
        {
            VerifyGoAway(frame, expectedLastStreamId.GetValueOrDefault());
        }
    }

    internal void VerifyGoAway(Http3FrameWithPayload frame, long expectedLastStreamId)
    {
        Http3InMemory.AssertFrameType(frame.Type, Http3FrameType.GoAway);
        var payload = frame.Payload;
        if (!VariableLengthIntegerHelper.TryRead(payload.Span, out var streamId, out var _))
        {
            throw new InvalidOperationException("Failed to read GO_AWAY stream ID.");
        }
        if (streamId != expectedLastStreamId)
        {
            throw new InvalidOperationException($"Expected stream ID {expectedLastStreamId}, got {streamId}.");
        }
    }
}

internal class TestMultiplexedConnectionContext : MultiplexedConnectionContext, IConnectionLifetimeNotificationFeature, IConnectionLifetimeFeature, IConnectionHeartbeatFeature, IProtocolErrorCodeFeature, IConnectionMetricsContextFeature, IConnectionMetricsTagsFeature
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

    private readonly Http3InMemory _testBase;
    private long? _error;

    public TestMultiplexedConnectionContext(Http3InMemory testBase)
    {
        _testBase = testBase;
        Features = new FeatureCollection();
        Features.Set<IConnectionLifetimeNotificationFeature>(this);
        Features.Set<IConnectionHeartbeatFeature>(this);
        Features.Set<IProtocolErrorCodeFeature>(this);
        Features.Set<IConnectionMetricsContextFeature>(this);
        Features.Set<IConnectionMetricsTagsFeature>(this);
        ConnectionClosedRequested = ConnectionClosingCts.Token;

        MetricsContext = TestContextFactory.CreateMetricsContext(this);
    }

    public override string ConnectionId { get; set; }

    public override IFeatureCollection Features { get; }

    public override IDictionary<object, object> Items { get; set; }

    public CancellationToken ConnectionClosedRequested { get; set; }

    public CancellationTokenSource ConnectionClosingCts { get; set; } = new CancellationTokenSource();

    public long Error
    {
        get => _error ?? -1;
        set => _error = value;
    }

    public ConnectionMetricsContext MetricsContext { get; }

    public ICollection<KeyValuePair<string, object>> Tags { get; } = new List<KeyValuePair<string, object>>();

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
        try
        {
            while (await ToServerAcceptQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                while (ToServerAcceptQueue.Reader.TryRead(out var connection))
                {
                    return connection;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation token. Graceful server abort.
        }

        return null;
    }

    public override ValueTask<ConnectionContext> ConnectAsync(IFeatureCollection features = null, CancellationToken cancellationToken = default)
    {
        var testStreamContext = new TestStreamContext(canRead: true, canWrite: false, _testBase);
        testStreamContext.Initialize(streamId: 3);

        var stream = _testBase.OnCreateServerControlStream?.Invoke(testStreamContext) ?? new Http3ControlStream(_testBase, testStreamContext);
        ToClientAcceptQueue.Writer.WriteAsync(stream);
        return new ValueTask<ConnectionContext>(stream.StreamContext);
    }

    public void OnHeartbeat(Action<object> action, object state)
    {
    }

    public void RequestClose()
    {
        ConnectionClosingCts.Cancel();
    }
}

internal class TestStreamContext : ConnectionContext, IStreamDirectionFeature, IStreamIdFeature, IProtocolErrorCodeFeature, IPersistentStateFeature, IStreamAbortFeature, IDisposable, IStreamClosedFeature
{
    private readonly record struct CloseAction(Action<object> Callback, object State);

    private readonly Http3InMemory _testBase;

    internal DuplexPipePair _pair;
    private Pipe _inputPipe;
    private Pipe _outputPipe;
    private CompletionPipeReader _transportPipeReader;
    private CompletionPipeWriter _transportPipeWriter;

    private bool _isAborted;
    private bool _isComplete;

    // Persistent state collection is not reset with a stream by design.
    private IDictionary<object, object> _persistentState;

    private TaskCompletionSource _disposingTcs;
    private TaskCompletionSource _disposedTcs;
    internal long? _error;
    private List<CloseAction> _onClosed;

    public TestStreamContext(bool canRead, bool canWrite, Http3InMemory testBase)
    {
        Features = new FeatureCollection();
        CanRead = canRead;
        CanWrite = canWrite;
        _testBase = testBase;
    }

    public void Initialize(long streamId)
    {
        if (!_isComplete)
        {
            // Create new pipes when test stream context is reused rather than reseting them.
            // This is required because the client tests read from these directly from these pipes.
            // When a request is finished they'll check to see whether there is anymore content
            // in the Application.Output pipe. If it has been reset then that code will error.
            var inputOptions = Http3InMemory.GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
            var outputOptions = Http3InMemory.GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);

            _inputPipe = new Pipe(inputOptions);
            _outputPipe = new Pipe(outputOptions);

            _transportPipeReader = new CompletionPipeReader(_inputPipe.Reader);
            _transportPipeWriter = new CompletionPipeWriter(_outputPipe.Writer);

            _pair = new DuplexPipePair(
                new DuplexPipe(_transportPipeReader, _transportPipeWriter),
                new DuplexPipe(_outputPipe.Reader, _inputPipe.Writer));
        }
        else
        {
            _pair.Application.Input.Complete();
            _pair.Application.Output.Complete();

            _transportPipeReader.Reset();
            _transportPipeWriter.Reset();

            _inputPipe.Reset();
            _outputPipe.Reset();
        }

        Features.Set<IStreamDirectionFeature>(this);
        Features.Set<IStreamIdFeature>(this);
        Features.Set<IStreamAbortFeature>(this);
        Features.Set<IProtocolErrorCodeFeature>(this);
        Features.Set<IPersistentStateFeature>(this);
        Features.Set<IStreamClosedFeature>(this);

        StreamId = streamId;
        _testBase.Logger.LogInformation($"Initializing stream {streamId}");
        ConnectionId = "TEST:" + streamId.ToString(CultureInfo.InvariantCulture);
        AbortReadException = null;
        AbortWriteException = null;
        _error = null;

        _disposedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _disposingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Disposed = false;
    }

    public TaskCompletionSource StartStreamDisposeTcs { get; internal set; }

    public ConnectionAbortedException AbortReadException { get; private set; }
    public ConnectionAbortedException AbortWriteException { get; private set; }

    public bool Disposed { get; private set; }

    public Task OnDisposingTask => _disposingTcs.Task;
    public Task OnDisposedTask => _disposedTcs.Task;

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

    public long Error
    {
        get => _error ?? -1;
        set => _error = value;
    }

    public override void Abort(ConnectionAbortedException abortReason)
    {
        _isAborted = true;
        _pair.Application.Output.Complete(abortReason);
    }

    public override async ValueTask DisposeAsync()
    {
        _disposingTcs.TrySetResult();
        if (StartStreamDisposeTcs != null)
        {
            await StartStreamDisposeTcs.Task;
        }

        _testBase.Logger.LogDebug($"Disposing stream {StreamId}");

        var readerCompletedSuccessfully = _transportPipeReader.IsCompletedSuccessfully;
        var writerCompletedSuccessfully = _transportPipeWriter.IsCompletedSuccessfully;
        CanReuse = !_isAborted &&
            readerCompletedSuccessfully &&
            writerCompletedSuccessfully;

        _pair.Transport.Input.Complete();
        _pair.Transport.Output.Complete();
    }

    public void Dispose()
    {
        if (CanReuse)
        {
            _testBase.Logger.LogDebug($"Pooling stream {StreamId} for reuse.");
            _testBase._streamContextPool.Enqueue(this);
        }
        else
        {
            // Note that completed flags could be out of date at this point.
            _testBase.Logger.LogDebug($"Can't reuse stream {StreamId}. Aborted: {_isAborted}, Reader completed successfully: {_transportPipeReader.IsCompletedSuccessfully}, Writer completed successfully: {_transportPipeWriter.IsCompletedSuccessfully}.");
        }

        Disposed = true;
        _disposedTcs.TrySetResult();
    }

    internal void Complete()
    {
        _isComplete = true;
    }

    IDictionary<object, object> IPersistentStateFeature.State
    {
        get
        {
            // Lazily allocate persistent state
            return _persistentState ?? (_persistentState = new ConnectionItems());
        }
    }

    public bool CanReuse { get; private set; }

    void IStreamAbortFeature.AbortRead(long errorCode, ConnectionAbortedException abortReason)
    {
        AbortReadException = abortReason;
    }

    void IStreamAbortFeature.AbortWrite(long errorCode, ConnectionAbortedException abortReason)
    {
        AbortWriteException = abortReason;
    }

    public void OnClosed(Action<object> callback, object state)
    {
        if (_onClosed == null)
        {
            _onClosed = new List<CloseAction>();
        }
        _onClosed.Add(new CloseAction(callback, state));
    }

    public void Close()
    {
        if (_onClosed != null)
        {
            foreach (var onClose in _onClosed)
            {
                onClose.Callback(onClose.State);
            }
        }
    }
}
