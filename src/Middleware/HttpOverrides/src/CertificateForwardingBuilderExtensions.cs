// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpOverrides;

namespace Microsoft.AspNetCore.Builder;

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
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<CertificateForwardingMiddleware>();
    }
}
