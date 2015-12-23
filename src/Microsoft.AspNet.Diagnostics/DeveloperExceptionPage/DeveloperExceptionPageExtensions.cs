// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// <see cref="IApplicationBuilder"/> extension methods for the <see cref="DeveloperExceptionPageMiddleware"/>.
    /// </summary>
    public static class DeveloperExceptionPageExtensions
    {
        /// <summary>
        /// Captures synchronous and asynchronous <see cref="Exception"/> instances from the pipeline and generates HTML error responses.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseDeveloperExceptionPage(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseDeveloperExceptionPage(options => { });
        }

        /// <summary>
        /// Captures synchronous and asynchronous <see cref="Exception"/> instances from the pipeline and generates HTML error responses.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="configureOptions">A callback to configure <see cref="DeveloperExceptionPageOptions"/>.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseDeveloperExceptionPage(
            this IApplicationBuilder app,
            Action<DeveloperExceptionPageOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new DeveloperExceptionPageOptions();
            configureOptions(options);
            return app.UseMiddleware<DeveloperExceptionPageMiddleware>(options);
        }
    }
}
