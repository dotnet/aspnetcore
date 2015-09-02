// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.JwtBearer;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods to add Jwt Bearer authentication capabilities to an HTTP application pipeline
    /// </summary>
    public static class JwtBearerServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureJwtBearerAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<JwtBearerAuthenticationOptions> configure)
        {
            return services.Configure(configure);
        }

        public static IServiceCollection ConfigureJwtBearerAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config)
        {
            return services.ConfigureJwtBearerAuthentication(config);
        }
    }
}
