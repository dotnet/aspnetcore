// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SessionSample;

public class Startup
{
    public Startup()
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Uncomment the following line to use the in-memory implementation of IDistributedCache
        services.AddDistributedMemoryCache();

        // Uncomment the following line to use the Microsoft SQL Server implementation of IDistributedCache
        // and add a PackageReference to Microsoft.Extensions.Caching.SqlServer in the .csrpoj.
        // Note that this would require setting up the session state database.
        //services.AddDistributedSqlServerCache(o =>
        //{
        //    o.ConnectionString = Configuration["AppSettings:ConnectionString"];
        //    o.SchemaName = "dbo";
        //    o.TableName = "Sessions";
        //});

        // Uncomment the following line to use the Redis implementation of IDistributedCache
        // and add a PackageReference to Microsoft.Extensions.Caching.StackExchangeRedis in the .csrpoj.
        // This will override any previously registered IDistributedCache service.
        //services.AddStackExchangeRedisCache(o =>
        //{
        //    o.Configuration = "localhost";
        //    o.InstanceName = "SampleInstance";
        //});

        services.AddSession(o =>
        {
            o.IdleTimeout = TimeSpan.FromSeconds(10);
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseSession();

        app.Map("/session", subApp =>
        {
            subApp.Run(async context =>
            {
                int visits = 0;
                visits = context.Session.GetInt32("visits") ?? 0;
                context.Session.SetInt32("visits", ++visits);
                await context.Response.WriteAsync("Counting: You have visited our page this many times: " + visits);
            });
        });

        app.Run(async context =>
        {
            int visits = 0;
            visits = context.Session.GetInt32("visits") ?? 0;
            await context.Response.WriteAsync("<html><body>");
            if (visits == 0)
            {
                await context.Response.WriteAsync("Your session has not been established.<br>");
                await context.Response.WriteAsync(DateTime.Now + "<br>");
                await context.Response.WriteAsync("<a href=\"/session\">Establish session</a>.<br>");
            }
            else
            {
                context.Session.SetInt32("visits", ++visits);
                await context.Response.WriteAsync("Your session was located, you've visited the site this many times: " + visits);
            }
            await context.Response.WriteAsync("</body></html>");
        });
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureLogging(factory => factory.AddConsole())
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>();
            }).Build();

        return host.RunAsync();
    }
}
