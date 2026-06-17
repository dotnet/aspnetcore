// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpLogging;

namespace HttpLogging.Sample;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.All;
        });
        services.AddHttpLoggingInterceptor<SampleHttpLoggingInterceptor>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseHttpLogging();
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
