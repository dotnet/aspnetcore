// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpLogging;

namespace Logging.W3C.Sample;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddW3CLogging(logging =>
        {
            // Log all W3C fields
            logging.LoggingFields = W3CLoggingFields.All;
            logging.AdditionalRequestHeaders.Clear();
            logging.AdditionalRequestHeaders.Add("x-forwarded-for");
            logging.AdditionalRequestHeaders.Add("x-client-ssl-protocol");
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseW3CLogging();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map("/", async context =>
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello World!");
            });
        });
    }
}
