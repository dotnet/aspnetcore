// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace RazorWebSite;

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
            .AddMvcLocalization(LanguageViewLocationExpanderFormat.SubFolder);

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
