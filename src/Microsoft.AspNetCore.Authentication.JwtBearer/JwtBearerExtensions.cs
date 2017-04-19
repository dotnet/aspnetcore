// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class JwtBearerExtensions
    {
        /// <summary>
        /// Adds JwtBearer authentication with options bound against the "Bearer" section 
        /// from the IConfiguration in the service container.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services)
        {
            services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerConfigureOptions>();
            return services.AddJwtBearerAuthentication(JwtBearerDefaults.AuthenticationScheme, _ => { });
        }

        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services, Action<JwtBearerOptions> configureOptions) 
            => services.AddJwtBearerAuthentication(JwtBearerDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services, string authenticationScheme, Action<JwtBearerOptions> configureOptions)
        {
            return services.AddScheme<JwtBearerOptions, JwtBearerHandler>(authenticationScheme, configureOptions);
        }
    }
}
