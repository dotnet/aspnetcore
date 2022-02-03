// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.Localization;

internal static class MvcLocalizationServices
{
    public static void AddLocalizationServices(
        IServiceCollection services,
        LanguageViewLocationExpanderFormat format,
        Action<LocalizationOptions>? setupAction)
    {
        AddMvcViewLocalizationServices(services, format);

        if (setupAction == null)
        {
            services.AddLocalization();
        }
        else
        {
            services.AddLocalization(setupAction);
        }
    }

    // To enable unit testing only 'MVC' specific services
    public static void AddMvcViewLocalizationServices(
        IServiceCollection services,
        LanguageViewLocationExpanderFormat format)
    {
        services.Configure<RazorViewEngineOptions>(
            options =>
            {
                options.ViewLocationExpanders.Add(new LanguageViewLocationExpander(format));
            });

        services.TryAdd(ServiceDescriptor.Singleton<IHtmlLocalizerFactory, HtmlLocalizerFactory>());
        services.TryAdd(ServiceDescriptor.Transient(typeof(IHtmlLocalizer<>), typeof(HtmlLocalizer<>)));
        services.TryAdd(ServiceDescriptor.Transient<IViewLocalizer, ViewLocalizer>());
    }
}
