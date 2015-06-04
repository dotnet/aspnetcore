// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace ResponseCacheWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.ConfigureMvcCaching(options =>
            {
                options.CacheProfiles.Add(
                    "PublicCache30Sec", new CacheProfile
                    {
                        Duration = 30,
                        Location = ResponseCacheLocation.Any
                    });

                options.CacheProfiles.Add(
                    "PrivateCache30Sec", new CacheProfile
                    {
                        Duration = 30,
                        Location = ResponseCacheLocation.Client
                    });

                options.CacheProfiles.Add(
                    "NoCache", new CacheProfile
                    {
                        NoStore = true,
                        Duration = 0,
                        Location = ResponseCacheLocation.None
                    });

                options.CacheProfiles.Add(
                    "PublicCache30SecVaryByAcceptHeader", new CacheProfile
                    {
                        Duration = 30,
                        Location = ResponseCacheLocation.Any,
                        VaryByHeader = "Accept"
                    });
            });

            services.ConfigureMvc(options =>
            { 
                options.Filters.Add(new ResponseCacheFilter(new CacheProfile
                {
                    NoStore = true,
                    VaryByHeader = "TestDefault",
                }));
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();
            app.UseMvcWithDefaultRoute();
        }
    }
}