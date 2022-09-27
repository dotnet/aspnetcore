// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace HealthChecksSample;

// Pass in `--scenario dbcontext` at the command line to run this sample.
public class DbContextHealthStartup
{
    public DbContextHealthStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Registers required services for health checks
        services.AddHealthChecks()

            // Registers a health check for the MyContext type. By default the name of the health check will be the
            // name of the DbContext type. There are other options available through AddDbContextCheck to configure
            // failure status, tags, and custom test query.
            .AddDbContextCheck<MyContext>();

        // Registers the MyContext type and configures the database provider.
        //
        // The health check added by AddDbContextCheck will create instances of MyContext from the service provider,
        // and so will reuse the configuration provided here.
        services.AddDbContext<MyContext>(
            options => options.UseSqlite(Configuration["ConnectionStrings:DefaultConnection"]));
    }

    public void Configure(IApplicationBuilder app)
    {
        // This will register the health checks middleware at the URL /health.
        //
        // Since this sample doesn't do anything to create the database by default, this will
        // return unhealthy by default.
        //
        // You can to to /createdatabase and /deletedatabase to create and delete the database
        // (respectively), and see how it immediately effects the health status.
        //
        app.UseHealthChecks("/health");

        app.Map("/createdatabase", b => b.Run(async (context) =>
        {
            await context.Response.WriteAsync("Creating the database...\n");
            await context.Response.Body.FlushAsync();

            var myContext = context.RequestServices.GetRequiredService<MyContext>();
            await myContext.Database.EnsureCreatedAsync();

            await context.Response.WriteAsync("Done\n");
            await context.Response.WriteAsync("Go to /health to see the health status\n");
        }));

        app.Map("/deletedatabase", b => b.Run(async (context) =>
        {
            await context.Response.WriteAsync("Deleting the database...\n");
            await context.Response.Body.FlushAsync();

            var myContext = context.RequestServices.GetRequiredService<MyContext>();
            await myContext.Database.EnsureDeletedAsync();

            await context.Response.WriteAsync("Done\n");
            await context.Response.WriteAsync("Go to /health to see the health status\n");
        }));

        app.Run(async (context) =>
        {
            await context.Response.WriteAsync("Go to /health to see the health status\n");
            await context.Response.WriteAsync("Go to /createdatabase to create the database\n");
            await context.Response.WriteAsync("Go to /deletedatabase to delete the database\n");
        });
    }
}
