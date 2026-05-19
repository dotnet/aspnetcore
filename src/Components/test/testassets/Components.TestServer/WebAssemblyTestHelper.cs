// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace TestServer;

public static class WebAssemblyTestHelper
{
    public static bool MultithreadingIsEnabled()
    {
        var entrypointAssembly = Assembly.GetExecutingAssembly();
        var attribute = entrypointAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key.Equals("Microsoft.AspNetCore.InternalTesting.RunWebAssemblyE2ETestsWithMultithreading", StringComparison.Ordinal));
        return attribute is not null && bool.Parse(attribute.Value);
    }

    public static void ServeCoopHeadersIfWebAssemblyThreadingEnabled(IApplicationBuilder app)
    {
        if (MultithreadingIsEnabled())
        {
            app.Use(async (ctx, next) =>
            {
                // Browser multi-threaded runtime requires cross-origin policy headers to enable SharedArrayBuffer.
                ctx.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
                ctx.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
                await next(ctx);
            });
        }
    }
}
