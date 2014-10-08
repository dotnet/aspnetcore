// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.PipelineCore;

namespace Microsoft.AspNet.Hosting.Builder
{
    public class HttpContextFactory : IHttpContextFactory
    {
        public HttpContext CreateHttpContext(object serverContext)
        {
            var featureObject = serverContext as IFeatureCollection ?? new FeatureObject(serverContext);
            var featureCollection = new FeatureCollection(featureObject);
            var httpContext = new DefaultHttpContext(featureCollection);
            return httpContext;
        }
    }
}