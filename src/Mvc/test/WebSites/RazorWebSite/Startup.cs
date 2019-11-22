// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace RazorWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITagHelperComponent, TestHeadTagHelperComponent>();
            services.AddSingleton<ITagHelperComponent, TestBodyTagHelperComponent>();

            services
                .AddMvc()
                .AddRazorOptions(options =>
                {
                    options.ViewLocationExpanders.Add(new NonMainPageViewLocationExpander());
                    options.ViewLocationExpanders.Add(new BackSlashExpander());
                })
                .AddViewOptions(options =>
                {
                    options.HtmlHelperOptions.ClientValidationEnabled = false;
                    options.HtmlHelperOptions.Html5DateRenderingMode = Microsoft.AspNetCore.Mvc.Rendering.Html5DateRenderingMode.Rfc3339;
                    options.HtmlHelperOptions.IdAttributeDotReplacement = "!";
                    options.HtmlHelperOptions.ValidationMessageElement = "validationMessageElement";
                    options.HtmlHelperOptions.ValidationSummaryMessageElement = "validationSummaryElement";
                })
                .AddMvcLocalization(LanguageViewLocationExpanderFormat.SubFolder)
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddTransient<InjectedHelper>();
            services.AddTransient<TaskReturningService>();
            services.AddTransient<FrameworkSpecificHelper>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseRouting();

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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
            });
        }
    }
}
