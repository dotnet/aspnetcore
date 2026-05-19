// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BasicLinkedApp;

public class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    // Do not change the signature of this method. It's used for tests.
    private static IHostBuilder CreateWebHostBuilder(string[] args)
    {
        return new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddCommandLine(args);
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseKestrel(o =>
                    {
                        o.ConfigureEndpointDefaults(lo =>
                        {
                            lo.UseConnectionLogging();
                        });

                    }).UseStartup<Startup>();
                });
    }
}

