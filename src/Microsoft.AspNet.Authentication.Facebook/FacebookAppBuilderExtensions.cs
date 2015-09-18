// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Facebook;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="FacebookMiddleware"/>.
    /// </summary>
    public static class FacebookAppBuilderExtensions
    {
        /// <summary>
        /// Authenticate users using Facebook.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseFacebookAuthentication([NotNull] this IApplicationBuilder app, [NotNull] FacebookOptions options)
        {
            return app.UseMiddleware<FacebookMiddleware>(options);
        }

        /// <summary>
        /// Authenticate users using Facebook.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="configureOptions">Configures the options.</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseFacebookAuthentication([NotNull] this IApplicationBuilder app, Action<FacebookOptions> configureOptions)
        {
            var options = new FacebookOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseFacebookAuthentication(options);
        }
    }
}
