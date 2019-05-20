// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Builder
{
    public static class CertificateForwarderExtensions
    {
        /// <summary>
        /// Adds a middleware to the pipeline that will look for a certificate in a request header
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
        /// <param name="configure">An action delegate to configure the provided <see cref="CertificateForwarderOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddCertificateHeaderForwarding(
            this IServiceCollection services,
            Action<CertificateForwarderOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return services.Configure(configure);
        }
    }
}
