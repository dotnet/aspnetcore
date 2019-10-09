// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public static class MvcWebApplicationBuilderExtensions
    {
        /// <summary>
        /// Sets up an <see cref="IStartupFilter"/> that configures the <see cref="CultureReplacerMiddleware"/> at the
        /// beginning of the pipeline to change the <see cref="CultureInfo.CurrentCulture"/> and <see cref="CultureInfo.CurrentUICulture"/>
        /// of the thread so that they match the cultures in <paramref name="culture"/> and <paramref name="uiCulture"/> for the rest of the
        /// <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="culture">The culture to use when processing <see cref="HttpRequest"/>.</param>
        /// <param name="uiCulture">The UI culture to use when processing <see cref="HttpRequest"/>.</param>
        /// <returns>An instance of this <see cref="IWebHostBuilder"/></returns>
        public static IWebHostBuilder UseRequestCulture<TStartup>(this IWebHostBuilder builder, string culture, string uiCulture)
            where TStartup : class
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            if (uiCulture == null)
            {
                throw new ArgumentNullException(nameof(uiCulture));
            }

            builder.ConfigureServices(services =>
            {
                services.TryAddSingleton(new TestCulture
                {
                    Culture = culture,
                    UICulture = uiCulture
                });
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, CultureReplacerStartupFilter>());
            });

            return builder;
        }
    }
}
