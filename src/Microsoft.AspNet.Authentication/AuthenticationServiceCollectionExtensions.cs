// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class AuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthentication([NotNull] this IServiceCollection services)
        {
            services.AddWebEncoders();
            services.AddDataProtection();
            return services;
        }

        public static IServiceCollection AddAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<SharedAuthenticationOptions> configureOptions)
        {
            services.Configure(configureOptions);
            return services.AddAuthentication();
        }
    }
}
