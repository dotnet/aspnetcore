// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Internal;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Http.Internal
{
    public class HttpContextFactory : IHttpContextFactory
    {
        private readonly ObjectPool<StringBuilder> _builderPool;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextFactory(ObjectPoolProvider poolProvider)
            : this(poolProvider, httpContextAccessor: null)
        {
        }

        public HttpContextFactory(ObjectPoolProvider poolProvider, IHttpContextAccessor httpContextAccessor)
        {
            if (poolProvider == null)
            {
                throw new ArgumentNullException(nameof(poolProvider));
            }

            _builderPool = poolProvider.CreateStringBuilderPool();
            _httpContextAccessor = httpContextAccessor;
        }

        public HttpContext Create(IFeatureCollection featureCollection)
        {
            if (featureCollection == null)
            {
                throw new ArgumentNullException(nameof(featureCollection));
            }

            var responseCookiesFeature = new ResponseCookiesFeature(featureCollection, _builderPool);
            featureCollection.Set<IResponseCookiesFeature>(responseCookiesFeature);

            var httpContext = new DefaultHttpContext(featureCollection);
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
        }
    }
}