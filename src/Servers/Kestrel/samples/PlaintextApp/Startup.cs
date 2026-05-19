// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace PlaintextApp;

public class Startup
{
    private static readonly byte[] _helloWorldBytes = Encoding.UTF8.GetBytes("Hello, World!");

    public void Configure(IApplicationBuilder app)
    {
        app.Run((httpContext) =>
        {
            var payload = _helloWorldBytes;
            var response = httpContext.Response;

            response.StatusCode = 200;
            response.ContentType = "text/plain";
            response.ContentLength = payload.Length;

            return response.BodyWriter.WriteAsync(payload).GetAsTask();
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
                        options.Listen(IPAddress.Loopback, 5001);
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
