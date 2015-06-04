// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Localization;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.DependencyInjection;

namespace RazorWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container
            services.AddMvc();
            services.AddTransient<InjectedHelper>();
            services.AddTransient<TaskReturningService>();
            services.AddTransient<FrameworkSpecificHelper>();
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new CustomPartialDirectoryViewLocationExpander());
            });
            services.ConfigureMvcViews(options =>
            {
                options.HtmlHelperOptions.ClientValidationEnabled = false;
                options.HtmlHelperOptions.Html5DateRenderingMode = Microsoft.AspNet.Mvc.Rendering.Html5DateRenderingMode.Rfc3339;
                options.HtmlHelperOptions.IdAttributeDotReplacement = "!";
                options.HtmlHelperOptions.ValidationMessageElement = "validationMessageElement";
                options.HtmlHelperOptions.ValidationSummaryMessageElement = "validationSummaryElement";
            });
            services.AddMvcLocalization(LanguageViewLocationExpanderOption.SubFolder);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRequestLocalization();

            // Add MVC to the request pipeline
            app.UseMvcWithDefaultRoute();
        }
    }
}
