// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics.Elm;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElmServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an <see cref="ElmStore"/> and configures default <see cref="ElmOptions"/>.
        /// </summary>
        public static IServiceCollection AddElm([NotNull] this IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<ElmStore>();
            return services;
        }

        /// <summary>
        /// Configures a set of <see cref="ElmOptions"/> for the application.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="configureOptions">The <see cref="ElmOptions"/> which need to be configured.</param>
        public static void ConfigureElm(
            [NotNull] this IServiceCollection services, 
            [NotNull] Action<ElmOptions> configureOptions)
        {
            services.Configure(configureOptions);
        }
    }
}