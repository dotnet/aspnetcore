// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Csp
{
    /// <summary>
    /// Extends <see cref="IApplicationBuilder"/> to add CSP middleware support.
    /// </summary>
    public static class CspMiddlewareExtensions
    {
        /// <summary>
        /// Adds a CSP middleware to this web application pipeline that will add a custom policy to responses and collect CSP violation reports sent by user agents.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to the Configure method</param>
        /// <param name="configurePolicy">A delegate to build a custom content security policy</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseCsp(this IApplicationBuilder app, Action<ContentSecurityPolicyBuilder> configurePolicy)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var policyBuilder = new ContentSecurityPolicyBuilder();
            configurePolicy(policyBuilder);

            if (policyBuilder.HasLocalReporting())
            {
                var loggerFactory = app.ApplicationServices.GetService<ICspReportLoggerFactory>();
                var reportLogger = policyBuilder.ReportLogger(loggerFactory);
                app.UseWhen(
                    context => context.Request.Path.StartsWithSegments(reportLogger.ReportUri),
                    appBuilder => appBuilder.UseMiddleware<CspReportingMiddleware>(reportLogger));
            }

            return app.UseMiddleware<CspMiddleware>(policyBuilder.Build());
        }

        /// <summary>
        /// Adds the necessary bindings for CSP. Namely, allows adding nonces to script tags automatically and provides a custom logging factory.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to the Configure method</param>
        /// <returns>The original services parameter</returns>
        public static IServiceCollection AddCsp(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddScoped<INonce, Nonce>();
            services.AddSingleton<ICspReportLoggerFactory, CspReportLoggerFactory>();

            return services;
        }
    }
}
