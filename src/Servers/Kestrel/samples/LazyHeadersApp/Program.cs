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
        if (access is not ("ignore" or "read-one" or "read-all" or "enumerate"))
        {
            throw new ArgumentException("access must be ignore, read-one, read-all, or enumerate.", nameof(args));
        }
        AppContext.SetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyRequestHeaders", lazy);

        using var host = new HostBuilder()
            .ConfigureWebHost(builder => builder
                .UseKestrel(options => options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1))
                .UseUrls(configuration["urls"] ?? "http://127.0.0.1:5000")
                .Configure(app => app.Run(context =>
                {
                    var observedLength = 0;
                    uint checksum = 0;
                    void Observe(Microsoft.Extensions.Primitives.StringValues values)
                    {
                        foreach (var value in values)
                        {
                            observedLength += value?.Length ?? 0;
                            foreach (var character in value ?? string.Empty)
                            {
                                checksum = unchecked(checksum + character);
                            }
                        }
                    }
                    if (access == "read-one")
                    {
                        Observe(context.Request.Headers.UserAgent);
                    }
                    else if (access == "read-all")
                    {
                        Observe(context.Request.Headers.Accept);
                        Observe(context.Request.Headers.AcceptLanguage);
                        Observe(context.Request.Headers.UserAgent);
                        Observe(context.Request.Headers["X-Example"]);
                        Observe(context.Request.Headers["X-Request-Nonce"]);
                    }
                    else if (access == "enumerate")
                    {
                        foreach (var header in context.Request.Headers)
                        {
                            Observe(header.Value);
                        }
                    }

                    context.Response.Headers["X-Observed-Length"] = observedLength.ToString(CultureInfo.InvariantCulture);
                    context.Response.Headers["X-Observed-Checksum"] = checksum.ToString(CultureInfo.InvariantCulture);
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
