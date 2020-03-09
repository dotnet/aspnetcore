// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class MockHttpContextFactory : IHttpContextFactory
    {
        private readonly object _lock = new object();
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
}
