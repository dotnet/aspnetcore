// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.AspNet.Diagnostics.Entity;
using Microsoft.AspNet.Diagnostics.Entity.Utilities;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// <see cref="IApplicationBuilder"/> extension methods for the <see cref="MigrationsEndPointMiddleware"/>.
    /// </summary>
    public static class MigrationsEndPointExtensions
    {
        /// <summary>
        /// Processes requests to execute migrations operations. The middleware will listen for requests made to <see cref="MigrationsEndPointOptions.DefaultPath"/>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to register the middleware with.</param>
        /// <returns>The same <see cref="IApplicationBuilder"/> instance so that multiple calls can be chained.</returns>
        public static IApplicationBuilder UseMigrationsEndPoint(this IApplicationBuilder app)
        {
            return app.UseMigrationsEndPoint(options => { });
        }

        /// <summary>
        /// Processes requests to execute migrations operations. The middleware will listen for requests to the path configured in <paramref name="configureOptions"/>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to register the middleware with.</param>
        /// <param name="configureOptions">An action to set the options for the middleware.</param>
        /// <returns>The same <see cref="IApplicationBuilder"/> instance so that multiple calls can be chained.</returns>
        public static IApplicationBuilder UseMigrationsEndPoint(this IApplicationBuilder app, Action<MigrationsEndPointOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new MigrationsEndPointOptions();
            configureOptions(options);

            return app.UseMiddleware<MigrationsEndPointMiddleware>(options);
        }
    }
}
