// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// <see cref="IApplicationBuilder"/> extension methods for the <see cref="HealthCheckMiddleware"/>.
    /// </summary>
    public static class HealthCheckApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds a middleware that provides health check status.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="path">The path on which to provide health check status.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        /// <remarks>
        /// <para>
        /// This method will use <see cref="MapExtensions.Map(IApplicationBuilder, PathString, Action{IApplicationBuilder})"/> to
        /// listen to health checks requests on the specified URL path.
        /// </para>
        /// <para>
        /// The health check middleware will use default settings from <see cref="IOptions{HealthCheckOptions}"/>.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (!path.HasValue)
            {
                throw new ArgumentException("A URL path must be provided", nameof(path));
            }

            return app.Map(path, b => b.UseMiddleware<HealthCheckMiddleware>());
        }

        /// <summary>
        /// Adds a middleware that provides health check status.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="path">The path on which to provide health check status.</param>
        /// <param name="options">A <see cref="HealthCheckOptions"/> used to configure the middleware.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        /// <remarks>
        /// <para>
        /// This method will use <see cref="MapExtensions.Map(IApplicationBuilder, PathString, Action{IApplicationBuilder})"/> to
        /// listen to health checks requests on the specified URL path.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path, HealthCheckOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (!path.HasValue)
            {
                throw new ArgumentException("A URL path must be provided", nameof(path));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.Map(path, b => b.UseMiddleware<HealthCheckMiddleware>(Options.Create(options)));
        }
    }
}
