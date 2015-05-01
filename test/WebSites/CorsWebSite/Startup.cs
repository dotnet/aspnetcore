// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace CorsWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.ConfigureCors(options =>
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
                    "WithCredentialsAnyOrigin",
                    builder =>
                    {
                        builder.AllowCredentials()
                               .AllowAnyOrigin()
                               .AllowAnyHeader()
                               .WithMethods("PUT", "POST")
                               .WithExposedHeaders("exposed1", "exposed2");
                    });
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseMvc();
        }
    }
}