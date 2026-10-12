// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

// IStartupFilter that injects E2E test middleware into the app pipeline.
// Registered by TestReadinessHostingStartup so it runs in every E2E app process.
//
// Adds two behaviors early in the pipeline:
//   1. Cookie middleware: reads "test-session-id" cookie → TestSessionContext.Id
//   2. Lock release endpoint: POST /_test/lock/release?key=... → completes the TCS
internal class TestInfrastructureStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                // Populate TestSessionContext from cookie (every request)
                if (context.Request.Cookies.TryGetValue("test-session-id", out var sessionId))
                {
                    var sessionContext = context.RequestServices
                        .GetRequiredService<TestSessionContext>();
                    sessionContext.Id = sessionId;
                }

                // Lock release endpoint
                if (context.Request.Path.StartsWithSegments("/_test/lock/release")
                    && context.Request.Method == "POST")
                {
                    var key = context.Request.Query["key"].ToString();
                    if (string.IsNullOrEmpty(key))
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Missing 'key' query parameter.").ConfigureAwait(false);
                        return;
                    }

                    var lockProvider = context.RequestServices
                        .GetRequiredService<TestLockProvider>();
                    var released = lockProvider.Release(key);

                    context.Response.StatusCode = released ? 200 : 404;
                    return;
                }

                await nextMiddleware().ConfigureAwait(false);
            });

            next(app);
        };
    }
}
