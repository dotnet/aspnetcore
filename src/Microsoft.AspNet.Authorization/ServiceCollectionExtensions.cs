// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authorization;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureClaimsTransformation([NotNull] this IServiceCollection services, [NotNull] Action<ClaimsTransformationOptions> configure)
        {
            return services.Configure(configure);
        }

        public static IServiceCollection ConfigureAuthorization([NotNull] this IServiceCollection services, [NotNull] Action<AuthorizationOptions> configure)
        {
            return services.Configure(configure);
        }

        public static IServiceCollection AddAuthorization([NotNull] this IServiceCollection services)
        {
            return services.AddAuthorization(configureOptions: null);
        }

        public static IServiceCollection AddAuthorization([NotNull] this IServiceCollection services, Action<AuthorizationOptions> configureOptions)
        {
            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Transient<IAuthorizationService, DefaultAuthorizationService>());
            services.AddTransient<IAuthorizationHandler, ClaimsAuthorizationHandler>();
            services.AddTransient<IAuthorizationHandler, DenyAnonymousAuthorizationHandler>();
            services.AddTransient<IAuthorizationHandler, PassThroughAuthorizationHandler>();
            return services;
        }
    }
}