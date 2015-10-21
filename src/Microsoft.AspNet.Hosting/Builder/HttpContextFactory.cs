// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.Hosting.Builder
{
    public class HttpContextFactory : IHttpContextFactory
    {
        private IHttpContextAccessor _httpContextAccessor;

        public HttpContextFactory(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public HttpContext CreateHttpContext(IFeatureCollection featureCollection)
        {
            var httpContext = new DefaultHttpContext(featureCollection);
            _httpContextAccessor.HttpContext = httpContext;
            return httpContext;
        }
    }
}