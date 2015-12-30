// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EncoderServiceCollectionExtensions
    {
        /// <summary>
        /// Adds <see cref="HtmlEncoder"/>, <see cref="JavaScriptEncoder"/> and <see cref="UrlEncoder"/>
        /// to the specified <paramref name="services" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance after the encoders have been added.</returns>
        public static IServiceCollection AddWebEncoders(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return AddWebEncoders(services, configureOptions: null);
        }

        /// <summary>
        /// Adds <see cref="HtmlEncoder"/>, <see cref="JavaScriptEncoder"/> and <see cref="UrlEncoder"/>
        /// to the specified <paramref name="services" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configureOptions">A callback to configure <see cref="WebEncoderOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance after the encoders have been added.</returns>
        public static IServiceCollection AddWebEncoders(this IServiceCollection services, Action<WebEncoderOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            // Register the default encoders
            // We want to call the 'Default' property getters lazily since they perform static caching
            services.TryAddSingleton(
                CreateFactory(() => HtmlEncoder.Default, settings => HtmlEncoder.Create(settings)));
            services.TryAddSingleton(
                CreateFactory(() => JavaScriptEncoder.Default, settings => JavaScriptEncoder.Create(settings)));
            services.TryAddSingleton(
                CreateFactory(() => UrlEncoder.Default, settings => UrlEncoder.Create(settings)));

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services;
        }

        private static Func<IServiceProvider, TService> CreateFactory<TService>(
            Func<TService> defaultFactory,
            Func<TextEncoderSettings, TService> customSettingsFactory)
        {
            return serviceProvider =>
            {
                var settings = serviceProvider
                    ?.GetService<IOptions<WebEncoderOptions>>()
                    ?.Value
                    ?.TextEncoderSettings;
                return (settings != null) ? customSettingsFactory(settings) : defaultFactory();
            };
        }
    }
}
