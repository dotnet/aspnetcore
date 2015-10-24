// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Http.Internal
{
    public class HttpContextFactory : IHttpContextFactory
    {
        private IHttpContextAccessor _httpContextAccessor;

        public HttpContextFactory(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public HttpContext Create(IFeatureCollection featureCollection)
        {
            var httpContext = new DefaultHttpContext(featureCollection);
            _httpContextAccessor.HttpContext = httpContext;
            return httpContext;
        }

        public void Dispose(HttpContext httpContext)
        {
            _httpContextAccessor.HttpContext = null;
        }
    }
}