// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the HttpsRedirection middleware.
    /// </summary>
    public static class HttpsPolicyBuilderExtensions
    {
        /// <summary>
        /// Adds middleware for redirecting HTTP Requests to HTTPS.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for HttpsRedirection.</returns>
        /// <remarks>
        /// HTTPS Enforcement interanlly uses the UrlRewrite middleware to redirect HTTP requests to HTTPS.
        /// </remarks>
        public static IApplicationBuilder UseHttpsRedirection(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var options = app.ApplicationServices.GetRequiredService<IOptions<HttpsRedirectionOptions>>().Value;

            // The tls port set in options will have priority over the one in configuration.
            var httpsPort = options.HttpsPort;
            if (httpsPort == null)
            {
                // Only read configuration if there is no httpsPort
                var config = app.ApplicationServices.GetRequiredService<IConfiguration>();
                var configHttpsPort = config["HTTPS_PORT"];
                // If the string isn't empty, try to parse it.
                if (!string.IsNullOrEmpty(configHttpsPort)
                    && int.TryParse(configHttpsPort, out var intHttpsPort))
                {
                    httpsPort = intHttpsPort;
                }
            }

            var rewriteOptions = new RewriteOptions();
            rewriteOptions.AddRedirectToHttps(
                options.RedirectStatusCode,
                httpsPort);

            app.UseRewriter(rewriteOptions);

            return app;
        }
    }
}
