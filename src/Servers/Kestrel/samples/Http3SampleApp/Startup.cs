// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Http3SampleApp;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            var memory = new Memory<byte>(new byte[4096]);
            var length = await context.Request.Body.ReadAsync(memory);

            AppContext.TryGetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams", out var isWebTransport);
            if (isWebTransport)
            {
                context.Response.Headers.Append("sec-webtransport-http3-draft", "draft02");
                await context.Response.Body.FlushAsync();

                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        });
    }
}
