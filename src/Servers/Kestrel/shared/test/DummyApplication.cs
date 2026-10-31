// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.InternalTesting;

public class DummyApplication : IHttpApplication<HttpContext>
{
    private readonly RequestDelegate _requestDelegate;
    private readonly IHttpContextFactory _httpContextFactory;

    public DummyApplication()
        : this(_ => Task.CompletedTask)
    {
    }

    public DummyApplication(RequestDelegate requestDelegate)
        : this(requestDelegate, null)
    {
    }

    public DummyApplication(RequestDelegate requestDelegate, IHttpContextFactory httpContextFactory)
    {
        _requestDelegate = requestDelegate;
        _httpContextFactory = httpContextFactory;
    }

    public HttpContext CreateContext(IFeatureCollection contextFeatures)
    {
        return _httpContextFactory?.Create(contextFeatures) ?? new DefaultHttpContext(contextFeatures);
    }

    public void DisposeContext(HttpContext context, Exception exception)
    {
        _httpContextFactory?.Dispose(context);
    }

    public async Task ProcessRequestAsync(HttpContext context)
    {
        await _requestDelegate(context);
    }
}
