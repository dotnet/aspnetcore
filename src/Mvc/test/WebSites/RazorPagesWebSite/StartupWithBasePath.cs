// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using RazorPagesWebSite.Conventions;

namespace RazorPagesWebSite
{
    public class StartupWithBasePath
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public StartupWithBasePath(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => options.LoginPath = "/Login");
            var builder = services.AddMvc()
                .AddCookieTempDataProvider()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizePage("/Conventions/Auth");
                    options.Conventions.AuthorizeFolder("/Conventions/AuthFolder");
                    options.Conventions.AuthorizeAreaFolder("Accounts", "/RequiresAuth");
                    options.Conventions.AllowAnonymousToAreaPage("Accounts", "/RequiresAuth/AllowAnonymous");
                    options.Conventions.Add(new CustomModelTypeConvention());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Latest);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("areaRoute", "{area:exists}/{controller=Home}/{action=Index}");
                endpoints.MapRazorPages();
            });
        }
    }
}
