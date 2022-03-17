// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HealthChecksSample;

// Pass in `--scenario basic` at the command line to run this sample.
public class BasicStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Registers required services for health checks
        services.AddHealthChecks();
    }

    public void Configure(IApplicationBuilder app)
    {
        // This will register the health checks middleware at the URL /health.
        // 
        // By default health checks will return a 200 with 'Healthy'.
        // - No health checks are registered by default, the app is healthy if it is reachable
        // - The default response writer writes the HealthCheckStatus as text/plain content
        //
        // This is the simplest way to use health checks, it is suitable for systems
        // that want to check for 'liveness' of an application.
        app.UseHealthChecks("/health");

        app.Run(async (context) =>
        {
            await context.Response.WriteAsync("Go to /health to see the health status");
        });
    }
}
