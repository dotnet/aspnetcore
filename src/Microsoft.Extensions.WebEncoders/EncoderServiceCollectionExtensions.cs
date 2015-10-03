// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.WebEncoders;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EncoderServiceCollectionExtensions
    {
        public static IServiceCollection AddWebEncoders(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return AddWebEncoders(services, configureOptions: null);
        }

        public static IServiceCollection AddWebEncoders(this IServiceCollection services, Action<WebEncoderOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            // Register the default encoders
            // We want to call the 'Default' property getters lazily since they perform static caching
            services.TryAdd(ServiceDescriptor.Singleton<IHtmlEncoder>(
                CreateFactory(() => HtmlEncoder.Default, filter => new HtmlEncoder(filter))));
            services.TryAdd(ServiceDescriptor.Singleton<IJavaScriptStringEncoder>(
                CreateFactory(() => JavaScriptStringEncoder.Default, filter => new JavaScriptStringEncoder(filter))));
            services.TryAdd(ServiceDescriptor.Singleton<IUrlEncoder>(
                CreateFactory(() => UrlEncoder.Default, filter => new UrlEncoder(filter))));

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services;
        }

        private static Func<IServiceProvider, T> CreateFactory<T>(
            Func<T> defaultFactory,
            Func<ICodePointFilter, T> customFilterFactory)
        {
            return serviceProvider =>
            {
                var codePointFilter = serviceProvider?.GetService<IOptions<WebEncoderOptions>>()?
                                                      .Value?
                                                      .CodePointFilter;
                return (codePointFilter != null) ? customFilterFactory(codePointFilter) : defaultFactory();
            };
        }
    }
}
