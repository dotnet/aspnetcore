// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc().AddJsonTranscoding();
        services.AddMvc();

        #region Secret
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
        });
        services.AddGrpcSwagger();
        #endregion
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        #region Secret
        app.UseSwagger();
        if (env.IsDevelopment())
        {
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
        }
        #endregion

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<JsonTranscodingGreeterService>();
            endpoints.MapGrpcService<GreeterService>();
        });
    }
}
