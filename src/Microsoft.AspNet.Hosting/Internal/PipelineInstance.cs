// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Hosting.Builder;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class PipelineInstance : IDisposable
    {
        private readonly IHttpContextFactory _httpContextFactory;
        private readonly RequestDelegate _requestDelegate;
        private readonly IHttpContextAccessor _contextAccessor;

        public PipelineInstance(IHttpContextFactory httpContextFactory, RequestDelegate requestDelegate, IHttpContextAccessor contextAccessor)
        {
            _httpContextFactory = httpContextFactory;
            _requestDelegate = requestDelegate;
            _contextAccessor = contextAccessor;
        }

        public Task Invoke(IFeatureCollection featureCollection)
        {
            var httpContext = _httpContextFactory.CreateHttpContext(featureCollection);
            _contextAccessor.HttpContext = httpContext;
            return _requestDelegate(httpContext);
        }

        public void Dispose()
        {
            // TODO: application notification of disposal
        }
    }
}
