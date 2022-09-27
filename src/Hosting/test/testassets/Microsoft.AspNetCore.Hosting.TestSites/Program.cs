// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace ServerComparison.TestSites;

public static class Program
{
    public static void Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddCommandLine(args)
            .AddEnvironmentVariables(prefix: "ASPNETCORE_")
            .Build();

        var builder = new WebHostBuilder()
            .UseServer(new NoopServer())
            .UseConfiguration(config)
            .SuppressStatusMessages(true)
            .ConfigureLogging((_, factory) =>
            {
                factory.AddConsole();
                factory.AddFilter<ConsoleLoggerProvider>(level => level >= LogLevel.Warning);
            })
            .UseStartup("Microsoft.AspNetCore.Hosting.TestSites");

        if (config["STARTMECHANIC"] == "Run")
        {
            var host = builder.Build();

            host.Run();
        }
        else if (config["STARTMECHANIC"] == "WaitForShutdown")
        {
            using (var host = builder.Build())
            {
                host.Start();

                host.WaitForShutdown();
            }
        }
        else
        {
            throw new InvalidOperationException("Starting mechanic not specified");
        }
    }
}

public class NoopServer : IServer
{
    public void Dispose()
    {
    }

    public IFeatureCollection Features { get; } = new FeatureCollection();

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
