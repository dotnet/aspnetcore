// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace RazorPagesWebSite
{
    public class StartupWithBasePath
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddCookieAuthentication(options => options.LoginPath = "/Login")
                .AddMvc()
                .AddCookieTempDataProvider()
                .AddRazorPagesOptions(options =>
                {
                    options.RootDirectory = "/Pages";
                    options.AuthorizePage("/Conventions/Auth");
                    options.AuthorizeFolder("/Conventions/AuthFolder");
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseAuthentication();

            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}
