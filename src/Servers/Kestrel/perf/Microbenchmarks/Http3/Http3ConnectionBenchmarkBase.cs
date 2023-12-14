// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public abstract class Http3ConnectionBenchmarkBase
{
    private Http3InMemory _http3;
    private IHeaderDictionary _httpRequestHeaders;
    private Http3RequestHeaderHandler _headerHandler;
    private Http3HeadersEnumerator _requestHeadersEnumerator;
    private Http3FrameWithPayload _httpFrame;

    protected abstract Task ProcessRequest(HttpContext httpContext);

    private sealed class DefaultTimeoutHandler : ITimeoutHandler
    {
        public void OnTimeout(TimeoutReason reason) { }
    }

    public virtual void GlobalSetup()
    {
        _headerHandler = new Http3RequestHeaderHandler();
        _requestHeadersEnumerator = new Http3HeadersEnumerator();
        _httpFrame = new Http3FrameWithPayload();

        _httpRequestHeaders = new HttpRequestHeaders();
        _httpRequestHeaders[InternalHeaderNames.Method] = new StringValues("GET");
        _httpRequestHeaders[InternalHeaderNames.Path] = new StringValues("/");
        _httpRequestHeaders[InternalHeaderNames.Scheme] = new StringValues("http");
        _httpRequestHeaders[InternalHeaderNames.Authority] = new StringValues("localhost:80");

        var timeProvider = new FakeTimeProvider();

        var serviceContext = TestContextFactory.CreateServiceContext(
            serverOptions: new KestrelServerOptions(),
            dateHeaderValueManager: new DateHeaderValueManager(timeProvider),
            timeProvider: timeProvider);
        serviceContext.DateHeaderValueManager.OnHeartbeat();

        _http3 = new Http3InMemory(serviceContext, timeProvider, new DefaultTimeoutHandler(), NullLoggerFactory.Instance);

        _http3.InitializeConnectionAsync(ProcessRequest).GetAwaiter().GetResult();
    }

    [Benchmark]
    public async Task MakeRequest()
    {
        _requestHeadersEnumerator.Initialize(_httpRequestHeaders);

        var stream = await _http3.CreateRequestStream(_requestHeadersEnumerator, _headerHandler);

        while (true)
        {
            var frame = await stream.ReceiveFrameAsync(allowEnd: true, frame: _httpFrame);
            if (frame == null)
            {
                // Tell stream that is can be reset.
                stream.Complete();

                return;
            }

            switch (frame.Type)
            {
                case System.Net.Http.Http3FrameType.Data:
                    break;
                case System.Net.Http.Http3FrameType.Headers:
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected frame: {frame.Type}");
            }
        }
    }

    [GlobalCleanup]
    public void Dispose()
    {
    }
}
