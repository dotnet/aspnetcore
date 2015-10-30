// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.WebEncoders;

namespace Microsoft.AspNet.Mvc.Localization.Internal
{
    public static class MvcLocalizationServices
    {
        public static void AddLocalizationServices(
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
            if (!services.Any(sd => sd.ServiceType == typeof(HtmlEncoder)))
            {
                services.TryAdd(ServiceDescriptor.Instance<HtmlEncoder>(HtmlEncoder.Default));
            }

            services.AddLocalization(setupAction);
        }
    }
}