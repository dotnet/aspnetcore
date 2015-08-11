// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
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

        public static IServiceCollection ConfigureClaimsTransformation([NotNull] this IServiceCollection services, [NotNull] Action<ClaimsTransformationOptions> configure)
        {
            return services.Configure(configure);
        }

        public static IServiceCollection ConfigureClaimsTransformation([NotNull] this IServiceCollection services, [NotNull] Func<ClaimsPrincipal, ClaimsPrincipal> transform)
        {
            return services.Configure<ClaimsTransformationOptions>(o => o.Transformer = new ClaimsTransformer { TransformSyncDelegate = transform });
        }

        public static IServiceCollection ConfigureClaimsTransformation([NotNull] this IServiceCollection services, [NotNull] Func<ClaimsPrincipal, Task<ClaimsPrincipal>> asyncTransform)
        {
            return services.Configure<ClaimsTransformationOptions>(o => o.Transformer = new ClaimsTransformer { TransformAsyncDelegate = asyncTransform });
        }
    }
}
