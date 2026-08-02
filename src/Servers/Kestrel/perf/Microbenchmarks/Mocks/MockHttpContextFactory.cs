// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class MockHttpContextFactory : IHttpContextFactory
{
    private readonly Lock _lock = new();
    private readonly Queue<DefaultHttpContext> _cache = new Queue<DefaultHttpContext>();

    public HttpContext Create(IFeatureCollection featureCollection)
    {
        DefaultHttpContext httpContext;

        lock (_lock)
        {
            if (!_cache.TryDequeue(out httpContext))
            {
                httpContext = new DefaultHttpContext();
            }
        }

        httpContext.Initialize(featureCollection);
        return httpContext;
    }

    public void Dispose(HttpContext httpContext)
    {
        lock (_lock)
        {
            var defaultHttpContext = (DefaultHttpContext)httpContext;

            defaultHttpContext.Uninitialize();
            _cache.Enqueue(defaultHttpContext);
        }
    }
}
