// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.AspNet.Hosting
{
    public class PipelineInstance : IDisposable
    {
        private readonly IHttpContextFactory _httpContextFactory;
        private readonly RequestDelegate _requestDelegate;

        public PipelineInstance(IHttpContextFactory httpContextFactory, RequestDelegate requestDelegate)
        {
            _httpContextFactory = httpContextFactory;
            _requestDelegate = requestDelegate;
        }

        public Task Invoke(object serverEnvironment)
        {
            var httpContext = _httpContextFactory.CreateHttpContext(serverEnvironment);
            return _requestDelegate(httpContext);
        }

        public void Dispose()
        {
            // TODO: application notification of disposal
        }
    }
}
