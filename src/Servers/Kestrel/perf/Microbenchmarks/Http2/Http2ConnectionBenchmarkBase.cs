// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http.HPack;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Http2HeadersEnumerator = Microsoft.AspNetCore.Server.Kestrel.Core.Tests.Http2HeadersEnumerator;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public abstract class Http2ConnectionBenchmarkBase
{
    private MemoryPool<byte> _memoryPool;
    private IHeaderDictionary _httpRequestHeaders;
    private Http2Connection _connection;
    private DynamicHPackEncoder _hpackEncoder;
    private Http2HeadersEnumerator _requestHeadersEnumerator;
    private int _currentStreamId;
    private byte[] _headersBuffer;
    private DuplexPipe.DuplexPipePair _connectionPair;
    private int _dataWritten;
    private Task _requestProcessingTask;

    private readonly Http2Frame _receiveHttpFrame = new();
    private readonly Http2Frame _sendHttpFrame = new();

    protected abstract Task ProcessRequest(HttpContext httpContext);

    [Params(0, 1, 3)]
    public int NumCookies { get; set; }

    public virtual void GlobalSetup()
    {
        _memoryPool = PinnedBlockMemoryPoolFactory.Create();

        var options = new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);

        _connectionPair = DuplexPipe.CreateConnectionPair(options, options);

        _httpRequestHeaders = new HttpRequestHeaders();
        _httpRequestHeaders[InternalHeaderNames.Method] = new StringValues("GET");
        _httpRequestHeaders[InternalHeaderNames.Path] = new StringValues("/");
        _httpRequestHeaders[InternalHeaderNames.Scheme] = new StringValues("http");
        _httpRequestHeaders[InternalHeaderNames.Authority] = new StringValues("localhost:80");

        if (NumCookies > 0)
        {
            var cookies = new string[NumCookies];
            for (var index = 0; index < NumCookies; index++)
            {
                cookies[index] = $"{index}={index + 1}";
            }
            _httpRequestHeaders[HeaderNames.Cookie] = cookies;
        }

        _headersBuffer = new byte[1024 * 16];
        _hpackEncoder = new DynamicHPackEncoder();

        var serviceContext = TestContextFactory.CreateServiceContext(
            serverOptions: new KestrelServerOptions(),
            dateHeaderValueManager: new DateHeaderValueManager(TimeProvider.System),
            timeProvider: TimeProvider.System);
        serviceContext.DateHeaderValueManager.OnHeartbeat();

        var featureCollection = new FeatureCollection();
        featureCollection.Set<IConnectionMetricsContextFeature>(new TestConnectionMetricsContextFeature());
        featureCollection.Set<IConnectionMetricsTagsFeature>(new TestConnectionMetricsTagsFeature());
        featureCollection.Set<IProtocolErrorCodeFeature>(new TestProtocolErrorCodeFeature());
        var connectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: serviceContext,
            connectionContext: null,
            transport: _connectionPair.Transport,
            timeoutControl: new MockTimeoutControl(),
            memoryPool: _memoryPool,
            connectionFeatures: featureCollection);

        _connection = new Http2Connection(connectionContext);

        _requestHeadersEnumerator = new Http2HeadersEnumerator();

        _currentStreamId = 1;

        _requestProcessingTask = _connection.ProcessRequestsAsync(new DummyApplication(ProcessRequest, new MockHttpContextFactory()));

        _connectionPair.Application.Output.Write(Http2Connection.ClientPreface);
        _connectionPair.Application.Output.WriteSettings(new Http2PeerSettings
        {
            InitialWindowSize = 2147483647
        });
        _connectionPair.Application.Output.FlushAsync().GetAwaiter().GetResult();

        // Read past connection setup frames
        ReceiveFrameAsync(_connectionPair.Application.Input).GetAwaiter().GetResult();
        Debug.Assert(_receiveHttpFrame.Type == Http2FrameType.SETTINGS);
        ReceiveFrameAsync(_connectionPair.Application.Input).GetAwaiter().GetResult();
        Debug.Assert(_receiveHttpFrame.Type == Http2FrameType.WINDOW_UPDATE);
        ReceiveFrameAsync(_connectionPair.Application.Input).GetAwaiter().GetResult();
        Debug.Assert(_receiveHttpFrame.Type == Http2FrameType.SETTINGS);
    }

    [Benchmark]
    public async Task MakeRequest()
    {
        _requestHeadersEnumerator.Initialize(_httpRequestHeaders);
        _requestHeadersEnumerator.MoveNext();
        _connectionPair.Application.Output.WriteStartStream(streamId: _currentStreamId, _hpackEncoder, _requestHeadersEnumerator, _headersBuffer, endStream: true, frame: _sendHttpFrame);
        await _connectionPair.Application.Output.FlushAsync();

        while (true)
        {
            await ReceiveFrameAsync(_connectionPair.Application.Input);

            if (_receiveHttpFrame.StreamId != _currentStreamId && _receiveHttpFrame.StreamId != 0)
            {
                throw new Exception($"Unexpected stream ID: {_receiveHttpFrame.StreamId}");
            }

            if (_receiveHttpFrame.Type == Http2FrameType.DATA)
            {
                _dataWritten += _receiveHttpFrame.DataPayloadLength;
            }

            if (_dataWritten > 1024 * 32)
            {
                _connectionPair.Application.Output.WriteWindowUpdateAsync(streamId: 0, _dataWritten, _sendHttpFrame);
                await _connectionPair.Application.Output.FlushAsync();

                _dataWritten = 0;
            }

            if ((_receiveHttpFrame.HeadersFlags & Http2HeadersFrameFlags.END_STREAM) == Http2HeadersFrameFlags.END_STREAM)
            {
                break;
            }
        }

        _currentStreamId += 2;
    }

    internal async ValueTask ReceiveFrameAsync(PipeReader pipeReader, uint maxFrameSize = Http2PeerSettings.DefaultMaxFrameSize)
    {
        while (true)
        {
            var result = await pipeReader.ReadAsync();
            var buffer = result.Buffer;
            var consumed = buffer.Start;
            var examined = buffer.Start;

            try
            {
                if (Http2FrameReader.TryReadFrame(ref buffer, _receiveHttpFrame, maxFrameSize, out var framePayload))
                {
                    consumed = examined = framePayload.End;
                    return;
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
                pipeReader.AdvanceTo(consumed, examined);
            }
        }
    }

    [GlobalCleanup]
    public async ValueTask DisposeAsync()
    {
        _connectionPair.Application.Output.Complete();
        await _requestProcessingTask;
        _memoryPool?.Dispose();
    }

    private sealed class TestConnectionMetricsContextFeature : IConnectionMetricsContextFeature
    {
        public ConnectionMetricsContext MetricsContext { get; }
    }

    private sealed class TestConnectionMetricsTagsFeature : IConnectionMetricsTagsFeature
    {
        public ICollection<KeyValuePair<string, object>> Tags { get; }
    }

    private sealed class TestProtocolErrorCodeFeature : IProtocolErrorCodeFeature
    {
        public long Error { get; set; } = -1;
    }
}
