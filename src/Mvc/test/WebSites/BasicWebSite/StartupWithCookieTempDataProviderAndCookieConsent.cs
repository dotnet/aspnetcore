// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
    public class StartupWithCookieTempDataProviderAndCookieConsent
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.Configure<CookiePolicyOptions>(o =>
            {
                o.CheckConsentNeeded = httpContext => true;
            });

            services.ConfigureBaseWebSiteAuthPolicies();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseCookiePolicy();

            app.UseMvcWithDefaultRoute();
        }
    }
}

