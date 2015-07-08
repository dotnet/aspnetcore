// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcJsonMvcBuilderExtensions
    {
        public static IMvcBuilder AddCors([NotNull] this IMvcBuilder builder)
        {
            AddCorsServices(builder.Services);
            return builder;
        }

        public static IMvcBuilder AddCors(
            [NotNull] this IMvcBuilder builder,
            [NotNull] Action<CorsOptions> setupAction)
        {
            AddCorsServices(builder.Services);

            if (setupAction != null)
            {
                builder.Services.Configure(setupAction);
            }

            return builder;
        }

        public static IMvcBuilder ConfigureCors(
            [NotNull] this IMvcBuilder builder,
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
