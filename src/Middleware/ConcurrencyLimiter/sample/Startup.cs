// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ConcurrencyLimiterSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddStackPolicy(options =>
        {
            options.MaxConcurrentRequests = 2;
            options.RequestQueueLimit = 25;
        });
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        app.UseConcurrencyLimiter();
#pragma warning restore CS0618 // Type or member is obsolete
        app.Run(async context =>
        {
            Task.Delay(100).Wait(); // 100ms sync-over-async

            await context.Response.WriteAsync("Hello World!");
        });
    }

    public static Task Main(string[] args)
    {
        return new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseKestrel()
                .UseStartup<Startup>();
            })
            .Build()
            .RunAsync();
    }
}
