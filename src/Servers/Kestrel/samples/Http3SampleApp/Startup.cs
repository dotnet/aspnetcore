// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        app.Use(async (context, next) =>
        {
            var feature = (IHttpWebTransportSessionFeature)context.Features[typeof(IHttpWebTransportSessionFeature)];
            if (feature is not null)
            {
                var session = await feature.AcceptAsync();
            }
            else
            {
                await next(context);
            }

            await Task.Delay(TimeSpan.FromMinutes(150));
        });
    }
}
