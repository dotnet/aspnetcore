// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RequestDecompressionSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRequestDecompression(options =>
        {
            options.DecompressionProviders.Add("custom", new CustomDecompressionProvider());
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRequestDecompression();
        app.Map("/test", testApp =>
        {
            testApp.Run(async context =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var decompressedBody = await reader.ReadToEndAsync(context.RequestAborted);

                await context.Response.WriteAsync(decompressedBody, context.RequestAborted);
            });
        });
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseKestrel()
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole()
                    .SetMinimumLevel(LogLevel.Debug);
                })
                .UseStartup<Startup>();
            }).Build();

        return host.RunAsync();
    }
}
