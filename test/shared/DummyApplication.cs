// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Testing
{
    public class DummyApplication : IHttpApplication<HttpContext>
    {
        private readonly RequestDelegate _requestDelegate;

        public DummyApplication(RequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
        }

        public HttpContext CreateContext(IFeatureCollection contextFeatures)
        {
            return new DefaultHttpContext(contextFeatures);
        }

        public void DisposeContext(HttpContext context, Exception exception)
        {

        }

        public async Task ProcessRequestAsync(HttpContext context)
        {
            await _requestDelegate(context);
        }
    }
}
