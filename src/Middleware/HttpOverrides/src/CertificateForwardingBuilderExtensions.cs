// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.HttpOverrides;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for using certificate forwarding.
    /// </summary>
    public static class CertificateForwardingBuilderExtensions
    {
        /// <summary>
        /// Adds a middleware to the pipeline that will look for a certificate in a request header
        /// decode it, and updates HttpContext.Connection.ClientCertificate.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCertificateForwarding(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<CertificateForwardingMiddleware>();
        }
    }
}
