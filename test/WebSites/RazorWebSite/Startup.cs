// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace RazorWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddRazorOptions(options =>
                {
                    options.ViewLocationExpanders.Add(new NonMainPageViewLocationExpander());
#if NET451
                    options.ParseOptions = options.ParseOptions.WithPreprocessorSymbols("DNX451", "NET451_CUSTOM_DEFINE");
#endif
                })
                .AddViewOptions(options =>
                {
                    options.HtmlHelperOptions.ClientValidationEnabled = false;
                    options.HtmlHelperOptions.Html5DateRenderingMode = Microsoft.AspNetCore.Mvc.Rendering.Html5DateRenderingMode.Rfc3339;
                    options.HtmlHelperOptions.IdAttributeDotReplacement = "!";
                    options.HtmlHelperOptions.ValidationMessageElement = "validationMessageElement";
                    options.HtmlHelperOptions.ValidationSummaryMessageElement = "validationSummaryElement";
                })
                .AddViewLocalization(LanguageViewLocationExpanderFormat.SubFolder);

            services.AddTransient<InjectedHelper>();
            services.AddTransient<TaskReturningService>();
            services.AddTransient<FrameworkSpecificHelper>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-GB", "en-US"),
                SupportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("fr"),
                    new CultureInfo("en-GB"),
                    new CultureInfo("en-US"),
                },
                SupportedUICultures = new List<CultureInfo>
                {
                    new CultureInfo("fr"),
                    new CultureInfo("en-GB"),
                    new CultureInfo("en-US"),
                }
            });

            app.UseMvcWithDefaultRoute();
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseDefaultHostingConfiguration(args)
                .UseStartup<Startup>()
                .UseKestrel()
                .UseIISIntegration()
                .Build();

            host.Run();
        }
    }
}

