// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal class DummyApplication : IHttpApplication<HttpContext>
{
    private readonly RequestDelegate _requestDelegate;

    public DummyApplication() : this(context => Task.CompletedTask) { }

    public DummyApplication(RequestDelegate requestDelegate)
    {
        _requestDelegate = requestDelegate;
    }

    public HttpContext CreateContext(IFeatureCollection contextFeatures)
    {
        return new DefaultHttpContext(contextFeatures);
    }

    public void DisposeContext(HttpContext httpContext, Exception exception)
    {

    }

    public async Task ProcessRequestAsync(HttpContext httpContext)
    {
        await _requestDelegate(httpContext);
    }
}
