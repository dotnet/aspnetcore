// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up web encoding services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class EncoderServiceCollectionExtensions
    {
        /// <summary>
        /// Adds <see cref="HtmlEncoder"/>, <see cref="JavaScriptEncoder"/> and <see cref="UrlEncoder"/>
        /// to the specified <paramref name="services" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddWebEncoders(this IServiceCollection services)
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

            return services;
        }

        /// <summary>
        /// Adds <see cref="HtmlEncoder"/>, <see cref="JavaScriptEncoder"/> and <see cref="UrlEncoder"/>
        /// to the specified <paramref name="services" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="setupAction">An <see cref="Action{WebEncoderOptions}"/> to configure the provided <see cref="WebEncoderOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddWebEncoders(this IServiceCollection services, Action<WebEncoderOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddWebEncoders();
            services.Configure(setupAction);

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
