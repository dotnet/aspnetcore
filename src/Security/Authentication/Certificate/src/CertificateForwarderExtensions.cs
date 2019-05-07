// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

using Microsoft.AspNetCore.Builder;

using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Hosting
{
    public static class CertificateForwarderExtensions
    {
        /// <summary>
        /// Adds a middleware to the pipeline that will look for a base64 encoded certificate in a request header
        /// and put that certificate on the request client certificate property.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCertificateHeaderForwarding(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<CertificateForwarderMiddleware>();
        }

        /// <summary>
        /// Adds certificate forwarding to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="certificateHeader"></param>
        /// <param name="converter"></param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddCertificateHeaderForwarding(
            this IServiceCollection services,
            string certificateHeader = "X-ARR-ClientCert",
            Func<string, X509Certificate2> converter = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // No op if certificate header is explicitly not set.
            if (string.IsNullOrEmpty(certificateHeader))
            {
                return services;
            }

            if (converter == null)
            {
                converter = (headerValue) => new X509Certificate2(Convert.FromBase64String(headerValue));
            }

            services.AddHeaderPropagation(options =>
            {
                // Generate a new X-BetaFeatures if not present.
                options.Headers.Add(headerName, context =>
                {
                    var clientCertificate = await context.HttpContext.Connection.GetClientCertificateAsync();

                    if (clientCertificate == null)
                    {
                        // Check for forwarding header
                        string certificateHeader = context.HttpContext.Request.Headers[certificateHeader];
                        if (!string.IsNullOrEmpty(certificateHeader))
                        {
                            try
                            {
                                httpContext.Connection.ClientCertificate = converter(certificateHeader);
                            }
                            catch
                            {
                                _logger.LogError("Could not read certificate from header.");
                            }
                        }
                    }
                });
            });

            return services;
        }
    }
}
