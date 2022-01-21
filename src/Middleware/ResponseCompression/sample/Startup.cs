// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;

namespace ResponseCompressionSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        services.AddResponseCompression(options =>
        {
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<CustomCompressionProvider>();
            // .Append(TItem) is only available on Core.
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "image/svg+xml" });

            ////Example of using excluded and wildcard MIME types:
            ////Compress all MIME types except various media types, but do compress SVG images.
            //options.MimeTypes = new[] { "*/*", "image/svg+xml" };
            //options.ExcludedMimeTypes = new[] { "image/*", "audio/*", "video/*" };
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseResponseCompression();

        app.Map("/testfile1kb.txt", fileApp =>
        {
            fileApp.Run(context =>
            {
                context.Response.ContentType = "text/plain";
                return context.Response.SendFileAsync("testfile1kb.txt");
            });
        });

        app.Map("/trickle", trickleApp =>
        {
            trickleApp.Run(async context =>
            {
                context.Response.ContentType = "text/plain";
                // Disables compression on net451 because that GZipStream does not implement Flush.
                context.Features.Get<IHttpResponseBodyFeature>().DisableBuffering();

                for (int i = 0; i < 100; i++)
                {
                    await context.Response.WriteAsync("a");
                    await context.Response.Body.FlushAsync();
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            });
        });

        app.Run(async context =>
        {
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(LoremIpsum.Text);
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
