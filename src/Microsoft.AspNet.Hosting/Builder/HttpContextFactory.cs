// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.Hosting.Builder
{
    public class HttpContextFactory : IHttpContextFactory
    {
        public HttpContext CreateHttpContext(IFeatureCollection featureCollection)
        {
            return new DefaultHttpContext(featureCollection);
        }
    }
}