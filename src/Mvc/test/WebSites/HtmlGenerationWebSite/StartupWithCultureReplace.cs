// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HtmlGenerationWebSite
{
    public class StartupWithCultureReplace
    {
        private readonly Startup Startup = new Startup();

        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization();
            Startup.ConfigureServices(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRequestLocalization(options =>
            {
                options.SupportedCultures.Add(new CultureInfo("fr-FR"));
                options.SupportedCultures.Add(new CultureInfo("en-GB"));

                options.SupportedUICultures.Add(new CultureInfo("fr-FR"));
                options.SupportedUICultures.Add(new CultureInfo("fr-CA"));
                options.SupportedUICultures.Add(new CultureInfo("en-GB"));
            });

            Startup.Configure(app);
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<StartupWithCultureReplace>()
                .UseKestrel()
                .UseIISIntegration();
    }
}
