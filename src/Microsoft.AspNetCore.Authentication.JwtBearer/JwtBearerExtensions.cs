// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.JwtBearer.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class JwtBearerExtensions
    {
        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services) 
            => services.AddJwtBearerAuthentication(JwtBearerDefaults.AuthenticationScheme, _ => { });

        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services, Action<JwtBearerOptions> configureOptions) 
            => services.AddJwtBearerAuthentication(JwtBearerDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services, string authenticationScheme, Action<JwtBearerOptions> configureOptions)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>());
            services.AddSingleton<ConfigureDefaultOptions<JwtBearerOptions>, JwtBearerConfigureOptions>();
            return services.AddScheme<JwtBearerOptions, JwtBearerHandler>(authenticationScheme, configureOptions);
        }
    }
}
