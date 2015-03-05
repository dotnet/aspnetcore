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

        // Review: Need UseDefaultSubkey parameter?
        public static IServiceCollection AddAuthorization([NotNull] this IServiceCollection services, IConfiguration config = null, Action<AuthorizationOptions> configureOptions = null)
        {
            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Transient<IAuthorizationService, DefaultAuthorizationService>());
            services.AddTransient<IAuthorizationHandler, ClaimsAuthorizationHandler>();
            services.AddTransient<IAuthorizationHandler, DenyAnonymousAuthorizationHandler>();
            services.AddTransient<IAuthorizationHandler, PassThroughAuthorizationHandler>();
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            return services;
        }
    }
}
