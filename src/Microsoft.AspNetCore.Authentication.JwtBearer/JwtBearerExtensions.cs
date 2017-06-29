// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class JwtBearerExtensions
    {
        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder)
            => builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, Action<JwtBearerOptions> configureOptions)
            => builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, Action<JwtBearerOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>());
            return builder.AddScheme<JwtBearerOptions, JwtBearerHandler>(authenticationScheme, configureOptions);
        }


        // REMOVE once callers updated
        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services) 
            => services.AddJwtBearerAuthentication(JwtBearerDefaults.AuthenticationScheme, _ => { });

        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services, Action<JwtBearerOptions> configureOptions) 
            => services.AddJwtBearerAuthentication(JwtBearerDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services, string authenticationScheme, Action<JwtBearerOptions> configureOptions)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>());
            return services.AddScheme<JwtBearerOptions, JwtBearerHandler>(authenticationScheme, configureOptions);
        }
    }
}
