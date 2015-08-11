// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Localization;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring MVC view localization.
    /// </summary>
    public static class MvcLocalizationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds MVC localization to the application.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMvcLocalization([NotNull] this IServiceCollection services)
        {
            return AddMvcLocalization(services, LanguageViewLocationExpanderFormat.Suffix);
        }

        /// <summary>
        ///  Adds MVC localization to the application.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="format">The view format for localized views.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMvcLocalization(
            [NotNull] this IServiceCollection services,
            LanguageViewLocationExpanderFormat format)
        {
            services.Configure<RazorViewEngineOptions>(
                options =>
                {
                    options.ViewLocationExpanders.Add(new LanguageViewLocationExpander(format));
                },
                DefaultOrder.DefaultFrameworkSortOrder);

            services.TryAdd(ServiceDescriptor.Singleton<IHtmlLocalizerFactory, HtmlLocalizerFactory>());
            services.TryAdd(ServiceDescriptor.Transient(typeof(IHtmlLocalizer<>), typeof(HtmlLocalizer<>)));
            services.TryAdd(ServiceDescriptor.Transient<IViewLocalizer, ViewLocalizer>());
            if (!services.Any(sd => sd.ServiceType == typeof(IHtmlEncoder)))
            {
                services.TryAdd(ServiceDescriptor.Instance<IHtmlEncoder>(HtmlEncoder.Default));
            }
            return services.AddLocalization();
        }
    }
}