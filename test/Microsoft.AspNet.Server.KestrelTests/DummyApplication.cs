// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Server.KestrelTests
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
