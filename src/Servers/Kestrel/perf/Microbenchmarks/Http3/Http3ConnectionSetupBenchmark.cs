// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

// Measures the cost of setting up an HTTP/3 connection like a browser would: the client opens its control,
// QPACK encoder and QPACK decoder streams, sends a single request and the server then closes the connection.
public class Http3ConnectionSetupBenchmark
{
    private ServiceContext _serviceContext;
    private FakeTimeProvider _timeProvider;
    private IHeaderDictionary _httpRequestHeaders;
    private Http3RequestHeaderHandler _headerHandler;
    private Http3HeadersEnumerator _requestHeadersEnumerator;
    private Http3FrameWithPayload _httpFrame;

    private sealed class DefaultTimeoutHandler : ITimeoutHandler
    {
        public void OnTimeout(TimeoutReason reason) { }
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _headerHandler = new Http3RequestHeaderHandler();
        _requestHeadersEnumerator = new Http3HeadersEnumerator();
        _httpFrame = new Http3FrameWithPayload();

        _httpRequestHeaders = new HttpRequestHeaders();
        _httpRequestHeaders[InternalHeaderNames.Method] = new StringValues("GET");
        _httpRequestHeaders[InternalHeaderNames.Path] = new StringValues("/");
        _httpRequestHeaders[InternalHeaderNames.Scheme] = new StringValues("http");
        _httpRequestHeaders[InternalHeaderNames.Authority] = new StringValues("localhost:80");

        _timeProvider = new FakeTimeProvider();

        _serviceContext = TestContextFactory.CreateServiceContext(
            serverOptions: new KestrelServerOptions(),
            dateHeaderValueManager: new DateHeaderValueManager(_timeProvider),
            timeProvider: _timeProvider);
        _serviceContext.DateHeaderValueManager.OnHeartbeat();
    }

    [Benchmark]
    public async Task ConnectionWithSingleRequest()
    {
        var http3 = new Http3InMemory(_serviceContext, _timeProvider, new DefaultTimeoutHandler(), NullLoggerFactory.Instance);

        await http3.InitializeConnectionAsync(static context => Task.CompletedTask);

        // Client control, QPACK encoder and QPACK decoder streams on distinct client-initiated unidirectional stream IDs.
        http3.OutboundControlStream = await http3.CreateControlStream(id: 0, streamId: 2);
        await http3.CreateControlStream(id: 2, streamId: 6);
        await http3.CreateControlStream(id: 3, streamId: 10);

        _requestHeadersEnumerator.Initialize(_httpRequestHeaders);
        var stream = await http3.CreateRequestStream(_requestHeadersEnumerator, _headerHandler, endStream: true);

        while (await stream.ReceiveFrameAsync(allowEnd: true, frame: _httpFrame) != null)
        {
        }

        await stream.OnStreamCompletedTask;

        http3.CloseServerGracefully();
        await http3.WaitForConnectionStopAsync(expectedLastStreamId: 4, ignoreNonGoAwayFrames: true, expectedErrorCode: System.Net.Http.Http3ErrorCode.NoError);
    }
}
