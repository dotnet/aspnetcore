// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Cors;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for configuring CORS using an <see cref="IMvcCoreBuilder"/>.
    /// </summary>
    public static class MvcCorsMvcCoreBuilderExtensions
    {
        /// <summary>
        /// Configures <see cref="IMvcCoreBuilder"/> to use CORS.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddCors(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddCorsServices(builder.Services);
            return builder;
        }

        /// <summary>
        /// Configures <see cref="IMvcCoreBuilder"/> to use CORS.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="setupAction">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="CorsOptions"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddCors(
            this IMvcCoreBuilder builder,
            Action<CorsOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            AddCorsServices(builder.Services);
            builder.Services.Configure(setupAction);
            
            return builder;
        }

        /// <summary>
        /// Configures <see cref="CorsOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="setupAction">The configure action.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder ConfigureCors(
            this IMvcCoreBuilder builder,
            Action<CorsOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            builder.Services.Configure(setupAction);
            return builder;
        }

        // Internal for testing.
        internal static void AddCorsServices(IServiceCollection services)
        {
            services.AddCors();

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApplicationModelProvider, CorsApplicationModelProvider>());
            services.TryAddTransient<CorsAuthorizationFilter, CorsAuthorizationFilter>();
        }
    }
}
