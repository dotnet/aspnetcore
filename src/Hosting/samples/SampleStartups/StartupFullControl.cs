// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Note that this sample will not run. It is only here to illustrate usage patterns.

namespace SampleStartups;

public class StartupFullControl
{
    public static Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables(prefix: "ASPNETCORE_")
            .AddJsonFile("hosting.json", optional: true)
            .AddCommandLine(args)
            .Build();

        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseConfiguration(config) // Default set of configurations to use, may be subsequently overridden
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory()) // Override the content root with the current directory
                    .UseUrls("http://*:1000", "https://*:902")
                    .UseEnvironment(Environments.Development)
                    .UseWebRoot("public")
                    .Configure(app =>
                    {
                        // Write the application inline, this won't call any startup class in the assembly

                        app.Use(next => context =>
                        {
                            return next(context);
                        });
                    });
            })
            .ConfigureServices(services =>
            {
                // Configure services that the application can see
                services.AddSingleton<IMyCustomService, MyCustomService>();
            })
            .Build();

        return host.RunAsync();
    }
}

public class MyHostLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

public interface IMyCustomService
{
    void Go();
}

public class MyCustomService : IMyCustomService
{
    public void Go()
    {
        throw new NotImplementedException();
    }
}
