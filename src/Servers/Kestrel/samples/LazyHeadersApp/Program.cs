// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LazyHeadersApp;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().AddCommandLine(args).Build();
        var lazy = configuration.GetValue<bool>("lazy");
        var access = configuration["access"] ?? "ignore";
        if (access is not ("ignore" or "read" or "enumerate"))
        {
            throw new ArgumentException("access must be ignore, read, or enumerate.", nameof(args));
        }
        AppContext.SetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyUserAgent", lazy);

        using var host = new HostBuilder()
            .ConfigureWebHost(builder => builder
                .UseKestrel(options => options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1))
                .UseUrls(configuration["urls"] ?? "http://127.0.0.1:5000")
                .Configure(app => app.Run(context =>
                {
                    var observedLength = 0;
                    if (access == "read")
                    {
                        observedLength = context.Request.Headers.UserAgent.ToString().Length;
                    }
                    else if (access == "enumerate")
                    {
                        foreach (var header in context.Request.Headers)
                        {
                            foreach (var value in header.Value)
                            {
                                observedLength += value?.Length ?? 0;
                            }
                        }
                    }

                    context.Response.Headers["X-Observed-Length"] = observedLength.ToString(CultureInfo.InvariantCulture);
                    context.Response.ContentType = "text/plain";
                    context.Response.ContentLength = 13;
                    return context.Response.WriteAsync("Hello, World!");
                })))
            .Build();
        await host.StartAsync();
        Console.WriteLine($"Application started. lazy={lazy}; access={access}");
        await host.WaitForShutdownAsync();
    }
}
