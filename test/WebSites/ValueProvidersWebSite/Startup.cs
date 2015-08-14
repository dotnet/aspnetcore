// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace ValueProvidersWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc(options =>
                {
                    options.ValueProviderFactories.Insert(1, new CustomValueProviderFactory());
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();
            
            app.UseMvcWithDefaultRoute();
        }
    }
}
