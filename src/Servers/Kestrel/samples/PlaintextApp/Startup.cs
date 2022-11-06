// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Connections;

namespace PlaintextApp;

public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        var payload = "Hello, World!"u8.ToArray();

        app.Run((httpContext) =>
        {
            return Task.RunAsGreenThread(() =>
            {
                var response = httpContext.Response;

                response.StatusCode = 200;
                response.ContentType = "text/plain";
                response.ContentLength = payload.Length;

                // This is synchronous IO!
                response.Body.Write(payload, 0, payload.Length);
            });
        });
    }

    public static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 5000);
                    })
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>();
            })
            .Build();

        await host.RunAsync();
    }
}

internal static class ValueTaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task GetAsTask(this in ValueTask<FlushResult> valueTask)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            // Signal consumption to the IValueTaskSource
            valueTask.GetAwaiter().GetResult();
            return Task.CompletedTask;
        }
        else
        {
            return valueTask.AsTask();
        }
    }
}
