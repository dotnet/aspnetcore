// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace LargeResponseApp;

public class Startup
{
    private const int _chunkSize = 4096;
    private const int _defaultNumChunks = 16;
    private static readonly byte[] _chunk = Encoding.UTF8.GetBytes(new string('a', _chunkSize));

    public void Configure(IApplicationBuilder app)
    {
        app.Run(async (context) =>
        {
            var path = context.Request.Path;
            if (!path.HasValue || !int.TryParse(path.Value.AsSpan(1), out var numChunks))
            {
                numChunks = _defaultNumChunks;
            }

            context.Response.ContentLength = _chunkSize * numChunks;
            context.Response.ContentType = "text/plain";

            for (int i = 0; i < numChunks; i++)
            {
                await context.Response.Body.WriteAsync(_chunk, 0, _chunkSize).ConfigureAwait(false);
            }
        });
    }

    public static Task Main(string[] args)
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

        return host.RunAsync();
    }
}
