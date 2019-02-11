// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using System;

namespace Microsoft.AspNetCore.SpaServices.DevelopmentServer
{
    /// <summary>
    /// Extension methods for enabling React development server middleware support.
    /// </summary>
    public static class DevelopmentServerMiddlewareExtensions
    {
        /// <summary>
        /// Handles requests by passing them through to an instance of a development npm web server.
        /// This means you can always serve up-to-date CLI-built resources without having
        /// to run the npm web server manually.
        ///
        /// This feature should only be used in development. For production deployments, be
        /// sure not to enable the npm web server.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="npmScript">The name of the script in your package.json file that launches the web server.</param>
        /// <param name="waitText">The text snippet identified during the build to indicate the Development Server has compiled and is ready.</param>
        /// <param name="serverName">The name of the Server used in the Console.</param>
        public static void UseDevelopmentServer(
            this ISpaBuilder spaBuilder,
            string npmScript,
            string waitText,
            string serverName = "App")
        {

            if (string.IsNullOrEmpty(waitText))
            {
                throw new InvalidOperationException($"To use {nameof(UseDevelopmentServer)}, you must supply a non-empty value for the {nameof(waitText)} parameter. This allows us the find when the Development Server has started.");
            }

            if (spaBuilder == null)
            {
                throw new ArgumentNullException(nameof(spaBuilder));
            }

            var spaOptions = spaBuilder.Options;

            if (string.IsNullOrEmpty(spaOptions.SourcePath))
            {
                throw new InvalidOperationException($"To use {nameof(UseDevelopmentServer)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            DevelopmentServerMiddleware.Attach(spaBuilder, npmScript, waitText, serverName);
        }

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
            UseDevelopmentServer(spaBuilder, npmScript, "Starting the development server", "create-react-app");
        }

        /// <summary>
        /// Handles requests by passing them through to an instance of the vue-cli-service server.
        /// This means you can always serve up-to-date CLI-built resources without having
        /// to run the vue-cli-service server manually.
        ///
        /// This feature should only be used in development. For production deployments, be
        /// sure not to enable the vue-cli-service server.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="npmScript">The name of the script in your package.json file that launches the vue-cli-service server.</param>
        public static void UseVueDevelopmentServer(
            this ISpaBuilder spaBuilder,
            string npmScript)
        {
            UseDevelopmentServer(spaBuilder, npmScript, "Starting development server...", "vue-cli-service");
        }
    }
}
