// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CorsWebSite;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(ConfigureMvcOptions);
        services.Configure<CorsOptions>(options =>
        {
            options.AddPolicy(
                "AllowAnySimpleRequest",
                builder =>
                {
                    builder.AllowAnyOrigin()
                           .WithMethods("GET", "POST", "HEAD");
                });

            options.AddPolicy(
                "AllowSpecificOrigin",
                builder =>
                {
                    builder.WithOrigins("http://example.com");
                });

            options.AddPolicy(
                "WithCredentials",
                builder =>
                {
                    builder.AllowCredentials()
                           .WithOrigins("http://example.com");
                });

            options.AddPolicy(
                "WithCredentialsAndOtherSettings",
                builder =>
                {
                    builder.AllowCredentials()
                           .WithOrigins("http://example.com")
                           .AllowAnyHeader()
                           .WithMethods("PUT", "POST")
                           .WithExposedHeaders("exposed1", "exposed2");
                });

            options.AddPolicy(
                "AllowAll",
                builder =>
                {
                    builder.AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowAnyOrigin();
                });

            options.AddPolicy(
                "Allow example.com",
                builder =>
                {
                    builder.AllowCredentials()
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .WithOrigins("http://example.com");
                });
        });
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseCors();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }

    protected virtual void ConfigureMvcOptions(MvcOptions options)
    {
    }
}
