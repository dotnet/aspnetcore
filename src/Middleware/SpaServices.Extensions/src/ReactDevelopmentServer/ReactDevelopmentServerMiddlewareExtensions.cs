// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer
{
    /// <summary>
    /// Extension methods for enabling React development server middleware support.
    /// </summary>
    public static class ReactDevelopmentServerMiddlewareExtensions
    {
        /// <summary>
        /// Handles requests by passing them through to an instance of the create-react-app server.
        /// This means you can always serve up-to-date CLI-built resources without having
        /// to run the create-react-app server manually.
        ///
        /// This feature should only be used in development. For production deployments, be
        /// sure not to enable the create-react-app server.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="npmScript">The name of the script in your package.json file that launches the create-react-app server.</param>
        public static void UseReactDevelopmentServer(
            this ISpaBuilder spaBuilder,
            string npmScript)
        {
            if (spaBuilder == null)
            {
                throw new ArgumentNullException(nameof(spaBuilder));
            }

            var spaOptions = spaBuilder.Options;

            if (string.IsNullOrEmpty(spaOptions.SourcePath))
            {
                throw new InvalidOperationException($"To use {nameof(UseReactDevelopmentServer)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            ReactDevelopmentServerMiddleware.Attach(spaBuilder, npmScript);
        }
    }
}
