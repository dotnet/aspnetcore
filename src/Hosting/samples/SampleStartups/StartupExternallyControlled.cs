// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

// Note that this sample will not run. It is only here to illustrate usage patterns.

namespace SampleStartups;

public class StartupExternallyControlled : StartupBase
{
    private IHost _host;
    private readonly List<string> _urls = new List<string>();

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public override void Configure(IApplicationBuilder app)
    {
        app.Run(async (context) =>
        {
            await context.Response.WriteAsync("Hello World!");
        });
    }

    public StartupExternallyControlled()
    {
    }

    public void Start()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .UseStartup<StartupExternallyControlled>()
                    .UseUrls(_urls.ToArray());
            })
            .Start();
    }

    public async Task StopAsync()
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }
    }

    public void AddUrl(string url)
    {
        _urls.Add(url);
    }
}
