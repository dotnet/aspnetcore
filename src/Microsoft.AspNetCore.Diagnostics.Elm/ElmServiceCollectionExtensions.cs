// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Diagnostics.Elm;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up Elm services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class ElmServiceCollectionExtensions
    {
        /// <summary>
        /// Adds error logging middleware services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        public static void AddElm(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.TryAddSingleton<ElmStore>();
            services.TryAddSingleton<ElmLoggerProvider>();
        }

        /// <summary>
        /// Adds error logging middleware services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action{ElmOptions}"/> to configure the provided <see cref="ElmOptions"/>.</param>
        public static void AddElm(this IServiceCollection services, Action<ElmOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddElm();
            services.Configure(setupAction);
        }
    }
}