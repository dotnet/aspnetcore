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
        private readonly IHostingEnvironment _hostingEnvironment;

        public StartupWithBasePath(IHostingEnvironment hostingEnvironment)
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
                    options.AllowAreas = true;
                    options.Conventions.AuthorizePage("/Conventions/Auth");
                    options.Conventions.AuthorizeFolder("/Conventions/AuthFolder");
                    options.Conventions.AuthorizeAreaFolder("Accounts", "/RequiresAuth");
                    options.Conventions.AllowAnonymousToAreaPage("Accounts", "/RequiresAuth/AllowAnonymous");
                    options.Conventions.Add(new CustomModelTypeConvention());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            // Ensure we don't have code paths that call IFileProvider.Watch in the default code path.
            // Comment this code block if you happen to run this site in Development.
            builder.AddRazorOptions(options =>
            {
                options.FileProviders.Clear();
                options.FileProviders.Add(new NonWatchingPhysicalFileProvider(_hostingEnvironment.ContentRootPath));
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute("areaRoute", "{area:exists}/{controller=Home}/{action=Index}");
            });
        }
    }
}
