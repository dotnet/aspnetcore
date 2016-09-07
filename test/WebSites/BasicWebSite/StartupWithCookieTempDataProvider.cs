// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
    public class StartupWithCookieTempDataProvider
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();
            app.UseMvcWithDefaultRoute();
        }
    }
}

