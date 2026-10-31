// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HealthChecksSample;

// Pass in `--scenario db` at the command line to run this sample.
public class DbHealthStartup
{
    public DbHealthStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Registers required services for health checks
        services.AddHealthChecks()
            // Add a health check for a SQL database
            .AddCheck("MyDatabase", new SqlConnectionHealthCheck(Configuration["ConnectionStrings:DefaultConnection"]));
    }

    public void Configure(IApplicationBuilder app)
    {
        // This will register the health checks middleware at the URL /health.
        // 
        // By default health checks will return a 200 with 'Healthy' when the database is responsive
        // - We've registered a SqlConnectionHealthCheck
        // - The default response writer writes the HealthCheckStatus as text/plain content
        //
        // This is the simplest way to use health checks, it is suitable for systems
        // that want to check for 'liveness' of an application with a database.
        app.UseHealthChecks("/health");

        app.Run(async (context) =>
        {
            await context.Response.WriteAsync("Go to /health to see the health status");
        });
    }
}
