// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.HPack;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http2TestBase : TestApplicationErrorLoggerLoggedTest, IDisposable, IHttpStreamHeadersHandler
{
    protected static readonly int MaxRequestHeaderFieldSize = 16 * 1024;
    protected static readonly string _4kHeaderValue = new string('a', 4096);

    protected static readonly IEnumerable<KeyValuePair<string, string>> _browserRequestHeaders = new[]
    {
        new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
        new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
        new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        new KeyValuePair<string, string>("user-agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:54.0) Gecko/20100101 Firefox/54.0"),
        new KeyValuePair<string, string>("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"),
        new KeyValuePair<string, string>("accept-language", "en-US,en;q=0.5"),
        new KeyValuePair<string, string>("accept-encoding", "gzip, deflate, br"),
        new KeyValuePair<string, string>("upgrade-insecure-requests", "1"),
    };

    protected static readonly IEnumerable<KeyValuePair<string, string>> _postRequestHeaders = new[]
    {
        new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
        new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
        new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
    };

    protected static readonly IEnumerable<KeyValuePair<string, string>> _expectContinueRequestHeaders = new[]
    {
        new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
        new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
        new KeyValuePair<string, string>(InternalHeaderNames.Authority, "127.0.0.1"),
        new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        new KeyValuePair<string, string>(HeaderNames.Expect, "100-continue"),
    };

    protected static readonly IEnumerable<KeyValuePair<string, string>> _requestTrailers = new[]
    {
        new KeyValuePair<string, string>("trailer-one", "1"),
        new KeyValuePair<string, string>("trailer-two", "2"),
    };

    protected static readonly IEnumerable<KeyValuePair<string, string>> _oneContinuationRequestHeaders = new[]
    {
        new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
        new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
        new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        new KeyValuePair<string, string>("a", _4kHeaderValue),
        new KeyValuePair<string, string>("b", _4kHeaderValue),
        new KeyValuePair<string, string>("c", _4kHeaderValue),
        new KeyValuePair<string, string>("d", _4kHeaderValue)
    };

    protected static readonly IEnumerable<KeyValuePair<string, string>> _twoContinuationsRequestHeaders = new[]
    {
        new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
        new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
        new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        new KeyValuePair<string, string>("a", _4kHeaderValue),
        new KeyValuePair<string, string>("b", _4kHeaderValue),
        new KeyValuePair<string, string>("c", _4kHeaderValue),
        new KeyValuePair<string, string>("d", _4kHeaderValue),
        new KeyValuePair<string, string>("e", _4kHeaderValue),
        new KeyValuePair<string, string>("f", _4kHeaderValue),
        new KeyValuePair<string, string>("g", _4kHeaderValue),
    };

    protected static IEnumerable<KeyValuePair<string, string>> ReadRateRequestHeaders(int expectedBytes) => new[]
    {
        new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
        new KeyValuePair<string, string>(InternalHeaderNames.Path, "/" + expectedBytes),
        new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
    };

    protected static readonly byte[] _helloBytes = Encoding.ASCII.GetBytes("hello");
    protected static readonly byte[] _worldBytes = Encoding.ASCII.GetBytes("world");
    protected static readonly byte[] _helloWorldBytes = Encoding.ASCII.GetBytes("hello, world");
    protected static readonly byte[] _noData = new byte[0];
    protected static readonly byte[] _maxData = Encoding.ASCII.GetBytes(new string('a', Http2PeerSettings.MinAllowedMaxFrameSize));

    private readonly MemoryPool<byte> _memoryPool = PinnedBlockMemoryPoolFactory.Create();

    internal readonly Http2PeerSettings _clientSettings = new Http2PeerSettings();
    internal readonly HPackDecoder _hpackDecoder;
    internal readonly DynamicHPackEncoder _hpackEncoder;
    private readonly byte[] _headerEncodingBuffer = new byte[Http2PeerSettings.MinAllowedMaxFrameSize];

    private readonly FakeTimeProvider _timeProvider = new();
    internal readonly TimeoutControl _timeoutControl;
    protected readonly Mock<ConnectionContext> _mockConnectionContext = new Mock<ConnectionContext>();
    internal readonly Mock<ITimeoutHandler> _mockTimeoutHandler = new Mock<ITimeoutHandler>();
    internal readonly Mock<MockTimeoutControlBase> _mockTimeoutControl;

    protected readonly ConcurrentDictionary<int, TaskCompletionSource> _runningStreams = new ConcurrentDictionary<int, TaskCompletionSource>();
    protected readonly Dictionary<string, string> _receivedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    protected readonly Dictionary<string, string> _receivedTrailers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    protected readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    protected readonly RequestFields _receivedRequestFields = new RequestFields();
    protected readonly HashSet<int> _abortedStreamIds = new HashSet<int>();
    protected readonly object _abortedStreamIdsLock = new object();
    protected readonly TaskCompletionSource _closingStateReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _closedStateReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    protected readonly RequestDelegate _noopApplication;
    protected readonly RequestDelegate _notImplementedApp;
    protected readonly RequestDelegate _readHeadersApplication;
    protected readonly RequestDelegate _readTrailersApplication;
    protected readonly RequestDelegate _bufferingApplication;
    protected readonly RequestDelegate _echoApplication;
    protected readonly RequestDelegate _echoWaitForAbortApplication;
    protected readonly RequestDelegate _largeHeadersApplication;
    protected readonly RequestDelegate _waitForAbortApplication;
    protected readonly RequestDelegate _waitForAbortFlushingApplication;
    protected readonly RequestDelegate _readRateApplication;
    protected readonly RequestDelegate _echoMethodNoBody;
    protected readonly RequestDelegate _echoHost;
    protected readonly RequestDelegate _echoPath;
    protected readonly RequestDelegate _appAbort;
    protected readonly RequestDelegate _appReset;

    internal TestServiceContext _serviceContext;

    internal DuplexPipe.DuplexPipePair _pair;
    internal IConnectionMetricsTagsFeature _metricsTagsFeature;
    internal IConnectionMetricsContextFeature _metricsContextFeature;
    internal Http2Connection _connection;
    protected Task _connectionTask;
    protected long _bytesReceived;

    internal Dictionary<string, object> ConnectionTags => _metricsTagsFeature.Tags.ToDictionary(t => t.Key, t => t.Value);
    internal ConnectionMetricsContext MetricsContext => _metricsContextFeature.MetricsContext;

    public Http2TestBase()
    {
        _hpackDecoder = new HPackDecoder((int)_clientSettings.HeaderTableSize, MaxRequestHeaderFieldSize);
        _hpackEncoder = new DynamicHPackEncoder();

        _timeoutControl = new TimeoutControl(_mockTimeoutHandler.Object, _timeProvider);
        _mockTimeoutControl = new Mock<MockTimeoutControlBase>(_timeoutControl) { CallBase = true };
        _timeoutControl.Debugger = Mock.Of<IDebugger>();

        _mockConnectionContext.Setup(c => c.Abort(It.IsAny<ConnectionAbortedException>())).Callback<ConnectionAbortedException>(ex =>
        {
            // Emulate transport abort so the _connectionTask completes.
            Task.Run(() =>
        {
            Logger.LogInformation(0, ex, "ConnectionContext.Abort() was called. Completing _pair.Application.Output.");
            _pair.Application.Output.Complete(ex);
        });
        });

        _noopApplication = context => Task.CompletedTask;
        _notImplementedApp = _ => throw new NotImplementedException();

        _readHeadersApplication = context =>
        {
            _receivedRequestFields.Method = context.Request.Method;
            _receivedRequestFields.Scheme = context.Request.Scheme;
            _receivedRequestFields.Path = context.Request.Path.Value;
            _receivedRequestFields.RawTarget = context.Features.Get<IHttpRequestFeature>().RawTarget;
            _receivedRequestFields.Authority = context.Request.Host.Value;
            foreach (var header in context.Request.Headers)
            {
                _receivedHeaders[header.Key] = header.Value.ToString();
            }

            return Task.CompletedTask;
        };

        _readTrailersApplication = async context =>
        {
            Assert.True(context.Request.SupportsTrailers(), "SupportsTrailers");
            Assert.False(context.Request.CheckTrailersAvailable(), "SupportsTrailers");

            using (var ms = new MemoryStream())
            {
                // Consuming the entire request body guarantees trailers will be available
                await context.Request.Body.CopyToAsync(ms);
            }

            Assert.True(context.Request.SupportsTrailers(), "SupportsTrailers");
            Assert.True(context.Request.CheckTrailersAvailable(), "SupportsTrailers");

            _receivedRequestFields.Method = context.Request.Method;
            _receivedRequestFields.Scheme = context.Request.Scheme;
            _receivedRequestFields.Path = context.Request.Path.Value;
            _receivedRequestFields.RawTarget = context.Features.Get<IHttpRequestFeature>().RawTarget;
            foreach (var header in context.Request.Headers)
            {
                _receivedHeaders[header.Key] = header.Value.ToString();
            }

            var trailers = context.Features.Get<IHttpRequestTrailersFeature>().Trailers;

            foreach (var header in trailers)
            {
                _receivedTrailers[header.Key] = header.Value.ToString();
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
            var buffer = new byte[Http2PeerSettings.MinAllowedMaxFrameSize];
            var received = 0;

            while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await context.Response.Body.WriteAsync(buffer, 0, received);
            }
        };

        _echoWaitForAbortApplication = async context =>
        {
            var buffer = new byte[Http2PeerSettings.MinAllowedMaxFrameSize];
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

            await sem.WaitAsync().DefaultTimeout();
        };

        _largeHeadersApplication = context =>
        {
            foreach (var name in new[] { "a", "b", "c", "d", "e", "f", "g", "h" })
            {
                context.Response.Headers[name] = _4kHeaderValue;
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

            await sem.WaitAsync().DefaultTimeout();

            _runningStreams[streamIdFeature.StreamId].TrySetResult();
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

            _runningStreams[streamIdFeature.StreamId].TrySetResult();
        };

        _readRateApplication = async context =>
        {
            var expectedBytes = int.Parse(context.Request.Path.Value.Substring(1), CultureInfo.InvariantCulture);

            var buffer = new byte[Http2PeerSettings.MinAllowedMaxFrameSize];
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

        _echoMethodNoBody = context =>
        {
            Assert.False(context.Request.CanHaveBody());
            Assert.False(context.Request.Headers.ContainsKey(InternalHeaderNames.Method));
            context.Response.Headers["Method"] = context.Request.Method;

            return Task.CompletedTask;
        };

        _echoHost = context =>
        {
            Assert.False(context.Request.Headers.ContainsKey(InternalHeaderNames.Authority));
            context.Response.Headers.Host = context.Request.Headers.Host;

            return Task.CompletedTask;
        };

        _echoPath = context =>
        {
            Assert.False(context.Request.Headers.ContainsKey(InternalHeaderNames.Path));
            context.Response.Headers["path"] = context.Request.Path.ToString();
            context.Response.Headers["rawtarget"] = context.Features.Get<IHttpRequestFeature>().RawTarget;

            return Task.CompletedTask;
        };

        _appAbort = context =>
        {
            context.Abort();
            return Task.CompletedTask;
        };

        _appReset = context =>
        {
            var resetFeature = context.Features.Get<IHttpResetFeature>();
            Assert.NotNull(resetFeature);
            resetFeature.Reset((int)Http2ErrorCode.CANCEL);
            return Task.CompletedTask;
        };
    }

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

        _serviceContext = new TestServiceContext(LoggerFactory)
        {
            Scheduler = PipeScheduler.Inline,
            FakeTimeProvider = _timeProvider,
            TimeProvider = _timeProvider,
        };

        TestSink.MessageLogged += context =>
        {
            if (context.EventId.Name == "Http2ConnectionClosing")
            {
                _closingStateReached.SetResult();
            }
            if (context.EventId.Name == "Http2ConnectionClosed")
            {
                _closedStateReached.SetResult();
            }
        };
    }

    public override void Dispose()
    {
        _pair.Application?.Input.Complete();
        _pair.Application?.Output.Complete();
        _pair.Transport?.Input.Complete();
        _pair.Transport?.Output.Complete();
        _memoryPool.Dispose();

        base.Dispose();
    }

    internal void AssertConnectionNoError()
    {
        MetricsAssert.NoError(ConnectionTags);
    }

    internal void AssertConnectionEndReason(ConnectionEndReason expectedEndReason)
    {
        Assert.Equal(expectedEndReason, MetricsContext.ConnectionEndReason);
    }

    void IHttpStreamHeadersHandler.OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        var nameStr = name.GetHeaderName();
        _decodedHeaders[nameStr] = value.GetRequestHeaderString(nameStr, _serviceContext.ServerOptions.RequestHeaderEncodingSelector, checkForNewlineChars: true);
    }

    public void OnStaticIndexedHeader(int index)
    {
        Debug.Assert(index <= H2StaticTable.Count);

        ref readonly var entry = ref H2StaticTable.Get(index - 1);
        ((IHttpStreamHeadersHandler)this).OnHeader(entry.Name, entry.Value);
    }

    public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
    {
        Debug.Assert(index <= H2StaticTable.Count);

        ((IHttpStreamHeadersHandler)this).OnHeader(H2StaticTable.Get(index - 1).Name, value);
    }

    void IHttpStreamHeadersHandler.OnHeadersComplete(bool endStream) { }

    void IHttpStreamHeadersHandler.OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        ((IHttpStreamHeadersHandler)this).OnHeader(name, value);
    }

    protected void CreateConnection()
    {
        // Always dispatch test code back to the ThreadPool. This prevents deadlocks caused by continuing
        // Http2Connection.ProcessRequestsAsync() loop with writer locks acquired. Run product code inline to make
        // it easier to verify request frames are processed correctly immediately after sending the them.
        var inputPipeOptions = GetInputPipeOptions(_serviceContext, _memoryPool, PipeScheduler.ThreadPool);
        var outputPipeOptions = GetOutputPipeOptions(_serviceContext, _memoryPool, PipeScheduler.ThreadPool);

        _pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);

        _metricsTagsFeature = new TestConnectionMetricsTagsFeature();

        var metricsContext = TestContextFactory.CreateMetricsContext(_mockConnectionContext.Object);
        _metricsContextFeature = new TestConnectionMetricsContextFeature() { MetricsContext = metricsContext };

        var features = new FeatureCollection();
        features.Set<IConnectionMetricsContextFeature>(_metricsContextFeature);
        features.Set<IConnectionMetricsTagsFeature>(_metricsTagsFeature);
        _mockConnectionContext.Setup(x => x.Features).Returns(features);
        var httpConnectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: _serviceContext,
            connectionContext: _mockConnectionContext.Object,
            transport: _pair.Transport,
            memoryPool: _memoryPool,
            connectionFeatures: features,
            timeoutControl: _mockTimeoutControl.Object,
            metricsContext: metricsContext);

        _connection = new Http2Connection(httpConnectionContext);
        _connection._streamLifetimeHandler = new LifetimeHandlerInterceptor(_connection._streamLifetimeHandler, this);

        var httpConnection = new HttpConnection(httpConnectionContext);
        httpConnection.Initialize(_connection);
        _mockTimeoutHandler.Setup(h => h.OnTimeout(It.IsAny<TimeoutReason>()))
                           .Callback<TimeoutReason>(r => httpConnection.OnTimeout(r));

        _timeoutControl.Initialize();
    }

    private sealed class TestConnectionMetricsTagsFeature : IConnectionMetricsTagsFeature
    {
        public ICollection<KeyValuePair<string, object>> Tags { get; } = new List<KeyValuePair<string, object>>();
    }

    private class TestConnectionMetricsContextFeature : IConnectionMetricsContextFeature
    {
        public ConnectionMetricsContext MetricsContext { get; init; }
    }

    private class LifetimeHandlerInterceptor : IHttp2StreamLifetimeHandler
    {
        private readonly IHttp2StreamLifetimeHandler _inner;
        private readonly Http2TestBase _httpTestBase;

        public LifetimeHandlerInterceptor(IHttp2StreamLifetimeHandler inner, Http2TestBase httpTestBase)
        {
            _inner = inner;
            _httpTestBase = httpTestBase;
        }

        public void DecrementActiveClientStreamCount()
        {
            _inner.DecrementActiveClientStreamCount();
        }

        public void OnStreamCompleted(Http2Stream stream)
        {
            _inner.OnStreamCompleted(stream);

            // Stream in test might not have been started with StartStream method.
            // In that case there isn't a record of a running stream.
            if (_httpTestBase._runningStreams.TryGetValue(stream.StreamId, out var tcs))
            {
                tcs.TrySetResult();
            }
        }
    }

    protected void InitializeConnectionWithoutPreface(RequestDelegate application, bool addKestrelFeatures = false)
    {
        if (_connection == null)
        {
            CreateConnection();
        }
        if (addKestrelFeatures)
        {
            AddKestrelConnection();
        }

        var connectionTask = _connection.ProcessRequestsAsync(new DummyApplication(application));

        async Task CompletePipeOnTaskCompletion()
        {
            try
            {
                await connectionTask;
            }
            finally
            {
                _pair.Transport.Input.Complete();
                _pair.Transport.Output.Complete();
            }
        }

        _connectionTask = CompletePipeOnTaskCompletion();
    }

    protected async Task InitializeConnectionAsync(RequestDelegate application, int expectedSettingsCount = 4, bool expectedWindowUpdate = true, bool addKestrelFeatures = false)
    {
        InitializeConnectionWithoutPreface(application, addKestrelFeatures);

        // Lose xUnit's AsyncTestSyncContext so middleware always runs inline for better determinism.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        await SendPreambleAsync();
        await SendSettingsAsync();

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: expectedSettingsCount * Http2FrameReader.SettingSize,
            withFlags: 0,
            withStreamId: 0);

        if (expectedWindowUpdate)
        {
            await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
                withLength: 4,
                withFlags: 0,
                withStreamId: 0);
        }

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);
    }

    protected void AddKestrelConnection()
    {
        new KestrelConnection<BaseConnectionContext>(
            0,
            _serviceContext,
            new TransportConnectionManager(_serviceContext.ConnectionManager),
            _ => throw new NotImplementedException($"{nameof(_connection.ProcessRequestsAsync)} should invoked instead - hence transport connection manager does not have the connection registered."),
            _mockConnectionContext.Object,
            new KestrelTrace(_serviceContext.LoggerFactory),
            TestContextFactory.CreateMetricsContext(_mockConnectionContext.Object));
    }

    protected Task StartStreamAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, bool endStream, bool flushFrame = true)
    {
        var writableBuffer = _pair.Application.Output;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runningStreams[streamId] = tcs;

        writableBuffer.WriteStartStream(streamId, _hpackEncoder, GetHeadersEnumerator(headers), _headerEncodingBuffer, endStream);

        if (flushFrame)
        {
            return FlushAsync(writableBuffer);
        }
        return Task.CompletedTask;
    }

    protected Task StartStreamAsync(int streamId, Span<byte> headerData, bool endStream)
    {
        var writableBuffer = _pair.Application.Output;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runningStreams[streamId] = tcs;

        writableBuffer.WriteStartStream(streamId, headerData, endStream);
        return FlushAsync(writableBuffer);
    }

    /* https://tools.ietf.org/html/rfc7540#section-6.2
        +---------------+
        |Pad Length? (8)|
        +-+-------------+-----------------------------------------------+
        |                   Header Block Fragment (*)                 ...
        +---------------------------------------------------------------+
        |                           Padding (*)                       ...
        +---------------------------------------------------------------+
    */
    protected Task SendHeadersWithPaddingAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte padLength, bool endStream)
    {
        var writableBuffer = _pair.Application.Output;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runningStreams[streamId] = tcs;

        var frame = new Http2Frame();

        frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PADDED, streamId);
        frame.HeadersPadLength = padLength;

        var extendedHeaderLength = 1; // Padding length field
        var buffer = _headerEncodingBuffer.AsSpan();
        var extendedHeader = buffer.Slice(0, extendedHeaderLength);
        extendedHeader[0] = padLength;
        var payload = buffer.Slice(extendedHeaderLength, buffer.Length - padLength - extendedHeaderLength);

        HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, GetHeadersEnumerator(headers), payload, out var length);
        var padding = buffer.Slice(extendedHeaderLength + length, padLength);
        padding.Clear();

        frame.PayloadLength = extendedHeaderLength + length + padLength;

        if (endStream)
        {
            frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
        }

        Http2FrameWriter.WriteHeader(frame, writableBuffer);
        writableBuffer.Write(buffer.Slice(0, frame.PayloadLength));
        return FlushAsync(writableBuffer);
    }

    /* https://tools.ietf.org/html/rfc7540#section-6.2
        +-+-------------+-----------------------------------------------+
        |E|                 Stream Dependency? (31)                     |
        +-+-------------+-----------------------------------------------+
        |  Weight? (8)  |
        +-+-------------+-----------------------------------------------+
        |                   Header Block Fragment (*)                 ...
        +---------------------------------------------------------------+
    */
    protected Task SendHeadersWithPriorityAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte priority, int streamDependency, bool endStream)
    {
        var writableBuffer = _pair.Application.Output;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runningStreams[streamId] = tcs;

        var frame = new Http2Frame();
        frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PRIORITY, streamId);
        frame.HeadersPriorityWeight = priority;
        frame.HeadersStreamDependency = streamDependency;

        var extendedHeaderLength = 5; // stream dependency + weight
        var buffer = _headerEncodingBuffer.AsSpan();
        var extendedHeader = buffer.Slice(0, extendedHeaderLength);
        Bitshifter.WriteUInt31BigEndian(extendedHeader, (uint)streamDependency);
        extendedHeader[4] = priority;
        var payload = buffer.Slice(extendedHeaderLength);

        HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, GetHeadersEnumerator(headers), payload, out var length);

        frame.PayloadLength = extendedHeaderLength + length;

        if (endStream)
        {
            frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
        }

        Http2FrameWriter.WriteHeader(frame, writableBuffer);
        writableBuffer.Write(buffer.Slice(0, frame.PayloadLength));
        return FlushAsync(writableBuffer);
    }

    /* https://tools.ietf.org/html/rfc7540#section-6.2
        +---------------+
        |Pad Length? (8)|
        +-+-------------+-----------------------------------------------+
        |E|                 Stream Dependency? (31)                     |
        +-+-------------+-----------------------------------------------+
        |  Weight? (8)  |
        +-+-------------+-----------------------------------------------+
        |                   Header Block Fragment (*)                 ...
        +---------------------------------------------------------------+
        |                           Padding (*)                       ...
        +---------------------------------------------------------------+
    */
    protected Task SendHeadersWithPaddingAndPriorityAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte padLength, byte priority, int streamDependency, bool endStream)
    {
        var writableBuffer = _pair.Application.Output;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runningStreams[streamId] = tcs;

        var frame = new Http2Frame();
        frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PADDED | Http2HeadersFrameFlags.PRIORITY, streamId);
        frame.HeadersPadLength = padLength;
        frame.HeadersPriorityWeight = priority;
        frame.HeadersStreamDependency = streamDependency;

        var extendedHeaderLength = 6; // pad length + stream dependency + weight
        var buffer = _headerEncodingBuffer.AsSpan();
        var extendedHeader = buffer.Slice(0, extendedHeaderLength);
        extendedHeader[0] = padLength;
        Bitshifter.WriteUInt31BigEndian(extendedHeader.Slice(1), (uint)streamDependency);
        extendedHeader[5] = priority;
        var payload = buffer.Slice(extendedHeaderLength, buffer.Length - padLength - extendedHeaderLength);

        HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, GetHeadersEnumerator(headers), payload, out var length);
        var padding = buffer.Slice(extendedHeaderLength + length, padLength);
        padding.Clear();

        frame.PayloadLength = extendedHeaderLength + length + padLength;

        if (endStream)
        {
            frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
        }

        Http2FrameWriter.WriteHeader(frame, writableBuffer);
        writableBuffer.Write(buffer.Slice(0, frame.PayloadLength));
        return FlushAsync(writableBuffer);
    }

    protected Task WaitForStreamAsync(int streamId)
    {
        return _runningStreams[streamId].Task;
    }

    protected Task WaitForAllStreamsAsync()
    {
        return Task.WhenAll(_runningStreams.Values.Select(tcs => tcs.Task)).DefaultTimeout();
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

    protected Task SendPreambleAsync() => SendAsync(Http2Connection.ClientPreface);

    protected async Task SendSettingsAsync()
    {
        _pair.Application.Output.WriteSettings(_clientSettings);
        await FlushAsync(_pair.Application.Output);
    }

    protected async Task SendSettingsAckWithInvalidLengthAsync(int length)
    {
        var writableBuffer = _pair.Application.Output;
        var frame = new Http2Frame();
        frame.PrepareSettings(Http2SettingsFrameFlags.ACK);
        frame.PayloadLength = length;
        Http2FrameWriter.WriteHeader(frame, writableBuffer);
        await SendAsync(new byte[length]);
    }

    protected async Task SendSettingsWithInvalidStreamIdAsync(int streamId)
    {
        var writableBuffer = _pair.Application.Output;
        var frame = new Http2Frame();
        frame.PrepareSettings(Http2SettingsFrameFlags.NONE);
        frame.StreamId = streamId;
        var settings = _clientSettings.GetNonProtocolDefaults();
        var payload = new byte[settings.Count * Http2FrameReader.SettingSize];
        frame.PayloadLength = payload.Length;
        Http2FrameWriter.WriteSettings(settings, payload);
        Http2FrameWriter.WriteHeader(frame, writableBuffer);
        await SendAsync(payload);
    }

    protected async Task SendSettingsWithInvalidLengthAsync(int length)
    {
        var writableBuffer = _pair.Application.Output;
        var frame = new Http2Frame();
        frame.PrepareSettings(Http2SettingsFrameFlags.NONE);

        frame.PayloadLength = length;
        var payload = new byte[length];
        Http2FrameWriter.WriteHeader(frame, writableBuffer);
        await SendAsync(payload);
    }

    internal async Task SendSettingsWithInvalidParameterValueAsync(Http2SettingsParameter parameter, uint value)
    {
        var writableBuffer = _pair.Application.Output;
        var frame = new Http2Frame();
        frame.PrepareSettings(Http2SettingsFrameFlags.NONE);
        frame.PayloadLength = 6;
        var payload = new byte[Http2FrameReader.SettingSize];
        payload[0] = (byte)((ushort)parameter >> 8);
        payload[1] = (byte)(ushort)parameter;
        payload[2] = (byte)(value >> 24);
        payload[3] = (byte)(value >> 16);
        payload[4] = (byte)(value >> 8);
        payload[5] = (byte)value;

        Http2FrameWriter.WriteHeader(frame, writableBuffer);
        await SendAsync(payload);
    }

    protected Task SendPushPromiseFrameAsync()
    {
        var writableBuffer = _pair.Application.Output;
        var frame = new Http2Frame();
        frame.PayloadLength = 0;
        frame.Type = Http2FrameType.PUSH_PROMISE;
        frame.StreamId = 1;

        Http2FrameWriter.WriteHeader(frame, writableBuffer);
        return FlushAsync(writableBuffer);
    }

    internal async Task<bool> SendHeadersAsync(int streamId, Http2HeadersFrameFlags flags, Http2HeadersEnumerator headersEnumerator)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareHeaders(flags, streamId);
        var buffer = _headerEncodingBuffer.AsMemory();
        var done = HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, headersEnumerator, buffer.Span, out var length);
        frame.PayloadLength = length;

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        await SendAsync(buffer.Span.Slice(0, length));

        return done == HeaderWriteResult.Done;
    }

    internal Task<bool> SendHeadersAsync(int streamId, Http2HeadersFrameFlags flags, IEnumerable<KeyValuePair<string, string>> headers)
    {
        return SendHeadersAsync(streamId, flags, GetHeadersEnumerator(headers));
    }

    internal async Task SendHeadersAsync(int streamId, Http2HeadersFrameFlags flags, byte[] headerBlock)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareHeaders(flags, streamId);
        frame.PayloadLength = headerBlock.Length;

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        await SendAsync(headerBlock);
    }

    protected async Task SendInvalidHeadersFrameAsync(int streamId, int payloadLength, byte padLength)
    {
        Assert.True(padLength >= payloadLength, $"{nameof(padLength)} must be greater than or equal to {nameof(payloadLength)} to create an invalid frame.");

        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareHeaders(Http2HeadersFrameFlags.PADDED, streamId);
        frame.PayloadLength = payloadLength;
        var payload = new byte[payloadLength];
        if (payloadLength > 0)
        {
            payload[0] = padLength;
        }

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        await SendAsync(payload);
    }

    protected async Task SendIncompleteHeadersFrameAsync(int streamId)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS, streamId);
        frame.PayloadLength = 3;
        var payload = new byte[3];
        // Set up an incomplete Literal Header Field w/ Incremental Indexing frame,
        // with an incomplete new name
        payload[0] = 0;
        payload[1] = 2;
        payload[2] = (byte)'a';

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        await SendAsync(payload);
    }

    internal async Task<bool> SendContinuationAsync(int streamId, Http2ContinuationFrameFlags flags, Http2HeadersEnumerator headersEnumerator)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareContinuation(flags, streamId);
        var buffer = _headerEncodingBuffer.AsMemory();
        var done = HPackHeaderWriter.ContinueEncodeHeaders(_hpackEncoder, headersEnumerator, buffer.Span, out var length);
        frame.PayloadLength = length;

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        await SendAsync(buffer.Span.Slice(0, length));

        return done == HeaderWriteResult.Done;
    }

    internal async Task SendContinuationAsync(int streamId, Http2ContinuationFrameFlags flags, byte[] payload)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareContinuation(flags, streamId);
        frame.PayloadLength = payload.Length;

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        await SendAsync(payload);
    }

    internal async Task<bool> SendContinuationAsync(int streamId, Http2ContinuationFrameFlags flags, IEnumerable<KeyValuePair<string, string>> headers)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareContinuation(flags, streamId);
        var buffer = _headerEncodingBuffer.AsMemory();
        var done = HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, GetHeadersEnumerator(headers), buffer.Span, out var length);
        frame.PayloadLength = length;

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        await SendAsync(buffer.Span.Slice(0, length));

        return done == HeaderWriteResult.Done;
    }

    internal Http2HeadersEnumerator GetHeadersEnumerator(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var dictionary = headers
            .GroupBy(g => g.Key)
            .ToDictionary(g => g.Key, g => new StringValues(g.Select(values => values.Value).ToArray()));

        var headersEnumerator = new Http2HeadersEnumerator();
        headersEnumerator.Initialize(dictionary);
        return headersEnumerator;
    }

    internal Task SendEmptyContinuationFrameAsync(int streamId, Http2ContinuationFrameFlags flags)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareContinuation(flags, streamId);
        frame.PayloadLength = 0;

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        return FlushAsync(outputWriter);
    }

    protected async Task SendIncompleteContinuationFrameAsync(int streamId)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareContinuation(Http2ContinuationFrameFlags.END_HEADERS, streamId);
        frame.PayloadLength = 3;
        var payload = new byte[3];
        // Set up an incomplete Literal Header Field w/ Incremental Indexing frame,
        // with an incomplete new name
        payload[0] = 0;
        payload[1] = 2;
        payload[2] = (byte)'a';

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        await SendAsync(payload);
    }

    protected Task SendDataAsync(int streamId, Memory<byte> data, bool endStream, bool flushFrame = true)
    {
        var outputWriter = _pair.Application.Output;
        outputWriter.WriteData(streamId, data, endStream);
        if (flushFrame)
        {
            return FlushAsync(outputWriter);
        }
        return Task.CompletedTask;
    }

    protected async Task SendDataWithPaddingAsync(int streamId, Memory<byte> data, byte padLength, bool endStream)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareData(streamId, padLength);
        frame.PayloadLength = data.Length + 1 + padLength;

        if (endStream)
        {
            frame.DataFlags |= Http2DataFrameFlags.END_STREAM;
        }

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        outputWriter.GetSpan(1)[0] = padLength;
        outputWriter.Advance(1);
        await SendAsync(data.Span);
        await SendAsync(new byte[padLength]);
    }

    protected Task SendInvalidDataFrameAsync(int streamId, int frameLength, byte padLength)
    {
        Assert.True(padLength >= frameLength, $"{nameof(padLength)} must be greater than or equal to {nameof(frameLength)} to create an invalid frame.");

        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareData(streamId);
        frame.DataFlags = Http2DataFrameFlags.PADDED;
        frame.PayloadLength = frameLength;
        var payload = new byte[frameLength];
        if (frameLength > 0)
        {
            payload[0] = padLength;
        }

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        return SendAsync(payload);
    }

    internal Task SendPingAsync(Http2PingFrameFlags flags)
    {
        var outputWriter = _pair.Application.Output;
        var pingFrame = new Http2Frame();
        pingFrame.PreparePing(flags);
        Http2FrameWriter.WriteHeader(pingFrame, outputWriter);
        return SendAsync(new byte[8]); // Empty payload
    }

    protected Task SendPingWithInvalidLengthAsync(int length)
    {
        var outputWriter = _pair.Application.Output;
        var pingFrame = new Http2Frame();
        pingFrame.PreparePing(Http2PingFrameFlags.NONE);
        pingFrame.PayloadLength = length;
        Http2FrameWriter.WriteHeader(pingFrame, outputWriter);
        return SendAsync(new byte[length]);
    }

    protected Task SendPingWithInvalidStreamIdAsync(int streamId)
    {
        Assert.NotEqual(0, streamId);

        var outputWriter = _pair.Application.Output;
        var pingFrame = new Http2Frame();
        pingFrame.PreparePing(Http2PingFrameFlags.NONE);
        pingFrame.StreamId = streamId;
        Http2FrameWriter.WriteHeader(pingFrame, outputWriter);
        return SendAsync(new byte[pingFrame.PayloadLength]);
    }

    /* https://tools.ietf.org/html/rfc7540#section-6.3
        +-+-------------------------------------------------------------+
        |E|                  Stream Dependency (31)                     |
        +-+-------------+-----------------------------------------------+
        |   Weight (8)  |
        +-+-------------+
    */
    protected Task SendPriorityAsync(int streamId, int streamDependency = 0)
    {
        var outputWriter = _pair.Application.Output;
        var priorityFrame = new Http2Frame();
        priorityFrame.PreparePriority(streamId, streamDependency: streamDependency, exclusive: false, weight: 0);

        var payload = new byte[priorityFrame.PayloadLength].AsSpan();
        Bitshifter.WriteUInt31BigEndian(payload, (uint)streamDependency);
        payload[4] = 0; // Weight

        Http2FrameWriter.WriteHeader(priorityFrame, outputWriter);
        return SendAsync(payload);
    }

    protected Task SendInvalidPriorityFrameAsync(int streamId, int length)
    {
        var outputWriter = _pair.Application.Output;
        var priorityFrame = new Http2Frame();
        priorityFrame.PreparePriority(streamId, streamDependency: 0, exclusive: false, weight: 0);
        priorityFrame.PayloadLength = length;

        Http2FrameWriter.WriteHeader(priorityFrame, outputWriter);
        return SendAsync(new byte[length]);
    }

    /* https://tools.ietf.org/html/rfc7540#section-6.4
        +---------------------------------------------------------------+
        |                        Error Code (32)                        |
        +---------------------------------------------------------------+
    */
    protected Task SendRstStreamAsync(int streamId)
    {
        var outputWriter = _pair.Application.Output;
        var rstStreamFrame = new Http2Frame();
        rstStreamFrame.PrepareRstStream(streamId, Http2ErrorCode.CANCEL);
        var payload = new byte[rstStreamFrame.PayloadLength];
        BinaryPrimitives.WriteUInt32BigEndian(payload, (uint)Http2ErrorCode.CANCEL);

        Http2FrameWriter.WriteHeader(rstStreamFrame, outputWriter);
        return SendAsync(payload);
    }

    protected Task SendInvalidRstStreamFrameAsync(int streamId, int length)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();
        frame.PrepareRstStream(streamId, Http2ErrorCode.CANCEL);
        frame.PayloadLength = length;
        Http2FrameWriter.WriteHeader(frame, outputWriter);
        return SendAsync(new byte[length]);
    }

    protected Task SendGoAwayAsync()
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();
        frame.PrepareGoAway(0, Http2ErrorCode.NO_ERROR);
        Http2FrameWriter.WriteHeader(frame, outputWriter);
        return SendAsync(new byte[frame.PayloadLength]);
    }

    protected Task SendInvalidGoAwayFrameAsync()
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();
        frame.PrepareGoAway(0, Http2ErrorCode.NO_ERROR);
        frame.StreamId = 1;
        Http2FrameWriter.WriteHeader(frame, outputWriter);
        return SendAsync(new byte[frame.PayloadLength]);
    }

    protected Task SendWindowUpdateAsync(int streamId, int sizeIncrement)
    {
        var outputWriter = _pair.Application.Output;
        outputWriter.WriteWindowUpdateAsync(streamId, sizeIncrement);
        return FlushAsync(outputWriter);
    }

    protected Task SendInvalidWindowUpdateAsync(int streamId, int sizeIncrement, int length)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();
        frame.PrepareWindowUpdate(streamId, sizeIncrement);
        frame.PayloadLength = length;
        Http2FrameWriter.WriteHeader(frame, outputWriter);
        return SendAsync(new byte[length]);
    }

    protected Task SendUnknownFrameTypeAsync(int streamId, int frameType)
    {
        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();
        frame.StreamId = streamId;
        frame.Type = (Http2FrameType)frameType;
        frame.PayloadLength = 0;
        Http2FrameWriter.WriteHeader(frame, outputWriter);
        return FlushAsync(outputWriter);
    }

    internal async Task<byte[]> ReadAllAsync()
    {
        while (true)
        {
            var result = await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout();

            if (result.IsCompleted)
            {
                return result.Buffer.ToArray();
            }

            // Consume nothing, just wait for everything
            _pair.Application.Input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
        }
    }

    internal async Task<Http2FrameWithPayload> ReceiveFrameAsync(uint maxFrameSize = uint.MaxValue)
    {
        var frame = new Http2FrameWithPayload();

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

                if (Http2FrameReader.TryReadFrame(ref buffer, frame, maxFrameSize, out var framePayload))
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

    internal async Task<Http2FrameWithPayload> ExpectAsync(Http2FrameType type, int withLength, byte withFlags, int withStreamId)
    {
        var frame = await ReceiveFrameAsync();

        Assert.Equal(type, frame.Type);
        Assert.Equal(withStreamId, frame.StreamId);
        Assert.Equal(withFlags, frame.Flags);
        Assert.Equal(withLength, frame.PayloadLength);

        return frame;
    }

    protected Task StopConnectionAsync(int expectedLastStreamId, bool ignoreNonGoAwayFrames)
    {
        _pair.Application.Output.Complete();

        return WaitForConnectionStopAsync(expectedLastStreamId, ignoreNonGoAwayFrames);
    }

    protected Task WaitForConnectionStopAsync(int expectedLastStreamId, bool ignoreNonGoAwayFrames)
    {
        return WaitForConnectionErrorAsync<Exception>(ignoreNonGoAwayFrames, expectedLastStreamId, Http2ErrorCode.NO_ERROR, expectedErrorMessage: null);
    }

    internal void VerifyGoAway(Http2Frame frame, int expectedLastStreamId, Http2ErrorCode expectedErrorCode)
    {
        Assert.Equal(Http2FrameType.GOAWAY, frame.Type);
        Assert.Equal(8, frame.PayloadLength);
        Assert.Equal(0, frame.Flags);
        Assert.Equal(0, frame.StreamId);
        Assert.Equal(expectedLastStreamId, frame.GoAwayLastStreamId);
        Assert.Equal(expectedErrorCode, frame.GoAwayErrorCode);
    }

    internal async Task WaitForConnectionErrorAsync<TException>(bool ignoreNonGoAwayFrames, int expectedLastStreamId, Http2ErrorCode expectedErrorCode, params string[] expectedErrorMessage)
        where TException : Exception
    {
        await WaitForConnectionErrorAsyncDoNotCloseTransport<TException>(ignoreNonGoAwayFrames, expectedLastStreamId, expectedErrorCode, expectedErrorMessage);
        _pair.Application.Output.Complete();
    }

    internal async Task WaitForConnectionErrorAsyncDoNotCloseTransport<TException>(bool ignoreNonGoAwayFrames, int expectedLastStreamId, Http2ErrorCode expectedErrorCode, params string[] expectedErrorMessage)
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

        VerifyGoAway(frame, expectedLastStreamId, expectedErrorCode);

        if (expectedErrorMessage?.Length > 0)
        {
            var message = Assert.Single(LogMessages, m => m.Exception is TException);

            Assert.Contains(expectedErrorMessage, expected => message.Exception.Message.Contains(expected));
        }

        Logger.LogInformation("Waiting for Connection task");
        await _connectionTask.DefaultTimeout();
        Logger.LogInformation("Stopping Connection From ConnectionErrorAsync");
    }

    internal async Task WaitForStreamErrorAsync(int expectedStreamId, Http2ErrorCode expectedErrorCode, string expectedErrorMessage)
    {
        var frame = await ReceiveFrameAsync();

        Assert.Equal(Http2FrameType.RST_STREAM, frame.Type);
        Assert.Equal(4, frame.PayloadLength);
        Assert.Equal(0, frame.Flags);
        Assert.Equal(expectedStreamId, frame.StreamId);
        Assert.Equal(expectedErrorCode, frame.RstStreamErrorCode);

        if (expectedErrorMessage != null)
        {
            Assert.Contains(LogMessages, m => m.Exception?.Message.Contains(expectedErrorMessage) ?? false);
        }
    }

    protected void VerifyDecodedRequestHeaders(IEnumerable<KeyValuePair<string, string>> expectedHeaders)
    {
        foreach (var header in expectedHeaders)
        {
            if (header.Key == InternalHeaderNames.Method)
            {
                Assert.Equal(header.Value, _receivedRequestFields.Method);
            }
            else if (header.Key == InternalHeaderNames.Authority)
            {
                Assert.True(_receivedHeaders.TryGetValue(HeaderNames.Host, out var host), header.Key);
                Assert.Equal(header.Value, host);
            }
            else if (header.Key == InternalHeaderNames.Scheme)
            {
                Assert.Equal(header.Value, _receivedRequestFields.Scheme);
            }
            else if (header.Key == InternalHeaderNames.Path)
            {
                Assert.Equal(header.Value, _receivedRequestFields.RawTarget);
            }
            else
            {
                Assert.True(_receivedHeaders.TryGetValue(header.Key, out var value), header.Key);
                Assert.Equal(header.Value, value, ignoreCase: true);
            }
        }
    }

    protected void TriggerTick(TimeSpan timeSpan)
    {
        _serviceContext.FakeTimeProvider.Advance(timeSpan);
        TriggerTick();
    }

    protected void TriggerTick()
    {
        var timestamp = _serviceContext.FakeTimeProvider.GetTimestamp();
        _timeoutControl.Tick(timestamp);
        ((IRequestProcessor)_connection)?.Tick(timestamp);
    }

    protected void AdvanceTime(TimeSpan timeSpan)
    {
        var timeProvider = _serviceContext.FakeTimeProvider;
        var endTime = timeProvider.GetTimestamp(timeSpan);

        while (timeProvider.GetTimestamp(Heartbeat.Interval) < endTime)
        {
            TriggerTick(Heartbeat.Interval);
        }

        timeProvider.Advance(timeProvider.GetElapsedTime(timeProvider.GetTimestamp(), endTime));
        TriggerTick();
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

    internal class Http2FrameWithPayload : Http2Frame
    {
        public Http2FrameWithPayload() : base()
        {
        }

        // This does not contain extended headers
        public Memory<byte> Payload { get; set; }

        public ReadOnlySequence<byte> PayloadSequence => new ReadOnlySequence<byte>(Payload);
    }

    internal class MockTimeoutControlBase : ITimeoutControl
    {
        private readonly ITimeoutControl _realTimeoutControl;

        public MockTimeoutControlBase(ITimeoutControl realTimeoutControl)
        {
            _realTimeoutControl = realTimeoutControl;
        }

        public virtual TimeoutReason TimerReason => _realTimeoutControl.TimerReason;

        public virtual void SetTimeout(TimeSpan timeout, TimeoutReason timeoutReason)
        {
            _realTimeoutControl.SetTimeout(timeout, timeoutReason);
        }

        public virtual void ResetTimeout(TimeSpan timeout, TimeoutReason timeoutReason)
        {
            _realTimeoutControl.ResetTimeout(timeout, timeoutReason);
        }

        public virtual void CancelTimeout()
        {
            _realTimeoutControl.CancelTimeout();
        }

        public virtual void InitializeHttp2(InputFlowControl connectionInputFlowControl)
        {
            _realTimeoutControl.InitializeHttp2(connectionInputFlowControl);
        }

        public virtual void StartRequestBody(MinDataRate minRate)
        {
            _realTimeoutControl.StartRequestBody(minRate);
        }

        public virtual void StopTimingRead()
        {
            _realTimeoutControl.StopTimingRead();
        }

        public virtual void StartTimingRead()
        {
            _realTimeoutControl.StartTimingRead();
        }

        public virtual void StopRequestBody()
        {
            _realTimeoutControl.StopRequestBody();
        }

        public virtual void BytesRead(long count)
        {
            _realTimeoutControl.BytesRead(count);
        }

        public virtual void StartTimingWrite()
        {
            _realTimeoutControl.StartTimingWrite();
        }

        public virtual void StopTimingWrite()
        {
            _realTimeoutControl.StopTimingWrite();
        }

        public virtual void BytesWrittenToBuffer(MinDataRate minRate, long size)
        {
            _realTimeoutControl.BytesWrittenToBuffer(minRate, size);
        }

        public virtual void Tick(long timestamp)
        {
            _realTimeoutControl.Tick(timestamp);
        }

        public long GetResponseDrainDeadline(long ticks, MinDataRate minRate)
        {
            return _realTimeoutControl.GetResponseDrainDeadline(ticks, minRate);
        }
    }

    public class RequestFields
    {
        public string Method { get; set; }
        public string Scheme { get; set; }
        public string Path { get; set; }
        public string RawTarget { get; set; }
        public string Authority { get; set; }
    }
}
