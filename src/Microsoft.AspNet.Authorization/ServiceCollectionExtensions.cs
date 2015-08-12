// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authorization;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureAuthorization([NotNull] this IServiceCollection services, [NotNull] Action<AuthorizationOptions> configure)
        {
            return services.Configure(configure);
        }

        public static IServiceCollection AddAuthorization([NotNull] this IServiceCollection services)
        {
            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Transient<IAuthorizationService, DefaultAuthorizationService>());
            services.AddTransient<IAuthorizationHandler, PassThroughAuthorizationHandler>();
            return services;
        }

        public static IServiceCollection AddAuthorization([NotNull] this IServiceCollection services, [NotNull] Action<AuthorizationOptions> configureOptions)
        {
            services.ConfigureAuthorization(configureOptions);
            return services.AddAuthorization();
        }
    }
}