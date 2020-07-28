// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        public virtual void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "areaRoute",
                    pattern: "{area:exists}/{controller}/{action}/{id?}",
                    defaults: new { action = "Index" });
                endpoints.MapControllerRoute(
                    name: "productRoute",
                    pattern: "Product/{action}",
                    defaults: new { controller = "Product" });
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action}/{id?}",
                    defaults: new { controller = "HtmlGeneration_Home", action = "Index" });

                endpoints.MapRazorPages();
            });
        }

        public static Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseStartup<Startup>()
                        .UseKestrel()
                        .UseIISIntegration();
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            return host.RunAsync();
        }

        protected virtual void ConfigureMvcOptions(MvcOptions options)
        {
        }
    }
}
