// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="OpenIdConnectAuthenticationMiddleware"/>
    /// </summary>
    public static class OpenIdConnectAuthenticationExtensions
    {
        /// <summary>
        /// Adds the <see cref="OpenIdConnectAuthenticationMiddleware"/> into the ASP.NET runtime.
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="options">Options which control the processing of the OpenIdConnect protocol and token validation.</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseOpenIdConnectAuthentication(this IApplicationBuilder app, Action<OpenIdConnectAuthenticationOptions> configureOptions = null, string optionsName = "")
        {
            return app.UseMiddleware<OpenIdConnectAuthenticationMiddleware>(
                 new ConfigureOptions<OpenIdConnectAuthenticationOptions>(configureOptions ?? (o => { }))
                 {
                     Name = optionsName
                 });
        }

        /// <summary>
        /// Adds the <see cref="OpenIdConnectAuthenticationMiddleware"/> into the ASP.NET runtime.
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="options">Options which control the processing of the OpenIdConnect protocol and token validation.</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseOpenIdConnectAuthentication(this IApplicationBuilder app, IOptions<OpenIdConnectAuthenticationOptions> options)
        {
            return app.UseMiddleware<OpenIdConnectAuthenticationMiddleware>(options);
        }
    }
}
