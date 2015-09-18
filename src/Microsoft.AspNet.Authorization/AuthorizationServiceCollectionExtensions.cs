// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authorization;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class AuthorizationServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthorization([NotNull] this IServiceCollection services)
        {
            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Transient<IAuthorizationService, DefaultAuthorizationService>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<IAuthorizationHandler, PassThroughAuthorizationHandler>());
            return services;
        }

        public static IServiceCollection AddAuthorization([NotNull] this IServiceCollection services, [NotNull] Action<AuthorizationOptions> configure)
        {
            services.Configure(configure);
            return services.AddAuthorization();
        }
    }
}