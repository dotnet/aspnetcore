// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.AspNetCore.Http.Features;

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
            context.Response.Headers["test"] = "foo";
            // for testing
            await context.Response.WriteAsync($"Hello World! {context.Request.Protocol} {context.Connection.ClientCertificate?.Subject}");
        });
    }
}
