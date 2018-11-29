// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace HtmlGenerationWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container. Change default FormTagHelper.AntiForgery to false. Usually
            // null which is interpreted as true unless element includes an action attribute.
            services.AddMvc(ConfigureMvcOptions)
                .InitializeTagHelper<FormTagHelper>((helper, _) => helper.Antiforgery = false)
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddSingleton(typeof(ISignalTokenProviderService<>), typeof(SignalTokenProviderService<>));
            services.AddSingleton<ProductsService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "areaRoute",
                    template: "{area:exists}/{controller}/{action}/{id?}",
                    defaults: new { action = "Index" });
                routes.MapRoute(
                    name: "productRoute",
                    template: "Product/{action}",
                    defaults: new { controller = "Product" });
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "HtmlGeneration_Home", action = "Index" });
            });
        }

        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args)
                .Build();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseKestrel()
                .UseIISIntegration();

        protected virtual void ConfigureMvcOptions(MvcOptions options)
        {
        }
    }
}
