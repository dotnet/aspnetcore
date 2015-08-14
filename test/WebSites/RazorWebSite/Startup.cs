// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.DependencyInjection;

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
                    options.ViewLocationExpanders.Add(new CustomPartialDirectoryViewLocationExpander());
                })
                .AddViewOptions(options =>
                {
                    options.HtmlHelperOptions.ClientValidationEnabled = false;
                    options.HtmlHelperOptions.Html5DateRenderingMode = Microsoft.AspNet.Mvc.Rendering.Html5DateRenderingMode.Rfc3339;
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
            app.UseRequestLocalization();
            
            app.UseMvcWithDefaultRoute();
        }
    }
}
