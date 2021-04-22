// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure JWT bearer authentication.
    /// </summary>
    public static class JwtBearerExtensions
    {
        /// <summary>
        /// Enables JWT-bearer authentication using the default scheme <see cref="JwtBearerDefaults.AuthenticationScheme"/>.
        /// <para>
        /// JWT bearer authentication performs authentication by extracting and validating a JWT token from the <c>Authorization</c> request header.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder)
            => builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, _ => { });

        /// <summary>
        /// Enables JWT-bearer authentication using the default scheme <see cref="JwtBearerDefaults.AuthenticationScheme"/>.
        /// <para>
        /// JWT bearer authentication performs authentication by extracting and validating a JWT token from the <c>Authorization</c> request header.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions">A delegate that allows configuring <see cref="JwtBearerOptions"/>.</param>
        /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, Action<JwtBearerOptions> configureOptions)
            => builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, configureOptions);

        /// <summary>
        /// Enables JWT-bearer authentication using the specified scheme.
        /// <para>
        /// JWT bearer authentication performs authentication by extracting and validating a JWT token from the <c>Authorization</c> request header.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme">The authentication scheme.</param>
        /// <param name="configureOptions">A delegate that allows configuring <see cref="JwtBearerOptions"/>.</param>
        /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, Action<JwtBearerOptions> configureOptions)
            => builder.AddJwtBearer(authenticationScheme, displayName: null, configureOptions: configureOptions);

        /// <summary>
        /// Enables JWT-bearer authentication using the specified scheme.
        /// <para>
        /// JWT bearer authentication performs authentication by extracting and validating a JWT token from the <c>Authorization</c> request header.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme">The authentication scheme.</param>
        /// <param name="displayName">The display name for the authentication handler.</param>
        /// <param name="configureOptions">A delegate that allows configuring <see cref="JwtBearerOptions"/>.</param>
        /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, string? displayName, Action<JwtBearerOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>());
            return builder.AddScheme<JwtBearerOptions, JwtBearerHandler>(authenticationScheme, displayName, configureOptions);
        }
    }
}
