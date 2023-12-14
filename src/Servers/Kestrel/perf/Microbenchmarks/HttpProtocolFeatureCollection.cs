// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class HttpProtocolFeatureCollection
{
    private readonly IFeatureCollection _collection;

    [Benchmark(Description = "Get<IHttpRequestFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpRequestFeature Get_IHttpRequestFeature()
    {
        return _collection.Get<IHttpRequestFeature>();
    }

    [Benchmark(Description = "Get<IHttpResponseFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpResponseFeature Get_IHttpResponseFeature()
    {
        return _collection.Get<IHttpResponseFeature>();
    }

    [Benchmark(Description = "Get<IHttpResponseBodyFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpResponseBodyFeature Get_IHttpResponseBodyFeature()
    {
        return _collection.Get<IHttpResponseBodyFeature>();
    }

    [Benchmark(Description = "Get<IRouteValuesFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IRouteValuesFeature Get_IRouteValuesFeature()
    {
        return _collection.Get<IRouteValuesFeature>();
    }

    [Benchmark(Description = "Get<IEndpointFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IEndpointFeature Get_IEndpointFeature()
    {
        return _collection.Get<IEndpointFeature>();
    }

    [Benchmark(Description = "Get<IServiceProvidersFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IServiceProvidersFeature Get_IServiceProvidersFeature()
    {
        return _collection.Get<IServiceProvidersFeature>();
    }

    [Benchmark(Description = "Get<IItemsFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IItemsFeature Get_IItemsFeature()
    {
        return _collection.Get<IItemsFeature>();
    }

    [Benchmark(Description = "Get<IQueryFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IQueryFeature Get_IQueryFeature()
    {
        return _collection.Get<IQueryFeature>();
    }

    [Benchmark(Description = "Get<IRequestBodyPipeFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IRequestBodyPipeFeature Get_IRequestBodyPipeFeature()
    {
        return _collection.Get<IRequestBodyPipeFeature>();
    }

    [Benchmark(Description = "Get<IFormFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IFormFeature Get_IFormFeature()
    {
        return _collection.Get<IFormFeature>();
    }

    [Benchmark(Description = "Get<IHttpAuthenticationFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpAuthenticationFeature Get_IHttpAuthenticationFeature()
    {
        return _collection.Get<IHttpAuthenticationFeature>();
    }

    [Benchmark(Description = "Get<IHttpRequestIdentifierFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpRequestIdentifierFeature Get_IHttpRequestIdentifierFeature()
    {
        return _collection.Get<IHttpRequestIdentifierFeature>();
    }

    [Benchmark(Description = "Get<IHttpConnectionFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpConnectionFeature Get_IHttpConnectionFeature()
    {
        return _collection.Get<IHttpConnectionFeature>();
    }

    [Benchmark(Description = "Get<ISessionFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ISessionFeature Get_ISessionFeature()
    {
        return _collection.Get<ISessionFeature>();
    }

    [Benchmark(Description = "Get<IResponseCookiesFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IResponseCookiesFeature Get_IResponseCookiesFeature()
    {
        return _collection.Get<IResponseCookiesFeature>();
    }

    [Benchmark(Description = "Get<IHttpRequestTrailersFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpRequestTrailersFeature Get_IHttpRequestTrailersFeature()
    {
        return _collection.Get<IHttpRequestTrailersFeature>();
    }

    [Benchmark(Description = "Get<IHttpResponseTrailersFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpResponseTrailersFeature Get_IHttpResponseTrailersFeature()
    {
        return _collection.Get<IHttpResponseTrailersFeature>();
    }

    [Benchmark(Description = "Get<ITlsConnectionFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ITlsConnectionFeature Get_ITlsConnectionFeature()
    {
        return _collection.Get<ITlsConnectionFeature>();
    }

    [Benchmark(Description = "Get<IHttpUpgradeFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpUpgradeFeature Get_IHttpUpgradeFeature()
    {
        return _collection.Get<IHttpUpgradeFeature>();
    }

    [Benchmark(Description = "Get<IHttpWebSocketFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpWebSocketFeature Get_IHttpWebSocketFeature()
    {
        return _collection.Get<IHttpWebSocketFeature>();
    }

    [Benchmark(Description = "Get<IHttp2StreamIdFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttp2StreamIdFeature Get_IHttp2StreamIdFeature()
    {
        return _collection.Get<IHttp2StreamIdFeature>();
    }

    [Benchmark(Description = "Get<IHttpRequestLifetimeFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpRequestLifetimeFeature Get_IHttpRequestLifetimeFeature()
    {
        return _collection.Get<IHttpRequestLifetimeFeature>();
    }

    [Benchmark(Description = "Get<IHttpMaxRequestBodySizeFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpMaxRequestBodySizeFeature Get_IHttpMaxRequestBodySizeFeature()
    {
        return _collection.Get<IHttpMaxRequestBodySizeFeature>();
    }

    [Benchmark(Description = "Get<IHttpMinRequestBodyDataRateFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpMinRequestBodyDataRateFeature Get_IHttpMinRequestBodyDataRateFeature()
    {
        return _collection.Get<IHttpMinRequestBodyDataRateFeature>();
    }

    [Benchmark(Description = "Get<IHttpMinResponseDataRateFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpMinResponseDataRateFeature Get_IHttpMinResponseDataRateFeature()
    {
        return _collection.Get<IHttpMinResponseDataRateFeature>();
    }

    [Benchmark(Description = "Get<IHttpBodyControlFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpBodyControlFeature Get_IHttpBodyControlFeature()
    {
        return _collection.Get<IHttpBodyControlFeature>();
    }

    [Benchmark(Description = "Get<IHttpRequestBodyDetectionFeature>*")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpRequestBodyDetectionFeature Get_IHttpRequestBodyDetectionFeature()
    {
        return _collection.Get<IHttpRequestBodyDetectionFeature>();
    }

    [Benchmark(Description = "Get<IHttpResetFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpResetFeature Get_IHttpResetFeature()
    {
        return _collection.Get<IHttpResetFeature>();
    }

    [Benchmark(Description = "Get<IHttpNotFoundFeature>")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IHttpNotFoundFeature Get_IHttpNotFoundFeature()
    {
        return _collection.Get<IHttpNotFoundFeature>();
    }

    public HttpProtocolFeatureCollection()
    {
        var memoryPool = PinnedBlockMemoryPoolFactory.Create();
        var options = new PipeOptions(memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);

        var serviceContext = TestContextFactory.CreateServiceContext(
            serverOptions: new KestrelServerOptions(),
            httpParser: new HttpParser<Http1ParsingHandler>(),
            dateHeaderValueManager: new DateHeaderValueManager(TimeProvider.System));

        var connectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: serviceContext,
            connectionContext: null,
            transport: pair.Transport,
            memoryPool: memoryPool,
            connectionFeatures: new FeatureCollection());

        var http1Connection = new Http1Connection(connectionContext);

        http1Connection.Reset();

        _collection = http1Connection;
    }

    public interface IHttpNotFoundFeature
    {
    }
}
