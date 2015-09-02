// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Cors;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcCorsMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddCors([NotNull] this IMvcCoreBuilder builder)
        {
            AddCorsServices(builder.Services);
            return builder;
        }

        public static IMvcCoreBuilder AddCors(
            [NotNull] this IMvcCoreBuilder builder,
            [NotNull] Action<CorsOptions> setupAction)
        {
            AddCorsServices(builder.Services);

            if (setupAction != null)
            {
                builder.Services.Configure(setupAction);
            }

            return builder;
        }

        public static IMvcCoreBuilder ConfigureCors(
            [NotNull] this IMvcCoreBuilder builder,
            [NotNull] Action<CorsOptions> setupAction)
        {
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
