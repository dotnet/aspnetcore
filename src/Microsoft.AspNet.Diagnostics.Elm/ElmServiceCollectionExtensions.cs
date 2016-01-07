// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics.Elm;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElmServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an <see cref="ElmStore"/> and configures default <see cref="ElmOptions"/>.
        /// </summary>
        public static IServiceCollection AddElm(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            
            services.AddSingleton<ElmStore>();
            return services;
        }

        /// <summary>
        /// Configures a set of <see cref="ElmOptions"/> for the application.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="configureOptions">The <see cref="ElmOptions"/> which need to be configured.</param>
        public static void ConfigureElm(
            this IServiceCollection services,
            Action<ElmOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.Configure(configureOptions);
        }
    }
}