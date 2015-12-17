// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;

namespace SampleApp
{
    public class PooledHttpContextFactory : IHttpContextFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Stack<PooledHttpContext> _pool = new Stack<PooledHttpContext>();

        public PooledHttpContextFactory(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public HttpContext Create(IFeatureCollection featureCollection)
        {
            PooledHttpContext httpContext = null;
            lock (_pool)
            {
                if (_pool.Count != 0)
                {
                    httpContext = _pool.Pop();
                }
            }

            if (httpContext == null)
            {
                httpContext = new PooledHttpContext(featureCollection);
            }
            else
            {
                httpContext.Initialize(featureCollection);
            }

            if (_httpContextAccessor != null)
            {
                _httpContextAccessor.HttpContext = httpContext;
            }
            return httpContext;
        }

        public void Dispose(HttpContext httpContext)
        {
            if (_httpContextAccessor != null)
            {
                _httpContextAccessor.HttpContext = null;
            }

            var pooled = httpContext as PooledHttpContext;
            if (pooled != null)
            {
                pooled.Uninitialize();
                lock (_pool)
                {
                    _pool.Push(pooled);
                }
            }
        }
    }
}
