// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

internal sealed class CircuitJavaScriptInitializationMiddleware
{
    private readonly IList<string> _initializers;

    // We don't need the request delegate for anything, however we need to inject it to satisfy the middleware
    // contract.
    public CircuitJavaScriptInitializationMiddleware(IOptions<CircuitOptions> options, RequestDelegate _)
    {
        _initializers = options.Value.JavaScriptInitializers;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await context.Response.WriteAsJsonAsync(_initializers);
    }
}
