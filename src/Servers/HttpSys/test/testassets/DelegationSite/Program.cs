// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DelegationSite;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseHttpSys(options =>
                {
                    options.RequestQueueName = Environment.GetEnvironmentVariable("queue");
                })
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello from delegatee");
                    });
                });
            });

        using var host = builder.Build();
        host.Run();
    }
}

