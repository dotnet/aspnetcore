// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.Localization.Internal
{
    public static class MvcLocalizationServices
    {
        public static void AddLocalizationServices(
            IServiceCollection services,
            LanguageViewLocationExpanderFormat format,
            Action<LocalizationOptions> setupAction)
        {
            AddMvcViewLocalizationServices(services, format, setupAction);

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
            LanguageViewLocationExpanderFormat format,
            Action<LocalizationOptions> setupAction)
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
}