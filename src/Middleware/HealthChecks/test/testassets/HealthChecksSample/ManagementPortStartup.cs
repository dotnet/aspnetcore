// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HealthChecksSample;

// Pass in `--scenario port` at the command line to run this sample.
public class ManagementPortStartup
{
    public ManagementPortStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Registers required services for health checks
        services.AddHealthChecks();
    }

    public void Configure(IApplicationBuilder app)
    {
        // This will register the health checks middleware at the URL /health but only on the specified port.
        // 
        // By default health checks will return a 200 with 'Healthy'.
        // - No health checks are registered by default, the app is healthy if it is reachable
        // - The default response writer writes the HealthCheckStatus as text/plain content
        //
        // Use UseHealthChecks with a port will only process health checks requests on connection
        // to the specified port. This is typically used in a container environment where you can expose
        // a port for monitoring services to have access to the service.
        // - In this case the management is configured in the launchSettings.json and passed through 
        //  an environment variable
        // - Additionally, the server is also configured to listen to requests on the management port.
        app.UseHealthChecks("/health", port: Configuration["ManagementPort"]);

        app.Run(async (context) =>
        {
            await context.Response.WriteAsync($"Go to http://localhost:{Configuration["ManagementPort"]}/health to see the health status");
        });
    }
}
