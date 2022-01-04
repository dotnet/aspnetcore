// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Rewrite;

namespace RewriteSample;

public class Startup
{
    public Startup(IWebHostEnvironment environment)
    {
        Environment = environment;
    }

    public IWebHostEnvironment Environment { get; private set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<RewriteOptions>(options =>
        {
            options.AddRedirect("(.*)/$", "$1")
                   .AddRewrite(@"app/(\d+)", "app?id=$1", skipRemainingRules: false)
                   .AddRedirectToHttps(302, 5001)
                   .AddIISUrlRewrite(Environment.ContentRootFileProvider, "UrlRewrite.xml")
                   .AddApacheModRewrite(Environment.ContentRootFileProvider, "Rewrite.txt");
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRewriter();

        app.Run(context =>
        {
            return context.Response.WriteAsync($"Rewritten Url: {context.Request.Path + context.Request.QueryString}");
        });
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 5000);
                    options.Listen(IPAddress.Loopback, 5001, listenOptions =>
                    {
                        // Configure SSL
                        listenOptions.UseHttps("testCert.pfx", "testPassword");
                    });
                })
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory());
            }).Build();

        return host.RunAsync();
    }
}
