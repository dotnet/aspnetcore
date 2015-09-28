// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.OpenIdConnect;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="OpenIdConnectMiddleware"/>
    /// </summary>
    public static class OpenIdConnectExtensions
    {
        /// <summary>
        /// Adds the <see cref="OpenIdConnectMiddleware"/> into the ASP.NET runtime.
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="options">Options which control the processing of the OpenIdConnect protocol and token validation.</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseOpenIdConnectAuthentication(this IApplicationBuilder app, Action<OpenIdConnectOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }


            var options = new OpenIdConnectOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseOpenIdConnectAuthentication(options);
        }

        /// <summary>
        /// Adds the <see cref="OpenIdConnectMiddleware"/> into the ASP.NET runtime.
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="options">Options which control the processing of the OpenIdConnect protocol and token validation.</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseOpenIdConnectAuthentication(this IApplicationBuilder app, OpenIdConnectOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<OpenIdConnectMiddleware>(options);
        }
    }
}
