// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class ResourceCollectionUrlEndpointTest
{
    [Fact]
    public async Task MapResourceCollectionEndpoints_GzipEndpointReturnsCompleteGzipPayload()
    {
        var endpoints = new List<Endpoint>();
        var collection = new ResourceAssetCollection(
        [
            new("/_framework/app.dll",
            [
                new("integrity", "sha256-OriginalHash123456")
            ])
        ]);

        ResourceCollectionUrlEndpoint.MapResourceCollectionEndpoints(
            endpoints,
            "_framework/resource-collection{0}.js{1}",
            collection);

        var gzipEndpoint = Assert.Single(endpoints.OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText == "_framework/resource-collection.js.gz");
        var contentEndpoint = Assert.Single(
            endpoints.OfType<RouteEndpoint>(),
            endpoint => endpoint.RoutePattern.RawText == "_framework/resource-collection.js" &&
                endpoint.Metadata.GetMetadata<ContentEncodingMetadata>() is null);

        var gzipContext = new DefaultHttpContext();
        await using var gzipResponseBody = new MemoryStream();
        gzipContext.Response.Body = gzipResponseBody;

        await gzipEndpoint.RequestDelegate!(gzipContext);

        gzipResponseBody.Position = 0;
        await using var decompressedBody = new MemoryStream();
        await using (var gzipStream = new GZipStream(gzipResponseBody, CompressionMode.Decompress, leaveOpen: true))
        {
            await gzipStream.CopyToAsync(decompressedBody);
        }

        var contentContext = new DefaultHttpContext();
        await using var contentResponseBody = new MemoryStream();
        contentContext.Response.Body = contentResponseBody;

        await contentEndpoint.RequestDelegate!(contentContext);

        Assert.Equal("gzip", gzipContext.Response.Headers.ContentEncoding);
        Assert.Equal(contentResponseBody.ToArray(), decompressedBody.ToArray());
    }
}
