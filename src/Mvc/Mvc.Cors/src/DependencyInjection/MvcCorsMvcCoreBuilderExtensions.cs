// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Cors;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcCorsMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddCors(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddCorsServices(builder.Services);
            return builder;
        }

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
