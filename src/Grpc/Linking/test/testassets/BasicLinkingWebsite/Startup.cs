// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using BasicWebsite.Services;
using Microsoft.Net.Http.Headers;

namespace BasicWebsite;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();
    }

    public void Configure(IApplicationBuilder app, IHostApplicationLifetime applicationLifetime)
    {
        // Required to notify test infrastructure that it can begin tests
        applicationLifetime.ApplicationStarted.Register(() =>
        {
            Console.WriteLine("Application started.");

            var runtimeVersion = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
            Console.WriteLine($"NetCoreAppVersion: {runtimeVersion}");
            var aspNetCoreVersion = typeof(HeaderNames).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
            Console.WriteLine($"AspNetCoreAppVersion: {aspNetCoreVersion}");
        });

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<GreeterService>();
        });
    }
}
