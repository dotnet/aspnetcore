// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// The <see cref="IApplicationBuilder"/> extensions for adding CORS middleware support.
    /// </summary>
    public static class CorsMiddlewareExtensions
    {
        /// <summary>
        /// Adds a CORS middleware to your web application pipeline to allow cross domain requests.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to your Configure method</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseCors(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<CorsMiddleware>();
        }

        /// <summary>
        /// Adds a CORS middleware to your web application pipeline to allow cross domain requests.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to your Configure method</param>
        /// <param name="policyName">The policy name of a configured policy.</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseCors(this IApplicationBuilder app, string policyName)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<CorsMiddleware>(policyName);
        }

        /// <summary>
        /// Adds a CORS middleware to your web application pipeline to allow cross domain requests.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to your Configure method.</param>
        /// <param name="configurePolicy">A delegate which can use a policy builder to build a policy.</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseCors(
            this IApplicationBuilder app,
            Action<CorsPolicyBuilder> configurePolicy)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (configurePolicy == null)
            {
                throw new ArgumentNullException(nameof(configurePolicy));
            }

            var policyBuilder = new CorsPolicyBuilder();
            configurePolicy(policyBuilder);
            return app.UseMiddleware<CorsMiddleware>(policyBuilder.Build());
        }
    }
}