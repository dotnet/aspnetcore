// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.AspNetCore.SpaServices.Extensions.Proxy;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for proxying requests to a local SPA development server during
    /// development. Not for use in production applications.
    /// </summary>
    public static class SpaProxyingExtensions
    {
        /// <summary>
        /// Configures the application to forward incoming requests to a local Single Page
        /// Application (SPA) development server. This is only intended to be used during
        /// development. Do not enable this middleware in production applications.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="baseUri">The target base URI to which requests should be proxied.</param>
        public static void UseProxyToSpaDevelopmentServer(
            this ISpaBuilder spaBuilder,
            string baseUri)
        {
            UseProxyToSpaDevelopmentServer(
                spaBuilder,
                new Uri(baseUri));
        }

        /// <summary>
        /// Configures the application to forward incoming requests to a local Single Page
        /// Application (SPA) development server. This is only intended to be used during
        /// development. Do not enable this middleware in production applications.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="baseUri">The target base URI to which requests should be proxied.</param>
        public static void UseProxyToSpaDevelopmentServer(
            this ISpaBuilder spaBuilder,
            Uri baseUri)
        {
            UseProxyToSpaDevelopmentServer(
                spaBuilder,
                () => Task.FromResult(baseUri));
        }

        /// <summary>
        /// Configures the application to forward incoming requests to a local Single Page
        /// Application (SPA) development server. This is only intended to be used during
        /// development. Do not enable this middleware in production applications.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="baseUriTaskFactory">A callback that will be invoked on each request to supply a <see cref="Task"/> that resolves with the target base URI to which requests should be proxied.</param>
        public static void UseProxyToSpaDevelopmentServer(
            this ISpaBuilder spaBuilder,
            Func<Task<Uri>> baseUriTaskFactory)
        {
            var applicationBuilder = spaBuilder.ApplicationBuilder;
            var applicationStoppingToken = GetStoppingToken(applicationBuilder);

            // Since we might want to proxy WebSockets requests (e.g., by default, AngularCliMiddleware
            // requires it), enable it for the app
            applicationBuilder.UseWebSockets();

            // It's important not to time out the requests, as some of them might be to
            // server-sent event endpoints or similar, where it's expected that the response
            // takes an unlimited time and never actually completes
            var neverTimeOutHttpClient =
                SpaProxy.CreateHttpClientForProxy(Timeout.InfiniteTimeSpan);

            // Proxy all requests to the SPA development server
            applicationBuilder.Use(async (context, next) =>
            {
                var didProxyRequest = await SpaProxy.PerformProxyRequest(
                    context, neverTimeOutHttpClient, baseUriTaskFactory(), applicationStoppingToken,
                    proxy404s: true);
            });
        }

        private static CancellationToken GetStoppingToken(IApplicationBuilder appBuilder)
        {
            var applicationLifetime = appBuilder
                .ApplicationServices
                .GetService(typeof(IHostApplicationLifetime));
            return ((IHostApplicationLifetime)applicationLifetime).ApplicationStopping;
        }
    }
}
