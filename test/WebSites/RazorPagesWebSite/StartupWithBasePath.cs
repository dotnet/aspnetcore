// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace RazorPagesWebSite
{
    public class StartupWithBasePath
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => options.LoginPath = "/Login");
            services.AddMvc()
                .AddCookieTempDataProvider()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizePage("/Conventions/Auth");
                    options.Conventions.AuthorizeFolder("/Conventions/AuthFolder");
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();

            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}
