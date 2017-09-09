// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace RazorPagesWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => options.LoginPath = "/Login");
            services.AddMvc()
                .AddMvcLocalization()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizePage("/HelloWorldWithAuth");
                    options.Conventions.AuthorizeFolder("/Pages/Admin");
                    options.Conventions.AllowAnonymousToPage("/Pages/Admin/Login");
                    options.Conventions.AddPageRoute("/HelloWorldWithRoute", "Different-Route/{text}");
                    options.Conventions.AddPageRoute("/Pages/NotTheRoot", string.Empty);
                })
                .WithRazorPagesAtContentRoot();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();

            app.UseStaticFiles();

            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("fr-FR"),
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            app.UseMvc();
        }
    }
}
