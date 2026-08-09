// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

internal sealed partial class CircuitJavaScriptInitializationMiddleware
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
        // The contract is generated rather than reflected over so that the endpoint keeps working in an
        // application that disabled reflection-based serialization, which Native AOT does by default.
        await context.Response.WriteAsJsonAsync(_initializers, JavaScriptInitializersJsonContext.Default.IListString);
    }

    [JsonSerializable(typeof(IList<string>))]
    private sealed partial class JavaScriptInitializersJsonContext : JsonSerializerContext;
}
