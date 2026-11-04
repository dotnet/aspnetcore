// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestDecompression.Benchmarks;

public class RequestDecompressionMiddlewareBenchmark
{
    private RequestDecompressionMiddleware _middleware;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var requestDecompressionProvider = new DefaultRequestDecompressionProvider(
            NullLogger<DefaultRequestDecompressionProvider>.Instance,
            Options.Create(new RequestDecompressionOptions())
        );

        _middleware = new RequestDecompressionMiddleware(
            context => Task.CompletedTask,
            NullLogger<RequestDecompressionMiddleware>.Instance,
            requestDecompressionProvider
        );
    }

    [Params(true, false)]
    public bool HasRequestSizeLimitMetadata { get; set; }

    [Benchmark]
    public async Task HandleRequest_Compressed()
    {
        var context = CreateHttpContext(HasRequestSizeLimitMetadata);

        context.Request.Headers.ContentEncoding = "gzip";

        await _middleware.Invoke(context);
    }

    [Benchmark]
    public async Task HandleRequest_Uncompressed()
    {
        var context = CreateHttpContext(HasRequestSizeLimitMetadata);

        await _middleware.Invoke(context);
    }

    private static DefaultHttpContext CreateHttpContext(bool hasRequestSizeLimitMetadata)
    {
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        features.Set<IHttpMaxRequestBodySizeFeature>(new MaxRequestBodySizeFeature());
        features.Set<IEndpointFeature>(new EndpointFeature(hasRequestSizeLimitMetadata));
        var context = new DefaultHttpContext(features);
        return context;
    }

    private sealed class MaxRequestBodySizeFeature : IHttpMaxRequestBodySizeFeature
    {
        public bool IsReadOnly => false;

        public long? MaxRequestBodySize { get; set; } = 30_000_000;
    }

    private sealed class EndpointFeature : IEndpointFeature
    {
        public Endpoint Endpoint { get; set; }

        public EndpointFeature(bool hasRequestSizeLimitMetadata)
        {
            var metadataCollection = hasRequestSizeLimitMetadata
                ? new EndpointMetadataCollection(new SizeLimitMetadata())
                : new EndpointMetadataCollection();

            Endpoint = new Endpoint(
                requestDelegate: null,
                metadata: metadataCollection,
                displayName: null);
        }
    }

    private sealed class SizeLimitMetadata : IRequestSizeLimitMetadata
    {
        public long? MaxRequestBodySize { get; set; } = 50_000_000;
    }
}
