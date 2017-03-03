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
                .AddMvc()
                .AddCookieTempDataProvider()
                .AddRazorPagesOptions(options =>
                {
                    options.RootDirectory = "/Pages";
                    options.AuthorizePage("/Conventions/Auth", string.Empty);
                    options.AuthorizeFolder("/Conventions/AuthFolder", string.Empty);
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                LoginPath = "/Login",
                AutomaticAuthenticate = true,
                AutomaticChallenge = true
            });

            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}
