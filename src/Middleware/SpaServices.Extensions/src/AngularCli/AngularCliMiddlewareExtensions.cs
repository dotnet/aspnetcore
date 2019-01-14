// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using System;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    /// <summary>
    /// Extension methods for enabling Angular CLI middleware support.
    /// </summary>
    public static class AngularCliMiddlewareExtensions
    {
        /// <summary>
        /// Handles requests by passing them through to an instance of the Angular CLI server.
        /// This means you can always serve up-to-date CLI-built resources without having
        /// to run the Angular CLI server manually.
        ///
        /// This feature should only be used in development. For production deployments, be
        /// sure not to enable the Angular CLI server.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="npmScript">The name of the script in your package.json file that launches the Angular CLI process.</param>
        public static void UseAngularCliServer(
            this ISpaBuilder spaBuilder,
            string npmScript)
        {
            UseAngularCliServer(spaBuilder, (options) => options.npmScript = npmScript);
        }
        /// <summary>
        /// Handles requests by passing them through to an instance of the Angular CLI server.
        /// This means you can always serve up-to-date CLI-built resources without having
        /// to run the Angular CLI server manually.
        ///
        /// This feature should only be used in development. For production deployments, be
        /// sure not to enable the Angular CLI server.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="configure">A callback used to configure the <see cref="AngularCliMiddlewareOptions"/>.</param>
        public static void UseAngularCliServer(
            this ISpaBuilder spaBuilder,
            Action<AngularCliMiddlewareOptions> configure)
        {
            if (spaBuilder == null)
            {
                throw new ArgumentNullException(nameof(spaBuilder));
            }

            var spaOptions = spaBuilder.Options;

            if (string.IsNullOrEmpty(spaOptions.SourcePath))
            {
                throw new InvalidOperationException($"To use {nameof(UseAngularCliServer)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            var devServerOptions = new AngularCliMiddlewareOptions();
            configure(devServerOptions);

            if(string.IsNullOrEmpty(devServerOptions.npmScript))
            {
                throw new ArgumentException($"{nameof(devServerOptions.npmScript)} has to be set in {nameof(configure)}.");
            }

            AngularCliMiddleware.Attach(spaBuilder, devServerOptions.npmScript, devServerOptions.spaPort);
        }
    }
}
