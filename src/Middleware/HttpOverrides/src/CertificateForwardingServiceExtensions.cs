// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.HttpOverrides;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for using certificate fowarding.
    /// </summary>
    public static class CertificateForwardingServiceExtensions
    {
        /// <summary>
        /// Adds certificate forwarding to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">An action delegate to configure the provided <see cref="CertificateForwardingOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddCertificateForwarding(
            this IServiceCollection services,
            Action<CertificateForwardingOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.AddOptions<CertificateForwardingOptions>().Validate(o => !string.IsNullOrEmpty(o.CertificateHeader), "CertificateForwarderOptions.CertificateHeader cannot be null or empty.");
            return services.Configure(configure);
        }
    }
}
