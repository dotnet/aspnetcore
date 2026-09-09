// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DojoClient.E2E.Tests.ServiceOverrides;

internal sealed class DojoRunStartupFilter(DojoRunStore runs) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (!context.Request.Path.StartsWithSegments(DojoRunStore.ControlPath, out var remaining))
                {
                    await nextMiddleware(context);
                    return;
                }

                var segments = remaining.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments is [var id] && Guid.TryParseExact(id, "N", out _))
                {
                    if (HttpMethods.IsPost(context.Request.Method))
                    {
                        DojoRecording? recording = null;
                        if (context.Request.Query.TryGetValue("recording", out var name))
                        {
                            if (!Enum.TryParse<DojoRecording>(name, out var value) || !Enum.IsDefined(value))
                            {
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                await context.Response.WriteAsync($"Unknown dojo recording '{name}'.");
                                return;
                            }

                            recording = value;
                        }

                        await runs.CreateAsync(id, recording);
                        context.Response.StatusCode = StatusCodes.Status201Created;
                        return;
                    }

                    if (HttpMethods.IsDelete(context.Request.Method))
                    {
                        await runs.RemoveAsync(id);
                        context.Response.StatusCode = StatusCodes.Status204NoContent;
                        return;
                    }
                }
                else if (segments is [var runId, var operation] &&
                    Guid.TryParseExact(runId, "N", out _) &&
                    ((operation == "release" && HttpMethods.IsPost(context.Request.Method)) ||
                     (operation == "checkpoint" && HttpMethods.IsGet(context.Request.Method))))
                {
                    var key = context.Request.Query["key"].ToString();
                    if (string.IsNullOrEmpty(key))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Missing checkpoint key.");
                        return;
                    }

                    var locks = runs.Get(runId).Locks;
                    if (operation == "release")
                    {
                        context.Response.StatusCode = locks.Release(key)
                            ? StatusCodes.Status200OK
                            : StatusCodes.Status409Conflict;
                    }
                    else
                    {
                        await context.Response.WriteAsync(locks.WaitOn(key).IsCompleted ? "true" : "false");
                    }

                    return;
                }

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid dojo test-session control request.");
            });
            next(app);
        };
    }
}
