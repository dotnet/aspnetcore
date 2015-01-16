// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureAuthorization([NotNull] this IServiceCollection services, [NotNull] Action<AuthorizationOptions> configure)
        {
            return services.Configure(configure);
        }

        // Review: Need UseDefaultSubkey parameter?
        public static IServiceCollection AddAuthorization([NotNull] this IServiceCollection services, IConfiguration config = null, Action<AuthorizationOptions> configureOptions = null)
        {
            var describe = new ServiceDescriber(config);
            services.AddOptions(config);
            services.TryAdd(describe.Transient<IAuthorizationService, DefaultAuthorizationService>());
            services.Add(describe.Transient<IAuthorizationHandler, ClaimsAuthorizationHandler>());
            services.Add(describe.Transient<IAuthorizationHandler, DenyAnonymousAuthorizationHandler>());
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            return services;
        }
    }
}
