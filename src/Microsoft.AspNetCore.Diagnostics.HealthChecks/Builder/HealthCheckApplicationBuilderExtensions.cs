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

            UseHealthChecksCore(app, path, port: null, Array.Empty<object>());
            return app;
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

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            UseHealthChecksCore(app, path, port: null, new[] { Options.Create(options), });
            return app;
        }

        /// <summary>
        /// Adds a middleware that provides health check status.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="path">The path on which to provide health check status.</param>
        /// <param name="port">The port to listen on. Must be a local port on which the server is listening.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        /// <remarks>
        /// <para>
        /// This method will use <see cref="MapWhenExtensions.MapWhen(IApplicationBuilder, Func{HttpContext, bool}, Action{IApplicationBuilder})"/> to
        /// listen to health checks requests on the specified URL path and port.
        /// </para>
        /// <para>
        /// The health check middleware will use default settings from <see cref="IOptions{HealthCheckOptions}"/>.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path, int port)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            UseHealthChecksCore(app, path, port, Array.Empty<object>());
            return app;
        }

        /// <summary>
        /// Adds a middleware that provides health check status.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="path">The path on which to provide health check status.</param>
        /// <param name="port">The port to listen on. Must be a local port on which the server is listening.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        /// <remarks>
        /// <para>
        /// This method will use <see cref="MapWhenExtensions.MapWhen(IApplicationBuilder, Func{HttpContext, bool}, Action{IApplicationBuilder})"/> to
        /// listen to health checks requests on the specified URL path and port.
        /// </para>
        /// <para>
        /// The health check middleware will use default settings from <see cref="IOptions{HealthCheckOptions}"/>.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path, string port)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (port == null)
            {
                throw new ArgumentNullException(nameof(port));
            }

            if (!int.TryParse(port, out var portAsInt))
            {
                throw new ArgumentException("The port must be a valid integer.", nameof(port));
            }

            UseHealthChecksCore(app, path, portAsInt, Array.Empty<object>());
            return app;
        }

        /// <summary>
        /// Adds a middleware that provides health check status.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="path">The path on which to provide health check status.</param>
        /// <param name="port">The port to listen on. Must be a local port on which the server is listening.</param>
        /// <param name="options">A <see cref="HealthCheckOptions"/> used to configure the middleware.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        /// <remarks>
        /// <para>
        /// This method will use <see cref="MapExtensions.Map(IApplicationBuilder, PathString, Action{IApplicationBuilder})"/> to
        /// listen to health checks requests on the specified URL path.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path, int port, HealthCheckOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            UseHealthChecksCore(app, path, port, new[] { Options.Create(options), });
            return app;
        }

        /// <summary>
        /// Adds a middleware that provides health check status.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="path">The path on which to provide health check status.</param>
        /// <param name="port">The port to listen on. Must be a local port on which the server is listening.</param>
        /// <param name="options">A <see cref="HealthCheckOptions"/> used to configure the middleware.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        /// <remarks>
        /// <para>
        /// This method will use <see cref="MapExtensions.Map(IApplicationBuilder, PathString, Action{IApplicationBuilder})"/> to
        /// listen to health checks requests on the specified URL path.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path, string port, HealthCheckOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(port));
            }

            if (!int.TryParse(port, out var portAsInt))
            {
                throw new ArgumentException("The port must be a valid integer.", nameof(port));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            UseHealthChecksCore(app, path, portAsInt, new[] { Options.Create(options), });
            return app;
        }

        private static void UseHealthChecksCore(IApplicationBuilder app, PathString path, int? port, object[] args)
        {
            if (port == null)
            {
                app.Map(path, b => b.UseMiddleware<HealthCheckMiddleware>(args));
            }
            else
            {
                app.MapWhen(
                    c => c.Connection.LocalPort == port,
                    b0 => b0.Map(path, b1 => b1.UseMiddleware<HealthCheckMiddleware>(args)));
            }
        }
    }
}
