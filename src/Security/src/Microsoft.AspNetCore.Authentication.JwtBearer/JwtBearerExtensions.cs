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
            => builder.AddJwtBearer(authenticationScheme, displayName: null, configureOptions: configureOptions);

        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<JwtBearerOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>());
            return builder.AddScheme<JwtBearerOptions, JwtBearerHandler>(authenticationScheme, displayName, configureOptions);
        }
    }
}
