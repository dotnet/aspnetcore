// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc().AddJsonTranscoding();
        services.AddMvc();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<JsonTranscodingGreeterService>();
            endpoints.MapGrpcService<GreeterService>();
        });
    }
}
